// BookmarkLbAnchorCompatTests — verifies the lb re-anchor fields (LbAnchor/Side/
// IntraLineOffset) round-trip and that legacy offset-only bookmark JSON still
// deserializes (fields default to null).

using System.Text.Json;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

[Trait("Domain", "Reader")]
public class BookmarkLbAnchorCompatTests
{
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    [Fact]
    public void LegacyOffsetOnlyJson_Deserializes_WithNullAnchorFields()
    {
        // Pre-lb-anchor bookmark shape written by older builds.
        const string legacy =
            "{\"RelPath\":\"T/T2076_.xml\",\"DisplayOffset\":1234,\"Label\":\"foo\"," +
            "\"CreatedUtc\":\"2026-01-01T00:00:00Z\"}";

        var bm = JsonSerializer.Deserialize<Bookmark>(legacy, Opts);

        Assert.NotNull(bm);
        Assert.Equal("T/T2076_.xml", bm!.RelPath);
        Assert.Equal(1234, bm.DisplayOffset);
        Assert.Null(bm.LbAnchor);
        Assert.Null(bm.Side);
        Assert.Null(bm.IntraLineOffset);
    }

    [Fact]
    public void NewBookmark_WithLbAnchor_RoundTrips()
    {
        var bm = new Bookmark
        {
            RelPath = "T/T2076_.xml",
            DisplayOffset = 500,
            Label = "line",
            LbAnchor = "0526c25",
            Side = "tran",
            IntraLineOffset = 7
        };

        var json = JsonSerializer.Serialize(bm, Opts);
        var back = JsonSerializer.Deserialize<Bookmark>(json, Opts);

        Assert.NotNull(back);
        Assert.Equal("0526c25", back!.LbAnchor);
        Assert.Equal("tran", back.Side);
        Assert.Equal(7, back.IntraLineOffset);
        Assert.Equal(500, back.DisplayOffset);
    }
}
