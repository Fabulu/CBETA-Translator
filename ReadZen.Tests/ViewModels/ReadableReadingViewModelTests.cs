// ReadableReadingViewModelTests — the reading-surface sub-VM extracted in R2.1:
// layout-mode <-> selected-option sync, the request/echo split, the view-mode / line-id
// intents (Wave A), and the bookmark collection + per-row commands.

using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

[Trait("Domain", "Reader")]
public class ReadableReadingViewModelTests
{
    // ---- Layout mode default + selected-option ----

    [Fact]
    public void LayoutMode_DefaultsToMergedFlow()
    {
        // A2 default-flip: MergedFlow is the SPA-parity default preference (was Page).
        var vm = new ReadableReadingViewModel();
        Assert.Equal(ReadingLayoutMode.MergedFlow, vm.LayoutMode);
    }

    [Fact]
    public void SelectedLayoutOption_MirrorsLayoutMode()
    {
        var vm = new ReadableReadingViewModel { LayoutMode = ReadingLayoutMode.Page };
        Assert.NotNull(vm.SelectedLayoutOption);
        Assert.Equal(ReadingLayoutMode.Page, vm.SelectedLayoutOption!.Mode);
        Assert.False(vm.SelectedLayoutOption.IsHeader);

        vm.LayoutMode = ReadingLayoutMode.Interleaved;
        Assert.Equal(ReadingLayoutMode.Interleaved, vm.SelectedLayoutOption!.Mode);
    }

    [Fact]
    public void SettingSelectedLayoutOption_UpdatesMode_AndRaisesRequest()
    {
        var vm = new ReadableReadingViewModel();
        var requested = new List<ReadingLayoutMode>();
        vm.LayoutModeChangeRequested += (_, m) => requested.Add(m);

        vm.SelectedLayoutOption = vm.LayoutModeOptions.First(o => o.Mode == ReadingLayoutMode.SyncedPanes && !o.IsHeader);

        Assert.Equal(ReadingLayoutMode.SyncedPanes, vm.LayoutMode);
        Assert.Equal(new[] { ReadingLayoutMode.SyncedPanes }, requested);
    }

    [Fact]
    public void SettingSelectedLayoutOption_ToHeader_IsIgnored()
    {
        var vm = new ReadableReadingViewModel { LayoutMode = ReadingLayoutMode.Page };
        bool raised = false;
        vm.LayoutModeChangeRequested += (_, _) => raised = true;

        vm.SelectedLayoutOption = vm.LayoutModeOptions.First(o => o.IsHeader);

        Assert.Equal(ReadingLayoutMode.Page, vm.LayoutMode); // unchanged
        Assert.False(raised);
    }

    [Fact]
    public void LayoutModeOptions_HasTwoHeaders_AndAllSevenModes()
    {
        var vm = new ReadableReadingViewModel();
        Assert.Equal(2, vm.LayoutModeOptions.Count(o => o.IsHeader));
        var modes = vm.LayoutModeOptions.Where(o => !o.IsHeader).Select(o => o.Mode).ToList();
        Assert.Equal(7, modes.Count);
        Assert.Contains(ReadingLayoutMode.Page, modes);
        Assert.Contains(ReadingLayoutMode.MergedStacked, modes);
    }

    [Fact]
    public void OnLayoutModeChanged_RaisesRequest()
    {
        var vm = new ReadableReadingViewModel { LayoutMode = ReadingLayoutMode.Page };
        ReadingLayoutMode? got = null;
        vm.LayoutModeChangeRequested += (_, m) => got = m;

        vm.LayoutMode = ReadingLayoutMode.MergedFlow;

        Assert.Equal(ReadingLayoutMode.MergedFlow, got);
    }

    [Fact]
    public void SetLayoutModeQuietly_DoesNotRaiseRequest()
    {
        var vm = new ReadableReadingViewModel { LayoutMode = ReadingLayoutMode.Page };
        bool raised = false;
        vm.LayoutModeChangeRequested += (_, _) => raised = true;

        vm.SetLayoutModeQuietly(ReadingLayoutMode.MergedFlow);

        Assert.Equal(ReadingLayoutMode.MergedFlow, vm.LayoutMode);
        Assert.False(raised);
    }

    [Fact]
    public void SetLayoutModeQuietly_NoOpWhenUnchanged()
    {
        var vm = new ReadableReadingViewModel { LayoutMode = ReadingLayoutMode.Page };
        int changes = 0;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.LayoutMode)) changes++; };

        vm.SetLayoutModeQuietly(ReadingLayoutMode.Page); // already Page

        Assert.Equal(0, changes);
    }

    // ---- View mode (ZH / Both / EN) ----

    [Fact]
    public void ViewModeIndex_DefaultsToBoth()
    {
        var vm = new ReadableReadingViewModel();
        Assert.Equal(1, vm.ViewModeIndex);
    }

    [Theory]
    [InlineData(0, ReaderViewMode.Zh)]
    [InlineData(1, ReaderViewMode.Both)]
    [InlineData(2, ReaderViewMode.En)]
    public void SettingViewModeIndex_RaisesRequestWithMappedMode(int index, ReaderViewMode expected)
    {
        var vm = new ReadableReadingViewModel();
        vm.SetViewModeQuietly((index + 1) % 3); // start from a different index so the set is a real change
        ReaderViewMode? got = null;
        vm.ViewModeChangeRequested += (_, m) => got = m;

        vm.ViewModeIndex = index;

        Assert.Equal(expected, got);
    }

    [Fact]
    public void SetViewModeQuietly_DoesNotRaiseRequest()
    {
        var vm = new ReadableReadingViewModel();
        bool raised = false;
        vm.ViewModeChangeRequested += (_, _) => raised = true;

        vm.SetViewModeQuietly(2);

        Assert.Equal(2, vm.ViewModeIndex);
        Assert.False(raised);
    }

    // ---- Line-id toggle ----

    [Fact]
    public void ShowLineIds_RaisesRequest()
    {
        var vm = new ReadableReadingViewModel();
        bool? got = null;
        vm.ShowLineIdsChangeRequested += (_, v) => got = v;

        vm.ShowLineIds = true;

        Assert.True(got);
    }

    [Fact]
    public void SetShowLineIdsQuietly_DoesNotRaiseRequest()
    {
        var vm = new ReadableReadingViewModel();
        bool raised = false;
        vm.ShowLineIdsChangeRequested += (_, _) => raised = true;

        vm.SetShowLineIdsQuietly(true);

        Assert.True(vm.ShowLineIds);
        Assert.False(raised);
    }

    // ---- Reading progress ----

    [Fact]
    public void ReadingProgressText_RaisesPropertyChanged()
    {
        var vm = new ReadableReadingViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ReadingProgressText = "Line 3/10 (30%)";

        Assert.Contains(nameof(vm.ReadingProgressText), changed);
    }

    // ---- Bookmarks ----

    [Fact]
    public void SetBookmarks_PopulatesCollection_AndHasBookmarks()
    {
        var vm = new ReadableReadingViewModel();
        Assert.False(vm.HasBookmarks);

        vm.SetBookmarks(new[] { MakeItem("a"), MakeItem("b") });

        Assert.Equal(2, vm.Bookmarks.Count);
        Assert.True(vm.HasBookmarks);
    }

    [Fact]
    public void SetBookmarks_ReplacesPrevious()
    {
        var vm = new ReadableReadingViewModel();
        vm.SetBookmarks(new[] { MakeItem("a"), MakeItem("b") });

        vm.SetBookmarks(new[] { MakeItem("c") });

        Assert.Single(vm.Bookmarks);
        Assert.Equal("c", vm.Bookmarks[0].DisplayLabel);
    }

    [Fact]
    public void SetBookmarks_Empty_ClearsHasBookmarks()
    {
        var vm = new ReadableReadingViewModel();
        vm.SetBookmarks(new[] { MakeItem("a") });

        vm.SetBookmarks(System.Array.Empty<ReadableBookmarkItem>());

        Assert.False(vm.HasBookmarks);
    }

    [Fact]
    public void AddBookmarkCommand_RaisesAddRequested()
    {
        var vm = new ReadableReadingViewModel();
        int raised = 0;
        vm.AddBookmarkRequested += (_, _) => raised++;

        vm.AddBookmarkCommand.Execute(null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemNavigateCommand_InvokesCallback()
    {
        ReadableBookmarkItem? navigated = null;
        var item = MakeItem("x", navigate: it => navigated = it);

        item.NavigateCommand.Execute(null);

        Assert.Same(item, navigated);
    }

    [Fact]
    public void ItemRemoveCommand_InvokesCallback()
    {
        ReadableBookmarkItem? removed = null;
        var item = MakeItem("x", remove: it => removed = it);

        item.RemoveCommand.Execute(null);

        Assert.Same(item, removed);
    }

    [Fact]
    public void Item_LabelOpacity_DimsCrossFile()
    {
        Assert.Equal(1.0, MakeItem("here", sameFile: true).LabelOpacity);
        Assert.Equal(0.8, MakeItem("away", sameFile: false).LabelOpacity);
    }

    private static ReadableBookmarkItem MakeItem(
        string label,
        bool sameFile = true,
        System.Action<ReadableBookmarkItem>? navigate = null,
        System.Action<ReadableBookmarkItem>? remove = null)
        => new(
            new Bookmark { RelPath = "T/T2076_.xml", Label = label },
            label,
            sameFile,
            navigate ?? (_ => { }),
            remove ?? (_ => { }));
}
