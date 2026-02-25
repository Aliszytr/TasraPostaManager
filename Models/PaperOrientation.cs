// PaperOrientation.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// Kağıt yönü - Dikey veya Yatay
    /// </summary>
    public enum PaperOrientation
    {
        [Display(Name = "Dikey")]
        Portrait = 0,    // Dikey

        [Display(Name = "Yatay")]
        Landscape = 1    // Yatay
    }

    /// <summary>
    /// PaperOrientation extension metodları
    /// </summary>
    public static class PaperOrientationExtensions
    {
        /// <summary>
        /// Enum değerinin görünen adını döndürür
        /// </summary>
        public static string GetDisplayName(this PaperOrientation orientation)
        {
            var field = orientation.GetType().GetField(orientation.ToString());
            var attribute = field?
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return attribute?.Name ?? orientation.ToString();
        }

        /// <summary>
        /// Yöne göre kağıt boyutlarını döndürür (genişlik, yükseklik)
        /// </summary>
        public static (double Width, double Height) GetPageDimensions(this PaperOrientation orientation, PaperSizeType paperSize)
        {
            var (width, height) = paperSize.GetDimensionsInMm();

            return orientation == PaperOrientation.Landscape
                ? (height, width)   // Yatay: yükseklik genişlik, genişlik yükseklik olur
                : (width, height);  // Dikey: normal boyutlar
        }

        /// <summary>
        /// Yönün dikey olup olmadığını kontrol eder
        /// </summary>
        public static bool IsPortrait(this PaperOrientation orientation)
        {
            return orientation == PaperOrientation.Portrait;
        }

        /// <summary>
        /// Yönün yatay olup olmadığını kontrol eder
        /// </summary>
        public static bool IsLandscape(this PaperOrientation orientation)
        {
            return orientation == PaperOrientation.Landscape;
        }

        /// <summary>
        /// Yön değiştirme (toggle)
        /// </summary>
        public static PaperOrientation Toggle(this PaperOrientation orientation)
        {
            return orientation == PaperOrientation.Portrait
                ? PaperOrientation.Landscape
                : PaperOrientation.Portrait;
        }

        /// <summary>
        /// CSS class adını döndürür (styling için)
        /// </summary>
        public static string GetCssClass(this PaperOrientation orientation)
        {
            return orientation == PaperOrientation.Portrait ? "orientation-portrait" : "orientation-landscape";
        }

        /// <summary>
        /// Bootstrap icon class'ını döndürür
        /// </summary>
        public static string GetIconClass(this PaperOrientation orientation)
        {
            return orientation == PaperOrientation.Portrait ? "bi-arrow-up-down" : "bi-arrow-left-right";
        }

        /// <summary>
        /// FontAwesome icon class'ını döndürür
        /// </summary>
        public static string GetFontAwesomeIcon(this PaperOrientation orientation)
        {
            return orientation == PaperOrientation.Portrait ? "fas fa-arrows-alt-v" : "fas fa-arrows-alt-h";
        }

        /// <summary>
        /// Yöne göre etiket yerleşimini optimize eder
        /// </summary>
        public static (int RecommendedColumns, int RecommendedRows) GetRecommendedGrid(
            this PaperOrientation orientation,
            PaperSizeType paperSize,
            double labelWidth,
            double labelHeight)
        {
            var dimensions = orientation.GetPageDimensions(paperSize);
            double availableWidth = dimensions.Width - 20; // 10mm kenar boşlukları
            double availableHeight = dimensions.Height - 20;

            int maxColumns = (int)(availableWidth / labelWidth);
            int maxRows = (int)(availableHeight / labelHeight);

            // Minimum 1 sütun/satır garantisi
            maxColumns = Math.Max(1, maxColumns);
            maxRows = Math.Max(1, maxRows);

            return (maxColumns, maxRows);
        }
    }

    /// <summary>
    /// PaperOrientation için helper sınıfı
    /// </summary>
    public static class PaperOrientations
    {
        /// <summary>
        /// Tüm yön seçenekleri
        /// </summary>
        public static readonly Dictionary<PaperOrientation, string> All = new()
        {
            { PaperOrientation.Portrait, "Dikey" },
            { PaperOrientation.Landscape, "Yatay" }
        };

        /// <summary>
        /// Yön seçeneklerini SelectList için hazırlar
        /// </summary>
        public static List<KeyValuePair<string, string>> GetSelectList()
        {
            return All.Select(x => new KeyValuePair<string, string>(
                x.Key.ToString(),
                x.Value
            )).ToList();
        }

        /// <summary>
        /// Varsayılan yönü döndürür
        /// </summary>
        public static PaperOrientation GetDefault()
        {
            return PaperOrientation.Portrait;
        }

        /// <summary>
        /// Yöne göre açıklama döndürür
        /// </summary>
        public static string GetDescription(PaperOrientation orientation)
        {
            return orientation switch
            {
                PaperOrientation.Portrait => "Kağıt dikey konumda - normal okuma yönü",
                PaperOrientation.Landscape => "Kağıt yatay konumda - geniş içerikler için",
                _ => "Bilinmeyen yön"
            };
        }

        /// <summary>
        /// Yön değişikliğinin etkilerini analiz eder
        /// </summary>
        public static string GetOrientationImpact(PaperOrientation current, PaperOrientation proposed, PaperSizeType paperSize)
        {
            var currentDims = current.GetPageDimensions(paperSize);
            var proposedDims = proposed.GetPageDimensions(paperSize);

            if (current == proposed)
                return "Yön değişmedi";

            string impact = current == PaperOrientation.Portrait
                ? $"Dikey → Yatay: Genişlik {currentDims.Width}mm → {proposedDims.Width}mm, Yükseklik {currentDims.Height}mm → {proposedDims.Height}mm"
                : $"Yatay → Dikey: Genişlik {currentDims.Width}mm → {proposedDims.Width}mm, Yükseklik {currentDims.Height}mm → {proposedDims.Height}mm";

            return impact;
        }
    }
}