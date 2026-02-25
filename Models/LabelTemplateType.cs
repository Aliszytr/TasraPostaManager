using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// Etiket Tabakası Şablonları - Tüm projede tutarlı isimlendirme
    /// </summary>
    public enum LabelTemplateType
    {
        [Display(Name = "A4 - 3x7 - 70x38 mm (Standart)")]
        A4_3x7_70x38 = 0,

        [Display(Name = "A4 - 2x8 - 100x38 mm (Kargo)")]
        A4_2x8_100x38 = 1,

        [Display(Name = "A4 - 3x8 - 70x35 mm (Yoğun)")]
        A4_3x8_70x35 = 2,

        [Display(Name = "A4 - 24 - 70x37 mm")]
        A4_24_70x37 = 3,

        [Display(Name = "A4 - 21 - 63x38 mm")]
        A4_21_63x38 = 4,

        [Display(Name = "A4 - 65 - 38x21 mm (Mini)")]
        A4_65_38x21 = 5,

        [Display(Name = "A4 - 8 - 99x68 mm (Büyük Kargo)")]
        A4_8_99x68 = 6,

        // A4_2x10_90x42 ve diğerlerini de ekleyelim
        [Display(Name = "A4 - 2x10 - 90x42 mm")]
        A4_2x10_90x42 = 7,

        [Display(Name = "A4 - 4x6 - 52x29 mm")]
        A4_4x6_52x29 = 8,

        [Display(Name = "Sürekli Form - 1xN - 100x50 mm")]
        Continuous_1xN_100x50 = 9,

        [Display(Name = "Rulo - 1xN - 80x40 mm")]
        Roll_1xN_80x40 = 10,

        [Display(Name = "Özel Ayarlar")]
        Custom = 99
    }

    /// <summary>
    /// Enum extension metodları
    /// </summary>
    public static class LabelTemplateTypeExtensions
    {
        /// <summary>
        /// Enum değerinin görünen adını döndürür
        /// </summary>
        public static string GetDisplayName(this LabelTemplateType template)
        {
            var field = template.GetType().GetField(template.ToString());
            var attribute = field?
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return attribute?.Name ?? template.ToString();
        }

        /// <summary>
        /// Şablonun hazır bir şablon olup olmadığını kontrol eder
        /// </summary>
        public static bool IsPredefined(this LabelTemplateType template)
        {
            return template != LabelTemplateType.Custom;
        }

        /// <summary>
        /// Şablonun grid boyutunu döndürür (Columns x Rows)
        /// </summary>
        public static (int Columns, int Rows) GetGridSize(this LabelTemplateType template)
        {
            return template switch
            {
                LabelTemplateType.A4_3x7_70x38 => (3, 7),
                LabelTemplateType.A4_2x8_100x38 => (2, 8),
                LabelTemplateType.A4_3x8_70x35 => (3, 8),
                LabelTemplateType.A4_24_70x37 => (4, 6),
                LabelTemplateType.A4_21_63x38 => (3, 7),
                LabelTemplateType.A4_65_38x21 => (5, 13),
                LabelTemplateType.A4_8_99x68 => (2, 4),
                LabelTemplateType.A4_2x10_90x42 => (2, 10),
                LabelTemplateType.A4_4x6_52x29 => (4, 6),
                LabelTemplateType.Continuous_1xN_100x50 => (1, 0), // N değişken
                LabelTemplateType.Roll_1xN_80x40 => (1, 0), // N değişken
                _ => (3, 7)
            };
        }

        /// <summary>
        /// Şablonun etiket boyutunu döndürür (Width x Height)
        /// </summary>
        public static (double Width, double Height) GetLabelSize(this LabelTemplateType template)
        {
            return template switch
            {
                LabelTemplateType.A4_3x7_70x38 => (70, 38),
                LabelTemplateType.A4_2x8_100x38 => (100, 38),
                LabelTemplateType.A4_3x8_70x35 => (70, 35),
                LabelTemplateType.A4_24_70x37 => (70, 37),
                LabelTemplateType.A4_21_63x38 => (63, 38),
                LabelTemplateType.A4_65_38x21 => (38, 21),
                LabelTemplateType.A4_8_99x68 => (99, 68),
                LabelTemplateType.A4_2x10_90x42 => (90, 42),
                LabelTemplateType.A4_4x6_52x29 => (52, 29),
                LabelTemplateType.Continuous_1xN_100x50 => (100, 50),
                LabelTemplateType.Roll_1xN_80x40 => (80, 40),
                _ => (70, 38)
            };
        }

        /// <summary>
        /// Şablonun kenar boşluklarını döndürür (Top, Left)
        /// </summary>
        public static (double Top, double Left) GetMargins(this LabelTemplateType template)
        {
            return template switch
            {
                LabelTemplateType.A4_3x7_70x38 => (14, 5),
                LabelTemplateType.A4_2x8_100x38 => (14, 5),
                LabelTemplateType.A4_3x8_70x35 => (10, 5),
                LabelTemplateType.A4_24_70x37 => (10, 5),
                LabelTemplateType.A4_21_63x38 => (10, 5),
                LabelTemplateType.A4_65_38x21 => (5, 5),
                LabelTemplateType.A4_8_99x68 => (10, 10),
                LabelTemplateType.A4_2x10_90x42 => (10, 5),
                LabelTemplateType.A4_4x6_52x29 => (10, 5),
                LabelTemplateType.Continuous_1xN_100x50 => (5, 5),
                LabelTemplateType.Roll_1xN_80x40 => (5, 5),
                _ => (14, 5)
            };
        }

        /// <summary>
        /// Şablonun boşluk değerlerini döndürür (Horizontal, Vertical)
        /// </summary>
        public static (double Horizontal, double Vertical) GetGaps(this LabelTemplateType template)
        {
            return template switch
            {
                LabelTemplateType.A4_3x7_70x38 => (2, 0),
                LabelTemplateType.A4_2x8_100x38 => (2, 0),
                LabelTemplateType.A4_3x8_70x35 => (2, 0),
                LabelTemplateType.A4_24_70x37 => (2, 1),
                LabelTemplateType.A4_21_63x38 => (2, 1),
                LabelTemplateType.A4_65_38x21 => (1, 1),
                LabelTemplateType.A4_8_99x68 => (2, 2),
                LabelTemplateType.A4_2x10_90x42 => (2, 1),
                LabelTemplateType.A4_4x6_52x29 => (2, 1),
                LabelTemplateType.Continuous_1xN_100x50 => (0, 2),
                LabelTemplateType.Roll_1xN_80x40 => (0, 2),
                _ => (2, 0)
            };
        }

        /// <summary>
        /// Şablonun kağıt boyutunu döndürür
        /// </summary>
        public static PaperSizeType GetPaperSize(this LabelTemplateType template)
        {
            return template switch
            {
                LabelTemplateType.Continuous_1xN_100x50 => PaperSizeType.ContinuousForm,
                LabelTemplateType.Roll_1xN_80x40 => PaperSizeType.Roll,
                _ => PaperSizeType.A4
            };
        }

        /// <summary>
        /// Şablonun yönünü döndürür
        /// </summary>
        public static PaperOrientation GetOrientation(this LabelTemplateType template)
        {
            return PaperOrientation.Portrait;
        }

        /// <summary>
        /// Şablonun toplam etiket sayısını döndürür
        /// </summary>
        public static int GetTotalLabels(this LabelTemplateType template)
        {
            var grid = template.GetGridSize();
            return grid.Columns * grid.Rows;
        }
    }
}