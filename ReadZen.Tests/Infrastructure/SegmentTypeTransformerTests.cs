// SegmentTypeTransformerTests — exercises the lb-ID extraction logic
// used by the segment type transformer to map RenderSegment keys to
// JSONL lb_range values.

using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Segmentation")]
public class SegmentTypeTransformerTests
{
    // Builds a transformer whose render segments simulate the TRANSLATED (English)
    // pane. The English pane carries the SAME "lb|{n}" anchor keys as the Chinese
    // pane (both emitted by TeiRenderer), so the identical SegmentMap resolves it.
    private static SegmentTypeTransformer BuildTranSideTransformer()
    {
        var verse = new SegmentInfo { Type = "verse", LbRange = new List<string> { "0100a01" } };
        var dialogue = new SegmentInfo { Type = "dialogue", LbRange = new List<string> { "0100a05" } };

        var byLbId = new Dictionary<string, SegmentInfo>
        {
            ["0100a01"] = verse,
            ["0100a05"] = dialogue,
        };
        var segMap = new SegmentMap(new List<SegmentInfo> { verse, dialogue }, byLbId);

        // Rendered offsets on the tran side (English text is longer, so offsets
        // differ from the Chinese pane — but the lb keys are shared).
        var tranSegments = new List<RenderSegment>
        {
            new RenderSegment("lb|0100a01|T", 0, 40),
            new RenderSegment("lb|0100a05|T", 40, 90),
            new RenderSegment("lb|9999z99|T", 90, 120), // lb absent from map → graceful null
        };

        return new SegmentTypeTransformer(segMap, tranSegments);
    }

    [Fact]
    public void ResolveSegmentType_ResolvesFromTranOffset_ViaSharedLbId()
    {
        var t = BuildTranSideTransformer();

        // Offset inside the first tran render segment → verse via shared lb "0100a01"
        Assert.Equal("verse", t.ResolveSegmentType(5));
        // Offset inside the second tran render segment → dialogue via shared lb "0100a05"
        Assert.Equal("dialogue", t.ResolveSegmentType(60));
    }

    [Fact]
    public void ResolveSegmentType_ReturnsNull_WhenLbNotInMap()
    {
        var t = BuildTranSideTransformer();

        // Offset falls in a render segment whose lb ("9999z99") is not in the
        // segment map → no styling. This is the correct graceful path.
        Assert.Null(t.ResolveSegmentType(100));
    }

    [Fact]
    public void ResolveSegmentType_ReturnsNull_WhenNoSegmentsCoverOffset()
    {
        var segMap = new SegmentMap(new List<SegmentInfo>(), new Dictionary<string, SegmentInfo>());
        var t = new SegmentTypeTransformer(segMap, new List<RenderSegment>());

        Assert.Null(t.ResolveSegmentType(0));
    }

    [Theory]
    [InlineData("lb|0526c25|T", "0526c25")]
    [InlineData("lb|0526b24|T", "0526b24")]
    [InlineData("lb|0001a01", "0001a01")]
    [InlineData("lb|0526c25|CBETA", "0526c25")]
    public void ExtractLbId_ReturnsNValue_FromLbKey(string key, string expected)
    {
        Assert.Equal(expected, SegmentTypeTransformer.ExtractLbId(key));
    }

    [Theory]
    [InlineData("START")]
    [InlineData("p|p0526c01")]
    [InlineData("pb|0526|T")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractLbId_ReturnsNull_ForNonLbKeys(string? key)
    {
        Assert.Null(SegmentTypeTransformer.ExtractLbId(key!));
    }
}
