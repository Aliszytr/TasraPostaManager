using System.Collections.Generic;
using System.Linq;
using TasraPostaManager.Models;

namespace TasraPostaManager.Controllers
{
    // Index sayfası için ana ViewModel
    public class RecordsViewModel
    {
        // Eski kod uyumluluğu için (artık gruplanmış liste esas alınıyor)
        public List<PostaRecord> DbRecords { get; set; } = new();

        // YENİ: Gruplanmış Kayıt Listesi
        public List<PostaGroupViewModel> GroupedRecords { get; set; } = new();
    }

    // Gruplanmış (Zarf) Kayıt Modeli
    public class PostaGroupViewModel
    {
        // BarkodNo veya UniqueID
        public string GroupId { get; set; } = string.Empty;

        // Örn: 2025/35706-35709
        public string DisplayMuhabereNo { get; set; } = string.Empty;

        public List<string> AllMuhabereNos { get; set; } = new();

        // Tabloda görünecek ana kayıt (Genelde grubun ilk kaydı)
        public PostaRecord MainRecord { get; set; } = new();

        // Zarfın içindeki toplam dosya sayısı
        public int Count { get; set; }

        // Toplam tutar (varsa)
        public decimal TotalAmount { get; set; }

        // Zarfın içindeki tüm kayıtlar
        public List<PostaRecord> SubRecords { get; set; } = new();

        // Muhabere Numaralarını (2025/1-5 gibi) formatlayan yardımcı metot
        public static string FormatMuhabereRange(List<string> muhabereList)
        {
            if (muhabereList == null || !muhabereList.Any()) return "";

            var byYear = muhabereList
                .Select(m => m.Split('/'))
                .Where(p => p.Length == 2 && int.TryParse(p[1], out _))
                .GroupBy(p => p[0])
                .ToList();

            if (!byYear.Any()) return string.Join(", ", muhabereList);

            var resultParts = new List<string>();

            foreach (var yearGroup in byYear)
            {
                var year = yearGroup.Key;
                var numbers = yearGroup.Select(x => int.Parse(x[1])).OrderBy(x => x).Distinct().ToList();

                var ranges = new List<string>();
                for (int i = 0; i < numbers.Count; i++)
                {
                    int start = numbers[i];
                    while (i + 1 < numbers.Count && numbers[i + 1] == numbers[i] + 1) i++;
                    int end = numbers[i];

                    if (start == end) ranges.Add(start.ToString());
                    else ranges.Add($"{start}-{end}");
                }
                resultParts.Add($"{year}/{string.Join(",", ranges)}");
            }

            var nonFormatted = muhabereList.Where(m => !m.Contains('/')).ToList();
            if (nonFormatted.Any()) resultParts.Add(string.Join(", ", nonFormatted));

            return string.Join(" | ", resultParts);
        }
    }
}
