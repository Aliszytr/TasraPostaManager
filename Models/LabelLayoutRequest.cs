using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace TasraPostaManager.Models
{
    public partial class LabelLayoutRequest
    {
        public PaperSizeType PaperSize { get; set; } = PaperSizeType.A4;

        // 🔹 YENİ: Kağıt yönlendirmesi
        public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

        public double PageWidthMM { get; set; }
        public double PageHeightMM { get; set; }
        public double LabelWidthMM { get; set; } = 100;
        public double LabelHeightMM { get; set; } = 50;
        public double PageMarginTopMM { get; set; } = 5;
        public double PageMarginBottomMM { get; set; } = 5;
        public double PageMarginLeftMM { get; set; } = 5;
        public double PageMarginRightMM { get; set; } = 5;
        public double HorizontalSpacingMM { get; set; } = 2;
        public double VerticalSpacingMM { get; set; } = 2;
        public double OffsetXMM { get; set; }
        public double OffsetYMM { get; set; }
        public bool AutoFitGrid { get; set; } = true;

        // 🔹 YENİ: Özel offset özellikleri
        public bool UseCustomOffset { get; set; }
        public double? CustomOffsetTopMM { get; set; }
        public double? CustomOffsetLeftMM { get; set; }

        // 🔹 YENİ: Gelişmiş layout özellikleri
        public bool UseAdvancedLayout { get; set; }

        // Gelişmiş yerleşim ayarları
        public bool ForceSingleLabelPerPage { get; set; }
        public int? PreferredColumns { get; set; }
        public int? PreferredRows { get; set; }
        public int? PreferredLabelsPerPage { get; set; }

        public LabelLayoutRequest()
        {
        }

        // 🔹 YENİ: Yönlendirmeye göre sayfa boyutlarını otomatik ayarla
        public void ApplyOrientation()
        {
            if (PageWidthMM <= 0 || PageHeightMM <= 0)
            {
                (PageWidthMM, PageHeightMM) = GetPaperDimensions(PaperSize);
            }

            if (Orientation == PaperOrientation.Landscape && PageWidthMM < PageHeightMM)
            {
                // Yatay mod: genişlik ve yüksekliği değiştir
                var temp = PageWidthMM;
                PageWidthMM = PageHeightMM;
                PageHeightMM = temp;
            }
        }

        private (double width, double height) GetPaperDimensions(PaperSizeType paperSize)
        {
            return paperSize switch
            {
                PaperSizeType.A4 => (210.0, 297.0),
                PaperSizeType.A5 => (148.0, 210.0),
                PaperSizeType.ContinuousForm => (210.0, 297.0),
                PaperSizeType.Roll => (210.0, double.MaxValue),
                _ => (210.0, 297.0)
            };
        }
    }

    // 🚨 BU KISIM KALDIRILDI - Tüm property'ler ana partial class'a taşındı!
    // public partial class LabelLayoutRequest { ... }
}