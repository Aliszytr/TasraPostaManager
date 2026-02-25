using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasraPostaManager.Data;
using TasraPostaManager.Models;
using TasraPostaManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TasraPostaManager.Controllers
{
    [Authorize]
    public class RecordsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ExcelImporter _excelImporter;
        private readonly IAppSettingsService _settingsService;
        private readonly IDynamicBarcodeService _barcodeService;
        private readonly ILogger<RecordsController> _logger;
        private readonly IPdfService _pdfService;
        private readonly IExcelExportService _excelExportService;
        private readonly ICsvExportService _csvExportService;
        private readonly IEmailService _emailService;

        public RecordsController(
            AppDbContext db,
            ExcelImporter excelImporter,
            IAppSettingsService settingsService,
            IDynamicBarcodeService barcodeService,
            ILogger<RecordsController> logger,
            IPdfService pdfService,
            IExcelExportService excelExportService,
            ICsvExportService csvExportService,
            IEmailService emailService)
        {
            _db = db;
            _excelImporter = excelImporter;
            _settingsService = settingsService;
            _barcodeService = barcodeService;
            _logger = logger;
            _pdfService = pdfService;
            _excelExportService = excelExportService;
            _csvExportService = csvExportService;
            _emailService = emailService;
        }

        // ===========================
        // YARDIMCI METOT: TEMEL SORGU (FILTRELEME)
        // ===========================
        private IQueryable<PostaRecord> GetBaseQuery(string? q, string? tip, DateTime? from, DateTime? to, string? searchType)
        {
            var dbData = _db.PostaRecords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                switch (searchType)
                {
                    case "barkod":
                        dbData = dbData.Where(x => x.BarkodNo != null && x.BarkodNo.Contains(q));
                        break;

                    case "tarih":
                        if (DateTime.TryParse(q, out var searchDate))
                        {
                            dbData = dbData.Where(x => x.Tarih.HasValue && x.Tarih.Value.Date == searchDate.Date);
                        }
                        break;

                    case "muhabere":
                    default:
                        dbData = dbData.Where(x => x.MuhabereNo.Contains(q));
                        break;
                }
            }

            // Liste tipi filtresi
            if (!string.IsNullOrWhiteSpace(tip) && Enum.TryParse<ListeTipi>(tip, true, out var t))
                dbData = dbData.Where(x => x.ListeTipi == t);

            // Tarih aralığı
            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                dbData = dbData.Where(x => x.Tarih >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1).AddTicks(-1);
                dbData = dbData.Where(x => x.Tarih <= toDate);
            }

            return dbData;
        }

        // ===========================
        // YARDIMCI METOT: GRUPLAMA MANTIĞI
        // ===========================
        private async Task<List<PostaGroupViewModel>> GetGroupedListAsync(List<PostaRecord> rawRecords, string? q)
        {
            // Arama varsa, aynı barkoda sahip eksik kardeşleri de tamamla
            var expandedRecords = new List<PostaRecord>(rawRecords);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var foundBarcodes = rawRecords
                    .Where(x => !string.IsNullOrEmpty(x.BarkodNo))
                    .Select(x => x.BarkodNo!)
                    .Distinct()
                    .ToList();

                if (foundBarcodes.Any())
                {
                    // DB'den bu barkodlara sahip diğer kayıtları çek
                    var siblings = await _db.PostaRecords.AsNoTracking()
                        .Where(x => x.BarkodNo != null && foundBarcodes.Contains(x.BarkodNo))
                        .ToListAsync();

                    var existingIds = rawRecords.Select(x => x.Id).ToHashSet();
                    foreach (var sib in siblings)
                    {
                        if (!existingIds.Contains(sib.Id))
                        {
                            expandedRecords.Add(sib);
                        }
                    }
                }
            }

            // Barkod'a göre grupla
            var groupedList = expandedRecords
                .GroupBy(r => !string.IsNullOrWhiteSpace(r.BarkodNo)
                    ? r.BarkodNo!
                    : $"NOBARCODE_{r.Id}")
                .Select(g =>
                {
                    var main = g.OrderBy(x => x.MuhabereNo).First();
                    var allMuhs = g
                        .Select(x => x.MuhabereNo)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .OrderBy(m => m)
                        .ToList();

                    return new PostaGroupViewModel
                    {
                        GroupId = g.Key,
                        MainRecord = main,
                        SubRecords = g.ToList(),
                        Count = g.Count(),
                        AllMuhabereNos = allMuhs,
                        DisplayMuhabereNo = PostaGroupViewModel.FormatMuhabereRange(allMuhs),
                        TotalAmount = g.Sum(x => x.Miktar ?? 0)
                    };
                })
                .ToList();

            return groupedList;
        }

        // ===========================
        // ANA SAYFA (INDEX)
        // ===========================
        public async Task<IActionResult> Index(
            string? q,
            string? tip,
            DateTime? from,
            DateTime? to,
            string? searchType,
            bool showImportSummary = false,
            int importedCount = 0,
            int skippedCount = 0,
            string? sortField = null,
            string? sortDirection = "desc",
            bool showAllLatest = false)
        {
            try
            {
                var viewModel = new RecordsViewModel();

                if (showImportSummary)
                {
                    ViewBag.ShowImportSummary = true;
                    ViewBag.ImportedCount = importedCount;
                    ViewBag.SkippedCount = skippedCount;
                }

                // Varsayılan açılış mı?
                bool isDefaultScenario =
                    string.IsNullOrWhiteSpace(q) &&
                    string.IsNullOrWhiteSpace(tip) &&
                    !from.HasValue &&
                    !to.HasValue &&
                    string.IsNullOrWhiteSpace(searchType);

                bool isLatestDateMode = false;
                DateTime? latestDate = null;
                int latestDateTotalCount = 0;

                // 1. TEMEL QUERY'Yİ AL
                var dbData = GetBaseQuery(q, tip, from, to, searchType);

                // 2. VARSAYILAN AÇILIŞ SENARYOSU (SON TARIH KURALI)
                if (isDefaultScenario)
                {
                    var hasAnyDate = await dbData.AnyAsync(x => x.Tarih.HasValue);

                    if (hasAnyDate)
                    {
                        var latestDateValue = await dbData
                            .Where(x => x.Tarih.HasValue)
                            .Select(x => x.Tarih!.Value)
                            .MaxAsync();

                        latestDate = latestDateValue.Date;

                        dbData = dbData.Where(x => x.Tarih.HasValue &&
                                                   x.Tarih.Value.Date == latestDate.Value);

                        latestDateTotalCount = await dbData.CountAsync();
                        isLatestDateMode = true;
                    }
                }

                // 3. SIRALAMA (RAW DATA İÇİN)
                bool desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
                sortField ??= "Tarih";

                dbData = sortField switch
                {
                    "MuhabereNo" => desc ? dbData.OrderByDescending(x => x.MuhabereNo) : dbData.OrderBy(x => x.MuhabereNo),
                    "GittigiYer" => desc ? dbData.OrderByDescending(x => x.GittigiYer) : dbData.OrderBy(x => x.GittigiYer),
                    "Durum" => desc ? dbData.OrderByDescending(x => x.Durum) : dbData.OrderBy(x => x.Durum),
                    "Tip" => desc ? dbData.OrderByDescending(x => x.ListeTipi) : dbData.OrderBy(x => x.ListeTipi),
                    "ListeTipi" => desc ? dbData.OrderByDescending(x => x.ListeTipi) : dbData.OrderBy(x => x.ListeTipi),
                    "Miktar" => desc ? dbData.OrderByDescending(x => x.Miktar) : dbData.OrderBy(x => x.Miktar),
                    "BarkodNo" => desc ? dbData.OrderByDescending(x => x.BarkodNo) : dbData.OrderBy(x => x.BarkodNo),
                    "Tarih" => desc ? dbData.OrderByDescending(x => x.Tarih).ThenBy(x => x.MuhabereNo) : dbData.OrderBy(x => x.Tarih).ThenBy(x => x.MuhabereNo),
                    _ => desc ? dbData.OrderByDescending(x => x.Tarih).ThenBy(x => x.MuhabereNo) : dbData.OrderBy(x => x.Tarih).ThenBy(x => x.MuhabereNo)
                };

                // 4. LIMIT UYGULA
                if (isLatestDateMode && !showAllLatest && latestDateTotalCount > 100)
                {
                    dbData = dbData.Take(100);
                    ViewBag.LimitedTo100 = true;
                    ViewBag.CurrentLimit = 100;
                }
                else
                {
                    ViewBag.LimitedTo100 = false;
                    ViewBag.CurrentLimit = (int?)null;
                }

                // 5. DB SORGUSU
                var rawRecords = await dbData.AsNoTracking().ToListAsync();

                // 6. GRUPLAMA
                var groupedList = await GetGroupedListAsync(rawRecords, q);

                // 7. SIRALAMA (GRUPLANMIŞ DATA İÇİN)
                bool descGrouped = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
                groupedList = sortField switch
                {
                    "MuhabereNo" => descGrouped ? groupedList.OrderByDescending(x => x.MainRecord.MuhabereNo).ToList() : groupedList.OrderBy(x => x.MainRecord.MuhabereNo).ToList(),
                    "GittigiYer" => descGrouped ? groupedList.OrderByDescending(x => x.MainRecord.GittigiYer).ToList() : groupedList.OrderBy(x => x.MainRecord.GittigiYer).ToList(),
                    "BarkodNo" => descGrouped ? groupedList.OrderByDescending(x => x.MainRecord.BarkodNo).ToList() : groupedList.OrderBy(x => x.MainRecord.BarkodNo).ToList(),
                    "Miktar" => descGrouped ? groupedList.OrderByDescending(x => x.TotalAmount).ToList() : groupedList.OrderBy(x => x.TotalAmount).ToList(),
                    "Tarih" => descGrouped ? groupedList.OrderByDescending(x => x.MainRecord.Tarih).ThenBy(x => x.MainRecord.MuhabereNo).ToList() : groupedList.OrderBy(x => x.MainRecord.Tarih).ThenBy(x => x.MainRecord.MuhabereNo).ToList(),
                    _ => groupedList
                };

                viewModel.DbRecords = (q != null && q.Length > 0) ? rawRecords : rawRecords;
                viewModel.GroupedRecords = groupedList;

                ViewBag.SearchQuery = q;
                ViewBag.SearchType = searchType;
                ViewBag.SelectedTip = tip;
                ViewBag.FromDate = from;
                ViewBag.ToDate = to;
                ViewBag.CurrentSortField = sortField;
                ViewBag.CurrentSortDirection = desc ? "desc" : "asc";
                ViewBag.IsLatestDateMode = isLatestDateMode;
                ViewBag.LatestDate = latestDate;
                ViewBag.LatestDateTotalCount = latestDateTotalCount;
                ViewBag.IsShowingAllLatest = isLatestDateMode && showAllLatest;
                ViewBag.BarcodeConfig = _barcodeService.GetBarcodeConfig();
                ViewBag.RemainingBarcodes = _barcodeService.GetRemainingBarcodeCount();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Records Index sayfası yüklenirken hata oluştu");
                TempData["Error"] = "Kayıtlar yüklenirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===========================
        // STANDART EXCEL EXPORT
        // ===========================
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            string? q,
            string? tip,
            DateTime? from,
            DateTime? to,
            string? searchType,
            string? emailAddress)
        {
            try
            {
                var query = GetBaseQuery(q, tip, from, to, searchType);

                var data = await query
                    .OrderBy(x => x.Tarih ?? DateTime.MaxValue)
                    .ThenBy(x => x.MuhabereNo)
                    .ToListAsync();

                if (data.Count == 0)
                {
                    TempData["Error"] = "Dışa aktarılacak kayıt bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                var bytes = _excelExportService.ExportPttExcel(data);
                var fileName = $"Posta_Listesi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    try
                    {
                        var subject = $"Posta Listesi (Excel) - {DateTime.Now:dd.MM.yyyy HH:mm}";
                        var body = $@"
                            <p>Merhaba,</p>
                            <p>Sistemden oluşturulan standart posta listesi ektedir.</p>
                            <p>
                                <strong>Tarih:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}<br/>
                                <strong>Kayıt Sayısı:</strong> {data.Count}
                            </p>
                            <p>İyi çalışmalar.</p>
                        ";

                        await _emailService.SendEmailWithAttachmentAsync(emailAddress, subject, body, bytes, fileName);
                        TempData["Success"] = $"Dosya indirildi ve {emailAddress} adresine başarıyla gönderildi.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Mail gönderilirken hata oluştu");
                        TempData["Warning"] = "Dosya indirildi ancak mail gönderilirken bir hata oluştu.";
                    }
                }

                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Standart Excel dışa aktarma sırasında hata oluştu");
                TempData["Error"] = "Excel dışa aktarma sırasında bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===========================
        // PTT UYUMLU EXCEL
        // ===========================
        [HttpGet]
        public async Task<IActionResult> ExportPttExcel(
            string? q,
            string? searchType,
            string? tip,
            DateTime? from,
            DateTime? to,
            string? emailAddress,
            string? exportFormat = "detailed")
        {
            try
            {
                var query = GetBaseQuery(q, tip, from, to, searchType);

                var data = await query
                    .OrderBy(x => x.Tarih ?? DateTime.MaxValue)
                    .ThenBy(x => x.MuhabereNo)
                    .ToListAsync();

                if (data.Count == 0)
                {
                    TempData["Error"] = "Dışa aktarılacak kayıt bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                byte[] bytes;
                string fileNameSuffix;

                if (exportFormat == "grouped")
                {
                    var groupedData = await GetGroupedListAsync(data, q);
                    bytes = _excelExportService.ExportGroupedPttExcel(groupedData);
                    fileNameSuffix = "PTT_Teslim_Listesi";
                }
                else
                {
                    bytes = _excelExportService.ExportPttExcel(data);
                    fileNameSuffix = "PTT_Detay_Listesi";
                }

                var fileName = $"{fileNameSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    try
                    {
                        var subject = $"PTT Listesi ({exportFormat}) - {DateTime.Now:dd.MM.yyyy HH:mm}";
                        var body = $@"
                            <p>Merhaba,</p>
                            <p>Sistemden oluşturulan PTT Excel listesi ektedir.</p>
                            <p>
                                <strong>Tarih:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}<br/>
                                <strong>Tür:</strong> {(exportFormat == "grouped" ? "Gruplandırılmış Teslim Listesi" : "Detaylı Liste")}<br/>
                                <strong>Kayıt Sayısı:</strong> {data.Count}
                            </p>
                            <p>İyi çalışmalar.</p>
                        ";

                        await _emailService.SendEmailWithAttachmentAsync(emailAddress, subject, body, bytes, fileName);
                        TempData["Success"] = $"Dosya indirildi ve {emailAddress} adresine başarıyla gönderildi.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Mail gönderilirken hata oluştu");
                        TempData["Warning"] = "Dosya indirildi ancak mail gönderilirken bir hata oluştu. Lütfen mail ayarlarını kontrol edin.";
                    }
                }

                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PTT Excel dışa aktarma sırasında hata oluştu");
                TempData["Error"] = "PTT Excel dışa aktarma sırasında bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===========================
        // PTT UYUMLU CSV (GÜNCELLENDİ)
        // ===========================
        [HttpGet]
        public async Task<IActionResult> ExportPttCsv(
            string? q,
            string? searchType,
            string? tip,
            DateTime? from,
            DateTime? to,
            string? emailAddress,
            string? exportFormat = "detailed") // YENİ PARAMETRE EKLENDİ
        {
            try
            {
                var query = GetBaseQuery(q, tip, from, to, searchType);

                var data = await query
                    .OrderBy(x => x.Tarih ?? DateTime.MaxValue)
                    .ThenBy(x => x.MuhabereNo)
                    .ToListAsync();

                if (data.Count == 0)
                {
                    TempData["Error"] = "Dışa aktarılacak kayıt bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                byte[] bytes;
                string fileNameSuffix;

                // FORMAT KONTROLÜ
                if (exportFormat == "grouped")
                {
                    // Gruplandırılmış Veri Hazırla
                    var groupedData = await GetGroupedListAsync(data, q);
                    // Yeni CSV servisine gönder
                    bytes = _csvExportService.ExportGroupedPttCsv(groupedData);
                    fileNameSuffix = "PTT_Teslim_Listesi";
                }
                else
                {
                    // Eski (Detaylı) Yöntem
                    bytes = _csvExportService.ExportPttCsv(data);
                    fileNameSuffix = "PTT_Detay_Listesi";
                }

                var fileName = $"{fileNameSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    try
                    {
                        var subject = $"PTT Listesi (CSV) - {DateTime.Now:dd.MM.yyyy HH:mm}";
                        var body = $@"
                            <p>Merhaba,</p>
                            <p>Sistemden oluşturulan PTT CSV listesi ektedir.</p>
                            <p>
                                <strong>Tarih:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}<br/>
                                <strong>Tür:</strong> {(exportFormat == "grouped" ? "Gruplandırılmış Teslim Listesi" : "Detaylı Liste")}<br/>
                                <strong>Kayıt Sayısı:</strong> {data.Count}
                            </p>
                            <p>İyi çalışmalar.</p>
                        ";

                        await _emailService.SendEmailWithAttachmentAsync(emailAddress, subject, body, bytes, fileName);
                        TempData["Success"] = $"Dosya indirildi ve {emailAddress} adresine başarıyla gönderildi.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Mail gönderilirken hata oluştu");
                        TempData["Warning"] = "Dosya indirildi ancak mail gönderilirken bir hata oluştu. Lütfen mail ayarlarını kontrol edin.";
                    }
                }

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PTT CSV dışa aktarma sırasında hata oluştu");
                TempData["Error"] = "PTT CSV dışa aktarma sırasında bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===========================
        // DİĞER CRUD İŞLEMLERİ (Aynen Korundu)
        // ===========================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostaRecord model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _db.PostaRecords.AnyAsync(x => x.MuhabereNo == model.MuhabereNo);
            if (existing)
            {
                ModelState.AddModelError("MuhabereNo", "Bu muhabere numarası zaten kayıtlı.");
                return View(model);
            }

            try
            {
                _db.PostaRecords.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{model.MuhabereNo} numaralı kayıt başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yeni kayıt oluşturulurken hata");
                ModelState.AddModelError("", "Kayıt oluşturulurken bir hata oluştu.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string muhabereNo)
        {
            if (string.IsNullOrWhiteSpace(muhabereNo)) return NotFound();
            var record = await _db.PostaRecords.FirstOrDefaultAsync(x => x.MuhabereNo == muhabereNo);
            if (record == null) return NotFound();
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string muhabereNo, PostaRecord model)
        {
            if (string.IsNullOrWhiteSpace(muhabereNo) || muhabereNo != model.MuhabereNo) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var record = await _db.PostaRecords.FirstOrDefaultAsync(x => x.MuhabereNo == muhabereNo);
            if (record == null) return NotFound();

            record.GittigiYer = model.GittigiYer;
            record.Durum = model.Durum;
            record.ListeTipi = model.ListeTipi;
            record.Miktar = model.Miktar;
            record.BarkodNo = model.BarkodNo;
            record.Tarih = model.Tarih;
            record.Adres = model.Adres;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{muhabereNo} numaralı kayıt başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string muhabereNo)
        {
            return await DeleteSingle(muhabereNo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSingle(string muhabereNo)
        {
            if (string.IsNullOrWhiteSpace(muhabereNo))
            {
                TempData["Error"] = "Silinecek kayıt bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            var record = await _db.PostaRecords.FirstOrDefaultAsync(x => x.MuhabereNo == muhabereNo);
            if (record == null)
            {
                TempData["Error"] = "Silinecek kayıt bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            _db.PostaRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{muhabereNo} numaralı kayıt silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEnvelope(string barkodNo)
        {
            if (string.IsNullOrEmpty(barkodNo))
            {
                TempData["Error"] = "Barkod numarası geçersiz.";
                return RedirectToAction(nameof(Index));
            }
            var records = await _db.PostaRecords.Where(x => x.BarkodNo == barkodNo).ToListAsync();
            if (records.Any())
            {
                int count = records.Count;
                _db.PostaRecords.RemoveRange(records);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{count} adet kayıt (Zarf: {barkodNo}) silindi.";
            }
            else
            {
                TempData["Error"] = "Bu barkoda ait kayıt bulunamadı.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendToLabels(List<string> selected)
        {
            if (selected == null || !selected.Any())
            {
                TempData["Error"] = "Lütfen etiket veya liste oluşturmak için en az bir kayıt seçin.";
                return RedirectToAction(nameof(Index));
            }
            string joinedData = string.Join("|", selected.Distinct());
            return RedirectToAction("Index", "Labels", new { selected = joinedData });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string[] muhabereNos)
        {
            try
            {
                if (muhabereNos == null || muhabereNos.Length == 0)
                {
                    TempData["Error"] = "Silmek için en az bir kayıt seçmelisiniz.";
                    return RedirectToAction(nameof(Index));
                }

                var keyList = muhabereNos.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
                if (keyList.Count == 0)
                {
                    TempData["Error"] = "Silinecek geçerli kayıt bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                var keySet = keyList.ToHashSet();
                var recordsToDelete = await _db.PostaRecords
                    .Where(r => keySet.Contains(r.MuhabereNo) || (r.BarkodNo != null && keySet.Contains(r.BarkodNo)) || keySet.Contains("NOBARCODE_" + r.Id))
                    .ToListAsync();

                if (recordsToDelete.Count == 0)
                {
                    TempData["Warning"] = "Seçilen kayıtlara ait veri bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                _db.PostaRecords.RemoveRange(recordsToDelete);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"{recordsToDelete.Count} kayıt başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu silme (BulkDelete) sırasında hata oluştu.");
                TempData["Error"] = "Seçili kayıtlar silinirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}