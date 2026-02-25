using TasraPostaManager.Services.Import;

namespace TasraPostaManager.Tests;

/// <summary>
/// ExcelFileReader — static utility methods tests.
/// </summary>
public class ExcelFileReaderTests
{
    // ═══════════════════════════════════════
    //  IsExcelFile
    // ═══════════════════════════════════════

    [Theory]
    [InlineData("test.xlsx", true)]
    [InlineData("test.xls", true)]
    [InlineData("test.XLSX", true)]
    [InlineData("test.csv", false)]
    [InlineData("test.pdf", false)]
    [InlineData("test.txt", false)]
    [InlineData("test", false)]
    public void IsExcelFile_ChecksExtension(string fileName, bool expected)
    {
        Assert.Equal(expected, ExcelFileReader.IsExcelFile(fileName));
    }

    // ═══════════════════════════════════════
    //  NormalizeHeaderName
    // ═══════════════════════════════════════

    [Theory]
    [InlineData("Muhabere No", "MUHABERENO")]
    [InlineData("muhabere no", "MUHABERENO")]
    [InlineData("MUHABERE NO", "MUHABERENO")]
    [InlineData("Gönderen", "GONDEREN")]
    [InlineData("Gittiği Yer", "GITTIGIYER")]
    [InlineData("   Barkod No  ", "BARKODNO")]
    [InlineData("Çıkış", "CIKIS")]
    [InlineData("Özel Şekil", "OZELSEKIL")]
    [InlineData("Ücret", "UCRET")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void NormalizeHeaderName_HandlesVariousInputs(string? input, string expected)
    {
        Assert.Equal(expected, ExcelFileReader.NormalizeHeaderName(input));
    }

    // ═══════════════════════════════════════
    //  GetColumnIndices
    // ═══════════════════════════════════════

    [Fact]
    public void GetColumnIndices_MapsCorrectly()
    {
        var mapping = new Dictionary<string, int>
        {
            { "MuhabereNo", 1 },
            { "GittigiYer", 3 },
            { "Miktar", 5 }
        };

        var indices = ExcelFileReader.GetColumnIndices(mapping);

        Assert.Equal(1, indices.MuhabereNo);
        Assert.Equal(3, indices.GittigiYer);
        Assert.Equal(5, indices.Miktar);
        Assert.Equal(-1, indices.Barkod); // Not mapped → -1
        Assert.Equal(-1, indices.Tarih);  // Not mapped → -1
    }

    [Fact]
    public void GetColumnIndices_EmptyMapping_AllNegative()
    {
        var indices = ExcelFileReader.GetColumnIndices(new Dictionary<string, int>());

        Assert.Equal(-1, indices.MuhabereNo);
        Assert.Equal(-1, indices.GittigiYer);
        Assert.Equal(-1, indices.Durum);
        Assert.Equal(-1, indices.Miktar);
    }
}
