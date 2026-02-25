// PaperSizeType.cs
using System;
using System.Collections.Generic;
using TasraPostaManager.Models;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// Kağıt boyutu tipi
    /// </summary>
    public enum PaperSizeType
    {
        A4,
        A5,
        ContinuousForm,
        Roll,
        Custom
    }

    /// <summary>
    /// PaperSizeType için extension metodlar
    /// </summary>
    public static class PaperSizeTypeExtensions
    {
        /// <summary>
        /// Kağıt boyutuna uygun şablonları döndürür
        /// </summary>
        public static List<LabelTemplateType> GetCompatibleTemplates(this PaperSizeType paperSize)
        {
            return paperSize switch
            {
                PaperSizeType.A4 => new List<LabelTemplateType>
                {
                    LabelTemplateType.A4_3x7_70x38,
                    LabelTemplateType.A4_2x8_100x38,
                    LabelTemplateType.A4_3x8_70x35,
                    LabelTemplateType.A4_24_70x37,
                    LabelTemplateType.A4_21_63x38,
                    LabelTemplateType.A4_65_38x21,
                    LabelTemplateType.A4_8_99x68,
                    LabelTemplateType.A4_2x10_90x42,
                    LabelTemplateType.A4_4x6_52x29,
                    LabelTemplateType.Custom
                },
                PaperSizeType.A5 => new List<LabelTemplateType>
                {
                    LabelTemplateType.A4_3x7_70x38,
                    LabelTemplateType.A4_2x8_100x38,
                    LabelTemplateType.Custom
                },
                PaperSizeType.ContinuousForm => new List<LabelTemplateType>
                {
                    LabelTemplateType.Continuous_1xN_100x50,
                    LabelTemplateType.Custom
                },
                PaperSizeType.Roll => new List<LabelTemplateType>
                {
                    LabelTemplateType.Roll_1xN_80x40,
                    LabelTemplateType.Custom
                },
                PaperSizeType.Custom => new List<LabelTemplateType>
                {
                    LabelTemplateType.Custom
                },
                _ => new List<LabelTemplateType> { LabelTemplateType.Custom }
            };
        }

        /// <summary>
        /// Kağıt boyutunun görünen adını döndürür
        /// </summary>
        public static string GetDisplayName(this PaperSizeType paperSize)
        {
            return paperSize switch
            {
                PaperSizeType.A4 => "A4 (210x297 mm)",
                PaperSizeType.A5 => "A5 (148x210 mm)",
                PaperSizeType.ContinuousForm => "Sürekli Form",
                PaperSizeType.Roll => "Rulo Kağıt",
                PaperSizeType.Custom => "Özel Boyut",
                _ => paperSize.ToString()
            };
        }

        /// <summary>
        /// Kağıt boyutunun milimetre cinsinden ölçülerini döndürür
        /// </summary>
        public static (double Width, double Height) GetDimensionsInMm(this PaperSizeType paperSize)
        {
            return paperSize switch
            {
                PaperSizeType.A4 => (210, 297),
                PaperSizeType.A5 => (148, 210),
                PaperSizeType.ContinuousForm => (210, 0), // 0 = sınırsız uzunluk
                PaperSizeType.Roll => (80, 0), // 0 = sınırsız uzunluk
                _ => (210, 297) // Varsayılan A4
            };
        }

        /// <summary>
        /// Kağıt boyutunun piksel cinsinden ölçülerini döndürür (300 DPI)
        /// </summary>
        public static (int Width, int Height) GetDimensionsInPixels(this PaperSizeType paperSize, int dpi = 300)
        {
            var (widthMm, heightMm) = paperSize.GetDimensionsInMm();
            double mmToInch = 0.0393701;

            int widthPx = (int)(widthMm * mmToInch * dpi);
            int heightPx = heightMm > 0 ? (int)(heightMm * mmToInch * dpi) : 0;

            return (widthPx, heightPx);
        }

        /// <summary>
        /// Kağıt tipinin rulo olup olmadığını kontrol eder
        /// </summary>
        public static bool IsRollType(this PaperSizeType paperSize)
        {
            return paperSize == PaperSizeType.Roll || paperSize == PaperSizeType.ContinuousForm;
        }
    }

    /// <summary>
    /// PaperSizeType için helper sınıfı
    /// </summary>
    public static class PaperSizeTypes
    {
        /// <summary>
        /// Tüm kağıt boyutu seçenekleri
        /// </summary>
        public static readonly Dictionary<PaperSizeType, string> All = new()
        {
            { PaperSizeType.A4, "A4 (210x297 mm)" },
            { PaperSizeType.A5, "A5 (148x210 mm)" },
            { PaperSizeType.ContinuousForm, "Sürekli Form" },
            { PaperSizeType.Roll, "Rulo Kağıt" },
            { PaperSizeType.Custom, "Özel Boyut" }
        };

        /// <summary>
        /// Kağıt boyutu seçeneklerini SelectList için hazırlar
        /// </summary>
        public static List<KeyValuePair<string, string>> GetSelectList()
        {
            return All.Select(x => new KeyValuePair<string, string>(
                x.Key.ToString(),
                x.Value
            )).ToList();
        }

        /// <summary>
        /// Varsayılan kağıt boyutunu döndürür
        /// </summary>
        public static PaperSizeType GetDefault()
        {
            return PaperSizeType.A4;
        }

        /// <summary>
        /// Kağıt boyutuna göre uygun yönelimleri döndürür
        /// </summary>
        public static List<PaperOrientation> GetSupportedOrientations(PaperSizeType paperSize)
        {
            return paperSize switch
            {
                PaperSizeType.A4 or PaperSizeType.A5 =>
                    new List<PaperOrientation> { PaperOrientation.Portrait, PaperOrientation.Landscape },
                _ => new List<PaperOrientation> { PaperOrientation.Portrait }
            };
        }
    }
}