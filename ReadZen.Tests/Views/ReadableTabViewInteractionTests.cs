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
            InvokePrivate(view, "PersistLayoutMode", ReadingLayoutMode.MergedFlow);

            // Stored under the relative key...
            Assert.Equal(ReadingLayoutMode.MergedFlow, svc.GetLayoutMode("T01/test.xml"));
            // ...NOT the machine-specific absolute path.
            Assert.Equal(ReadingLayoutMode.Page,
                svc.GetLayoutMode(@"C:\portable\install\CbetaZenTexts\xml-p5\T01\test.xml"));
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }
}



