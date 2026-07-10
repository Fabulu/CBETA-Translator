// ReadableReadingViewModelTests — the reading-surface sub-VM extracted in R2.1:
// layout-mode <-> index sync, the request/echo split, and the bookmark collection +
// per-row commands.

using System.Collections.Generic;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

[Trait("Domain", "Reader")]
public class ReadableReadingViewModelTests
{
    // ---- Layout mode <-> index ----

    [Fact]
    public void LayoutModeIndex_MirrorsEnum()
    {
        var vm = new ReadableReadingViewModel();
        Assert.Equal(0, vm.LayoutModeIndex);

        vm.LayoutMode = ReadingLayoutMode.MergedFlow;
        Assert.Equal(1, vm.LayoutModeIndex);
    }

    [Fact]
    public void SettingLayoutModeIndex_UpdatesEnum_AndRaisesRequest()
    {
        var vm = new ReadableReadingViewModel();
        var requested = new List<ReadingLayoutMode>();
        vm.LayoutModeChangeRequested += (_, m) => requested.Add(m);

        vm.LayoutModeIndex = 1;

        Assert.Equal(ReadingLayoutMode.MergedFlow, vm.LayoutMode);
        Assert.Equal(new[] { ReadingLayoutMode.MergedFlow }, requested);
    }

    [Fact]
    public void LayoutModeIndex_OutOfRange_ClampsToPage()
    {
        var vm = new ReadableReadingViewModel { LayoutMode = ReadingLayoutMode.MergedFlow };

        vm.LayoutModeIndex = 5; // anything but 1 means page

        Assert.Equal(ReadingLayoutMode.Page, vm.LayoutMode);
    }

    [Fact]
    public void OnLayoutModeChanged_RaisesRequest()
    {
        var vm = new ReadableReadingViewModel();
        ReadingLayoutMode? got = null;
        vm.LayoutModeChangeRequested += (_, m) => got = m;

        vm.LayoutMode = ReadingLayoutMode.MergedFlow;

        Assert.Equal(ReadingLayoutMode.MergedFlow, got);
    }

    [Fact]
    public void SetLayoutModeQuietly_DoesNotRaiseRequest()
    {
        var vm = new ReadableReadingViewModel();
        bool raised = false;
        vm.LayoutModeChangeRequested += (_, _) => raised = true;

        vm.SetLayoutModeQuietly(ReadingLayoutMode.MergedFlow);

        Assert.Equal(ReadingLayoutMode.MergedFlow, vm.LayoutMode);
        Assert.Equal(1, vm.LayoutModeIndex);
        Assert.False(raised);
    }

    [Fact]
    public void SetLayoutModeQuietly_NoOpWhenUnchanged()
    {
        var vm = new ReadableReadingViewModel();
        int changes = 0;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.LayoutMode)) changes++; };

        vm.SetLayoutModeQuietly(ReadingLayoutMode.Page); // already Page

        Assert.Equal(0, changes);
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
