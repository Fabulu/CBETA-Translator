// ReaderStateModelTests — pins the reader-state.json DATA CONTRACT that lives in the
// model itself, complementing ReaderStateServiceTests (behavior) and
// ReadingLayoutModeWireTests (the ReadingLayoutMode wire ints). Covers the pieces those
// two don't: the ReaderViewMode toolbar ordinals, the ResumeAnchor JSON property names +
// default timestamp, and the case-insensitive Documents map + full-tree round-trip.

using System;
using System.Text.Json;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

[Trait("Domain", "Reader")]
public class ReaderStateModelTests
{
    private static readonly JsonSerializerOptions Opts = new();

    // ── ReaderViewMode: ordinal-aligned with the toolbar view selector (0=ZH, 1=Both, 2=EN) ──

    [Theory]
    [InlineData(ReaderViewMode.Zh, 0)]
    [InlineData(ReaderViewMode.Both, 1)]
    [InlineData(ReaderViewMode.En, 2)]
    public void ReaderViewMode_HasToolbarAlignedOrdinals(ReaderViewMode mode, int ordinal)
        => Assert.Equal(ordinal, (int)mode);

    [Theory]
    [InlineData(0, ReaderViewMode.Zh)]
    [InlineData(1, ReaderViewMode.Both)]
    [InlineData(2, ReaderViewMode.En)]
    public void ReaderViewMode_SerializesAsRawInt(int wire, ReaderViewMode expected)
    {
        var json = JsonSerializer.Serialize(expected, Opts);
        Assert.Equal(wire.ToString(System.Globalization.CultureInfo.InvariantCulture), json);
        Assert.Equal(expected, JsonSerializer.Deserialize<ReaderViewMode>(json, Opts));
    }

    // ── ResumeAnchor: JSON property-name mapping + default timestamp ──

    [Fact]
    public void ResumeAnchor_UsesCamelCaseJsonPropertyNames()
    {
        var anchor = new ResumeAnchor { Lb = "0526c25", Side = "orig", UpdatedUtc = new DateTime(2026, 7, 20, 1, 2, 3, DateTimeKind.Utc) };
        var json = JsonSerializer.Serialize(anchor, Opts);

        Assert.Contains("\"lb\":\"0526c25\"", json);
        Assert.Contains("\"side\":\"orig\"", json);
        Assert.Contains("\"updatedUtc\":", json);
        // No PascalCase leakage — the wire names are frozen lowercase.
        Assert.DoesNotContain("\"Lb\"", json);
        Assert.DoesNotContain("\"Side\"", json);
    }

    [Fact]
    public void ResumeAnchor_RoundTripsAllFields()
    {
        var when = new DateTime(2026, 7, 20, 8, 30, 0, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new ResumeAnchor { Lb = "0001a01", Side = "tran", UpdatedUtc = when }, Opts);

        var back = JsonSerializer.Deserialize<ResumeAnchor>(json, Opts);
        Assert.NotNull(back);
        Assert.Equal("0001a01", back!.Lb);
        Assert.Equal("tran", back.Side);
        Assert.Equal(when, back.UpdatedUtc);
    }

    [Fact]
    public void ResumeAnchor_DefaultsTimestampToRecentUtc()
    {
        var before = DateTime.UtcNow;
        var anchor = new ResumeAnchor();
        var after = DateTime.UtcNow;

        // The default UpdatedUtc is stamped at construction (DateTime.UtcNow).
        Assert.InRange(anchor.UpdatedUtc, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Null(anchor.Lb);
        Assert.Null(anchor.Side);
    }

    // ── ReaderDocumentState: default layout + nested resume round-trip ──

    [Fact]
    public void ReaderDocumentState_DefaultLayoutIsMergedFlow()
        => Assert.Equal(ReadingLayoutMode.MergedFlow, new ReaderDocumentState().LayoutMode);

    [Fact]
    public void ReaderDocumentState_NestedResumeRoundTrips()
    {
        var doc = new ReaderDocumentState
        {
            LayoutMode = ReadingLayoutMode.Page,
            Resume = new ResumeAnchor { Lb = "0100b12", Side = "orig" },
        };
        var back = JsonSerializer.Deserialize<ReaderDocumentState>(JsonSerializer.Serialize(doc, Opts), Opts);

        Assert.NotNull(back);
        Assert.Equal(ReadingLayoutMode.Page, back!.LayoutMode);
        Assert.NotNull(back.Resume);
        Assert.Equal("0100b12", back.Resume!.Lb);
        Assert.Equal("orig", back.Resume.Side);
    }

    // ── ReaderState.Documents: freshly-constructed map is case-insensitive by contract ──

    [Fact]
    public void Documents_FreshInstance_IsCaseInsensitiveByRelPath()
    {
        var state = new ReaderState();
        state.Documents["T/T2076_.xml"] = new ReaderDocumentState { LayoutMode = ReadingLayoutMode.Page };

        // Case-insensitive keying matches how the corpus resolves paths on Windows.
        Assert.True(state.Documents.ContainsKey("t/t2076_.xml"));
        Assert.Equal(ReadingLayoutMode.Page, state.Documents["T/T2076_.XML"].LayoutMode);
    }

    [Fact]
    public void ReaderState_FullTree_RoundTripsData()
    {
        var state = new ReaderState();
        state.Documents["T/A.xml"] = new ReaderDocumentState
        {
            LayoutMode = ReadingLayoutMode.Interleaved,
            Resume = new ResumeAnchor { Lb = "0005c07", Side = "tran" },
        };
        state.Documents["X/B.xml"] = new ReaderDocumentState { LayoutMode = ReadingLayoutMode.Page };

        var back = JsonSerializer.Deserialize<ReaderState>(JsonSerializer.Serialize(state, Opts), Opts);

        Assert.NotNull(back);
        Assert.Equal(2, back!.Documents.Count);
        Assert.Equal(ReadingLayoutMode.Interleaved, back.Documents["T/A.xml"].LayoutMode);
        Assert.Equal("0005c07", back.Documents["T/A.xml"].Resume!.Lb);
        Assert.Equal(ReadingLayoutMode.Page, back.Documents["X/B.xml"].LayoutMode);
    }
}
