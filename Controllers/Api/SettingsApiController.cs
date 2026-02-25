using Microsoft.AspNetCore.Mvc;
using TasraPostaManager.Core.Interfaces;
using TasraPostaManager.Services;

namespace TasraPostaManager.Controllers.Api;

/// <summary>
/// REST API — Uygulama ayarları endpoint'leri.
/// Prefix: /api/settings
/// </summary>
[ApiController]
[Route("api/settings")]
[Produces("application/json")]
public class SettingsApiController : ControllerBase
{
    private readonly IAppSettingsService _settingsService;
    private readonly ICachingService _cache;
    private readonly ILogger<SettingsApiController> _logger;

    public SettingsApiController(
        IAppSettingsService settingsService,
        ICachingService cache,
        ILogger<SettingsApiController> logger)
    {
        _settingsService = settingsService;
        _cache = cache;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/settings
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tüm uygulama ayarlarını getirir.
    /// </summary>
    [HttpGet]
    public IActionResult GetSettings()
    {
        var settings = _settingsService.GetAppSettings();
        return Ok(settings);
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/settings/labels
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Etiket ayarlarını getirir.
    /// </summary>
    [HttpGet("labels")]
    public IActionResult GetLabelSettings()
    {
        var labels = _settingsService.GetLabelSettingsV2();
        return Ok(labels);
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/settings/barcode
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Barkod konfigürasyonunu getirir.
    /// </summary>
    [HttpGet("barcode")]
    public IActionResult GetBarcodeConfig()
    {
        var config = _settingsService.GetBarcodeConfig();
        return Ok(config);
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/settings/barcode/remaining
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kalan barkod sayısını döner.
    /// </summary>
    [HttpGet("barcode/remaining")]
    public IActionResult GetRemainingBarcodeCount()
    {
        var remaining = _settingsService.GetRemainingBarcodeCount();
        return Ok(new { remaining });
    }
}
