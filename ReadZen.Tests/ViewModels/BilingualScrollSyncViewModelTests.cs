// BilingualScrollSyncViewModelTests — the cross-pane scroll-sync component extracted from
// ReadableTabView's code-behind (MVVM ratchet). Covers enable/disable (config toggle vs the
// SyncedPanes mode forcing sync on), the full ShouldSync gate (grid surface / suppress /
// user-intent window), the per-pane intent bookkeeping, and the shared-line-id lead/follow
// mapping (orientation of ZH/EN by which pane leads).

using System;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Text;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

[Trait("Domain", "Reader")]
public class BilingualScrollSyncViewModelTests
{
    private static readonly DateTime T0 = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

    // ---- Enable / disable ----

    [Fact]
    public void Defaults_ConfigEnabled_MergedFlow_SyncActive()
    {
        var vm = new BilingualScrollSyncViewModel();
        Assert.True(vm.ConfigEnabled);
        Assert.Equal(ReadingLayoutMode.MergedFlow, vm.LayoutMode);
        Assert.False(vm.ModeForcesSync);
        Assert.True(vm.IsSyncActive); // via config
    }

    [Fact]
    public void ConfigDisabled_OrdinaryMode_SyncInactive()
    {
        var vm = new BilingualScrollSyncViewModel { ConfigEnabled = false, LayoutMode = ReadingLayoutMode.Page };
        Assert.False(vm.ModeForcesSync);
        Assert.False(vm.IsSyncActive);
    }

    [Fact]
    public void SyncedPanes_ForcesSync_EvenWhenConfigDisabled()
    {
        // The defining behavior of SyncedPanes is always-on viewport scroll-sync, independent
        // of the global config toggle.
        var vm = new BilingualScrollSyncViewModel { ConfigEnabled = false, LayoutMode = ReadingLayoutMode.SyncedPanes };
        Assert.True(vm.ModeForcesSync);
        Assert.True(vm.IsSyncActive);
    }

    [Theory]
    [InlineData(ReadingLayoutMode.Page, false)]
    [InlineData(ReadingLayoutMode.MergedFlow, false)]
    [InlineData(ReadingLayoutMode.AlignedLines, false)]
    [InlineData(ReadingLayoutMode.SyncedPanes, true)]
    public void ModeForcesSync_OnlyForSyncedPanes(ReadingLayoutMode mode, bool forces)
    {
        var vm = new BilingualScrollSyncViewModel { LayoutMode = mode };
        Assert.Equal(forces, vm.ModeForcesSync);
    }

    // ---- Visible "linked scroll" affordance (IsSyncForcedByMode) ----

    [Fact]
    public void IsSyncForcedByMode_TracksMode_AndOverridesConfigOff()
    {
        // The chip's visibility signal: true iff the mode forces sync, independent of config.
        var vm = new BilingualScrollSyncViewModel { ConfigEnabled = false, LayoutMode = ReadingLayoutMode.SyncedPanes };
        Assert.True(vm.IsSyncForcedByMode);

        vm.LayoutMode = ReadingLayoutMode.Page;
        Assert.False(vm.IsSyncForcedByMode);
    }

    [Fact]
    public void IsSyncForcedByMode_RaisesPropertyChanged_WhenLayoutModeChanges()
    {
        var vm = new BilingualScrollSyncViewModel { LayoutMode = ReadingLayoutMode.Page };
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BilingualScrollSyncViewModel.IsSyncForcedByMode)) raised = true;
        };

        vm.LayoutMode = ReadingLayoutMode.SyncedPanes;

        Assert.True(raised);
        Assert.True(vm.IsSyncForcedByMode);
    }

    // ---- User-intent window ----

    [Fact]
    public void HasRecentIntent_IsPerPane_AndWindowed()
    {
        var vm = new BilingualScrollSyncViewModel();
        vm.StampIntent(sourceIsOrig: true, T0);

        Assert.True(vm.HasRecentIntent(sourceIsOrig: true, T0));                                   // just stamped
        Assert.True(vm.HasRecentIntent(sourceIsOrig: true, T0 + TimeSpan.FromMilliseconds(500)));  // inside window
        Assert.False(vm.HasRecentIntent(sourceIsOrig: true, T0 + TimeSpan.FromMilliseconds(700))); // past window
        Assert.False(vm.HasRecentIntent(sourceIsOrig: false, T0));                                 // the other pane never stamped
    }

    // ---- ShouldSync gate ----

    [Fact]
    public void ShouldSync_TrueWhenActive_GridOff_NotSuppressed_RecentIntent()
    {
        var vm = new BilingualScrollSyncViewModel();
        vm.StampIntent(sourceIsOrig: true, T0);
        Assert.True(vm.ShouldSync(sourceIsOrig: true, gridSurfaceActive: false, T0));
    }

    [Fact]
    public void ShouldSync_FalseOnGridSurface()
    {
        var vm = new BilingualScrollSyncViewModel();
        vm.StampIntent(sourceIsOrig: true, T0);
        Assert.False(vm.ShouldSync(sourceIsOrig: true, gridSurfaceActive: true, T0));
    }

    [Fact]
    public void ShouldSync_FalseWhenSuppressed()
    {
        var vm = new BilingualScrollSyncViewModel { Suppressed = true };
        vm.StampIntent(sourceIsOrig: true, T0);
        Assert.False(vm.ShouldSync(sourceIsOrig: true, gridSurfaceActive: false, T0));
    }

    [Fact]
    public void ShouldSync_FalseWithoutRecentIntent()
    {
        // No stamp at all: a programmatic scroll must never drag the peer pane.
        var vm = new BilingualScrollSyncViewModel();
        Assert.False(vm.ShouldSync(sourceIsOrig: true, gridSurfaceActive: false, T0));
    }

    [Fact]
    public void ShouldSync_FalseWhenSyncInactive_EvenWithIntent()
    {
        var vm = new BilingualScrollSyncViewModel { ConfigEnabled = false, LayoutMode = ReadingLayoutMode.MergedFlow };
        vm.StampIntent(sourceIsOrig: true, T0);
        Assert.False(vm.ShouldSync(sourceIsOrig: true, gridSurfaceActive: false, T0));
    }

    [Fact]
    public void ShouldSync_SyncedPanesRunsEvenWithConfigOff()
    {
        var vm = new BilingualScrollSyncViewModel { ConfigEnabled = false, LayoutMode = ReadingLayoutMode.SyncedPanes };
        vm.StampIntent(sourceIsOrig: false, T0);
        Assert.True(vm.ShouldSync(sourceIsOrig: false, gridSurfaceActive: false, T0));
    }

    // ---- Shared-line-id lead/follow mapping ----

    private const string Header =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>T</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body><div1>";
    private const string Footer = "</div1></body></text></TEI>";
    private static RenderedDocument Render(string body) => TeiRenderer.Render(Header + body + Footer);

    private static readonly RenderedDocument Zh = Render(
        "<p><lb n=\"0001a01\" ed=\"T\"/>甲甲甲甲" +
        "<lb n=\"0001a02\" ed=\"T\"/>乙乙乙乙" +
        "<lb n=\"0001a03\" ed=\"T\"/>丙丙丙丙</p>");
    private static readonly RenderedDocument En = Render(
        "<p><lb n=\"0001a01\" ed=\"T\"/>First English line with much more text than the Chinese." +
        "<lb n=\"0001a02\" ed=\"T\"/>Second English line, also long." +
        "<lb n=\"0001a03\" ed=\"T\"/>Third English line here.</p>");

    [Fact]
    public void MapLeadToFollow_OrigLeads_MapsZhOffsetIntoEn()
    {
        var vm = new BilingualScrollSyncViewModel();
        Assert.True(Zh.TryGetSegmentByKey("lb|0001a02|T", out var zhSeg));
        Assert.True(En.TryGetSegmentByKey("lb|0001a02|T", out var enSeg));

        // sourceIsOrig = true → lead = Zh (orig), follow = En (tran).
        var mapped = vm.MapLeadToFollow(sourceIsOrig: true, orig: Zh, tran: En, srcOffset: zhSeg.Start);

        Assert.Equal(enSeg.Start, mapped);
    }

    [Fact]
    public void MapLeadToFollow_TranLeads_MapsEnOffsetIntoZh()
    {
        var vm = new BilingualScrollSyncViewModel();
        Assert.True(En.TryGetSegmentByKey("lb|0001a03|T", out var enSeg));
        Assert.True(Zh.TryGetSegmentByKey("lb|0001a03|T", out var zhSeg));

        // sourceIsOrig = false → lead = En (tran), follow = Zh (orig). Orientation is the
        // VM's job — same orig/tran args, only the flag flips.
        var mapped = vm.MapLeadToFollow(sourceIsOrig: false, orig: Zh, tran: En, srcOffset: enSeg.Start);

        Assert.Equal(zhSeg.Start, mapped);
    }

    [Fact]
    public void MapLeadToFollow_NoCounterpart_ReturnsNull()
    {
        var vm = new BilingualScrollSyncViewModel();
        var mapped = vm.MapLeadToFollow(sourceIsOrig: true, orig: Zh, tran: RenderedDocument.Empty, srcOffset: 0);
        Assert.Null(mapped);
    }
}
