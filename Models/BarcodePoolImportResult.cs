using System.Collections.Generic;

namespace TasraPostaManager.Models;

public class BarcodePoolImportResult
{
    public string BatchId { get; set; } = string.Empty;
    public int RowsRead { get; set; }
    public int ValidBarcodes { get; set; }
    public int Added { get; set; }
    public int AlreadyExists { get; set; }
    public int Invalid { get; set; }

    public List<string> SampleInvalid { get; set; } = new();
}
