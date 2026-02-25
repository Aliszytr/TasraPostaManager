using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TasraPostaManager.Data;
using TasraPostaManager.Models;
using TasraPostaManager.Services;

namespace TasraPostaManager.Controllers
{
    [Authorize]
    public class LabelsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILabelLayoutService _labelLayoutService;
        private readonly IPdfService _pdf;
        private readonly IAppSettingsService _settings;
        private readonly ILogger<LabelsController> _logger;

        public LabelsController(
            AppDbContext db,
            ILabelLayoutService labelLayoutService,
            IPdfService pdf,
            IAppSettingsService settings,
            ILogger<LabelsController> logger)
        {
            _db = db;
            _labelLayoutService = labelLayoutService;
            _pdf = pdf;
            _settings = settings;
            _logger = logger;
        }

        // GET: /Labels/Index
        [HttpGet]
        public IActionResult Index(string? selected)
        {
            try
            {
                var recordsQuery = _db.PostaRecords.AsNoTracking().AsQueryable();
                List<PostaRecord> model;

                if (!string.IsNullOrEmpty(selected))
                {
                    var selectedTokens = GetSelectedIds(selected);
                    if (selectedTokens.Any())
                    {
                        var filteredQuery = ApplySelectionFilter(recordsQuery, selectedTokens);
                        var rawRecords = filteredQuery.ToList();
                        model = SortRecordsBySelectionOrder(rawRecords, selectedTokens);
                    }
                    else
                    {
                        model = new List<PostaRecord>();
                    }
                }
                else
                {
                    model = new List<PostaRecord>();
                }

                ViewBag.SelectedString = selected ?? string.Empty;
                ViewBag.DefaultGonderen = _settings.GetDefaultGonderen();

                // Ayarları veritabanından çek
                var s = _settings.GetLabelSettingsV2() ?? new LabelSettings();
                s.NormalizeForSafety();
                s.ApplyTemplateIfNeeded();

                PrepareViewBagSettings(s);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Labels/Index sırasında hata oluştu.");
                TempData["Error"] = "Etiket & liste oluşturma ekranı yüklenirken bir hata oluştu.";
                return RedirectToAction("Index", "Records");
            }
        }

        // POST: /Labels/GenerateList
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateList(
            string outputType,
            string selected,
            bool forceSave,
            [FromForm] LabelSettings inputSettings)
        {
            try
            {
                var selectedTokens = GetSelectedIds(selected);
                if (!selectedTokens.Any())
                {
                    TempData["Error"] = "İşlem yapılacak kayıt seçilmedi.";
                    return RedirectToAction("Index", "Records");
                }

                var query = ApplySelectionFilter(_db.PostaRecords.AsQueryable(), selectedTokens);
                var rawRecords = await query.ToListAsync();
                var records = SortRecordsBySelectionOrder(rawRecords, selectedTokens);

                // 1. Mevcut ayarları DB'den al
                var currentSettings = _settings.GetLabelSettingsV2() ?? new LabelSettings();

                // 2. Formdan gelen ayarları currentSettings'e işle (Override)
                if (inputSettings != null)
                {
                    // Tüm ayarları inputSettings'ten al, 0 veya false da olsa
                    currentSettings.LabelWidthMm = inputSettings.LabelWidthMm;
                    currentSettings.LabelHeightMm = inputSettings.LabelHeightMm;
                    currentSettings.TopMarginMm = inputSettings.TopMarginMm;
                    currentSettings.LeftMarginMm = inputSettings.LeftMarginMm;
                    currentSettings.RightMarginMm = inputSettings.RightMarginMm;
                    currentSettings.BottomMarginMm = inputSettings.BottomMarginMm;
                    currentSettings.Columns = inputSettings.Columns;
                    currentSettings.Rows = inputSettings.Rows;
                    currentSettings.HorizontalGapMm = inputSettings.HorizontalGapMm;
                    currentSettings.VerticalGapMm = inputSettings.VerticalGapMm;
                    currentSettings.FontSize = inputSettings.FontSize;
                    currentSettings.Template = inputSettings.Template;
                    currentSettings.PaperSize = inputSettings.PaperSize;
                    currentSettings.Orientation = inputSettings.Orientation;
                    currentSettings.IncludeBarcode = inputSettings.IncludeBarcode;
                    currentSettings.BarcodeTextScalePercent = inputSettings.BarcodeTextScalePercent;
                    currentSettings.BarcodeFontSize = inputSettings.BarcodeFontSize;
                    currentSettings.ListShowRowNumber = inputSettings.ListShowRowNumber;
                    currentSettings.ListShowMuhabere = inputSettings.ListShowMuhabere;
                    currentSettings.ListShowBarcode = inputSettings.ListShowBarcode;
                    currentSettings.ListShowReceiver = inputSettings.ListShowReceiver;
                    currentSettings.ListShowAmount = inputSettings.ListShowAmount;
                    currentSettings.ListShowDate = inputSettings.ListShowDate;
                    currentSettings.ListShowSignature = inputSettings.ListShowSignature;
                    currentSettings.ListFontSize = inputSettings.ListFontSize;
                }

                currentSettings.NormalizeForSafety();

                if (currentSettings.Template != LabelTemplateType.Custom)
                    currentSettings.ApplyTemplateFromDefinition();

                // Taşma kontrolü
                string overflowMessage = string.Empty;
                bool isListOutput = string.Equals(outputType, "list", StringComparison.OrdinalIgnoreCase);

                if (!isListOutput)
                {
                    overflowMessage = CheckIfSettingsFitPage(currentSettings);
                }

                var hasOverflowWarning = !string.IsNullOrEmpty(overflowMessage);

                // Ayarları Kaydetme Mantığı
                if (isListOutput)
                {
                    await _settings.SaveLabelSettingsV2Async(currentSettings);
                }
                else
                {
                    // Etiket ise ve taşma yoksa veya kullanıcı "Buna rağmen kaydet" dediyse kaydet
                    if (!hasOverflowWarning || forceSave)
                    {
                        await _settings.SaveLabelSettingsV2Async(currentSettings);
                    }
                    else
                    {
                        _logger.LogWarning("LabelSettings sayfaya sığmıyor (forceSave=false). Ayarlar kaydedilmedi.");
                        TempData["OverflowError"] = overflowMessage;
                        TempData["ShowForceSave"] = true;
                        return RedirectToAction("Index", new { selected = selected });
                    }
                }

                // PDF Üretimi
                byte[] fileBytes;
                string fileName;

                if (isListOutput)
                {
                    fileBytes = _pdf.ListPdf(records, currentSettings, "Posta Teslim Listesi");
                    fileName = $"Teslim_Listesi_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                }
                else
                {
                    var labelRecords = BuildLabelRecords(records);
                    fileBytes = _pdf.LabelsV3(labelRecords, currentSettings, "Etiketler", hasOverflowWarning);
                    fileName = $"Etiketler_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                }

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateList PDF oluşturma hatası.");
                TempData["Error"] = "PDF oluşturma sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index", new { selected = selected });
            }
        }

        // POST: /Labels/PreviewLabels
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewLabels(string? selected, [FromForm] LabelSettings? inputSettings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selected)) return RedirectToAction("Index");

                var selectedTokens = GetSelectedIds(selected);
                var query = ApplySelectionFilter(_db.PostaRecords.AsQueryable(), selectedTokens);
                var rawRecords = await query.ToListAsync();
                var records = SortRecordsBySelectionOrder(rawRecords, selectedTokens);

                if (!records.Any()) return RedirectToAction("Index");

                var settings = _settings.GetLabelSettingsV2() ?? new LabelSettings();

                // Override Settings - Tüm ayarları al
                if (inputSettings != null)
                {
                    settings.LabelWidthMm = inputSettings.LabelWidthMm;
                    settings.LabelHeightMm = inputSettings.LabelHeightMm;
                    settings.TopMarginMm = inputSettings.TopMarginMm;
                    settings.LeftMarginMm = inputSettings.LeftMarginMm;
                    settings.RightMarginMm = inputSettings.RightMarginMm;
                    settings.BottomMarginMm = inputSettings.BottomMarginMm;
                    settings.Columns = inputSettings.Columns;
                    settings.Rows = inputSettings.Rows;
                    settings.HorizontalGapMm = inputSettings.HorizontalGapMm;
                    settings.VerticalGapMm = inputSettings.VerticalGapMm;
                    settings.FontSize = inputSettings.FontSize;
                    settings.IncludeBarcode = inputSettings.IncludeBarcode;
                    settings.BarcodeFontSize = inputSettings.BarcodeFontSize;
                    settings.BarcodeTextScalePercent = inputSettings.BarcodeTextScalePercent;
                    settings.Template = inputSettings.Template;
                    settings.PaperSize = inputSettings.PaperSize;
                    settings.Orientation = inputSettings.Orientation;
                }

                settings.NormalizeForSafety();

                // Şablon özel değilse hesapla
                if (settings.Template != LabelTemplateType.Custom)
                    settings.ApplyTemplateFromDefinition();

                var overflowMessage = CheckIfSettingsFitPage(settings);
                bool hasWarning = !string.IsNullOrEmpty(overflowMessage);

                var labelRecords = BuildLabelRecords(records);
                var title = "ETİKET ÖNİZLEME";

                var pdfBytes = _pdf.LabelsV3(labelRecords, settings, title, hasWarning);
                var base64 = Convert.ToBase64String(pdfBytes);

                ViewBag.RecordCount = labelRecords.Count;
                ViewBag.TitleText = title;
                ViewBag.OverflowMessage = overflowMessage;
                ViewBag.HasOverflowWarning = hasWarning;

                return View("PreviewLabels", model: base64);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreviewLabels hatası.");
                TempData["Error"] = "Önizleme hatası.";
                return RedirectToAction("Index", new { selected });
            }
        }

        // POST: /Labels/PreviewList
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewList(string? selected, [FromForm] LabelSettings? inputSettings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selected)) return RedirectToAction("Index");

                var selectedTokens = GetSelectedIds(selected);
                var query = ApplySelectionFilter(_db.PostaRecords.AsQueryable(), selectedTokens);
                var rawRecords = await query.ToListAsync();
                var records = SortRecordsBySelectionOrder(rawRecords, selectedTokens);

                if (!records.Any()) return RedirectToAction("Index");

                var settings = _settings.GetLabelSettingsV2() ?? new LabelSettings();

                if (inputSettings != null)
                {
                    settings.ListShowRowNumber = inputSettings.ListShowRowNumber;
                    settings.ListShowMuhabere = inputSettings.ListShowMuhabere;
                    settings.ListShowBarcode = inputSettings.ListShowBarcode;
                    settings.ListShowReceiver = inputSettings.ListShowReceiver;
                    settings.ListShowAmount = inputSettings.ListShowAmount;
                    settings.ListShowDate = inputSettings.ListShowDate;
                    settings.ListShowSignature = inputSettings.ListShowSignature;
                    settings.ListFontSize = inputSettings.ListFontSize;
                }

                settings.NormalizeForSafety();

                var title = "TESLİM LİSTESİ ÖNİZLEME";
                var pdfBytes = _pdf.ListPdf(records, settings, title, selectedFields: null);
                var base64 = Convert.ToBase64String(pdfBytes);

                ViewBag.RecordCount = records.Count;
                ViewBag.TitleText = title;

                return View("PreviewList", model: base64);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreviewList hatası.");
                TempData["Error"] = "Liste önizleme hatası.";
                return RedirectToAction("Index", new { selected });
            }
        }

        // POST: /Labels/LayoutPreview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LayoutPreview([FromForm] LabelSettings inputSettings)
        {
            try
            {
                inputSettings ??= new LabelSettings();
                inputSettings.NormalizeForSafety();
                if (inputSettings.Template != LabelTemplateType.Custom)
                    inputSettings.ApplyTemplateFromDefinition();

                var layout = _labelLayoutService.CalculateLayout(inputSettings, totalItemCount: 20);
                var msg = CheckIfSettingsFitPage(inputSettings);
                ViewBag.ValidationMessage = msg;
                ViewBag.HasOverflowWarning = !string.IsNullOrEmpty(msg);

                return View("PreviewLayout", layout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LayoutPreview hatası");
                return RedirectToAction("Index", "Records");
            }
        }

        [AcceptVerbs("GET", "POST")]
        public IActionResult CreateFromRecords(string muhabereNos)
        {
            return RedirectToAction("Index", new { selected = muhabereNos });
        }

        #region Helpers

        private List<PostaRecord> SortRecordsBySelectionOrder(List<PostaRecord> records, List<string> selectedTokens)
        {
            if (records == null || !records.Any()) return new List<PostaRecord>();
            if (selectedTokens == null || !selectedTokens.Any()) return records;

            var orderMap = selectedTokens
                .Select((token, index) => new { Token = token, Index = index })
                .ToDictionary(x => x.Token, x => x.Index);

            return records
                .OrderBy(r =>
                {
                    if (!string.IsNullOrEmpty(r.MuhabereNo) && orderMap.ContainsKey(r.MuhabereNo))
                        return orderMap[r.MuhabereNo];
                    if (!string.IsNullOrEmpty(r.BarkodNo) && orderMap.ContainsKey(r.BarkodNo))
                        return orderMap[r.BarkodNo];
                    return int.MaxValue;
                })
                .ThenBy(r => r.MuhabereNo)
                .ToList();
        }

        private List<PostaRecord> BuildLabelRecords(List<PostaRecord> source)
        {
            var result = new List<PostaRecord>();
            if (source == null || source.Count == 0) return result;

            var processedBarcodes = new HashSet<string>();
            var groupsWithBarcode = source
                .Where(r => !string.IsNullOrWhiteSpace(r.BarkodNo))
                .GroupBy(r => r.BarkodNo!)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var record in source)
            {
                if (!string.IsNullOrWhiteSpace(record.BarkodNo))
                {
                    if (processedBarcodes.Contains(record.BarkodNo)) continue;

                    if (groupsWithBarcode.TryGetValue(record.BarkodNo, out var items))
                    {
                        var sortedItems = items.OrderBy(x => x.MuhabereNo).ToList();
                        var master = sortedItems.First();
                        var muhabereList = sortedItems
                            .Select(x => x.MuhabereNo)
                            .Where(m => !string.IsNullOrWhiteSpace(m))
                            .ToList();

                        var mergedMuh = FormatMuhabereRange(muhabereList);

                        // DÜZELTME: CS8629 Warning Fix
                        // Eski kod: decimal toplam = sortedItems.Where(x => x.Miktar.HasValue).Sum(x => x.Miktar.Value);
                        // Yeni kod: Null ise 0 kabul et ve topla
                        decimal toplam = sortedItems.Sum(x => x.Miktar ?? 0);

                        var labelRecord = new PostaRecord
                        {
                            BarkodNo = master.BarkodNo,
                            GittigiYer = master.GittigiYer,
                            Durum = master.Durum,
                            Adres = master.Adres,
                            Tarih = master.Tarih,
                            ListeTipi = master.ListeTipi,
                            Miktar = toplam,
                            MuhabereNo = mergedMuh
                        };

                        result.Add(labelRecord);
                        processedBarcodes.Add(record.BarkodNo);
                    }
                }
                else
                {
                    result.Add(record);
                }
            }
            return result;
        }

        private static string FormatMuhabereRange(List<string> muhabereNos)
        {
            if (muhabereNos == null || muhabereNos.Count == 0) return string.Empty;
            if (muhabereNos.Count == 1) return muhabereNos.First();

            try
            {
                var parsed = muhabereNos.Select(m => {
                    var raw = (m ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(raw)) return new { Full = m, Year = "OTHER", Num = -1 };
                    var parts = raw.Split('/');
                    if (parts.Length < 2 || !int.TryParse(parts[1], out var num))
                        return new { Full = m, Year = "OTHER", Num = -1 };
                    return new { Full = m, Year = parts[0], Num = num };
                }).ToList();

                if (parsed.Any(x => x.Num == -1)) return string.Join(", ", muhabereNos);

                var yearGroups = parsed.GroupBy(p => p.Year);
                var finalParts = new List<string>();

                foreach (var grp in yearGroups)
                {
                    var nums = grp.Select(x => x.Num).OrderBy(x => x).Distinct().ToList();
                    var ranges = new List<string>();
                    for (int i = 0; i < nums.Count; i++)
                    {
                        int start = nums[i];
                        while (i < nums.Count - 1 && nums[i + 1] == nums[i] + 1) i++;
                        int end = nums[i];
                        ranges.Add(start == end ? start.ToString() : $"{start}-{end}");
                    }
                    finalParts.Add($"{grp.Key}/{string.Join(",", ranges)}");
                }
                return string.Join(" ", finalParts);
            }
            catch
            {
                return string.Join(", ", muhabereNos);
            }
        }

        private List<string> GetSelectedIds(string? paramSelected)
        {
            if (string.IsNullOrEmpty(paramSelected)) return new List<string>();
            return paramSelected
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private IQueryable<PostaRecord> ApplySelectionFilter(IQueryable<PostaRecord> query, List<string> selectedTokens)
        {
            if (selectedTokens == null || !selectedTokens.Any()) return query.Where(r => false);

            return query.Where(r =>
                (selectedTokens.Contains(r.MuhabereNo)) ||
                (!string.IsNullOrEmpty(r.BarkodNo) && selectedTokens.Contains(r.BarkodNo))
            );
        }

        private void PrepareViewBagSettings(LabelSettings s)
        {
            ViewBag.PaperSize = s.PaperSize;
            ViewBag.Orientation = s.Orientation;
            ViewBag.Template = s.Template;
            ViewBag.DefaultMarginLeft = s.LeftMarginMm;
            ViewBag.DefaultMarginTop = s.TopMarginMm;
            ViewBag.DefaultMarginRight = s.RightMarginMm;
            ViewBag.DefaultMarginBottom = s.BottomMarginMm;
            ViewBag.DefaultHorizontalGap = s.HorizontalGapMm;
            ViewBag.DefaultVerticalGap = s.VerticalGapMm;
            ViewBag.DefaultColumns = s.Columns;
            ViewBag.DefaultRows = s.Rows;
            ViewBag.DefaultLabelWidth = s.LabelWidthMm;
            ViewBag.DefaultLabelHeight = s.LabelHeightMm;
            ViewBag.DefaultFontSize = s.FontSize;
            ViewBag.ListShowRowNumber = s.ListShowRowNumber;
            ViewBag.ListShowMuhabere = s.ListShowMuhabere;
            ViewBag.ListShowBarcode = s.ListShowBarcode;
            ViewBag.ListShowReceiver = s.ListShowReceiver;
            ViewBag.ListShowAmount = s.ListShowAmount;
            ViewBag.ListShowDate = s.ListShowDate;
            ViewBag.ListShowSignature = s.ListShowSignature;
            ViewBag.ListFontSize = s.ListFontSize;
            ViewBag.ShowBarcodeText = s.ShowBarcodeText;
            ViewBag.BarcodeFontSize = s.BarcodeFontSize ?? 0;
            ViewBag.BarcodeTextScalePercent = s.BarcodeTextScalePercent;
            ViewBag.IncludeBarcode = s.IncludeBarcode;
        }

        private string CheckIfSettingsFitPage(LabelSettings settings)
        {
            try
            {
                var layout = _labelLayoutService.CalculateLayout(settings, totalItemCount: 20);
                if (layout == null || layout.LabelPositions == null || !layout.LabelPositions.Any())
                    return "Etiket yerleşimi hesaplanamadı.";

                // PaperSize Enum olduğu için '?' operatörü kullanılamaz.
                var paperSizeStr = settings.PaperSize.ToString();

                var pageWidthMm = layout.PageWidthMM > 0 ? layout.PageWidthMM :
                    (paperSizeStr == "A5" ? 148 : 210);
                var pageHeightMm = layout.PageHeightMM > 0 ? layout.PageHeightMM :
                    (paperSizeStr == "A5" ? 210 : 297);

                foreach (var label in layout.LabelPositions)
                {
                    double labelRight = label.X + label.Width;
                    double labelBottom = label.Y + label.Height;

                    if (labelRight > pageWidthMm + 0.5)
                    {
                        return $"Etiketler yatayda sayfa sınırını aşıyor. ({labelRight:0.0} > {pageWidthMm:0.0} mm)";
                    }

                    if (labelBottom > pageHeightMm + 0.5)
                    {
                        return $"Etiketler dikeyde sayfa sınırını aşıyor. ({labelBottom:0.0} > {pageHeightMm:0.0} mm)";
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckIfSettingsFitPage hatası");
                return "Sayfa sığma kontrolünde hata.";
            }
        }
        #endregion
    }
}