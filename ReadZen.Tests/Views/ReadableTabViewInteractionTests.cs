using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

public class ReadableTabViewInteractionTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer]

    private static ReadableTabView CreateViewShell(out ReadableTabViewModel vm)
    {
        var view = (ReadableTabView)RuntimeHelpers.GetUninitializedObject(typeof(ReadableTabView));
        vm = new ReadableTabViewModel();

        SetField(view, "_vm", vm);
        SetField(view, "_appliedTags", new List<DocumentTag>());
        SetField(view, "_communityTags", new Dictionary<string, List<DocumentTag>>(StringComparer.OrdinalIgnoreCase));
        SetField(view, "_communityVocabularies", new Dictionary<string, TagVocabulary>(StringComparer.OrdinalIgnoreCase));

        return view;
    }

    private static RenderedDocument MakeDoc(string text = "abc")
        => new(
            text,
            new List<RenderSegment> { new("seg", 0, text.Length) },
            new List<DocAnnotation>(),
            new List<AnnotationMarkerInserter.MarkerSpan>());

    private static void SetField(object target, string name, object? value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name}");
        field.SetValue(target, value);
    }

    private static T GetField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name}");
        return (T)field.GetValue(target)!;
    }

    private static object? InvokePrivate(object target, string name, params object?[] args)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {name}");
        return method.Invoke(target, args);
    }


    [Fact]
    public void BtnDictionary_Click_RaisesDictionaryRequested()
    {
        var view = CreateViewShell(out _);
        var method = typeof(ReadableTabView).GetMethod("BtnDictionary_Click", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing BtnDictionary_Click");

        int raised = 0;
        view.DictionaryRequested += (_, _) => raised++;

        method.Invoke(view, new object?[] { null, new RoutedEventArgs() });

        Assert.Equal(1, raised);
    }

    [Fact]
    public void OnCompareTagsClicked_UsesReadableIdentityAndFiltersToCurrentFile()
    {
        var view = CreateViewShell(out var vm);
        vm.RenderOrig = MakeDoc();
        vm.SetZenContext("T01/test.xml", isZen: true);

        var myTags = GetField<List<DocumentTag>>(view, "_appliedTags");
        myTags.Add(new DocumentTag { RelPath = "T01/test.xml", TagId = "mine", FromLb = "0292a26", ToLb = "0292a27" });
        myTags.Add(new DocumentTag { RelPath = "T01/other.xml", TagId = "ignore", FromLb = "0292a28", ToLb = "0292a29" });

        SetField(view, "_selectedTagUser", "otheruser");
        SetField(view, "_communityTags", new Dictionary<string, List<DocumentTag>>(StringComparer.OrdinalIgnoreCase)
        {
            ["otheruser"] = new List<DocumentTag>
            {
                new() { RelPath = "T01/test.xml", TagId = "other", FromLb = "0292a26", ToLb = "0292a30" },
                new() { RelPath = "T01/ignore.xml", TagId = "skip", FromLb = "0292b01", ToLb = "0292b02" }
            }
        });
        SetField(view, "_communityVocabularies", new Dictionary<string, TagVocabulary>(StringComparer.OrdinalIgnoreCase)
        {
            ["otheruser"] = new TagVocabulary()
        });
        view.CurrentTagCompareIdentity = "octocat";
        view.CurrentTagUsername = "Alice";
        SetField(view, "_tagVocabulary", new TagVocabulary());

        CompareTagsRequestData? captured = null;
        view.CompareTagsRequested += (_, data) => captured = data;

        InvokePrivate(view, "OnCompareTagsClicked", null, new RoutedEventArgs());

        Assert.NotNull(captured);
        Assert.Equal("octocat", captured!.MyUsername);
        Assert.Equal("otheruser", captured.OtherUsername);
        Assert.Single(captured.MyTags);
        Assert.Single(captured.OtherTags);
        Assert.All(captured.MyTags, t => Assert.Equal("T01/test.xml", t.RelPath));
        Assert.All(captured.OtherTags, t => Assert.Equal("T01/test.xml", t.RelPath));
    }

    [Fact]
    public async Task ShowCommunityUserTags_SetsReadOnlyStatusAndDimsCodeBar()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var view = CreateViewShell(out var vm);
            vm.RenderOrig = MakeDoc();
            vm.RenderTran = MakeDoc("translated");
            vm.SetZenContext("T01/test.xml", isZen: true);

            var status = new TextBlock();
            var slots = new StackPanel();
            SetField(view, "_txtCodeBarStatus", status);
            SetField(view, "_codeBarSlots", slots);
            SetField(view, "_communityTags", new Dictionary<string, List<DocumentTag>>(StringComparer.OrdinalIgnoreCase)
            {
                ["otheruser"] = new List<DocumentTag>
                {
                    new() { RelPath = "T01/test.xml", TagId = "other", FromLb = "0292a26", ToLb = "0292a30" }
                }
            });
            SetField(view, "_communityVocabularies", new Dictionary<string, TagVocabulary>(StringComparer.OrdinalIgnoreCase)
            {
                ["otheruser"] = new TagVocabulary()
            });

            InvokePrivate(view, "ShowCommunityUserTags", "otheruser");

            Assert.Contains("Viewing otheruser's tags", status.Text);
            Assert.Contains("read-only", status.Text);
            Assert.Equal(0.35, slots.Opacity, 3);
        });
    }

    [Fact]
    public async Task CodingApplyTag_InCommunityMode_DoesNotApplyAndShowsReadOnlyStatus()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var view = CreateViewShell(out _);
            var status = new TextBlock();
            SetField(view, "_txtCodeBarStatus", status);
            SetField(view, "_selectedTagUser", "otheruser");
            SetField(view, "_tagVocabulary", new TagVocabulary
            {
                Tags = new List<TagDefinition> { new() { Id = "zen", Name = "Zen" } },
                Pages = new Dictionary<int, string?[]> { [1] = new string?[] { "zen", null, null, null, null, null, null, null, null } }
            });

            DocumentTag? applied = null;
            view.TagApplied += (_, tag) => applied = tag;

            InvokePrivate(view, "CodingApplyTag", 0);

            Assert.Null(applied);
            Assert.Contains("Viewing otheruser's tags", status.Text);
            Assert.Contains("read-only", status.Text);
        });
    }

    [Fact]
    public void TryFindSegmentRange_FindsReaderTmAnchorRange()
    {
        var method = typeof(ReadableTabView).GetMethod("TryFindSegmentRange", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing TryFindSegmentRange");

        var args = new object?[]
        {
            "ÃƒÂ¥Ã¢â‚¬Â°Ã‚ÂÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¦Ã‚Â³Ã¢â‚¬Â¢ÃƒÂ¥Ã‚Â¾Ã…â€™",
            "ÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¦Ã‚Â³Ã¢â‚¬Â¢",
            null,
            "ÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¦Ã‚Â³Ã¢â‚¬Â¢",
            0,
            0,
            "ÃƒÂ¥Ã¢â‚¬Â°Ã‚ÂÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¦Ã‚Â³Ã¢â‚¬Â¢ÃƒÂ¥Ã‚Â¾Ã…â€™",
            0,
            0
        };

        var ok = (bool)method.Invoke(null, args)!;

        Assert.True(ok);
        Assert.True((int)args[7]! >= 0);
        Assert.True((int)args[8]! > 0);
    }

    [Fact]
    public async Task ApplyTagDeepLinkAsync_WithoutTagId_SelectsRequestedCommunityLayer()
    {
        var view = CreateViewShell(out var vm);
        vm.SetZenContext("T01/test.xml", isZen: true);
        SetField(view, "_communityTags", new Dictionary<string, List<DocumentTag>>(StringComparer.OrdinalIgnoreCase)
        {
            ["otheruser"] = new List<DocumentTag>
            {
                new() { Id = "tag-link-1", RelPath = "T01/test.xml", TagId = "topic", FromLb = "0292a26", ToLb = "0292a27" }
            }
        });

        var ok = await view.ApplyTagDeepLinkAsync("otheruser", null);

        Assert.True(ok);
        Assert.Equal("otheruser", GetField<string?>(view, "_selectedTagUser"));
    }

    [Fact]
    public async Task ApplyTagDeepLinkAsync_WithoutTagId_SelectsOwnTagsLayer()
    {
        var view = CreateViewShell(out var vm);
        vm.SetZenContext("T01/test.xml", isZen: true);
        SetField(view, "_selectedTagUser", "otheruser");

        var ok = await view.ApplyTagDeepLinkAsync(null, null);

        Assert.True(ok);
        Assert.Null(GetField<string?>(view, "_selectedTagUser"));
    }

    // ---- Reading-layout gate: keys on historical-view state, not timeline visibility ----

    [Fact]
    public async Task IsReadingLayoutGated_VisibleTimelineAtPresent_NotGated()
    {
        // Regression: the timeline bar is visible for the whole lifetime of a CE
        // document (including present view), so gating on its visibility permanently
        // blocked merged-flow toggling. The gate now keys on _viewingHistorical.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var view = CreateViewShell(out var vm);
            vm.PendingRefresh = false;
            var bar = new CorrectionTimelineBar { IsVisible = true };
            SetField(view, "_correctionTimeline", bar);
            SetField(view, "_viewingHistorical", false);

            Assert.False((bool)InvokePrivate(view, "IsReadingLayoutGated")!);
        });
    }

    [Fact]
    public async Task IsReadingLayoutGated_ViewingHistorical_Gated()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var view = CreateViewShell(out var vm);
            vm.PendingRefresh = false;
            SetField(view, "_viewingHistorical", true);

            Assert.True((bool)InvokePrivate(view, "IsReadingLayoutGated")!);
        });
    }

    [Fact]
    public async Task IsReadingLayoutGated_PendingRefresh_Gated()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var view = CreateViewShell(out var vm);
            vm.PendingRefresh = true;
            SetField(view, "_viewingHistorical", false);

            Assert.True((bool)InvokePrivate(view, "IsReadingLayoutGated")!);
        });
    }

    // ---- Reader-state persistence is keyed by corpus-relative path, not absolute ----

    // ---- SyncedPanes is its own render strategy, no longer aliased to MergedFlow ----

    [Fact]
    public void RenderStrategyFor_SyncedPanes_DiffersFromMergedFlow()
    {
        // P8: the mapping moved to the pure static ReaderLayoutStrategy.For helper.
        var syncedStrategy = ReaderLayoutStrategy.For(ReadingLayoutMode.SyncedPanes);
        var mergedStrategy = ReaderLayoutStrategy.For(ReadingLayoutMode.MergedFlow);
        var pageStrategy = ReaderLayoutStrategy.For(ReadingLayoutMode.Page);

        // SyncedPanes must render distinctly from MergedFlow (the alias bug) ...
        Assert.NotEqual(mergedStrategy, syncedStrategy);
        // ... via its own strategy value (per-line synced two-pane), not Page's or Merged's.
        Assert.Equal("SyncedTwoPane", syncedStrategy.ToString());
        Assert.Equal("MergedTwoPane", mergedStrategy.ToString());
        Assert.Equal("PageTwoPane", pageStrategy.ToString());
        Assert.NotEqual(pageStrategy, syncedStrategy);
    }

    // ---- SyncedPanes forced sync overrides the global scroll-sync config toggle ----

    [Fact]
    public void SyncedPanes_ForcedSync_OverridesConfigOff_ThroughTheViewsConfigEntryPoint()
    {
        // The view pushes AppConfig.EnableBilingualScrollSync into the sync VM via
        // SetScrollSyncEnabled; SyncedPanes must still force sync on even when that config is off,
        // and expose the visible "linked scroll" affordance (IsSyncForcedByMode).
        var view = CreateViewShell(out var vm);

        view.SetScrollSyncEnabled(false);                       // user disabled scroll-sync globally
        vm.Reading.LayoutMode = ReadingLayoutMode.SyncedPanes;  // ... but picks SyncedPanes

        var sync = vm.Reading.ScrollSync;
        Assert.False(sync.ConfigEnabled);       // config really is off
        Assert.True(sync.ModeForcesSync);       // the mode forces it
        Assert.True(sync.IsSyncActive);         // ... so sync is active regardless
        Assert.True(sync.IsSyncForcedByMode);   // and the affordance chip shows
    }

    [Fact]
    public void OrdinaryMode_WithConfigOff_LeavesSyncInactive_ThroughTheView()
    {
        // Control: only SyncedPanes forces sync — other modes honor the (off) config.
        var view = CreateViewShell(out var vm);

        view.SetScrollSyncEnabled(false);
        vm.Reading.LayoutMode = ReadingLayoutMode.AlignedLines;

        var sync = vm.Reading.ScrollSync;
        Assert.False(sync.ModeForcesSync);
        Assert.False(sync.IsSyncActive);
        Assert.False(sync.IsSyncForcedByMode);
    }

    // ---- Mode-of-record is committed only AFTER a render that produced rows ----
    // TryReapplyPersistedGridSync is the synchronous first-paint commit path; it mirrors the
    // async ApplyRowGridLayoutAsync tail and advances _currentLayoutMode ONLY on the success
    // branch (after the model is built with >0 rows). Every early-out must leave the mode of
    // record and the render seq untouched so a failed/deferred render never lies about the mode.

    private static RenderedDocument LbDoc(string text, params (string key, int start, int end)[] segs)
    {
        var list = new List<RenderSegment>();
        foreach (var (key, start, end) in segs)
            list.Add(new RenderSegment(key, start, end));
        return new RenderedDocument(text, list, new List<DocAnnotation>(),
            new List<AnnotationMarkerInserter.MarkerSpan>());
    }

    private static bool InvokeTryReapply(ReadableTabView view, ReadingLayoutMode mode, ISegmentMapService? svc, string path)
    {
        var m = typeof(ReadableTabView).GetMethod("TryReapplyPersistedGridSync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing TryReapplyPersistedGridSync");
        return (bool)m.Invoke(view, new object?[] { mode, svc, path })!;
    }

    [Fact]
    public void TryReapplyPersistedGridSync_NonGridMode_DoesNotCommitMode()
    {
        var view = CreateViewShell(out _);
        Assert.Equal(ReadingLayoutMode.Page, GetField<ReadingLayoutMode>(view, "_currentLayoutMode"));

        var committed = InvokeTryReapply(view, ReadingLayoutMode.MergedFlow, null, "x.xml");

        Assert.False(committed);
        Assert.Equal(ReadingLayoutMode.Page, GetField<ReadingLayoutMode>(view, "_currentLayoutMode"));
        Assert.Equal(0, GetField<int>(view, "_readingLayoutRenderSeq")); // seq not claimed
    }

    [Fact]
    public void TryReapplyPersistedGridSync_EmptyDocs_DoesNotCommitMode()
    {
        var view = CreateViewShell(out var vm);
        vm.RenderOrig = RenderedDocument.Empty;
        vm.RenderTran = RenderedDocument.Empty;

        var committed = InvokeTryReapply(view, ReadingLayoutMode.AlignedLines, null, "x.xml");

        Assert.False(committed);
        Assert.Equal(ReadingLayoutMode.Page, GetField<ReadingLayoutMode>(view, "_currentLayoutMode"));
        Assert.Equal(0, GetField<int>(view, "_readingLayoutRenderSeq"));
    }

    [Fact]
    public void TryReapplyPersistedGridSync_MapGroupedMode_NoMap_DoesNotCommitMode()
    {
        // AlignedBlocks needs a segment map; with the service returning null the sync path defers
        // to the async downgrade path WITHOUT committing AlignedBlocks as the mode of record.
        var view = CreateViewShell(out var vm);
        vm.RenderOrig = LbDoc("初句\n二句", ("lb|0001a01", 0, 2), ("lb|0001a02", 3, 5));
        vm.RenderTran = LbDoc("first\nsecond", ("lb|0001a01", 0, 5), ("lb|0001a02", 6, 12));

        var committed = InvokeTryReapply(view, ReadingLayoutMode.AlignedBlocks, new NullSegmentMapService(), "x.xml");

        Assert.False(committed);
        Assert.Equal(ReadingLayoutMode.Page, GetField<ReadingLayoutMode>(view, "_currentLayoutMode"));
        Assert.Equal(0, GetField<int>(view, "_readingLayoutRenderSeq"));
    }

    [Fact]
    public void TryReapplyPersistedGridSync_ZeroRows_DoesNotCommitMode()
    {
        // A doc with no lb n-value keys builds zero rows; the sync path must NOT commit the mode
        // (the async path owns the documented Page fallback for a blank grid).
        var view = CreateViewShell(out var vm);
        vm.RenderOrig = LbDoc("abc", ("seg", 0, 3));   // non-empty text but no lb segments
        vm.RenderTran = LbDoc("xyz", ("seg", 0, 3));

        var committed = InvokeTryReapply(view, ReadingLayoutMode.AlignedLines, null, "x.xml");

        Assert.False(committed);
        Assert.Equal(ReadingLayoutMode.Page, GetField<ReadingLayoutMode>(view, "_currentLayoutMode"));
        Assert.Equal(0, GetField<int>(view, "_readingLayoutRenderSeq"));
    }

    [Fact]
    public void PersistLayoutMode_KeysOnRelativePath_NotAbsolute()
    {
        // Regression: persisting by absolute path orphaned all layout/resume state
        // when the portable install directory moved. Persistence now prefers relPath.
        var view = CreateViewShell(out var vm);
        var tmp = Path.Combine(Path.GetTempPath(), "readzen-readerstate-" + Guid.NewGuid().ToString("N") + ".json");
        var svc = new ReaderStateService(tmp);
        SetField(view, "_readerStateService", svc);
        vm.CurrentRelPathForZen = "T01/test.xml";
        SetField(view, "_provenanceXmlAbsPath", @"C:\portable\install\CbetaZenTexts\xml-p5\T01\test.xml");

        try
        {
            // Use a mode distinct from the default (MergedFlow, post A2 flip) so the
            // absolute-key lookup returning the default is unambiguously "not stored here".
            InvokePrivate(view, "PersistLayoutMode", ReadingLayoutMode.SyncedPanes);

            // Stored under the relative key...
            Assert.Equal(ReadingLayoutMode.SyncedPanes, svc.GetLayoutMode("T01/test.xml"));
            // ...NOT the machine-specific absolute path (unknown key → default MergedFlow).
            Assert.Equal(ReadingLayoutMode.MergedFlow,
                svc.GetLayoutMode(@"C:\portable\install\CbetaZenTexts\xml-p5\T01\test.xml"));
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }
}

/// <summary>A segment-map service that never resolves a map — the map-missing case that drives
/// the AlignedBlocks->AlignedLines / MergedStacked->Interleaved downgrade ladder.</summary>
internal sealed class NullSegmentMapService : ISegmentMapService
{
    public SegmentMap? TryLoad(string xmlAbsPath) => null;
}



