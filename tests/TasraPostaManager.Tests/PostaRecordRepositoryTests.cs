using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TasraPostaManager.Core.Interfaces;
using TasraPostaManager.Data;
using TasraPostaManager.Data.Repositories;
using TasraPostaManager.Models;

namespace TasraPostaManager.Tests;

/// <summary>
/// PostaRecordRepository — In-memory DB testleri.
/// </summary>
public class PostaRecordRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PostaRecordRepository _repo;

    public PostaRecordRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new PostaRecordRepository(_db, NullLogger<PostaRecordRepository>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    //  Yardımcı — test verisi oluşturma
    // ═══════════════════════════════════════

    private PostaRecord CreateRecord(string muhabereNo, decimal? miktar = null, ListeTipi tip = ListeTipi.ParaliDegil)
        => new()
        {
            MuhabereNo = muhabereNo,
            GittigiYer = "Test Şehri",
            Miktar = miktar,
            Tarih = DateTime.UtcNow,
            ListeTipi = tip,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    // ═══════════════════════════════════════
    //  CRUD Testleri
    // ═══════════════════════════════════════

    [Fact]
    public async Task AddAsync_KayitEklerVeSayartirir()
    {
        var record = CreateRecord("2025/100", 50m);
        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();

        var count = await _repo.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByMuhabereNoAsync_VarOlanKayitDoner()
    {
        var record = CreateRecord("2025/200");
        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByMuhabereNoAsync("2025/200");
        Assert.NotNull(found);
        Assert.Equal("Test Şehri", found.GittigiYer);
    }

    [Fact]
    public async Task GetByMuhabereNoAsync_OlmayanKayitNullDoner()
    {
        var found = await _repo.GetByMuhabereNoAsync("YOK/999");
        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_TumKayitlariDoner()
    {
        await _repo.AddAsync(CreateRecord("2025/1"));
        await _repo.AddAsync(CreateRecord("2025/2"));
        await _repo.AddAsync(CreateRecord("2025/3"));
        await _repo.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task AddRangeAsync_TopluEklemeCalisiyor()
    {
        var records = new[]
        {
            CreateRecord("2025/10"),
            CreateRecord("2025/11"),
            CreateRecord("2025/12"),
            CreateRecord("2025/13")
        };
        await _repo.AddRangeAsync(records);
        await _repo.SaveChangesAsync();

        Assert.Equal(4, await _repo.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_KayitSilmeCalisir()
    {
        var record = CreateRecord("2025/SIL");
        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();

        await _repo.DeleteAsync(record);
        await _repo.SaveChangesAsync();

        Assert.Equal(0, await _repo.CountAsync());
    }

    [Fact]
    public async Task DeleteRangeAsync_TopluSilmeCalisiyor()
    {
        var records = new[]
        {
            CreateRecord("2025/A"),
            CreateRecord("2025/B"),
            CreateRecord("2025/C")
        };
        await _repo.AddRangeAsync(records);
        await _repo.SaveChangesAsync();

        await _repo.DeleteRangeAsync(records);
        await _repo.SaveChangesAsync();

        Assert.Equal(0, await _repo.CountAsync());
    }

    // ═══════════════════════════════════════
    //  Filtreleme Testleri
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetFilteredQuery_MuhabereArama_DogrusunuBulur()
    {
        await _repo.AddAsync(CreateRecord("2025/500"));
        await _repo.AddAsync(CreateRecord("2025/600"));
        await _repo.AddAsync(CreateRecord("2025/501"));
        await _repo.SaveChangesAsync();

        var filter = new RecordFilterDto { SearchQuery = "500", SearchType = "muhabere" };
        var result = await _repo.GetFilteredQuery(filter).ToListAsync();

        Assert.Equal(1, result.Count);
        Assert.Equal("2025/500", result[0].MuhabereNo);
    }

    [Fact]
    public async Task GetFilteredQuery_BarkodArama_Calisiyor()
    {
        var r1 = CreateRecord("2025/700");
        r1.BarkodNo = "RR123456789TR";
        var r2 = CreateRecord("2025/701");
        r2.BarkodNo = "RR999999999TR";

        await _repo.AddAsync(r1);
        await _repo.AddAsync(r2);
        await _repo.SaveChangesAsync();

        var filter = new RecordFilterDto { SearchQuery = "123456", SearchType = "barkod" };
        var result = await _repo.GetFilteredQuery(filter).ToListAsync();

        Assert.Single(result);
        Assert.Equal("RR123456789TR", result[0].BarkodNo);
    }

    [Fact]
    public async Task GetFilteredQuery_ListeTipi_Filtreler()
    {
        await _repo.AddAsync(CreateRecord("2025/P1", 10m, ListeTipi.Parali));
        await _repo.AddAsync(CreateRecord("2025/P2", 20m, ListeTipi.Parali));
        await _repo.AddAsync(CreateRecord("2025/U1", null, ListeTipi.ParaliDegil));
        await _repo.SaveChangesAsync();

        var filter = new RecordFilterDto { ListeTipi = "Parali" };
        var result = await _repo.GetFilteredQuery(filter).ToListAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(ListeTipi.Parali, r.ListeTipi));
    }

    // ═══════════════════════════════════════
    //  İstatistik Testleri
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetStatsAsync_DogruIstatistikDoner()
    {
        await _repo.AddAsync(CreateRecord("2025/S1", 100m, ListeTipi.Parali));
        await _repo.AddAsync(CreateRecord("2025/S2", 250m, ListeTipi.Parali));
        await _repo.AddAsync(CreateRecord("2025/S3", null, ListeTipi.ParaliDegil));
        await _repo.SaveChangesAsync();

        var stats = await _repo.GetStatsAsync();

        Assert.Equal(3, stats.TotalRecords);
        Assert.Equal(2, stats.PaidRecords);
        Assert.Equal(1, stats.FreeRecords);
        Assert.Equal(350m, stats.TotalAmount);
    }

    // ═══════════════════════════════════════
    //  Son Kayıtlar
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetRecentAsync_SonKayitlariSiraliDoner()
    {
        for (int i = 1; i <= 5; i++)
        {
            var r = CreateRecord($"2025/R{i}");
            r.CreatedAt = DateTime.UtcNow.AddMinutes(-i); // Eski → yeni sıralama
            await _repo.AddAsync(r);
        }
        await _repo.SaveChangesAsync();

        var recent = await _repo.GetRecentAsync(3);
        Assert.Equal(3, recent.Count);
        // En yeni ilk sırada olmalı
        Assert.Equal("2025/R1", recent[0].MuhabereNo);
    }

    [Fact]
    public async Task GetByBarcodesAsync_BarkodlarlaSorgulama()
    {
        var r1 = CreateRecord("2025/B1"); r1.BarkodNo = "AAA111";
        var r2 = CreateRecord("2025/B2"); r2.BarkodNo = "BBB222";
        var r3 = CreateRecord("2025/B3"); r3.BarkodNo = "CCC333";

        await _repo.AddRangeAsync(new[] { r1, r2, r3 });
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByBarcodesAsync(new[] { "AAA111", "CCC333" });
        Assert.Equal(2, found.Count);
    }
}
