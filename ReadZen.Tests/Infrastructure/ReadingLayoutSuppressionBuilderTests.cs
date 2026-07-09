// ReadingLayoutSuppressionBuilderTests — verifies the pure suppression-set builder
// used by merged-flow reading layout, including the verse/dharani preservation rule.

using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Reader")]
public class ReadingLayoutSuppressionBuilderTests
{
    private static SegmentInfo Seg(string type, params string[] lbs)
        => new() { Type = type, LbRange = new List<string>(lbs) };

    [Fact]
    public void Build_SuppressesAllButFirstLb_PerProseSegment()
    {
        var segs = new List<SegmentInfo>
        {
            Seg("prose", "a1", "a2", "a3"),
            Seg("commentary", "b1", "b2"),
        };

        var set = ReadingLayoutSuppressionBuilder.Build(segs);

        Assert.DoesNotContain("a1", set); // leading lb keeps its newline (paragraph break)
        Assert.Contains("a2", set);
        Assert.Contains("a3", set);
        Assert.DoesNotContain("b1", set);
        Assert.Contains("b2", set);
    }

    [Theory]
    [InlineData("verse")]
    [InlineData("dharani")]
    [InlineData("VERSE")]   // case-insensitive
    [InlineData("Dharani")]
    public void Build_PreservesEveryLineBreak_ForVerseAndDharani(string type)
    {
        var segs = new List<SegmentInfo> { Seg(type, "v1", "v2", "v3") };

        var set = ReadingLayoutSuppressionBuilder.Build(segs);

        // None of the poem/mantra line breaks are suppressed.
        Assert.Empty(set);
    }

    [Fact]
    public void Build_MixedDocument_KeepsVerseButMergesProse()
    {
        var segs = new List<SegmentInfo>
        {
            Seg("prose", "p1", "p2"),
            Seg("verse", "v1", "v2", "v3"),
            Seg("prose", "q1", "q2"),
        };

        var set = ReadingLayoutSuppressionBuilder.Build(segs);

        Assert.Contains("p2", set);
        Assert.Contains("q2", set);
        Assert.DoesNotContain("v2", set);
        Assert.DoesNotContain("v3", set);
    }

    [Fact]
    public void Build_SingleLbOrEmptySegments_ProduceNoSuppression()
    {
        var segs = new List<SegmentInfo>
        {
            Seg("prose", "only"),          // single lb → nothing to merge
            new() { Type = "prose", LbRange = null },
            new() { Type = "prose", LbRange = new List<string>() },
        };

        Assert.Empty(ReadingLayoutSuppressionBuilder.Build(segs));
    }

    [Fact]
    public void Build_NullInputs_ReturnEmptySet()
    {
        Assert.Empty(ReadingLayoutSuppressionBuilder.Build((SegmentMap?)null));
        Assert.Empty(ReadingLayoutSuppressionBuilder.Build((IReadOnlyList<SegmentInfo>?)null));
    }

    [Fact]
    public void Build_SkipsNullOrEmptyLbEntries()
    {
        var segs = new List<SegmentInfo>
        {
            new() { Type = "prose", LbRange = new List<string> { "a1", "", "a3" } },
        };

        var set = ReadingLayoutSuppressionBuilder.Build(segs);

        Assert.Contains("a3", set);
        Assert.DoesNotContain("", set);
    }
}
