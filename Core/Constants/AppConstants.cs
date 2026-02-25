namespace TasraPostaManager.Core.Constants;

/// <summary>
/// Uygulama genelinde kullanılan sabit değerler.
/// Magic string ve number kullanımını önler.
/// </summary>
public static class AppConstants
{
    // Uygulama bilgileri
    public const string AppName = "TasraPostaManager";
    public const string AppVersion = "2.0.0";

    // Varsayılan değerler
    public const string DefaultPdfOutputPath = @"D:\TasraPostaManagerOutput";
    public const string DefaultGonderen = "Taşra Belediyesi";
    public const string DefaultPaperSize = "A4";
    public const int DefaultLabelsPerPage = 14;
    public const string DefaultFontFamily = "Arial";

    // Barkod sabitleri
    public const long DefaultBarcodeStartNumber = 7876500000000;
    public const int DefaultBarcodeDigitCount = 13;
    public const long DefaultBarcodeQuantity = 1_000_000;
    public const int BarcodePoolLowThreshold = 5000;

    // Dosya ayarları
    public const int MaxRequestBodySizeBytes = 50 * 1024 * 1024; // 50 MB
    public const int MaxRequestHeadersTotalSizeBytes = 64 * 1024; // 64 KB
    public static readonly string[] AllowedExcelExtensions = { ".xlsx", ".xls" };

    // Session
    public const string SessionCookieName = "TasraPosta.Session";
    public const int SessionIdleTimeoutMinutes = 15;

    // PDF ayarları
    public const int MaxLabelsPerPdfPage = 50;
    public const int DefaultLabelWidthMM = 100;
    public const int DefaultLabelHeightMM = 50;
    public const int DefaultMarginMM = 5;

    // AppSettings key'leri (DB Key/Value tablosu için)
    public static class SettingKeys
    {
        public const string PdfOutputPath = "PdfOutputPath";
        public const string DefaultGonderen = "DefaultGonderen";
        public const string BarcodePrefix = "BarcodePrefix";
        public const string BarcodeStartNumber = "BarcodeStartNumber";
        public const string BarcodeDigitCount = "BarcodeDigitCount";
        public const string BarcodeSuffix = "BarcodeSuffix";
        public const string BarcodeQuantity = "BarcodeQuantity";
        public const string BarcodeCurrentIndex = "BarcodeCurrentIndex";
        public const string DefaultBarcodeMode = "DefaultBarcodeMode";
        public const string BarcodePoolLowThreshold = "BarcodePoolLowThreshold";
    }
}
