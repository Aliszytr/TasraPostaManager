using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TasraPostaManager.Data;
using TasraPostaManager.Data.Repositories;
using TasraPostaManager.Models;

namespace TasraPostaManager.Tests;

/// <summary>
/// BarcodePoolRepository — In-memory DB testleri.
/// </summary>
public class BarcodePoolRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BarcodePoolRepository _repo;

    public BarcodePoolRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new BarcodePoolRepository(_db, NullLogger<BarcodePoolRepository>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    //  Yardımcı
    // ═══════════════════════════════════════

    private BarcodePoolItem CreateItem(string barcode, bool isUsed = false)
        => new()
        {
            Barcode = barcode,
            IsUsed = isUsed,
            Status = isUsed ? BarcodePoolStatus.Used : BarcodePoolStatus.Available,
            ImportedAt = DateTime.UtcNow,
            BatchId = "TEST01"
        };

    // ═══════════════════════════════════════
    //  CRUD Testleri
    // ═══════════════════════════════════════

    [Fact]
    public async Task AddAsync_BarkodEkler()
    {
        await _repo.AddAsync(CreateItem("RR000000001TR"));
        await _repo.SaveChangesAsync();

        Assert.Equal(1, await _repo.GetTotalCountAsync());
    }

    [Fact]
    public async Task GetByBarcodeAsync_VarOlaniBulur()
    {
        await _repo.AddAsync(CreateItem("RR111111111TR"));
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByBarcodeAsync("RR111111111TR");
        Assert.NotNull(found);
        Assert.Equal("RR111111111TR", found.Barcode);
    }

    [Fact]
    public async Task GetByBarcodeAsync_OlmayaniNullDoner()
    {
        Assert.Null(await _repo.GetByBarcodeAsync("YOOOK"));
    }

    // ═══════════════════════════════════════
    //  Kullanılabilirlik Testleri
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetNextAvailableAsync_MusaitBarkodDoner()
    {
        await _repo.AddAsync(CreateItem("RR000001TR", isUsed: true));
        await _repo.AddAsync(CreateItem("RR000002TR", isUsed: false));
        await _repo.SaveChangesAsync();

        var next = await _repo.GetNextAvailableAsync();
        Assert.NotNull(next);
        Assert.Equal("RR000002TR", next.Barcode);
        Assert.False(next.IsUsed);
    }

    [Fact]
    public async Task GetNextAvailableAsync_HepsiKullanilmissa_NullDoner()
    {
        await _repo.AddAsync(CreateItem("RR100TR", isUsed: true));
        await _repo.SaveChangesAsync();

        Assert.Null(await _repo.GetNextAvailableAsync());
    }

    [Fact]
    public async Task GetAvailableCountAsync_DogruSayiDoner()
    {
        await _repo.AddAsync(CreateItem("B1", isUsed: false));
        await _repo.AddAsync(CreateItem("B2", isUsed: false));
        await _repo.AddAsync(CreateItem("B3", isUsed: true));
        await _repo.SaveChangesAsync();

        Assert.Equal(2, await _repo.GetAvailableCountAsync());
        Assert.Equal(3, await _repo.GetTotalCountAsync());
        Assert.Equal(1, await _repo.GetUsedCountAsync());
    }

    [Fact]
    public async Task GetAvailableAsync_IstenenKadarDoner()
    {
        for (int i = 0; i < 10; i++)
            await _repo.AddAsync(CreateItem($"AVAIL{i:D3}"));
        await _repo.SaveChangesAsync();

        var batch = await _repo.GetAvailableAsync(3);
        Assert.Equal(3, batch.Count);
    }

    // ═══════════════════════════════════════
    //  MarkAsUsed Testi
    // ═══════════════════════════════════════

    [Fact]
    public async Task MarkAsUsedAsync_BarkodKullanimiIsaretler()
    {
        var item = CreateItem("MARK001");
        await _repo.AddAsync(item);
        await _repo.SaveChangesAsync();

        await _repo.MarkAsUsedAsync(item.Id, "2025/100");
        await _repo.SaveChangesAsync();

        var updated = await _repo.GetByBarcodeAsync("MARK001");
        Assert.NotNull(updated);
        Assert.True(updated.IsUsed);
        Assert.Equal(BarcodePoolStatus.Used, updated.Status);
        Assert.NotNull(updated.UsedAt);
        Assert.Equal("2025/100", updated.UsedByRecordKey);
    }

    // ═══════════════════════════════════════
    //  Toplu İşlem Testleri
    // ═══════════════════════════════════════

    [Fact]
    public async Task ImportBatchAsync_TopluIthalatIsliyor()
    {
        var barcodes = Enumerable.Range(1, 50).Select(i => $"IMP{i:D5}");
        var count = await _repo.ImportBatchAsync(barcodes, "BATCH01", "Test");

        Assert.Equal(50, count);
        Assert.Equal(50, await _repo.GetTotalCountAsync());
        Assert.Equal(50, await _repo.GetAvailableCountAsync());
    }

    [Fact]
    public async Task PurgeUsedAsync_KullanilmislariTemizler()
    {
        await _repo.AddAsync(CreateItem("PURGE1", isUsed: false));
        await _repo.AddAsync(CreateItem("PURGE2", isUsed: true));
        await _repo.AddAsync(CreateItem("PURGE3", isUsed: true));
        await _repo.SaveChangesAsync();

        var purged = await _repo.PurgeUsedAsync();

        Assert.Equal(2, purged);
        Assert.Equal(1, await _repo.GetTotalCountAsync());

        var remaining = await _repo.GetByBarcodeAsync("PURGE1");
        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task DeleteAsync_TekBarkodSilme()
    {
        var item = CreateItem("DEL001");
        await _repo.AddAsync(item);
        await _repo.SaveChangesAsync();

        await _repo.DeleteAsync(item);
        await _repo.SaveChangesAsync();

        Assert.Equal(0, await _repo.GetTotalCountAsync());
    }
}
