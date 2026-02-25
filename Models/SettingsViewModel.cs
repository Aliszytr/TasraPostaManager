using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TasraPostaManager.Models
{
    public class SettingsViewModel
    {
        [Display(Name = "Etiket & Sayfa Ayarları v2")]
        public LabelSettings LabelSettingsV2 { get; set; } = new LabelSettings();

        // 📁 DOSYA AYARLARI
        private string? _pdfOutputPath;

        [Required(ErrorMessage = "PDF çıktı yolu zorunludur")]
        [Display(Name = "PDF Çıktı Klasörü")]
        public string PdfOutputPath
        {
            get => string.IsNullOrWhiteSpace(_pdfOutputPath) ? @"D:\TasraPostaManagerOutput" : _pdfOutputPath;
            set => _pdfOutputPath = value;
        }

        private string? _pdfExternalPath;

        [Display(Name = "Harici PDF Klasörü")]
        public string? PdfExternalPath
        {
            get => _pdfExternalPath;
            set => _pdfExternalPath = value;
        }

        [Display(Name = "Harici Klasör Durumu")]
        public bool IsExternalPathAvailable { get; set; }

        [Display(Name = "Durum Mesajı")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Ayarlar Kaydedildi mi?")]
        public bool SettingsSaved { get; set; }

        [Display(Name = "Yedek Klasör")]
        public string PdfFallbackPath { get; set; } = string.Empty;

        private string? _defaultGonderen;

        [Required(ErrorMessage = "Varsayılan gönderen bilgisi zorunludur")]
        [MaxLength(400, ErrorMessage = "Gönderen bilgisi 400 karakteri geçemez")]
        [Display(Name = "Varsayılan Gönderen")]
        [DataType(DataType.MultilineText)]
        public string DefaultGonderen
        {
            get => string.IsNullOrWhiteSpace(_defaultGonderen) ? "Taşra Belediyesi" : _defaultGonderen;
            set => _defaultGonderen = value;
        }

        private string? _defaultPaperSize;

        [Required(ErrorMessage = "Varsayılan kağıt boyutu zorunludur")]
        [Display(Name = "Varsayılan Kağıt Boyutu")]
        public string DefaultPaperSize
        {
            get => string.IsNullOrWhiteSpace(_defaultPaperSize) ? "A4" : _defaultPaperSize;
            set => _defaultPaperSize = value;
        }

        // 🔤 FONT VE RENK AYARLARI
        [Display(Name = "Birincil Yazı Tipi")]
        public string PrimaryFontFamily { get; set; } = "Arial";

        [Display(Name = "İkincil Yazı Tipi")]
        public string SecondaryFontFamily { get; set; } = "Courier New";

        [Display(Name = "Gönderen Metin Rengi")]
        public string SenderTextColor { get; set; } = "#000000";

        [Display(Name = "Alıcı Metin Rengi")]
        public string ReceiverTextColor { get; set; } = "#000000";

        [Display(Name = "Adres Metin Rengi")]
        public string AddressTextColor { get; set; } = "#666666";

        // 🎨 GELİŞMİŞ TASARIM AYARLARI
        [Display(Name = "Gelişmiş Etiket Tasarımı Kullan")]
        public bool UseEnhancedLabelDesign { get; set; } = true;

        [Display(Name = "Gönderen Etiket Stili")]
        public string SenderLabelStyle { get; set; } = "BOLD_VIBRANT";

        [Display(Name = "Alıcı Etiket Stili")]
        public string ReceiverLabelStyle { get; set; } = "BOLD_HIGHLIGHT";

        [Display(Name = "Adres Etiket Stili")]
        public string AddressLabelStyle { get; set; } = "READABLE_PROFESSIONAL";

        // 🎫 BARKOD AYARLARI
        [Required(ErrorMessage = "Barkod ön eki zorunludur")]
        [Display(Name = "Barkod Ön Ek")]
        [MaxLength(10, ErrorMessage = "Ön ek en fazla 10 karakter olabilir")]
        public string BarcodePrefix { get; set; } = "RR";

        [Required(ErrorMessage = "Başlangıç numarası zorunludur")]
        [Display(Name = "Başlangıç Numarası")]
        [Range(1, long.MaxValue, ErrorMessage = "Geçerli bir başlangıç numarası girin")]
        public long BarcodeStartNumber { get; set; } = 78765000000;

        [Required(ErrorMessage = "Basamak sayısı zorunludur")]
        [Display(Name = "Basamak Sayısı")]
        [Range(8, 20, ErrorMessage = "Basamak sayısı 8-20 arası olmalı")]
        public int BarcodeDigitCount { get; set; } = 11;

        [Display(Name = "Barkod Son Ek")]
        [MaxLength(10, ErrorMessage = "Son ek en fazla 10 karakter olabilir")]
        public string? BarcodeSuffix { get; set; }

        [Required(ErrorMessage = "Barkod adedi zorunludur")]
        [Display(Name = "Barkod Adedi")]
        [Range(1, 1000000, ErrorMessage = "Adet 1-1.000.000 arası olmalı")]
        public long BarcodeQuantity { get; set; } = 100000;

        [Display(Name = "Mevcut İndeks")]
        public long BarcodeCurrentIndex { get; set; }

        [Display(Name = "Kalan Barkod Sayısı")]
        public long BarcodeRemainingCount { get; set; }

        // 🔍 BARKOD GÖRSEL AYARLARI
        [Display(Name = "Varsayılan Barkod Boyutu")]
        public string DefaultBarcodeSize { get; set; } = "medium";

        [Display(Name = "Barkod Altında Metin Göster")]
        public bool ShowBarcodeText { get; set; } = true;

        [Display(Name = "Küçük Barkod Yüksekliği (px)")]
        [Range(8, 100, ErrorMessage = "Yükseklik 8-100 px arası olmalı")]
        public int BarcodeHeightSmall { get; set; } = 12;

        [Display(Name = "Orta Barkod Yüksekliği (px)")]
        [Range(8, 200, ErrorMessage = "Yükseklik 8-200 px arası olmalı")]
        public int BarcodeHeightMedium { get; set; } = 20;

        [Display(Name = "Büyük Barkod Yüksekliği (px)")]
        [Range(8, 300, ErrorMessage = "Yükseklik 8-300 px arası olmalı")]
        public int BarcodeHeightLarge { get; set; } = 30;

        [Display(Name = "XL Barkod Yüksekliği (px)")]
        [Range(8, 400, ErrorMessage = "Yükseklik 8-400 px arası olmalı")]
        public int BarcodeHeightXLarge { get; set; } = 40;

        // ✅ Barkod Havuzu (Pool) Switch
        [Display(Name = "Varsayılan Barkod Modu")]
        public string DefaultBarcodeMode { get; set; } = "Legacy"; // Legacy | Pool

        [Display(Name = "Pool Düşük Eşik (Uyarı)")]
        [Range(0, 999999999, ErrorMessage = "Geçerli bir eşik değeri girin")]
        public int BarcodePoolLowThreshold { get; set; } = 5000;

        // UI bilgilendirme
        public BarcodePoolStats? BarcodePoolStats { get; set; }
        public BarcodePoolImportResult? LastBarcodePoolImport { get; set; }

        // 🎨 Legacy / Ofset
        [Display(Name = "Sayfa Başına Etiket (V1)")]
        [Range(1, 50, ErrorMessage = "Sayfa başına etiket 1-50 arası olmalı")]
        public int LabelsPerPage { get; set; } = 14;

        public bool UseCustomOffset { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Üst ofset 0-100 mm arası olmalı")]
        public double CustomOffsetTopMM { get; set; } = 14.0;

        [Range(0, 100, ErrorMessage = "Sol ofset 0-100 mm arası olmalı")]
        public double CustomOffsetLeftMM { get; set; } = 5.0;

        [Range(10, 200, ErrorMessage = "Etiket genişliği 10-200 mm arası olmalı")]
        public double LabelWidthMM { get; set; } = 70.0;

        [Range(10, 200, ErrorMessage = "Etiket yüksekliği 10-200 mm arası olmalı")]
        public double LabelHeightMM { get; set; } = 38.0;

        // 🎨 GELİŞMİŞ LAYOUT
        [Display(Name = "Gelişmiş Layout Kullan")]
        public bool UseAdvancedLayout { get; set; } = false;

        [Range(1, 6, ErrorMessage = "Sütun sayısı 1-6 arası olmalı")]
        [Display(Name = "Tercih Edilen Sütun Sayısı")]
        public int? PreferredColumns { get; set; }

        [Range(1, 20, ErrorMessage = "Satır sayısı 1-20 arası olmalı")]
        [Display(Name = "Tercih Edilen Satır Sayısı")]
        public int? PreferredRows { get; set; }

        [Display(Name = "Sayfada Tek Etiket")]
        public bool ForceSingleLabelPerPage { get; set; }

        // =======================================================================
        //  SORUNU ÇÖZEN KISIM: Sözlüğü direkt buraya koyduk, Provider silindi.
        // =======================================================================
        public Dictionary<LabelTemplateType, string> LabelTemplateDisplayNames { get; set; }
            = new Dictionary<LabelTemplateType, string>
            {
                { LabelTemplateType.A4_3x7_70x38, "A4 - 3x7 - 70x38 mm (PTT Standart)" },
                { LabelTemplateType.A4_2x8_100x38, "A4 - 2x8 - 100x38 mm" },
                { LabelTemplateType.A4_3x8_70x35, "A4 - 3x8 - 70x35 mm" },
                { LabelTemplateType.Custom, "Özel Ayarlar (Manuel)" }
            };
    }
}