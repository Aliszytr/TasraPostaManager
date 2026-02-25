using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services.Pdf
{
    /// <summary>
    /// Teslim Listesi PDF üretimi — PdfService üzerine ince facade.
    /// Gelecekte PdfService'den tamamen ayrılabilir.
    /// </summary>
    public class ListPdfService
    {
        private readonly IPdfService _pdfService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILogger<ListPdfService> _logger;

        public ListPdfService(
            IPdfService pdfService,
            IAppSettingsService appSettingsService,
            ILogger<ListPdfService> logger)
        {
            _pdfService = pdfService;
            _appSettingsService = appSettingsService;
            _logger = logger;
        }

        /// <summary>
        /// Teslim listesi PDF'i üretir ve dosyaya kaydeder.
        /// </summary>
        public string GenerateList(
            IReadOnlyList<PostaRecord> records,
            LabelSettings settings,
            string? title = null)
        {
            _logger.LogInformation("Liste PDF üretimi başladı: {Count} kayıt", records.Count);

            var appSettings = _appSettingsService.GetAppSettings();
            var outputDir = appSettings.PdfOutputPath ?? @"D:\TasraPostaManagerOutput";
            Directory.CreateDirectory(outputDir);

            var fileName = $"TeslimListesi_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var outputPath = Path.Combine(outputDir, fileName);

            var pdfBytes = _pdfService.ListPdf(records, settings, title);
            File.WriteAllBytes(outputPath, pdfBytes);

            _logger.LogInformation("Liste PDF kaydedildi: {Path}", outputPath);
            return outputPath;
        }
    }
}
