using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services;

/// <summary>
/// PTT Barkod Havuzu import servisi.
/// 
/// Kritik kural:
/// - Excel dosyasında barkod sütunu ilk sütun olmayabilir.
/// - Mutlaka başlık satırından (header) "barkod" içeren sütunu bularak o sütundan okumalıyız.
/// - Bulamazsak (çok istisnai) 1. kolona fallback yaparız ama log'larız.
/// </summary>
public class BarcodePoolImportService : IBarcodePoolImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BarcodePoolImportService> _logger;

    public BarcodePoolImportService(AppDbContext db, ILogger<BarcodePoolImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BarcodePoolImportResult> ImportFromXlsxAsync(Stream xlsxStream, string sourceName, CancellationToken ct = default)
    {
        var result = new BarcodePoolImportResult
        {
            BatchId = Guid.NewGuid().ToString("N"),
        };

        try
        {
            using var wb = new XLWorkbook(xlsxStream);
            var ws = wb.Worksheets.First();

            var usedRange = ws.RangeUsed();
            if (usedRange == null)
                return result;

            // 1) Header satırını ve barkod kolonunu tespit et
            //    PTT dosyalarında ilk kolon genelde "Sıra No" gibi sayısal bir alan olabiliyor.
            //    Eski sürüm ilk kolondan okuduğu için 100000..300000 gibi yanlış değerleri import ediyordu.
            var (headerRowNumber, barcodeColumnNumber) = FindBarcodeColumn(usedRange);

            if (barcodeColumnNumber <= 0)
            {
                // Fallback: en kötü ihtimal 1. kolon.
                barcodeColumnNumber = 1;
                headerRowNumber = usedRange.FirstRow().RowNumber();
                _logger.LogWarning("Barkod sütunu başlıktan bulunamadı. Fallback olarak 1. kolon kullanılacak. Source: {Source}", sourceName);
            }

            // 2) Barkodları topla
            var barcodes = new List<string>(capacity: 64_000);
            var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in usedRange.Rows())
            {
                ct.ThrowIfCancellationRequested();

                var rowNo = row.RowNumber();
                if (rowNo == headerRowNumber)
                {
                    // header satırı
                    continue;
                }

                result.RowsRead++;

                var cell = row.Cell(barcodeColumnNumber);
                var raw = ReadCellAsString(cell);
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var normalized = NormalizeBarcode(raw);
                if (!IsValidBarcode(normalized))
                {
                    result.Invalid++;
                    if (result.SampleInvalid.Count < 10)
                        result.SampleInvalid.Add(raw);
                    continue;
                }

                if (!seenInFile.Add(normalized))
                    continue;

                barcodes.Add(normalized);
            }

            result.ValidBarcodes = barcodes.Count;
            if (barcodes.Count == 0)
                return result;

            // 3) DB insert: var olanları atla, yenileri ekle
            const int batchSize = 5000;
            for (var i = 0; i < barcodes.Count; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();

                var chunk = barcodes.Skip(i).Take(batchSize).ToList();

                var existing = await _db.BarcodePoolItems
                    .Where(x => chunk.Contains(x.Barcode))
                    .Select(x => x.Barcode)
                    .ToListAsync(ct);

                var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var toAdd = new List<BarcodePoolItem>();
                foreach (var bc in chunk)
                {
                    if (existingSet.Contains(bc))
                    {
                        result.AlreadyExists++;
                        continue;
                    }

                    toAdd.Add(new BarcodePoolItem
                    {
                        Barcode = bc,
                        IsUsed = false,
                        Status = BarcodePoolStatus.Available,
                        ImportedAt = DateTime.UtcNow,
                        BatchId = result.BatchId,
                        Source = sourceName
                    });
                }

                if (toAdd.Count == 0)
                    continue;

                await _db.BarcodePoolItems.AddRangeAsync(toAdd, ct);

                try
                {
                    await _db.SaveChangesAsync(ct);
                    result.Added += toAdd.Count;
                }
                catch (DbUpdateException dbEx)
                {
                    // Unique constraint çakışmaları olabilir (aynı barkod daha önce eklenmiş).
                    _logger.LogWarning(dbEx, "BarcodePool import sırasında unique çakışması. Chunk tek tek değerlendirilecek.");

                    foreach (var item in toAdd)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            _db.BarcodePoolItems.Add(item);
                            await _db.SaveChangesAsync(ct);
                            result.Added++;
                        }
                        catch
                        {
                            result.AlreadyExists++;
                            try { _db.Entry(item).State = EntityState.Detached; } catch { /* ignore */ }
                        }
                    }
                }
            }

            return result;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "BarcodePoolItems tablosu yok gibi görünüyor. Migration uygulanmamış olabilir.");
            throw new InvalidOperationException("Barkod Havuzu tablosu bulunamadı. Önce migration çalıştırın (Add-Migration / Update-Database).", ex);
        }
    }

    /// <summary>
    /// Kullanılan range içinde, ilk 20 satırda "barkod" içeren başlık hücresini bulur.
    /// Dönüş: (headerRowNumber, barcodeColumnNumber)
    /// </summary>
    private static (int headerRowNumber, int barcodeColumnNumber) FindBarcodeColumn(IXLRange usedRange)
    {
        var firstRow = usedRange.FirstRow().RowNumber();
        var lastRow = usedRange.LastRow().RowNumber();
        var maxHeaderScanRows = Math.Min(lastRow, firstRow + 20);

        var ws = usedRange.Worksheet;

        for (var r = firstRow; r <= maxHeaderScanRows; r++)
        {
            var row = ws.Row(r);

            foreach (var cell in row.CellsUsed())
            {
                var text = ReadCellAsString(cell);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var norm = text.Trim().ToLowerInvariant();

                // 🎯 PTT için NET KURAL
                // "nihai barkodlar" veya en azından "barkod" içermeli
                if (norm.Contains("barkod"))
                {
                    return (r, cell.Address.ColumnNumber);
                }
            }
        }

        // bulunamadı
        return (0, 0);
    }

    private static string ReadCellAsString(IXLCell cell)
    {
        try
        {
            if (cell == null)
                return string.Empty;

            // Sayısal hücreler (ör. barkod) Excel'de number olarak gelebilir.
            // Bu durumda format bozulmadan stringe çevirelim.
            if (cell.DataType == XLDataType.Number)
            {
                // Double -> string dönüşümünde bilimsel gösterim riski var.
                // ClosedXML GetFormattedString genelde güvenli.
                var formatted = cell.GetFormattedString();
                return formatted?.Trim() ?? string.Empty;
            }

            // Diğerleri
            return cell.GetString()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeBarcode(string input)
    {
        return input.Trim();
    }

    private static bool IsValidBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        if (barcode.Length < 6 || barcode.Length > 64) return false;

        foreach (var ch in barcode)
        {
            if (char.IsLetterOrDigit(ch)) continue;
            if (ch == '-' || ch == '_') continue;
            return false;
        }

        return true;
    }
}
