using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services.Pdf
{
    /// <summary>
    /// Etiket PDF üretimi — PdfService üzerine ince facade.
    /// Gelecekte PdfService'den tamamen ayrılabilir.
    /// </summary>
    public class LabelPdfService
    {
        private readonly IPdfService _pdfService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILogger<LabelPdfService> _logger;

        public LabelPdfService(
            IPdfService pdfService,
            IAppSettingsService appSettingsService,
            ILogger<LabelPdfService> logger)
        {
            _pdfService = pdfService;
            _appSettingsService = appSettingsService;
            _logger = logger;
        }

        /// <summary>
        /// Etiket PDF'i üretir ve byte[] olarak döner.
        /// </summary>
        public byte[] GenerateLabels(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title = null)
        {
            _logger.LogInformation("Etiket PDF üretimi başladı: {Count} kayıt", records.Count);
            var result = _pdfService.LabelsV3(records, settings, title);
            _logger.LogInformation("Etiket PDF üretimi tamamlandı: {Size} bytes", result.Length);
            return result;
        }

        /// <summary>
        /// Etiket PDF'ini dosyaya kaydeder.
        /// </summary>
        public string GenerateAndSaveLabels(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title = null)
        {
            var pdfBytes = GenerateLabels(records, settings, title);

            var appSettings = _appSettingsService.GetAppSettings();
            var outputDir = appSettings.PdfOutputPath ?? @"D:\TasraPostaManagerOutput";
            Directory.CreateDirectory(outputDir);

            var fileName = $"Etiketler_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var outputPath = Path.Combine(outputDir, fileName);

            File.WriteAllBytes(outputPath, pdfBytes);
            _logger.LogInformation("Etiket PDF kaydedildi: {Path}", outputPath);
            return outputPath;
        }
    }
}
