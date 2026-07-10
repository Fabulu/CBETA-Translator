using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Behavior pins for the consolidated CJK ideograph classifier (dead-code audit
/// 2026-07-09, item #2).
///
/// The load-bearing test is <see cref="IsIdeograph_EqualsHistoricalThreeRangeSet_OverEntireBmp"/>:
/// three callers (SearchIndexService.IsCjk / IsIndexableCjk,
/// InvertedSearchIndex.IsIndexable) feed FIVE GUID-versioned search artifacts that
/// must stay byte-identical. If <see cref="CjkText.IsIdeograph"/> ever drifts from
/// the historical {U+3400-4DBF, U+4E00-9FFF, U+F900-FAFF} set, the index silently
/// corrupts. This test makes that drift impossible without a red build.
/// </summary>
public class CjkTextTests
{
    // The exact three-range predicate every routed copy used, expressed with
    // encoding-independent numeric code points.
    private static bool Historical(int cp)
        => (cp >= 0x3400 && cp <= 0x4DBF)
        || (cp >= 0x4E00 && cp <= 0x9FFF)
        || (cp >= 0xF900 && cp <= 0xFAFF);

    [Fact]
    public void IsIdeograph_EqualsHistoricalThreeRangeSet_OverEntireBmp()
    {
        for (int cp = 0x0000; cp <= 0xFFFF; cp++)
        {
            char c = (char)cp;
            Assert.Equal(Historical(cp), CjkText.IsIdeograph(c));
        }
    }

    [Theory]
    // range boundaries (inclusive) and the gaps that must be excluded
    [InlineData(0x33FF, false)] // just below Ext A
    [InlineData(0x3400, true)]  // Ext A start
    [InlineData(0x4DBF, true)]  // Ext A end
    [InlineData(0x4DC0, false)] // Yijing hexagram gap start
    [InlineData(0x4DFF, false)] // Yijing hexagram gap end
    [InlineData(0x4E00, true)]  // CJK Unified start
    [InlineData(0x9FFF, true)]  // CJK Unified end
    [InlineData(0xA000, false)] // just above Unified
    [InlineData(0xF8FF, false)] // just below Compatibility
    [InlineData(0xF900, true)]  // Compatibility start
    [InlineData(0xFAFF, true)]  // Compatibility end
    [InlineData(0xFB00, false)] // just above Compatibility
    [InlineData('A', false)]
    [InlineData(' ', false)]
    public void IsIdeograph_Boundaries(int cp, bool expected)
    {
        Assert.Equal(expected, CjkText.IsIdeograph((char)cp));
    }

    [Fact]
    public void ContainsIdeograph_NullOrEmpty_IsFalse()
    {
        Assert.False(CjkText.ContainsIdeograph(null));
        Assert.False(CjkText.ContainsIdeograph(""));
    }

    [Theory]
    [InlineData("hello", false)]
    [InlineData("師", true)]           // U+5E2B in CJK Unified
    [InlineData("mixed 無門 text", true)]
    [InlineData("123 abc .,!", false)]
    public void ContainsIdeograph_MatchesAnyIdeograph(string input, bool expected)
    {
        Assert.Equal(expected, CjkText.ContainsIdeograph(input));
    }
}
