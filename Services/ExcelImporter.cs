using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public class ExcelImporter
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExcelImporter> _logger;

        public ExcelImporter(
            AppDbContext db,
            IWebHostEnvironment environment,
            IServiceProvider serviceProvider,
            ILogger<ExcelImporter> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string Norm(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            var normalized = s.Trim().ToUpperInvariant();
            normalized = normalized
                .Replace('Ğ', 'G').Replace('Ü', 'U').Replace('Ş', 'S')
                .Replace('İ', 'I').Replace('I', 'I').Replace('Ö', 'O').Replace('Ç', 'C');

            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Ayar sistemi üzerinden barkod üretir
        /// </summary>
        private async Task<string> GenerateBarcodeWithSettingsAsync(string? usedByRecordKey)
        {
            // ✅ TEK kaynak: IAppSettingsService.AllocateBarcodeAsync
            // Pool modunda sessiz RR fallback YOK. Havuz boşsa / erişilemiyorsa exception fırlar,
            // üst seviye satır işlemci bunu yakalayıp ImportResult.Errors'a yazacaktır.
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

            var barcode = await settingsService.AllocateBarcodeAsync(usedByRecordKey);
            _logger.LogDebug("Barkod tahsis edildi (Settings->Allocate): {Barcode} | RecordKey: {Key}", barcode, usedByRecordKey);
            return barcode;
        }

        public class ImportResult
        {
            public List<PostaRecord> ImportedRecords { get; set; } = new();
            public List<PostaRecord> ExistingRecords { get; set; } = new();
            public List<string> Errors { get; set; } = new();
            public int TotalRecordsFound { get; set; }
            public int SuccessfullyImported { get; set; }
            public int SkippedDueToDuplicate { get; set; }
        }

        public async Task<ImportResult> ReadExcelAsync(IFormFile? file)
        {
            var result = new ImportResult();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("Dosya seçilmedi veya boş dosya.");
                return result;
            }

            if (!IsExcelFile(file.FileName))
            {
                result.Errors.Add("Geçersiz dosya formatı. Sadece Excel dosyaları desteklenir.");
                return result;
            }

            try
            {
                var mappingConfig = await LoadMappingConfigurationAsync();
                if (mappingConfig == null)
                {
                    result.Errors.Add("Mapping konfigürasyonu yüklenemedi.");
                    return result;
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                using var wb = new XLWorkbook(ms);
                var ws = wb.Worksheets.First();

                var headerInfo = FindHeaderRow(ws, mappingConfig);
                if (!headerInfo.Found)
                {
                    result.Errors.Add($"Başlıklar eşleşmedi (MuhabereNo zorunlu). Bulunan normalize başlıklar: {headerInfo.FoundHeaders}");
                    return result;
                }

                await ProcessDataRows(ws, headerInfo, result);

                result.TotalRecordsFound = result.SuccessfullyImported + result.SkippedDueToDuplicate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel işleme hatası (genel)");
                result.Errors.Add($"Excel işleme hatası: {ex.Message}");
            }

            return result;
        }

        private static bool IsExcelFile(string fileName)
        {
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            return allowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
        }

        private async Task<Dictionary<string, HashSet<string>>?> LoadMappingConfigurationAsync()
        {
            try
            {
                var mapPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "config", "mapping.json");
                if (!File.Exists(mapPath))
                {
                    _logger.LogError("Mapping.json dosyası bulunamadı: {Path}", mapPath);
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(mapPath);
                using var doc = JsonDocument.Parse(jsonContent);

                var columns = doc.RootElement.GetProperty("Columns");
                var aliases = new Dictionary<string, HashSet<string>>();

                foreach (var property in columns.EnumerateObject())
                {
                    var set = new HashSet<string>();
                    foreach (var value in property.Value.EnumerateArray())
                    {
                        var alias = value.GetString();
                        if (!string.IsNullOrEmpty(alias))
                            set.Add(Norm(alias));
                    }
                    aliases[property.Name] = set;
                }

                return aliases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mapping konfigürasyonu yüklenirken hata");
                return null;
            }
        }

        /// <summary>
        /// Başlık satırını bul ve KANONİK kolon adları -> kolon index map'i döndür.
        /// </summary>
        private (bool Found, int RowNumber, Dictionary<string, int> ColumnMapping, string FoundHeaders)
            FindHeaderRow(IXLWorksheet worksheet, Dictionary<string, HashSet<string>> aliases)
        {
            var lastRow = worksheet.LastRowUsed().RowNumber();
            var headerRow = -1;
            var bestScore = -1;

            // Geçici olarak: normalize başlık metni -> kolon index
            var bestRawMapping = new Dictionary<string, int>();
            var foundHeaders = string.Empty;

            for (int row = 1; row <= Math.Min(10, lastRow); row++)
            {
                var currentRow = worksheet.Row(row);
                var tempMapping = new Dictionary<string, int>();
                var score = 0;
                var column = 1;

                foreach (var cell in currentRow.Cells(1, currentRow.LastCellUsed()?.Address.ColumnNumber ?? 1))
                {
                    var value = Norm(cell.GetString());
                    if (!string.IsNullOrEmpty(value) && !tempMapping.ContainsKey(value))
                    {
                        tempMapping[value] = column;

                        // Bu normalize değer, herhangi bir kolon alias setinde geçiyorsa skor artır
                        if (aliases.Values.Any(set => set.Contains(value)))
                            score++;
                    }
                    column++;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    headerRow = row;
                    bestRawMapping = tempMapping;
                    foundHeaders = string.Join(", ", tempMapping.Keys.Select(k => $"'{k}'"));
                }
            }

            // Normalize başlık mapping'ini KANONİK isimlere çevir
            var canonicalMapping = new Dictionary<string, int>();

            foreach (var aliasEntry in aliases)
            {
                var canonicalName = aliasEntry.Key;          // Örn: "MuhabereNo"
                var aliasSet = aliasEntry.Value;            // Örn: { "MUHABEREENO", "MUH", ... }

                // Best raw mapping içinde bu alias setinden bir key yakalamaya çalış
                var match = bestRawMapping
                    .FirstOrDefault(x => aliasSet.Contains(x.Key));

                if (!string.IsNullOrEmpty(match.Key))
                {
                    // Artık "MuhabereNo" -> 3 gibi kanonik mapping oluşuyor
                    canonicalMapping[canonicalName] = match.Value;
                }
            }

            // MuhabereNo zorunlu kontrolü - artık kanonik isim üzerinden
            var hasMuhabereNo = canonicalMapping.ContainsKey("MuhabereNo");

            return (hasMuhabereNo, headerRow, canonicalMapping, foundHeaders);
        }

        private async Task ProcessDataRows(IXLWorksheet worksheet,
            (bool Found, int RowNumber, Dictionary<string, int> ColumnMapping, string FoundHeaders) headerInfo,
            ImportResult result)
        {
            var lastRow = worksheet.LastRowUsed().RowNumber();
            var columnMapping = headerInfo.ColumnMapping;

            // Kolon indekslerini bul (kanonik isimlerden)
            var columnIndices = GetColumnIndices(columnMapping);

            var muhabereNoSet = new HashSet<string>();
            var recordsToAdd = new List<PostaRecord>();
            var batchSize = 50; // Performans için batch işlemi
            var rowCount = 0;

            for (int rowNumber = headerInfo.RowNumber + 1; rowNumber <= lastRow; rowNumber++)
            {
                try
                {
                    var rowRecords = await ProcessSingleRowExpanded(worksheet, rowNumber, columnIndices, muhabereNoSet, result);
                    if (rowRecords != null && rowRecords.Count > 0)
                    {
                        recordsToAdd.AddRange(rowRecords);
                        rowCount += rowRecords.Count;

                        // Batch işlemi
                        if (recordsToAdd.Count >= batchSize)
                        {
                            await SaveRecordsBatchAsync(recordsToAdd, result);
                            recordsToAdd.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Satır {RowNumber} işlenirken hata", rowNumber);
                    result.Errors.Add($"Satır {rowNumber} işlenirken hata: {ex.Message}");
                }
            }

            // Kalan kayıtları kaydet
            if (recordsToAdd.Any())
            {
                await SaveRecordsBatchAsync(recordsToAdd, result);
            }
        }

        /// <summary>
        /// Bir Excel satırındaki MuhabereNo alanını genişleterek (ör. 2025/35706-35709)
        /// birden fazla PostaRecord üreten yeni mantık.
        /// Aynı satırdan çıkan tüm kayıtlar AYNI BarkodNo'yu paylaşır.
        /// </summary>
        private async Task<List<PostaRecord>> ProcessSingleRowExpanded(
            IXLWorksheet worksheet,
            int rowNumber,
            ColumnIndices indices,
            HashSet<string> muhabereNoSet,
            ImportResult result)
        {
            var records = new List<PostaRecord>();

            // MuhabereNo kolon index güvenlik kontrolü
            if (indices.MuhabereNo <= 0 || indices.MuhabereNo > 16384)
            {
                result.Errors.Add($"Satır {rowNumber}: MuhabereNo kolon indexi geçersiz (index={indices.MuhabereNo}) - satır atlandı.");
                _logger.LogWarning("Row {RowNumber}: MuhabereNo column index invalid: {Index}", rowNumber, indices.MuhabereNo);


                return records;
            }

            var row = worksheet.Row(rowNumber);

            // MuhabereNo kontrolü
            var muhabereNoCell = row.Cell(indices.MuhabereNo);
            if (muhabereNoCell == null)
                return records;

            var rawMuhabereNo = muhabereNoCell.GetString()?.Trim();
            if (string.IsNullOrEmpty(rawMuhabereNo))
                return records;

            // Hücredeki değeri parçalara ayır (range + liste desteği)
            var expandedMuhList = ExpandMuhabereNos(rawMuhabereNo);
            if (expandedMuhList.Count == 0)
                return records;

            // Satırın diğer alanlarını bir kez doldur, sonra kopyala
            var baseRecord = new PostaRecord();
            await FillRecordFields(baseRecord, row, indices);

            // 🔴 KRİTİK MANTIK:
            // Bu satırdan çıkan TÜM muhabereler aynı zarfa ait.
            // Zarfın ücreti (Miktar) sadece İLK muhabereye yazılacak,
            // diğer kardeş kayıtların Miktar'ı 0 olacak.
            for (int i = 0; i < expandedMuhList.Count; i++)
            {
                var muhabere = expandedMuhList[i];
                var formattedMuhabereNo = muhabere.Trim();
                if (string.IsNullOrEmpty(formattedMuhabereNo))
                    continue;

                // Excel içi duplicate kontrolü
                if (!muhabereNoSet.Add(formattedMuhabereNo))
                {
                    result.Errors.Add($"Satır {rowNumber}: MuhabereNo '{formattedMuhabereNo}' excel içinde tekrar ediyor - atlandı");
                    continue;
                }

                // Veritabanı duplicate kontrolü (Muhabere bazlı)
                var existingRecord = await _db.PostaRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pr => pr.MuhabereNo == formattedMuhabereNo);

                if (existingRecord != null)
                {
                    result.ExistingRecords.Add(existingRecord);
                    result.SkippedDueToDuplicate++;
                    continue;
                }

                // Miktar dağıtımı:
                // i == 0  → bu zarftaki ilk muhabere: gerçek tutar
                // i > 0   → kardeş muhabereler: 0 TL (takip için kayıt)
                decimal? miktarToSet = baseRecord.Miktar;
                if (i > 0)
                    miktarToSet = 0;

                // Yeni kayıt oluştur (baseRecord'dan türeterek)
                var record = new PostaRecord
                {
                    MuhabereNo = formattedMuhabereNo,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    BarkodNo = baseRecord.BarkodNo,
                    Miktar = miktarToSet,
                    ListeTipi = baseRecord.ListeTipi,  // Paralı / Parasız bilgisi zarf bazında korunuyor
                    GittigiYer = baseRecord.GittigiYer,
                    Durum = baseRecord.Durum,
                    Adres = baseRecord.Adres,
                    Tarih = baseRecord.Tarih
                };

                records.Add(record);
            }

            // 🟢 BARKOD TAHSİSİ — Sadece yeni kayıtlar varsa havuzdan barkod çek
            // Bu sayede mükerrer kayıtlar için barkod israf edilmez
            if (records.Count > 0 && records.All(r => string.IsNullOrEmpty(r.BarkodNo)))
            {
                try
                {
                    var barcode = await GenerateBarcodeWithSettingsAsync(records[0].MuhabereNo);
                    foreach (var r in records)
                        r.BarkodNo = barcode;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Barkod tahsisi başarısız (satır {RowNumber})", rowNumber);
                    result.Errors.Add($"Satır {rowNumber}: Barkod tahsis hatası - {ex.Message}");
                    // Barkod atanamayan kayıtları yine de listeye ekle (barkod sonra atanabilir)
                }
            }

            return records;
        }

        /// <summary>
        /// Eski tek-kayıt mantığı (şimdilik kullanılmıyor ama referans kalsın).
        /// </summary>
        private async Task<PostaRecord?> ProcessSingleRow(
            IXLWorksheet worksheet,
            int rowNumber,
            ColumnIndices indices,
            HashSet<string> muhabereNoSet,
            ImportResult result)
        {
            // MuhabereNo kolon index güvenlik kontrolü
            if (indices.MuhabereNo <= 0 || indices.MuhabereNo > 16384)
            {
                result.Errors.Add($"Satır {rowNumber}: MuhabereNo kolon indexi geçersiz (index={indices.MuhabereNo}) - satır atlandı.");
                _logger.LogWarning("Row {RowNumber}: MuhabereNo column index invalid: {Index}", rowNumber, indices.MuhabereNo);
                return null;
            }

            var row = worksheet.Row(rowNumber);

            // MuhabereNo kontrolü
            var muhabereNoCell = row.Cell(indices.MuhabereNo);
            if (muhabereNoCell == null)
                return null;

            var rawMuhabereNo = muhabereNoCell.GetString()?.Trim();
            if (string.IsNullOrEmpty(rawMuhabereNo))
                return null;

            var formattedMuhabereNo = FormatMuhabereNoFromExcel(rawMuhabereNo);

            // Excel içi duplicate kontrolü
            if (!muhabereNoSet.Add(formattedMuhabereNo))
            {
                result.Errors.Add($"Satır {rowNumber}: MuhabereNo '{formattedMuhabereNo}' excel içinde tekrar ediyor - atlandı");
                return null;
            }

            // Veritabanı duplicate kontrolü
            var existingRecord = await _db.PostaRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(pr => pr.MuhabereNo == formattedMuhabereNo);

            if (existingRecord != null)
            {
                result.ExistingRecords.Add(existingRecord);
                result.SkippedDueToDuplicate++;
                return null;
            }

            // Yeni kayıt oluştur
            var record2 = new PostaRecord
            {
                MuhabereNo = formattedMuhabereNo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                BarkodNo = null
            };

            // Diğer alanları doldur
            await FillRecordFields(record2, row, indices);

            return record2;
        }

        private async Task FillRecordFields(PostaRecord record, IXLRow row, ColumnIndices indices)
        {
            // Miktar ve ListeTipi
            if (indices.Miktar > 0 && indices.Miktar <= 16384)
            {
                var miktarCell = row.Cell(indices.Miktar);
                if (miktarCell != null)
                {
                    var miktarStr = miktarCell.GetString()?.Trim().Replace(",", ".");
                    if (decimal.TryParse(miktarStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var miktar))
                    {
                        record.Miktar = miktar;
                        record.ListeTipi = miktar > 0 ? ListeTipi.Parali : ListeTipi.ParaliDegil;
                    }
                }
            }

            // ✅ Diğer alanlar - güvenli kontroller
            record.GittigiYer = GetCellStringValue(row, indices.GittigiYer);
            record.Durum = GetCellStringValue(row, indices.Durum);
            record.Adres = GetCellStringValue(row, indices.Adres);

            // ✅ Barkod — Sadece Excel'den oku. Havuzdan tahsis ProcessSingleRowExpanded'da yapılır.
            var excelBarkod = GetCellStringValue(row, indices.Barkod);
            record.BarkodNo = !string.IsNullOrEmpty(excelBarkod) ? excelBarkod : null;

            // ✅ Tarih - güvenli kontrol
            if (indices.Tarih > 0 && indices.Tarih <= 16384)
            {
                var tarihCell = row.Cell(indices.Tarih);
                if (tarihCell != null)
                {
                    record.Tarih = tarihCell.DataType == XLDataType.DateTime
                        ? tarihCell.GetDateTime()
                        : DateTime.TryParse(tarihCell.GetString(), out var dt) ? dt : null;
                }
            }
        }

        private async Task SaveRecordsBatchAsync(List<PostaRecord> records, ImportResult result)
        {
            if (records == null || records.Count == 0)
                return;

            try
            {
                // Önce tüm batch'i context'e ekle
                await _db.PostaRecords.AddRangeAsync(records);

                while (true)
                {
                    try
                    {
                        await _db.SaveChangesAsync();

                        // Buraya geldiysek, artık sadece başarılı kayıtlar context'te kaldı
                        result.SuccessfullyImported += records.Count;
                        result.ImportedRecords.AddRange(records);

                        _logger.LogInformation("{Count} kayıt başarıyla import edildi", records.Count);
                        break;
                    }
                    catch (DbUpdateException dbEx) when (dbEx.InnerException is SqlException sqlEx &&
                                                         (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                    {
                        // 2601 / 2627: Unique constraint ihlali
                        var duplicateBarcode = TryExtractDuplicateBarcode(sqlEx.Message);

                        if (string.IsNullOrWhiteSpace(duplicateBarcode))
                        {
                            // Barkodu çıkaramazsak, eskisi gibi genel hata ver ve batch'i bırak
                            _logger.LogError(dbEx, "Batch kayıt sırasında benzersiz kısıt hatası (barkod çözülemedi)");
                            result.Errors.Add($"Batch kayıt hatası (benzersiz kısıt): {sqlEx.Message}");

                            // Bu batch'teki tüm entity'leri context'ten ayır
                            foreach (var r in records)
                            {
                                var entry = _db.Entry(r);
                                if (entry != null)
                                    entry.State = EntityState.Detached;
                            }

                            break;
                        }

                        // Bu barkoda sahip batch içi kayıtları bul
                        var dupesInBatch = records
                            .Where(r => string.Equals(r.BarkodNo, duplicateBarcode, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (!dupesInBatch.Any())
                        {
                            // Batch'te yoksa, muhtemelen sadece veritabanıyla çakışıyor
                            _logger.LogWarning(dbEx,
                                "BarkodNo {Barcode} veritabanında zaten mevcut, yeni kayıtlar kaydedilemedi.",
                                duplicateBarcode);
                            result.Errors.Add(
                                $"BarkodNo {duplicateBarcode} zaten veritabanında kayıtlı, bu barkoda ait kayıtlar atlandı.");

                            break;
                        }

                        // Batch içinden bu barkoda ait tüm kayıtları düşür
                        foreach (var dup in dupesInBatch)
                        {
                            var entry = _db.Entry(dup);
                            if (entry != null)
                                entry.State = EntityState.Detached;

                            records.Remove(dup);
                            result.SkippedDueToDuplicate++;
                        }

                        // Mevcut barkoda ait veritabanı kaydını "ExistingRecords" listesine eklemeye çalış
                        try
                        {
                            var existing = await _db.PostaRecords
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.BarkodNo == duplicateBarcode);

                            if (existing != null)
                                result.ExistingRecords.Add(existing);
                        }
                        catch (Exception lookupEx)
                        {
                            _logger.LogWarning(lookupEx,
                                "Duplicate barkod için existing record sorgulanırken hata: {Barcode}", duplicateBarcode);
                        }

                        if (!records.Any())
                        {
                            _logger.LogWarning(
                                "Batch'teki tüm kayıtlar duplicate barkod nedeniyle atıldı, kaydedilecek kayıt kalmadı.");
                            break;
                        }

                        _logger.LogWarning(
                            "BarkodNo {Barcode} duplicate olduğu için batch içinden {Count} kayıt çıkarıldı, SaveChanges yeniden denenecek.",
                            duplicateBarcode,
                            dupesInBatch.Count);
                        // while döngüsü tekrar SaveChangesAsync denenecek
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Batch kayıt işlemi sırasında DbUpdateException hatası");
                        result.Errors.Add($"Batch kayıt hatası: {dbEx.Message}");

                        // Tüm batch'i context'ten ayır, sonraki batch'lere temiz başla
                        foreach (var r in records)
                        {
                            var entry = _db.Entry(r);
                            if (entry != null)
                                entry.State = EntityState.Detached;
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Batch kayıt işlemi sırasında beklenmeyen hata");
                        result.Errors.Add($"Batch kayıt hatası: {ex.Message}");

                        foreach (var r in records)
                        {
                            var entry = _db.Entry(r);
                            if (entry != null)
                                entry.State = EntityState.Detached;
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch kayıt işlemi başlatılırken hata");
                result.Errors.Add($"Batch kayıt hatası (AddRange): {ex.Message}");
            }
        }

        private static string? GetCellStringValue(IXLRow row, int columnIndex)
        {
            if (columnIndex <= 0 || columnIndex > 16384)
                return null;

            try
            {
                var cell = row.Cell(columnIndex);
                return cell?.GetString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private record ColumnIndices(
            int MuhabereNo,
            int GittigiYer,
            int Durum,
            int Miktar,
            int Adres,
            int Tarih,
            int Barkod,
            int IsLabelGenerated
        );

        private ColumnIndices GetColumnIndices(Dictionary<string, int> columnMapping)
        {
            int FindColumn(string columnName)
            {
                return columnMapping.TryGetValue(columnName, out var index) ? index : -1;
            }

            return new ColumnIndices(
                MuhabereNo: FindColumn("MuhabereNo"),
                GittigiYer: FindColumn("GittigiYer"),
                Durum: FindColumn("Durum"),
                Miktar: FindColumn("Miktar"),
                Adres: FindColumn("Adres"),
                Tarih: FindColumn("Tarih"),
                Barkod: FindColumn("Barkod"),
                IsLabelGenerated: FindColumn("IsLabelGenerated")
            );
        }

        /// <summary>
        /// Excel'den gelen MuhabereNo'yu tek tek muhabere string'lerine genişletir.
        /// Örn: 2025/35706-35709,35711  →  [2025/35706, 2025/35707, 2025/35708, 2025/35709, 2025/35711]
        /// </summary>
        private static List<string> ExpandMuhabereNos(string muhabereNo)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(muhabereNo))
                return result;

            var trimmed = muhabereNo.Trim();
            var parts = trimmed.Split('/');
            if (parts.Length != 2)
            {
                // Beklenen format değilse olduğu gibi tek kayıt olarak dön
                result.Add(trimmed);
                return result;
            }

            var yearPart = parts[0].Trim();
            var numbersPart = parts[1].Trim();

            if (string.IsNullOrEmpty(yearPart) || string.IsNullOrEmpty(numbersPart))
            {
                result.Add(trimmed);
                return result;
            }

            var tokens = numbersPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var tokenRaw in tokens)
            {
                var token = tokenRaw.Trim();
                if (string.IsNullOrEmpty(token))
                    continue;

                // Range ise (örn: 35706-35709)
                if (token.Contains('-'))
                {
                    var rangeParts = token.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out var start) &&
                        int.TryParse(rangeParts[1].Trim(), out var end))
                    {
                        if (end < start)
                        {
                            var tmp = start;
                            start = end;
                            end = tmp;
                        }

                        for (int i = start; i <= end; i++)
                        {
                            result.Add($"{yearPart}/{i}");
                        }
                    }
                    else
                    {
                        // Range düzgün parse edilemediyse, token'ı tek numara gibi dene
                        if (int.TryParse(token.Replace("-", "").Trim(), out var single))
                        {
                            result.Add($"{yearPart}/{single}");
                        }
                        else
                        {
                            // Son çare: ham string
                            result.Add(trimmed);
                        }
                    }
                }
                else
                {
                    // Tek numara (örn: 35706)
                    if (int.TryParse(token, out var num))
                    {
                        result.Add($"{yearPart}/{num}");
                    }
                    else
                    {
                        // Tam parse edilmediyse yine de tek muhabere gibi ekle
                        result.Add($"{yearPart}/{token}");
                    }
                }
            }

            // Tekrarlara karşı distinct
            return result.Distinct().ToList();
        }

        private static string FormatMuhabereNoFromExcel(string muhabereNo)
        {
            if (string.IsNullOrWhiteSpace(muhabereNo))
                return string.Empty;

            try
            {
                var parts = muhabereNo.Split('/');
                if (parts.Length != 2)
                    return muhabereNo.Trim();

                var yearPart = parts[0];
                var numbersPart = parts[1];

                List<int> numbers = new List<int>();

                if (!string.IsNullOrWhiteSpace(numbersPart))
                {
                    numbers = numbersPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => int.TryParse(n.Trim(), out int num) ? num : (int?)null)
                        .Where(n => n.HasValue)
                        .Cast<int>() // Nullable<int>'den int'e cast eder
                        .OrderBy(n => n)
                        .ToList();
                }

                if (numbers.Count == 0)
                    return muhabereNo.Trim();

                var ranges = FindConsecutiveRanges(numbers);
                var formattedNumbers = ranges.Select(range =>
                    range.Start == range.End
                        ? range.Start.ToString(CultureInfo.InvariantCulture)
                        : $"{range.Start}-{range.End}"
                );

                return $"{yearPart}/{string.Join(",", formattedNumbers)}";
            }
            catch
            {
                return muhabereNo.Trim();
            }
        }

        private static List<NumberRange> FindConsecutiveRanges(List<int> numbers)
        {
            var ranges = new List<NumberRange>();

            if (numbers.Count == 0)
                return ranges;

            int start = numbers[0];
            int end = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == end + 1)
                {
                    end = numbers[i];
                }
                else
                {
                    ranges.Add(new NumberRange(start, end));
                    start = numbers[i];
                    end = numbers[i];
                }
            }

            ranges.Add(new NumberRange(start, end));
            return ranges;
        }

        private record NumberRange(int Start, int End);

        /// <summary>
        /// SqlException içinden "The duplicate key value is (...)" kısmından barkod değerini çekmeye çalışır.
        /// </summary>
        private static string? TryExtractDuplicateBarcode(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            const string marker = "The duplicate key value is (";
            var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            idx += marker.Length;
            var endIdx = message.IndexOf(')', idx);
            if (endIdx <= idx)
                return null;

            var value = message.Substring(idx, endIdx - idx).Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
