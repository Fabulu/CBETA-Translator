using System.Linq;
using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

public class CjkCharDiffTests
{
    [Fact]
    public void IdenticalStrings_SingleEqualSpan()
    {
        var result = CjkCharDiff.Diff("至道無難", "至道無難");
        Assert.Single(result);
        Assert.Equal(CharDiffKind.Equal, result[0].Kind);
        Assert.Equal("至道無難", result[0].Text);
    }

    [Fact]
    public void CompletelyDifferent_DeleteThenInsert()
    {
        var result = CjkCharDiff.Diff("ABC", "XYZ");
        Assert.True(result.Any(s => s.Kind == CharDiffKind.Delete));
        Assert.True(result.Any(s => s.Kind == CharDiffKind.Insert));
    }

    [Fact]
    public void OcrCorrection_ShowsChangedCharacters()
    {
        // Real case from Faith in Mind correction log
        var result = CjkCharDiff.Diff("至道無雅催焦择", "至道無難唯嫌揀擇");
        var compact = CjkCharDiff.FormatCompact(result);

        // The first 3 chars should be Equal
        Assert.Equal(CharDiffKind.Equal, result[0].Kind);
        Assert.StartsWith("至道無", result[0].Text);

        // Should contain both delete and insert spans for the changed portion
        Assert.Contains(result, s => s.Kind == CharDiffKind.Delete);
        Assert.Contains(result, s => s.Kind == CharDiffKind.Insert);

        // Compact format should have [-...] and [+...] markers
        Assert.Contains("[-", compact);
        Assert.Contains("[+", compact);
    }

    [Fact]
    public void SingleCharChange_MinimalDiff()
    {
        var result = CjkCharDiff.Diff("信心名", "信心銘");
        var compact = CjkCharDiff.FormatCompact(result);

        // "信心" equal, "名" deleted, "銘" inserted
        Assert.Contains("信心", compact);
        Assert.Contains("[-名]", compact);
        Assert.Contains("[+銘]", compact);
    }

    [Fact]
    public void EmptyBefore_AllInsert()
    {
        var result = CjkCharDiff.Diff("", "新增文字");
        Assert.Single(result);
        Assert.Equal(CharDiffKind.Insert, result[0].Kind);
    }

    [Fact]
    public void EmptyAfter_AllDelete()
    {
        var result = CjkCharDiff.Diff("删除文字", "");
        Assert.Single(result);
        Assert.Equal(CharDiffKind.Delete, result[0].Kind);
    }

    [Fact]
    public void FormatCompact_ProducesReadableOutput()
    {
        var spans = CjkCharDiff.Diff("三祖大部信心名", "三祖大師信心銘");
        var compact = CjkCharDiff.FormatCompact(spans);

        // Should be something like: 三祖大[-部][+師]信心[-名][+銘]
        Assert.Contains("三祖大", compact);
        Assert.DoesNotContain("三祖大[", compact.Replace("三祖大[-", "").Replace("三祖大[+", ""));
    }
}
