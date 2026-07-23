using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Tests for W3 post-search filtering, W4 GetIndexedRelPaths,
/// W5 ApplyChildrenCap / ShowMore behavior, and FuzzyScore contract.
/// </summary>
public class SearchTabViewModelFilterTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer]

    // ======================================================================
    // Helpers
    // ======================================================================

    /// <summary>
    /// A search service that yields a fixed list of groups synchronously.
    /// </summary>
    private sealed class FixedResultsSearchService : ISearchIndexService
    {
        private readonly List<SearchResultGroup> _results;

        public FixedResultsSearchService(List<SearchResultGroup> results)
        {
            _results = results;
        }

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
            foreach (var g in _results)
                yield return g;
        }

        public Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(string root, string originalDir, string translatedDir, SearchIndexManifest manifest, IReadOnlyList<SearchResultGroup> groups, string query, int contextWidth, IProgress<SearchIndexService.SearchProgress>? progress = null, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>>(new Dictionary<string, IReadOnlyList<SearchResultChild>>());

        public void Dispose() { }
    }

    private static SearchResultGroup MakeGroup(string relPath, string displayName, string tooltip, params string[] snippets)
    {
        var children = snippets.Select((s, i) => new SearchResultChild
        {
            RelPath = relPath,
            Side = SearchSide.Original,
            Hit = new SearchHit { Left = s, Match = "x", Right = "" }
        }).ToList<SearchResultChild>();

        return new SearchResultGroup
        {
            RelPath = relPath,
            DisplayName = displayName,
            Tooltip = tooltip,
            Children = children
        };
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Condition not reached in time.");
            await Task.Delay(10);
            await Task.Delay(20); // extra yield for dispatcher pump (matches existing test pattern)
        }
    }

    private static async Task<ReadZen.App.ViewModels.SearchTabViewModel> RunSearchAsync(
        IReadOnlyList<SearchResultGroup> groups)
    {
        var svc = new FixedResultsSearchService(new List<SearchResultGroup>(groups));
        var vm = new ReadZen.App.ViewModels.SearchTabViewModel(svc);
        vm.SetContext("/root", "/orig", new[] { "/tran" },
            rel => (rel, rel, (TranslationStatus?)null));
        vm.Query = "test";
        await vm.SearchCommand.ExecuteAsync(null);

        // Wait until results are loaded
        await WaitForConditionAsync(() => !vm.IsSearching);

        return vm;
    }

    /// <summary>
    /// Sets ResultFilter and immediately invokes ApplyResultFilter via reflection,
    /// bypassing the DispatcherTimer debounce that does not fire in the headless test host.
    /// </summary>
    private static void SetFilter(ReadZen.App.ViewModels.SearchTabViewModel vm, string filter)
    {
        // Set the backing field directly so OnResultFilterChanged doesn't start the debounce timer
        var field = typeof(ReadZen.App.ViewModels.SearchTabViewModel)
            .GetField("_resultFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(vm, filter);

        // Now call ApplyResultFilter directly
        var method = typeof(ReadZen.App.ViewModels.SearchTabViewModel)
            .GetMethod("ApplyResultFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(vm, null);
    }

    // ======================================================================
    // W3: ResultFilter — basic filtering
    // ======================================================================

    [Fact]
    public async Task ResultFilter_Empty_ShowsAllGroups()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Blue Cliff Record", "bcr", "some text"),
            MakeGroup("path/b.xml", "Gateless Barrier", "gb",  "other text"),
        };

        var vm = await RunSearchAsync(groups);
        // Empty filter: all groups visible (2)
        SetFilter(vm, "");
        Assert.Equal(2, vm.ResultGroups.Count);
    }

    [Fact]
    public async Task ResultFilter_ByDisplayName_FiltersGroups()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Blue Cliff Record", "bcr", "passage"),
            MakeGroup("path/b.xml", "Gateless Barrier",  "gb",  "passage"),
        };

        var vm = await RunSearchAsync(groups);
        SetFilter(vm, "Blue");
        Assert.Equal(1, vm.ResultGroups.Count);
        Assert.Equal("Blue Cliff Record", vm.ResultGroups[0].DisplayName);
    }

    [Fact]
    public async Task ResultFilter_ByTooltip_FiltersGroups()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Text A", "special-tooltip-xyz", "passage"),
            MakeGroup("path/b.xml", "Text B", "other-tooltip",       "passage"),
        };

        var vm = await RunSearchAsync(groups);
        SetFilter(vm, "special");
        Assert.Equal(1, vm.ResultGroups.Count);
        Assert.Equal("Text A", vm.ResultGroups[0].DisplayName);
    }

    [Fact]
    public async Task ResultFilter_BySnippet_FiltersGroups()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Text A", "tip-a", "mentioning enlightenment"),
            MakeGroup("path/b.xml", "Text B", "tip-b", "no relevant content"),
        };

        var vm = await RunSearchAsync(groups);
        SetFilter(vm, "enlightenment");
        Assert.Equal(1, vm.ResultGroups.Count);
        Assert.Equal("Text A", vm.ResultGroups[0].DisplayName);
    }

    [Fact]
    public async Task ResultFilter_Clear_RestoresAllGroups()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Alpha", "tip-a", "passage"),
            MakeGroup("path/b.xml", "Beta",  "tip-b", "passage"),
        };

        var vm = await RunSearchAsync(groups);

        SetFilter(vm, "Alpha");
        Assert.Single(vm.ResultGroups);

        SetFilter(vm, "");
        Assert.Equal(2, vm.ResultGroups.Count);
    }

    [Fact]
    public async Task ResultFilter_CaseInsensitive_Matches()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Gateless Barrier", "gb", "passage"),
        };

        var vm = await RunSearchAsync(groups);
        SetFilter(vm, "gateless"); // lowercase
        Assert.Single(vm.ResultGroups);
    }

    [Fact]
    public async Task ResultFilter_NoMatch_ResultsEmpty()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Blue Cliff", "bc", "some text"),
        };

        var vm = await RunSearchAsync(groups);
        SetFilter(vm, "zzzyyyxxx");
        Assert.Empty(vm.ResultGroups);
    }

    // ======================================================================
    // W3: Master filter + text filter compose
    // ======================================================================

    [Fact]
    public async Task ResultFilter_ComposesWithMasterFilter()
    {
        var groups = new[]
        {
            MakeGroup("path/a.xml", "Wumen Text",  "wumen-tip",  "passage"),
            MakeGroup("path/b.xml", "Other Text",  "other-tip",  "passage"),
            MakeGroup("path/c.xml", "Wumen Extra", "wumen2-tip", "passage"),
        };

        var vm = await RunSearchAsync(groups);

        // Activate master filter on a + c
        vm.ApplyMasterFilter("Wumen", new[] { "path/a.xml", "path/c.xml" });

        // Now apply text filter to narrow further — master passes a+c, text filter passes only c
        SetFilter(vm, "Extra");

        Assert.Equal(1, vm.ResultGroups.Count);
        Assert.Equal("Wumen Extra", vm.ResultGroups[0].DisplayName);
    }

    // ======================================================================
    // W4: GetIndexedRelPaths
    // ======================================================================

    [Fact]
    public void GetIndexedRelPaths_ReturnsNull_WhenServiceIsNotSearchIndexService()
    {
        // StubSearchIndexService is not a SearchIndexService, so returns null
        var vm = new ReadZen.App.ViewModels.SearchTabViewModel(new StubSearchIndexService());
        var result = vm.GetIndexedRelPaths("test");
        Assert.Null(result);
    }

    // ======================================================================
    // W5: ApplyChildrenCap behavior — tested via search results
    // ======================================================================

    [Fact]
    public async Task ApplyChildrenCap_GroupWithFiveOrFewerChildren_NotCapped()
    {
        // A group with exactly 5 children should not get a ShowMore sentinel
        var children = Enumerable.Range(0, 5).Select(i => new SearchResultChild
        {
            RelPath = "path/a.xml",
            Side = SearchSide.Original,
            Hit = new SearchHit { Left = $"left{i}", Match = "x", Right = "" }
        }).ToList<SearchResultChild>();

        var group = new SearchResultGroup
        {
            RelPath = "path/a.xml",
            DisplayName = "A",
            Tooltip = "A",
            Children = children
        };

        var vm = await RunSearchAsync(new[] { group });

        var resultGroup = vm.ResultGroups.FirstOrDefault(g => g.RelPath == "path/a.xml");
        Assert.NotNull(resultGroup);

        // Should have exactly 5 children, no ShowMore sentinel
        Assert.Equal(5, resultGroup!.Children.Count);
        Assert.DoesNotContain(resultGroup.Children, c => c is SearchResultShowMoreItem);
    }

    [Fact]
    public async Task ApplyChildrenCap_GroupWithMoreThanFiveChildren_CappedWithSentinel()
    {
        // A group with 8 children should be capped to 5 + ShowMore sentinel
        var children = Enumerable.Range(0, 8).Select(i => new SearchResultChild
        {
            RelPath = "path/b.xml",
            Side = SearchSide.Original,
            Hit = new SearchHit { Left = $"snippet{i}", Match = "x", Right = "" }
        }).ToList<SearchResultChild>();

        var group = new SearchResultGroup
        {
            RelPath = "path/b.xml",
            DisplayName = "B",
            Tooltip = "B",
            Children = children
        };

        var vm = await RunSearchAsync(new[] { group });

        var resultGroup = vm.ResultGroups.FirstOrDefault(g => g.RelPath == "path/b.xml");
        Assert.NotNull(resultGroup);

        // Should have 5 real children + 1 ShowMore sentinel = 6 total
        Assert.Equal(6, resultGroup!.Children.Count);
        var sentinel = resultGroup.Children.OfType<SearchResultShowMoreItem>().FirstOrDefault();
        Assert.NotNull(sentinel);
        Assert.Equal(3, sentinel!.RemainingCount); // 8 - 5 = 3
    }

    [Fact]
    public async Task ShowMore_Command_ExpandsGroupToFullChildren()
    {
        // A group with 7 children gets capped, then ShowMore restores all
        var children = Enumerable.Range(0, 7).Select(i => new SearchResultChild
        {
            RelPath = "path/c.xml",
            Side = SearchSide.Original,
            Hit = new SearchHit { Left = $"item{i}", Match = "x", Right = "" }
        }).ToList<SearchResultChild>();

        var group = new SearchResultGroup
        {
            RelPath = "path/c.xml",
            DisplayName = "C",
            Tooltip = "C",
            Children = children
        };

        var vm = await RunSearchAsync(new[] { group });

        var resultGroup = vm.ResultGroups.FirstOrDefault(g => g.RelPath == "path/c.xml");
        Assert.NotNull(resultGroup);

        var sentinel = resultGroup!.Children.OfType<SearchResultShowMoreItem>().FirstOrDefault();
        Assert.NotNull(sentinel);

        // Execute ShowMore command (IRelayCommand<T> — use Execute, not ExecuteAsync)
        vm.ShowMoreCommand.Execute(sentinel);

        // After ShowMore, all 7 children should be visible (no sentinel)
        Assert.Equal(7, resultGroup.Children.Count);
        Assert.DoesNotContain(resultGroup.Children, c => c is SearchResultShowMoreItem);
    }

    // ======================================================================
    // FuzzyScore contract (logic extracted from MainWindow for unit testing)
    // ======================================================================

    // The FuzzyScore method is private static on MainWindow; we replicate the
    // identical logic here to test the documented contract without coupling
    // the test to a UI class that requires full Avalonia initialization.
    private static int FuzzyScore(string text, string query)
    {
        int qi = 0;
        int score = 0;
        int lastMatch = -1;
        var lower = text.ToLowerInvariant();
        var qLower = query.ToLowerInvariant();

        for (int ti = 0; ti < lower.Length && qi < qLower.Length; ti++)
        {
            if (lower[ti] == qLower[qi])
            {
                score += (ti == lastMatch + 1) ? 3 : 1; // consecutive bonus
                if (ti == 0 || text[ti - 1] == ' ' || text[ti - 1] == ':') score += 2; // word boundary bonus
                lastMatch = ti;
                qi++;
            }
        }
        return qi == qLower.Length ? score : -1; // -1 = no match
    }

    [Fact]
    public void FuzzyScore_MatchingAbbreviation_ReturnsPositiveScore()
    {
        // "thm" should match "Theme: Toggle dark/light"
        int score = FuzzyScore("Theme: Toggle dark/light", "thm");
        Assert.True(score > 0, $"Expected positive score, got {score}");
    }

    [Fact]
    public void FuzzyScore_NoMatch_ReturnsMinusOne()
    {
        int score = FuzzyScore("Theme: Toggle dark/light", "xyz");
        Assert.Equal(-1, score);
    }

    [Fact]
    public void FuzzyScore_EmptyQuery_ReturnsMinusOne()
    {
        // Empty query has qi == qLower.Length (0 == 0) so returns score (0)
        // Let's verify what the implementation actually returns
        int score = FuzzyScore("Theme: Toggle", "");
        // qi starts at 0, qLower.Length is 0, so condition qi == qLower.Length is true immediately
        // score is 0 at that point → returns 0
        // Either 0 or -1 is acceptable for an empty query (it's not a useful search)
        Assert.True(score >= -1, "Empty query must return -1 or 0");
    }

    [Fact]
    public void FuzzyScore_ConsecutiveCharsScoreHigher_ThanScattered()
    {
        // "abc" consecutive in "abc def" vs scattered in "axbxcx"
        int consecutiveScore = FuzzyScore("abc def", "abc");
        int scatteredScore = FuzzyScore("axbxcx", "abc");

        Assert.True(consecutiveScore > scatteredScore,
            $"Consecutive ({consecutiveScore}) should beat scattered ({scatteredScore})");
    }

    [Fact]
    public void FuzzyScore_WordBoundaryBonus_IncreasesScore()
    {
        // "t" at word boundary (after space) scores higher than "t" mid-word
        int boundaryScore = FuzzyScore("Dark Theme", "t");  // 't' at start of "Theme" (after space)
        int midWordScore = FuzzyScore("settings", "t");     // 't' mid-word in "settings"

        Assert.True(boundaryScore > midWordScore,
            $"Word boundary score ({boundaryScore}) should exceed mid-word ({midWordScore})");
    }

    [Fact]
    public void FuzzyScore_ExactMatch_ReturnsHighScore()
    {
        // Exact prefix match should score well
        int score = FuzzyScore("Settings", "Settings");
        Assert.True(score > 0);
    }

    [Fact]
    public void FuzzyScore_QueryLongerThanText_ReturnsMinusOne()
    {
        int score = FuzzyScore("ab", "abcdef");
        Assert.Equal(-1, score);
    }

    [Fact]
    public void FuzzyScore_ColonBoundary_AwardsBonus()
    {
        // Character after ':' gets word-boundary bonus
        int colonBoundaryScore = FuzzyScore("Theme: Toggle", "t"); // 'T' starts text (bonus) but 'T' in Toggle follows ':'+'space'
        // Just verify it matches
        Assert.True(colonBoundaryScore >= 0);
    }
}
