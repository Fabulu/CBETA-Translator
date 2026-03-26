using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class TermbaseEditorWindowViewModelTests
{
    private static StubTermbaseStorageService MakeStorage(params TermbaseEntry[] entries)
    {
        return new StubTermbaseStorageService { Entries = entries.ToList() };
    }

    private static TermbaseEditorWindowViewModel MakeVm(StubTermbaseStorageService? storage = null)
    {
        return new TermbaseEditorWindowViewModel(storage ?? new StubTermbaseStorageService(), "/root");
    }

    // ---- Constructor ----

    [Fact]
    public void Constructor_ThrowsOnNullStorage()
    {
        Assert.Throws<ArgumentNullException>(() => new TermbaseEditorWindowViewModel(null!, "/root"));
    }

    [Fact]
    public void Constructor_ThrowsOnNullRoot()
    {
        Assert.Throws<ArgumentNullException>(() => new TermbaseEditorWindowViewModel(new StubTermbaseStorageService(), null!));
    }

    // ---- Load ----

    [Fact]
    public async Task LoadAsync_PopulatesEntriesAndSetsStatus()
    {
        var storage = MakeStorage(
            new TermbaseEntry { SourceTerm = "ABC", PreferredTarget = "abc" },
            new TermbaseEntry { SourceTerm = "DEF", PreferredTarget = "def" }
        );
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.AllEntries.Count);
        Assert.Equal(2, vm.FilteredEntries.Count);
        Assert.Contains("Loaded 2", vm.StatusMessage);
        Assert.NotNull(vm.SelectedEntry);
    }

    [Fact]
    public async Task LoadAsync_EmptyList_SetsSelectedToNull()
    {
        var vm = MakeVm();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Empty(vm.AllEntries);
        Assert.Null(vm.SelectedEntry);
    }

    [Fact]
    public async Task LoadAsync_StorageThrows_SetsErrorStatus()
    {
        var storage = new StubTermbaseStorageService { ThrowOnLoad = true };
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Contains("Load failed", vm.StatusMessage);
    }

    // ---- NewTerm ----

    [Fact]
    public void NewTerm_AddsEntryAndSelectsIt()
    {
        var vm = MakeVm();

        vm.NewTermCommand.Execute(null);

        Assert.Single(vm.AllEntries);
        Assert.NotNull(vm.SelectedEntry);
        Assert.Contains("New term created", vm.StatusMessage);
    }

    [Fact]
    public void NewTerm_RequestsFocusOnSourceTerm()
    {
        var vm = MakeVm();
        bool focusRequested = false;
        vm.FocusSourceTermRequested = () => focusRequested = true;

        vm.NewTermCommand.Execute(null);

        Assert.True(focusRequested);
    }

    // ---- DeleteTerm ----

    [Fact]
    public void DeleteTerm_RemovesSelectedEntry()
    {
        var vm = MakeVm();
        vm.NewTermCommand.Execute(null);
        Assert.Single(vm.AllEntries);

        vm.DeleteTermCommand.Execute(null);

        Assert.Empty(vm.AllEntries);
        Assert.Null(vm.SelectedEntry);
        Assert.Contains("deleted", vm.StatusMessage);
    }

    [Fact]
    public void DeleteTerm_NoSelection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedEntry = null;

        vm.DeleteTermCommand.Execute(null);

        Assert.Empty(vm.AllEntries);
    }

    // ---- DuplicateTerm ----

    [Fact]
    public void DuplicateTerm_CopiesEntry()
    {
        var vm = MakeVm();
        vm.NewTermCommand.Execute(null);
        vm.SourceTerm = "Test";
        vm.PreferredTarget = "Test EN";

        vm.DuplicateTermCommand.Execute(null);

        Assert.Equal(2, vm.AllEntries.Count);
        Assert.Contains("duplicated", vm.StatusMessage);
    }

    [Fact]
    public void DuplicateTerm_NoSelection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedEntry = null;

        vm.DuplicateTermCommand.Execute(null);

        Assert.Empty(vm.AllEntries);
    }

    // ---- Save ----

    [Fact]
    public async Task SaveAsync_SavesCleanedEntries()
    {
        var storage = new StubTermbaseStorageService();
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");

        vm.NewTermCommand.Execute(null);
        vm.SourceTerm = "Hello";
        vm.PreferredTarget = "World";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.Saved);
        Assert.NotNull(storage.LastSaved);
        Assert.Single(storage.LastSaved!);
        Assert.Contains("Saved", vm.StatusMessage);
    }

    [Fact]
    public async Task SaveAsync_EntryWithoutSourceTerm_Blocked()
    {
        var storage = new StubTermbaseStorageService();
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");

        vm.NewTermCommand.Execute(null);
        vm.SourceTerm = "";
        vm.PreferredTarget = "Something";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.Saved);
        Assert.Contains("source term", vm.StatusMessage);
    }

    [Fact]
    public async Task SaveAsync_StorageThrows_SetsErrorStatus()
    {
        var storage = new StubTermbaseStorageService { ThrowOnSave = true };
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");

        vm.NewTermCommand.Execute(null);
        vm.SourceTerm = "Valid";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.Saved);
        Assert.Contains("Save failed", vm.StatusMessage);
    }

    [Fact]
    public async Task SaveAsync_FiresTermsSavedEvent()
    {
        var storage = new StubTermbaseStorageService();
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        bool eventFired = false;
        vm.TermsSaved += (_, _) => eventFired = true;

        vm.NewTermCommand.Execute(null);
        vm.SourceTerm = "Valid";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(eventFired);
    }

    // ---- Filter ----

    [Fact]
    public async Task SearchQuery_FiltersEntries()
    {
        var storage = MakeStorage(
            new TermbaseEntry { SourceTerm = "Alpha", PreferredTarget = "alpha" },
            new TermbaseEntry { SourceTerm = "Beta", PreferredTarget = "beta" }
        );
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SearchQuery = "Alpha";

        Assert.Single(vm.FilteredEntries);
        Assert.Equal("Alpha", vm.FilteredEntries[0].SourceTerm);
    }

    [Fact]
    public async Task SearchQuery_Empty_ShowsAll()
    {
        var storage = MakeStorage(
            new TermbaseEntry { SourceTerm = "Alpha" },
            new TermbaseEntry { SourceTerm = "Beta" }
        );
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SearchQuery = "Alpha";
        Assert.Single(vm.FilteredEntries);

        vm.SearchQuery = "";
        Assert.Equal(2, vm.FilteredEntries.Count);
    }

    // ---- Field sync ----

    [Fact]
    public void SelectedEntry_LoadsFieldsIntoProperties()
    {
        var vm = MakeVm();

        var entry = new TermbaseEntry
        {
            SourceTerm = "Src",
            PreferredTarget = "Tgt",
            Status = "allowed",
            Note = "A note",
            AlternateTargets = new List<string> { "Alt1", "Alt2" }
        };

        vm.AllEntries.Add(entry);
        vm.SelectedEntry = entry;

        Assert.Equal("Src", vm.SourceTerm);
        Assert.Equal("Tgt", vm.PreferredTarget);
        Assert.Equal(1, vm.SelectedStatusIndex); // "allowed" = index 1
        Assert.Equal("A note", vm.NoteText);
        Assert.Contains("Alt1", vm.AlternatesText);
        Assert.Contains("Alt2", vm.AlternatesText);
    }

    [Fact]
    public void SelectedEntry_Null_ClearsFields()
    {
        var vm = MakeVm();
        vm.NewTermCommand.Execute(null);
        vm.SourceTerm = "Something";

        vm.SelectedEntry = null;

        Assert.Equal("", vm.SourceTerm);
        Assert.Equal("", vm.PreferredTarget);
        Assert.Equal("", vm.NoteText);
        Assert.Equal(0, vm.SelectedStatusIndex);
    }

    // ---- CloseWindow ----

    [Fact]
    public void CloseWindow_FiresCloseRequested()
    {
        var vm = MakeVm();
        bool closeCalled = false;
        vm.CloseRequested = () => closeCalled = true;

        vm.CloseWindowCommand.Execute(null);

        Assert.True(closeCalled);
    }

    // ---- Sort order ----

    [Fact]
    public async Task FilteredEntries_SortedBySourceTermThenTarget()
    {
        var storage = MakeStorage(
            new TermbaseEntry { SourceTerm = "Zebra", PreferredTarget = "z" },
            new TermbaseEntry { SourceTerm = "Alpha", PreferredTarget = "a" },
            new TermbaseEntry { SourceTerm = "Alpha", PreferredTarget = "b" }
        );
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Alpha", vm.FilteredEntries[0].SourceTerm);
        Assert.Equal("a", vm.FilteredEntries[0].PreferredTarget);
        Assert.Equal("Alpha", vm.FilteredEntries[1].SourceTerm);
        Assert.Equal("b", vm.FilteredEntries[1].PreferredTarget);
        Assert.Equal("Zebra", vm.FilteredEntries[2].SourceTerm);
    }
}
