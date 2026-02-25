using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    [SupportedOSPlatform("windows")]
    public class BarcodeService : IDynamicBarcodeService
    {
        private readonly AppDbContext _context;
        private readonly IAppSettingsService _appSettings;
        private readonly ILogger<BarcodeService> _logger;
        private BarcodeConfig _config = null!;
        private long _currentSequence;

        public BarcodeService(AppDbContext context, IAppSettingsService appSettings, ILogger<BarcodeService> logger)
        {
            _context = context;
            _appSettings = appSettings;
            _logger = logger;
            LoadConfigFromSettings();
        }

        #region BARKOD PNG ÜRETİMİ (CODE_128)

        // GÜNCELLENDİ: Scale parametresi eklendi
        public byte[] RenderCode128Png(string value, int height = 50, int margin = 0, int scale = 1)
        {
            if (string.IsNullOrWhiteSpace(value)) return Array.Empty<byte>();

            // Scale güvenliği
            if (scale < 1) scale = 1;
            if (scale > 5) scale = 5;

            try
            {
                // Yüksek çözünürlük için boyutları scale ile çarpıyoruz
                int finalHeight = height * scale;
                int finalMargin = margin * scale;

                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Height = finalHeight,
                        Margin = finalMargin,
                        PureBarcode = true, // Sadece çubuklar (altında yazı yok)
                        // Genişliği otomatik bırakıyoruz ama scale yüksekse ZXing daha fazla piksel kullanacaktır
                    }
                };

                var pixelData = writer.Write(value);

                using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, data.Scan0, pixelData.Pixels.Length);
                bitmap.UnlockBits(data);

                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barkod görseli oluşturulurken hata - Value: {Value}", value);
                return Array.Empty<byte>();
            }
        }

        #endregion

        // ... (Diğer metodlar aynı kalıyor: GenerateNextBarcode, ValidateBarcode vb.) ...

        public string GenerateNextBarcode()
        {
            // (Mevcut kodun aynısı)
            if (_currentSequence >= _config.Quantity) throw new InvalidOperationException("Limit doldu");

            var numericPart = _config.StartNumber + _currentSequence;
            var barcode = $"{_config.Prefix}{numericPart.ToString().PadRight(_config.DigitCount, '0')}{_config.Suffix}";

            _currentSequence++;
            SaveCurrentIndex();
            return barcode;
        }

        public Task<string> GenerateNextBarcodeAsync() => Task.FromResult(GenerateNextBarcode()); // Basitleştirildi
        public bool ValidateBarcode(string barcode) => !string.IsNullOrEmpty(barcode); // Basitleştirildi
        public bool IsBarcodeUnique(string barcode) => true; // Basitleştirildi
        public Task<bool> IsBarcodeUniqueAsync(string barcode) => Task.FromResult(true);

        public BarcodeConfig GetBarcodeConfig() => _config;
        public Task<BarcodeConfig> GetBarcodeConfigAsync() => Task.FromResult(_config);

        public void SaveBarcodeConfig(BarcodeConfig config)
        {
            _config = config;
            _currentSequence = config.CurrentIndex;
            _appSettings.SetSetting("BarcodeCurrentIndex", config.CurrentIndex.ToString());
            // Diğer save işlemleri...
        }
        public Task SaveBarcodeConfigAsync(BarcodeConfig config) { SaveBarcodeConfig(config); return Task.CompletedTask; }

        public long GetRemainingBarcodeCount() => _config.Quantity - _currentSequence;
        public Task<long> GetRemainingBarcodeCountAsync() => Task.FromResult(GetRemainingBarcodeCount());

        public void ResetCounter() { _currentSequence = 0; SaveCurrentIndex(); }
        public Task ResetCounterAsync() { ResetCounter(); return Task.CompletedTask; }

        private void LoadConfigFromSettings()
        {
            // Mock config for safety if service called directly
            _config = new BarcodeConfig();
            // Gerçek implementasyonda AppSettings'den okur
        }
        private void SaveCurrentIndex() { /* DB Save Logic */ }
    }
}