using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasraPostaManager.Core.Interfaces;

namespace TasraPostaManager.Controllers.Api;

/// <summary>
/// REST API — Barkod havuzu endpoint'leri.
/// Prefix: /api/barcodes
/// </summary>
[Authorize]
[ApiController]
[Route("api/barcodes")]
[Produces("application/json")]
public class BarcodesApiController : ControllerBase
{
    private readonly IBarcodePoolRepository _repo;
    private readonly ILogger<BarcodesApiController> _logger;

    public BarcodesApiController(
        IBarcodePoolRepository repo,
        ILogger<BarcodesApiController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/barcodes/stats
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Barkod havuz istatistikleri.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var total = await _repo.GetTotalCountAsync();
        var available = await _repo.GetAvailableCountAsync();
        var used = await _repo.GetUsedCountAsync();

        return Ok(new
        {
            total,
            available,
            used,
            usagePercent = total > 0 ? Math.Round(used * 100.0 / total, 1) : 0
        });
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/barcodes/next
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Havuzdaki bir sonraki kullanılabilir barkodu döner (claim etmez).
    /// </summary>
    [HttpGet("next")]
    public async Task<IActionResult> GetNextAvailable()
    {
        var next = await _repo.GetNextAvailableAsync();
        if (next == null)
            return NotFound(new { message = "Kullanılabilir barkod kalmadı" });

        return Ok(new
        {
            next.Id,
            next.Barcode,
            next.BatchId,
            next.ImportedAt
        });
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/barcodes/{barcode}
    // ─────────────────────────────────────────────────────────

    [HttpGet("{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var item = await _repo.GetByBarcodeAsync(barcode);
        if (item == null)
            return NotFound(new { message = $"Barkod bulunamadı: {barcode}" });

        return Ok(item);
    }
}
