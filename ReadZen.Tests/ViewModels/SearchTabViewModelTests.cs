using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class SearchTabViewModelTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer]

    private static SearchTabViewModel MakeVm()
    {
        return new SearchTabViewModel(new StubSearchIndexService());
    }

    private sealed class ControlledSearchIndexService : ISearchIndexService
    {
        public TaskCompletionSource<bool> FirstYieldGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> FinishGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public SearchIndexService.SearchIndexServiceOptions Options => new();
        public IReadOnlyDictionary<string, int>? CorpusCharFreqs => null;
        public IReadOnlyDictionary<string, int>? CorpusBigramFreqs => null;
        public long CorpusTotalChars => 0;
        public bool HasCorpusFrequencies => false;
        public string GetManifestPath(string root) => "";
        public string GetBinPath(string root) => "";
        public string GetTextManifestPath(string root) => "";
        public string GetTextBinPath(string root) => "";
        public string GetCjk2ManifestPath(string root) => "";
        public void ClearBloomCache() { }
        public void ClearVerifyTextCache() { }
    public void InvalidateIndexCaches() { }
        public Task<SearchIndexManifest?> TryLoadAsync(string root) => Task.FromResult<SearchIndexManifest?>(new SearchIndexManifest());
        public Task<SearchTextManifest?> TryLoadTextManifestAsync(string root) => Task.FromResult<SearchTextManifest?>(null);
        public Task<SearchCjkBigramManifest?> TryLoadCjk2ManifestAsync(string root) => Task.FromResult<SearchCjkBigramManifest?>(null);
        public Task<bool> IsStaleAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs) => Task.FromResult(false);
        public Task BuildAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task BuildOrUpdateAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, bool forceRebuild, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default) => Task.CompletedTask;
        public async IAsyncEnumerable<SearchResultGroup> SearchAllAsync(string root, string originalDir, string translatedDir, SearchIndexManifest manifest, string query, bool includeOriginal, bool includeTranslated, Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta, int contextWidth, IProgress<SearchIndexService.SearchProgress>? progress = null, Func<string, bool>? relPathFilter = null, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            progress?.Report(new SearchIndexService.SearchProgress { Phase = "Building candidates", VerifiedDocs = 0, TotalDocsToVerify = 10, Groups = 0, TotalHits = 0 });
            await FirstYieldGate.Task.WaitAsync(ct);
            yield return new SearchResultGroup
            {
                RelPath = "T/T48/T48n2005.xml",
                DisplayName = "Blue Cliff",
                Tooltip = "T48n2005",
                Children = new List<SearchResultChild>
                {
                    new()
                    {
                        RelPath = "T/T48/T48n2005.xml",
                        Side = SearchSide.Original,
                        Hit = new SearchHit { Index = 0, Left = "left ", Match = "match", Right = " right" }
                    }
                }
            };
            progress?.Report(new SearchIndexService.SearchProgress { Phase = "Searching", VerifiedDocs = 1, TotalDocsToVerify = 10, Groups = 1, TotalHits = 1 });
            await FinishGate.Task.WaitAsync(ct);
        }
        public Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(string root, string originalDir, string translatedDir, SearchIndexManifest manifest, IReadOnlyList<SearchResultGroup> groups, string query, int contextWidth, IProgress<SearchIndexService.SearchProgress>? progress = null, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>>(new Dictionary<string, IReadOnlyList<SearchResultChild>>());
        public void Dispose() { }
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Condition was not reached in time.");
            // Yield to the dispatcher so queued Post / InvokeAsync callbacks
            // from background threads get a chance to execute.
            await Task.Delay(10);
            await Task.Delay(20);
        }
    }

    // ---- Initial state ----

    [Fact]
    public async Task SearchAsync_ShowsLoadingPlaceholderBeforeFirstResultArrives()
    {
        var svc = new ControlledSearchIndexService();
        var vm = new SearchTabViewModel(svc);
        vm.SetContext("/root", "/orig", new[] { "/tran" }, rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "wumen";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);
        await WaitForAsync(() => vm.IsSearching);

        Assert.True(vm.IsSearchProgressVisible);
        Assert.True(vm.IsResultsLoadingVisible);
        Assert.Empty(vm.ResultGroups);
        Assert.Contains("Searching", vm.ResultsLoadingText);

        svc.FirstYieldGate.TrySetResult(true);
        svc.FinishGate.TrySetResult(true);
        await searchTask;
    }

    [Fact]
    public async Task SearchAsync_ShowsFirstBatchBeforeSearchCompletes()
    {
        var svc = new ControlledSearchIndexService();
        var vm = new SearchTabViewModel(svc);
        vm.SetContext("/root", "/orig", new[] { "/tran" }, rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "wumen";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);
        svc.FirstYieldGate.TrySetResult(true);

        await WaitForAsync(() => vm.ResultGroups.Count > 0);

        Assert.True(vm.IsSearching);
        Assert.Single(vm.ResultGroups);
        Assert.False(vm.IsResultsLoadingVisible);

        svc.FinishGate.TrySetResult(true);
        await searchTask;
    }

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
        Assert.True(vm.SearchTranslated); // Always search both languages now
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
    public void DefaultContextIndex_Is80Chars()
    {
        var vm = MakeVm();
        Assert.Equal(2, vm.SelectedContextIndex);
    }
    [Fact]
    public void ContextItems_HasExpandedEntries()
    {
        var vm = MakeVm();
        Assert.Equal(new[]
        {
            "5 chars",
            "10 chars",
            "15 chars",
            "20 chars",
            "40 chars",
            "80 chars",
            "160 chars",
            "320 chars"
        }, vm.ContextItems);
    }

    [Fact]
    public void CoocMetricItems_HasEightEntries()
    {
        var vm = MakeVm();
        Assert.Equal(8, vm.CoocMetricItems.Length);
    }

    // ---- SetRootContext ----

    [Fact]
    public void SetRootContext_SetsInternalState()
    {
        var vm = MakeVm();
        vm.SetRootContext("/root", "/orig", new[] { "/tran" });

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
        vm.SetRootContext("/root", "/orig", new[] { "/tran" });
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
    [Fact]
    public void HandleResultDoubleTap_SearchResultChild_FiresNavigationRequestedWithHitData()
    {
        var vm = MakeVm();
        NavigationRequest? received = null;
        vm.NavigationRequested += (_, req) => received = req;

        var child = new SearchResultChild
        {
            RelPath = "test/file.xml",
            Side = SearchSide.Original,
            Hit = new SearchHit
            {
                Index = 42,
                Left = "?",
                Match = "?",
                Right = "?"
            }
        };

        vm.HandleResultDoubleTap(child);

        Assert.NotNull(received);
        Assert.Equal("test/file.xml", received!.RelPath);
        Assert.Equal(SearchSide.Original, received.Side);
        Assert.Equal("?", received.MatchText);
        Assert.Equal("?", received.LeftContext);
        Assert.Equal("?", received.RightContext);
        Assert.Equal(42, received.AnchorStartHint);
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
        vm.SetContext("/root", "/orig", new[] { "/tran" }, rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "";

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.True(vm.HasValidationError);
        Assert.False(string.IsNullOrEmpty(vm.ValidationMessage));
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ValidationMessageIsNonEmpty()
    {
        var vm = MakeVm();
        vm.SetContext("/root", "/orig", new[] { "/tran" }, rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.Query = "   ";

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.True(vm.HasValidationError);
        Assert.NotEqual("", vm.ValidationMessage);
    }

    [Fact]
    public async Task SearchAsync_NeitherOriginalNorTranslated_SetsValidationError()
    {
        var vm = MakeVm();
        vm.SetContext("/root", "/orig", new[] { "/tran" }, rel => ("display", "tooltip", (TranslationStatus?)null));
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

    [Fact]
    public void ExportUiState_IncludesSelectedTagFilterId()
    {
        var vm = MakeVm();
        vm.SetTagFilterData(
            new List<DocumentTag> { new() { RelPath = "T01/test.xml", TagId = "tag-1" } },
            new TagVocabulary { Tags = new List<TagDefinition> { new() { Id = "tag-1", Name = "Practice" } } });
        vm.SelectedTagFilterIndex = 1;

        var state = vm.ExportUiState();

        Assert.Equal("Practice", state.SelectedTagFilterName);
        Assert.Equal("tag-1", state.SelectedTagFilterId);
    }

    [Fact]
    public async Task ApplyUiStateAsync_AppliesVisibleFiltersById_WithoutIntermediateAutoSearch()
    {
        var vm = MakeVm();
        vm.SetContext("/root", "/orig", new[] { "/tran" }, rel => ("display", "tooltip", (TranslationStatus?)null));
        vm.SetTagFilterData(
            new List<DocumentTag> { new() { RelPath = "T01/test.xml", TagId = "tag-1" } },
            new TagVocabulary { Tags = new List<TagDefinition> { new() { Id = "tag-1", Name = "Practice" } } });

        int statusCount = 0;
        vm.StatusChanged += (_, _) => statusCount++;

        await vm.ApplyUiStateAsync(new SearchTabViewModel.SearchUiState
        {
            Query = "restored",
            SearchOriginal = false,
            SearchTranslated = true,
            ZenOnly = true,
            SelectedStatusIndex = 3,
            SelectedContextIndex = 0,
            SelectedTagFilterId = "tag-1"
        });

        await Task.Delay(250);

        Assert.Equal("restored", vm.Query);
        Assert.False(vm.SearchOriginal);
        Assert.True(vm.SearchTranslated);
        Assert.True(vm.ZenOnly);
        Assert.Equal(3, vm.SelectedStatusIndex);
        Assert.Equal(0, vm.SelectedContextIndex);
        Assert.Equal(1, vm.SelectedTagFilterIndex);
        Assert.Equal(0, statusCount);
    }

    [Fact]
    public async Task ApplyUiStateAsync_PendingTagIdRestoresAfterTagDataArrives()
    {
        var vm = MakeVm();

        await vm.ApplyUiStateAsync(new SearchTabViewModel.SearchUiState
        {
            SelectedTagFilterId = "tag-1"
        });

        vm.SetTagFilterData(
            new List<DocumentTag> { new() { RelPath = "T01/test.xml", TagId = "tag-1" } },
            new TagVocabulary { Tags = new List<TagDefinition> { new() { Id = "tag-1", Name = "Practice" } } });

        Assert.Equal(1, vm.SelectedTagFilterIndex);
    }
    [Fact]
    public async Task ApplyUiStateAsync_AndExportUiState_RoundTripsExpandedContextIndex()
    {
        var vm = MakeVm();

        await vm.ApplyUiStateAsync(new SearchTabViewModel.SearchUiState
        {
            Query = "wumenguan",
            SearchOriginal = true,
            SearchTranslated = true,
            ZenOnly = true,
            SelectedStatusIndex = 2,
            SelectedContextIndex = 5
        });

        var exported = vm.ExportUiState();

        Assert.Equal("wumenguan", exported.Query);
        Assert.True(exported.SearchOriginal);
        Assert.True(exported.SearchTranslated);
        Assert.True(exported.ZenOnly);
        Assert.Equal(2, exported.SelectedStatusIndex);
        Assert.Equal(5, exported.SelectedContextIndex);
    }

    [Fact]
    public async Task ExportCommand_UsesSelectedFormatAndWritesResults()
    {
        var vm = MakeVm();
        vm.Query = "wumenguan";
        vm.SearchOriginal = true;
        vm.SearchTranslated = true;
        vm.ZenOnly = true;
        vm.SelectedStatusIndex = 2;
        vm.SelectedTagFilterIndex = 0;
        vm.ResultGroups.Add(new SearchResultGroup
        {
            RelPath = "T/T48/T48n2005.xml",
            DisplayName = "Blue Cliff",
            Tooltip = "T48n2005",
            Children = new List<SearchResultChild>
            {
                new()
                {
                    RelPath = "T/T48/T48n2005.xml",
                    Side = SearchSide.Original,
                    Hit = new SearchHit { Index = 4, Left = "\u5DE6", Match = "\u4E2D", Right = "\u53F3" }
                }
            }
        });

        var tempDir = Path.Combine(Path.GetTempPath(), "search-vm-export-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "results.md");
        SearchExportFormat? pickedFormat = null;
        string? pickedBaseName = null;

        vm.PickExportFormatAsync = () => Task.FromResult<SearchExportFormat?>(SearchExportFormat.Markdown);
        vm.PickExportFileAsync = (format, suggestedName) =>
        {
            pickedFormat = format;
            pickedBaseName = suggestedName;
            return Task.FromResult<string?>(path);
        };

        try
        {
            await vm.ExportCommand.ExecuteAsync(null);
            var text = await File.ReadAllTextAsync(path);

            Assert.Equal(SearchExportFormat.Markdown, pickedFormat);
            Assert.Equal("search-wumenguan", pickedBaseName);
            Assert.Contains("# Search Results", text);
            Assert.Contains("Blue Cliff", text);
            Assert.Contains("wumenguan", text);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
    [Fact]
    public void AnalyticsScopeItems_ExposeCurrentAndCorpusModes()
    {
        var vm = MakeVm();

        Assert.Equal(new[] { "Current Results", "Zen Corpus", "Full Corpus (slow)" }, vm.AnalyticsScopeItems);
        Assert.Equal(0, vm.SelectedAnalyticsScopeIndex);
    }
}




