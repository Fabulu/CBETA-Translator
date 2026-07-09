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
    public void GetLayoutMode_DefaultsToPage_WhenUnset()
    {
        var svc = new ReaderStateService(_tmp);
        Assert.Equal(ReadingLayoutMode.Page, svc.GetLayoutMode("T/T2076_.xml"));
    }

    [Fact]
    public void SetLayoutMode_RoundTrips_AndPersistsToDisk()
    {
        var svc = new ReaderStateService(_tmp);
        svc.SetLayoutMode("T/T2076_.xml", ReadingLayoutMode.MergedFlow);

        Assert.Equal(ReadingLayoutMode.MergedFlow, svc.GetLayoutMode("T/T2076_.xml"));
        Assert.True(File.Exists(_tmp));

        // A fresh instance re-reads the sidecar.
        var svc2 = new ReaderStateService(_tmp);
        Assert.Equal(ReadingLayoutMode.MergedFlow, svc2.GetLayoutMode("T/T2076_.xml"));
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
        svc.SetLayoutMode("", ReadingLayoutMode.MergedFlow);
        Assert.Equal(ReadingLayoutMode.Page, svc.GetLayoutMode(""));
    }
}
