// ReaderStateServiceTests — verifies the reader-state.json sidecar: layout-mode and
// resume-anchor round-trips, defaults, and persistence across service instances.

using System;
using System.IO;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

[Trait("Domain", "Reader")]
public class ReaderStateServiceTests : IDisposable
{
    private readonly string _tmp;

    public ReaderStateServiceTests()
    {
        _tmp = Path.Combine(Path.GetTempPath(), "readerstate-" + Guid.NewGuid().ToString("N") + ".json");
    }

    public void Dispose()
    {
        try { if (File.Exists(_tmp)) File.Delete(_tmp); } catch { }
        try { if (File.Exists(_tmp + ".tmp")) File.Delete(_tmp + ".tmp"); } catch { }
    }

    [Fact]
    public void GetLayoutMode_DefaultsToMergedFlow_WhenUnset()
    {
        // A2 default-flip: an unknown/unset relPath now coalesces to MergedFlow (the
        // SPA-parity default preference). Page remains the runtime no-map fallback.
        var svc = new ReaderStateService(_tmp);
        Assert.Equal(ReadingLayoutMode.MergedFlow, svc.GetLayoutMode("T/T2076_.xml"));
    }

    [Fact]
    public void SetLayoutMode_RoundTrips_AndPersistsToDisk()
    {
        var svc = new ReaderStateService(_tmp);
        // Use Page (non-default after the A2 flip) so the equality guard in SetLayoutMode
        // does not short-circuit the write. (Persisting the default MergedFlow is a
        // deliberate no-op: GetLayoutMode coalesces a missing entry back to MergedFlow.)
        svc.SetLayoutMode("T/T2076_.xml", ReadingLayoutMode.Page);

        Assert.Equal(ReadingLayoutMode.Page, svc.GetLayoutMode("T/T2076_.xml"));
        Assert.True(File.Exists(_tmp));

        // A fresh instance re-reads the sidecar.
        var svc2 = new ReaderStateService(_tmp);
        Assert.Equal(ReadingLayoutMode.Page, svc2.GetLayoutMode("T/T2076_.xml"));
    }

    [Fact]
    public void LayoutMode_IsPerDocument()
    {
        var svc = new ReaderStateService(_tmp);
        svc.SetLayoutMode("A.xml", ReadingLayoutMode.MergedFlow);
        svc.SetLayoutMode("B.xml", ReadingLayoutMode.Page);

        Assert.Equal(ReadingLayoutMode.MergedFlow, svc.GetLayoutMode("A.xml"));
        Assert.Equal(ReadingLayoutMode.Page, svc.GetLayoutMode("B.xml"));
    }

    [Fact]
    public void ResumeAnchor_RoundTrips()
    {
        var svc = new ReaderStateService(_tmp);
        svc.SetResumeAnchor("T/T2076_.xml", "0526c25", "orig");

        var svc2 = new ReaderStateService(_tmp);
        var anchor = svc2.GetResumeAnchor("T/T2076_.xml");
        Assert.NotNull(anchor);
        Assert.Equal("0526c25", anchor!.Lb);
        Assert.Equal("orig", anchor.Side);
    }

    [Fact]
    public void SetResumeAnchor_IgnoresNullLb()
    {
        var svc = new ReaderStateService(_tmp);
        svc.SetResumeAnchor("X.xml", null, "orig");
        Assert.Null(svc.GetResumeAnchor("X.xml"));
    }

    [Fact]
    public void SettingLayout_PreservesExistingResume()
    {
        var svc = new ReaderStateService(_tmp);
        svc.SetResumeAnchor("Z.xml", "0001a01", "tran");
        svc.SetLayoutMode("Z.xml", ReadingLayoutMode.MergedFlow);

        var svc2 = new ReaderStateService(_tmp);
        Assert.Equal(ReadingLayoutMode.MergedFlow, svc2.GetLayoutMode("Z.xml"));
        Assert.Equal("0001a01", svc2.GetResumeAnchor("Z.xml")?.Lb);
    }

    [Fact]
    public void BlankRelPath_IsIgnored()
    {
        var svc = new ReaderStateService(_tmp);
        svc.SetLayoutMode("", ReadingLayoutMode.Page);
        // Nothing was stored for the blank key, so it coalesces to the default (MergedFlow).
        Assert.Equal(ReadingLayoutMode.MergedFlow, svc.GetLayoutMode(""));
    }
}

[Trait("Domain", "Reader")]
public class ReadingLayoutModeWireTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Opts = new();

    // The reader-state.json contract stores ReadingLayoutMode as a raw integer. Page=0
    // and MergedFlow=1 are WIRE-FROZEN; the new modes append 2..6. A regression here
    // would silently remap every user's persisted preference.

    [Theory]
    [InlineData("{\"layoutMode\":0}", ReadingLayoutMode.Page)]
    [InlineData("{\"layoutMode\":1}", ReadingLayoutMode.MergedFlow)]
    [InlineData("{\"layoutMode\":2}", ReadingLayoutMode.SyncedPanes)]
    [InlineData("{\"layoutMode\":3}", ReadingLayoutMode.AlignedLines)]
    [InlineData("{\"layoutMode\":4}", ReadingLayoutMode.AlignedBlocks)]
    [InlineData("{\"layoutMode\":5}", ReadingLayoutMode.Interleaved)]
    [InlineData("{\"layoutMode\":6}", ReadingLayoutMode.MergedStacked)]
    public void Deserialize_WireInt_MapsToMode(string json, ReadingLayoutMode expected)
    {
        var doc = System.Text.Json.JsonSerializer.Deserialize<ReaderDocumentState>(json, Opts);
        Assert.NotNull(doc);
        Assert.Equal(expected, doc!.LayoutMode);
    }

    [Theory]
    [InlineData(ReadingLayoutMode.Page, 0)]
    [InlineData(ReadingLayoutMode.MergedFlow, 1)]
    [InlineData(ReadingLayoutMode.SyncedPanes, 2)]
    [InlineData(ReadingLayoutMode.AlignedLines, 3)]
    [InlineData(ReadingLayoutMode.AlignedBlocks, 4)]
    [InlineData(ReadingLayoutMode.Interleaved, 5)]
    [InlineData(ReadingLayoutMode.MergedStacked, 6)]
    public void Serialize_Mode_EmitsWireInt(ReadingLayoutMode mode, int expected)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new ReaderDocumentState { LayoutMode = mode }, Opts);
        Assert.Contains($"\"layoutMode\":{expected}", json);
    }
}
