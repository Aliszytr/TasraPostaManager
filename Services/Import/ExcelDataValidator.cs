using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services.Import
{
    /// <summary>
    /// Excel veri satırlarının doğrulanması, dönüştürülmesi ve MuhabereNo genişletme mantığı.
    /// Saf (pure) iş mantığı — DB bağımlılığı yok, kolayca test edilebilir.
    /// </summary>
    public static class ExcelDataValidator
    {
        // ═══════════════════════════════════════
        //  MUHABERENO EXPANSION
        // ═══════════════════════════════════════

        /// <summary>
        /// Excel'den gelen MuhabereNo'yu tek tek muhabere string'lerine genişletir.
        /// Örn: 2025/35706-35709,35711 → [2025/35706, 2025/35707, 2025/35708, 2025/35709, 2025/35711]
        /// </summary>
        public static List<string> ExpandMuhabereNos(string muhabereNo)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(muhabereNo))
                return result;

            var trimmed = muhabereNo.Trim();
            var parts = trimmed.Split('/');
            if (parts.Length != 2)
            {
                result.Add(trimmed);
                return result;
            }

            var yearPart = parts[0].Trim();
            var numbersPart = parts[1].Trim();

            if (string.IsNullOrEmpty(yearPart) || string.IsNullOrEmpty(numbersPart))
            {
                result.Add(trimmed);
                return result;
            }

            var tokens = numbersPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var tokenRaw in tokens)
            {
                var token = tokenRaw.Trim();
                if (string.IsNullOrEmpty(token))
                    continue;

                if (token.Contains('-'))
                {
                    var rangeParts = token.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out var start) &&
                        int.TryParse(rangeParts[1].Trim(), out var end))
                    {
                        if (end < start) (start, end) = (end, start);

                        for (int i = start; i <= end; i++)
                            result.Add($"{yearPart}/{i}");
                    }
                    else
                    {
                        if (int.TryParse(token.Replace("-", "").Trim(), out var single))
                            result.Add($"{yearPart}/{single}");
                        else
                            result.Add(trimmed);
                    }
                }
                else
                {
                    if (int.TryParse(token, out var num))
                        result.Add($"{yearPart}/{num}");
                    else
                        result.Add($"{yearPart}/{token}");
                }
            }

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Excel'den gelen ham MuhabereNo'yu formatlar.
        /// </summary>
        public static string FormatMuhabereNoFromExcel(string muhabereNo)
        {
            if (string.IsNullOrWhiteSpace(muhabereNo))
                return string.Empty;

            try
            {
                var parts = muhabereNo.Split('/');
                if (parts.Length != 2)
                    return muhabereNo.Trim();

                var yearPart = parts[0];
                var numbersPart = parts[1];

                var numbers = new List<int>();

                if (!string.IsNullOrWhiteSpace(numbersPart))
                {
                    numbers = numbersPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim())
                        .Where(n => int.TryParse(n, out _))
                        .Select(n => int.Parse(n))
                        .OrderBy(n => n)
                        .ToList();
                }

                if (numbers.Count == 0)
                    return muhabereNo.Trim();

                if (numbers.Count == 1)
                    return $"{yearPart}/{numbers[0]}";

                var ranges = FindConsecutiveRanges(numbers);
                return $"{yearPart}/{string.Join(",", ranges.Select(r => r.ToString()))}";
            }
            catch
            {
                return muhabereNo.Trim();
            }
        }

        // ═══════════════════════════════════════
        //  ROW FIELD EXTRACTION
        // ═══════════════════════════════════════

        /// <summary>
        /// Excel satırından Miktar ve ListeTipi değerlerini parse eder.
        /// </summary>
        public static (decimal? Miktar, ListeTipi ListeTipi) ParseMiktar(IXLRow row, int miktarColumnIndex)
        {
            if (miktarColumnIndex <= 0 || miktarColumnIndex > 16384)
                return (null, ListeTipi.ParaliDegil);

            var miktarCell = row.Cell(miktarColumnIndex);
            if (miktarCell == null)
                return (null, ListeTipi.ParaliDegil);

            var miktarStr = miktarCell.GetString()?.Trim().Replace(",", ".");
            if (decimal.TryParse(miktarStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var miktar))
                return (miktar, miktar > 0 ? ListeTipi.Parali : ListeTipi.ParaliDegil);

            return (null, ListeTipi.ParaliDegil);
        }

        /// <summary>
        /// Excel satırından Tarih değerini parse eder.
        /// </summary>
        public static DateTime? ParseTarih(IXLRow row, int tarihColumnIndex)
        {
            if (tarihColumnIndex <= 0 || tarihColumnIndex > 16384)
                return null;

            var tarihCell = row.Cell(tarihColumnIndex);
            if (tarihCell == null)
                return null;

            return tarihCell.DataType == XLDataType.DateTime
                ? tarihCell.GetDateTime()
                : DateTime.TryParse(tarihCell.GetString(), out var dt) ? dt : null;
        }

        // ═══════════════════════════════════════
        //  DUPLICATE BARCODE EXTRACTION
        // ═══════════════════════════════════════

        /// <summary>
        /// SqlException mesajından duplicate barkod değerini çıkarmaya çalışır.
        /// </summary>
        public static string? TryExtractDuplicateBarcode(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return null;

            // "The duplicate key value is (RR123456789TR)" — parantez içini al
            var startMarker = "The duplicate key value is (";
            var idx = errorMessage.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);

            if (idx < 0)
                return null;

            var startIdx = idx + startMarker.Length;
            var endIdx = errorMessage.IndexOf(')', startIdx);
            if (endIdx < 0 || endIdx <= startIdx)
                return null;

            var duplicateValue = errorMessage[startIdx..endIdx].Trim();
            return string.IsNullOrWhiteSpace(duplicateValue) ? null : duplicateValue;
        }

        // ═══════════════════════════════════════
        //  CONSECUTIVE RANGE HELPER
        // ═══════════════════════════════════════

        public static List<NumberRange> FindConsecutiveRanges(List<int> numbers)
        {
            var ranges = new List<NumberRange>();
            if (numbers == null || numbers.Count == 0)
                return ranges;

            var sorted = numbers.OrderBy(x => x).ToList();
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

        public record NumberRange(int Start, int End)
        {
            public override string ToString()
                => Start == End ? Start.ToString() : $"{Start}-{End}";
        }
    }
}
