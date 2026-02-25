using System;
using System.Collections.Generic;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// V3 etiket motoru için kullanılan ölçü birimi.
    /// Şimdilik yalnızca mm kullanıyoruz ama ileride Point/Pixel eklenebilir.
    /// </summary>
    public enum MeasureUnit
    {
        Millimeter = 0,
        Point = 1
    }

    /// <summary>
    /// Sayfa tuvali – V3 motoru hesaplamalarını bu tuval üzerinde yapar.
    /// </summary>
    public class LabelV3Canvas
    {
        public double PageWidthMm { get; set; }
        public double PageHeightMm { get; set; }

        /// <summary>
        /// A4, A5 vb. – mevcut enum ile çakışma yok, sadece kullanıyoruz.
        /// </summary>
        public PaperSizeType PaperSize { get; set; }

        /// <summary>
        /// Dikey / Yatay – mevcut enum ile çakışma yok.
        /// </summary>
        public PaperOrientation Orientation { get; set; }

        public MeasureUnit Unit { get; set; } = MeasureUnit.Millimeter;
    }

    /// <summary>
    /// V3 için offset ayarları – sayfanın tamamına uygulanan kaydırmalar.
    /// Örneğin yazıcının kağıdı biraz kaydırması durumunda telafi için.
    /// </summary>
    public class LabelOffsetSettings
    {
        public double OffsetTopMm { get; set; }
        public double OffsetLeftMm { get; set; }
        public double OffsetRightMm { get; set; }
        public double OffsetBottomMm { get; set; }

        public bool HasAnyOffset =>
            Math.Abs(OffsetTopMm) > 0.0001 ||
            Math.Abs(OffsetLeftMm) > 0.0001 ||
            Math.Abs(OffsetRightMm) > 0.0001 ||
            Math.Abs(OffsetBottomMm) > 0.0001;
    }

    /// <summary>
    /// Etiket kenarlığı ile ilgili görsel ayarlar.
    /// </summary>
    public class LabelBorderSettings
    {
        /// <summary>
        /// Etiket kutularının etrafına çerçeve çizilsin mi?
        /// </summary>
        public bool DrawBorder { get; set; } = true;

        /// <summary>
        /// Çerçeve kalınlığı (mm cinsinden).
        /// </summary>
        public double BorderThicknessMm { get; set; } = 0.2;
    }

    /// <summary>
    /// V3 debug / önizleme amaçlı çizim ayarları.
    /// Grid çizgileri, margin gösterimi vb.
    /// </summary>
    public class LabelDebugSettings
    {
        public bool ShowGridLines { get; set; } = true;
        public bool ShowLabelBounds { get; set; } = true;
        public bool ShowPageMargins { get; set; } = true;
        public bool ShowLabelIndexes { get; set; } = false;
    }

    /// <summary>
    /// V3 grid üzerinde tek bir hücre – satır/sütun + mutlak koordinatlar.
    /// </summary>
    public class LabelV3GridCell
    {
        public int Row { get; set; }
        public int Column { get; set; }

        /// <summary>
        /// Sayfa sol üst köşe (0,0) kabul edilerek mm cinsinden X/Y koordinatları.
        /// </summary>
        public double Xmm { get; set; }
        public double Ymm { get; set; }

        /// <summary>
        /// Etiketin genişlik ve yüksekliği (mm cinsinden).
        /// </summary>
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
    }

    /// <summary>
    /// V3 motoru için özet bir layout talebi (request).
    /// Bu sınıf, UI/Ayarlar ekranından gelecek tüm parametreleri temsil eder.
    /// </summary>
    public class LabelV3LayoutRequest
    {
        // Sayfa / Kağıt bilgileri
        public PaperSizeType PaperSize { get; set; } = PaperSizeType.A4;
        public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

        // Marginler (mm)
        public double MarginTopMm { get; set; }
        public double MarginLeftMm { get; set; }
        public double MarginRightMm { get; set; }
        public double MarginBottomMm { get; set; }

        // Grid yapısı
        public int Rows { get; set; }
        public int Columns { get; set; }

        // Etiket boyutları
        public double LabelWidthMm { get; set; }
        public double LabelHeightMm { get; set; }

        // Etiketler arası boşluklar
        public double HorizontalGapMm { get; set; }
        public double VerticalGapMm { get; set; }

        /// <summary>
        /// Bazı PTT şablonlarında ilk satır/sütunda boşluk bırakmak isteyebiliriz.
        /// Null ise 1 kabul edilebilir.
        /// </summary>
        public int? StartRow { get; set; }
        public int? StartColumn { get; set; }

        /// <summary>
        /// Yazıcı sapmalarını telafi etmek için global offset.
        /// </summary>
        public LabelOffsetSettings Offsets { get; set; } = new LabelOffsetSettings();

        /// <summary>
        /// Çerçeve / kenarlık ayarları.
        /// </summary>
        public LabelBorderSettings Border { get; set; } = new LabelBorderSettings();

        /// <summary>
        /// Önizleme / debug çizim ayarları.
        /// </summary>
        public LabelDebugSettings Debug { get; set; } = new LabelDebugSettings();

        public MeasureUnit Unit { get; set; } = MeasureUnit.Millimeter;
    }

    /// <summary>
    /// Tek bir etiketin sayfa üzerindeki mutlak konumu (V3).
    /// PdfService bu koordinatları kullanarak içerikleri çizer.
    /// </summary>
    public class LabelV3Position
    {
        public int PageNumber { get; set; }
        public int IndexOnPage { get; set; }

        public int Row { get; set; }
        public int Column { get; set; }

        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
    }

    /// <summary>
    /// V3 layout hesaplamasının sonucu.
    /// </summary>
    public class LabelV3LayoutResult
    {
        /// <summary>
        /// Sayfa tuvali (A4/A5, orientation, mm ölçüler).
        /// </summary>
        public LabelV3Canvas Canvas { get; set; } = new LabelV3Canvas();

        /// <summary>
        /// Grid hücreleri (satır/sütun bazlı konumlar).
        /// </summary>
        public List<LabelV3GridCell> GridCells { get; set; } = new List<LabelV3GridCell>();

        /// <summary>
        /// Etiketlerin gerçek pozisyonları (page/index/row/col + koordinatlar).
        /// Genelde GridCells ile aynı olabilir, ancak çok sayfalı senaryolarda farklı sayfalar için tekrar kullanılır.
        /// </summary>
        public List<LabelV3Position> LabelPositions { get; set; } = new List<LabelV3Position>();

        public int TotalLabelsPerPage { get; set; }
        public int TotalPages { get; set; }

        /// <summary>
        /// PTT ya da özel şablon adı gibi meta bilgiler.
        /// </summary>
        public string? TemplateName { get; set; }
    }
}
