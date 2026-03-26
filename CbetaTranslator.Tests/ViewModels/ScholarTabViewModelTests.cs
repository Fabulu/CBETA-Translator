using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
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
}
