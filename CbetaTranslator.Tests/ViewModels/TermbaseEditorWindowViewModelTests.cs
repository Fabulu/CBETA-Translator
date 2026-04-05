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

    [Fact]
    public async Task ConfigureLanding_Term_SelectsExactLocalMatchAfterLoad()
    {
        var storage = MakeStorage(
            new TermbaseEntry { SourceTerm = "Alpha", PreferredTarget = "first" },
            new TermbaseEntry { SourceTerm = "Beta", PreferredTarget = "second" },
            new TermbaseEntry { SourceTerm = "Gamma", PreferredTarget = "third" }
        );
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        vm.ConfigureLanding("Beta");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Beta", vm.SearchQuery);
        Assert.Single(vm.FilteredEntries);
        Assert.Equal("Beta", vm.SelectedEntry!.SourceTerm);
    }

    [Fact]
    public async Task ConfigureLanding_PreferredTarget_SelectsMatchingLocalEntry()
    {
        var storage = MakeStorage(
            new TermbaseEntry { SourceTerm = "fo", PreferredTarget = "Buddha" },
            new TermbaseEntry { SourceTerm = "fa", PreferredTarget = "Dharma" }
        );
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        vm.ConfigureLanding("Dharma");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Dharma", vm.SearchQuery);
        Assert.Single(vm.FilteredEntries);
        Assert.Equal("fa", vm.SelectedEntry!.SourceTerm);
    }

    [Fact]
    public async Task ConfigureLanding_WithCommunityUserBias_FiltersAndSelectsCommunityMatch()
    {
        var storage = new StubTermbaseStorageService
        {
            Entries = new List<TermbaseEntry>
            {
                new() { SourceTerm = "Alpha", PreferredTarget = "alpha" },
                new() { SourceTerm = "Beta", PreferredTarget = "beta" }
            },
            CommunityEntriesByUser = new Dictionary<string, List<TermbaseEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                ["bob"] = new()
                {
                    new() { SourceTerm = "koan", PreferredTarget = "case", CreatedBy = "bob" },
                    new() { SourceTerm = "other", PreferredTarget = "misc", CreatedBy = "bob" }
                },
                ["carol"] = new()
                {
                    new() { SourceTerm = "koan", PreferredTarget = "public case", CreatedBy = "carol" }
                }
            }
        };
        var vm = new TermbaseEditorWindowViewModel(storage, "/root");
        vm.ConfigureLanding("koan", "bob");

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("koan", vm.SearchQuery);
        Assert.Equal("koan", vm.CommunityFilter);
        Assert.Equal(1, vm.SelectedCommunityUserIndex);
        Assert.Equal(new[] { "All Users", "bob", "carol" }, vm.CommunityUsernames);
        Assert.Single(vm.CommunityEntries);
        Assert.Equal("bob", vm.SelectedCommunityEntry!.CreatedBy);
        Assert.Equal("koan", vm.SelectedCommunityEntry.SourceTerm);
    }
    // ---- 11. AdoptSelectedTerm — copies entry to local with user's CreatedBy ----

    [Fact]
    public void AdoptSelectedTerm_CopiesEntryToLocalWithUsersCreatedBy()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");

        var communityEntry = new TermbaseEntry
        {
            SourceTerm = "\u4f5b",
            PreferredTarget = "Buddha",
            AlternateTargets = new List<string> { "Awakened One" },
            Status = "preferred",
            Note = "Original note",
            CreatedBy = "Bob"
        };

        vm.SelectedCommunityEntry = communityEntry;
        vm.AdoptSelectedTermCommand.Execute(null);

        Assert.Single(vm.AllEntries);
        var adopted = vm.AllEntries[0];
        Assert.Equal("\u4f5b", adopted.SourceTerm);
        Assert.Equal("Buddha", adopted.PreferredTarget);
        Assert.Single(adopted.AlternateTargets);
        Assert.Contains("Awakened One", adopted.AlternateTargets);
        Assert.Equal("preferred", adopted.Status);
        Assert.Equal("Original note", adopted.Note);
        Assert.Equal("Alice", adopted.CreatedBy);
        Assert.NotNull(adopted.WrittenUtc);
        Assert.Contains("Adopted", vm.StatusMessage);
    }

    [Fact]
    public void AdoptSelectedTerm_NullSelection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");
        vm.SelectedCommunityEntry = null;

        vm.AdoptSelectedTermCommand.Execute(null);

        Assert.Empty(vm.AllEntries);
    }

    [Fact]
    public void AdoptSelectedTerm_SelectsAdoptedEntry()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");

        vm.SelectedCommunityEntry = new TermbaseEntry
        {
            SourceTerm = "Test",
            PreferredTarget = "test"
        };

        vm.AdoptSelectedTermCommand.Execute(null);

        Assert.NotNull(vm.SelectedEntry);
        Assert.Equal("Test", vm.SelectedEntry!.SourceTerm);
    }

    // ---- 12. AdoptSelectedTerm — warns about duplicate SourceTerm ----

    [Fact]
    public void AdoptSelectedTerm_WarnsAboutDuplicateSourceTerm()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");

        // Add an existing entry with the same SourceTerm
        vm.AllEntries.Add(new TermbaseEntry
        {
            SourceTerm = "\u4f5b",
            PreferredTarget = "Existing"
        });

        vm.SelectedCommunityEntry = new TermbaseEntry
        {
            SourceTerm = "\u4f5b",
            PreferredTarget = "Buddha",
            CreatedBy = "Bob"
        };

        vm.AdoptSelectedTermCommand.Execute(null);

        // Should still adopt (adds it)
        Assert.Equal(2, vm.AllEntries.Count);
        // Status message should warn about duplicate
        Assert.Contains("already exists", vm.StatusMessage);
    }

    [Fact]
    public void AdoptSelectedTerm_NoDuplicate_NoWarning()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");

        vm.SelectedCommunityEntry = new TermbaseEntry
        {
            SourceTerm = "Unique",
            PreferredTarget = "unique",
            CreatedBy = "Bob"
        };

        vm.AdoptSelectedTermCommand.Execute(null);

        Assert.DoesNotContain("already exists", vm.StatusMessage);
        Assert.Contains("Adopted", vm.StatusMessage);
        Assert.Contains("Bob", vm.StatusMessage);
    }

    // ---- 13. CommunityFilter — filters by SourceTerm, PreferredTarget, author ----

    [Fact]
    public void CommunityFilter_FiltersBySourceTerm()
    {
        var vm = MakeVm();
        vm.SetUsername("NotTheAuthor");

        // Manually populate the community entries via reflection or direct list access
        // We need to use the internal _allCommunityEntries list
        var allCommunityField = typeof(TermbaseEditorWindowViewModel)
            .GetField("_allCommunityEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(allCommunityField);

        var allCommunity = (List<(string Author, TermbaseEntry Entry)>)allCommunityField!.GetValue(vm)!;
        allCommunity.Add(("bob", new TermbaseEntry { SourceTerm = "Buddha", PreferredTarget = "Buddha", CreatedBy = "bob" }));
        allCommunity.Add(("bob", new TermbaseEntry { SourceTerm = "Dharma", PreferredTarget = "Dharma", CreatedBy = "bob" }));
        allCommunity.Add(("carol", new TermbaseEntry { SourceTerm = "Sangha", PreferredTarget = "Sangha", CreatedBy = "carol" }));

        vm.HasCommunityEntries = true;

        // Trigger refresh: set to something then clear to trigger both changes
        vm.CommunityFilter = "x";
        vm.CommunityFilter = "";
        Assert.Equal(3, vm.CommunityEntries.Count);

        // Filter by source term
        vm.CommunityFilter = "Buddha";
        Assert.Single(vm.CommunityEntries);
        Assert.Equal("Buddha", vm.CommunityEntries[0].SourceTerm);
    }

    [Fact]
    public void CommunityFilter_FiltersByPreferredTarget()
    {
        var vm = MakeVm();
        var allCommunityField = typeof(TermbaseEditorWindowViewModel)
            .GetField("_allCommunityEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allCommunity = (List<(string Author, TermbaseEntry Entry)>)allCommunityField!.GetValue(vm)!;

        allCommunity.Add(("bob", new TermbaseEntry { SourceTerm = "A", PreferredTarget = "Apple" }));
        allCommunity.Add(("bob", new TermbaseEntry { SourceTerm = "B", PreferredTarget = "Banana" }));
        vm.HasCommunityEntries = true;

        // Trigger initial refresh then filter
        vm.CommunityFilter = "x";
        vm.CommunityFilter = "Banana";
        Assert.Single(vm.CommunityEntries);
        Assert.Equal("B", vm.CommunityEntries[0].SourceTerm);
    }

    [Fact]
    public void CommunityFilter_FiltersByAuthor()
    {
        var vm = MakeVm();
        var allCommunityField = typeof(TermbaseEditorWindowViewModel)
            .GetField("_allCommunityEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allCommunity = (List<(string Author, TermbaseEntry Entry)>)allCommunityField!.GetValue(vm)!;

        allCommunity.Add(("bob", new TermbaseEntry { SourceTerm = "A", PreferredTarget = "a" }));
        allCommunity.Add(("carol", new TermbaseEntry { SourceTerm = "B", PreferredTarget = "b" }));
        vm.HasCommunityEntries = true;

        // Trigger initial refresh then filter
        vm.CommunityFilter = "x";
        vm.CommunityFilter = "carol";
        Assert.Single(vm.CommunityEntries);
        Assert.Equal("B", vm.CommunityEntries[0].SourceTerm);
    }

    // ---- 14. HasCommunityEntries — correct boolean state ----

    [Fact]
    public void HasCommunityEntries_DefaultIsFalse()
    {
        var vm = MakeVm();
        Assert.False(vm.HasCommunityEntries);
    }

    [Fact]
    public void HasCommunityEntries_TrueWhenSet()
    {
        var vm = MakeVm();
        vm.HasCommunityEntries = true;
        Assert.True(vm.HasCommunityEntries);
    }

    [Fact]
    public void HasCommunityEntries_FalseWhenNoCommunityEntries()
    {
        var vm = MakeVm();
        var allCommunityField = typeof(TermbaseEditorWindowViewModel)
            .GetField("_allCommunityEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allCommunity = (List<(string Author, TermbaseEntry Entry)>)allCommunityField!.GetValue(vm)!;

        // Empty list
        Assert.Empty(allCommunity);
        Assert.False(vm.HasCommunityEntries);
    }
}


