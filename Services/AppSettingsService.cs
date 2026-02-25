using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using TasraPostaManager.Core.Interfaces;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AppSettingsService> _logger;
        private readonly IBarcodePoolService? _barcodePool;
        private readonly ICachingService? _cache;

        private const string CacheKeyAppSettings = "AppSettings:All";
        private const string CacheKeyStats = "Stats:Dashboard";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public AppSettingsService(AppDbContext db, ILogger<AppSettingsService> logger, IBarcodePoolService? barcodePool = null, ICachingService? cache = null)
        {
            _db = db;
            _logger = logger;
            _barcodePool = barcodePool;
            _cache = cache;
        }

        // ========================
        //  TEMEL AYAR METODLARI
        // ========================
        public AppSettings GetAppSettings()
        {
            // 🔄 Önbellek kontrolü
            var cached = _cache?.Get<AppSettings>(CacheKeyAppSettings);
            if (cached != null) return cached;

            try
            {
                var settings = new AppSettings();

                // 📁 DOSYA AYARLARI
                settings.PdfOutputPath = GetSetting("PdfOutputPath", @"D:\TasraPostaManagerOutput");
                settings.PdfExternalPath = GetSetting("PdfExternalPath", "");
                settings.DefaultGonderen = GetSetting("DefaultGonderen", "Taşra Belediyesi");
                settings.DefaultPaperSize = GetSetting("DefaultPaperSize", "A4");

                // 🏷️ ETİKET BASKI AYARLARI (eski v1 alanları)
                if (int.TryParse(GetSetting("LabelsPerPage", "14"), out var labelsPerPage))
                    settings.LabelsPerPage = labelsPerPage;

                // 📐 ÖZEL OFSET AYARLARI (eski v1)
                settings.UseCustomOffset = bool.Parse(GetSetting("UseCustomOffset", "false"));
                settings.CustomOffsetTopMM = double.Parse(GetSetting("CustomOffsetTopMM", "14.0"), CultureInfo.InvariantCulture);
                settings.CustomOffsetLeftMM = double.Parse(GetSetting("CustomOffsetLeftMM", "5.0"), CultureInfo.InvariantCulture);
                settings.LabelWidthMM = double.Parse(GetSetting("LabelWidthMM", "100.0"), CultureInfo.InvariantCulture);
                settings.LabelHeightMM = double.Parse(GetSetting("LabelHeightMM", "38.0"), CultureInfo.InvariantCulture);

                // 🎨 GELİŞMİŞ LAYOUT AYARLARI (eski v1)
                settings.UseAdvancedLayout = bool.Parse(GetSetting("UseAdvancedLayout", "false"));
                settings.PreferredColumns = GetNullableInt(GetSetting("PreferredColumns", ""));
                settings.PreferredRows = GetNullableInt(GetSetting("PreferredRows", ""));
                settings.ForceSingleLabelPerPage = bool.Parse(GetSetting("ForceSingleLabelPerPage", "false"));

                // 🔤 FONT VE RENK AYARLARI
                settings.PrimaryFontFamily = GetSetting("PrimaryFontFamily", "Arial");
                settings.SecondaryFontFamily = GetSetting("SecondaryFontFamily", "Courier New");
                settings.SenderTextColor = GetSetting("SenderTextColor", "#000000");
                settings.ReceiverTextColor = GetSetting("ReceiverTextColor", "#000000");
                settings.AddressTextColor = GetSetting("AddressTextColor", "#666666");

                // 🏷️ ETİKET TASARIM AYARLARI
                settings.UseEnhancedLabelDesign = bool.Parse(GetSetting("UseEnhancedLabelDesign", "true"));
                settings.SenderLabelStyle = GetSetting("SenderLabelStyle", "BOLD_VIBRANT");
                settings.ReceiverLabelStyle = GetSetting("ReceiverLabelStyle", "BOLD_HIGHLIGHT");
                settings.AddressLabelStyle = GetSetting("AddressLabelStyle", "READABLE_PROFESSIONAL");

                // 🎫 BARKOD AYARLARI - ESNEK SİSTEM
                settings.BarcodePrefix = GetSetting("BarcodePrefix", "RR");
                settings.BarcodeStartNumber = long.Parse(GetSetting("BarcodeStartNumber", "78765000000"));
                settings.BarcodeDigitCount = int.Parse(GetSetting("BarcodeDigitCount", "11"));
                settings.BarcodeSuffix = GetSetting("BarcodeSuffix", "");
                settings.BarcodeQuantity = long.Parse(GetSetting("BarcodeQuantity", "1000000"));
                settings.BarcodeCurrentIndex = long.Parse(GetSetting("BarcodeCurrentIndex", "0"));

                // 🔍 BARKOD GÖRSELLİK AYARLARI
                settings.DefaultBarcodeSize = GetSetting("DefaultBarcodeSize", "medium");
                settings.ShowBarcodeText = bool.Parse(GetSetting("ShowBarcodeText", "true"));
                settings.BarcodeHeightSmall = int.Parse(GetSetting("BarcodeHeightSmall", "12"));
                settings.BarcodeHeightMedium = int.Parse(GetSetting("BarcodeHeightMedium", "20"));
                settings.BarcodeHeightLarge = int.Parse(GetSetting("BarcodeHeightLarge", "30"));
                settings.BarcodeHeightXLarge = int.Parse(GetSetting("BarcodeHeightXLarge", "40"));

                // ✅ Barkod Modu (Legacy/Pool)
                settings.DefaultBarcodeMode = GetSetting("DefaultBarcodeMode", "Legacy");
                if (int.TryParse(GetSetting("BarcodePoolLowThreshold", "5000"), out var poolThreshold))
                    settings.BarcodePoolLowThreshold = poolThreshold;

                // ==============================
                //  📄 ETİKET & SAYFA AYARLARI v2
                // ==============================
                settings.LabelSettings = GetLabelSettingsV2();

                // 🔄 Önbelleğe al
                _cache?.Set(CacheKeyAppSettings, settings, CacheDuration);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayarlar yüklenirken hata oluştu");
                return CreateDefaultAppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                // 📁 DOSYA AYARLARI
                SetSetting("PdfOutputPath", settings.PdfOutputPath);
                SetSetting("PdfExternalPath", settings.PdfExternalPath);
                SetSetting("DefaultGonderen", settings.DefaultGonderen);
                SetSetting("DefaultPaperSize", settings.DefaultPaperSize);

                // 🏷️ ETİKET BASKI AYARLARI (v1)
                SetSetting("LabelsPerPage", settings.LabelsPerPage.ToString());

                // 📐 ÖZEL OFSET AYARLARI (v1)
                SetSetting("UseCustomOffset", settings.UseCustomOffset.ToString().ToLower());
                SetSetting("CustomOffsetTopMM", settings.CustomOffsetTopMM.ToString(CultureInfo.InvariantCulture));
                SetSetting("CustomOffsetLeftMM", settings.CustomOffsetLeftMM.ToString(CultureInfo.InvariantCulture));
                SetSetting("LabelWidthMM", settings.LabelWidthMM.ToString(CultureInfo.InvariantCulture));
                SetSetting("LabelHeightMM", settings.LabelHeightMM.ToString(CultureInfo.InvariantCulture));

                // 🎨 GELİŞMİŞ LAYOUT AYARLARI (v1)
                SetSetting("UseAdvancedLayout", settings.UseAdvancedLayout.ToString().ToLower());
                SetSetting("PreferredColumns", settings.PreferredColumns?.ToString() ?? "");
                SetSetting("PreferredRows", settings.PreferredRows?.ToString() ?? "");
                SetSetting("ForceSingleLabelPerPage", settings.ForceSingleLabelPerPage.ToString().ToLower());

                // 🔤 FONT VE RENK AYARLARI
                SetSetting("PrimaryFontFamily", settings.PrimaryFontFamily);
                SetSetting("SecondaryFontFamily", settings.SecondaryFontFamily);
                SetSetting("SenderTextColor", settings.SenderTextColor);
                SetSetting("ReceiverTextColor", settings.ReceiverTextColor);
                SetSetting("AddressTextColor", settings.AddressTextColor);

                // 🏷️ ETİKET TASARIM AYARLARI
                SetSetting("UseEnhancedLabelDesign", settings.UseEnhancedLabelDesign.ToString().ToLower());
                SetSetting("SenderLabelStyle", settings.SenderLabelStyle);
                SetSetting("ReceiverLabelStyle", settings.ReceiverLabelStyle);
                SetSetting("AddressLabelStyle", settings.AddressLabelStyle);

                // 🎫 BARKOD AYARLARI
                SetSetting("BarcodePrefix", settings.BarcodePrefix);
                SetSetting("BarcodeStartNumber", settings.BarcodeStartNumber.ToString());
                SetSetting("BarcodeDigitCount", settings.BarcodeDigitCount.ToString());
                SetSetting("BarcodeSuffix", settings.BarcodeSuffix);
                SetSetting("BarcodeQuantity", settings.BarcodeQuantity.ToString());
                SetSetting("BarcodeCurrentIndex", settings.BarcodeCurrentIndex.ToString());

                // 🔍 BARKOD GÖRSELLİK AYARLARI
                SetSetting("DefaultBarcodeSize", settings.DefaultBarcodeSize);
                SetSetting("ShowBarcodeText", settings.ShowBarcodeText.ToString().ToLower());
                SetSetting("BarcodeHeightSmall", settings.BarcodeHeightSmall.ToString());
                SetSetting("BarcodeHeightMedium", settings.BarcodeHeightMedium.ToString());
                SetSetting("BarcodeHeightLarge", settings.BarcodeHeightLarge.ToString());
                SetSetting("BarcodeHeightXLarge", settings.BarcodeHeightXLarge.ToString());

                // ✅ Barkod Modu (Legacy/Pool)
                SetSetting("DefaultBarcodeMode", string.IsNullOrWhiteSpace(settings.DefaultBarcodeMode) ? "Legacy" : settings.DefaultBarcodeMode);
                SetSetting("BarcodePoolLowThreshold", settings.BarcodePoolLowThreshold.ToString());

                // ==============================
                //  📄 ETİKET & SAYFA AYARLARI v2
                // ==============================
                if (settings.LabelSettings != null)
                {
                    await SaveLabelSettingsV2Async(settings.LabelSettings);
                }

                await _db.SaveChangesAsync();

                // 🔄 Cache temizle — ayarlar değişti
                InvalidateSettingsCache();
                _logger.LogInformation("Ayarlar başarıyla kaydedildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayarlar kaydedilirken hata oluştu");
                throw;
            }
        }

        // ========================
        //  ETİKET & SAYFA AYARLARI v2 - TAM DÜZELTİLMİŞ VERSİYON
        // ========================
        public LabelSettings GetLabelSettingsV2()
        {
            try
            {
                var label = new LabelSettings();

                // 🧾 Kağıt Ayarları
                label.PaperSize = ParseEnum(GetSetting("Label.PaperSize", label.PaperSize.ToString()), label.PaperSize);
                label.Orientation = ParseEnum(GetSetting("Label.Orientation", label.Orientation.ToString()), label.Orientation);
                label.Template = ParseEnum(GetSetting("Label.Template", label.Template.ToString()), label.Template);

                // 📐 Sayfa Kenar Boşlukları
                label.TopMarginMm = ParseDouble(GetSetting("Label.TopMarginMm", "14.0"), 14.0);
                label.LeftMarginMm = ParseDouble(GetSetting("Label.LeftMarginMm", "5.0"), 5.0);
                label.RightMarginMm = ParseDouble(GetSetting("Label.RightMarginMm", "5.0"), 5.0);
                label.BottomMarginMm = ParseDouble(GetSetting("Label.BottomMarginMm", "5.0"), 5.0);

                // 📏 Etiket Boyutları
                label.LabelWidthMm = ParseDouble(GetSetting("Label.LabelWidthMm", "70.0"), 70.0);
                label.LabelHeightMm = ParseDouble(GetSetting("Label.LabelHeightMm", "38.0"), 38.0);

                // 📊 Grid
                label.Columns = ParseInt(GetSetting("Label.Columns", "3"), 3);
                label.Rows = ParseInt(GetSetting("Label.Rows", "7"), 7);

                // 🔁 Etiketler Arası Boşluk
                label.HorizontalGapMm = ParseDouble(GetSetting("Label.HorizontalGapMm", "2.0"), 2.0);
                label.VerticalGapMm = ParseDouble(GetSetting("Label.VerticalGapMm", "0.0"), 0.0);

                // ⭐ PDF ve Etiket Ayarları
                label.FontSize = ParseInt(GetSetting("Label.FontSize", "10"), 10);
                label.IncludeBarcode = ParseBool(GetSetting("Label.IncludeBarcode", "true"), true);
                label.BarcodeSize = GetSetting("Label.BarcodeSize", "medium");
                label.DrawLabelBorders = ParseBool(GetSetting("Label.DrawLabelBorders", "false"), false);
                label.BarcodeEmphasis = ParseInt(GetSetting("Label.BarcodeEmphasis", "1"), 1);

                // 👁‍🗨 ETİKET ÜZERİNDE GÖRÜNTÜLENECEK ALANLAR
                label.ShowMuhabereNo = ParseBool(GetSetting("Label.ShowMuhabereNo", "true"), true);
                // [FIX]: Muhabere kutu gösterimi okuma
                label.ShowMuhabereBox = ParseBool(GetSetting("Label.ShowMuhabereBox", "false"), false);

                label.ShowGittigiYer = ParseBool(GetSetting("Label.ShowGittigiYer", "true"), true);
                label.ShowGonderenBilgisi = ParseBool(GetSetting("Label.ShowGonderenBilgisi", "true"), true);
                label.ShowMiktar = ParseBool(GetSetting("Label.ShowMiktar", "true"), true);
                label.ShowBarkodNo = ParseBool(GetSetting("Label.ShowBarkodNo", "true"), true);
                label.ShowTarih = ParseBool(GetSetting("Label.ShowTarih", "false"), false);

                // 📋 TESLİM LİSTESİ AYARLARI
                label.ListFontSize = ParseInt(GetSetting("Label.ListFontSize", "9"), 9);
                label.ListShowRowNumber = ParseBool(GetSetting("Label.ListShowRowNumber", "true"), true);
                label.ListShowBarcode = ParseBool(GetSetting("Label.ListShowBarcode", "true"), true);
                label.ListShowReceiver = ParseBool(GetSetting("Label.ListShowReceiver", "true"), true);
                label.ListShowAmount = ParseBool(GetSetting("Label.ListShowAmount", "true"), true);
                label.ListShowDate = ParseBool(GetSetting("Label.ListShowDate", "true"), true);
                label.ListShowSignature = ParseBool(GetSetting("Label.ListShowSignature", "true"), true);

                // 🔧 Custom Page Size
                label.CustomPageWidthMm = ParseDouble(GetSetting("Label.CustomPageWidthMm", "210.0"), 210.0);
                label.CustomPageHeightMm = ParseDouble(GetSetting("Label.CustomPageHeightMm", "297.0"), 297.0);

                return label;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LabelSettingsV2 okunurken hata oluştu, varsayılan değerler kullanılacak.");
                return new LabelSettings();
            }
        }

        public async Task SaveLabelSettingsV2Async(LabelSettings label)
        {
            try
            {
                // Eğer Custom değilse şablon uygula
                if (label.Template != LabelTemplateType.Custom)
                {
                    label.ApplyTemplateFromDefinition();
                }

                // 🧾 Kağıt Ayarları
                SetSetting("Label.PaperSize", label.PaperSize.ToString());
                SetSetting("Label.Orientation", label.Orientation.ToString());
                SetSetting("Label.Template", label.Template.ToString());

                // 📐 Sayfa Kenar Boşlukları
                SetSetting("Label.TopMarginMm", label.TopMarginMm.ToString(CultureInfo.InvariantCulture));
                SetSetting("Label.LeftMarginMm", label.LeftMarginMm.ToString(CultureInfo.InvariantCulture));
                SetSetting("Label.RightMarginMm", label.RightMarginMm.ToString(CultureInfo.InvariantCulture));
                SetSetting("Label.BottomMarginMm", label.BottomMarginMm.ToString(CultureInfo.InvariantCulture));

                // 📏 Etiket Boyutları
                SetSetting("Label.LabelWidthMm", label.LabelWidthMm.ToString(CultureInfo.InvariantCulture));
                SetSetting("Label.LabelHeightMm", label.LabelHeightMm.ToString(CultureInfo.InvariantCulture));

                // 📊 Grid
                SetSetting("Label.Columns", label.Columns.ToString());
                SetSetting("Label.Rows", label.Rows.ToString());

                // 🔁 Etiketler Arası Boşluk
                SetSetting("Label.HorizontalGapMm", label.HorizontalGapMm.ToString(CultureInfo.InvariantCulture));
                SetSetting("Label.VerticalGapMm", label.VerticalGapMm.ToString(CultureInfo.InvariantCulture));

                // ⭐ PDF ve Etiket Ayarları
                SetSetting("Label.FontSize", label.FontSize.ToString());
                SetSetting("Label.IncludeBarcode", label.IncludeBarcode.ToString().ToLower());
                SetSetting("Label.BarcodeSize", label.BarcodeSize);
                SetSetting("Label.DrawLabelBorders", label.DrawLabelBorders.ToString().ToLower());
                SetSetting("Label.BarcodeEmphasis", label.BarcodeEmphasis.ToString());

                // 👁‍🗨 ETİKET ÜZERİNDE GÖRÜNTÜLENECEK ALANLAR
                SetSetting("Label.ShowMuhabereNo", label.ShowMuhabereNo.ToString().ToLower());
                // [FIX]: Muhabere kutu gösterimi kaydetme
                SetSetting("Label.ShowMuhabereBox", label.ShowMuhabereBox.ToString().ToLower());

                SetSetting("Label.ShowGittigiYer", label.ShowGittigiYer.ToString().ToLower());
                SetSetting("Label.ShowGonderenBilgisi", label.ShowGonderenBilgisi.ToString().ToLower());
                SetSetting("Label.ShowMiktar", label.ShowMiktar.ToString().ToLower());
                SetSetting("Label.ShowBarkodNo", label.ShowBarkodNo.ToString().ToLower());
                SetSetting("Label.ShowTarih", label.ShowTarih.ToString().ToLower());

                // 📋 TESLİM LİSTESİ AYARLARI
                SetSetting("Label.ListFontSize", label.ListFontSize.ToString());
                SetSetting("Label.ListShowRowNumber", label.ListShowRowNumber.ToString().ToLower());
                SetSetting("Label.ListShowBarcode", label.ListShowBarcode.ToString().ToLower());
                SetSetting("Label.ListShowReceiver", label.ListShowReceiver.ToString().ToLower());
                SetSetting("Label.ListShowAmount", label.ListShowAmount.ToString().ToLower());
                SetSetting("Label.ListShowDate", label.ListShowDate.ToString().ToLower());
                SetSetting("Label.ListShowSignature", label.ListShowSignature.ToString().ToLower());

                // 🔧 Custom Page Size
                SetSetting("Label.CustomPageWidthMm", label.CustomPageWidthMm.ToString(CultureInfo.InvariantCulture));
                SetSetting("Label.CustomPageHeightMm", label.CustomPageHeightMm.ToString(CultureInfo.InvariantCulture));

                await _db.SaveChangesAsync();
                _logger.LogInformation("LabelSettingsV2 (Tüm alanlar) başarıyla kaydedildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LabelSettingsV2 kaydedilirken hata oluştu");
                throw;
            }
        }

        // ========================
        //  BARKOD SİSTEMİ METODLARI
        // ========================
        public BarcodeConfig GetBarcodeConfig()
        {
            var settings = GetAppSettings();

            // 🔧 Barcode Size Engine ile zenginleştirilmiş config
            return new BarcodeConfig
            {
                Prefix = settings.BarcodePrefix,
                StartNumber = settings.BarcodeStartNumber,
                DigitCount = settings.BarcodeDigitCount,
                Suffix = settings.BarcodeSuffix,
                Quantity = settings.BarcodeQuantity,
                CurrentIndex = settings.BarcodeCurrentIndex,

                // 🔍 Görsel boyut & stil bilgileri
                DefaultBarcodeSize = settings.DefaultBarcodeSize,
                ShowBarcodeText = settings.ShowBarcodeText,
                BarcodeHeightSmall = settings.BarcodeHeightSmall,
                BarcodeHeightMedium = settings.BarcodeHeightMedium,
                BarcodeHeightLarge = settings.BarcodeHeightLarge,
                BarcodeHeightXLarge = settings.BarcodeHeightXLarge
            };
        }

        public async Task SaveBarcodeConfigAsync(BarcodeConfig config)
        {
            var settings = GetAppSettings();

            // Temel numaralama ayarları
            settings.BarcodePrefix = config.Prefix;
            settings.BarcodeStartNumber = config.StartNumber;
            settings.BarcodeDigitCount = config.DigitCount;
            settings.BarcodeSuffix = config.Suffix;
            settings.BarcodeQuantity = config.Quantity;
            settings.BarcodeCurrentIndex = config.CurrentIndex;

            // 🔍 Barkod görsellik ayarları (Size Engine)
            settings.DefaultBarcodeSize = string.IsNullOrWhiteSpace(config.DefaultBarcodeSize)
                ? "medium"
                : config.DefaultBarcodeSize;

            settings.ShowBarcodeText = config.ShowBarcodeText;
            settings.BarcodeHeightSmall = config.BarcodeHeightSmall;
            settings.BarcodeHeightMedium = config.BarcodeHeightMedium;
            settings.BarcodeHeightLarge = config.BarcodeHeightLarge;
            settings.BarcodeHeightXLarge = config.BarcodeHeightXLarge;

            await SaveSettingsAsync(settings);
        }

        public Task<string> GenerateNextBarcodeAsync()
            => AllocateBarcodeAsync(null);

        public async Task<string> AllocateBarcodeAsync(string? usedByRecordKey = null)
        {
            // ✅ Pool modu açıksa: DB havuzundan claim et
            var mode = GetSetting("DefaultBarcodeMode", "Legacy");
            if (string.Equals(mode, "Pool", StringComparison.OrdinalIgnoreCase))
            {
                if (_barcodePool == null)
                    throw new InvalidOperationException("Pool modu aktif ancak IBarcodePoolService DI'da bulunamadı.");

                // Pool modunda sessiz fallback YOK.
                return await _barcodePool.ClaimNextAsync(usedByRecordKey, CancellationToken.None);
            }

            var config = GetBarcodeConfig();

            if (config.CurrentIndex >= config.Quantity)
                throw new InvalidOperationException("Barkod limiti doldu. Lütfen ayarları kontrol edin.");

            string barcode;
            bool exists;
            int maxRetry = 1000;

            do
            {
                var numericBase = config.StartNumber + config.CurrentIndex;
                var numericStr = numericBase.ToString();

                if (numericStr.Length > config.DigitCount)
                    numericStr = numericStr[^config.DigitCount..];
                else if (numericStr.Length < config.DigitCount)
                    numericStr = numericStr.PadLeft(config.DigitCount, '0');

                barcode = $"{config.Prefix}{numericStr}{config.Suffix}";

                exists = await _db.PostaRecords.AnyAsync(x => x.BarkodNo == barcode);

                if (!exists)
                {
                    config.CurrentIndex++;
                    SetSetting("BarcodeCurrentIndex", config.CurrentIndex.ToString());
                    await _db.SaveChangesAsync();
                    break;
                }

                config.CurrentIndex++;
                maxRetry--;

            } while (exists && config.CurrentIndex < config.Quantity && maxRetry > 0);

            if (maxRetry <= 0)
                throw new InvalidOperationException("Benzersiz barkod üretilemedi.");

            return barcode;
        }

        public long GetRemainingBarcodeCount()
        {
            var config = GetBarcodeConfig();
            return config.Quantity - config.CurrentIndex;
        }

        // ========================
        //  VALIDATION METODLARI
        // ========================
        public List<string> ValidateSettings(AppSettings settings)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.PdfOutputPath))
                errors.Add("PDF çıktı yolu zorunludur");

            if (string.IsNullOrWhiteSpace(settings.DefaultGonderen))
                errors.Add("Varsayılan gönderen bilgisi zorunludur");

            if (settings.BarcodeStartNumber < 1)
                errors.Add("Geçerli bir başlangıç numarası girin");

            if (settings.BarcodeStartNumber.ToString().Length > settings.BarcodeDigitCount)
                errors.Add($"Başlangıç numarası ({settings.BarcodeStartNumber}) basamak sayısından ({settings.BarcodeDigitCount}) uzun olamaz");

            if (settings.BarcodeDigitCount < 8 || settings.BarcodeDigitCount > 20)
                errors.Add("Basamak sayısı 8-20 arası olmalı");

            if (settings.BarcodeQuantity < 1)
                errors.Add("Geçerli bir barkod adedi girin");

            if (settings.CustomOffsetTopMM < 0 || settings.CustomOffsetTopMM > 100)
                errors.Add("Üst ofset 0-100 mm arası olmalı");

            if (settings.CustomOffsetLeftMM < 0 || settings.CustomOffsetLeftMM > 100)
                errors.Add("Sol ofset 0-100 mm arası olmalı");

            return errors;
        }

        // ========================
        //  LEGACY METODLAR
        // ========================
        public AppSettingsSnapshot GetSettings()
        {
            var app = GetAppSettings();
            var snap = new AppSettingsSnapshot
            {
                DefaultGonderen = app.DefaultGonderen,
                PdfOutputPath = app.PdfOutputPath,
                PdfExternalPath = app.PdfExternalPath,
                DefaultPaperSize = app.DefaultPaperSize,
                LabelsPerPage = app.LabelsPerPage,
                UseCustomOffset = app.UseCustomOffset,
                CustomOffsetTopMM = app.CustomOffsetTopMM,
                CustomOffsetLeftMM = app.CustomOffsetLeftMM,
                LabelWidthMM = app.LabelWidthMM,
                LabelHeightMM = app.LabelHeightMM,
                UseAdvancedLayout = app.UseAdvancedLayout,
                PreferredColumns = app.PreferredColumns,
                PreferredRows = app.PreferredRows,
                ForceSingleLabelPerPage = app.ForceSingleLabelPerPage,
                PrimaryFontFamily = app.PrimaryFontFamily,
                SecondaryFontFamily = app.SecondaryFontFamily,
                SenderTextColor = app.SenderTextColor,
                ReceiverTextColor = app.ReceiverTextColor,
                AddressTextColor = app.AddressTextColor,
                UseEnhancedLabelDesign = app.UseEnhancedLabelDesign,
                SenderLabelStyle = app.SenderLabelStyle,
                ReceiverLabelStyle = app.ReceiverLabelStyle,
                AddressLabelStyle = app.AddressLabelStyle,
                BarcodePrefix = app.BarcodePrefix,
                BarcodeLength = app.BarcodeDigitCount,
                DefaultBarcodeSize = app.DefaultBarcodeSize,
                ShowBarcodeText = app.ShowBarcodeText,
                BarcodeHeightSmall = app.BarcodeHeightSmall,
                BarcodeHeightMedium = app.BarcodeHeightMedium,
                BarcodeHeightLarge = app.BarcodeHeightLarge,
                BarcodeHeightXLarge = app.BarcodeHeightXLarge
            };

            var ls = app.LabelSettings ?? new LabelSettings();
            snap.PaperSize = ls.PaperSize;
            snap.OrientationV2 = ls.Orientation;
            snap.LabelTemplate = ls.Template;
            snap.TopMarginMm = ls.TopMarginMm;
            snap.LeftMarginMm = ls.LeftMarginMm;
            snap.LabelWidthMmV2 = ls.LabelWidthMm;
            snap.LabelHeightMmV2 = ls.LabelHeightMm;
            snap.Columns = ls.Columns;
            snap.Rows = ls.Rows;
            snap.HorizontalGapMm = ls.HorizontalGapMm;
            snap.VerticalGapMm = ls.VerticalGapMm;
            snap.LabelWidthMm = (int)Math.Round(ls.LabelWidthMm);
            snap.LabelHeightMm = (int)Math.Round(ls.LabelHeightMm);

            return snap;
        }

        public async Task SaveSettingsAsync(AppSettingsSnapshot snapshot)
        {
            var settings = GetAppSettings();
            var label = settings.LabelSettings ?? new LabelSettings();

            settings.DefaultGonderen = snapshot.DefaultGonderen ?? "Taşra Belediyesi";
            settings.PdfExternalPath = snapshot.PdfExternalPath ?? "";
            settings.PdfOutputPath = snapshot.PdfOutputPath ?? @"D:\TasraPostaManagerOutput";
            settings.DefaultPaperSize = snapshot.DefaultPaperSize ?? "A4";
            settings.LabelsPerPage = snapshot.PreferredLabelsPerPage ?? snapshot.LabelsPerPage;

            settings.UseCustomOffset = snapshot.UseCustomOffset;
            settings.CustomOffsetTopMM = snapshot.CustomOffsetTopMM;
            settings.CustomOffsetLeftMM = snapshot.CustomOffsetLeftMM;
            settings.LabelWidthMM = snapshot.LabelWidthMM;
            settings.LabelHeightMM = snapshot.LabelHeightMM;
            settings.UseAdvancedLayout = snapshot.UseAdvancedLayout;
            settings.PreferredColumns = snapshot.PreferredColumns;
            settings.PreferredRows = snapshot.PreferredRows;
            settings.ForceSingleLabelPerPage = snapshot.ForceSingleLabelPerPage;

            settings.PrimaryFontFamily = snapshot.PrimaryFontFamily ?? "Arial";
            settings.SecondaryFontFamily = snapshot.SecondaryFontFamily ?? "Courier New";
            settings.SenderTextColor = snapshot.SenderTextColor ?? "#000000";
            settings.ReceiverTextColor = snapshot.ReceiverTextColor ?? "#000000";
            settings.AddressTextColor = snapshot.AddressTextColor ?? "#666666";

            settings.UseEnhancedLabelDesign = snapshot.UseEnhancedLabelDesign;
            settings.SenderLabelStyle = snapshot.SenderLabelStyle ?? "BOLD_VIBRANT";
            settings.ReceiverLabelStyle = snapshot.ReceiverLabelStyle ?? "BOLD_HIGHLIGHT";
            settings.AddressLabelStyle = snapshot.AddressLabelStyle ?? "READABLE_PROFESSIONAL";

            settings.BarcodePrefix = snapshot.BarcodePrefix ?? "";
            settings.BarcodeDigitCount = snapshot.BarcodeLength;
            settings.DefaultBarcodeSize = snapshot.DefaultBarcodeSize ?? "medium";
            settings.ShowBarcodeText = snapshot.ShowBarcodeText;
            settings.BarcodeHeightSmall = snapshot.BarcodeHeightSmall;
            settings.BarcodeHeightMedium = snapshot.BarcodeHeightMedium;
            settings.BarcodeHeightLarge = snapshot.BarcodeHeightLarge;
            settings.BarcodeHeightXLarge = snapshot.BarcodeHeightXLarge;

            label.PaperSize = snapshot.PaperSize;
            label.Orientation = snapshot.OrientationV2;
            label.Template = snapshot.LabelTemplate;

            if (snapshot.TopMarginMm > 0) label.TopMarginMm = snapshot.TopMarginMm;
            if (snapshot.LeftMarginMm > 0) label.LeftMarginMm = snapshot.LeftMarginMm;
            if (snapshot.LabelWidthMmV2 > 0) label.LabelWidthMm = snapshot.LabelWidthMmV2;
            if (snapshot.LabelHeightMmV2 > 0) label.LabelHeightMm = snapshot.LabelHeightMmV2;
            if (snapshot.Columns > 0) label.Columns = snapshot.Columns;
            if (snapshot.Rows > 0) label.Rows = snapshot.Rows;
            if (snapshot.HorizontalGapMm >= 0) label.HorizontalGapMm = snapshot.HorizontalGapMm;
            if (snapshot.VerticalGapMm >= 0) label.VerticalGapMm = snapshot.VerticalGapMm;

            settings.LabelSettings = label;
            await SaveSettingsAsync(settings);
        }

        // ========================
        //  EKSİK METODLAR
        // ========================
        public string GetDefaultGonderen()
        {
            return GetSetting("DefaultGonderen", "Taşra Belediyesi");
        }

        public void SetDefaultGonderen(string gonderen)
        {
            try
            {
                var value = string.IsNullOrWhiteSpace(gonderen) ? "Taşra Belediyesi" : gonderen.Trim();
                SetSetting("DefaultGonderen", value);
                _db.SaveChanges();
                _logger.LogInformation("Varsayılan Gönderen güncellendi: {Value}", value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DefaultGonderen kaydedilirken hata oluştu");
                throw;
            }
        }

        public async Task ValidateAndSaveSettingsAsync(AppSettings settings)
        {
            var validationResults = ValidateSettings(settings);
            if (validationResults.Any())
                throw new ValidationException($"Ayarlar geçersiz: {string.Join(", ", validationResults)}");

            await SaveSettingsAsync(settings);
        }

        // ========================
        //  ASYNC METODLAR
        // ========================
        public async Task<AppSettingsSnapshot> GetSettingsAsync()
        {
            return await Task.FromResult(GetSettings());
        }

        public async Task<string> GetDefaultGonderenAsync()
        {
            return await Task.FromResult(GetDefaultGonderen());
        }

        public async Task SetDefaultGonderenAsync(string gonderen)
        {
            SetSetting("DefaultGonderen", gonderen);
            await _db.SaveChangesAsync();
        }

        // ========================
        //  YARDIMCI METODLAR
        // ========================
        public async Task EnsureDefaultSettingsAsync()
        {
            var defaultSettings = new Dictionary<string, string>
            {
                ["PdfOutputPath"] = @"D:\TasraPostaManagerOutput",
                ["DefaultGonderen"] = "Taşra Belediyesi",
                ["DefaultPaperSize"] = "A4",
                ["LabelsPerPage"] = "14",
                ["BarcodeStartNumber"] = "78765000000",
                ["BarcodeDigitCount"] = "11",
                ["BarcodeQuantity"] = "1000000",
                ["UseCustomOffset"] = "false",
                ["CustomOffsetTopMM"] = "14.0",
                ["CustomOffsetLeftMM"] = "5.0",
                ["LabelWidthMM"] = "100.0",
                ["LabelHeightMM"] = "38.0",
                ["UseAdvancedLayout"] = "false",
                ["ForceSingleLabelPerPage"] = "false",
                ["PrimaryFontFamily"] = "Arial",
                ["SecondaryFontFamily"] = "Courier New",
                ["SenderTextColor"] = "#000000",
                ["ReceiverTextColor"] = "#000000",
                ["AddressTextColor"] = "#666666",
                ["UseEnhancedLabelDesign"] = "true",
                ["SenderLabelStyle"] = "BOLD_VIBRANT",
                ["ReceiverLabelStyle"] = "BOLD_HIGHLIGHT",
                ["AddressLabelStyle"] = "READABLE_PROFESSIONAL",
                ["BarcodePrefix"] = "RR",
                ["BarcodeSuffix"] = "",
                ["BarcodeCurrentIndex"] = "0",
                ["DefaultBarcodeSize"] = "medium",
                ["ShowBarcodeText"] = "true",
                ["BarcodeHeightSmall"] = "12",
                ["BarcodeHeightMedium"] = "20",
                ["BarcodeHeightLarge"] = "30",
                ["BarcodeHeightXLarge"] = "40",

                // ✅ Pool switch defaults
                ["DefaultBarcodeMode"] = "Legacy",
                ["BarcodePoolLowThreshold"] = "5000",

                // 🔹 v2 label defaults
                ["Label.PaperSize"] = "A4",
                ["Label.Orientation"] = "Portrait",
                ["Label.Template"] = "A4_3x7_70x38",
                ["Label.TopMarginMm"] = "14",
                ["Label.LeftMarginMm"] = "5",
                ["Label.RightMarginMm"] = "5",
                ["Label.BottomMarginMm"] = "5",
                ["Label.LabelWidthMm"] = "70",
                ["Label.LabelHeightMm"] = "38",
                ["Label.Columns"] = "3",
                ["Label.Rows"] = "7",
                ["Label.HorizontalGapMm"] = "2",
                ["Label.VerticalGapMm"] = "0",
                ["Label.FontSize"] = "10",
                ["Label.IncludeBarcode"] = "true",
                ["Label.BarcodeSize"] = "medium",
                ["Label.DrawLabelBorders"] = "false",
                ["Label.BarcodeEmphasis"] = "1",
                ["Label.ShowMuhabereNo"] = "true",
                // [FIX]: Muhabere kutu default değeri
                ["Label.ShowMuhabereBox"] = "false",

                ["Label.ShowGittigiYer"] = "true",
                ["Label.ShowGonderenBilgisi"] = "true",
                ["Label.ShowMiktar"] = "true",
                ["Label.ShowBarkodNo"] = "true",
                ["Label.ShowTarih"] = "false",
                ["Label.CustomPageWidthMm"] = "210.0",
                ["Label.CustomPageHeightMm"] = "297.0",

                // 🔹 v2 LIST defaults
                ["Label.ListShowRowNumber"] = "true",
                ["Label.ListShowBarcode"] = "true",
                ["Label.ListShowReceiver"] = "true",
                ["Label.ListShowAmount"] = "true",
                ["Label.ListShowDate"] = "true",
                ["Label.ListShowSignature"] = "true",
                ["Label.ListFontSize"] = "9"
            };

            foreach (var (key, value) in defaultSettings)
            {
                if (!await _db.AppSettings.AnyAsync(x => x.Key == key))
                {
                    _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
                    _logger.LogInformation("Varsayılan ayar eklendi: {Key} = {Value}", key, value);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Varsayılan ayarlar kontrolü tamamlandı");
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                var setting = _db.AppSettings.AsNoTracking().FirstOrDefault(x => x.Key == key);
                return setting?.Value ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ayar okunurken hata: {Key}, varsayılan değer kullanılıyor", key);
                return defaultValue;
            }
        }

        public void SetSetting(string key, string value)
        {
            try
            {
                var setting = _db.AppSettings.FirstOrDefault(x => x.Key == key);
                if (setting == null)
                {
                    setting = new AppSetting { Key = key, Value = value };
                    _db.AppSettings.Add(setting);
                }
                else
                {
                    setting.Value = value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayar kaydedilirken hata: {Key} = {Value}", key, value);
                throw;
            }
        }

        // --- Yardımcı Parserlar ---
        private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return Enum.TryParse<TEnum>(value, out var result) ? result : defaultValue;
        }

        private static double ParseDouble(string value, double defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : defaultValue;
        }

        private static int ParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return int.TryParse(value, out var i) ? i : defaultValue;
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return bool.TryParse(value, out var b) ? b : defaultValue;
        }

        private int? GetNullableInt(string value) => string.IsNullOrWhiteSpace(value) ? null : int.Parse(value);

        private AppSettings CreateDefaultAppSettings()
        {
            return new AppSettings
            {
                PdfOutputPath = @"D:\TasraPostaManagerOutput",
                PdfExternalPath = "",
                DefaultGonderen = "Taşra Belediyesi",
                DefaultPaperSize = "A4",
                LabelsPerPage = 14,
                LabelSettings = new LabelSettings()
            };
        }

        /// <summary>
        /// Ayar cache'ini temizler (ayar değişikliklerinde çağrılır).
        /// </summary>
        private void InvalidateSettingsCache()
        {
            _cache?.Remove(CacheKeyAppSettings);
            _cache?.Remove(CacheKeyStats);
            _cache?.RemoveByPrefix("AppSettings:");
        }
    }
}