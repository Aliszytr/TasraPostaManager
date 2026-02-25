// Services/LabelLayoutService.cs
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    /// <summary>
    /// Tek etiket yerleşim motoru.
    /// V2 / V3 ayrımı yok; tüm yollar LabelSettings -> LabelLayoutResult zincirini kullanır.
    /// </summary>
    public class LabelLayoutService : ILabelLayoutService
    {
        private readonly ILogger<LabelLayoutService> _logger;

        public LabelLayoutService(ILogger<LabelLayoutService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public LabelLayoutResult CalculateLayout(LabelSettings settings, int totalItemCount)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (totalItemCount < 0)
                throw new ArgumentOutOfRangeException(nameof(totalItemCount));

            // 🧹 Tehlikeli değerleri normalize et (font 0, satır/sütun 0, negatif margin vb.)
            settings.NormalizeForSafety();

            // Temel validasyon
            ValidateSettings(settings);

            int rows = Math.Max(1, settings.Rows);
            int columns = Math.Max(1, settings.Columns);

            double labelWidthMm = settings.LabelWidthMm;
            double labelHeightMm = settings.LabelHeightMm;
            double gapH = Math.Max(0, settings.HorizontalGapMm);
            double gapV = Math.Max(0, settings.VerticalGapMm);

            double marginLeft = Math.Max(0, settings.LeftMarginMm);
            double marginTop = Math.Max(0, settings.TopMarginMm);
            double marginRight = Math.Max(0, settings.RightMarginMm);
            double marginBottom = Math.Max(0, settings.BottomMarginMm);

            // Sayfa boyutları LabelSettings'ten
            double pageWidthMm = settings.PageWidthMm;
            double pageHeightMm = settings.PageHeightMm;

            // ✅ DEBUG: Değerleri logla
            _logger.LogDebug(
                "LabelLayout CalculateLayout (before auto-fit): Page={PageWidth}x{PageHeight}mm, Label={LabelWidth}x{LabelHeight}mm, Margins={Left}/{Top}/{Right}/{Bottom}mm, Gaps={GapH}/{GapV}mm, Grid={Columns}x{Rows}",
                pageWidthMm, pageHeightMm, labelWidthMm, labelHeightMm,
                marginLeft, marginTop, marginRight, marginBottom,
                gapH, gapV, columns, rows);

            // 📐 Kullanılabilir alan
            double availableWidth = pageWidthMm - marginLeft - marginRight;
            double availableHeight = pageHeightMm - marginTop - marginBottom;

            if (availableWidth <= 0 || availableHeight <= 0)
            {
                // Bu durumda zaten sağlıklı yerleşim mümkün değil, sert hata vermek mantıklı
                throw new InvalidOperationException(
                    $"Sayfada kullanılabilir alan sıfır veya negatif. AvailableWidth={availableWidth}, AvailableHeight={availableHeight}.");
            }

            // 📏 Etiket alanının ihtiyaç duyduğu toplam genişlik/yükseklik
            double requiredWidth = (labelWidthMm * columns) + (gapH * (columns - 1));
            double requiredHeight = (labelHeightMm * rows) + (gapV * (rows - 1));

            // 🧠 Auto-Fit: Grid, sayfaya sığmıyorsa satır/sütun sayısını küçült
            bool gridAdjusted = false;

            // Önce tek etiketin bile sayfaya sığıp sığmadığını kontrol et
            if (labelWidthMm > availableWidth || labelHeightMm > availableHeight)
            {
                // Burada program patlamasın; sadece uyarı logu bırakıyoruz.
                _logger.LogWarning(
                    "Etiket boyutu sayfa içi kullanılabilir alandan büyük. Label={LabelWidth}x{LabelHeight}mm, Available={AvailableWidth}x{AvailableHeight}mm. Etiket sayfaya taşabilir.",
                    labelWidthMm, labelHeightMm, availableWidth, availableHeight);
            }
            else
            {
                // Yatayda maksimum sığabilecek sütun sayısı
                int maxColumnsFit = (int)Math.Floor(
                    (availableWidth + gapH) / (labelWidthMm + gapH));

                // Dikeyde maksimum sığabilecek satır sayısı
                int maxRowsFit = (int)Math.Floor(
                    (availableHeight + gapV) / (labelHeightMm + gapV));

                maxColumnsFit = Math.Max(1, maxColumnsFit);
                maxRowsFit = Math.Max(1, maxRowsFit);

                if (columns > maxColumnsFit)
                {
                    _logger.LogWarning(
                        "Sütun sayısı sayfaya sığmıyor. Columns={Columns} -> AutoFit={MaxColumnsFit}",
                        columns, maxColumnsFit);
                    columns = maxColumnsFit;
                    gridAdjusted = true;
                }

                if (rows > maxRowsFit)
                {
                    _logger.LogWarning(
                        "Satır sayısı sayfaya sığmıyor. Rows={Rows} -> AutoFit={MaxRowsFit}",
                        rows, maxRowsFit);
                    rows = maxRowsFit;
                    gridAdjusted = true;
                }

                // Auto-fit sonrası tekrar requiredWidth / requiredHeight hesapla
                requiredWidth = (labelWidthMm * columns) + (gapH * (columns - 1));
                requiredHeight = (labelHeightMm * rows) + (gapV * (rows - 1));
            }

            if (gridAdjusted)
            {
                _logger.LogInformation(
                    "Grid Auto-Fit uygulandı. Yeni Grid={Columns}x{Rows}, Required={RequiredWidth}x{RequiredHeight}mm, Available={AvailableWidth}x{AvailableHeight}mm",
                    columns, rows, requiredWidth, requiredHeight, availableWidth, availableHeight);
            }

            int labelsPerPage = rows * columns;
            if (labelsPerPage <= 0)
                throw new InvalidOperationException("Rows * Columns 0 hesaplandı. LabelSettings.Rows / Columns değerlerini kontrol edin.");

            int totalPages = totalItemCount == 0
                ? 1
                : (int)Math.Ceiling(totalItemCount / (double)labelsPerPage);

            var result = new LabelLayoutResult
            {
                Rows = rows,
                Columns = columns,
                TotalPages = totalPages,
                TotalLabels = totalItemCount,
                PageWidthMM = pageWidthMm,
                PageHeightMM = pageHeightMm,
                LabelPositions = new List<LabelPosition>(Math.Max(1, totalItemCount))
            };

            int currentIndex = 0;

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        if (currentIndex >= totalItemCount)
                            break;

                        double x = marginLeft + col * (labelWidthMm + gapH);
                        double y = marginTop + row * (labelHeightMm + gapV);

                        var pos = new LabelPosition
                        {
                            PageIndex = pageIndex,
                            RowIndex = row,
                            ColumnIndex = col,
                            X = x,
                            Y = y,
                            Width = labelWidthMm,
                            Height = labelHeightMm
                        };

                        result.LabelPositions.Add(pos);
                        currentIndex++;
                    }
                }
            }

            _logger.LogInformation(
                "LabelLayout hesaplandı. Page={PageWidth}x{PageHeight}mm, Rows={Rows}, Columns={Columns}, LabelsPerPage={LabelsPerPage}, TotalPages={TotalPages}, TotalLabels={TotalLabels}, AutoFit={AutoFitApplied}",
                pageWidthMm, pageHeightMm, rows, columns, labelsPerPage, totalPages, totalItemCount, gridAdjusted);

            return result;
        }

        /// <inheritdoc />
        public LabelLayoutResult CalculateLayout(LabelLayoutRequest request, int totalItemCount)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Eski LabelLayoutRequest modelini yeni LabelSettings modeline map ediyoruz.
            var settings = new LabelSettings
            {
                PaperSize = request.PaperSize,
                Orientation = request.Orientation,
                TopMarginMm = request.PageMarginTopMM,
                LeftMarginMm = request.PageMarginLeftMM,
                RightMarginMm = request.PageMarginRightMM,
                BottomMarginMm = request.PageMarginBottomMM,
                LabelWidthMm = request.LabelWidthMM,
                LabelHeightMm = request.LabelHeightMM,
                HorizontalGapMm = request.HorizontalSpacingMM,
                VerticalGapMm = request.VerticalSpacingMM,
                Columns = request.PreferredColumns.HasValue && request.PreferredColumns.Value > 0
                    ? request.PreferredColumns.Value
                    : 1,
                Rows = request.PreferredRows.HasValue && request.PreferredRows.Value > 0
                    ? request.PreferredRows.Value
                    : 1,
                Template = LabelTemplateType.Custom // request'ten şablon bilgisi gelmiyorsa Custom kabul ediyoruz
            };

            // Eğer request'te sayfa boyutları belirlenmişse ve Custom kâğıt kullanılıyorsa,
            // bunları doğrudan LabelSettings'in Custom boyutlarına yaz.
            if (request.PageWidthMM > 0 && request.PageHeightMM > 0 &&
                settings.PaperSize == PaperSizeType.Custom)
            {
                settings.CustomPageWidthMm = request.PageWidthMM;
                settings.CustomPageHeightMm = request.PageHeightMM;
            }

            // Tek motoru kullan
            return CalculateLayout(settings, totalItemCount);
        }

        /// <summary>
        /// Temel ayar validasyonlarını yapar.
        /// </summary>
        private void ValidateSettings(LabelSettings settings)
        {
            // Normalize edilmiş değerler üzerinden kontrol
            settings.NormalizeForSafety();

            if (settings.LabelWidthMm <= 0 || settings.LabelHeightMm <= 0)
                throw new InvalidOperationException("Etiket boyutları (LabelWidthMm, LabelHeightMm) 0 veya negatif olamaz.");

            if (settings.Rows <= 0)
                throw new InvalidOperationException("Rows 0 veya negatif olamaz.");

            if (settings.Columns <= 0)
                throw new InvalidOperationException("Columns 0 veya negatif olamaz.");

            var pageWidth = settings.PageWidthMm;
            var pageHeight = settings.PageHeightMm;

            if (pageWidth <= 0 || pageHeight <= 0)
                throw new InvalidOperationException($"Geçersiz sayfa boyutu: {pageWidth}x{pageHeight} mm");

            if (pageWidth < 50 || pageHeight < 50) // Minimum 50x50 mm
                throw new InvalidOperationException(
                    $"Sayfa boyutu çok küçük: {pageWidth}x{pageHeight} mm. Minimum 50x50 mm olmalı.");

            // Grid'in sayfaya sığıp sığmadığını burada throw etmek yerine,
            // Auto-Fit mekanizmasına bırakıyoruz. Burada sadece bilgi amaçlı log atabiliriz.
            double availableWidth = pageWidth - settings.LeftMarginMm - settings.RightMarginMm;
            double availableHeight = pageHeight - settings.TopMarginMm - settings.BottomMarginMm;

            double requiredWidth = (settings.LabelWidthMm * settings.Columns) +
                                   (settings.HorizontalGapMm * (settings.Columns - 1));
            double requiredHeight = (settings.LabelHeightMm * settings.Rows) +
                                    (settings.VerticalGapMm * (settings.Rows - 1));

            if (requiredWidth > availableWidth || requiredHeight > availableHeight)
            {
                _logger.LogWarning(
                    "Başlangıç grid boyutu sayfaya sığmıyor olabilir. Required={RequiredWidth}x{RequiredHeight}mm, Available={AvailableWidth}x{AvailableHeight}mm. Auto-Fit devreye girecek.",
                    requiredWidth, requiredHeight, availableWidth, availableHeight);
            }
        }
    }
}
