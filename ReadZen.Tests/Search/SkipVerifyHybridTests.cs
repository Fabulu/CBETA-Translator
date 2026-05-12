using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// Tests for the PR2 skip-verify hybrid path (2-char pure-CJK queries).
///
/// The hybrid: when a query is exactly two indexable-CJK characters, the bigram
/// inverted index already proves contiguous adjacency, so <c>VerifyFileAllHits</c>
/// is redundant — except verify is also the snippet collector. Hybrid: verify the
/// top-N candidates (ordered by size desc) for snippets, skip verify for the long
/// tail.
///
/// Tests use a real on-disk synthetic corpus + a real <see cref="SearchIndexService"/>
/// because the verify-call site is internal to SearchAllAsync. We assert on the
/// observable side effects:
///   - <see cref="SearchResultChild.IsSkippedVerify"/> = true on placeholder rows
///   - <see cref="SearchIndexService.LastSearchSkippedVerifyGroups"/> exposes counts
///   - emitted-group count equals candidate-list size in both paths
/// </summary>
public class SkipVerifyHybridTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;
    private readonly string _tranDir;

    public SkipVerifyHybridTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-skipverify-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);
        _origDir = Path.Combine(_tempRoot, "xml-p5");
        _tranDir = Path.Combine(_tempRoot, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    // ===== IsTwoCharCjk predicate (boundary cases) =====

    [Fact]
    public void IsTwoCharCjk_BoundaryCases()
    {
        // null/empty/short/long latin
        Assert.False(SearchIndexService.IsTwoCharCjk(null));
        Assert.False(SearchIndexService.IsTwoCharCjk(""));
        Assert.False(SearchIndexService.IsTwoCharCjk("a"));
        Assert.False(SearchIndexService.IsTwoCharCjk("ab"));

        // CJK length-1: not 2 chars
        Assert.False(SearchIndexService.IsTwoCharCjk("無")); // 無 (single CJK char)

        // CJK length-2 pure: the happy path
        Assert.True(SearchIndexService.IsTwoCharCjk("無門")); // 無門

        // CJK length-3: not 2 chars
        Assert.False(SearchIndexService.IsTwoCharCjk("無門關")); // 無門關

        // Mixed CJK + Latin: not all CJK
        Assert.False(SearchIndexService.IsTwoCharCjk("無a")); // 無a
        Assert.False(SearchIndexService.IsTwoCharCjk("a無")); // a無

        // Surrogate pair: char.Length == 2 but neither code unit is in IsIndexableCjk
        // (CJK Extension B starts at U+20000 = high surrogate U+D840, low surrogate U+DC00).
        // CJK Extension B characters are *out of scope* for the BMP-only IsIndexableCjk;
        // a surrogate pair must therefore return false.
        string surrogatePair = char.ConvertFromUtf32(0x20000); // CJK Ext B "𠀀"
        Assert.Equal(2, surrogatePair.Length); // sanity: pair occupies 2 chars
        Assert.False(SearchIndexService.IsTwoCharCjk(surrogatePair));
    }

    // ===== Integration tests over a real index =====

    /// <summary>
    /// Build a synthetic corpus of <paramref name="fileCount"/> XML files all containing
    /// <paramref name="match"/> at least once, with varying body sizes (so the size-desc
    /// candidate ordering is non-trivial). File index <c>i</c> has body length proportional
    /// to <c>i</c> so that file 49 is the *largest* and file 0 is the *smallest*.
    /// </summary>
    private async Task<SearchIndexService> BuildCorpusAsync(int fileCount, string match, int skipVerifyTopN = 20)
    {
        for (int i = 0; i < fileCount; i++)
        {
            // Body filler scaled with index so largest files have most repetitions of `match`.
            // Each file is unique so the index treats them as separate entries.
            var filler = new System.Text.StringBuilder();
            for (int k = 0; k <= i; k++)
                filler.Append(match);
            // Pad with non-matching CJK so file sizes diverge clearly between i and i+1.
            filler.Append(new string('中', (i + 1) * 50)); // 中 (filler char, not in the query)

            var path = Path.Combine(_origDir, $"f{i:D3}.xml");
            File.WriteAllText(path, $"<TEI><text><body>{filler}</body></text></TEI>");
        }

        var svc = new SearchIndexService();
        svc.Options.SkipVerifySnippetTopN = skipVerifyTopN;
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });
        return svc;
    }

    private async Task<List<SearchResultGroup>> RunSearchAsync(
        SearchIndexService svc,
        string query,
        bool includeTranslated = false)
    {
        var manifest = await svc.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);
        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            _tempRoot,
            _origDir,
            _tranDir,
            manifest!,
            query,
            includeOriginal: true,
            includeTranslated: includeTranslated,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 30))
        {
            groups.Add(g);
        }
        return groups;
    }

    [Fact]
    public async Task Search_TwoCharCjkQuery_AboveTopN_SkipsVerify()
    {
        // 50 files all containing 無門. Top-N=20 means exactly 20 groups should be
        // verified (have real snippets) and the remaining 30 should be skip-verify.
        var svc = await BuildCorpusAsync(fileCount: 50, match: "無門", skipVerifyTopN: 20);
        var groups = await RunSearchAsync(svc, "無門");

        Assert.Equal(50, groups.Count);

        int skipped = groups.Count(g => g.Children.Count > 0 && g.Children[0].IsSkippedVerify);
        int verified = groups.Count(g => g.Children.Count > 0 && !g.Children[0].IsSkippedVerify);

        Assert.Equal(20, verified);
        Assert.Equal(30, skipped);

        // Mirror via the service-level counters.
        Assert.Equal(30, svc.LastSearchSkippedVerifyGroups);
        Assert.Equal(20, svc.LastSearchVerifiedGroups);
    }

    [Fact]
    public async Task Search_TwoCharCjkQuery_BelowTopN_VerifiesAll()
    {
        // Only 10 files, top-N=20: every candidate verifies, no skip.
        var svc = await BuildCorpusAsync(fileCount: 10, match: "無門", skipVerifyTopN: 20);
        var groups = await RunSearchAsync(svc, "無門");

        Assert.Equal(10, groups.Count);
        Assert.All(groups, g => Assert.False(g.Children.Count > 0 && g.Children[0].IsSkippedVerify));
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
        Assert.Equal(10, svc.LastSearchVerifiedGroups);
    }

    [Fact]
    public async Task Search_ThreeCharCjkQuery_VerifiesEvery()
    {
        // 30 files all containing 無門關. Query length 3 falls through the
        // hybrid gate (only 2-char CJK qualifies); every candidate is verified.
        var svc = await BuildCorpusAsync(fileCount: 30, match: "無門關", skipVerifyTopN: 5);
        var groups = await RunSearchAsync(svc, "無門關");

        Assert.Equal(30, groups.Count);
        Assert.All(groups, g => Assert.False(g.Children.Count > 0 && g.Children[0].IsSkippedVerify));
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
    }

    [Fact]
    public async Task Search_TwoCharNonCjkQuery_VerifiesEvery()
    {
        // Build a corpus with the Latin literal "ab" inside every body. Although
        // the query is length-2, both chars are non-CJK so IsTwoCharCjk returns
        // false and the hybrid does not engage.
        for (int i = 0; i < 30; i++)
        {
            var filler = string.Concat(Enumerable.Repeat("ab ", i + 1));
            File.WriteAllText(
                Path.Combine(_origDir, $"f{i:D3}.xml"),
                $"<TEI><text><body>{filler}</body></text></TEI>");
        }

        var svc = new SearchIndexService();
        svc.Options.SkipVerifySnippetTopN = 5;
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });
        var groups = await RunSearchAsync(svc, "ab");

        // All groups verified (no Latin skip-verify in the hybrid).
        Assert.All(groups, g => Assert.False(g.Children.Count > 0 && g.Children[0].IsSkippedVerify));
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
    }

    [Fact]
    public async Task Search_TwoCharCjkQuery_OrdersBySize_SkipsSmallest()
    {
        // 25 files with monotonically increasing body size. Top-N=5 means the 5
        // largest should be verified; the 20 smallest should be skipped.
        var svc = await BuildCorpusAsync(fileCount: 25, match: "無門", skipVerifyTopN: 5);
        var groups = await RunSearchAsync(svc, "無門");

        Assert.Equal(25, groups.Count);

        // We can identify each result group by the rel-path number suffix (f000.xml … f024.xml).
        // Build a map: relPath -> wasVerified.
        var verifiedRels = groups
            .Where(g => g.Children.Count > 0 && !g.Children[0].IsSkippedVerify)
            .Select(g => g.RelPath)
            .ToList();
        var skippedRels = groups
            .Where(g => g.Children.Count > 0 && g.Children[0].IsSkippedVerify)
            .Select(g => g.RelPath)
            .ToList();

        Assert.Equal(5, verifiedRels.Count);
        Assert.Equal(20, skippedRels.Count);

        // The verified set should be the FIVE LARGEST files (f020 … f024), since
        // body length increases monotonically with the file index.
        var expectedVerified = Enumerable.Range(20, 5).Select(i => $"f{i:D3}.xml").ToHashSet();
        var actualVerifiedNames = verifiedRels.Select(Path.GetFileName).ToHashSet();
        Assert.True(expectedVerified.SetEquals(actualVerifiedNames!),
            $"Expected verified = [{string.Join(",", expectedVerified)}], got [{string.Join(",", actualVerifiedNames!)}]");

        // And the smallest (f000) MUST be in the skipped set.
        Assert.Contains(skippedRels, r => Path.GetFileName(r) == "f000.xml");
    }

    [Fact]
    public async Task Search_TwoCharCjkQuery_PreservesHitCount()
    {
        // Total emitted group count must equal candidate-list length even when
        // verify is skipped for the long tail. We assert no result is silently dropped.
        var svc = await BuildCorpusAsync(fileCount: 40, match: "無門", skipVerifyTopN: 10);
        var groups = await RunSearchAsync(svc, "無門");

        Assert.Equal(40, groups.Count);

        int totalGroupsWithChildren = groups.Count(g => g.Children.Count > 0);
        Assert.Equal(40, totalGroupsWithChildren);

        // Each side-mask hit should be either verified-with-real-snippet OR skipped-placeholder.
        Assert.Equal(40, svc.LastSearchSkippedVerifyGroups + svc.LastSearchVerifiedGroups);
        Assert.Equal(10, svc.LastSearchVerifiedGroups);
        Assert.Equal(30, svc.LastSearchSkippedVerifyGroups);

        // For skipped groups, the placeholder should have IsSkippedVerify=true and
        // empty/placeholder snippet text — never a populated SnippetText.
        foreach (var g in groups.Where(g => g.Children[0].IsSkippedVerify))
        {
            Assert.True(g.HitsOriginal >= 1 || g.HitsTranslated >= 1);
            // Placeholder snippet is empty (no Left/Match/Right populated).
            Assert.Equal("", g.Children[0].Hit.Match);
            Assert.Equal("", g.Children[0].Hit.Left);
            Assert.Equal("", g.Children[0].Hit.Right);
        }

        // For verified groups, snippet text should be populated (Match = the query).
        foreach (var g in groups.Where(g => !g.Children[0].IsSkippedVerify))
        {
            // Match text should contain the query — VerifyFileAllHits records the
            // actual matched substring there.
            Assert.NotEqual("", g.Children[0].Hit.Match);
        }
    }

    // ===== Gap-fill: degenerate top-N values + serialization =====

    [Fact]
    public async Task Search_TwoCharCjkQuery_TopNZero_DisablesHybrid_VerifiesAll()
    {
        // SkipVerifySnippetTopN == 0 is the documented "disable hybrid" sentinel
        // (option-doc: "Set to 0 to disable the hybrid"). Every candidate must verify
        // — the size-sort path also must not engage, but its absence is observable only
        // by counters: 0 skipped, all verified.
        var svc = await BuildCorpusAsync(fileCount: 30, match: "無門", skipVerifyTopN: 0);
        var groups = await RunSearchAsync(svc, "無門");

        Assert.Equal(30, groups.Count);
        Assert.All(groups, g => Assert.False(g.Children.Count > 0 && g.Children[0].IsSkippedVerify));
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
        Assert.Equal(30, svc.LastSearchVerifiedGroups);
    }

    [Fact]
    public async Task Search_TwoCharCjkQuery_TopNIntMax_DegeneratesToVerifyEverything()
    {
        // SkipVerifySnippetTopN == int.MaxValue: hybrid engages (size sort runs), but
        // Math.Min(candidateList.Count, int.MaxValue) clamps the budget to the full set,
        // so verifyBudgetRelPaths contains every relPath and skipVerify is always false.
        // This is the "everyone in the budget" edge case — locks in that the clamp doesn't
        // overflow and the gate logic correctly degenerates to verify-all.
        var svc = await BuildCorpusAsync(fileCount: 15, match: "無門", skipVerifyTopN: int.MaxValue);
        var groups = await RunSearchAsync(svc, "無門");

        Assert.Equal(15, groups.Count);
        Assert.All(groups, g => Assert.False(g.Children.Count > 0 && g.Children[0].IsSkippedVerify));
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
        Assert.Equal(15, svc.LastSearchVerifiedGroups);
    }

    [Fact]
    public void IsSkippedVerify_RoundTripsThroughJsonSerialization()
    {
        // The skip-verify placeholder may be persisted (search results export, saved-search
        // recovery, telemetry, etc.). The flag must round-trip through System.Text.Json so
        // downstream consumers can distinguish placeholders from real hits.
        var original = new SearchResultChild
        {
            RelPath = "T01/T0001.xml",
            Side = SearchSide.Original,
            IsSkippedVerify = true,
            Hit = new SearchHit { Index = 0, Left = "", Match = "", Right = "" }
        };

        var opts = new JsonSerializerOptions { WriteIndented = false };
        var json = JsonSerializer.Serialize(original, opts);
        Assert.Contains("\"IsSkippedVerify\":true", json);

        var deserialized = JsonSerializer.Deserialize<SearchResultChild>(json, opts);
        Assert.NotNull(deserialized);
        Assert.True(deserialized!.IsSkippedVerify);
        Assert.Equal("T01/T0001.xml", deserialized.RelPath);

        // Default value (false) round-trips too — confirms property is not [JsonIgnore]'d.
        var defaultChild = new SearchResultChild { RelPath = "x.xml", Hit = new SearchHit() };
        var jsonDefault = JsonSerializer.Serialize(defaultChild, opts);
        var roundDefault = JsonSerializer.Deserialize<SearchResultChild>(jsonDefault, opts);
        Assert.NotNull(roundDefault);
        Assert.False(roundDefault!.IsSkippedVerify);
    }
}
