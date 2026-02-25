using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasraPostaManager.Core.Interfaces;
using TasraPostaManager.Models;
using TasraPostaManager.Services;

namespace TasraPostaManager.Controllers.Api;

/// <summary>
/// REST API — Kayıt yönetimi endpoint'leri.
/// Prefix: /api/records
/// </summary>
[ApiController]
[Route("api/records")]
[Produces("application/json")]
public class RecordsApiController : ControllerBase
{
    private readonly IPostaRecordRepository _repo;
    private readonly ICachingService _cache;
    private readonly ILogger<RecordsApiController> _logger;

    public RecordsApiController(
        IPostaRecordRepository repo,
        ICachingService cache,
        ILogger<RecordsApiController> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/records
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kayıtları filtreli olarak getirir (sayfalama destekli).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchQuery,
        [FromQuery] string? searchType,
        [FromQuery] string? listeTipi,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var filter = new RecordFilterDto
        {
            SearchQuery = searchQuery,
            SearchType = searchType ?? "muhabere",
            ListeTipi = listeTipi
        };

        var query = _repo.GetFilteredQuery(filter);
        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = records.Select(r => new
            {
                r.MuhabereNo,
                r.GittigiYer,
                r.BarkodNo,
                r.Miktar,
                r.ListeTipi,
                r.Tarih,
                r.CreatedAt
            }),
            pagination = new
            {
                page,
                pageSize,
                totalItems = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/records/{muhabereNo}
    // ─────────────────────────────────────────────────────────

    [HttpGet("{muhabereNo}")]
    public async Task<IActionResult> GetByMuhabereNo(string muhabereNo)
    {
        var record = await _repo.GetByMuhabereNoAsync(muhabereNo);
        if (record == null)
            return NotFound(new { message = $"Kayıt bulunamadı: {muhabereNo}" });

        return Ok(record);
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/records/stats
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dashboard istatistikleri.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        const string cacheKey = "Stats:Dashboard";
        var cached = _cache.Get<RecordStatsDto>(cacheKey);
        if (cached != null) return Ok(cached);

        var stats = await _repo.GetStatsAsync();
        _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(2));

        return Ok(stats);
    }

    // ─────────────────────────────────────────────────────────
    //  GET /api/records/recent?count=10
    // ─────────────────────────────────────────────────────────

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        var records = await _repo.GetRecentAsync(Math.Min(count, 100));
        return Ok(records.Select(r => new
        {
            r.MuhabereNo,
            r.GittigiYer,
            r.BarkodNo,
            r.Miktar,
            r.ListeTipi,
            r.CreatedAt
        }));
    }

    // ─────────────────────────────────────────────────────────
    //  DELETE /api/records/{muhabereNo}
    // ─────────────────────────────────────────────────────────

    [HttpDelete("{muhabereNo}")]
    public async Task<IActionResult> Delete(string muhabereNo)
    {
        var record = await _repo.GetByMuhabereNoAsync(muhabereNo);
        if (record == null)
            return NotFound(new { message = $"Kayıt bulunamadı: {muhabereNo}" });

        await _repo.DeleteAsync(record);
        await _repo.SaveChangesAsync();

        _cache.Remove("Stats:Dashboard");
        _logger.LogInformation("Kayıt silindi: {MuhabereNo}", muhabereNo);

        return NoContent();
    }
}
