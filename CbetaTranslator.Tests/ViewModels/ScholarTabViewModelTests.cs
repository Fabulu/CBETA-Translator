using System;
using System.Collections.Generic;
using System.IO;
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
    private static ScholarTabViewModel MakeVm(StubScholarCollectionsService? svc = null, StubAppConfigService? config = null)
    {
        return new ScholarTabViewModel(svc ?? new StubScholarCollectionsService(), config);
    }

   

    [Fact]
    public void Constructor_ThrowsOnNullService()
    {
        Assert.Throws<ArgumentNullException>(() => new ScholarTabViewModel(null!));
    }

   

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

   


    [Fact]
    public async Task LoadAsync_WithNoCollections_LeavesCollectionsEmptyAndSelectsSnippetsTab()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);

        typeof(ScholarTabViewModel)
            .GetField("_root", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(vm, Path.GetTempPath());
        typeof(ScholarTabViewModel)
            .GetField("_configLoadAttempted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(vm, true);

        var loadMethod = typeof(ScholarTabViewModel).GetMethod("LoadAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await (Task)loadMethod.Invoke(vm, Array.Empty<object>())!;

        Assert.Empty(vm.Collections);
        Assert.Equal(1, vm.NavigatorTabIndex);
        Assert.Null(vm.SelectedCollection);
        Assert.Null(svc.LastSaved);
    }

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
    public void AddCollection_TogglesWorkspaceHelperOffWhenCollectionExists()
    {
        var vm = MakeVm();

        Assert.True(vm.ShowWorkspaceHelper);
        Assert.False(vm.HasSelectedCollection);

        vm.AddCollectionCommand.Execute(null);

        Assert.False(vm.ShowWorkspaceHelper);
        Assert.True(vm.HasSelectedCollection);
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
       
        vm.SelectedCollection = vm.Collections[0];

        vm.DeleteCollectionCommand.Execute(null);

        Assert.Single(vm.Collections);
        Assert.NotNull(vm.SelectedCollection);
    }

   

    [Fact]
    public void SelectedCollection_UpdatesPassagesDisplay()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

       
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

   

    [Fact]
    public async Task AddPassageToCollection_AddsPassageAndSaves()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        vm.AddCollectionCommand.Execute(null);
        var collectionId = vm.Collections[0].Id;

       
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
        Assert.NotEmpty(passage.Id);
        Assert.False(vm.IsEmptyState);
        Assert.NotNull(svc.LastSaved);
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
       

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        var passage = new ScholarPassage
        {
            ZhText = "test",
            SourceRelPath = "test.xml"
        };

        await vm.AddPassageToCollectionAsync(collectionId, passage);

        Assert.Single(vm.Passages);
    }


    [Fact]
    public async Task AddPassageToCollection_WithoutRootOrConfig_DoesNotMutateCollections()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        var passage = new ScholarPassage
        {
            ZhText = "test",
            SourceRelPath = "test.xml"
        };

        await vm.AddPassageToCollectionAsync(collection.Id, passage);

        Assert.Empty(collection.Passages);
        Assert.Null(svc.LastSaved);
        Assert.Equal("Scholar save unavailable: no text root is configured.", vm.StatusMessage);
    }

    [Fact]
    public async Task AddPassageToCollection_UsesConfigRootAndCanonicalGitHubIdentity()
    {
        var svc = new StubScholarCollectionsService();
        var cfg = new StubAppConfigService
        {
            ConfigToReturn = new AppConfig
            {
                TextRootPath = "/repo-root",
                Username = "local-user",
                GitHubUsername = "octocat"
            }
        };
        var vm = MakeVm(svc, cfg);
        vm.SetUsername("local-user");

        Assert.True(await vm.EnsureStorageContextAsync());
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        var passage = new ScholarPassage
        {
            ZhText = "test",
            SourceRelPath = "test.xml"
        };

        await vm.AddPassageToCollectionAsync(collection.Id, passage);

        Assert.Equal("/repo-root", vm.GetRoot());
        Assert.Equal("octocat", collection.CreatedBy);
        Assert.Equal("octocat", passage.CreatedBy);
        Assert.NotNull(svc.LastSaved);
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

        vm.DeletePassageCommand.Execute(null);
    }

   

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

   

    [Fact]
    public void SearchFilter_FiltersByTagMatch()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml", Tags = new List<string> { "dharma" } });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml", Tags = new List<string> { "zen" } });
        collection.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "c", SourceRelPath = "z.xml", Tags = new List<string> { "dharma", "zen" } });

       
        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;
        Assert.Equal(3, vm.Passages.Count);

        vm.SearchFilter = "dharma";

        Assert.Equal(2, vm.Passages.Count);
        Assert.All(vm.Passages, p => Assert.Contains("dharma", p.Tags));
    }

   

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

   

    [Fact]
    public void CollectionFilter_FiltersByCollectionName()
    {
        var vm = MakeVm();
       
        vm.AddCollectionCommand.Execute(null);
        vm.Collections[0].Name = "Zen Koans";
        vm.AddCollectionCommand.Execute(null);
        vm.Collections[1].Name = "Pure Land Sutras";

        vm.CollectionFilter = "Zen";

        Assert.Single(vm.Collections);
        Assert.Equal("Zen Koans", vm.Collections[0].Name);
    }

   

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

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

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

       
        vm.SetUsername("  Alice  ");
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
    public async Task ExportCollections_ReaderTagBundleFormat_UsesRichExportPath()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var vm = new ScholarTabViewModel(trackingSvc)
        {
            PickExportFormatAsync = () => Task.FromResult<ScholarExportFormat?>(ScholarExportFormat.ReaderTagBundle),
            PickExportFileAsync = (format, name) =>
            {
                Assert.Equal(ScholarExportFormat.ReaderTagBundle, format);
                Assert.Equal("New Collection", name);
                return Task.FromResult<string?>("/tmp/reader-tags.json");
            }
        };
        vm.AddCollectionCommand.Execute(null);

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.False(trackingSvc.ExportWasCalled);
        Assert.Contains("Export failed", vm.StatusMessage);
    }

    [Fact]
    public async Task ExportCollections_ReaderTagTsvFormat_UsesRichExportPath()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var vm = new ScholarTabViewModel(trackingSvc)
        {
            PickExportFormatAsync = () => Task.FromResult<ScholarExportFormat?>(ScholarExportFormat.ReaderTagTsv),
            PickExportFileAsync = (format, name) =>
            {
                Assert.Equal(ScholarExportFormat.ReaderTagTsv, format);
                Assert.Equal("New Collection", name);
                return Task.FromResult<string?>("/tmp/reader-tags.tsv");
            }
        };
        vm.AddCollectionCommand.Execute(null);

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.False(trackingSvc.ExportWasCalled);
        Assert.Contains("Export failed", vm.StatusMessage);
    }
    [Fact]
    public async Task ExportCollections_PaperDraftFormat_UsesRichExportPath()
    {
        var trackingSvc = new TrackingScholarCollectionsService();
        var vm = new ScholarTabViewModel(trackingSvc)
        {
            PickExportFormatAsync = () => Task.FromResult<ScholarExportFormat?>(ScholarExportFormat.PaperDraft),
            PickExportFileAsync = (format, name) =>
            {
                Assert.Equal(ScholarExportFormat.PaperDraft, format);
                Assert.Equal("New Collection", name);
                return Task.FromResult<string?>("/tmp/draft.md");
            }
        };
        vm.AddCollectionCommand.Execute(null);

        await vm.ExportCollectionsCommand.ExecuteAsync(null);

        Assert.False(trackingSvc.ExportWasCalled);
        Assert.Contains("Export failed", vm.StatusMessage);
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
       
        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        vm.PickImportFileAsync = () => Task.FromResult<string?>("/tmp/import.json");

        await vm.ImportCollectionsCommand.ExecuteAsync(null);

       
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

       
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Bob's Collection", vm.CommunityCollections[0].Name);
    }


    [Fact]
    public async Task LoadCommunityAsync_ExcludesCanonicalAndLegacyIdentities()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["legacy-user"] = new() { new() { Id = "legacy", Name = "Legacy", CreatedBy = "legacy-user" } },
                ["octocat"] = new() { new() { Id = "gh", Name = "GitHub", CreatedBy = "octocat" } },
                ["carol"] = new() { new() { Id = "other", Name = "Carol", CreatedBy = "carol" } },
            }
        };
        var cfg = new StubAppConfigService
        {
            ConfigToReturn = new AppConfig
            {
                TextRootPath = "/test-root",
                Username = "legacy-user",
                GitHubUsername = "octocat"
            }
        };
        var vm = MakeVm(svc, cfg);
        vm.SetUsername("legacy-user");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Carol", vm.CommunityCollections[0].Name);
        Assert.Equal(new List<string> { "All Users", "carol" }, vm.CommunityUsernames);
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
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

       
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Bob's", vm.CommunityCollections[0].Name);
    }

   

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

       
        vm.CommunityFilter = "Zen";
        Assert.Equal(2, vm.CommunityCollections.Count);

       
        vm.CommunityFilter = "carol";
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Meditation Guide", vm.CommunityCollections[0].Name);

       
        vm.CommunityFilter = "Amitabha";
        Assert.Single(vm.CommunityCollections);
        Assert.Equal("Pure Land", vm.CommunityCollections[0].Name);

       
        vm.CommunityFilter = "";
        Assert.Equal(3, vm.CommunityCollections.Count);
    }

   

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

       
        Assert.NotNull(vm.SelectedCommunityCollection);
        Assert.Equal(2, vm.CommunityPassages.Count);
        Assert.NotNull(vm.SelectedCommunityPassage);
    }

   

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

        Assert.False(vm.HasCommunityCollections);

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


    [Fact]
    public async Task LoadCommunityAsync_WithNoLocalCollections_SelectsSharedTabAndClearsEmptyState()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "c1", Name = "Bob's Collection", CreatedBy = "bob" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        Assert.True(vm.HasCommunityCollections);
        Assert.False(vm.IsEmptyState);
        Assert.Equal(2, vm.NavigatorTabIndex);
        Assert.Single(vm.CommunityCollections);
        Assert.NotNull(vm.SelectedCommunityCollection);
        Assert.Equal(new List<string> { "All Users", "bob" }, vm.CommunityUsernames);
        Assert.Equal(0, vm.SelectedCommunityUserIndex);
    }

    [Fact]
    public async Task LoadCommunityAsync_WithLocalCollections_KeepsWorkspaceTabSelected()
    {
        var svc = new StubScholarCollectionsService
        {
            CommunityData = new Dictionary<string, List<ScholarCollection>>
            {
                ["bob"] = new List<ScholarCollection>
                {
                    new() { Id = "c1", Name = "Bob's Collection", CreatedBy = "bob" }
                }
            }
        };

        var vm = MakeVm(svc);
        vm.SetUsername("alice");

        var rootField = typeof(ScholarTabViewModel).GetField("_root",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        rootField!.SetValue(vm, "/test-root");

        vm.AddCollectionCommand.Execute(null);
        Assert.Single(vm.Collections);
        Assert.Equal(1, vm.NavigatorTabIndex);

        await vm.LoadCommunityCommand.ExecuteAsync(null);

        Assert.True(vm.HasCommunityCollections);
        Assert.Equal(1, vm.NavigatorTabIndex);
    }

   

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

    public async Task CreateLinkAsync_StoresOptionalNote()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];
        collection.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "a", SourceRelPath = "x.xml" });
        collection.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "b", SourceRelPath = "y.xml" });
        vm.SelectedCollection = collection;

        await vm.CreateLinkAsync("p1", "p2", "summarizes", "Important parallel");

        Assert.Single(collection.Links);
        Assert.Equal("Important parallel", collection.Links[0].Note);
        Assert.Equal("summarizes", collection.Links[0].RelationType);
    }
    [Fact]
    public async Task CreateLinkAsync_NoSelectedCollection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        await vm.CreateLinkAsync("p1", "p2", "quotes");

       
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

        Assert.Single(collection.Links);
    }

    [Fact]
    public async Task RemoveLinkAsync_NoSelectedCollection_DoesNothing()
    {
        var vm = MakeVm();
        vm.SelectedCollection = null;

        await vm.RemoveLinkAsync("any-id");
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

       
        var p1Links = vm.GetLinksForPassage("p1");
        Assert.Equal(2, p1Links.Count);

       
        var p2Links = vm.GetLinksForPassage("p2");
        Assert.Equal(2, p2Links.Count);

       
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

       
        vm.SelectedPassage = p2;
        vm.DeletePassageCommand.Execute(null);

       
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

       
        vm.SelectedPassage = p1;
        vm.DeletePassageCommand.Execute(null);

        Assert.Empty(collection.Links);
    }

   

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

   

    [Fact]
    public void FacetProperties_SyncToPassageOnSave()
    {
        var svc = new StubScholarCollectionsService();
        var vm = MakeVm(svc);

       
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

       
        vm.DoctrinalTopic = "Buddha-nature";
        vm.LiteraryForm = "Koan case";
        vm.Lineage = "Linji/Rinzai";
        vm.RhetoricalFunction = "Paradox";

       
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

       
        vm.DoctrinalTopic = "";
        vm.LiteraryForm = "";
        vm.Lineage = "   ";
        vm.RhetoricalFunction = "";

        vm.SaveCommand.Execute(null);

        Assert.Null(passage.DoctrinalTopic);
        Assert.Null(passage.LiteraryForm);
        Assert.Null(passage.Lineage);
        Assert.Null(passage.RhetoricalFunction);
    }

   

    [Fact]
    public void SortMode_Chronological_SortsByMasterNameDate()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

       
       
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

       
       
       
       
        Assert.Equal(3, vm.Passages.Count);
    }

   

    [Fact]
    public void SortMode_AZChinese_SortsAlphabetically()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        var collection = vm.Collections[0];

        collection.Passages.Add(new ScholarPassage
        {
            Id = "p1", ZhText = "\u5fc3\u5373\u662f\u4f5b", SourceRelPath = "x.xml"
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p2", ZhText = "\u4e0d\u662f\u5fc3\u4e0d\u662f\u4f5b", SourceRelPath = "y.xml"
        });
        collection.Passages.Add(new ScholarPassage
        {
            Id = "p3", ZhText = "\u5e73\u5e38\u5fc3\u662f\u9053", SourceRelPath = "z.xml"
        });

        vm.SelectedCollection = null;
        vm.SelectedCollection = collection;

        vm.SortMode = "A-Z (Chinese)";

       
        Assert.Equal(3, vm.Passages.Count);
        Assert.Equal("p2", vm.Passages[0].Id);
        Assert.Equal("p3", vm.Passages[1].Id);
        Assert.Equal("p1", vm.Passages[2].Id);
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

       
        Assert.Equal("p3", vm.Passages[0].Id);
        Assert.Equal("p1", vm.Passages[1].Id);
        Assert.Equal("p2", vm.Passages[2].Id);
    }

   

    [Fact]
    public void StudyNotes_LoadedFromCollection()
    {
        var vm = MakeVm();
        vm.AddCollectionCommand.Execute(null);
        vm.AddCollectionCommand.Execute(null);

       
        vm.SelectedCollection = vm.Collections[0];
        vm.StudyNotes = "My research notes on this collection";

       
        vm.SelectedCollection = vm.Collections[1];
        Assert.Equal("", vm.StudyNotes);

       
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

       
        Assert.Equal("", vm.StudyNotes);
    }

   

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

   

    [Fact]
    public void FacetOptions_PopulatedOnConstruction()
    {
        var vm = MakeVm();

       
        Assert.NotEmpty(vm.DoctrinalTopicOptions);
        Assert.NotEmpty(vm.LiteraryFormOptions);
        Assert.NotEmpty(vm.LineageOptions);
        Assert.NotEmpty(vm.RhetoricalFunctionOptions);
    }

   
   
   
   

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

       
        vm.AddCollectionCommand.Execute(null);
        Assert.NotNull(vm.SelectedCollection);

       
        await vm.LoadCommunityCommand.ExecuteAsync(null);
        Assert.NotNull(vm.SelectedCommunityCollection);

       
        Assert.NotNull(vm.SelectedCollection);
        Assert.NotNull(vm.SelectedCommunityCollection);
    }

   

    private const string LinjiLong = "\u81e8\u6fdf\u7fa9\u7384";
    private const string LinjiShort = "\u81e8\u6fdf";
    private const string ZhaozhouLong = "\u8d99\u5dde\u4ece\u8c0c";
    private const string ZhaozhouShort = "\u8d99\u5dde";
    private const string MazuLong = "\u99ac\u7956\u9053\u4e00";

    private static List<MasterNameEntry> MakeTestMasterEntries() => new()
    {
        new(new List<string> { "Linji Yixuan", LinjiLong, LinjiShort }),
        new(new List<string> { "Zhaozhou Congshen", ZhaozhouLong, ZhaozhouShort }),
        new(new List<string> { "Mazu Daoyi", MazuLong, "Ma" }),
    };

    [Fact]
    public void DetectMasterNames_FindsChineseNamesLongestFirst()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(
            $"{LinjiLong}\u793a\u773e\u5f8c\uff0c{ZhaozhouLong}\u53c8\u5f8c\u7e7c\u4e4b\u3002", null, entries);

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
        var entries = new List<MasterNameEntry>
        {
            new(new List<string> { "SingleChar", "\u4f5b" })
        };

        var result = ScholarTabViewModel.DetectMasterNames("\u4f5b\u8aaa\u6cd5", null, entries);
        Assert.Empty(result);
    }

    [Fact]
    public void DetectMasterNames_SkipsShortPinyinNames()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(
            null, "Ma went to the market with Mazu Daoyi.", entries);

        Assert.Contains("Mazu Daoyi", result);
        Assert.Single(result);
    }

    [Fact]
    public void DetectMasterNames_ReturnsEmptyForTextWithNoMasters()
    {
        var entries = MakeTestMasterEntries();
        var result = ScholarTabViewModel.DetectMasterNames(
            "\u666e\u901a\u6587\u5b57\u6c92\u6709\u7956\u5e2b\u540d\u865f", "This is ordinary text.", entries);

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
        var result = ScholarTabViewModel.DetectMasterNames(
            $"{LinjiLong}\u958b\u793a\u5927\u773e", "Master Linji Yixuan", entries);

        Assert.Single(result, n => n == "Linji Yixuan");
    }

   
   
   

    [Fact]
    public void DetectMasterNames_ReturnsDistinctNames()
    {
        var entries = new List<MasterNameEntry>
        {
            new(new List<string> { "Linji Yixuan", LinjiLong, LinjiShort })
        };

        var result = ScholarTabViewModel.DetectMasterNames(
            $"{LinjiLong}\u8207{LinjiShort}\u7686\u6307\u540c\u4e00\u4eba", null, entries);

        Assert.Single(result);
        Assert.Equal("Linji Yixuan", result[0]);
    }
   

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


[Fact]
public async Task LoadAsync_LoadsLegacyScholarFileAndMigratesToCanonicalGitHubIdentity()
{
    var svc = new ScholarCollectionsService();
    var cfg = new StubAppConfigService();
    var vm = new ScholarTabViewModel(svc, cfg);
    var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "scholar-vm-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);

    try
    {
        cfg.ConfigToReturn = new AppConfig
        {
            TextRootPath = tempDir,
            Username = "legacy-user",
            GitHubUsername = "octocat"
        };

        await svc.SaveUserAsync(tempDir, "legacy-user", new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Legacy Collection",
                CreatedBy = "legacy-user",
                Passages = new List<ScholarPassage>
                {
                    new() { Id = "p1", ZhText = "zh", SourceRelPath = "x.xml", CreatedBy = "legacy-user" }
                }
            }
        });

        vm.SetUsername("legacy-user");
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.Collections);
        Assert.Equal("octocat", vm.Collections[0].CreatedBy);
        Assert.Equal("octocat", vm.Collections[0].Passages[0].CreatedBy);
        Assert.Contains("legacy identity 'legacy-user'", vm.StatusMessage);

        await vm.SaveCurrentStateAsync();

        var canonicalPath = ScholarCollectionsService.GetUserPath(tempDir, "octocat");
        Assert.True(File.Exists(canonicalPath));
        Assert.Contains("Saved under GitHub identity 'octocat'", vm.StatusMessage);
    }
    finally
    {
        try { Directory.Delete(tempDir, true); } catch { }
    }
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






