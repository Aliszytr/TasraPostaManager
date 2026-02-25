public class FixedImportViewModel
{
    public string? BasePath { get; set; }

    public string FreeFileName { get; set; } = "PostaListesiParaliDegil.xlsx";
    public string PaidFileName { get; set; } = "PostaListesiParali.xlsx";

    public string FreeFullPath =>
        string.IsNullOrWhiteSpace(BasePath) ? "" : Path.Combine(BasePath, FreeFileName);

    public string PaidFullPath =>
        string.IsNullOrWhiteSpace(BasePath) ? "" : Path.Combine(BasePath, PaidFileName);

    public bool FreeFileExists { get; set; }
    public bool PaidFileExists { get; set; }

    public string? ErrorMessage { get; set; }

}
