using System.ComponentModel.DataAnnotations;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// Etiket & Sayfa Ayarları - (Esnek ve Genişletilmiş Versiyon)
    /// </summary>
    public class LabelSettings
    {
        // --- GÜVENLİK SABİTLERİ (ESNETİLDİ) ---
        private const int MinFontSize = 4;
        private const int MaxFontSize = 144; // Arttırıldı

        private const int MinRows = 1;
        private const int MaxRows = 500; // Arttırıldı

        private const int MinColumns = 1;
        private const int MaxColumns = 100; // Arttırıldı

        private const double MinLabelSizeMm = 1.0;
        private const double MaxLabelSizeMm = 1000.0; // Sınır kaldırıldı gibi

        private const double MinMarginMm = 0.0;
        private const double MaxMarginMm = 500.0; // Kenar boşluğu sınırı arttı

        private const double MinGapMm = 0.0;
        private const double MaxGapMm = 200.0;
        public bool ListShowMuhabere { get; set; } = true; // Varsayılan true olsun

        // Barkod metin ölçeği yüzdesi için güvenli sınırlar
        private const int MinBarcodeTextScalePercent = 10;   // %10
        private const int MaxBarcodeTextScalePercent = 300;  // %300

        // Backing fields
        private int _fontSize = 10;
        private int _columns = 3;
        private int _rows = 7;
        private double _labelWidthMm = 70.0;
        private double _labelHeightMm = 38.0;
        private double _topMarginMm = 14.0;
        private double _leftMarginMm = 5.0;
        private double _rightMarginMm = 5.0;
        private double _bottomMarginMm = 5.0;
        private double _horizontalGapMm = 2.0;
        private double _verticalGapMm = 0.0;

        // 🧾 Kağıt Ayarları
        [Required(ErrorMessage = "Kağıt boyutu seçimi zorunludur")]
        [Display(Name = "Kağıt Boyutu")]
        public PaperSizeType PaperSize { get; set; } = PaperSizeType.A4;

        [Required(ErrorMessage = "Yön seçimi zorunludur")]
        [Display(Name = "Yön")]
        public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

        // 🧩 Etiket Tabakası Şablonu
        [Required(ErrorMessage = "Şablon seçimi zorunludur")]
        [Display(Name = "Şablon")]
        public LabelTemplateType Template { get; set; } = LabelTemplateType.A4_3x7_70x38;

        // 📐 Sayfa Kenar Boşlukları
        [Range(MinMarginMm, MaxMarginMm, ErrorMessage = "Üst boşluk değerleri desteklenmiyor.")]
        [Display(Name = "Üst Boşluk (mm)")]
        public double TopMarginMm { get => Clamp(_topMarginMm, MinMarginMm, MaxMarginMm); set => _topMarginMm = value; }

        [Range(MinMarginMm, MaxMarginMm, ErrorMessage = "Sol boşluk değerleri desteklenmiyor.")]
        [Display(Name = "Sol Boşluk (mm)")]
        public double LeftMarginMm { get => Clamp(_leftMarginMm, MinMarginMm, MaxMarginMm); set => _leftMarginMm = value; }

        [Range(MinMarginMm, MaxMarginMm, ErrorMessage = "Sağ boşluk değerleri desteklenmiyor.")]
        [Display(Name = "Sağ Boşluk (mm)")]
        public double RightMarginMm { get => Clamp(_rightMarginMm, MinMarginMm, MaxMarginMm); set => _rightMarginMm = value; }

        [Range(MinMarginMm, MaxMarginMm, ErrorMessage = "Alt boşluk değerleri desteklenmiyor.")]
        [Display(Name = "Alt Boşluk (mm)")]
        public double BottomMarginMm { get => Clamp(_bottomMarginMm, MinMarginMm, MaxMarginMm); set => _bottomMarginMm = value; }

        // 📏 Etiket Boyutları
        [Range(MinLabelSizeMm, MaxLabelSizeMm, ErrorMessage = "Etiket boyutları desteklenmiyor.")]
        [Display(Name = "Etiket Genişliği (mm)")]
        public double LabelWidthMm { get => Clamp(_labelWidthMm, MinLabelSizeMm, MaxLabelSizeMm); set => _labelWidthMm = value; }

        [Range(MinLabelSizeMm, MaxLabelSizeMm, ErrorMessage = "Etiket boyutları desteklenmiyor.")]
        [Display(Name = "Etiket Yüksekliği (mm)")]
        public double LabelHeightMm { get => Clamp(_labelHeightMm, MinLabelSizeMm, MaxLabelSizeMm); set => _labelHeightMm = value; }

        // 📊 Grid
        [Range(MinColumns, MaxColumns, ErrorMessage = "Sütun sayısı desteklenmiyor.")]
        [Display(Name = "Sütun Sayısı")]
        public int Columns { get => Clamp(_columns, MinColumns, MaxColumns); set => _columns = value; }

        [Range(MinRows, MaxRows, ErrorMessage = "Satır sayısı desteklenmiyor.")]
        [Display(Name = "Satır Sayısı")]
        public int Rows { get => Clamp(_rows, MinRows, MaxRows); set => _rows = value; }

        // 🔁 Etiketler Arası Boşluk
        [Range(MinGapMm, MaxGapMm)]
        [Display(Name = "Yatay Boşluk (mm)")]
        public double HorizontalGapMm { get => Clamp(_horizontalGapMm, MinGapMm, MaxGapMm); set => _horizontalGapMm = value; }

        [Range(MinGapMm, MaxGapMm)]
        [Display(Name = "Dikey Boşluk (mm)")]
        public double VerticalGapMm { get => Clamp(_verticalGapMm, MinGapMm, MaxGapMm); set => _verticalGapMm = value; }

        // ⭐ PDF ve Etiket Ayarları
        [Range(MinFontSize, MaxFontSize)]
        [Display(Name = "Yazı Boyutu")]
        public int FontSize { get => Clamp(_fontSize, MinFontSize, MaxFontSize); set => _fontSize = value; }

        [Display(Name = "Barkod Ekle")]
        public bool IncludeBarcode { get; set; } = true;

        [Display(Name = "Barkod Boyutu")]
        public string BarcodeSize { get; set; } = "medium";

        [Display(Name = "Barkod Metnini Göster")]
        public bool ShowBarcodeText { get; set; } = true;

        // --- Vurgu ve Çerçeve ---
        [Display(Name = "Etiket Çerçevesi Çiz")]
        public bool DrawLabelBorders { get; set; } = false;

        [Display(Name = "Barkod Vurgusu (Kalite)")]
        public int BarcodeEmphasis { get; set; } = 1;

        // 👁‍🗨 ETİKET ÜZERİNDE GÖRÜNTÜLENECEK ALANLAR
        public bool ShowMuhabereNo { get; set; } = true;

        // YENİ ÖZELLİK: Muhabere numarasını kutu içinde vurgulu göster
        [Display(Name = "Muhabere No Kutulu Göster")]
        public bool ShowMuhabereBox { get; set; } = false;

        public bool ShowGittigiYer { get; set; } = true;
        public bool ShowGonderenBilgisi { get; set; } = true;
        public bool ShowMiktar { get; set; } = true;
        public bool ShowBarkodNo { get; set; } = true;
        public bool ShowTarih { get; set; } = false;

        // ---------------------------------------------------------
        // 📋 TESLİM LİSTESİ AYARLARI
        // ---------------------------------------------------------
        [Display(Name = "Liste Yazı Boyutu")]
        public int ListFontSize { get; set; } = 9;

        [Display(Name = "Sıra No Göster")]
        public bool ListShowRowNumber { get; set; } = true;

        [Display(Name = "Barkod Göster")]
        public bool ListShowBarcode { get; set; } = true;

        [Display(Name = "Alıcı Bilgisi Göster")]
        public bool ListShowReceiver { get; set; } = true;

        [Display(Name = "Tutar Göster")]
        public bool ListShowAmount { get; set; } = true;

        [Display(Name = "Tarih Göster")]
        public bool ListShowDate { get; set; } = true;

        [Display(Name = "İmza Alanı Göster")]
        public bool ListShowSignature { get; set; } = true;

        // ---------------------------------------------------------
        // 🆕 BARKOD METİN BOYUTU / ÖLÇEK AYARLARI
        // ---------------------------------------------------------

        [Display(Name = "Barkod Metin Boyutu (pt)")]
        [Range(MinFontSize, MaxFontSize, ErrorMessage = "Barkod metin boyutu desteklenmiyor.")]
        public int? BarcodeFontSize { get; set; } = null; // null ise otomatik hesap (FontSize / ListFontSize baz alınır)

        [Display(Name = "Barkod Metin Ölçeği (%)")]
        [Range(MinBarcodeTextScalePercent, MaxBarcodeTextScalePercent, ErrorMessage = "Barkod metin ölçeği 10–300 arası olmalıdır.")]
        public int BarcodeTextScalePercent { get; set; } = 100; // %100 = normal

        // 🔹 Hesaplanan Özellikler
        [Display(Name = "Toplam Etiket Sayısı")]
        public int TotalLabels => Columns * Rows;

        public double TotalLabelAreaWidthMm => (LabelWidthMm * Columns) + (HorizontalGapMm * (Columns - 1));
        public double TotalLabelAreaHeightMm => (LabelHeightMm * Rows) + (VerticalGapMm * (Rows - 1));

        [Display(Name = "Sayfa Genişliği (mm)")]
        public double PageWidthMm
        {
            get
            {
                if (PaperSize == PaperSizeType.Custom) return CustomPageWidthMm;
                var dims = PaperSize.GetDimensionsInMm();
                return Orientation == PaperOrientation.Landscape ? dims.Height : dims.Width;
            }
        }

        [Display(Name = "Sayfa Yüksekliği (mm)")]
        public double PageHeightMm
        {
            get
            {
                if (PaperSize == PaperSizeType.Custom) return CustomPageHeightMm;
                var dims = PaperSize.GetDimensionsInMm();
                return Orientation == PaperOrientation.Landscape ? dims.Width : dims.Height;
            }
        }

        public double CustomPageWidthMm { get; set; } = 210.0;
        public double CustomPageHeightMm { get; set; } = 297.0;

        // --- Metodlar ---

        public bool IsValid()
        {
            // İSTEĞE ÖZEL: Kullanıcı "ne girersem kaydet, gerekirse çıktı bozuk olsun ama kaydet" dedi.
            // Bu yüzden IsValid artık sadece temel null/sıfır kontrolü yapıp true dönüyor.
            // Sayfaya sığıp sığmama kontrolünü kaldırdık.

            if (LabelWidthMm <= 0 || LabelHeightMm <= 0) return false;
            // Diğer kontroller esnetildi
            return true;
        }

        public void NormalizeForSafety()
        {
            // Kullanıcının girdiği değerleri zorla "mantıklı" sınırlara çekme işlemini de
            // yeni genişletilmiş sınırlara göre yapıyoruz.
            _fontSize = Clamp(_fontSize, MinFontSize, MaxFontSize);
            _columns = Clamp(_columns, MinColumns, MaxColumns);
            _rows = Clamp(_rows, MinRows, MaxRows);

            // Etiket boyutlarını aşırı daraltmayı engelle ama üst sınırı serbest bırak
            _labelWidthMm = Clamp(_labelWidthMm, MinLabelSizeMm, MaxLabelSizeMm);
            _labelHeightMm = Clamp(_labelHeightMm, MinLabelSizeMm, MaxLabelSizeMm);

            _topMarginMm = Clamp(_topMarginMm, MinMarginMm, MaxMarginMm);
            _leftMarginMm = Clamp(_leftMarginMm, MinMarginMm, MaxMarginMm);
            _rightMarginMm = Clamp(_rightMarginMm, MinMarginMm, MaxMarginMm);
            _bottomMarginMm = Clamp(_bottomMarginMm, MinMarginMm, MaxMarginMm);

            _horizontalGapMm = Clamp(_horizontalGapMm, MinGapMm, MaxGapMm);
            _verticalGapMm = Clamp(_verticalGapMm, MinGapMm, MaxGapMm);

            if (BarcodeEmphasis < 1) BarcodeEmphasis = 1;
            if (BarcodeEmphasis > 5) BarcodeEmphasis = 5;

            if (ListFontSize < 6) ListFontSize = 6;
            if (ListFontSize > 36) ListFontSize = 36;

            // Barkod metin ölçeğini güvenli aralığa çek
            if (BarcodeTextScalePercent < MinBarcodeTextScalePercent)
                BarcodeTextScalePercent = MinBarcodeTextScalePercent;
            if (BarcodeTextScalePercent > MaxBarcodeTextScalePercent)
                BarcodeTextScalePercent = MaxBarcodeTextScalePercent;

            if (BarcodeFontSize.HasValue)
            {
                if (BarcodeFontSize.Value < MinFontSize)
                    BarcodeFontSize = MinFontSize;
                if (BarcodeFontSize.Value > MaxFontSize)
                    BarcodeFontSize = MaxFontSize;
            }
        }

        public void ApplyTemplateFromDefinition()
        {
            if (Template == LabelTemplateType.Custom) return;

            var templateDefinition = PredefinedTemplates.GetTemplate(Template);
            if (templateDefinition != null)
            {
                PaperSize = templateDefinition.PaperSize;
                Orientation = templateDefinition.Orientation;
                LabelWidthMm = templateDefinition.LabelWidthMM;
                LabelHeightMm = templateDefinition.LabelHeightMM;
                Columns = templateDefinition.Columns;
                Rows = templateDefinition.Rows;
                TopMarginMm = templateDefinition.PageMarginTopMM;
                LeftMarginMm = templateDefinition.PageMarginLeftMM;
                RightMarginMm = templateDefinition.PageMarginRightMM;
                BottomMarginMm = templateDefinition.PageMarginBottomMM;
                HorizontalGapMm = templateDefinition.HorizontalSpacingMM;
                VerticalGapMm = templateDefinition.VerticalSpacingMM;
            }
            // Normalize çağırırken artık geniş limitleri kullanacak
            NormalizeForSafety();
        }

        public void ApplyTemplateIfNeeded(LabelTemplateType previousTemplate)
        {
            if (Template != LabelTemplateType.Custom && Template != previousTemplate)
                ApplyTemplateFromDefinition();
        }

        public void ApplyTemplateIfNeeded()
        {
            ApplyTemplateIfNeeded(Template);
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
        private static double Clamp(double value, double min, double max) => value < min ? min : (value > max ? max : value);
    }
}