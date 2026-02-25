using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TasraPostaManager.Data;
using TasraPostaManager.Models;
using TasraPostaManager.Services;
using System.Collections.Generic;

namespace TasraPostaManager.Controllers
{
    public class FixedImportsController : Controller
    {
        private readonly ExcelImporter _importer;
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public FixedImportsController(ExcelImporter importer, AppDbContext db, IConfiguration cfg)
        {
            _importer = importer;
            _db = db;
            _cfg = cfg;
        }

        // 📄 Ekran
        [HttpGet]
        public IActionResult Index()
        {
            var root = _cfg["FixedFiles:Root"];
            var paraliDegil = _cfg["FixedFiles:ParaliDegil"] ?? "PostaListesiParaliDegil.xlsx";
            var parali = _cfg["FixedFiles:Parali"] ?? "PostaListesiParali.xlsx";

            string? basePath = null;
            if (!string.IsNullOrWhiteSpace(root))
            {
                basePath = Path.Combine(AppContext.BaseDirectory, root);
            }

            ViewBag.Root = root;
            ViewBag.BasePath = basePath;
            ViewBag.ParaliDegil = paraliDegil; // YENİ: Ucretsiz → ParaliDegil
            ViewBag.Parali = parali;

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                var freeFull = Path.Combine(basePath, paraliDegil);
                var paidFull = Path.Combine(basePath, parali);

                ViewBag.FreeFullPath = freeFull;
                ViewBag.PaidFullPath = paidFull;

                ViewBag.FreeFileExists = System.IO.File.Exists(freeFull);
                ViewBag.PaidFileExists = System.IO.File.Exists(paidFull);
            }
            else
            {
                ViewBag.FreeFileExists = false;
                ViewBag.PaidFileExists = false;
            }

            return View();
        }

        // 🔹 Tek dosya içe aktarma (Ücretsiz veya Paralı)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(string which)
        {
            var root = _cfg["FixedFiles:Root"];
            if (string.IsNullOrWhiteSpace(root))
            {
                TempData["Error"] = "appsettings içinde 'FixedFiles:Root' ayarı bulunamadı.";
                return RedirectToAction("Index");
            }

            var basePath = root; // Root artık tam klasör yolu

            // YENİ: which parametresi artık "parali" veya "paralidegil" olacak
            var file = which == "parali"
                ? (_cfg["FixedFiles:Parali"] ?? "PostaListesiParali.xlsx")
                : (_cfg["FixedFiles:ParaliDegil"] ?? "PostaListesiParaliDegil.xlsx"); // YENİ: Ucretsiz → ParaliDegil

            var path = Path.Combine(basePath, file);

            if (!System.IO.File.Exists(path))
            {
                TempData["Error"] = $"Dosya bulunamadı: {path}";
                return RedirectToAction("Index");
            }

            try
            {
                await using var fs = System.IO.File.OpenRead(path);
                var ff = new FormFile(fs, 0, fs.Length, "file", Path.GetFileName(path));

                // YENİ: ListeTipi parametresi kaldırıldı, artık Miktar'a göre otomatik belirlenecek
                var result = await _importer.ReadExcelAsync(ff);

                if (result.Errors.Any())
                {
                    TempData["Error"] = string.Join(" | ", result.Errors.Take(3));
                    if (result.Errors.Count > 3)
                    {
                        TempData["Error"] += $" ... ve {result.Errors.Count - 3} hata daha";
                    }
                }

                if (result.SuccessfullyImported == 0 && result.ExistingRecords.Count == 0)
                {
                    if (!result.Errors.Any())
                    {
                        TempData["Error"] = "Excel'den okunan kayıt bulunamadı.";
                    }
                    return RedirectToAction("Index");
                }

                var successMessage = $"{result.SuccessfullyImported} kayıt başarıyla eklendi.";
                if (result.SkippedDueToDuplicate > 0)
                {
                    successMessage += $" {result.SkippedDueToDuplicate} kayıt atlandı (zaten mevcut).";
                }
                TempData["Success"] = successMessage;

                return RedirectToAction("Index", "Records", new
                {
                    showImportSummary = true,
                    importedCount = result.SuccessfullyImported,
                    skippedCount = result.SkippedDueToDuplicate
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Excel okuma hatası: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // 🔹 Hepsini İçe Aktar (Ücretsiz + Paralı)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportAll()
        {
            var root = _cfg["FixedFiles:Root"];
            if (string.IsNullOrWhiteSpace(root))
            {
                TempData["Error"] = "appsettings içinde 'FixedFiles:Root' ayarı bulunamadı.";
                return RedirectToAction("Index");
            }

            var basePath = root; // Root artık tam klasör yolu

            // YENİ: whichList güncellendi
            var whichList = new[] { "paralidegil", "parali" };

            int totalImported = 0;
            int totalSkipped = 0;
            var allErrors = new List<string>();

            foreach (var which in whichList)
            {
                var file = which == "parali"
                    ? (_cfg["FixedFiles:Parali"] ?? "PostaListesiParali.xlsx")
                    : (_cfg["FixedFiles:ParaliDegil"] ?? "PostaListesiParaliDegil.xlsx"); // YENİ: Ucretsiz → ParaliDegil

                var path = Path.Combine(basePath, file);

                if (!System.IO.File.Exists(path))
                {
                    allErrors.Add($"Dosya bulunamadı: {path}");
                    continue;
                }

                try
                {
                    await using var fs = System.IO.File.OpenRead(path);
                    var ff = new FormFile(fs, 0, fs.Length, "file", Path.GetFileName(path));

                    // YENİ: ListeTipi parametresi kaldırıldı
                    var result = await _importer.ReadExcelAsync(ff);

                    if (result.Errors.Any())
                        allErrors.AddRange(result.Errors);

                    totalImported += result.SuccessfullyImported;
                    totalSkipped += result.SkippedDueToDuplicate;
                }
                catch (Exception ex)
                {
                    allErrors.Add($"Excel okuma hatası ({which}): {ex.Message}");
                }
            }

            if (allErrors.Any())
            {
                TempData["Error"] = string.Join(" | ", allErrors.Take(3));
                if (allErrors.Count > 3)
                {
                    TempData["Error"] += $" ... ve {allErrors.Count - 3} hata daha";
                }
            }

            if (totalImported == 0 && totalSkipped == 0)
            {
                if (!allErrors.Any())
                    TempData["Error"] = "Sabit dosyalardan okunan kayıt bulunamadı.";

                return RedirectToAction("Index");
            }

            var success = $"{totalImported} kayıt başarıyla eklendi.";
            if (totalSkipped > 0)
                success += $" {totalSkipped} kayıt atlandı (zaten mevcut).";

            TempData["Success"] = success;

            return RedirectToAction("Index", "Records", new
            {
                showImportSummary = true,
                importedCount = totalImported,
                skippedCount = totalSkipped
            });
        }
    }
}