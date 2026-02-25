using System;
using System.ComponentModel.DataAnnotations;

namespace TasraPostaManager.Models
{
    // 🔹 DATABASE ENTITY - Veritabanı için basit Key/Value ayar tablosu
    public class AppSetting
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Value { get; set; } = string.Empty;

        public DateTime LastModified { get; set; } = DateTime.Now;

        // İstersen ileride kullanmak için bırakılmış alan
        public string? PdfOutputPath { get; set; }
    }

    // 🔹 ANA AYARLAR MODELİ - Uygulama için
    public class AppSettings
    {
        // 📁 DOSYA AYARLARI
        [Required(ErrorMessage = "PDF çıktı yolu zorunludur")]
        public string PdfOutputPath { get; set; } = @"D:\TasraPostaManagerOutput";

        public string PdfExternalPath { get; set; } = @"";

        [Required(ErrorMessage = "Varsayılan gönderen bilgisi zorunludur")]
        [MaxLength(100, ErrorMessage = "Gönderen bilgisi 100 karakteri geçemez")]
        public string DefaultGonderen { get; set; } = "Taşra Belediyesi";

        // 🏷️ ETİKET BASKI AYARLARI
        [Required(ErrorMessage = "Kağıt boyutu seçilmelidir")]
        public string DefaultPaperSize { get; set; } = "A4";

        [Range(1, 50, ErrorMessage = "Sayfa başına etiket 1-50 arası olmalı")]
        public int LabelsPerPage { get; set; } = 14;

        // 📐 ÖZEL OFSET AYARLARI
        public bool UseCustomOffset { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Üst ofset 0-100 mm arası olmalı")]
        public double CustomOffsetTopMM { get; set; } = 14.0;

        [Range(0, 100, ErrorMessage = "Sol ofset 0-100 mm arası olmalı")]
        public double CustomOffsetLeftMM { get; set; } = 5.0;

        [Range(10, 200, ErrorMessage = "Etiket genişliği 10-200 mm arası olmalı")]
        public double LabelWidthMM { get; set; } = 100.0;

        [Range(10, 200, ErrorMessage = "Etiket yüksekliği 10-200 mm arası olmalı")]
        public double LabelHeightMM { get; set; } = 38.0;

        // 🎨 GELİŞMİŞ LAYOUT AYARLARI
        public bool UseAdvancedLayout { get; set; } = false;

        [Range(1, 6, ErrorMessage = "Sütun sayısı 1-6 arası olmalı")]
        public int? PreferredColumns { get; set; }

        [Range(1, 20, ErrorMessage = "Satır sayısı 1-20 arası olmalı")]
        public int? PreferredRows { get; set; }

        public bool ForceSingleLabelPerPage { get; set; } = false;

        // 🔤 FONT VE RENK AYARLARI
        [Required(ErrorMessage = "Birincil font ailesi seçilmelidir")]
        public string PrimaryFontFamily { get; set; } = "Arial";

        public string SecondaryFontFamily { get; set; } = "Courier New";

        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir renk kodu giriniz")]
        public string SenderTextColor { get; set; } = "#000000";

        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir renk kodu giriniz")]
        public string ReceiverTextColor { get; set; } = "#000000";

        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir renk kodu giriniz")]
        public string AddressTextColor { get; set; } = "#666666";

        // 🏷️ ETİKET TASARIM AYARLARI
        public bool UseEnhancedLabelDesign { get; set; } = true;

        [Required(ErrorMessage = "Gönderen etiket stili seçilmelidir")]
        public string SenderLabelStyle { get; set; } = "BOLD_VIBRANT";

        [Required(ErrorMessage = "Alıcı etiket stili seçilmelidir")]
        public string ReceiverLabelStyle { get; set; } = "BOLD_HIGHLIGHT";

        [Required(ErrorMessage = "Adres etiket stili seçilmelidir")]
        public string AddressLabelStyle { get; set; } = "READABLE_PROFESSIONAL";

        // 🎫 BARKOD AYARLARI - YENİ DİNAMİK SİSTEM
        public string BarcodePrefix { get; set; } = "";

        [Required(ErrorMessage = "Başlangıç numarası zorunludur")]
        [Range(1, long.MaxValue, ErrorMessage = "Geçerli bir başlangıç numarası girin")]
        public long BarcodeStartNumber { get; set; } = 7876500000000;

        [Required(ErrorMessage = "Basamak sayısı zorunludur")]
        [Range(8, 20, ErrorMessage = "Basamak sayısı 8-20 arası olmalı")]
        public int BarcodeDigitCount { get; set; } = 13;

        public string BarcodeSuffix { get; set; } = "";

        [Required(ErrorMessage = "Barkod adedi zorunludur")]
        [Range(1, long.MaxValue, ErrorMessage = "Geçerli bir barkod adedi girin")]
        public long BarcodeQuantity { get; set; } = 1000000;

        public long BarcodeCurrentIndex { get; set; } = 0;

        // 🆕 PTT Barkod Havuzu (Pool) Ayarları
        // Legacy sistemi bozmamak için default her zaman "Legacy".
        // (Değerler AppSetting Key/Value tablosundan okunur/yazılır.)
        [Required]
        public string DefaultBarcodeMode { get; set; } = "Legacy"; // Legacy | Pool

        [Range(0, int.MaxValue, ErrorMessage = "Havuz düşük eşik 0 veya pozitif olmalıdır")]
        public int BarcodePoolLowThreshold { get; set; } = 5000;

        // 🔍 BARKOD GÖRSELLİK AYARLARI
        [Required(ErrorMessage = "Barkod boyutu seçilmelidir")]
        public string DefaultBarcodeSize { get; set; } = "medium";

        public bool ShowBarcodeText { get; set; } = true;

        [Range(10, 100, ErrorMessage = "Barkod yüksekliği 10-100 px arası olmalı")]
        public int BarcodeHeightSmall { get; set; } = 12;

        [Range(10, 100, ErrorMessage = "Barkod yüksekliği 10-100 px arası olmalı")]
        public int BarcodeHeightMedium { get; set; } = 20;

        [Range(10, 100, ErrorMessage = "Barkod yüksekliği 10-100 px arası olmalı")]
        public int BarcodeHeightLarge { get; set; } = 30;

        [Range(10, 100, ErrorMessage = "Barkod yüksekliği 10-100 px arası olmalı")]
        public int BarcodeHeightXLarge { get; set; } = 40;
        // 🧩 Etiket & Sayfa Ayarları v2 - isteğe bağlı, hafif model
        public LabelSettings LabelSettings { get; set; } = new LabelSettings();

    }

    // 🔹 AYAR GRUPLARI İÇİN VIEW MODEL
    public class AppSettingsViewModel
    {
        public AppSettings GeneralSettings { get; set; } = new AppSettings();
        public bool SettingsSaved { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool HasErrors { get; set; }
    }

    // 🔹 ENUM'LAR (Eski kullanım yerlerini bozmasın diye burada tutuyoruz)
    public enum BarcodeSize
    {
        Small,
        Medium,
        Large,
        XLarge
    }

    public enum LabelDesignStyle
    {
        BOLD_VIBRANT,
        BOLD_HIGHLIGHT,
        READABLE_PROFESSIONAL,
        MINIMALIST,
        COMPACT
    }
}
