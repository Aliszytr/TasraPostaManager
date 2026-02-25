using TasraPostaManager.Models;

namespace TasraPostaManager.Services;

public interface IBarcodePoolService
{
    /// <summary>
    /// Transaction-safe şekilde havuzdan bir barkod claim eder.
    /// </summary>
    Task<string> ClaimNextAsync(string? usedByRecordKey = null, CancellationToken ct = default);

    Task<BarcodePoolStats?> GetStatsAsync(CancellationToken ct = default);

    Task<long> GetAvailableCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Export için havuzdan barkod satırlarını döndürür.
    /// Varsayılan olarak Id ASC (FIFO) sıralıdır.
    /// </summary>
    Task<List<BarcodePoolExportRow>> GetExportRowsAsync(BarcodePoolExportScope scope, CancellationToken ct = default);
}
