using TasraPostaManager.Models;

namespace TasraPostaManager.Services;

public interface IBarcodePoolImportService
{
    Task<BarcodePoolImportResult> ImportFromXlsxAsync(Stream xlsxStream, string sourceName, CancellationToken ct = default);
}
