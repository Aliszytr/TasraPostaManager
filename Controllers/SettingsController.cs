using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TasraPostaManager.Data;
using TasraPostaManager.Models;
using TasraPostaManager.Services;

namespace TasraPostaManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly IAppSettingsService _settingsService;
        private readonly IBarcodePoolService _barcodePoolService;
        private readonly IBarcodePoolImportService _barcodePoolImport;
        private readonly IBarcodePoolExportService _barcodePoolExport;
        private readonly IDatabaseBackupService _backupService;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            IAppSettingsService settingsService,
            IBarcodePoolService barcodePoolService,
            IBarcodePoolImportService barcodePoolImport,
            IBarcodePoolExportService barcodePoolExport,
            IDatabaseBackupService backupService,
            AppDbContext db,
            IWebHostEnvironment env,
            ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _barcodePoolService = barcodePoolService;
            _barcodePoolImport = barcodePoolImport;
            _barcodePoolExport = barcodePoolExport;
            _backupService = backupService;
            _db = db;
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                await _settingsService.EnsureDefaultSettingsAsync();

                var settings = _settingsService.GetAppSettings();
                var barcodeConfig = _settingsService.GetBarcodeConfig();
                var labelSettings = _settingsService.GetLabelSettingsV2();

                var model = new SettingsViewModel
                {
                    PdfOutputPath = settings.PdfOutputPath,
                    PdfExternalPath = settings.PdfExternalPath,
                    DefaultGonderen = settings.DefaultGonderen,
                    DefaultPaperSize = settings.DefaultPaperSize,

                    BarcodePrefix = barcodeConfig.Prefix,
                    BarcodeStartNumber = barcodeConfig.StartNumber,
                    BarcodeDigitCount = barcodeConfig.DigitCount,
                    BarcodeSuffix = barcodeConfig.Suffix,
                    BarcodeQuantity = barcodeConfig.Quantity,
                    BarcodeCurrentIndex = barcodeConfig.CurrentIndex,
                    BarcodeRemainingCount = _settingsService.GetRemainingBarcodeCount(),

                    DefaultBarcodeSize = settings.DefaultBarcodeSize,
                    ShowBarcodeText = settings.ShowBarcodeText,
                    BarcodeHeightSmall = settings.BarcodeHeightSmall,
                    BarcodeHeightMedium = settings.BarcodeHeightMedium,
                    BarcodeHeightLarge = settings.BarcodeHeightLarge,
                    BarcodeHeightXLarge = settings.BarcodeHeightXLarge,

                    DefaultBarcodeMode = settings.DefaultBarcodeMode,
                    BarcodePoolLowThreshold = settings.BarcodePoolLowThreshold,

                    // Mevcut ayarları modele yükle
                    LabelSettingsV2 = labelSettings,

                    IsExternalPathAvailable = Directory.Exists(settings.PdfExternalPath ?? string.Empty),
                    SettingsSaved = TempData["SettingsSaved"] as bool? ?? false,
                    Message = TempData["Message"] as string ?? string.Empty
                };

                try
                {
                    model.BarcodePoolStats = await _barcodePoolService.GetStatsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Barcode pool stats alınamadı (muhtemelen migration uygulanmadı).");
                    model.BarcodePoolStats = null;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayarlar sayfası yüklenirken hata oluştu");
                TempData["Error"] = "Ayarlar yüklenirken bir hata oluştu.";
                return View(new SettingsViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            try
            {
                // 1) Form verisini güvenle al
                var labelSettings = model.LabelSettingsV2 ?? new LabelSettings();

                // ÖNEMLİ: NormalizeForSafety çağrısını buraya taşıdık
                labelSettings.NormalizeForSafety();

                // 2) Validasyon hatalarını TEMİZLE (Esnek Mod - Zorlama Yok)
                ModelState.Remove("LabelSettingsV2");
                foreach (var key in ModelState.Keys.Where(k => k.StartsWith("LabelSettingsV2")))
                {
                    ModelState.Remove(key);
                }

                // 3) AppSettings oluştur
                var settings = new AppSettings
                {
                    PdfOutputPath = model.PdfOutputPath?.Trim() ?? @"D:\TasraPostaManagerOutput",
                    PdfExternalPath = model.PdfExternalPath?.Trim() ?? string.Empty,
                    DefaultGonderen = model.DefaultGonderen?.Trim() ?? "Taşra Belediyesi",
                    DefaultPaperSize = model.DefaultPaperSize?.Trim() ?? "A4",

                    // Renk ve Font Ayarları
                    PrimaryFontFamily = model.PrimaryFontFamily ?? "Arial",
                    SecondaryFontFamily = model.SecondaryFontFamily ?? "Courier New",
                    SenderTextColor = model.SenderTextColor ?? "#000000",
                    ReceiverTextColor = model.ReceiverTextColor ?? "#000000",
                    AddressTextColor = model.AddressTextColor ?? "#666666",
                    UseEnhancedLabelDesign = true,

                    DefaultBarcodeSize = model.DefaultBarcodeSize ?? "medium",
                    ShowBarcodeText = model.ShowBarcodeText,
                    BarcodeHeightSmall = model.BarcodeHeightSmall,
                    BarcodeHeightMedium = model.BarcodeHeightMedium,
                    BarcodeHeightLarge = model.BarcodeHeightLarge,
                    BarcodeHeightXLarge = model.BarcodeHeightXLarge,

                    DefaultBarcodeMode = string.IsNullOrWhiteSpace(model.DefaultBarcodeMode) ? "Legacy" : model.DefaultBarcodeMode,
                    BarcodePoolLowThreshold = model.BarcodePoolLowThreshold
                };

                // 4) Barkod Konfigürasyonu
                var barcodeConfig = new BarcodeConfig
                {
                    Prefix = model.BarcodePrefix?.Trim() ?? string.Empty,
                    StartNumber = model.BarcodeStartNumber,
                    DigitCount = model.BarcodeDigitCount,
                    Suffix = model.BarcodeSuffix?.Trim() ?? string.Empty,
                    Quantity = model.BarcodeQuantity,
                    CurrentIndex = model.BarcodeCurrentIndex
                };

                // 5) Kullanıcının girdiği değerleri korumak için şablonu Custom yap
                labelSettings.Template = LabelTemplateType.Custom;

                // 6) Kaydet - TÜM AYARLAR KAYDEDİLECEK
                await _settingsService.SaveSettingsAsync(settings);
                await _settingsService.SaveBarcodeConfigAsync(barcodeConfig);
                await _settingsService.SaveLabelSettingsV2Async(labelSettings);

                TempData["Success"] = "Tüm ayarlar başarıyla güncellendi.";
                TempData["SettingsSaved"] = true;

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayarlar kaydedilirken hata oluştu");
                TempData["Error"] = $"Hata: {ex.Message}";
                return View(model);
            }
        }

        // --- AJAX ACTIONS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestBarcodeGeneration()
        {
            try
            {
                var settings = _settingsService.GetAppSettings();

                // ✅ Mod'a göre doğru kalan sayısı:
                // - Pool: BarcodePoolItems tablosundaki Available sayısı
                // - Legacy: legacy config quantity-current
                var barcode = await _settingsService.GenerateNextBarcodeAsync();

                long remaining;
                if (string.Equals(settings.DefaultBarcodeMode, "Pool", StringComparison.OrdinalIgnoreCase))
                    remaining = await _barcodePoolService.GetAvailableCountAsync();
                else
                    remaining = _settingsService.GetRemainingBarcodeCount();

                return Json(new { success = true, barcode, remainingCount = remaining, message = "Test başarılı!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetBarcodeSettings()
        {
            var def = new BarcodeConfig
            {
                Prefix = "RR",
                StartNumber = 78765000000,
                DigitCount = 11,
                Quantity = 1000000
            };

            await _settingsService.SaveBarcodeConfigAsync(def);
            TempData["Success"] = "Barkod ayarları sıfırlandı.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Barkod Havuzu (Pool) Import (Settings ekranı)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportBarcodePool(IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Lütfen bir .xlsx dosyası seçin.";
                    return RedirectToAction(nameof(Index));
                }

                if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Sadece .xlsx dosyası kabul edilir.";
                    return RedirectToAction(nameof(Index));
                }

                const long maxBytes = 20L * 1024 * 1024; // 20MB güvenli limit
                if (file.Length > maxBytes)
                {
                    TempData["Error"] = "Dosya çok büyük. Maksimum 20MB.";
                    return RedirectToAction(nameof(Index));
                }

                // ClosedXML seekable stream ister; MemoryStream'e alıyoruz.
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                var result = await _barcodePoolImport.ImportFromXlsxAsync(ms, file.FileName);

                TempData["Success"] = $"Pool import tamamlandı. Eklenen: {result.Added}, Zaten vardı: {result.AlreadyExists}, Geçersiz: {result.Invalid}. BatchId: {result.BatchId}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BarcodePool import sırasında hata oluştu");
                TempData["Error"] = $"Pool import hatası: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ Barkod Havuzu Export (Kalan / Tümü / Snapshot)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportBarcodePool(string scope = "Remaining", string format = "csv", bool pttFormat = false)
        {
            try
            {
                if (!Enum.TryParse<BarcodePoolExportScope>(scope, ignoreCase: true, out var exportScope))
                    exportScope = BarcodePoolExportScope.Remaining;

                format = (format ?? "csv").Trim().ToLowerInvariant();
                if (format != "csv" && format != "xlsx") format = "csv";

                var rows = await _barcodePoolService.GetExportRowsAsync(exportScope);
                if (rows.Count == 0)
                {
                    TempData["Error"] = "Dışa aktarılacak barkod bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // PTT format: Remaining/All için tek kolon barkod + başlıksız.
                // Snapshot her zaman başlıklı export olur.
                var includeHeader = !(pttFormat && exportScope != BarcodePoolExportScope.Snapshot);

                var bytes = format == "xlsx"
                    ? _barcodePoolExport.ExportXlsx(rows, exportScope, includeHeader)
                    : _barcodePoolExport.ExportCsv(rows, exportScope, includeHeader);

                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var safeScope = exportScope.ToString();
                var ext = format == "xlsx" ? "xlsx" : "csv";
                var fileName = $"BarcodePool_{safeScope}_{ts}.{ext}";

                var contentType = format == "xlsx"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv";

                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BarcodePool export sırasında hata oluştu");
                TempData["Error"] = $"Export hatası: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAllSettings()
        {
            await _settingsService.SaveSettingsAsync(new AppSettings());
            await _settingsService.SaveLabelSettingsV2Async(new LabelSettings());
            return RedirectToAction(nameof(ResetBarcodeSettings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OnLabelTemplateChanged([FromBody] LabelTemplateChangeRequest request)
        {
            if (request == null) return BadRequest("Geçersiz istek");

            try
            {
                var label = new LabelSettings { Template = request.NewTemplate };
                if (request.NewTemplate != LabelTemplateType.Custom)
                {
                    label.ApplyTemplateFromDefinition();
                }
                ViewData.TemplateInfo.HtmlFieldPrefix = "LabelSettingsV2";
                return PartialView("_LabelSettingsV2Partial", label);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ================================================================
        //  VERİTABANI YEDEKLEME
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupDatabase()
        {
            var result = await _backupService.BackupAsync();
            if (result.Success)
            {
                TempData["Success"] = $"Veritabanı yedeği başarıyla oluşturuldu: <strong>{result.FileName}</strong> ({FormatSize(result.FileSizeBytes)}) — {result.Duration.TotalSeconds:F1} saniye";
                // JavaScript otomatik indirme tetikleyecek
                TempData["AutoDownloadBackup"] = result.FileName;
            }
            else
            {
                TempData["Error"] = $"Yedekleme hatası: {result.ErrorMessage}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetBackups()
        {
            var backups = await _backupService.GetBackupListAsync();
            return Json(backups.Select(b => new
            {
                b.FileName,
                b.SizeDisplay,
                CreatedAt = b.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                b.SizeBytes
            }));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadBackup(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            var stream = await _backupService.GetBackupStreamAsync(fileName);
            if (stream == null)
                return NotFound();

            return File(stream, "application/octet-stream", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBackup(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["Error"] = "Dosya adı belirtilmedi.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _backupService.DeleteBackupAsync(fileName);
            if (success)
                TempData["Success"] = $"Yedek dosyası silindi: <strong>{fileName}</strong>";
            else
                TempData["Error"] = $"Yedek dosyası silinemedi: {fileName}";

            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        //  BARKOD HAVUZU SIFIRLAMA
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetBarcodePool()
        {
            try
            {
                var count = await _db.BarcodePoolItems.CountAsync();
                _db.BarcodePoolItems.RemoveRange(_db.BarcodePoolItems);
                await _db.SaveChangesAsync();

                _logger.LogWarning("Barkod havuzu sıfırlandı — {Count} barkod silindi (Admin: {User})",
                    count, User.Identity?.Name);

                TempData["Success"] = $"Barkod havuzu sıfırlandı — <strong>{count:N0}</strong> barkod silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barkod havuzu sıfırlama hatası");
                TempData["Error"] = $"Barkod havuzu sıfırlanırken hata oluştu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        //  YARDIMCI
        // ================================================================

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
            _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
        };
    }

    public class LabelTemplateChangeRequest
    {
        public LabelTemplateType NewTemplate { get; set; }
        public LabelTemplateType PreviousTemplate { get; set; }
    }
}