using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public class PdfService : IPdfService
    {
        private readonly ILabelLayoutService _labelLayoutService;
        private readonly IDynamicBarcodeService _barcodeService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILogger<PdfService> _logger;

        public PdfService(
            ILabelLayoutService labelLayoutService,
            IDynamicBarcodeService barcodeService,
            IAppSettingsService appSettingsService,
            ILogger<PdfService> logger)
        {
            _labelLayoutService = labelLayoutService;
            _barcodeService = barcodeService;
            _appSettingsService = appSettingsService;
            _logger = logger;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region LABELS V3

        public byte[] LabelsV3(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title = null)
            => LabelsV3Internal(records, settings, title, false);

        public byte[] LabelsV3(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title, bool hasOverflowWarning)
            => LabelsV3Internal(records, settings, title, hasOverflowWarning);

        private byte[] LabelsV3Internal(
            IReadOnlyList<PostaRecord> records,
            LabelSettings settings,
            string? title,
            bool hasOverflowWarning)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // Güvenli normalizasyon
            settings.NormalizeForSafety();

            var layout = _labelLayoutService.CalculateLayout(settings, records.Count);
            var defaultSender = _appSettingsService.GetDefaultGonderen() ?? string.Empty;

            // Barkod görsel parametreleri
            var barcodeConfig = _appSettingsService.GetBarcodeConfig();
            ResolveBarcodeRenderParams(settings, barcodeConfig, out var barcodeHeight, out var barcodeScale, out var showBarcodeText);

            var document = new LabelsDocument(
                records,
                settings,
                layout,
                title,
                defaultSender,
                _barcodeService,
                hasOverflowWarning,
                barcodeHeight,
                barcodeScale,
                showBarcodeText);

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return ms.ToArray();
        }

        #endregion

        #region LIST PDF

        public byte[] ListPdf(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title = null)
            => ListPdfInternal(records, settings, title, null);

        public byte[] ListPdf(
            IReadOnlyList<PostaRecord> records,
            LabelSettings settings,
            string? title,
            IReadOnlyList<string>? selectedFields)
            => ListPdfInternal(records, settings, title, selectedFields);

        private byte[] ListPdfInternal(
            IReadOnlyList<PostaRecord> records,
            LabelSettings settings,
            string? title,
            IReadOnlyList<string>? selectedFields)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // En az bir alan seçili olmalı, yoksa varsayılan ayarları uygula (Boş sayfa çıkmaması için)
            bool isAnyColumnSelected = settings.ListShowRowNumber ||
                                       settings.ListShowMuhabere ||
                                       settings.ListShowReceiver ||
                                       settings.ListShowAmount ||
                                       settings.ListShowDate ||
                                       settings.ListShowSignature ||
                                       settings.ListShowBarcode;

            if (!isAnyColumnSelected)
            {
                // Varsayılan olarak en az Muhabere No ve Alıcı göster
                settings.ListShowMuhabere = true;
                settings.ListShowReceiver = true;
                _logger.LogWarning("Liste PDF için hiçbir alan seçilmemiş. Varsayılan alanlar kullanılıyor.");
            }

            var listFontSize = settings.ListFontSize <= 0 ? 10 : settings.ListFontSize;
            if (listFontSize < 6) listFontSize = 6;

            var barcodeConfig = _appSettingsService.GetBarcodeConfig();
            ResolveBarcodeRenderParams(settings, barcodeConfig, out var barcodeHeight, out var barcodeScale, out var showBarcodeText);

            float listBarcodeTextFontSize = CalculateBarcodeTextFontSizeForList(settings, listFontSize);

            // 🔹 ÖNCE INDEX'LE (UI'dan gelen sırayı tutmak için)
            var indexedRecords = records
                .Select((r, index) => new { Record = r, Index = index })
                .ToList();

            // 🔹 KAYITLARI GRUPLA (Zarf bazında) ve sıra bilgisini koru
            var groupedRecords = indexedRecords
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Record.BarkodNo)
                    ? $"NOBARCODE_{x.Record.Id}"
                    : x.Record.BarkodNo)
                .Select(g =>
                {
                    // Bu grubun listedeki ilk görüldüğü index
                    int orderIndex = g.Min(x => x.Index);

                    var mainIndexed = g.First();
                    var main = mainIndexed.Record;

                    var allMuhs = g
                        .Select(x => x.Record.MuhabereNo)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct()
                        .OrderBy(m => m)
                        .ToList();

                    var displayMuh = PostaRecord.FormatMuhabereRange(allMuhs);
                    if (string.IsNullOrWhiteSpace(displayMuh))
                    {
                        displayMuh = main.GetFormattedMuhabereNo();
                    }

                    var totalAmountForGroup = g.Sum(x => x.Record.Miktar ?? 0);

                    return new
                    {
                        GroupKey = g.Key,
                        Main = main,
                        AllMuhabereNos = allMuhs,
                        DisplayMuhabereNo = displayMuh,
                        TotalAmount = totalAmountForGroup,
                        Count = g.Count(),
                        OrderIndex = orderIndex
                    };
                })
                .OrderBy(g => g.OrderIndex)
                .ToList();

            decimal totalAmount = groupedRecords.Sum(g => g.TotalAmount);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(10, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(listFontSize).FontFamily("Arial"));

                    // Başlık
                    page.Header().PaddingBottom(5).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item()
                               .Text(title ?? "PTT TOPLU GÖNDERİ TESLİM LİSTESİ")
                               .FontSize(14)
                               .Bold()
                               .AlignCenter();

                            col.Item()
                               .Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm} - Toplam Kayıt: {groupedRecords.Count}")
                               .FontSize(10)
                               .AlignCenter();
                        });
                    });

                    // İçerik
                    page.Content().PaddingTop(5).Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            // 1. ADIM: KOLON TANIMLARI
                            table.ColumnsDefinition(columns =>
                            {
                                if (settings.ListShowRowNumber) columns.ConstantColumn(25);
                                if (settings.ListShowMuhabere) columns.RelativeColumn(3);
                                if (settings.ListShowReceiver) columns.RelativeColumn(6);
                                if (settings.ListShowAmount) columns.RelativeColumn(2);
                                if (settings.ListShowDate) columns.RelativeColumn(2);
                                if (settings.ListShowSignature) columns.RelativeColumn(2);
                                if (settings.ListShowBarcode) columns.RelativeColumn(3);
                            });

                            // 2. ADIM: TABLO BAŞLIĞI
                            table.Header(header =>
                            {
                                if (settings.ListShowRowNumber) header.Cell().Element(HeaderStyle).Text("#");
                                if (settings.ListShowMuhabere) header.Cell().Element(HeaderStyle).Text("Muhabere No");
                                if (settings.ListShowReceiver) header.Cell().Element(HeaderStyle).Text("Alıcı Adı / Adres");
                                if (settings.ListShowAmount) header.Cell().Element(HeaderStyle).Text("Tutar (TL)");
                                if (settings.ListShowDate) header.Cell().Element(HeaderStyle).Text("Tarih");
                                if (settings.ListShowSignature) header.Cell().Element(HeaderStyle).Text("İmza");
                                if (settings.ListShowBarcode) header.Cell().Element(HeaderStyle).Text("Barkod");
                            });

                            // 3. ADIM: SATIRLAR
                            int counter = 1;
                            foreach (var group in groupedRecords)
                            {
                                var record = group.Main;

                                if (settings.ListShowRowNumber)
                                {
                                    table.Cell().Element(CellStyle).AlignCenter().Text(counter.ToString());
                                }

                                if (settings.ListShowMuhabere)
                                {
                                    var muhabereDisplay = string.IsNullOrWhiteSpace(group.DisplayMuhabereNo)
                                        ? (record.MuhabereNo ?? "-")
                                        : group.DisplayMuhabereNo;

                                    table.Cell()
                                         .Element(CellStyle)
                                         .AlignLeft()
                                         .PaddingLeft(5)
                                         .Text(muhabereDisplay)
                                         .FontSize(listFontSize); // DÜZELTİLDİ: Artık tam font size kullanıyor
                                }

                                if (settings.ListShowReceiver)
                                {
                                    table.Cell()
                                         .Element(CellStyle)
                                         .PaddingLeft(2)
                                         .PaddingRight(2)
                                         .Text(record.GittigiYer ?? "-")
                                         .FontSize(listFontSize); // DÜZELTİLDİ: Artık tam font size kullanıyor
                                }

                                if (settings.ListShowAmount)
                                {
                                    var tutarText = $"{group.TotalAmount:N2}";
                                    table.Cell().Element(CellStyle).AlignCenter().Text(tutarText);
                                }

                                if (settings.ListShowDate)
                                {
                                    string tarihStr = record.Tarih.HasValue
                                        ? record.Tarih.Value.ToString("dd.MM.yyyy")
                                        : "-";
                                    table.Cell().Element(CellStyle).AlignCenter().Text(tarihStr);
                                }

                                if (settings.ListShowSignature)
                                {
                                    table.Cell().Element(CellStyle).Height(15).Text(string.Empty);
                                }

                                // BARKOD SÜTUNU MANTIĞI
                                if (settings.ListShowBarcode)
                                {
                                    table.Cell().Element(CellStyle).Column(col =>
                                    {
                                        if (!string.IsNullOrEmpty(record.BarkodNo))
                                        {
                                            if (settings.IncludeBarcode)
                                            {
                                                try
                                                {
                                                    var barcodeBytes = _barcodeService.RenderCode128Png(
                                                        record.BarkodNo,
                                                        height: barcodeHeight,
                                                        margin: 5,
                                                        scale: barcodeScale);

                                                    col.Item()
                                                       .AlignCenter()
                                                       .MaxWidth(60)
                                                       .MinWidth(40)
                                                       .Height(25)
                                                       .Image(barcodeBytes)
                                                       .FitWidth();

                                                    if (showBarcodeText)
                                                    {
                                                        col.Item()
                                                           .AlignCenter()
                                                           .PaddingTop(2)
                                                           .Text(record.BarkodNo)
                                                           .FontSize(listBarcodeTextFontSize);
                                                    }
                                                }
                                                catch
                                                {
                                                    col.Item().AlignCenter().Text(record.BarkodNo).FontSize(listBarcodeTextFontSize);
                                                }
                                            }
                                            else
                                            {
                                                col.Item().AlignCenter().Text(record.BarkodNo).FontSize(listBarcodeTextFontSize);
                                            }
                                        }
                                        else
                                        {
                                            col.Item().AlignCenter().Text("-");
                                        }
                                    });
                                }

                                counter++;
                            }

                            static IContainer HeaderStyle(IContainer container) =>
                                container.BorderBottom(1)
                                         .BorderColor(Colors.Black)
                                         .PaddingVertical(4)
                                         .PaddingHorizontal(2)
                                         .DefaultTextStyle(x => x.Bold())
                                         .AlignMiddle();

                            static IContainer CellStyle(IContainer container) =>
                                container.BorderBottom(1)
                                         .BorderColor(Colors.Grey.Lighten3)
                                         .PaddingVertical(4)
                                         .PaddingHorizontal(2)
                                         .AlignMiddle()
                                         .MinHeight(25);
                        });

                        if (settings.ListShowAmount && totalAmount > 0)
                        {
                            column.Item()
                                  .AlignRight()
                                  .PaddingTop(5)
                                  .Text($"Liste Toplamı: {totalAmount:N2} TL")
                                  .FontSize(listFontSize + 1)
                                  .Bold();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            using var ms = new MemoryStream();
            try
            {
                document.GeneratePdf(ms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Liste PDF oluşturma hatası");
                throw;
            }

            return ms.ToArray();
        }

        #endregion

        #region HELPERS

        private void ResolveBarcodeRenderParams(
           LabelSettings settings,
           BarcodeConfig config,
           out int height,
           out int scale,
           out bool showText)
        {
            var size = (settings.BarcodeSize ?? config.DefaultBarcodeSize ?? "medium").Trim().ToLowerInvariant();
            var emphasis = settings.BarcodeEmphasis;
            if (emphasis <= 0) emphasis = 1;
            if (emphasis > 4) emphasis = 4;

            int hSmall = config.BarcodeHeightSmall > 0 ? config.BarcodeHeightSmall : 12;
            int hMed = config.BarcodeHeightMedium > 0 ? config.BarcodeHeightMedium : 20;
            int hLarge = config.BarcodeHeightLarge > 0 ? config.BarcodeHeightLarge : 30;
            int hXl = config.BarcodeHeightXLarge > 0 ? config.BarcodeHeightXLarge : 40;

            switch (size)
            {
                case "small":
                    height = hSmall;
                    scale = Math.Max(1, emphasis - 1);
                    break;
                case "large":
                    height = hLarge;
                    scale = emphasis + 1;
                    break;
                case "xlarge":
                case "xl":
                    height = hXl;
                    scale = emphasis + 1;
                    break;
                default:
                    height = hMed;
                    scale = emphasis;
                    break;
            }

            if (scale < 1) scale = 1;
            if (scale > 5) scale = 5;
            showText = settings.ShowBarcodeText;
        }

        private static float CalculateBarcodeTextFontSizeForList(LabelSettings settings, int listFontSize)
        {
            int baseSize = (settings.BarcodeFontSize.HasValue && settings.BarcodeFontSize.Value > 0)
                ? settings.BarcodeFontSize.Value
                : listFontSize;

            if (baseSize < 6) baseSize = 6;
            if (baseSize > 72) baseSize = 72;

            int scalePercent = settings.BarcodeTextScalePercent <= 0 ? 100 : settings.BarcodeTextScalePercent;
            if (scalePercent < 10) scalePercent = 10;
            if (scalePercent > 300) scalePercent = 300;

            var scaled = baseSize * (scalePercent / 100f);
            return Math.Clamp(scaled, 6f, 96f);
        }

        private static float CalculateBarcodeTextFontSizeForLabel(LabelSettings settings)
        {
            float fs = settings.FontSize <= 0 ? 10 : settings.FontSize;
            int baseSize = (settings.BarcodeFontSize.HasValue && settings.BarcodeFontSize.Value > 0)
                ? settings.BarcodeFontSize.Value
                : (int)Math.Round(fs - 2);

            if (baseSize < 6) baseSize = 6;
            if (baseSize > 72) baseSize = 72;

            int scalePercent = settings.BarcodeTextScalePercent <= 0 ? 100 : settings.BarcodeTextScalePercent;
            var scaled = baseSize * (scalePercent / 100f);
            return Math.Clamp(scaled, 6f, 96f);
        }

        #endregion

        #region INNER LABEL DOCUMENT

        private class LabelsDocument : IDocument
        {
            private readonly IReadOnlyList<PostaRecord> _records;
            private readonly LabelSettings _settings;
            private readonly LabelLayoutResult _layout;
            private readonly string _defaultSender;
            private readonly IDynamicBarcodeService _barcodeService;
            private readonly bool _showOverflowWarning;
            private readonly int _barcodeHeight;
            private readonly int _barcodeScale;
            private readonly bool _showBarcodeText;

            public LabelsDocument(
                IReadOnlyList<PostaRecord> records,
                LabelSettings settings,
                LabelLayoutResult layout,
                string? title,
                string defaultSender,
                IDynamicBarcodeService barcodeService,
                bool showOverflowWarning,
                int barcodeHeight,
                int barcodeScale,
                bool showBarcodeText)
            {
                _records = records;
                _settings = settings;
                _layout = layout;
                Title = title;
                _defaultSender = defaultSender;
                _barcodeService = barcodeService;
                _showOverflowWarning = showOverflowWarning;
                _barcodeHeight = barcodeHeight;
                _barcodeScale = barcodeScale;
                _showBarcodeText = showBarcodeText;
            }

            public string? Title { get; }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    float mmToPt = 2.83465f;

                    page.Size(new PageSize(
                        (float)(_settings.PageWidthMm * mmToPt),
                        (float)(_settings.PageHeightMm * mmToPt)));

                    page.Margin(0);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(TextStyle.Default.FontFamily("Arial").FontSize(_settings.FontSize));

                    if (_showOverflowWarning)
                    {
                        page.Header()
                            .Background(Colors.Red.Lighten4)
                            .Padding(5)
                            .AlignCenter()
                            .Text("UYARI: TAŞMA VAR - AYARLARI KONTROL EDİN")
                            .FontColor(Colors.Red.Darken4);
                    }

                    page.Content().Column(col =>
                    {
                        for (int i = 0; i < _layout.TotalPages; i++)
                        {
                            if (i > 0) col.Item().PageBreak();
                            ComposeSinglePage(col.Item(), i);
                        }
                    });
                });
            }

            private void ComposeSinglePage(IContainer container, int pageIndex)
            {
                float mmToPt = 2.83465f;

                container
                    .PaddingTop((float)_settings.TopMarginMm * mmToPt)
                    .PaddingLeft((float)_settings.LeftMarginMm * mmToPt)
                    .PaddingRight((float)_settings.RightMarginMm * mmToPt)
                    .PaddingBottom((float)_settings.BottomMarginMm * mmToPt)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            for (int c = 0; c < _layout.Columns; c++)
                            {
                                cols.ConstantColumn((float)_settings.LabelWidthMm * mmToPt);

                                if (c < _layout.Columns - 1 && _settings.HorizontalGapMm > 0)
                                    cols.ConstantColumn((float)_settings.HorizontalGapMm * mmToPt);
                            }
                        });

                        int itemsPerPage = _layout.Rows * _layout.Columns;
                        int startIndex = pageIndex * itemsPerPage;

                        for (int r = 0; r < _layout.Rows; r++)
                        {
                            for (int c = 0; c < _layout.Columns; c++)
                            {
                                int idx = startIndex + (r * _layout.Columns) + c;

                                table.Cell().Element(cell =>
                                {
                                    var box = cell
                                        .Width((float)_settings.LabelWidthMm * mmToPt)
                                        .Height((float)_settings.LabelHeightMm * mmToPt);

                                    if (_settings.DrawLabelBorders)
                                        box = box.Border(0.5f).BorderColor(Colors.Black);

                                    box = box.Padding(2);

                                    if (idx < _records.Count)
                                        ComposeLabelContent(box, _records[idx]);
                                });

                                if (c < _layout.Columns - 1 && _settings.HorizontalGapMm > 0)
                                    table.Cell(); // boş gap hücresi
                            }

                            if (r < _layout.Rows - 1 && _settings.VerticalGapMm > 0)
                            {
                                table.Cell()
                                     .ColumnSpan((uint)(_layout.Columns + (_layout.Columns - 1)))
                                     .Height((float)_settings.VerticalGapMm * mmToPt);
                            }
                        }
                    });
            }

            private void ComposeLabelContent(IContainer container, PostaRecord record)
            {
                float fs = _settings.FontSize <= 0 ? 10 : _settings.FontSize;
                float lineHeight = fs * 1.2f;
                float barcodeTextFontSize = CalculateBarcodeTextFontSizeForLabel(_settings);

                container.Layers(layers =>
                {
                    // 1. KATMAN: ANA AKIŞ
                    layers.PrimaryLayer().Column(col =>
                    {
                        // GÖNDEREN
                        if (_settings.ShowGonderenBilgisi)
                        {
                            col.Item()
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(1)
                                .Column(s =>
                                {
                                    s.Item()
                                     .Text("GÖNDEREN")
                                     .FontSize(fs - 4)
                                     .FontColor(Colors.Grey.Darken1);

                                    s.Item()
                                     .MaxHeight(lineHeight + 2)
                                     .Text(t => t.Span(_defaultSender).FontSize(fs - 1).Bold());
                                });
                        }

                        // ALICI
                        if (_settings.ShowGittigiYer)
                        {
                            var aliciText = record.GittigiYer ?? string.Empty;

                            // 🟢 Uzun adresler için dinamik punto hesaplama
                            // Standart: fs+2, uzun metinlerde kademeli küçültme
                            float aliciFontSize = fs + 2;
                            if (aliciText.Length > 50)
                                aliciFontSize = fs;        // 50+ karakter → normal punto
                            else if (aliciText.Length > 35)
                                aliciFontSize = fs + 1;    // 35-50 → biraz küçük

                            col.Item()
                               .PaddingVertical(2)
                               .Column(a =>
                               {
                                   a.Item()
                                    .Text("ALICI")
                                    .FontSize(fs - 4)
                                    .FontColor(Colors.Grey.Darken1);

                                   // MaxHeight ile etiket sınırı korunur, ScaleToFit ile taşma engellenir
                                   a.Item()
                                    .MaxHeight((lineHeight * 2) + 4)
                                    .ScaleToFit()
                                    .Text(t => t.Span(aliciText).FontSize(aliciFontSize).Bold());
                               });
                        }

                        // ALT SATIR
                        col.Item().Row(row =>
                        {
                            // SOL METİN BLOĞU
                            row.RelativeItem().Column(info =>
                            {
                                if (!_settings.ShowMuhabereBox &&
                                    _settings.ShowMuhabereNo &&
                                    !string.IsNullOrEmpty(record.MuhabereNo))
                                {
                                    info.Item()
                                        .Text($"Muh: {record.MuhabereNo}")
                                        .FontSize(fs - 2);
                                }

                                if (_settings.ShowMiktar)
                                {
                                    info.Item()
                                        .Text($"Tutar: {record.Miktar:N2} TL")
                                        .FontSize(fs - 2);
                                }

                                if (_settings.ShowTarih)
                                {
                                    info.Item()
                                        .Text(record.Tarih.HasValue
                                            ? record.Tarih.Value.ToString("dd.MM.yyyy")
                                            : "-")
                                        .FontSize(fs - 2);
                                }
                            });

                            // SAĞ BLOK: BARKOD ve METNİ
                            string barkodDegeri = record.BarkodNo ?? string.Empty;
                            if (_settings.ShowBarkodNo && !string.IsNullOrEmpty(barkodDegeri))
                            {
                                row.RelativeItem().Column(right =>
                                {
                                    if (_settings.IncludeBarcode)
                                    {
                                        try
                                        {
                                            var bytes = _barcodeService.RenderCode128Png(
                                                barkodDegeri,
                                                height: _barcodeHeight,
                                                margin: 0,
                                                scale: _barcodeScale);

                                            if (bytes != null)
                                            {
                                                right.Item()
                                                     .Height(_barcodeHeight)
                                                     .AlignRight()
                                                     .Image(bytes)
                                                     .FitArea();
                                            }
                                        }
                                        catch { /* Hata yönetimi */ }
                                    }

                                    if (_showBarcodeText)
                                    {
                                        right.Item()
                                             .Width(45, Unit.Millimetre)
                                             .Height(5, Unit.Millimetre)
                                             .PaddingRight(3)
                                             .PaddingTop(1)
                                             .AlignRight()
                                             .ScaleToFit()
                                             .Text(text =>
                                             {
                                                 text.Span(barkodDegeri)
                                                     .FontFamily("Courier New")
                                                     .FontSize(barcodeTextFontSize)
                                                     .Bold();
                                             });
                                    }
                                });
                            }
                        });
                    });

                    // 2. KATMAN: MUHABERE KUTUSU
                    if (_settings.ShowMuhabereBox && !string.IsNullOrEmpty(record.MuhabereNo))
                    {
                        layers.Layer()
                              .AlignBottom()
                              .AlignLeft()
                              .PaddingBottom(0)
                              .Element(boxContainer =>
                              {
                                  boxContainer.Row(r =>
                                  {
                                      r.AutoItem()
                                       .Border(1.5f)
                                       .BorderColor(Colors.Black)
                                       .Background(Colors.White)
                                       .PaddingVertical(2)
                                       .PaddingHorizontal(4)
                                       .Text(record.MuhabereNo)
                                       .FontSize(fs + 2)
                                       .Bold()
                                       .FontColor(Colors.Black);
                                  });
                              });
                    }
                });
            }
        }
        #endregion
    }
}