using TasraPostaManager.Models;

namespace TasraPostaManager.Services;

public interface IBarcodePoolExportService
{
    /// <summary>
    /// CSV export.
    /// includeHeader=false -> PTT benzeri: tek kolon barkod, başlıksız.
    /// Snapshot export'ta header her zaman yazılır.
    /// </summary>
    byte[] ExportCsv(IEnumerable<BarcodePoolExportRow> rows, BarcodePoolExportScope scope, bool includeHeader = true);

    /// <summary>
    /// XLSX export.
    /// includeHeader=false -> PTT benzeri: tek kolon barkod, başlıksız.
    /// Snapshot export'ta header her zaman yazılır.
    /// </summary>
    byte[] ExportXlsx(IEnumerable<BarcodePoolExportRow> rows, BarcodePoolExportScope scope, bool includeHeader = true);
}
