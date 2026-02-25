using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.Extensions.Logging;
using TasraPostaManager.Services;

namespace TasraPostaManager.Models
{
    public enum ListeTipi
    {
        ParaliDegil = 0,
        Parali = 1
    }

    public partial class PostaRecord
    {
        #region Properties

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Muhabere numarası zorunludur")]
        [StringLength(50, ErrorMessage = "Muhabere numarası 50 karakteri geçemez")]
        [Display(Name = "Muhabere No")]
        public string MuhabereNo { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Gittiği yer 200 karakteri geçemez")]
        [Display(Name = "Gittiği Yer")]
        public string? GittigiYer { get; set; }

        [StringLength(100, ErrorMessage = "Durum 100 karakteri geçemez")]
        [Display(Name = "Durum")]
        public string? Durum { get; set; }

        [Display(Name = "Liste Tipi")]
        public ListeTipi ListeTipi { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Miktar 0-999999.99 arası olmalı")]
        [Display(Name = "Miktar")]
        public decimal? Miktar { get; set; }

        [StringLength(500, ErrorMessage = "Adres 500 karakteri geçemez")]
        [Display(Name = "Adres")]
        public string? Adres { get; set; }

        [Display(Name = "Tarih")]
        public DateTime? Tarih { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        [StringLength(50, ErrorMessage = "Barkod no 50 karakteri geçemez")]
        [Display(Name = "Barkod No")]
        public string? BarkodNo { get; set; }

        [StringLength(200, ErrorMessage = "Gönderen 200 karakteri geçemez")]
        [Display(Name = "Gönderen")]
        public string? Gonderen { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Etiket Oluşturuldu")]
        public bool IsLabelGenerated { get; set; }

        #endregion

        #region Constructors

        public PostaRecord() { }

        // Factory method for creating new records with consistent data
        public static PostaRecord CreateNew(
            string muhabereNo,
            string? gittigiYer = null,
            decimal? miktar = null,
            IAppSettingsService? settingsService = null,
            ILogger<PostaRecord>? logger = null)
        {
            var record = new PostaRecord
            {
                MuhabereNo = muhabereNo ?? throw new ArgumentNullException(nameof(muhabereNo)),
                GittigiYer = gittigiYer,
                Miktar = miktar,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            record.EnsureConsistentData(settingsService, logger);
            return record;
        }

        #endregion

        #region Business Logic Methods

        /// <summary>
        /// Dinamik gönderen bilgisi - AppSettingsService ile entegre
        /// </summary>
        public string GetGonderenWithDefault(IAppSettingsService? settingsService = null)
        {
            if (!string.IsNullOrEmpty(Gonderen))
                return Gonderen;

            // SettingsService varsa ondan al, yoksa varsayılan değer
            try
            {
                return settingsService?.GetDefaultGonderen() ?? "Taşra Belediyesi";
            }
            catch (Exception)
            {
                return "Taşra Belediyesi";
            }
        }

        /// <summary>
        /// Barkod atama metodu - thread-safe ve optimized
        /// </summary>
        public void AssignBarkod(IAppSettingsService? settingsService = null, ILogger<PostaRecord>? logger = null)
        {
            if (!string.IsNullOrEmpty(BarkodNo))
                return;

            // ✅ TEK kaynak: IAppSettingsService.AllocateBarcodeAsync()
            // Pool modunda sessiz fallback YOK. Havuz boşsa / erişilemiyorsa exception fırlar.
            // (Bu davranış istenen şekilde import/etiket akışını zorlar.)
            if (settingsService != null)
            {
                try
                {
                    BarkodNo = settingsService.AllocateBarcodeAsync(MuhabereNo).GetAwaiter().GetResult();
                    return;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Barkod atama hatası (AllocateBarcodeAsync) - MuhabereNo: {MuhabereNo}", MuhabereNo);
                    throw;
                }
            }

            // DI verilmemişse (çok nadir) - en azından uygulama tamamen kırılmasın
            BarkodNo = GenerateFallbackBarcode();
        }

        /// <summary>
        /// Fallback barkod üretimi (thread-safe)
        /// </summary>
        private static string GenerateFallbackBarcode()
        {
            var random = new Random();
            lock (random)
            {
                var numbers = random.Next(100000000, 999999999).ToString("D9");
                return $"TP{numbers}00";
            }
        }

        // Not: Önceki sürümde ServiceScope ile IBarcodeService çözülmeye çalışılıyordu.
        // Bu yaklaşım hem DI karmaşası yaratıyor hem de Pool modunu bypass edebiliyordu.
        // Artık barkod üretimi tek noktadan (IAppSettingsService) yönetildiği için scope ihtiyacı yok.

        #endregion

        #region UI Helper Methods

        public string GetStatusBadgeClass()
        {
            if (string.IsNullOrEmpty(Durum))
                return "bg-secondary";

            return Durum.ToLowerInvariant() switch
            {
                "teslim edildi" => "bg-success",
                "reddiyatı yapılmamış" => "bg-warning",
                "iade edildi" => "bg-danger",
                "yolda" => "bg-info",
                _ => "bg-secondary"
            };
        }

        public string GetTypeBadgeClass()
        {
            return ListeTipi switch
            {
                ListeTipi.Parali => "bg-danger",      // Kırmızı - Paralı
                ListeTipi.ParaliDegil => "bg-success", // Yeşil - Ücretsiz
                _ => "bg-secondary"
            };
        }

        public string GetTypeDisplayName()
        {
            return ListeTipi switch
            {
                ListeTipi.Parali => "Paralı",
                ListeTipi.ParaliDegil => "Ücretsiz",
                _ => ListeTipi.ToString()
            };
        }

        public string GetFormattedMiktar()
        {
            return Miktar.HasValue ? $"{Miktar.Value:0.##} ₺" : "-";
        }

        public string GetFormattedTarih()
        {
            return Tarih.HasValue ? Tarih.Value.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture) : "-";
        }

        #endregion

        #region Data Consistency Methods

        /// <summary>
        /// Miktar değiştiğinde ListeTipi'ni otomatik güncelle
        /// </summary>
        public void UpdateListeTipiFromMiktar()
        {
            ListeTipi = (Miktar.HasValue && Miktar > 0)
                ? ListeTipi.Parali
                : ListeTipi.ParaliDegil;
        }

        /// <summary>
        /// Gelişmiş veri tutarlılığı metodu
        /// </summary>
        public void EnsureConsistentData(IAppSettingsService? settingsService = null, ILogger<PostaRecord>? logger = null)
        {
            // Barkod yoksa oluştur
            AssignBarkod(settingsService, logger);

            // Miktar ve ListeTipi tutarlılığını sağla
            UpdateListeTipiFromMiktar();

            // Gonderen bilgisi yoksa default ata
            if (string.IsNullOrEmpty(Gonderen))
            {
                Gonderen = GetGonderenWithDefault(settingsService);
            }

            // UpdatedAt'i güncelle
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Formatting Methods

        /// <summary>
        /// Tek bir MuhabereNo içinde (ör. 2025/35706,35707,35708) range formatı uygular.
        /// </summary>
        public string GetFormattedMuhabereNo()
        {
            if (string.IsNullOrEmpty(MuhabereNo))
                return string.Empty;

            try
            {
                var parts = MuhabereNo.Split('/', 2);
                if (parts.Length != 2)
                    return MuhabereNo;

                var yearPart = parts[0];
                var numbersPart = parts[1];

                var numbers = ParseNumbersFromString(numbersPart);
                if (numbers.Count == 0)
                    return MuhabereNo;

                var ranges = FindConsecutiveRanges(numbers);
                return FormatRanges(yearPart, ranges);
            }
            catch (Exception)
            {
                return MuhabereNo;
            }
        }

        /// <summary>
        /// Bir zarfa ait birden fazla MuhabereNo listesini
        /// yıl bazında gruplayıp 2025/35706-35709,35711 | 2026/1-3 gibi
        /// tek bir range string'ine çevirir.
        /// </summary>
        public static string FormatMuhabereRange(List<string> muhabereList)
        {
            if (muhabereList == null || !muhabereList.Any())
                return string.Empty;

            var parsed = new List<(string Year, int Number)>();
            var nonFormatted = new List<string>();

            foreach (var m in muhabereList)
            {
                if (string.IsNullOrWhiteSpace(m))
                    continue;

                var parts = m.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[1], out var num))
                {
                    parsed.Add((parts[0], num));
                }
                else
                {
                    // Format dışı olan her şeyi (örn: "2025/ABC" veya "ABC") buraya at
                    nonFormatted.Add(m);
                }
            }

            if (!parsed.Any())
            {
                // Hiç parse edilebilen yoksa orijinal listeyi virgülle döndür
                return string.Join(", ", muhabereList);
            }

            var resultParts = new List<string>();

            // Yıl bazında grupla
            foreach (var yearGroup in parsed
                         .GroupBy(p => p.Year)
                         .OrderBy(g => g.Key))
            {
                var year = yearGroup.Key;
                var numbers = yearGroup
                    .Select(x => x.Number)
                    .OrderBy(x => x)
                    .Distinct()
                    .ToList();

                var ranges = new List<string>();

                for (int i = 0; i < numbers.Count; i++)
                {
                    int start = numbers[i];
                    while (i + 1 < numbers.Count && numbers[i + 1] == numbers[i] + 1)
                    {
                        i++;
                    }
                    int end = numbers[i];

                    if (start == end)
                        ranges.Add(start.ToString());
                    else
                        ranges.Add($"{start}-{end}");
                }

                resultParts.Add($"{year}/{string.Join(",", ranges)}");
            }

            if (nonFormatted.Any())
            {
                resultParts.Add(string.Join(", ", nonFormatted));
            }

            // Birden fazla yıl varsa " | " ile ayır
            return string.Join(" | ", resultParts);
        }

        private static List<int> ParseNumbersFromString(string numbersPart)
        {
            return numbersPart.Split(',')
                .Select(n => n.Trim())
                .Select(n => int.TryParse(n, out int num) ? num : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .OrderBy(n => n)
                .ToList();
        }

        private static string FormatRanges(string yearPart, List<NumberRange> ranges)
        {
            var formattedNumbers = ranges.Select(range =>
                range.Start == range.End
                    ? range.Start.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : $"{range.Start}-{range.End}");

            return $"{yearPart}/{string.Join(",", formattedNumbers)}";
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Barkod validasyon metodu
        /// </summary>
        public bool ValidateBarcode(BarcodeConfig? config = null)
        {
            if (string.IsNullOrEmpty(BarkodNo))
                return false;

            // Basit uzunluk kontrolü
            if (BarkodNo.Length < 8 || BarkodNo.Length > 50)
                return false;

            // Config varsa advanced validasyon
            if (config != null)
            {
                // Prefix kontrolü
                if (!string.IsNullOrEmpty(config.Prefix) &&
                    !BarkodNo.StartsWith(config.Prefix, StringComparison.Ordinal))
                    return false;

                // Suffix kontrolü
                if (!string.IsNullOrEmpty(config.Suffix) &&
                    !BarkodNo.EndsWith(config.Suffix, StringComparison.Ordinal))
                    return false;

                // Numeric kısım kontrolü
                var numericPart = ExtractNumericPart(BarkodNo, config);
                if (!IsValidNumericPart(numericPart, config))
                    return false;
            }

            return true;
        }

        private static string ExtractNumericPart(string barcode, BarcodeConfig config)
        {
            var startIndex = config.Prefix?.Length ?? 0;
            var length = barcode.Length - startIndex - (config.Suffix?.Length ?? 0);

            return length > 0
                ? barcode.Substring(startIndex, length)
                : string.Empty;
        }

        private static bool IsValidNumericPart(string numericPart, BarcodeConfig config)
        {
            return numericPart.Length == config.DigitCount &&
                   long.TryParse(numericPart, System.Globalization.NumberStyles.None,
                       System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        /// <summary>
        /// Barkod bilgilerini parse etme
        /// </summary>
        public (string Prefix, string NumericPart, string Suffix) ParseBarcode()
        {
            if (string.IsNullOrEmpty(BarkodNo))
                return (string.Empty, string.Empty, string.Empty);

            // Basit parsing - gerçek implementasyon BarcodeService'te olacak
            var prefix = BarkodNo.Length > 2 ? BarkodNo[..2] : string.Empty;
            var suffix = BarkodNo.Length > 2 ? BarkodNo[^2..] : string.Empty;
            var numericPart = BarkodNo.Length > 4
                ? BarkodNo[2..^2]
                : string.Empty;

            return (prefix, numericPart, suffix);
        }

        /// <summary>
        /// Kayıt için özet bilgi
        /// </summary>
        public string GetRecordSummary()
        {
            return $"{MuhabereNo} - {GittigiYer} - {GetFormattedMiktar()}";
        }

        /// <summary>
        /// Validation metodu - tüm business rule'ları kontrol eder
        /// </summary>
        public List<string> ValidateRecord()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(MuhabereNo))
                errors.Add("Muhabere numarası zorunludur");

            if (string.IsNullOrWhiteSpace(GittigiYer))
                errors.Add("Gittiği yer bilgisi zorunludur");

            if (Miktar.HasValue && Miktar < 0)
                errors.Add("Miktar negatif olamaz");

            if (!string.IsNullOrEmpty(BarkodNo) && !ValidateBarcode())
                errors.Add("Geçersiz barkod formatı");

            // Tarih validasyonu
            if (Tarih.HasValue && Tarih > DateTime.UtcNow.AddDays(1))
                errors.Add("Gelecekteki tarih olamaz");

            return errors;
        }

        /// <summary>
        /// Hızlı validasyon - sadece temel alanlar
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(MuhabereNo) &&
                   !string.IsNullOrWhiteSpace(GittigiYer) &&
                   (!Miktar.HasValue || Miktar >= 0) &&
                   (!Tarih.HasValue || Tarih <= DateTime.UtcNow.AddDays(1));
        }

        #endregion

        #region Utility Methods

        private static List<NumberRange> FindConsecutiveRanges(List<int> numbers)
        {
            var ranges = new List<NumberRange>();

            if (numbers.Count == 0)
                return ranges;

            int start = numbers[0];
            int end = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == end + 1)
                {
                    end = numbers[i];
                }
                else
                {
                    ranges.Add(new NumberRange(start, end));
                    start = numbers[i];
                    end = numbers[i];
                }
            }

            ranges.Add(new NumberRange(start, end));
            return ranges;
        }

        public override string ToString() => GetRecordSummary();

        public PostaRecord Clone()
        {
            return new PostaRecord
            {
                Id = Id,
                MuhabereNo = MuhabereNo,
                GittigiYer = GittigiYer,
                Durum = Durum,
                ListeTipi = ListeTipi,
                Miktar = Miktar,
                Adres = Adres,
                Tarih = Tarih,
                BarkodNo = BarkodNo,
                Gonderen = Gonderen,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                IsLabelGenerated = IsLabelGenerated
            };
        }

        #endregion

        #region Nested Types

        private readonly record struct NumberRange(int Start, int End)
        {
            public override string ToString() => Start == End
                ? Start.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : $"{Start}-{End}";
        }

        #endregion
    }
}
