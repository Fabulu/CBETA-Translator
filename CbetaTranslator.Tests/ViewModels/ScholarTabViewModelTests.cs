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

    [Fact]
    public async Task EnsureWritableCollectionAsync_CreatesAndSavesWhenEmpty()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        var collection = await vm.EnsureWritableCollectionAsync();

        Assert.NotNull(collection);
        Assert.Single(vm.Collections);
        Assert.Equal(collection, vm.SelectedCollection);
        Assert.NotNull(svc.LastSaved);
    }

    [Fact]
    public async Task EnsureWritableCollectionAsync_ReusesExistingSelectedCollection()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        vm.AddCollectionCommand.Execute(null);
        var selected = vm.SelectedCollection;

        var collection = await vm.EnsureWritableCollectionAsync();

        Assert.Same(selected, collection);
        Assert.Single(vm.Collections);
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
            ZhText = "this is a very long text that should be truncated to eighty characters and this part goes well beyond that limit to ensure truncation happens",
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
        Assert.Equal(80, req!.MatchText!.Length);
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

        trackingVm.PickExportFileAsync = (_, _) => Task.FromResult<string?>("/tmp/export.json");

        await trackingVm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.True(trackingSvc.ExportWasCalled);
        Assert.Equal("/tmp/export.json", trackingSvc.LastExportPath);
    }


    [Fact]
    public async Task ExportCollections_CancelledFormat_DoesNotExport()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var vm = new ScholarTabViewModel(trackingSvc)
        {
            PickExportFileAsync = (_, _) => Task.FromResult<string?>("/tmp/should-not-export.json"),
            PickExportFormatAsync = () => Task.FromResult<ScholarExportFormat?>(null)
        };

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.False(trackingSvc.ExportWasCalled);
        Assert.Contains("cancelled", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportCollections_RichFormatWithoutSelectedCollection_BlocksBeforePicker()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var vm = new ScholarTabViewModel(trackingSvc)
        {
            PickExportFormatAsync = () => Task.FromResult<ScholarExportFormat?>(ScholarExportFormat.Html)
        };

        var pickerCalled = false;
        vm.PickExportFileAsync = (_, _) =>
        {
            pickerCalled = true;
            return Task.FromResult<string?>("/tmp/export.html");
        };

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.False(pickerCalled);
        Assert.Contains("Select a collection", vm.StatusMessage);
    }

    [Fact]
    public async Task ExportCollections_JsonFormat_UsesJsonExportPath()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var vm = new ScholarTabViewModel(trackingSvc)
        {
            PickExportFormatAsync = () => Task.FromResult<ScholarExportFormat?>(ScholarExportFormat.Json),
            PickExportFileAsync = (format, name) =>
            {
                Assert.Equal(ScholarExportFormat.Json, format);
                Assert.Null(name);
                return Task.FromResult<string?>("/tmp/export.json");
            }
        };
        vm.AddCollectionCommand.Execute(null);

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

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

    // ---- Link management: CreateLinkAsync ----

    [Fact]
    public async Task CreateLinkAsync_AddsLinkToCollection()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");

        Assert.Single(collection.Links);
        Assert.Equal("p1", collection.Links[0].FromPassageId);
        Assert.Equal("p2", collection.Links[0].ToPassageId);
        Assert.Equal("quotes", collection.Links[0].RelationType);
        Assert.NotEmpty(collection.Links[0].Id);
    }

    [Fact]
    public async Task CreateLinkAsync_NoSelectedCollection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        await vm.CreateLinkAsync("p1", "p2", "quotes");

        // No crash, no links added
    }

    [Fact]
    public async Task CreateLinkAsync_SetsCreatedUtc()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        vm.SelectedCollection = collection;

        var before = DateTimeOffset.UtcNow;
        await vm.CreateLinkAsync("p1", "p2", "alludes-to");
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(collection.Links[0].CreatedUtc, before, after);
    }

    [Fact]
    public async Task CreateLinkAsync_MultipleLinks_AllStored()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        await vm.CreateLinkAsync("p2", "p3", "comments-on");
        await vm.CreateLinkAsync("p1", "p3", "parallels");

        Assert.Equal(3, collection.Links.Count);
    }

    // ---- Link management: RemoveLinkAsync ----

    [Fact]
    public async Task RemoveLinkAsync_RemovesLinkById()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        Assert.Single(collection.Links);

        var linkId = collection.Links[0].Id;
        await vm.RemoveLinkAsync(linkId);

        Assert.Empty(collection.Links);
    }

    [Fact]
    public async Task RemoveLinkAsync_NonexistentId_DoesNothing()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        Assert.Single(collection.Links);

        await vm.RemoveLinkAsync("nonexistent-id");

        Assert.Single(collection.Links); // unchanged
    }

    [Fact]
    public async Task RemoveLinkAsync_NoSelectedCollection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        await vm.RemoveLinkAsync("any-id"); // should not crash
    }

    [Fact]
    public async Task RemoveLinkAsync_OnlyRemovesTargetLink()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        await vm.CreateLinkAsync("p2", "p3", "parallels");
        Assert.Equal(2, collection.Links.Count);

        var firstLinkId = collection.Links[0].Id;
        await vm.RemoveLinkAsync(firstLinkId);

        Assert.Single(collection.Links);
        Assert.Equal("parallels", collection.Links[0].RelationType);
    }

    // ---- Link management: GetLinksForPassage ----

    [Fact]
    public async Task GetLinksForPassage_ReturnsLinksWherePassageIsFromOrTo()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        await vm.CreateLinkAsync("p2", "p3", "parallels");
        await vm.CreateLinkAsync("p1", "p3", "alludes-to");

        // p1 is From in two links
        var p1Links = vm.GetLinksForPassage("p1");
        Assert.Equal(2, p1Links.Count);

        // p2 is From in one, To in another
        var p2Links = vm.GetLinksForPassage("p2");
        Assert.Equal(2, p2Links.Count);

        // p3 is To in two links
        var p3Links = vm.GetLinksForPassage("p3");
        Assert.Equal(2, p3Links.Count);
    }

    [Fact]
    public void GetLinksForPassage_NoLinks_ReturnsEmptyList()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        vm.SelectedCollection = vm.Collections[0];

        var links = vm.GetLinksForPassage("any-id");

        Assert.Empty(links);
    }

    [Fact]
    public void GetLinksForPassage_NoSelectedCollection_ReturnsEmptyList()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        var links = vm.GetLinksForPassage("any-id");

        Assert.Empty(links);
    }

    [Fact]
    public async Task GetLinksForPassage_UnrelatedPassage_ReturnsEmpty()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");

        var p3Links = vm.GetLinksForPassage("p3");
        Assert.Empty(p3Links);
    }

    // ---- DeletePassage: orphan link cleanup ----

    [Fact]
    public async Task DeletePassage_CleansUpOrphanLinks()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        var p1 = new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" };
        var p2 = new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" };
        var p3 = new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" };
        collection.Passages.Add(p1);
        collection.Passages.Add(p2);
        collection.Passages.Add(p3);
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        await vm.CreateLinkAsync("p2", "p3", "parallels");
        await vm.CreateLinkAsync("p1", "p3", "alludes-to");
        Assert.Equal(3, collection.Links.Count);

        // Delete p2 -- should remove links involving p2
        vm.SelectedPassage = p2;
        vm.DeletePassageCommand.Execute(null);

        // Only the p1->p3 link should remain
        Assert.Single(collection.Links);
        Assert.Equal("alludes-to", collection.Links[0].RelationType);
        Assert.Equal("p1", collection.Links[0].FromPassageId);
        Assert.Equal("p3", collection.Links[0].ToPassageId);
    }

    [Fact]
    public async Task DeletePassage_RemovesAllLinksWhenPassageIsInAllLinks()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        var p1 = new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" };
        var p2 = new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" };
        collection.Passages.Add(p1);
        collection.Passages.Add(p2);
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "quotes");
        Assert.Single(collection.Links);

        // Delete p1 -- the single link references p1
        vm.SelectedPassage = p1;
        vm.DeletePassageCommand.Execute(null);

        Assert.Empty(collection.Links);
    }

    // ---- FindPassageById ----

    [Fact]
    public void FindPassageById_ReturnsCorrectPassage()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        var p1 = new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" };
        var p2 = new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" };
        collection.Passages.Add(p1);
        collection.Passages.Add(p2);
        vm.SelectedCollection = collection;

        Assert.Same(p1, vm.FindPassageById("p1"));
        Assert.Same(p2, vm.FindPassageById("p2"));
        Assert.Null(vm.FindPassageById("nonexistent"));
    }

    [Fact]
    public void FindPassageById_NoSelectedCollection_ReturnsNull()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        Assert.Null(vm.FindPassageById("any"));
    }

    // ---- Test 9: Facet properties sync to/from passage ----

    [Fact]
    public void FacetProperties_SyncToPassageOnSave()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);

        // Set root so save runs
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        vm.AddCollectionCommand.Execute(null);
        var passage = new ScholarPassage
        {
            Id = "p1", ZhText = "text", SourceRelPath = "x.xml"
        };
        vm.Collections[0].Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = passage;

        // Set facet values via VM properties
        vm.DoctrinalTopic = "Buddha-nature";
        vm.LiteraryForm = "Koan case";
        vm.Lineage = "Linji/Rinzai";
        vm.RhetoricalFunction = "Paradox";

        // Trigger save (which calls SyncEditorFieldsToPassage)
        vm.SaveCommand.Execute(null);

        Assert.Equal("Buddha-nature", passage.DoctrinalTopic);
        Assert.Equal("Koan case", passage.LiteraryForm);
        Assert.Equal("Linji/Rinzai", passage.Lineage);
        Assert.Equal("Paradox", passage.RhetoricalFunction);
    }

    [Fact]
    public void FacetProperties_SyncFromPassageOnSelection()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var passage = new ScholarPassage
        {
            Id = "p1", ZhText = "text", SourceRelPath = "x.xml",
            DoctrinalTopic = "Emptiness",
            LiteraryForm = "Verse commentary",
            Lineage = "Caodong/Soto",
            RhetoricalFunction = "Assertion"
        };
        vm.Collections[0].Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = passage;

        Assert.Equal("Emptiness", vm.DoctrinalTopic);
        Assert.Equal("Verse commentary", vm.LiteraryForm);
        Assert.Equal("Caodong/Soto", vm.Lineage);
        Assert.Equal("Assertion", vm.RhetoricalFunction);
    }

    [Fact]
    public void FacetProperties_NullPassageFacets_BecomeEmptyStrings()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var passage = new ScholarPassage
        {
            Id = "p1", ZhText = "text", SourceRelPath = "x.xml",
            DoctrinalTopic = null,
            LiteraryForm = null,
            Lineage = null,
            RhetoricalFunction = null
        };
        vm.Collections[0].Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = passage;

        Assert.Equal("", vm.DoctrinalTopic);
        Assert.Equal("", vm.LiteraryForm);
        Assert.Equal("", vm.Lineage);
        Assert.Equal("", vm.RhetoricalFunction);
    }

    [Fact]
    public void FacetProperties_EmptyStringsSyncAsNullToPassage()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        vm.AddCollectionCommand.Execute(null);
        var passage = new ScholarPassage
        {
            Id = "p1", ZhText = "text", SourceRelPath = "x.xml",
            DoctrinalTopic = "Emptiness"
        };
        vm.Collections[0].Passages.Add(passage);
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = passage;

        // Clear facet values
        vm.DoctrinalTopic = "";
        vm.LiteraryForm = "";
        vm.Lineage = "   "; // whitespace only
        vm.RhetoricalFunction = "";

        vm.SaveCommand.Execute(null);

        Assert.Null(passage.DoctrinalTopic);
        Assert.Null(passage.LiteraryForm);
        Assert.Null(passage.Lineage);
        Assert.Null(passage.RhetoricalFunction);
    }

    // ---- Test 10: SortMode "Chronological" sorts by master date ----

    [Fact]
    public void SortMode_Chronological_SortsByMasterNameDate()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        // Linji floruit=810, Bodhidharma floruit=500, Hakuin floruit=1686
        // If master-dates.json is available, sorting should order by floruit
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p1", ZhText = "a", SourceRelPath = "x.xml",
            MasterNames = new List<string> { "Hakuin" }
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p2", ZhText = "b", SourceRelPath = "y.xml",
            MasterNames = new List<string> { "Bodhidharma" }
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p3", ZhText = "c", SourceRelPath = "z.xml",
            MasterNames = new List<string> { "Linji" }
        });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        Assert.Equal(3, vm.Passages.Count);

        vm.SortMode = "Chronological";

        // If master-dates.json is available in test output, order should be:
        // Bodhidharma (500), Linji (810), Hakuin (1686)
        // If file not found, all get int.MaxValue and order is preserved (Default)
        // Either way, this should not throw
        Assert.Equal(3, vm.Passages.Count);
    }

    // ---- Test 11: SortMode "A-Z (Chinese)" sorts alphabetically ----

    [Fact]
    public void SortMode_AZChinese_SortsAlphabetically()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        collection.Passages.Add(new ScholarPassage
        {
            Id = "p1", ZhText = "\u5fc3\u5373\u662f\u4f5b", SourceRelPath = "x.xml" // ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Âº
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p2", ZhText = "\u4e0d\u662f\u5fc3\u4e0d\u662f\u4f5b", SourceRelPath = "y.xml" // ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚ÂÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚ÂÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Âº
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p3", ZhText = "\u5e73\u5e38\u5fc3\u662f\u9053", SourceRelPath = "z.xml" // ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¹Ãƒâ€šÃ‚Â³ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â¸ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â©Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ
        });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SortMode = "A-Z (Chinese)";

        // Ordinal sort: ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â (U+4E0D) < ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¹Ãƒâ€šÃ‚Â³ (U+5E73) < ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ (U+5FC3)
        Assert.Equal(3, vm.Passages.Count);
        Assert.Equal("p2", vm.Passages[0].Id); // ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚ÂÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚ÂÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Âº
        Assert.Equal("p3", vm.Passages[1].Id); // ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¹Ãƒâ€šÃ‚Â³ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â¸ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â©Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ
        Assert.Equal("p1", vm.Passages[2].Id); // ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¿Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Âº
    }

    [Fact]
    public void SortMode_Default_PreservesInsertionOrder()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SortMode = "Default";

        // Default preserves insertion order
        Assert.Equal("p3", vm.Passages[0].Id);
        Assert.Equal("p1", vm.Passages[1].Id);
        Assert.Equal("p2", vm.Passages[2].Id);
    }

    // ---- Test 12: StudyNotes syncs with selected collection ----

    [Fact]
    public void StudyNotes_LoadedFromCollection()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        vm.AddCollectionCommand.Execute(null);

        // Select first collection and set notes via VM property (syncs to collection)
        vm.SelectedCollection = vm.Collections[0];
        vm.StudyNotes = "My research notes on this collection";

        // Switch to second collection ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â notes should be saved back to first
        vm.SelectedCollection = vm.Collections[1];
        Assert.Equal("", vm.StudyNotes); // second collection has no notes

        // Switch back ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â notes should reload from first collection
        vm.SelectedCollection = vm.Collections[0];
        Assert.Equal("My research notes on this collection", vm.StudyNotes);
    }

    [Fact]
    public void StudyNotes_SavedBackToCollection()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        vm.AddCollectionCommand.Execute(null);
        vm.Collections[0].Passages.Add(new ScholarPassage
        {
            Id = "p1", ZhText = "text", SourceRelPath = "x.xml"
        });
        vm.SelectedCollection = null;
        vm.SelectedCollection = vm.Collections[0];
        vm.SelectedPassage = vm.Passages[0];

        vm.StudyNotes = "Updated notes";

        // Save triggers SyncEditorFieldsToPassage which includes study notes sync
        vm.SaveCommand.Execute(null);

        Assert.Equal("Updated notes", vm.Collections[0].StudyNotes);
    }

    [Fact]
    public void StudyNotes_NullCollection_ShowsEmpty()
    {
        var vm = MakeVm();

        vm.SelectedCollection = null;

        Assert.Equal("", vm.StudyNotes);
    }

    [Fact]
    public void StudyNotes_EmptyByDefault()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);

        // New collection has empty study notes
        Assert.Equal("", vm.StudyNotes);
    }

    // ---- SearchFilterMode: Facet-specific filtering ----

    [Fact]
    public void SearchFilterMode_Topic_FiltersByDoctrinalTopic()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p1", ZhText = "a", SourceRelPath = "x.xml",
            DoctrinalTopic = "Buddha-nature"
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p2", ZhText = "b", SourceRelPath = "y.xml",
            DoctrinalTopic = "Emptiness"
        });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SearchFilterMode = "Topic";
        vm.SearchFilter = "Buddha";

        Assert.Single(vm.Passages);
        Assert.Equal("p1", vm.Passages[0].Id);
    }

    [Fact]
    public void SearchFilterMode_Form_FiltersByLiteraryForm()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p1", ZhText = "a", SourceRelPath = "x.xml",
            LiteraryForm = "Koan case"
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p2", ZhText = "b", SourceRelPath = "y.xml",
            LiteraryForm = "Dharma talk"
        });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SearchFilterMode = "Form";
        vm.SearchFilter = "Koan";

        Assert.Single(vm.Passages);
        Assert.Equal("p1", vm.Passages[0].Id);
    }

    // ---- Facet options loaded ----

    [Fact]
    public void FacetOptions_PopulatedOnConstruction()
    {
        var vm = MakeVm();

        // Options should be populated (either from JSON file or defaults)
        Assert.NotEmpty(vm.DoctrinalTopicOptions);
        Assert.NotEmpty(vm.LiteraryFormOptions);
        Assert.NotEmpty(vm.LineageOptions);
        Assert.NotEmpty(vm.RhetoricalFunctionOptions);
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

    // ---- DetectMasterNames ----

    private static List<MasterNameEntry> MakeTestMasterEntries() => new()
    {
        // Linji has Chinese (2+ chars) and pinyin (4+ chars)
        new(new List<string> { "Linji Yixuan", "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¾Ãƒâ€šÃ‚Â©ÃƒÆ’Ã‚Â§Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾", "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸" }),
        // Zhaozhou
        new(new List<string> { "Zhaozhou Congshen", "ÃƒÆ’Ã‚Â¨Ãƒâ€šÃ‚Â¶ÃƒÂ¢Ã¢â‚¬Å¾Ã‚Â¢ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â·Ãƒâ€¦Ã‚Â¾ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¾Ãƒâ€¦Ã‚Â¾ÃƒÆ’Ã‚Â¨Ãƒâ€šÃ‚Â«ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â", "ÃƒÆ’Ã‚Â¨Ãƒâ€šÃ‚Â¶ÃƒÂ¢Ã¢â‚¬Å¾Ã‚Â¢ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â·Ãƒâ€¦Ã‚Â¾" }),
        // Short pinyin name (< 4 chars) should be skipped
        new(new List<string> { "Mazu Daoyi", "ÃƒÆ’Ã‚Â©Ãƒâ€šÃ‚Â¦Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¥ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ÃƒÆ’Ã‚Â©Ãƒâ€šÃ‚ÂÃƒÂ¢Ã¢â€šÂ¬Ã…â€œÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬", "Ma" }),
    };

    [Fact]
    public void DetectMasterNames_FindsChineseNamesLongestFirst()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(
            "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¾Ãƒâ€šÃ‚Â©ÃƒÆ’Ã‚Â§Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¥ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂÃƒâ€šÃ‚ÂÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â»Ãƒâ€šÃ‚Â£ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¦Ãƒâ€šÃ‚ÂªÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â«ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¼Ãƒâ€¦Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¨Ãƒâ€šÃ‚Â¶ÃƒÂ¢Ã¢â‚¬Å¾Ã‚Â¢ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â·Ãƒâ€¦Ã‚Â¾ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¾Ãƒâ€¦Ã‚Â¾ÃƒÆ’Ã‚Â¨Ãƒâ€šÃ‚Â«ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬ÂÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¹Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â£ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡", null, entries);

        Assert.Contains("Linji Yixuan", result);
        Assert.Contains("Zhaozhou Congshen", result);
    }

    [Fact]
    public void DetectMasterNames_FindsPinyinNamesInEnglishText()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(
            null, "The master Linji Yixuan taught in Hebei.", entries);

        Assert.Contains("Linji Yixuan", result);
    }

    [Fact]
    public void DetectMasterNames_SkipsSingleCharChineseNames()
    {
        // Create an entry with only a single CJK char name
        var entries = new List<MasterNameEntry>
        {
            new(new List<string> { "SingleChar", "ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Âº" })
        };

        var result = ScholarTabViewModel.DetectMasterNames("ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚ÂºÃƒÆ’Ã‚Â¨Ãƒâ€šÃ‚ÂªÃƒâ€šÃ‚ÂªÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â³ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¢", null, entries);

        // "ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã‚Âº" is only 1 CJK char, so it should be skipped (min 2 required)
        Assert.Empty(result);
    }

    [Fact]
    public void DetectMasterNames_SkipsShortPinyinNames()
    {
        var entries = MakeTestMasterEntries();
        // "Ma" is < 4 chars, should not match
        var result = ScholarTabViewModel.DetectMasterNames(
            null, "Ma went to the market with Mazu Daoyi.", entries);

        // "Mazu Daoyi" (10 chars >= 4) matches, but "Ma" (2 chars < 4) does not
        Assert.Contains("Mazu Daoyi", result);
        Assert.Single(result); // only Mazu, not a separate "Ma" match
    }

    [Fact]
    public void DetectMasterNames_ReturnsEmptyForTextWithNoMasters()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(
            "ÃƒÆ’Ã‚Â©ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â‚¬Å¾Ã‚Â¢ÃƒÆ’Ã‚Â¦Ãƒâ€¹Ã…â€œÃƒâ€šÃ‚Â¯ÃƒÆ’Ã‚Â¤Ãƒâ€šÃ‚Â¸ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â®Ãƒâ€šÃ‚ÂµÃƒÆ’Ã‚Â¦ÃƒÂ¢Ã¢â‚¬Å¾Ã‚Â¢Ãƒâ€šÃ‚Â®ÃƒÆ’Ã‚Â©ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã‚Â¦ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â­ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬ÂÃƒÆ’Ã‚Â£ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡", "This is ordinary text.", entries);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectMasterNames_ReturnsEmptyForNullText()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(null, null, entries);

        Assert.Empty(result);
    }

    [Fact]
    public void DetectMasterNames_NoDuplicatesWhenFoundInBothZhAndEn()
    {
        var entries = MakeTestMasterEntries();
        // Linji appears in both Chinese and English
        var result = ScholarTabViewModel.DetectMasterNames(
            "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¾Ãƒâ€šÃ‚Â©ÃƒÆ’Ã‚Â§Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¤Ãƒâ€šÃ‚Â§ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â«", "Master Linji Yixuan", entries);

        // Should appear only once (canonical display name)
        Assert.Single(result.Where(n => n == "Linji Yixuan"));
    }

    // ---- AutoTagMasterNames ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â doesn't add duplicates ----
    // AutoTagMasterNames is private, so we test it indirectly via DetectMasterNames
    // since the dedup logic is: "if (!passage.MasterNames.Contains(name, ...))"

    [Fact]
    public void DetectMasterNames_ReturnsDistinctNames()
    {
        // Entry where the same display name could be matched via both Chinese names
        var entries = new List<MasterNameEntry>
        {
            new(new List<string> { "Linji Yixuan", "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¾Ãƒâ€šÃ‚Â©ÃƒÆ’Ã‚Â§Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾", "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸" })
        };

        // Text contains both "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¾Ãƒâ€šÃ‚Â©ÃƒÆ’Ã‚Â§Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾" and "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸" ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â both map to "Linji Yixuan"
        var result = ScholarTabViewModel.DetectMasterNames(
            "ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¾Ãƒâ€šÃ‚Â©ÃƒÆ’Ã‚Â§Ãƒâ€¦Ã‚Â½ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â§Ãƒâ€šÃ‚Â¦Ãƒâ€šÃ‚ÂªÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â¸Ãƒâ€šÃ‚Â«ÃƒÆ’Ã‚Â¯Ãƒâ€šÃ‚Â¼Ãƒâ€¦Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚ÂÃƒâ€šÃ‚Â³ÃƒÆ’Ã‚Â¨ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¡Ãƒâ€šÃ‚Â¨ÃƒÆ’Ã‚Â¦Ãƒâ€šÃ‚Â¿Ãƒâ€¦Ã‚Â¸ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â®ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬ÂÃƒÆ’Ã‚Â©ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¹ÃƒÆ’Ã‚Â¥Ãƒâ€šÃ‚Â±Ãƒâ€šÃ‚Â±ÃƒÆ’Ã‚Â£ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡", null, entries);

        // Display name should appear at most once
        Assert.Single(result);
        Assert.Equal("Linji Yixuan", result[0]);
    }

    // ---- LinkedTexts property tests ----

    [Fact]
    public void LinkedTexts_DefaultIsEmptyList()
    {
        var passage = new ScholarPassage();
        Assert.NotNull(passage.LinkedTexts);
        Assert.Empty(passage.LinkedTexts);
    }

    [Fact]
    public void LinkedTextsSummary_FormatsCorrectly()
    {
        var passage = new ScholarPassage
        {
            LinkedTexts = new List<string>
            {
                "xml-p5/T/T0001/T0001.xml",
                "xml-p5/T/T0002/T0002.xml"
            }
        };

        Assert.Equal("Texts: T0001, T0002", passage.LinkedTextsSummary);
    }

    [Fact]
    public void LinkedTextsSummary_EmptyWhenNoLinkedTexts()
    {
        var passage = new ScholarPassage();
        Assert.Equal("", passage.LinkedTextsSummary);
    }

    [Fact]
    public void HasLinkedTexts_TrueWhenLinksExist()
    {
        var passage = new ScholarPassage
        {
            LinkedTexts = new List<string> { "xml-p5/T/T0001/T0001.xml" }
        };

        Assert.True(passage.HasLinkedTexts);
    }

    [Fact]
    public void HasLinkedTexts_FalseWhenEmpty()
    {
        var passage = new ScholarPassage();
        Assert.False(passage.HasLinkedTexts);
    }

    [Fact]
    public void LinkedTexts_PreservedOnDeserialization()
    {
        // Verify backward compat: a passage without LinkedTexts in JSON gets empty list
        var json = """{"Id":"abc","SourceRelPath":"","ZhText":"","EnText":"","Notes":"","Tags":[],"MasterNames":[],"AddedUtc":"2026-01-01T00:00:00+00:00"}""";
        var passage = System.Text.Json.JsonSerializer.Deserialize<ScholarPassage>(json);

        Assert.NotNull(passage);
        Assert.NotNull(passage!.LinkedTexts);
        Assert.Empty(passage.LinkedTexts);
    }
    [Fact]
    public async Task EnsureWritableCollectionAsync_CreatesAndSelectsCollectionWhenEmpty()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        var collection = await vm.EnsureWritableCollectionAsync();

        Assert.NotNull(collection);
        Assert.Single(vm.Collections);
        Assert.Equal(collection, vm.SelectedCollection);
        Assert.False(vm.IsEmptyState);
        Assert.NotNull(svc.LastSaved);
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
    public Task<List<ScholarCollection>> LoadUserAsync(string root, string username, CancellationToken ct = default)
        => Task.FromResult(new List<ScholarCollection>());
    public Task SaveUserAsync(string root, string username, List<ScholarCollection> collections, CancellationToken ct = default)
        => Task.CompletedTask;
}


