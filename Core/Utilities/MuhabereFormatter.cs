using System.Globalization;
using System.Text.RegularExpressions;

namespace TasraPostaManager.Core.Utilities;

/// <summary>
/// MuhabereNo formatlama, genişletme ve range oluşturma işlemleri.
/// Daha önce PostaRecord, LabelsController ve ExcelImporter'da tekrarlanan
/// mantık burada merkezileştirildi.
/// </summary>
public static class MuhabereFormatter
{
    /// <summary>
    /// Ardışık sayılar için range modeli.
    /// </summary>
    public sealed record NumberRange(long Start, long End)
    {
        public override string ToString() => Start == End ? Start.ToString() : $"{Start}-{End}";
    }

    /// <summary>
    /// Excel'den gelen MuhabereNo'yu tek tek muhabere string'lerine genişletir.
    /// Örn: "2025/35706-35709,35711" → ["2025/35706", "2025/35707", "2025/35708", "2025/35709", "2025/35711"]
    /// </summary>
    public static List<string> ExpandMuhabereNos(string rawMuhabereNo)
    {
        if (string.IsNullOrWhiteSpace(rawMuhabereNo))
            return new List<string>();

        var result = new List<string>();
        var trimmed = rawMuhabereNo.Trim();

        // "2025/" gibi yıl prefix'i var mı?
        string prefix = "";
        string numberPart = trimmed;

        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex > 0)
        {
            prefix = trimmed[..(slashIndex + 1)]; // "2025/"
            numberPart = trimmed[(slashIndex + 1)..];
        }

        // virgül ile ayrılmış parçalar
        var segments = numberPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var seg in segments)
        {
            // Range: "35706-35709"
            var dashMatch = Regex.Match(seg, @"^(\d+)\s*-\s*(\d+)$");
            if (dashMatch.Success)
            {
                if (long.TryParse(dashMatch.Groups[1].Value, out var start) &&
                    long.TryParse(dashMatch.Groups[2].Value, out var end))
                {
                    if (end < start) (start, end) = (end, start);

                    // Makul bir aralık kontrolü (10.000'den fazla genişletme yapma)
                    var count = end - start + 1;
                    if (count > 10_000) count = 10_000;

                    for (var i = start; i <= start + count - 1; i++)
                    {
                        result.Add($"{prefix}{i}");
                    }
                }
            }
            // Tek sayı: "35711"
            else if (long.TryParse(seg.Trim(), out _))
            {
                result.Add($"{prefix}{seg.Trim()}");
            }
            // Ham değer (parse edilemeyen)
            else
            {
                result.Add($"{prefix}{seg.Trim()}");
            }
        }

        return result.Count > 0 ? result : new List<string> { trimmed };
    }

    /// <summary>
    /// Tek bir MuhabereNo string'i içindeki (ör. "2025/35706,35707,35708")
    /// ardışık sayıları range formatına çevirir (ör. "2025/35706-35708").
    /// </summary>
    public static string FormatSingleMuhabereNo(string muhabereNo)
    {
        if (string.IsNullOrWhiteSpace(muhabereNo))
            return muhabereNo ?? "";

        var trimmed = muhabereNo.Trim();
        var slashIndex = trimmed.IndexOf('/');

        if (slashIndex < 0) return trimmed;

        var prefix = trimmed[..(slashIndex + 1)];
        var numberPart = trimmed[(slashIndex + 1)..];

        var numbers = ParseNumbersFromString(numberPart);
        if (numbers.Count == 0) return trimmed;

        var ranges = FindConsecutiveRanges(numbers);
        return $"{prefix}{FormatRanges(ranges)}";
    }

    /// <summary>
    /// Birden fazla MuhabereNo listesini yıl bazında gruplayıp
    /// "2025/35706-35709,35711 | 2026/1-3" gibi tek bir string'e çevirir.
    /// </summary>
    public static string FormatMuhabereRange(IEnumerable<string> muhabereNos)
    {
        if (muhabereNos == null) return "";

        var list = muhabereNos.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        if (list.Count == 0) return "";
        if (list.Count == 1) return FormatSingleMuhabereNo(list[0]);

        // Yıl bazında grupla
        var yearGroups = new Dictionary<string, List<long>>();
        var fallbackItems = new List<string>();

        foreach (var muh in list)
        {
            var trimmed = muh.Trim();
            var slashIdx = trimmed.IndexOf('/');

            if (slashIdx > 0)
            {
                var yearPrefix = trimmed[..slashIdx];
                var numPart = trimmed[(slashIdx + 1)..];

                if (long.TryParse(numPart, out var num))
                {
                    if (!yearGroups.ContainsKey(yearPrefix))
                        yearGroups[yearPrefix] = new List<long>();

                    yearGroups[yearPrefix].Add(num);
                }
                else
                {
                    fallbackItems.Add(trimmed);
                }
            }
            else
            {
                fallbackItems.Add(trimmed);
            }
        }

        var parts = new List<string>();

        foreach (var (year, numbers) in yearGroups.OrderBy(g => g.Key))
        {
            var sorted = numbers.Distinct().OrderBy(n => n).ToList();
            var ranges = FindConsecutiveRanges(sorted);
            parts.Add($"{year}/{FormatRanges(ranges)}");
        }

        parts.AddRange(fallbackItems);
        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Ardışık sayı gruplarını bulur.
    /// </summary>
    public static List<NumberRange> FindConsecutiveRanges(IEnumerable<long> numbers)
    {
        var sorted = numbers.Distinct().OrderBy(n => n).ToList();
        var ranges = new List<NumberRange>();

        if (sorted.Count == 0) return ranges;

        var start = sorted[0];
        var end = sorted[0];

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == end + 1)
            {
                end = sorted[i];
            }
            else
            {
                ranges.Add(new NumberRange(start, end));
                start = sorted[i];
                end = sorted[i];
            }
        }

        ranges.Add(new NumberRange(start, end));
        return ranges;
    }

    // --- Private helpers ---

    private static List<long> ParseNumbersFromString(string numberStr)
    {
        var numbers = new List<long>();
        var parts = numberStr.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (long.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                numbers.Add(num);
        }

        return numbers;
    }

    private static string FormatRanges(List<NumberRange> ranges)
    {
        return string.Join(",", ranges.Select(r => r.ToString()));
    }
}
