using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace TasraPostaManager.Services.Import
{
    /// <summary>
    /// Excel dosyası okuma, başlık eşleme ve hücre değeri çıkarma işlemleri.
    /// ExcelImporter'ın dosya I/O sorumluluğunu üstlenir.
    /// </summary>
    public class ExcelFileReader
    {
        private readonly ILogger<ExcelFileReader> _logger;

        public ExcelFileReader(ILogger<ExcelFileReader> logger)
        {
            _logger = logger;
        }

        // ═══════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════

        /// <summary>
        /// Desteklenen Excel dosya uzantıları kontrolü.
        /// </summary>
        public static bool IsExcelFile(string fileName)
        {
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            return allowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
        }

        /// <summary>
        /// Mapping.json konfigürasyon dosyasını okur ve normalize alias map'i döndürür.
        /// Key: kanonik kolon adı (örn: "MuhabereNo"),
        /// Value: normalize alias seti (örn: {"MUHABERENO", "MUH", ...})
        /// </summary>
        public async Task<Dictionary<string, HashSet<string>>?> LoadMappingConfigurationAsync(string contentRootPath)
        {
            try
            {
                var mapPath = Path.Combine(contentRootPath, "wwwroot", "config", "mapping.json");
                if (!File.Exists(mapPath))
                {
                    _logger.LogError("Mapping.json dosyası bulunamadı: {Path}", mapPath);
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(mapPath);
                using var doc = JsonDocument.Parse(jsonContent);

                var columns = doc.RootElement.GetProperty("Columns");
                var aliases = new Dictionary<string, HashSet<string>>();

                foreach (var property in columns.EnumerateObject())
                {
                    var set = new HashSet<string>();
                    foreach (var value in property.Value.EnumerateArray())
                    {
                        var alias = value.GetString();
                        if (!string.IsNullOrEmpty(alias))
                            set.Add(NormalizeHeaderName(alias));
                    }
                    aliases[property.Name] = set;
                }

                return aliases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mapping konfigürasyonu yüklenirken hata");
                return null;
            }
        }

        /// <summary>
        /// Excel worksheet'inde başlık satırını bulur ve kanonik kolon adları → kolon index map'i döndürür.
        /// </summary>
        public HeaderResult FindHeaderRow(IXLWorksheet worksheet, Dictionary<string, HashSet<string>> aliases)
        {
            var lastRow = worksheet.LastRowUsed().RowNumber();
            var headerRow = -1;
            var bestScore = -1;

            var bestRawMapping = new Dictionary<string, int>();
            var foundHeaders = string.Empty;

            for (int row = 1; row <= Math.Min(10, lastRow); row++)
            {
                var currentRow = worksheet.Row(row);
                var tempMapping = new Dictionary<string, int>();
                var score = 0;
                var column = 1;

                foreach (var cell in currentRow.Cells(1, currentRow.LastCellUsed()?.Address.ColumnNumber ?? 1))
                {
                    var value = NormalizeHeaderName(cell.GetString());
                    if (!string.IsNullOrEmpty(value) && !tempMapping.ContainsKey(value))
                    {
                        tempMapping[value] = column;

                        if (aliases.Values.Any(set => set.Contains(value)))
                            score++;
                    }
                    column++;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    headerRow = row;
                    bestRawMapping = tempMapping;
                    foundHeaders = string.Join(", ", tempMapping.Keys.Select(k => $"'{k}'"));
                }
            }

            // Normalize başlık mapping'ini kanonik isimlere çevir
            var canonicalMapping = new Dictionary<string, int>();

            foreach (var aliasEntry in aliases)
            {
                var canonicalName = aliasEntry.Key;
                var aliasSet = aliasEntry.Value;

                var match = bestRawMapping
                    .FirstOrDefault(x => aliasSet.Contains(x.Key));

                if (!string.IsNullOrEmpty(match.Key))
                {
                    canonicalMapping[canonicalName] = match.Value;
                }
            }

            var hasMuhabereNo = canonicalMapping.ContainsKey("MuhabereNo");

            return new HeaderResult(hasMuhabereNo, headerRow, canonicalMapping, foundHeaders);
        }

        /// <summary>
        /// Hücre değerini güvenli bir şekilde string olarak okur.
        /// </summary>
        public static string? GetCellStringValue(IXLRow row, int columnIndex)
        {
            if (columnIndex <= 0 || columnIndex > 16384)
                return null;

            try
            {
                var cell = row.Cell(columnIndex);
                return cell?.GetString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Kanonik kolon adları → kolon index'lerine dönüştürür.
        /// </summary>
        public static ColumnIndices GetColumnIndices(Dictionary<string, int> columnMapping)
        {
            int FindColumn(string columnName)
                => columnMapping.TryGetValue(columnName, out var index) ? index : -1;

            return new ColumnIndices(
                MuhabereNo: FindColumn("MuhabereNo"),
                GittigiYer: FindColumn("GittigiYer"),
                Durum: FindColumn("Durum"),
                Miktar: FindColumn("Miktar"),
                Adres: FindColumn("Adres"),
                Tarih: FindColumn("Tarih"),
                Barkod: FindColumn("Barkod"),
                IsLabelGenerated: FindColumn("IsLabelGenerated")
            );
        }

        // ═══════════════════════════════════════
        //  INTERNAL HELPERS
        // ═══════════════════════════════════════

        /// <summary>
        /// Türkçe karakter desteği ile string normalizasyonu.
        /// </summary>
        public static string NormalizeHeaderName(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            var normalized = s.Trim().ToUpperInvariant();
            normalized = normalized
                .Replace('Ğ', 'G').Replace('Ü', 'U').Replace('Ş', 'S')
                .Replace('İ', 'I').Replace('I', 'I').Replace('Ö', 'O').Replace('Ç', 'C')
                // ı (dotless i, U+0131) survives ToUpperInvariant in .NET invariant culture
                .Replace('ğ', 'G').Replace('ü', 'U').Replace('ş', 'S')
                .Replace('ı', 'I').Replace('ö', 'O').Replace('ç', 'C');

            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════
        //  RESULT TYPES
        // ═══════════════════════════════════════

        public record HeaderResult(
            bool Found,
            int RowNumber,
            Dictionary<string, int> ColumnMapping,
            string FoundHeaders
        );

        public record ColumnIndices(
            int MuhabereNo,
            int GittigiYer,
            int Durum,
            int Miktar,
            int Adres,
            int Tarih,
            int Barkod,
            int IsLabelGenerated
        );
    }
}
