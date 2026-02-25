using TasraPostaManager.Services.Import;

namespace TasraPostaManager.Tests;

/// <summary>
/// ExcelDataValidator — pure static methods, 0 dependency.
/// </summary>
public class ExcelDataValidatorTests
{
    // ═══════════════════════════════════════
    //  ExpandMuhabereNos
    // ═══════════════════════════════════════

    [Fact]
    public void ExpandMuhabereNos_SingleNumber_ReturnsSingleItem()
    {
        var result = ExcelDataValidator.ExpandMuhabereNos("2025/35706");
        Assert.Single(result);
        Assert.Equal("2025/35706", result[0]);
    }

    [Fact]
    public void ExpandMuhabereNos_Range_ExpandsCorrectly()
    {
        var result = ExcelDataValidator.ExpandMuhabereNos("2025/35706-35709");
        Assert.Equal(4, result.Count);
        Assert.Equal("2025/35706", result[0]);
        Assert.Equal("2025/35707", result[1]);
        Assert.Equal("2025/35708", result[2]);
        Assert.Equal("2025/35709", result[3]);
    }

    [Fact]
    public void ExpandMuhabereNos_RangeWithComma_ExpandsAll()
    {
        var result = ExcelDataValidator.ExpandMuhabereNos("2025/35706-35709,35711");
        Assert.Equal(5, result.Count);
        Assert.Contains("2025/35706", result);
        Assert.Contains("2025/35707", result);
        Assert.Contains("2025/35708", result);
        Assert.Contains("2025/35709", result);
        Assert.Contains("2025/35711", result);
    }

    [Fact]
    public void ExpandMuhabereNos_ReversedRange_StillExpands()
    {
        var result = ExcelDataValidator.ExpandMuhabereNos("2025/35709-35706");
        Assert.Equal(4, result.Count);
        Assert.Contains("2025/35706", result);
        Assert.Contains("2025/35709", result);
    }

    [Fact]
    public void ExpandMuhabereNos_NoSlash_ReturnAsIs()
    {
        var result = ExcelDataValidator.ExpandMuhabereNos("ABC123");
        Assert.Single(result);
        Assert.Equal("ABC123", result[0]);
    }

    [Fact]
    public void ExpandMuhabereNos_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Empty(ExcelDataValidator.ExpandMuhabereNos(""));
        Assert.Empty(ExcelDataValidator.ExpandMuhabereNos(null!));
        Assert.Empty(ExcelDataValidator.ExpandMuhabereNos("   "));
    }

    [Fact]
    public void ExpandMuhabereNos_DuplicatesRemoved()
    {
        var result = ExcelDataValidator.ExpandMuhabereNos("2025/1-3,2-4");
        // 1,2,3 from first range + 2,3,4 from second = distinct [1,2,3,4]
        Assert.Equal(4, result.Count);
    }

    // ═══════════════════════════════════════
    //  FormatMuhabereNoFromExcel
    // ═══════════════════════════════════════

    [Fact]
    public void FormatMuhabereNoFromExcel_SingleNumber_FormatsCorrectly()
    {
        Assert.Equal("2025/100", ExcelDataValidator.FormatMuhabereNoFromExcel("2025/100"));
    }

    [Fact]
    public void FormatMuhabereNoFromExcel_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ExcelDataValidator.FormatMuhabereNoFromExcel(null!));
        Assert.Equal(string.Empty, ExcelDataValidator.FormatMuhabereNoFromExcel(""));
    }

    [Fact]
    public void FormatMuhabereNoFromExcel_NoSlash_ReturnsTrimmed()
    {
        Assert.Equal("ABC123", ExcelDataValidator.FormatMuhabereNoFromExcel("  ABC123  "));
    }

    // ═══════════════════════════════════════
    //  TryExtractDuplicateBarcode
    // ═══════════════════════════════════════

    [Fact]
    public void TryExtractDuplicateBarcode_ValidMessage_ExtractsBarcode()
    {
        var msg = "Cannot insert duplicate key. The duplicate key value is (RR123456789TR). The statement has been terminated.";
        var result = ExcelDataValidator.TryExtractDuplicateBarcode(msg);
        Assert.Equal("RR123456789TR", result);
    }

    [Fact]
    public void TryExtractDuplicateBarcode_NoMarker_ReturnsNull()
    {
        Assert.Null(ExcelDataValidator.TryExtractDuplicateBarcode("Some random error message"));
    }

    [Fact]
    public void TryExtractDuplicateBarcode_EmptyOrNull_ReturnsNull()
    {
        Assert.Null(ExcelDataValidator.TryExtractDuplicateBarcode(""));
        Assert.Null(ExcelDataValidator.TryExtractDuplicateBarcode(null!));
    }

    // ═══════════════════════════════════════
    //  FindConsecutiveRanges
    // ═══════════════════════════════════════

    [Fact]
    public void FindConsecutiveRanges_ConsecutiveNumbers_MergesIntoRange()
    {
        var ranges = ExcelDataValidator.FindConsecutiveRanges(new List<int> { 1, 2, 3, 5, 7, 8, 9 });
        Assert.Equal(3, ranges.Count);
        Assert.Equal("1-3", ranges[0].ToString());
        Assert.Equal("5", ranges[1].ToString());
        Assert.Equal("7-9", ranges[2].ToString());
    }

    [Fact]
    public void FindConsecutiveRanges_SingleNumber_ReturnsSingleString()
    {
        var ranges = ExcelDataValidator.FindConsecutiveRanges(new List<int> { 42 });
        Assert.Single(ranges);
        Assert.Equal("42", ranges[0].ToString());
    }

    [Fact]
    public void FindConsecutiveRanges_EmptyList_ReturnsEmpty()
    {
        Assert.Empty(ExcelDataValidator.FindConsecutiveRanges(new List<int>()));
    }
}
