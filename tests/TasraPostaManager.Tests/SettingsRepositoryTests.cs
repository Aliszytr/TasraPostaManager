using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TasraPostaManager.Data;
using TasraPostaManager.Models;
using TasraPostaManager.Services.Settings;

namespace TasraPostaManager.Tests;

/// <summary>
/// SettingsRepository — In-memory DB testleri.
/// </summary>
public class SettingsRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SettingsRepository _repo;

    public SettingsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new SettingsRepository(_db, NullLogger<SettingsRepository>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    //  Tekil Ayar Okuma/Yazma
    // ═══════════════════════════════════════

    [Fact]
    public async Task SetSettingAsync_YeniAyarOlusturur()
    {
        await _repo.SetSettingAsync("TestKey", "TestValue");
        var value = await _repo.GetSettingAsync("TestKey");

        Assert.Equal("TestValue", value);
    }

    [Fact]
    public async Task SetSettingAsync_VarOlanAyariGunceller()
    {
        await _repo.SetSettingAsync("UpdateMe", "Eski");
        await _repo.SetSettingAsync("UpdateMe", "Yeni");

        var value = await _repo.GetSettingAsync("UpdateMe");
        Assert.Equal("Yeni", value);
    }

    [Fact]
    public async Task GetSettingAsync_OlmayanAyarNullDoner()
    {
        var value = await _repo.GetSettingAsync("Yok");
        Assert.Null(value);
    }

    // ═══════════════════════════════════════
    //  Toplu Okuma/Yazma
    // ═══════════════════════════════════════

    [Fact]
    public async Task SetSettingsAsync_TopluYazmaCalisiyor()
    {
        var ayarlar = new Dictionary<string, string>
        {
            { "PdfOutputPath", @"D:\Output" },
            { "DefaultGonderen", "Belediye" },
            { "BarcodePrefix", "RR" }
        };
        await _repo.SetSettingsAsync(ayarlar);

        var okunan = await _repo.GetSettingsAsync(new[] { "PdfOutputPath", "DefaultGonderen", "BarcodePrefix" });
        Assert.Equal(3, okunan.Count);
        Assert.Equal(@"D:\Output", okunan["PdfOutputPath"]);
        Assert.Equal("Belediye", okunan["DefaultGonderen"]);
    }

    [Fact]
    public async Task GetSettingsAsync_EksikKeylerBosStringDoner()
    {
        await _repo.SetSettingAsync("Var", "Deger");

        var okunan = await _repo.GetSettingsAsync(new[] { "Var", "Yok" });
        Assert.Equal("Deger", okunan["Var"]);
        Assert.Equal(string.Empty, okunan["Yok"]);
    }

    // ═══════════════════════════════════════
    //  GetAllSettings & EnsureExists
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetAllSettingsAsync_TumAyarlariDoner()
    {
        await _repo.SetSettingAsync("A", "1");
        await _repo.SetSettingAsync("B", "2");

        var all = await _repo.GetAllSettingsAsync();
        Assert.True(all.Count >= 2);
        Assert.Equal("1", all["A"]);
    }

    [Fact]
    public async Task EnsureSettingExistsAsync_YogsaOlusturur()
    {
        await _repo.EnsureSettingExistsAsync("Yeni", "Varsayilan");

        var value = await _repo.GetSettingAsync("Yeni");
        Assert.Equal("Varsayilan", value);
    }

    [Fact]
    public async Task EnsureSettingExistsAsync_VarsaDokunmaz()
    {
        await _repo.SetSettingAsync("Mevcut", "OzelDeger");
        await _repo.EnsureSettingExistsAsync("Mevcut", "Varsayilan");

        var value = await _repo.GetSettingAsync("Mevcut");
        Assert.Equal("OzelDeger", value); // Üzerine yazılmamalı
    }
}
