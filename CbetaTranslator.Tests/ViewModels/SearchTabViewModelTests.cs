using System.Collections.Generic;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class SearchTabViewModelTests
{
    private static SearchTabViewModel MakeVm()
    {
        return new SearchTabViewModel(new StubSearchIndexService());
    }

    // ---- Initial state ----

    [Fact]
    public void InitialState_HasDefaults()
    {
        var vm = MakeVm();

        Assert.Equal("", vm.Query);
        Assert.Contains("Index not loaded", vm.ProgressText);
        Assert.Equal("Ready.", vm.SummaryText);
        Assert.False(vm.IsSearching);
        Assert.False(vm.IsBuildingIndex);
        Assert.True(vm.SearchOriginal);
        Assert.False(vm.SearchTranslated);
        Assert.False(vm.ZenOnly);
        Assert.False(vm.IsCancelEnabled);
        Assert.False(vm.IsExportEnabled);
        Assert.Empty(vm.ResultGroups);
    }

    // ---- StatusItems / ContextItems arrays ----

    [Fact]
    public void StatusItems_HasFourEntries()
    {
        var vm = MakeVm();
        Assert.Equal(4, vm.StatusItems.Length);
        Assert.Equal("All", vm.StatusItems[0]);
    }

    [Fact]
    public void ContextItems_HasThreeEntries()
    {
        var vm = MakeVm();
        Assert.Equal(3, vm.ContextItems.Length);
    }

    [Fact]
    public void CoocMetricItems_HasNineEntries()
    {
        var vm = MakeVm();
        Assert.Equal(9, vm.CoocMetricItems.Length);
    }

    // ---- SetRootContext ----

    [Fact]
    public void SetRootContext_SetsInternalState()
    {
        var vm = MakeVm();
        vm.SetRootContext("/root", "/orig", "/tran");

        // No direct public accessor but Clear should reset
        // This just verifies no exceptions
    }

    // ---- SetFileIndex ----

    [Fact]
    public void SetFileIndex_AcceptsEmptyList()
    {
        var vm = MakeVm();
        vm.SetFileIndex(new List<FileNavItem>());
    }

    [Fact]
    public void SetFileIndex_AcceptsNull()
    {
        var vm = MakeVm();
        vm.SetFileIndex(null!);
    }

    // ---- SetZenResolver ----

    [Fact]
    public void SetZenResolver_AcceptsNull()
    {
        var vm = MakeVm();
        vm.SetZenResolver(null);
    }

    [Fact]
    public void SetZenResolver_AcceptsFunc()
    {
        var vm = MakeVm();
        vm.SetZenResolver(rel => rel.Contains("zen"));
    }

    // ---- Clear ----

    [Fact]
    public void Clear_ResetsState()
    {
        var vm = MakeVm();
        vm.SetRootContext("/root", "/orig", "/tran");
        vm.Query = "test";
        vm.ZenOnly = true;

        vm.Clear();

        Assert.Contains("No root loaded", vm.ProgressText);
        Assert.Equal("Ready.", vm.SummaryText);
        Assert.False(vm.IsExportEnabled);
        Assert.False(vm.ZenOnly);
        Assert.Empty(vm.ResultGroups);
    }

    // ---- SetForceRebuild ----

    [Fact]
    public void SetForceRebuild_DoesNotThrow()
    {
        var vm = MakeVm();
        vm.SetForceRebuild();
    }

    // ---- HandleResultDoubleTap ----

    [Fact]
    public void HandleResultDoubleTap_Null_DoesNotThrow()
    {
        var vm = MakeVm();
        vm.HandleResultDoubleTap(null);
    }

    [Fact]
    public void HandleResultDoubleTap_SearchResultGroup_FiresNavigationRequested()
    {
        var vm = MakeVm();
        NavigationRequest? received = null;
        vm.NavigationRequested += (_, req) => received = req;

        var group = new SearchResultGroup
        {
            RelPath = "test/file.xml",
            DisplayName = "Test File"
        };

        vm.HandleResultDoubleTap(group);

        Assert.NotNull(received);
        Assert.Equal("test/file.xml", received!.RelPath);
    }

    // ---- Empty state and validation ----

    [Fact]
    public void InitialState_IsEmptyStateVisible_IsTrue()
    {
        var vm = MakeVm();
        Assert.True(vm.IsEmptyStateVisible);
    }

    [Fact]
    public void InitialState_HasValidationError_IsFalse()
    {
        var vm = MakeVm();
        Assert.False(vm.HasValidationError);
    }

    [Fact]
    public void InitialState_ValidationMessage_IsEmpty()
    {
        var vm = MakeVm();
        Assert.Equal("", vm.ValidationMessage);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_SetsHasValidationError()
    {
        var vm = MakeVm();
        vm.SetContext("/root", "/orig", "/tran", rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "";

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.True(vm.HasValidationError);
        Assert.False(string.IsNullOrEmpty(vm.ValidationMessage));
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ValidationMessageIsNonEmpty()
    {
        var vm = MakeVm();
        vm.SetContext("/root", "/orig", "/tran", rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "   ";

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.True(vm.HasValidationError);
        Assert.NotEqual("", vm.ValidationMessage);
    }

    [Fact]
    public async Task SearchAsync_NeitherOriginalNorTranslated_SetsValidationError()
    {
        var vm = MakeVm();
        vm.SetContext("/root", "/orig", "/tran", rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "test";
        vm.SearchOriginal = false;
        vm.SearchTranslated = false;

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.True(vm.HasValidationError);
        Assert.Contains("Original", vm.ValidationMessage);
    }

    [Fact]
    public void IsEmptyStateVisible_PropertyChanged_Fires()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.IsEmptyStateVisible = false;

        Assert.Contains("IsEmptyStateVisible", changed);
    }

    [Fact]
    public void HasValidationError_PropertyChanged_Fires()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.HasValidationError = true;

        Assert.Contains("HasValidationError", changed);
    }

    // ---- PropertyChanged notifications ----

    [Fact]
    public void PropertyChanged_FiredForQuery()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.Query = "test";

        Assert.Contains("Query", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForProgressText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ProgressText = "Building...";

        Assert.Contains("ProgressText", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForZenOnly()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ZenOnly = true;

        Assert.Contains("ZenOnly", changed);
    }
}
