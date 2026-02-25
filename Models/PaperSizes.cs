// PaperSizes.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// PaperSizeType için helper sınıfı (UI için)
    /// </summary>
    public static class PaperSizes
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

        /// <summary>
        /// Kağıt boyutunun görünen adını döndürür
        /// </summary>
        public static string GetDisplayName(PaperSizeType paperSize)
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
        /// Kağıt boyutunu string'den enum'a çevirir
        /// </summary>
        public static PaperSizeType FromString(string paperSize)
        {
            if (Enum.TryParse<PaperSizeType>(paperSize, true, out var result))
            {
                return result;
            }
            return GetDefault();
        }
    }
}