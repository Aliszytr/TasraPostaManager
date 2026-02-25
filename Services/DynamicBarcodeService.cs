using Microsoft.Extensions.Logging;
using TasraPostaManager.Data;

namespace TasraPostaManager.Services
{
    // Eski ismi korumak için köprü sınıf
    public class DynamicBarcodeService : BarcodeService
    {
        public DynamicBarcodeService(
            AppDbContext? context,
            IAppSettingsService appSettings,
            ILogger<BarcodeService> logger
        ) : base(context!, appSettings, logger)
        {
        }
    }
}
