using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace TasraPostaManager.Models;

public class FormState
{
    public int Id { get; set; }
    public string[] Fields { get; set; } = new[] { "MuhabereNo", "GittigiYer", "Adres" };

    [Range(50, 300)]
    public int PageWidthMm { get; set; } = 100;

    [Range(30, 200)]
    public int PageHeightMm { get; set; } = 50;

    [Range(1, 20)]
    public int MarginMm { get; set; } = 5;

    [Range(8, 20)]
    public int FontSize { get; set; } = 10;

    public bool IncludeBarcode { get; set; } = false;
    public bool IncludeFee { get; set; } = false;
    public string BarcodeSize { get; set; } = "medium";
    public string Preset { get; set; } = "custom";

    [DisplayName("Sabit Dosya Klasörü")]
    public string? FixedFilesBasePath { get; set; }

    // 🔹 Ücretsiz liste dosya adı
    [DisplayName("Ücretsiz Liste Dosya Adı")]
    public string? FixedFreeListFileName { get; set; } = "PostaListesiParaliDegil.xlsx";

    // 🔹 Paralı liste dosya adı
    [DisplayName("Paralı Liste Dosya Adı")]
    public string? FixedPaidListFileName { get; set; } = "PostaListesiParali.xlsx";
}
