using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class ScholarTabViewModelTests
{
    private static ScholarTabViewModel MakeVm(StubScholarCollectionsService? svc = null)
    {
        return new ScholarTabViewModel(svc ?? new StubScholarCollectionsService());
    }

    // ---- Constructor ----

    [Fact]
    public void Constructor_ThrowsOnNullService()
    {
        Assert.Throws<ArgumentNullException>(() => new ScholarTabViewModel(null!));
    }

    // ---- Initial state ----

    [Fact]
    public void InitialState_IsEmptyStateTrue()
    {
        var vm = MakeVm();

        Assert.True(vm.IsEmptyState);
    }

    [Fact]
    public void InitialState_CollectionsEmpty()
    {
        var vm = MakeVm();

        Assert.Empty(vm.Collections);
    }

    [Fact]
    public void InitialState_PassagesEmpty()
    {
        var vm = MakeVm();

        Assert.Empty(vm.Passages);
    }

    [Fact]
    public void InitialState_SelectedCollectionNull()
    {
        var vm = MakeVm();

        Assert.Null(vm.SelectedCollection);
    }

    [Fact]
    public void InitialState_SelectedPassageNull()
    {
        var vm = MakeVm();

        Assert.Null(vm.SelectedPassage);
    }

    // ---- AddCollection ----

    [Fact]
    public void AddCollection_CreatesNewCollection()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);

        vm.AddCollectionCommand.Execute(null);

        Assert.Single(vm.Collections);
        Assert.Equal("New Collection", vm.Collections[0].Name);
    }

    [Fact]
    public void AddCollection_SelectsNewCollection()
    {
        var vm = MakeVm();

        vm.AddCollectionCommand.Execute(null);

        Assert.NotNull(vm.SelectedCollection);
        Assert.Equal(vm.Collections[0], vm.SelectedCollection);
    }

    [Fact]
    public void AddCollection_SetsIsEmptyStateFalse()
    {
        var vm = MakeVm();

        vm.AddCollectionCommand.Execute(null);

        Assert.False(vm.IsEmptyState);
    }

    [Fact]
    public void AddCollection_AssignsUniqueId()
    {
        var vm = MakeVm();

        vm.AddCollectionCommand.Execute(null);
        vm.AddCollectionCommand.Execute(null);

        Assert.Equal(2, vm.Collections.Count);
        Assert.NotEqual(vm.Collections[0].Id, vm.Collections[1].Id);
    }

    // ---- DeleteCollection ----

    [Fact]
    public void DeleteCollection_RemovesSelected()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        Assert.Single(vm.Collections);

        vm.DeleteCollectionCommand.Execute(null);

        Assert.Empty(vm.Collections);
    }

    [Fact]
    public void DeleteCollection_SetsIsEmptyStateWhenLastRemoved()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);

        vm.DeleteCollectionCommand.Execute(null);

        Assert.True(vm.IsEmptyState);
    }

    [Fact]
    public void DeleteCollection_NoSelection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        vm.DeleteCollectionCommand.Execute(null);

        Assert.Empty(vm.Collections);
    }

    [Fact]
    public void DeleteCollection_SelectsNextAvailable()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        vm.AddCollectionCommand.Execute(null);
        // Select the first one
        vm.SelectedCollection = vm.Collections[0];

        vm.DeleteCollectionCommand.Execute(null);

        Assert.Single(vm.Collections);
        Assert.NotNull(vm.SelectedCollection);
    }

    // ---- SelectedCollection changes update Passages ----

    [Fact]
    public void SelectedCollection_UpdatesPassagesDisplay()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        // Add passages directly to the collection model
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p1",
            ZhText = "test",
            SourceRelPath = "test.xml"
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p2",
            ZhText = "test2",
            SourceRelPath = "test2.xml"
        });

        // Trigger re-selection to refresh Passages
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        Assert.Equal(2, vm.Passages.Count);
    }

    [Fact]
    public void SelectedCollection_SetToNull_ClearsPassages()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "test", SourceRelPath = "x.xml" });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        Assert.NotEmpty(vm.Passages);

        vm.SelectedCollection = null;
        Assert.Empty(vm.Passages);
    }

    // ---- AddPassageToCollectionAsync ----

    [Fact]
    public async Task AddPassageToCollection_AddsPassageAndSaves()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        vm.AddCollectionCommand.Execute(null);
        var collectionId = vm.Collections[0].Id;

        // Set _root via reflection so SaveAsync actually runs
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        var passage = new ScholarPassage
        {
            ZhText = "test chinese",
            EnText = "test english",
            SourceRelPath = "xml-p5/T/T0001.xml"
        };

        await vm.AddPassageToCollectionAsync(collectionId, passage);

        Assert.Single(vm.Collections[0].Passages);
        Assert.NotEmpty(passage.Id); // ID was assigned
        Assert.False(vm.IsEmptyState);
        Assert.NotNull(svc.LastSaved); // Save was triggered
    }

    [Fact]
    public async Task AddPassageToCollection_InvalidCollectionId_DoesNothing()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);

        var passage = new ScholarPassage { ZhText = "test" };
        await vm.AddPassageToCollectionAsync("nonexistent", passage);

        Assert.Null(svc.LastSaved);
    }

    [Fact]
    public async Task AddPassageToCollection_UpdatesPassagesIfSelectedCollection()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collectionId = vm.Collections[0].Id;
        // SelectedCollection is already set to the new collection

        var passage = new ScholarPassage
        {
            ZhText = "test",
            SourceRelPath = "test.xml"
        };

        await vm.AddPassageToCollectionAsync(collectionId, passage);

        Assert.Single(vm.Passages); // Passages observable was updated
    }

    // ---- NavigateToPassage ----

    [Fact]
    public void NavigateToPassage_FiresNavigationRequested()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        var passage = new ScholarPassage
        {
            Id = "p1",
            ZhText = "some chinese text for navigation",
            SourceRelPath = "xml-p5/T/T0001.xml"
        };
        collection.Passages.Add(passage);

        // Re-select to populate Passages
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        vm.SelectedPassage = passage;

        NavigationRequest? receivedRequest = null;
        vm.NavigationRequested += (_, req) => receivedRequest = req;

        vm.NavigateToPassageCommand.Execute(null);

        Assert.NotNull(receivedRequest);
        Assert.Equal("xml-p5/T/T0001.xml", receivedRequest!.RelPath);
        Assert.NotNull(receivedRequest.MatchText);
    }

    [Fact]
    public void NavigateToPassage_NoSelection_DoesNotFire()
    {
        var vm = MakeVm();
        vm.SelectedPassage = null;

        bool fired = false;
        vm.NavigationRequested += (_, _) => fired = true;

        vm.NavigateToPassageCommand.Execute(null);

        Assert.False(fired);
    }

    [Fact]
    public void NavigateToPassage_LongText_TruncatesMatchText()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        var passage = new ScholarPassage
        {
            Id = "p1",
            ZhText = "this is a very long text that should be truncated to twenty characters",
            SourceRelPath = "test.xml"
        };
        collection.Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        vm.SelectedPassage = passage;

        NavigationRequest? req = null;
        vm.NavigationRequested += (_, r) => req = r;

        vm.NavigateToPassageCommand.Execute(null);

        Assert.NotNull(req);
        Assert.Equal(20, req!.MatchText!.Length);
    }

    // ---- DeletePassage ----

    [Fact]
    public void DeletePassage_RemovesFromCollectionAndPassages()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        var passage = new ScholarPassage { Id = "p1", ZhText = "test", SourceRelPath = "x.xml" };
        collection.Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        vm.SelectedPassage = passage;

        vm.DeletePassageCommand.Execute(null);

        Assert.Empty(vm.Passages);
        Assert.Empty(collection.Passages);
    }

    [Fact]
    public void DeletePassage_NoSelection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedPassage = null;

        vm.DeletePassageCommand.Execute(null); // should not throw
    }

    // ---- Clear ----

    [Fact]
    public void Clear_ResetsEverything()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        Assert.Single(vm.Collections);
        Assert.False(vm.IsEmptyState);

        vm.Clear();

        Assert.Empty(vm.Collections);
        Assert.Empty(vm.Passages);
        Assert.Null(vm.SelectedCollection);
        Assert.Null(vm.SelectedPassage);
        Assert.True(vm.IsEmptyState);
    }

    // ---- SelectedPassage syncs editor fields ----

    [Fact]
    public void SelectedPassage_SyncsEditorFields()
    {
        var vm = MakeVm();
        var passage = new ScholarPassage
        {
            Id = "p1",
            Notes = "some notes",
            Tags = new List<string> { "tag1", "tag2" },
            MasterNames = new List<string> { "Master A" },
            SourceRelPath = "x.xml",
            ZhText = "zh"
        };

        vm.AddCollectionCommand.Execute(null);
        vm.Collections[0].Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = passage;

        Assert.Equal("some notes", vm.PassageNotes);
        Assert.Contains("tag1", vm.PassageTags);
        Assert.Contains("tag2", vm.PassageTags);
        Assert.Contains("Master A", vm.PassageMasterNames);
    }

    [Fact]
    public void SelectedPassage_Null_ClearsEditorFields()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var passage = new ScholarPassage
        {
            Id = "p1",
            Notes = "notes",
            Tags = new List<string> { "tag" },
            MasterNames = new List<string> { "name" },
            SourceRelPath = "x.xml",
            ZhText = "zh"
        };
        vm.Collections[0].Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = passage;
        Assert.NotEmpty(vm.PassageNotes);

        vm.SelectedPassage = null;

        Assert.Equal("", vm.PassageNotes);
        Assert.Equal("", vm.PassageTags);
        Assert.Equal("", vm.PassageMasterNames);
    }

    // ---- SearchFilter: Tag matching ----

    [Fact]
    public void SearchFilter_FiltersByTagMatch()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml", Tags = new List<string> { "dharma" } });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml", Tags = new List<string> { "zen" } });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml", Tags = new List<string> { "dharma", "zen" } });

        // Re-select to populate passages
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        Assert.Equal(3, vm.Passages.Count);

        vm.SearchFilter = "dharma";

        Assert.Equal(2, vm.Passages.Count);
        Assert.All(vm.Passages, p => Assert.Contains("dharma", p.Tags));
    }

    // ---- SearchFilter: Master name matching ----

    [Fact]
    public void SearchFilter_FiltersByMasterNameMatch()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml", MasterNames = new List<string> { "Huineng" } });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml", MasterNames = new List<string> { "Linji" } });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        Assert.Equal(2, vm.Passages.Count);

        vm.SearchFilter = "Huineng";

        Assert.Single(vm.Passages);
        Assert.Equal("p1", vm.Passages[0].Id);
    }

    // ---- SearchFilter: ZhText/EnText content matching ----

    [Fact]
    public void SearchFilter_FiltersByZhTextContent()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "buddha nature", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "emptiness", SourceRelPath = "y.xml" });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SearchFilter = "buddha";

        Assert.Single(vm.Passages);
        Assert.Equal("p1", vm.Passages[0].Id);
    }

    [Fact]
    public void SearchFilter_FiltersByEnTextContent()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "zh1", EnText = "the way of zen", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "zh2", EnText = "pure land", SourceRelPath = "y.xml" });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SearchFilter = "zen";

        Assert.Single(vm.Passages);
        Assert.Equal("p1", vm.Passages[0].Id);
    }

    // ---- CollectionFilter: Name matching ----

    [Fact]
    public void CollectionFilter_FiltersByCollectionName()
    {
        var vm = MakeVm();
        // Manually add collections with distinct names
        vm.AddCollectionCommand.Execute(null);
        vm.Collections[0].Name = "Zen Koans";
        vm.AddCollectionCommand.Execute(null);
        vm.Collections[1].Name = "Pure Land Sutras";

        vm.CollectionFilter = "Zen";

        Assert.Single(vm.Collections);
        Assert.Equal("Zen Koans", vm.Collections[0].Name);
    }

    // ---- Empty filter shows all items ----

    [Fact]
    public void SearchFilter_EmptyShowsAllPassages()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml", Tags = new List<string> { "dharma" } });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml", Tags = new List<string> { "zen" } });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SearchFilter = "dharma";
        Assert.Single(vm.Passages);

        vm.SearchFilter = "";
        Assert.Equal(2, vm.Passages.Count);
    }

    [Fact]
    public void CollectionFilter_EmptyShowsAllCollections()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        vm.Collections[0].Name = "Alpha";
        vm.AddCollectionCommand.Execute(null);
        vm.Collections[1].Name = "Beta";

        vm.CollectionFilter = "Alpha";
        Assert.Single(vm.Collections);

        vm.CollectionFilter = "";
        Assert.Equal(2, vm.Collections.Count);
    }

    // ---- Clearing filter restores all items ----

    [Fact]
    public void SearchFilter_ClearingFilterRestoresAllPassages()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml", MasterNames = new List<string> { "Linji" } });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        Assert.Equal(3, vm.Passages.Count);

        vm.SearchFilter = "Linji";
        Assert.Single(vm.Passages);

        vm.SearchFilter = "";
        Assert.Equal(3, vm.Passages.Count);
    }

    // ---- Author fields (CreatedBy) ----

    [Fact]
    public void AddCollection_SetsCreatedByFromUsername()
    {
        var vm = MakeVm();
        vm.SetUsername("TestUser");

        vm.AddCollectionCommand.Execute(null);

        Assert.Equal("TestUser", vm.Collections[0].CreatedBy);
    }

    [Fact]
    public async Task AddPassageToCollection_SetsCreatedByFromUsername()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        vm.SetUsername("Scholar1");
        vm.AddCollectionCommand.Execute(null);
        var collectionId = vm.Collections[0].Id;

        var passage = new ScholarPassage
        {
            ZhText = "test chinese",
            SourceRelPath = "xml-p5/T/T0001.xml"
        };

        await vm.AddPassageToCollectionAsync(collectionId, passage);

        Assert.Equal("Scholar1", passage.CreatedBy);
    }

    [Fact]
    public void SetUsername_UpdatesInternalField()
    {
        var vm = MakeVm();

        // Set username, then add collection to verify it takes effect
        vm.SetUsername("  Alice  "); // should be trimmed
        vm.AddCollectionCommand.Execute(null);

        Assert.Equal("Alice", vm.Collections[0].CreatedBy);
    }

    [Fact]
    public void SetUsername_NullClearsField()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");
        vm.SetUsername(null);

        vm.AddCollectionCommand.Execute(null);

        Assert.Null(vm.Collections[0].CreatedBy);
    }

    [Fact]
    public void SetUsername_WhitespaceClearsField()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");
        vm.SetUsername("   ");

        vm.AddCollectionCommand.Execute(null);

        Assert.Null(vm.Collections[0].CreatedBy);
    }

    // ---- Export/Import ----

    [Fact]
    public async Task ExportCollections_CallsServiceExport()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var trackingVm = new ScholarTabViewModel(trackingSvc);
        trackingVm.AddCollectionCommand.Execute(null);

        trackingVm.PickExportFileAsync = () => Task.FromResult<string?>("/tmp/export.json");

        await trackingVm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.True(trackingSvc.ExportWasCalled);
        Assert.Equal("/tmp/export.json", trackingSvc.LastExportPath);
    }

    [Fact]
    public async Task ImportCollections_MergesImportedCollections()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        trackingSvc.ImportResult = new List<ScholarCollection>
        {
            new() { Id = "imported1", Name = "Imported Collection", Passages = new List<ScholarPassage>() }
        };

        var vm = new ScholarTabViewModel(trackingSvc);
        // Set root so save works
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        vm.PickImportFileAsync = () => Task.FromResult<string?>("/tmp/import.json");

        await vm.ImportCollectionsCommand.ExecuteAsync(null);

        // The imported collection should be in _allCollections (reflected through Collections)
        Assert.Single(vm.Collections);
        Assert.Equal("Imported Collection", vm.Collections[0].Name);
    }

    [Fact]
    public async Task ExportCollections_NoFilePicker_SetsStatus()
    {
        var vm = MakeVm();
        vm.PickExportFileAsync = null;

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.Contains("not available", vm.StatusMessage);
    }

    [Fact]
    public async Task ImportCollections_NoFilePicker_SetsStatus()
    {
        var vm = MakeVm();
        vm.PickImportFileAsync = null;

        await vm.ImportCollectionsCommand.ExecuteAsync(null);

        Assert.Contains("not available", vm.StatusMessage);
    }

    // ---- Community: LoadCommunityAsync ----

    [Fact]
    public async Task LoadCommunityAsync_PopulatesCommunityCollections()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "comm1", Name = "Bob's Koans", CreatedBy = "bob",
                            Passages = new List<ScholarPassage>
                            {
                                new() { Id = "p1", ZhText = "koan text", SourceRelPath = "x.xml" }
                            } },
                    new() { Id = "comm2", Name = "Bob's Sutras", CreatedBy = "bob" }
                },
                ["carol"] = new List<ScholarCollection>
                {
                    new() { Id = "comm3", Name = "Carol's Collection", CreatedBy = "carol" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        // Set root via reflection to enable LoadCommunityAsync
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.CommunityCollections.Count);
        Assert.True(vm.HasCommunityCollections);
    }

    [Fact]
    public async Task LoadCommunityAsync_ExcludesCurrentUsersCollections()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["alice"] = new List<ScholarCollection>
                {
                    new() { Id = "own1", Name = "My Own Collection", CreatedBy = "alice" }
                },
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "other1", Name = "Bob's Collection", CreatedBy = "bob" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        // Alice's own collection should be excluded
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Bob's Collection", vm.CommunityCollections[0].Name);
    }

    [Fact]
    public async Task LoadCommunityAsync_ExcludesCurrentUser_CaseInsensitive()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["Alice"] = new List<ScholarCollection>
                {
                    new() { Id = "own1", Name = "My Collection" }
                },
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "other1", Name = "Bob's" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice"); // lowercase

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        // "Alice" should be excluded even though username is "alice"
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Bob's", vm.CommunityCollections[0].Name);
    }

    // ---- Community: CommunityFilter ----

    [Fact]
    public async Task CommunityFilter_FiltersByNameAuthorDescription()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "c1", Name = "Zen Koans", Description = "A collection of koans" },
                    new() { Id = "c2", Name = "Pure Land", Description = "Amitabha sutras" }
                },
                ["carol"] = new List<ScholarCollection>
                {
                    new() { Id = "c3", Name = "Meditation Guide", Description = "Zen practices" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);
        Assert.Equal(3, vm.CommunityCollections.Count);

        // Filter by name
        vm.CommunityFilter = "Zen";
        Assert.Equal(2, vm.CommunityCollections.Count); // "Zen Koans" and "Meditation Guide" (description has "Zen")

        // Filter by author
        vm.CommunityFilter = "carol";
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Meditation Guide", vm.CommunityCollections[0].Name);

        // Filter by description
        vm.CommunityFilter = "Amitabha";
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Pure Land", vm.CommunityCollections[0].Name);

        // Clear filter restores all
        vm.CommunityFilter = "";
        Assert.Equal(3, vm.CommunityCollections.Count);
    }

    // ---- Community: Selecting community collection populates CommunityPassages ----

    [Fact]
    public async Task SelectingCommunityCollection_PopulatesCommunityPassages()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new()
                    {
                        Id = "c1",
                        Name = "Bob's Collection",
                        Passages = new List<ScholarPassage>
                        {
                            new() { Id = "p1", ZhText = "passage 1", SourceRelPath = "x.xml" },
                            new() { Id = "p2", ZhText = "passage 2", SourceRelPath = "y.xml" }
                        }
                    }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        // After load, first community collection should be auto-selected
        Assert.NotNull(vm.SelectedCommunityCollection);
        Assert.Equal(2, vm.CommunityPassages.Count);
        Assert.NotNull(vm.SelectedCommunityPassage);
    }

    // ---- Community: HasCommunityCollections ----

    [Fact]
    public async Task HasCommunityCollections_TrueWhenDataExists()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "c1", Name = "Test" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        Assert.False(vm.HasCommunityCollections); // initially false

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        Assert.True(vm.HasCommunityCollections);
    }

    [Fact]
    public async Task HasCommunityCollections_FalseWhenEmpty()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>()
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        Assert.False(vm.HasCommunityCollections);
    }

    // ---- Clear resets community state ----

    [Fact]
    public async Task Clear_ResetsAllCommunityState()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new()
                    {
                        Id = "c1",
                        Name = "Community Col",
                        Passages = new List<ScholarPassage>
                        {
                            new() { Id = "p1", ZhText = "text", SourceRelPath = "x.xml" }
                        }
                    }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);
        Assert.NotEmpty(vm.CommunityCollections);
        Assert.True(vm.HasCommunityCollections);

        vm.Clear();

        Assert.Empty(vm.CommunityCollections);
        Assert.Empty(vm.CommunityPassages);
        Assert.Null(vm.SelectedCommunityCollection);
        Assert.Null(vm.SelectedCommunityPassage);
        Assert.False(vm.HasCommunityCollections);
    }

    // ---- Mutual exclusion: selecting community clears user selection ----
    // Note: The current VM does not explicitly implement mutual exclusion between
    // user collections and community collections. They are independent panels.
    // This test verifies they are independent (both can have selections).

    [Fact]
    public async Task UserAndCommunitySelections_AreIndependent()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "comm1", Name = "Bob's Collection" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        // Add user collection
        vm.AddCollectionCommand.Execute(null);
        Assert.NotNull(vm.SelectedCollection);

        // Load community
        await vm.LoadCommunityCommand.ExecuteAsync(null);
        Assert.NotNull(vm.SelectedCommunityCollection);

        // Both should be selected independently
        Assert.NotNull(vm.SelectedCollection);
        Assert.NotNull(vm.SelectedCommunityCollection);
    }
}

// ---- Helper stub for export/import tracking ----

internal class TrackingScholarCollectionsService : IScholarCollectionsService
{
    public bool ExportWasCalled { get; private set; }
    public string? LastExportPath { get; private set; }
    public List<ScholarCollection> ImportResult { get; set; } = new();

    public Task<List<ScholarCollection>> LoadAsync(string root, CancellationToken ct = default)
        => Task.FromResult(new List<ScholarCollection>());

    public Task SaveAsync(string root, List<ScholarCollection> collections, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ExportAsync(string filePath, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        ExportWasCalled = true;
        LastExportPath = filePath;
        return Task.CompletedTask;
    }

    public Task<List<ScholarCollection>> ImportAsync(string filePath, CancellationToken ct = default)
        => Task.FromResult(new List<ScholarCollection>(ImportResult));
    public Task WriteUserJsonlAsync(string communityDir, string username, List<ScholarCollection> collections, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Dictionary<string, List<ScholarCollection>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default) => Task.FromResult(new Dictionary<string, List<ScholarCollection>>());
}
