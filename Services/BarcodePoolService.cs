using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services;

public class BarcodePoolService : IBarcodePoolService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BarcodePoolService> _logger;

    public BarcodePoolService(AppDbContext db, ILogger<BarcodePoolService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> ClaimNextAsync(string? usedByRecordKey = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var recordKey = usedByRecordKey ?? string.Empty;

        // ✅ SQL Server uyumlu: ORDER BY yalnızca CTE içinde
        var sql = @"
;WITH cte AS
(
    SELECT TOP (1) *
    FROM dbo.BarcodePoolItems WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE IsUsed = 0 AND Status = @pAvail
    ORDER BY Id ASC
)
UPDATE cte
SET IsUsed = 1,
    Status = @pStatus,
    UsedAt = @pNow,
    UsedByRecordKey = NULLIF(@pRecordKey, '')
OUTPUT inserted.Barcode;
";

        try
        {
            // ✅ DbConnection yerine direkt SqlConnection/SqlCommand kullan
            var conn = _db.Database.GetDbConnection();
            if (conn is not SqlConnection sqlConn)
            {
                throw new InvalidOperationException(
                    $"Beklenmeyen DB provider: {conn.GetType().Name}. SqlConnection bekleniyordu.");
            }

            await _db.Database.OpenConnectionAsync(ct);

            await using var cmd = new SqlCommand(sql, sqlConn)
            {
                CommandType = CommandType.Text
            };

            // ✅ Parametreleri TİPİYLE ver (log'da @pAvail bigint görünüyor → BigInt kullan)
            cmd.Parameters.Add("@pStatus", SqlDbType.BigInt).Value = Convert.ToInt64((int)BarcodePoolStatus.Used);
            cmd.Parameters.Add("@pAvail", SqlDbType.BigInt).Value = Convert.ToInt64((int)BarcodePoolStatus.Available);
            cmd.Parameters.Add("@pNow", SqlDbType.DateTime).Value = now;

            // NVARCHAR uzunluğu: key uzunluğu değişkense 256 güvenli
            var pRecordKey = cmd.Parameters.Add("@pRecordKey", SqlDbType.NVarChar, 256);
            pRecordKey.Value = recordKey;

            var result = await cmd.ExecuteScalarAsync(ct);
            var barcode = result?.ToString();

            if (string.IsNullOrWhiteSpace(barcode))
            {
                // Bu SQL hatası değil → claim edilecek barkod yok / status uyumsuz
                throw new InvalidOperationException(
                    "Barkod Havuzu'nda claim edilebilecek barkod bulunamadı. " +
                    "Tümü kullanılmış olabilir veya Status/IsUsed koşulu eşleşmiyor olabilir."
                );
            }

            return barcode;
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name
        {
            _logger.LogCritical(ex, "BarcodePoolItems tablosu bulunamadı (SQL 208).");
            throw new InvalidOperationException(
                "Barkod Havuzu tablosu bulunamadı (SQL 208). Doğru veritabanına bağlandığını kontrol et.",
                ex
            );
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Barkod havuzu claim sırasında SQL hatası. SQL Number={Number}", ex.Number);
            throw new InvalidOperationException(
                $"Barkod Havuzu erişiminde SQL hatası oluştu. (SQL {ex.Number})",
                ex
            );
        }
        finally
        {
            try { await _db.Database.CloseConnectionAsync(); } catch { /* ignore */ }
        }
    }

    public async Task<BarcodePoolStats?> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var total = await _db.BarcodePoolItems.LongCountAsync(ct);
            var used = await _db.BarcodePoolItems.LongCountAsync(x => x.IsUsed || x.Status == BarcodePoolStatus.Used, ct);
            var disabled = await _db.BarcodePoolItems.LongCountAsync(x => x.Status == BarcodePoolStatus.Disabled, ct);
            var available = await _db.BarcodePoolItems.LongCountAsync(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available, ct);

            var todayStart = DateTime.UtcNow.Date;
            var tomorrowStart = todayStart.AddDays(1);
            var usedToday = await _db.BarcodePoolItems.LongCountAsync(
                x => x.UsedAt != null && x.UsedAt >= todayStart && x.UsedAt < tomorrowStart, ct);

            return new BarcodePoolStats
            {
                Total = total,
                Used = used,
                Available = available,
                Disabled = disabled,
                UsedToday = usedToday
            };
        }
        catch (SqlException ex)
        {
            _logger.LogInformation(ex, "BarcodePoolItems Stats sorgusunda SQL hatası. SQL Number={Number}", ex.Number);
            return null;
        }
    }

    public async Task<long> GetAvailableCountAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.BarcodePoolItems.LongCountAsync(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available, ct);
        }
        catch (SqlException ex)
        {
            _logger.LogInformation(ex, "GetAvailableCount SQL hatası. SQL Number={Number}", ex.Number);
            return 0;
        }
    }

    public async Task<List<BarcodePoolExportRow>> GetExportRowsAsync(BarcodePoolExportScope scope, CancellationToken ct = default)
    {
        try
        {
            var q = _db.BarcodePoolItems.AsNoTracking();

            q = scope switch
            {
                BarcodePoolExportScope.Remaining => q.Where(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available),
                BarcodePoolExportScope.All => q,
                BarcodePoolExportScope.Snapshot => q,
                _ => q.Where(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available)
            };

            // FIFO mantığını korumak için Id ASC
            var rows = await q.OrderBy(x => x.Id)
                .Select(x => new BarcodePoolExportRow
                {
                    Barcode = x.Barcode,
                    Status = x.Status.ToString(),
                    UsedAt = x.UsedAt,
                    UsedByRecordKey = x.UsedByRecordKey,
                    BatchId = x.BatchId
                })
                .ToListAsync(ct);

            if (scope == BarcodePoolExportScope.Remaining)
            {
                // Remaining export tek kolon istendiği için (CSV/XLSX üretiminde) diğer alanlar boş bırakılabilir.
                // Burada dokunmuyoruz; export service scope'a göre kolonları seçecek.
            }

            return rows;
        }
        catch (SqlException ex)
        {
            _logger.LogInformation(ex, "ExportRows SQL hatası. SQL Number={Number}", ex.Number);
            return new List<BarcodePoolExportRow>();
        }
    }
}
