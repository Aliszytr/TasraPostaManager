using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public class AppSettingsSnapshot
    {
        // 🌐 TEMEL YOL / GÖNDEREN AYARLARI
        public string? DefaultGonderen { get; set; }
        public string? PdfExternalPath { get; set; }
        public string? PdfFallbackPath { get; set; }
        public string? PdfOutputPath { get; set; }

        // 🏷️ ESKİ ETİKET LAYOUT ALANLARI (V1 - GERİYE DÖNÜK UYUMLULUK İÇİN)
        public int LabelWidthMm { get; set; }
        public int LabelHeightMm { get; set; }
        public int LabelMarginMm { get; set; }
        public int LabelFontSize { get; set; }
        public bool LabelIncludeBarcode { get; set; }
        public string? LabelBarcodeSize { get; set; }
        public string? DefaultPaperSize { get; set; }
        public PaperOrientation DefaultOrientation { get; set; }
        public int ListFontSize { get; set; }
        public string? FixedFilesBasePath { get; set; }
        public string? FixedFreeListFileName { get; set; }
        public string? FixedPaidListFileName { get; set; }

        // 📐 LAYOUT VE GRID AYARLARI (V1)
        public bool UseAdvancedLayout { get; set; }
        public bool ForceSingleLabelPerPage { get; set; }
        public int? PreferredColumns { get; set; }
        public int? PreferredRows { get; set; }
        public int? PreferredLabelsPerPage { get; set; }

        // 🎫 BARKOD AYARLARI (LEGACY GÖRÜNÜM, YENİ SİSTEME MAP EDİLİYOR)
        public string BarcodePrefix { get; set; } = "RR";
        // Numeric kısım uzunluğu (BarcodeDigitCount ile eşleşir)
        public int BarcodeLength { get; set; } = 11;
        public bool BarcodeUseChecksum { get; set; } = false;
        public string BarcodeChecksumAlgorithm { get; set; } = "Luhn";
        public string BarcodeFillMode { get; set; } = "RandomDigits";
        public string BarcodePatternTemplate { get; set; } = "PREFIX{SEQUENTIAL}";
        public bool BarcodeAllowOverride { get; set; } = true;

        // 🏷️ ETİKET TASARIM AYARLARI
        public bool UseEnhancedLabelDesign { get; set; } = true;
        public string SenderLabelStyle { get; set; } = "BOLD_VIBRANT";
        public string ReceiverLabelStyle { get; set; } = "BOLD_HIGHLIGHT";
        public string AddressLabelStyle { get; set; } = "READABLE_PROFESSIONAL";

        // 📐 ÖZEL OFSET AYARLARI (V1)
        public bool UseCustomOffset { get; set; } = false;
        public double CustomOffsetTopMM { get; set; } = 14.0;
        public double CustomOffsetLeftMM { get; set; } = 5.0;
        public double LabelWidthMM { get; set; } = 100.0;
        public double LabelHeightMM { get; set; } = 38.0;
        public int LabelsPerPage { get; set; } = 14;

        // 🔤 FONT VE RENK AYARLARI
        public string PrimaryFontFamily { get; set; } = "Arial";
        public string SecondaryFontFamily { get; set; } = "Courier New";
        public string SenderTextColor { get; set; } = "#000000";
        public string ReceiverTextColor { get; set; } = "#000000";
        public string AddressTextColor { get; set; } = "#666666";
        public string SecondaryTextColor { get; set; } = "#444444";

        // 🔍 BARKOD GÖRSELLİK AYARLARI
        public string DefaultBarcodeSize { get; set; } = "medium";
        public int BarcodeHeightSmall { get; set; } = 12;
        public int BarcodeHeightMedium { get; set; } = 20;
        public int BarcodeHeightLarge { get; set; } = 30;
        public int BarcodeHeightXLarge { get; set; } = 40;
        public bool ShowBarcodeText { get; set; } = true;

        // 🏷️ ETİKET & SAYFA AYARLARI v2 (YENİ MERKEZİ BLOK)
        // Bu alanlar AppSettings.LabelSettings ile birebir uyuşacak.
        public PaperSizeType PaperSize { get; set; } = PaperSizeType.A4;
        public PaperOrientation OrientationV2 { get; set; } = PaperOrientation.Portrait;
        public LabelTemplateType LabelTemplate { get; set; } = LabelTemplateType.A4_3x7_70x38;

        // Üst Boşluk (mm)
        public double TopMarginMm { get; set; } = 14.0;

        // Sol Boşluk (mm)
        public double LeftMarginMm { get; set; } = 5.0;

        // ✅ EKSİK ALANLAR EKLENDİ:
        // Sağ Boşluk (mm)
        public double RightMarginMm { get; set; } = 5.0;

        // Alt Boşluk (mm)
        public double BottomMarginMm { get; set; } = 5.0;



        // Etiket Genişliği (mm) - V2
        public double LabelWidthMmV2 { get; set; } = 70.0;

        // Etiket Yüksekliği (mm) - V2
        public double LabelHeightMmV2 { get; set; } = 38.0;

        // Sütun Sayısı (V2)
        public int Columns { get; set; } = 3;

        // Satır Sayısı (V2)
        public int Rows { get; set; } = 7;

        // Yatay Boşluk (mm) – etiketler arası (V2)
        public double HorizontalGapMm { get; set; } = 2.0;

        // Dikey Boşluk (mm) – satırlar arası (V2)
        public double VerticalGapMm { get; set; } = 0.0;

        // 🔤 GENEL FONT / ETİKET AYARLARI (PdfService ile uyum için)
        // Etiketlerde kullanılacak ana font boyutu
        public int FontSize { get; set; } = 10;

        // Etiketlerde barkod gösterilsin mi?
        public bool IncludeBarcode { get; set; } = true;

        // Barkod boyutu (small / medium / large)
        public string? BarcodeSize { get; set; } = "medium";

        // 🧱 ETİKET ÇERÇEVE VE DEBUG IZGARA AYARLARI
        // Etiket kutularının etrafına sınır çizilsin mi?
        public bool DrawBorder { get; set; } = true;

        // Kalibrasyon için debug grid gösterilsin mi?
        public bool ShowDebugGrid { get; set; } = false;



    }
}