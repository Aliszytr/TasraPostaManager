namespace TasraPostaManager.Services
{
    public class BarcodeConfig
    {
        // 🔢 Sayısal barkod üretim ayarları
        public string Prefix { get; set; } = "";
        public long StartNumber { get; set; } = 7876500000000;
        public int DigitCount { get; set; } = 13;
        public string Suffix { get; set; } = "";
        public long Quantity { get; set; } = 1_000_000;
        public long CurrentIndex { get; set; } = 0;

        // 🎨 Görsel / boyut ayarları (Barcode Size Engine)
        /// <summary>
        /// small / medium / large / xlarge
        /// </summary>
        public string DefaultBarcodeSize { get; set; } = "medium";

        /// <summary>
        /// Barkodun altında/üstünde metin (numara) yazılsın mı?
        /// </summary>
        public bool ShowBarcodeText { get; set; } = true;

        /// <summary>
        /// "small" seçildiğinde kullanılacak yükseklik (ör: 12 pt / px karşılığı)
        /// </summary>
        public int BarcodeHeightSmall { get; set; } = 12;

        /// <summary>
        /// "medium" seçildiğinde kullanılacak yükseklik (varsayılan)
        /// </summary>
        public int BarcodeHeightMedium { get; set; } = 20;

        /// <summary>
        /// "large" seçildiğinde kullanılacak yükseklik
        /// </summary>
        public int BarcodeHeightLarge { get; set; } = 30;

        /// <summary>
        /// "xlarge" seçildiğinde kullanılacak yükseklik
        /// </summary>
        public int BarcodeHeightXLarge { get; set; } = 40;
    }
}
