// SegmentTypeTransformerTests — exercises the lb-ID extraction logic
// used by the segment type transformer to map RenderSegment keys to
// JSONL lb_range values.

using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Segmentation")]
public class SegmentTypeTransformerTests
{
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
