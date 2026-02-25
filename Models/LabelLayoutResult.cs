using System.Collections.Generic;

namespace TasraPostaManager.Models
{
    public class LabelLayoutResult
    {
        // 🔹 PDF tarafında aktif olarak kullanılan temel grid bilgileri
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int TotalPages { get; set; }

        // Bir sayfadaki toplam etiket
        public int LabelsPerPage => Rows * Columns;

        // 🔹 Eski kodlarla uyum için EK özellikler (zorunlu değil ama derleme için gerekli)

        // Toplam etiket sayısı (Preview / debug için kullanılabilir)
        public int TotalLabels { get; set; }

        // Sayfa boyutları – bazı eski kodlarda log veya önizlemede kullanılıyor olabilir
        public double PageWidthMM { get; set; }
        public double PageHeightMM { get; set; }

        // Kağıt tipi (A4, A5 vs.)
        public PaperSizeType PaperSize { get; set; }

        // Yönlendirme (Dikey / Yatay)
        public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

        public bool IsLandscape => Orientation == PaperOrientation.Landscape;
        public string OrientationDisplay => IsLandscape ? "Yatay" : "Dikey";

        // Eski pozisyon bazlı hesaplar için list (şu an PdfService bunu kullanmıyor ama dursun)
        public List<LabelPosition> LabelPositions { get; set; } = new List<LabelPosition>();

        public LabelLayoutResult()
        {
        }

        public LabelLayoutResult(int totalLabels)
        {
            TotalLabels = totalLabels;
        }
    }
}
