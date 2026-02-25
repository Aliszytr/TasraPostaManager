using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasraPostaManager.Services;
using TasraPostaManager.Models;
using TasraPostaManager.Data;

namespace TasraPostaManager.Controllers
{
    [Authorize]
    public class UploadsController : Controller
    {
        private readonly ExcelImporter _importer;
        private readonly AppDbContext _db;

        public UploadsController(ExcelImporter importer, AppDbContext db)
        {
            _importer = importer;
            _db = db;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile excelFile, string listType)
        {
            if (excelFile is null || excelFile.Length == 0)
            {
                TempData["Error"] = "Excel dosyası seçiniz.";
                return RedirectToAction("Index");
            }

            // Dosya uzantısı kontrolü
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Sadece Excel dosyaları (.xlsx, .xls) yükleyebilirsiniz.";
                return RedirectToAction("Index");
            }

            try
            {
                // YENİ: ListeTipi parametresi kaldırıldı, artık Miktar'a göre otomatik belirleniyor
                var result = await _importer.ReadExcelAsync(excelFile);

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
                        TempData["Error"] = "Hiç kayıt bulunamadı. Muhtemelen başlık adları eşleşmedi. " +
                                            "Lütfen Excel başlıklarını veya config/mapping.json dosyasını kontrol edin.";
                    }
                    return RedirectToAction("Index");
                }

                // Başarı mesajı
                var successMessage = $"{result.SuccessfullyImported} kayıt başarıyla eklendi.";
                if (result.SkippedDueToDuplicate > 0)
                {
                    successMessage += $" {result.SkippedDueToDuplicate} kayıt atlandı (zaten mevcut).";
                }
                TempData["Success"] = successMessage;

                // OTOMATİK YÖNLENDİRME - Kayıtlar listesine git
                return RedirectToAction("Index", "Records", new
                {
                    showImportSummary = true,
                    importedCount = result.SuccessfullyImported,
                    skippedCount = result.SkippedDueToDuplicate
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İçe aktarma sırasında hata: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}