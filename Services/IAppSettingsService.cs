using TasraPostaManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TasraPostaManager.Services
{
    public interface IAppSettingsService
    {
        // ========================
        //  TEMEL AYAR METODLARI
        // ========================
        /// <summary>
        /// AppSettings tablosundaki ham entity'i döner (senkron).
        /// Yeni kod yazarken mümkünse async snapshot tabanlı metotları tercih et.
        /// </summary>
        AppSettings GetAppSettings();

        /// <summary>
        /// AppSettings entity'sini asenkron olarak kaydeder.
        /// </summary>
        Task SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// Uygulama açılışında default ayarların var olduğundan emin olur.
        /// </summary>
        Task EnsureDefaultSettingsAsync();

        /// <summary>
        /// Ayarları validate edip, hatasızsa kaydeder.
        /// </summary>
        Task ValidateAndSaveSettingsAsync(AppSettings settings);

        // ========================
        //  BARKOD SİSTEMİ METODLARI
        // ========================
        /// <summary>
        /// Barkod konfigürasyonunu döner.
        /// </summary>
        BarcodeConfig GetBarcodeConfig();

        /// <summary>
        /// Barkod konfigürasyonunu asenkron kaydeder.
        /// </summary>
        Task SaveBarcodeConfigAsync(BarcodeConfig config); // 🔹 Async

        /// <summary>
        /// Bir sonraki barkodu üretir ve string olarak döner.
        /// </summary>
        Task<string> GenerateNextBarcodeAsync();

        /// <summary>
        /// Barkod tahsisi için TEK entry-point.
        /// 
        /// - DefaultBarcodeMode == Pool ise BarcodePoolItems tablosundan claim eder.
        /// - DefaultBarcodeMode == Legacy ise mevcut dinamik üretim mantığını kullanır.
        /// 
        /// Pool modunda barkod tükenirse veya havuz erişilemiyorsa exception fırlatır.
        /// (Sessiz RR/TP fallback YOKTUR.)
        /// </summary>
        Task<string> AllocateBarcodeAsync(string? usedByRecordKey = null);

        /// <summary>
        /// Kalan üretilebilir barkod adedini döner.
        /// </summary>
        long GetRemainingBarcodeCount();

        // ========================
        //  VALIDATION METODLARI
        // ========================
        /// <summary>
        /// Verilen AppSettings için validation hatalarını listeler.
        /// </summary>
        List<string> ValidateSettings(AppSettings settings);

        // ========================
        //  LEGACY METODLAR (Uyumluluk için)
        // ========================
        /// <summary>
        /// Mevcut senkron snapshot okuma metodu (eski kodlar için).
        /// Yeni geliştirmelerde mümkünse GetSettingsAsync kullan.
        /// </summary>
        AppSettingsSnapshot GetSettings();

        /// <summary>
        /// Snapshot üzerinden ayarları kaydeden eski metot.
        /// </summary>
        Task SaveSettingsAsync(AppSettingsSnapshot snapshot); // 🔹 Async

        /// <summary>
        /// Varsayılan gönderen bilgisini senkron döner.
        /// Yeni geliştirmede mümkünse GetDefaultGonderenAsync kullan.
        /// </summary>
        string GetDefaultGonderen();

        /// <summary>
        /// Varsayılan gönderen bilgisini senkron olarak ayarlar.
        /// Yeni geliştirmede mümkünse SetDefaultGonderenAsync kullan.
        /// </summary>
        void SetDefaultGonderen(string gonderen);

        /// <summary>
        /// Generic string ayar okuma (eski kodlarla uyumluluk için).
        /// </summary>
        string GetSetting(string key, string defaultValue = "");

        /// <summary>
        /// Generic string ayar yazma (eski kodlarla uyumluluk için).
        /// </summary>
        void SetSetting(string key, string value);

        // ========================
        //  ETİKET & SAYFA AYARLARI v2
        // ========================
        /// <summary>
        /// Etiket & Sayfa Ayarları v2 yapısını döner.
        /// </summary>
        LabelSettings GetLabelSettingsV2();

        /// <summary>
        /// Etiket & Sayfa Ayarları v2'yi kaydeder.
        /// </summary>
        Task SaveLabelSettingsV2Async(LabelSettings label);

        // ========================
        //  SNAPSHOT & GÖNDEREN ASYNC METOTLARI (YENİ)
        // ========================

        /// <summary>
        /// Tüm ayarların asenkron snapshot'ını döner.
        /// Yeni kodlarda bunu kullanmak daha güvenli ve uygundur.
        /// </summary>
        Task<AppSettingsSnapshot> GetSettingsAsync();

        /// <summary>
        /// Varsayılan gönderen bilgisini asenkron döner.
        /// Etiket ve Liste PDF gibi yeni geliştirmelerde bunu kullan.
        /// </summary>
        Task<string> GetDefaultGonderenAsync();

        /// <summary>
        /// Varsayılan gönderen bilgisini asenkron olarak kaydeder.
        /// Gönderen ayar modalinde bu metodu kullanacağız.
        /// </summary>
        Task SetDefaultGonderenAsync(string gonderen);
    }
}
