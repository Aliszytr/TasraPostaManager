namespace TasraPostaManager.Services
{
    public interface IDynamicBarcodeService
    {
        // Üretim
        string GenerateNextBarcode();
        Task<string> GenerateNextBarcodeAsync();

        // Doğrulama
        bool ValidateBarcode(string barcode);

        // Benzersizlik
        bool IsBarcodeUnique(string barcode);
        Task<bool> IsBarcodeUniqueAsync(string barcode);

        // Render (GÜNCELLENDİ: Scale parametresi eklendi)
        byte[] RenderCode128Png(string value, int height = 50, int margin = 0, int scale = 1);

        // Konfigürasyon
        BarcodeConfig GetBarcodeConfig();
        Task<BarcodeConfig> GetBarcodeConfigAsync();
        void SaveBarcodeConfig(BarcodeConfig config);
        Task SaveBarcodeConfigAsync(BarcodeConfig config);

        // Sayaç
        long GetRemainingBarcodeCount();
        Task<long> GetRemainingBarcodeCountAsync();
        void ResetCounter();
        Task ResetCounterAsync();
    }
}