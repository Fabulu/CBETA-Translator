using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// PR4 tests for the streaming-flush coalescer + threshold-1 batch flush +
/// in-place SearchResultGroup identity preservation during the end-of-stream rebuild.
/// </summary>
public class SearchTabStreamingTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer]

    // ------------------------------------------------------------------
    // Test stub: multi-yield search service with per-yield gates.
    // ------------------------------------------------------------------
    private sealed class GatedSearchIndexService : ISearchIndexService
    {
        public List<SearchResultGroup> Groups { get; } = new();

        // Gate i is awaited before yielding Groups[i]. FinishGate is awaited after the last yield.
        public List<TaskCompletionSource<bool>> YieldGates { get; } = new();
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
        public void ClearBloomCache() { }
        public void ClearVerifyTextCache() { }
        public void InvalidateIndexCaches() { }
        public Task<SearchIndexManifest?> TryLoadAsync(string root) => Task.FromResult<SearchIndexManifest?>(new SearchIndexManifest());
        public Task<SearchTextManifest?> TryLoadTextManifestAsync(string root) => Task.FromResult<SearchTextManifest?>(null);
        public Task<bool> IsStaleAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null) => Task.FromResult(false);
        public Task BuildAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task BuildOrUpdateAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, bool forceRebuild, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default) => Task.CompletedTask;

        public async IAsyncEnumerable<SearchResultGroup> SearchAllAsync(
            string root, string originalDir, string translatedDir,
            SearchIndexManifest manifest, string query,
            bool includeOriginal, bool includeTranslated,
            Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta,
            int contextWidth,
            IProgress<SearchIndexService.SearchProgress>? progress = null,
            Func<string, bool>? relPathFilter = null,
            IReadOnlyList<string>? additionalOriginalDirs = null,
            IReadOnlyList<string>? additionalTranslatedDirs = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                if (i < YieldGates.Count)
                    await YieldGates[i].Task.WaitAsync(ct);
                yield return Groups[i];
            }
            await FinishGate.Task.WaitAsync(ct);
        }

        public Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(string root, string originalDir, string translatedDir, SearchIndexManifest manifest, IReadOnlyList<SearchResultGroup> groups, string query, int contextWidth, IProgress<SearchIndexService.SearchProgress>? progress = null, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>>(new Dictionary<string, IReadOnlyList<SearchResultChild>>());

        public void Dispose() { }

        public static GatedSearchIndexService WithGroups(int count)
        {
            var svc = new GatedSearchIndexService();
            for (int i = 0; i < count; i++)
            {
                svc.Groups.Add(MakeGroup($"T/T48/file{i:D3}.xml", $"File {i}", "tip"));
                svc.YieldGates.Add(new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
            }
            return svc;
        }
    }

    /// <summary>
    /// Search service whose result set can be swapped between calls (no gates).
    /// </summary>
    private sealed class SwappableSearchService : ISearchIndexService
    {
        public List<SearchResultGroup> NextResults { get; set; } = new();

        public SearchIndexService.SearchIndexServiceOptions Options => new();
        public IReadOnlyDictionary<string, int>? CorpusCharFreqs => null;
        public IReadOnlyDictionary<string, int>? CorpusBigramFreqs => null;
        public long CorpusTotalChars => 0;
        public bool HasCorpusFrequencies => false;
        public string GetManifestPath(string root) => "";
        public string GetBinPath(string root) => "";
        public string GetTextManifestPath(string root) => "";
        public string GetTextBinPath(string root) => "";
        public void ClearBloomCache() { }
        public void ClearVerifyTextCache() { }
        public void InvalidateIndexCaches() { }
        public Task<SearchIndexManifest?> TryLoadAsync(string root) => Task.FromResult<SearchIndexManifest?>(new SearchIndexManifest());
        public Task<SearchTextManifest?> TryLoadTextManifestAsync(string root) => Task.FromResult<SearchTextManifest?>(null);
        public Task<bool> IsStaleAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null) => Task.FromResult(false);
        public Task BuildAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task BuildOrUpdateAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, bool forceRebuild, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default) => Task.CompletedTask;

        public async IAsyncEnumerable<SearchResultGroup> SearchAllAsync(
            string root, string originalDir, string translatedDir,
            SearchIndexManifest manifest, string query,
            bool includeOriginal, bool includeTranslated,
            Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta,
            int contextWidth,
            IProgress<SearchIndexService.SearchProgress>? progress = null,
            Func<string, bool>? relPathFilter = null,
            IReadOnlyList<string>? additionalOriginalDirs = null,
            IReadOnlyList<string>? additionalTranslatedDirs = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            foreach (var g in NextResults)
                yield return g;
        }

        public Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(string root, string originalDir, string translatedDir, SearchIndexManifest manifest, IReadOnlyList<SearchResultGroup> groups, string query, int contextWidth, IProgress<SearchIndexService.SearchProgress>? progress = null, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>>(new Dictionary<string, IReadOnlyList<SearchResultChild>>());

        public void Dispose() { }
    }

    private static SearchResultGroup MakeGroup(string relPath, string display, string tooltip)
    {
        return new SearchResultGroup
        {
            RelPath = relPath,
            DisplayName = display,
            Tooltip = tooltip,
            HitsOriginal = 1,
            HitsTranslated = 0,
            Children = new List<SearchResultChild>
            {
                new()
                {
                    RelPath = relPath,
                    Side = SearchSide.Original,
                    Hit = new SearchHit { Index = 0, Left = "L ", Match = "x", Right = " R" }
                }
            }
        };
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            try
            {
                if (condition()) return;
            }
            catch (InvalidOperationException)
            {
                // Conditions enumerate ResultGroups while the streaming flush mutates
                // it concurrently; a torn read ("Collection was modified") is not a
                // failure, just "not settled yet" — poll again. Without this the suite
                // failed ~1 in 5 full runs on this race.
            }
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Condition was not reached in time.");
            await Task.Delay(10);
            await Task.Delay(20);
        }
    }

    private static SearchTabViewModel MakeVm(ISearchIndexService svc)
    {
        var vm = new SearchTabViewModel(svc);
        vm.SetContext("/root", "/orig", new[] { "/tran" },
            rel => (rel, rel, (TranslationStatus?)null));
        return vm;
    }

    private static DispatcherTimer? GetCoalescer(SearchTabViewModel vm)
    {
        var field = typeof(SearchTabViewModel)
            .GetField("_streamFlushCoalescer", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(vm) as DispatcherTimer;
    }

    // ==================================================================
    // 1) First result appears before second verify completes.
    // ==================================================================
    [Fact]
    public async Task Search_FirstResult_AppearsBeforeSecondVerify()
    {
        var svc = GatedSearchIndexService.WithGroups(3);
        var vm = MakeVm(svc);
        vm.Query = "x";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);
        // Release the first yield gate; the others remain blocked.
        svc.YieldGates[0].TrySetResult(true);

        await WaitForAsync(() => vm.ResultGroups.Count >= 1);

        // Exactly one full-text group is visible BEFORE the second yield is released.
        Assert.True(vm.ResultGroups.Count >= 1);
        Assert.Equal(1, vm.ResultGroups.Count(g => g.RelPath == "T/T48/file000.xml"));
        // The second/third groups must NOT be present yet.
        Assert.Equal(0, vm.ResultGroups.Count(g => g.RelPath == "T/T48/file001.xml"));
        Assert.Equal(0, vm.ResultGroups.Count(g => g.RelPath == "T/T48/file002.xml"));

        // Cleanup.
        svc.YieldGates[1].TrySetResult(true);
        svc.YieldGates[2].TrySetResult(true);
        svc.FinishGate.TrySetResult(true);
        await searchTask;
    }

    // ==================================================================
    // 2) User-expanded group survives the end-of-stream rebuild.
    // ==================================================================
    [Fact]
    public async Task Search_UserExpandsGroupMidStream_StaysExpandedAfterRebuild()
    {
        var svc = GatedSearchIndexService.WithGroups(3);
        var vm = MakeVm(svc);
        vm.Query = "x";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);

        // Release first two groups, wait for file001 to appear, then user expands it.
        // file000 is the first full-text group and will be auto-expanded by the default policy;
        // file001 starts collapsed — user expansion of file001 is the interesting toggle.
        svc.YieldGates[0].TrySetResult(true);
        svc.YieldGates[1].TrySetResult(true);
        await WaitForAsync(() => vm.ResultGroups.Count(g => g.RelPath == "T/T48/file001.xml") >= 1);

        var userExpanded = vm.ResultGroups.First(g => g.RelPath == "T/T48/file001.xml");
        userExpanded.IsExpanded = true;
        Assert.True(userExpanded.IsExpanded);

        // Release the rest and let the rebuild run.
        svc.YieldGates[2].TrySetResult(true);
        svc.FinishGate.TrySetResult(true);
        await searchTask;
        await WaitForAsync(() => !vm.IsSearching);

        // Identity is preserved via in-place mutation: same instance, IsExpanded still true.
        var after = vm.ResultGroups.First(g => g.RelPath == "T/T48/file001.xml");
        Assert.Same(userExpanded, after);
        Assert.True(after.IsExpanded);
    }

    // ==================================================================
    // 3) A subsequent independent search resets expansion for NEW groups
    //    to the default policy (first FT expanded, others collapsed).
    // ==================================================================
    [Fact]
    public async Task Search_NewSearch_DoesNotPreserveExpansion()
    {
        var svc = new SwappableSearchService
        {
            NextResults = new List<SearchResultGroup>
            {
                MakeGroup("T/T48/file000.xml", "File 0", "tip"),
                MakeGroup("T/T48/file001.xml", "File 1", "tip"),
            }
        };

        var vm = MakeVm(svc);
        vm.Query = "x";
        await vm.SearchCommand.ExecuteAsync(null);
        await WaitForAsync(() => !vm.IsSearching);

        // User expands file001 (collapsed by default).
        var secondGroup = vm.ResultGroups.First(g => g.RelPath == "T/T48/file001.xml");
        secondGroup.IsExpanded = true;

        // Second search with brand-new non-overlapping RelPaths.
        svc.NextResults = new List<SearchResultGroup>
        {
            MakeGroup("OTHER/file_a.xml", "Other A", "tipA"),
            MakeGroup("OTHER/file_b.xml", "Other B", "tipB"),
        };

        vm.Query = "y";
        await vm.SearchCommand.ExecuteAsync(null);
        await WaitForAsync(() => !vm.IsSearching);

        // Fresh groups get default expansion: first FT expanded, second collapsed.
        var fileA = vm.ResultGroups.FirstOrDefault(g => g.RelPath == "OTHER/file_a.xml");
        var fileB = vm.ResultGroups.FirstOrDefault(g => g.RelPath == "OTHER/file_b.xml");
        Assert.NotNull(fileA);
        Assert.NotNull(fileB);
        Assert.True(fileA!.IsExpanded);
        Assert.False(fileB!.IsExpanded);
        // No leftover from the previous search.
        Assert.Empty(vm.ResultGroups.Where(g => g.RelPath == "T/T48/file001.xml"));
    }

    // ==================================================================
    // 4) Master card instance is reused across streaming flushes + rebuild.
    // ==================================================================
    [Fact]
    public async Task Search_MasterCard_ReusedAcrossStreamingFlushes()
    {
        var svc = GatedSearchIndexService.WithGroups(2);
        var vm = MakeVm(svc);

        // Inject a master catalog so the query matches and a master card is inserted
        // at line 1689 BEFORE the streaming begins.
        var catalog = new ZenMasterCatalog
        {
            Records = new List<ZenMasterRecord>
            {
                new()
                {
                    CanonicalName = "Mumon Ekai",
                    Aliases = new List<string>(),
                    Variants = new List<ZenMasterVariant>
                    {
                        new() { Names = new List<string> { "Mumon Ekai" }, Floruit = 1183, Death = 1260, IsBase = true }
                    }
                }
            }
        };
        vm.SetMasterCatalog(catalog);

        vm.Query = "Mumon";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);

        // Wait until the master card appears (synchronous insert before await foreach).
        await WaitForAsync(() => vm.ResultGroups.Any(g => g.RelPath == "__master__"));
        var masterBefore = vm.ResultGroups.First(g => g.RelPath == "__master__");

        // Release everything and finish.
        svc.YieldGates[0].TrySetResult(true);
        svc.YieldGates[1].TrySetResult(true);
        svc.FinishGate.TrySetResult(true);
        await searchTask;
        await WaitForAsync(() => !vm.IsSearching);

        var masterAfter = vm.ResultGroups.First(g => g.RelPath == "__master__");
        Assert.Same(masterBefore, masterAfter); // identity preserved across the end-of-stream rebuild
    }

    // ==================================================================
    // 5) Coalescer doesn't fire after Cancel(): the field is cleared.
    // ==================================================================
    [Fact]
    public async Task Search_StreamFlushCoalescer_DoesNotFireAfterCancel()
    {
        var svc = GatedSearchIndexService.WithGroups(0); // no results
        var vm = MakeVm(svc);
        vm.Query = "x";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);

        await WaitForAsync(() => vm.IsSearching);

        // Cancel via reflection (Cancel is private).
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var method = typeof(SearchTabViewModel).GetMethod("Cancel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(vm, null);
        });

        // Allow time beyond the 60 ms coalescer interval.
        svc.FinishGate.TrySetResult(true);
        await Task.Delay(200);

        // The coalescer field has been cleared by Cancel(); no further Tick scheduled.
        Assert.Null(GetCoalescer(vm));

        try { await searchTask; } catch { /* canceled */ }
    }

    // ==================================================================
    // 6) After Cancel(), _streamFlushCoalescer is null (stopped + disposed).
    // ==================================================================
    [Fact]
    public async Task Search_StreamFlushCoalescer_DisposedOnCancel()
    {
        var svc = GatedSearchIndexService.WithGroups(0);
        var vm = MakeVm(svc);
        vm.Query = "x";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);
        await WaitForAsync(() => vm.IsSearching);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var method = typeof(SearchTabViewModel).GetMethod("Cancel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(vm, null);
        });

        Assert.Null(GetCoalescer(vm));

        svc.FinishGate.TrySetResult(true);
        try { await searchTask; } catch { /* canceled */ }
    }

    // ==================================================================
    // GAP-FILL 1: ApplyDefaultExpansionForNewGroupsOnly preserves
    //             user-expanded title-section group across rebuild.
    // ==================================================================
    [Fact]
    public void ApplyDefaultExpansionForNewGroupsOnly_PreservesPreviouslyKnownTitleSection()
    {
        // Direct unit test of the static helper via reflection. The helper is the
        // hinge of the "in-place rebuild" semantics — it must NOT clobber existing
        // groups (regardless of their RelPath, including the __title_section__
        // pseudo-path) and SHOULD apply the default policy to brand-new ones.
        var method = typeof(SearchTabViewModel).GetMethod(
            "ApplyDefaultExpansionForNewGroupsOnly",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Existing title-section (previously known) — user toggled it to collapsed.
        var titleSection = new SearchResultGroup
        {
            RelPath = "__title_section__",
            DisplayName = "Title matches",
            IsExpanded = false // user collapsed
        };
        // A brand-new full-text group: should receive default IsExpanded = true (first FT).
        var freshFt = new SearchResultGroup
        {
            RelPath = "T/T48/file000.xml",
            DisplayName = "File 0",
            IsExpanded = false
        };
        var allGroups = new List<SearchResultGroup> { titleSection, freshFt };
        var previouslyKnown = new Dictionary<string, SearchResultGroup>(StringComparer.OrdinalIgnoreCase)
        {
            ["__title_section__"] = titleSection
        };

        method!.Invoke(null, new object[] { allGroups, previouslyKnown });

        // Title section retained the user toggle — NOT auto-re-expanded by the helper.
        Assert.False(titleSection.IsExpanded);
        // Fresh full-text group got the default policy (first FT expanded).
        Assert.True(freshFt.IsExpanded);
    }

    // ==================================================================
    // GAP-FILL 2: ApplyDefaultExpansionForNewGroupsOnly suppresses
    //             auto-expand of a brand-new FT group when a previously-known
    //             FT group is already expanded ("first FT already seen" branch).
    // ==================================================================
    [Fact]
    public void ApplyDefaultExpansionForNewGroupsOnly_RespectsPriorExpandedFt()
    {
        // The helper's "firstFullTextSeen" detection scans previouslyKnown for an
        // already-expanded FT group; if one is found, any *new* FT groups must NOT
        // be auto-expanded (only the existing expanded one stays expanded). Locks in
        // the user-pinned-expansion-survives semantics across the rebuild.
        var method = typeof(SearchTabViewModel).GetMethod(
            "ApplyDefaultExpansionForNewGroupsOnly",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var existingExpandedFt = new SearchResultGroup
        {
            RelPath = "T/T48/file_existing.xml",
            IsExpanded = true // user-expanded prior to rebuild
        };
        var freshFt1 = new SearchResultGroup { RelPath = "T/T48/new1.xml", IsExpanded = false };
        var freshFt2 = new SearchResultGroup { RelPath = "T/T48/new2.xml", IsExpanded = false };

        var allGroups = new List<SearchResultGroup> { existingExpandedFt, freshFt1, freshFt2 };
        var previouslyKnown = new Dictionary<string, SearchResultGroup>(StringComparer.OrdinalIgnoreCase)
        {
            [existingExpandedFt.RelPath] = existingExpandedFt
        };

        method!.Invoke(null, new object[] { allGroups, previouslyKnown });

        // The previously-expanded FT keeps its state — helper does NOT touch known groups.
        Assert.True(existingExpandedFt.IsExpanded);
        // Both new FT groups stay COLLAPSED because the "first FT" slot is already taken
        // by the existing expanded one.
        Assert.False(freshFt1.IsExpanded);
        Assert.False(freshFt2.IsExpanded);
    }

    // ==================================================================
    // GAP-FILL 3: ApplyDefaultExpansionForNewGroupsOnly auto-expands
    //             master/title-section pseudo-groups when they are brand-new.
    // ==================================================================
    [Fact]
    public void ApplyDefaultExpansionForNewGroupsOnly_AutoExpandsNewMasterAndTitleSection()
    {
        // The helper has an explicit branch (lines 1624-1628) that force-expands
        // master/title-section pseudo-paths when they are NEW (not in previouslyKnown).
        // Locks in that these pseudo-groups always begin life expanded regardless of
        // whether any other FT group is already expanded.
        var method = typeof(SearchTabViewModel).GetMethod(
            "ApplyDefaultExpansionForNewGroupsOnly",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var newMaster = new SearchResultGroup { RelPath = "__master__", IsExpanded = false };
        var newTitleSection = new SearchResultGroup { RelPath = "__title_section__", IsExpanded = false };
        var newFt = new SearchResultGroup { RelPath = "T/T48/x.xml", IsExpanded = false };

        var allGroups = new List<SearchResultGroup> { newMaster, newTitleSection, newFt };
        var previouslyKnown = new Dictionary<string, SearchResultGroup>(StringComparer.OrdinalIgnoreCase);
        // (empty: all three are brand-new)

        method!.Invoke(null, new object[] { allGroups, previouslyKnown });

        // Master and title-section pseudo-groups are auto-expanded by the new-only policy.
        Assert.True(newMaster.IsExpanded);
        Assert.True(newTitleSection.IsExpanded);
        // First brand-new FT group is also auto-expanded (firstFullTextSeen=false initially).
        Assert.True(newFt.IsExpanded);
    }

    // ==================================================================
    // 7) Empty-results case: rebuild still produces a coherent UI state.
    // ==================================================================
    [Fact]
    public async Task Search_EmptyResults_ApplyDefaultExpansionStillRuns()
    {
        var svc = GatedSearchIndexService.WithGroups(0); // zero full-text results
        var vm = MakeVm(svc);
        vm.Query = "no-hits-zzzz";

        var searchTask = vm.SearchCommand.ExecuteAsync(null);
        svc.FinishGate.TrySetResult(true);
        await searchTask;
        await WaitForAsync(() => !vm.IsSearching);

        // No exception, ResultGroups empty (no master, no title matches), HasResults false.
        Assert.Empty(vm.ResultGroups);
        Assert.False(vm.HasResults);
        Assert.False(vm.IsSearching);
        // Coalescer was cleared by end-of-stream cleanup.
        Assert.Null(GetCoalescer(vm));
    }
}
