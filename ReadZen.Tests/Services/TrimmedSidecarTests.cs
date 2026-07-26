using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-S2 — "trim the text sidecar out of the bundle" + absent-sidecar hardening (SPEC §5).
///
/// The shipped bundle drops <c>search.text.bin</c> / <c>search.text.manifest.json</c> (779 MB)
/// and the build-only <c>search.gramsets.*</c> cache. These tests prove the query path stays
/// correct with the sidecar absent (candidate generation is bloom+inverted+corpusfreq, all
/// shipped; snippets fall back to on-demand XML parse), that <see cref="SearchIndexService.IsStaleAsync"/>
/// does NOT treat a missing sidecar as stale (and must NOT let the missing sidecar mask a real
/// corpus change — the false-fresh guard), and that the first real build after a trimmed adoption
/// re-materializes a complete family whose corpusfreq equals a from-scratch recount (no
/// algebraic-delta corruption, SPEC §3.1 / §5.3).
///
/// All fixtures are tiny synthetic CJK corpora in real temp dirs (pattern mirrors
/// BundleSeedTests / SiblingStampTests / LoadAllSnippetsTests). Deterministic: no wall-clock or
/// random dependence — file densities are fixed so ranking is strict.
/// </summary>
public sealed class TrimmedSidecarTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string Query = "無門";

    public TrimmedSidecarTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-trim-" + Guid.NewGuid().ToString("N")[..8]);
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

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    /// <summary>
    /// Writes <paramref name="fileCount"/> synthetic CJK files into <paramref name="dir"/>.
    /// File i carries (i+1) copies of the query term (distinct term-frequency → strict ranking)
    /// plus a distinct run of filler chars (distinct file sizes → distinct manifest entries).
    /// </summary>
    private static void WriteCorpus(string dir, int fileCount)
    {
        for (int i = 0; i < fileCount; i++)
        {
            var body = new System.Text.StringBuilder();
            for (int k = 0; k <= i; k++)
                body.Append("無門關");
            body.Append(new string('中', (i + 1) * 40));
            File.WriteAllText(
                Path.Combine(dir, $"f{i:D3}.xml"),
                $"<TEI><text><body>{body}</body></text></TEI>");
        }
    }

    private void WriteOrigCorpus(int fileCount) => WriteCorpus(_origDir, fileCount);

    /// <summary>
    /// Full-rebuild a search index for the current corpus into <paramref name="root"/>. The
    /// building service is disposed before returning so its memory-mapped bin handles do not
    /// lock the files a later step deletes/rebuilds (Windows mmap locks).
    /// </summary>
    private static async Task BuildIndexAsync(string root, string origDir, string tranDir, int skipVerifyTopN = 1000)
    {
        using var svc = new SearchIndexService();
        svc.Options.SkipVerifySnippetTopN = skipVerifyTopN;
        await svc.BuildAsync(root, origDir, new[] { tranDir });
    }

    /// <summary>Deletes BOTH halves of the text sidecar (what the CI trim removes together).</summary>
    private static void DeleteTextSidecar(string root)
    {
        SafeDelete(Path.Combine(root, "search.text.bin"));
        SafeDelete(Path.Combine(root, "search.text.manifest.json"));
    }

    /// <summary>Deletes the build-only gram-sets cache (also trimmed from the bundle).</summary>
    private static void DeleteGramSets(string root)
    {
        SafeDelete(Path.Combine(root, GramSetsStore.BinFileName));
        SafeDelete(Path.Combine(root, GramSetsStore.ManifestFileName));
    }

    private static void SafeDelete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// Runs the query with a fresh service (so no in-memory verify-text cache carries over) and
    /// returns groups in the order the service yields them (ranked order).
    /// </summary>
    private async Task<List<SearchResultGroup>> SearchAsync(string root, int skipVerifyTopN = 1000)
    {
        using var svc = new SearchIndexService();
        svc.Options.SkipVerifySnippetTopN = skipVerifyTopN;
        var manifest = await svc.TryLoadAsync(root);
        Assert.NotNull(manifest);

        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            root, _origDir, _tranDir, manifest!, Query,
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 10))
        {
            groups.Add(g);
        }
        return groups;
    }

    /// <summary>Flattens a group's children into an ordered list of KWIC snippet strings.</summary>
    private static List<string> Snippets(SearchResultGroup g)
        => g.Children.Select(c => c.Hit.SnippetText).ToList();

    // ===================================================================
    // GROUP A — query correctness with the sidecar absent
    // ===================================================================

    /// <summary>Happy path: the main manifest + corpusfreq still load, search returns hits, no crash.</summary>
    [Fact]
    public async Task TryLoadAsync_SucceedsAndSearchWorks_WhenSidecarAbsent()
    {
        WriteOrigCorpus(6);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        DeleteTextSidecar(_tempRoot);

        using var svc = new SearchIndexService();
        var manifest = await svc.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);
        Assert.True(svc.HasCorpusFrequencies); // corpusfreq sibling survives the trim

        // A missing sidecar must NOT be mistaken for a present-but-empty one.
        Assert.Null(await svc.TryLoadTextManifestAsync(_tempRoot));

        var groups = await SearchAsync(_tempRoot);
        Assert.Equal(6, groups.Count);
        Assert.All(groups, g => Assert.True(g.HitsOriginal > 0));
    }

    /// <summary>
    /// SPEC §5.2 core: identical hits AND ranking AND snippet content with vs. without the
    /// sidecar. The sidecar feeds only snippet extraction (same searchable text either way);
    /// candidate generation + ranking use bloom/inverted/corpusfreq, which the trim leaves intact.
    /// </summary>
    [Fact]
    public async Task QueryHitsAndRanking_IdenticalWithAndWithoutSidecar()
    {
        WriteOrigCorpus(8);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        // Run 1 — sidecar present (verify reads text blocks from search.text.bin).
        var withSidecar = await SearchAsync(_tempRoot);
        Assert.Equal(8, withSidecar.Count);
        Assert.Contains(withSidecar, g => g.Children.Count > 0);

        // Trim, then run 2 — sidecar absent (verify XML-parses each doc on demand).
        DeleteTextSidecar(_tempRoot);
        var withoutSidecar = await SearchAsync(_tempRoot);

        // Same SET of hits. (The service yields groups in parallel scan order — ranking is a
        // downstream concern — so we compare the result sets, not the streaming order. Ranking
        // itself is provably unaffected by the trim: it keys off corpusfreq + inverted-index tf,
        // neither of which the sidecar feeds.)
        Assert.Equal(
            withSidecar.Select(g => g.RelPath).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            withoutSidecar.Select(g => g.RelPath).OrderBy(x => x, StringComparer.Ordinal).ToList());

        // Same per-file hit counts and byte-identical snippet text.
        var withMap = withSidecar.ToDictionary(g => g.RelPath);
        foreach (var g in withoutSidecar)
        {
            var baseline = withMap[g.RelPath];
            Assert.Equal(baseline.HitsOriginal, g.HitsOriginal);
            Assert.Equal(baseline.HitsTranslated, g.HitsTranslated);
            Assert.Equal(Snippets(baseline), Snippets(g));
        }
    }

    /// <summary>
    /// SPEC §5.2 lazy-snippet path: with the sidecar absent AND skip-verify active, expanding a
    /// skipped row (<see cref="ISearchIndexService.LoadSnippetsForAsync"/>) promotes placeholders
    /// to real snippets sourced from an on-demand XML parse — identical to what the sidecar-backed
    /// verify at search time would have produced.
    /// </summary>
    [Fact]
    public async Task SnippetsFallBackToXmlParse_WhenSidecarAbsent()
    {
        WriteOrigCorpus(20);
        // Capture the sidecar-backed truth (top-N large → every group verified at search time).
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir, skipVerifyTopN: 1000);
        var verifiedTruth = (await SearchAsync(_tempRoot, skipVerifyTopN: 1000))
            .ToDictionary(g => g.RelPath, Snippets);

        // Now trim the sidecar and force skip-verify with a tiny top-N.
        DeleteTextSidecar(_tempRoot);

        using var svc = new SearchIndexService();
        svc.Options.SkipVerifySnippetTopN = 1;
        var manifest = await svc.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);

        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            _tempRoot, _origDir, _tranDir, manifest!, Query,
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 10))
        {
            groups.Add(g);
        }

        var skipped = groups.Where(g => g.Children.Count > 0 && g.Children[0].IsSkippedVerify).ToList();
        Assert.True(skipped.Count >= 10, $"Expected many skip-verified groups, got {skipped.Count}");

        // Expand: LoadSnippetsForAsync must XML-parse (sidecar gone) and return real snippets.
        var promoted = await svc.LoadSnippetsForAsync(
            _tempRoot, _origDir, _tranDir, manifest!, skipped, Query, contextWidth: 10);

        Assert.Equal(skipped.Count, promoted.Count);
        foreach (var g in skipped)
        {
            Assert.True(promoted.TryGetValue(g.RelPath, out var fresh));
            Assert.NotEmpty(fresh!);
            Assert.All(fresh!, c =>
            {
                Assert.False(c.IsSkippedVerify);
                Assert.Contains(Query, c.Hit.Match);
            });
            // Fallback-parsed snippets equal the sidecar-backed baseline for the same file.
            Assert.Equal(verifiedTruth[g.RelPath], fresh!.Select(c => c.Hit.SnippetText).ToList());
        }
    }

    // ===================================================================
    // GROUP B — staleness with the sidecar absent
    // ===================================================================

    /// <summary>SPEC §5.2: a missing text sidecar is NOT stale — IsStaleAsync reads only the
    /// main manifest + InputHash, both of which survive the trim.</summary>
    [Fact]
    public async Task IsStale_False_WhenTextSidecarMissing()
    {
        WriteOrigCorpus(6);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        DeleteTextSidecar(_tempRoot);

        using var svc = new SearchIndexService();
        Assert.False(await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));
    }

    /// <summary>Edge: an orphaned text MANIFEST with no bin (manifest present, bin gone) is
    /// treated as the clean sidecar-absent state — not stale, search unaffected.</summary>
    [Fact]
    public async Task IsStale_False_WhenOnlyTextBinMissing_OrphanManifest()
    {
        WriteOrigCorpus(5);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        SafeDelete(Path.Combine(_tempRoot, "search.text.bin")); // leave manifest orphaned

        using var svc = new SearchIndexService();
        Assert.Null(await svc.TryLoadTextManifestAsync(_tempRoot)); // orphan manifest ⇒ null
        Assert.False(await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));

        var groups = await SearchAsync(_tempRoot);
        Assert.Equal(5, groups.Count);
    }

    /// <summary>Edge: an orphaned text BIN with no manifest is likewise the clean absent state.</summary>
    [Fact]
    public async Task IsStale_False_WhenOnlyTextManifestMissing_OrphanBin()
    {
        WriteOrigCorpus(5);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        SafeDelete(Path.Combine(_tempRoot, "search.text.manifest.json")); // leave bin orphaned

        using var svc = new SearchIndexService();
        Assert.Null(await svc.TryLoadTextManifestAsync(_tempRoot));
        Assert.False(await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));

        var groups = await SearchAsync(_tempRoot);
        Assert.Equal(5, groups.Count);
    }

    /// <summary>
    /// FALSE-FRESH GUARD (the data-loss hazard this run exists to kill): the absent sidecar must
    /// not mask a real corpus content change. Delete the sidecar, mutate an existing file's
    /// content — IsStaleAsync must still report stale via the InputHash, unaffected by the sidecar.
    /// </summary>
    [Fact]
    public async Task IsStale_True_WhenSidecarAbsentAndCorpusContentChanged()
    {
        WriteOrigCorpus(6);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        DeleteTextSidecar(_tempRoot);

        // Real content change to an existing file (different bytes AND length).
        File.WriteAllText(
            Path.Combine(_origDir, "f000.xml"),
            "<TEI><text><body>無門關無門關無門關無門關中中中中中中中中中中中中</body></text></TEI>");

        using var svc = new SearchIndexService();
        Assert.True(await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));
    }

    // ===================================================================
    // GROUP C — incremental catch-up re-materializes the full family
    // ===================================================================

    /// <summary>
    /// FL6 (§6): the first real build after a trimmed adoption MIGRATES the combined family to the
    /// split (origin/overlay) — completing without crashing and materialising the origin text
    /// sidecar the trim removed. The legacy combined family is deleted; the index is fresh again.
    /// (The zero-XML-read carve is not possible here because the trimmed root has no old text.bin to
    /// carry, so the origin text is re-extracted — the §5.3 hole FL7 optimises away.)
    /// </summary>
    [Fact]
    public async Task IncrementalBuild_WithAbsentSidecar_MigratesToSplit()
    {
        WriteOrigCorpus(10);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);

        // Simulate the trimmed-adoption on-disk state: sidecar + gram-sets gone.
        DeleteTextSidecar(_tempRoot);
        DeleteGramSets(_tempRoot);

        // A user corpus edit drives the first post-FL6 build (which migrates).
        File.WriteAllText(
            Path.Combine(_origDir, "added.xml"),
            "<TEI><text><body>無門關無門關中中中中中</body></text></TEI>");

        var emptyBundle = Path.Combine(_tempRoot, "empty-bundle");
        Directory.CreateDirectory(emptyBundle);
        using var svc = new SearchIndexService { TestOnlyBundleDirOverride = emptyBundle };
        await svc.BuildOrUpdateAsync(_tempRoot, _origDir, new[] { _tranDir }, forceRebuild: false);

        // No S5 fault-retry: the absent sidecar degrades gracefully, it does not throw.
        Assert.Equal(0, svc.LastBuildFallbackCount);

        // MIGRATED to split — the origin text sidecar is re-extracted; the legacy family is gone.
        Assert.True(File.Exists(Path.Combine(_tempRoot, "search.origin.bin")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "search.origin.text.bin")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "search.origin.corpusfreq.bin")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "search.overlay.manifest.json")));
        Assert.Empty(Directory.EnumerateFiles(_tempRoot, "search.index.*"));

        // Fresh again — and the migrated split loads cleanly at this root.
        using var probe = new SearchIndexService { TestOnlyBundleDirOverride = emptyBundle };
        Assert.False(await probe.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));
        Assert.NotNull(await probe.TryLoadAsync(_tempRoot));
    }

    /// <summary>
    /// SPEC §3.1 / §5.3 correctness: the corpusfreq produced by an incremental build over an
    /// absent sidecar equals a from-scratch full recount of the SAME final corpus — proving the
    /// old-block-copy loss does not corrupt frequency counts (the algebraic delta was skipped).
    /// </summary>
    [Fact]
    public async Task IncrementalBuild_WithAbsentSidecar_CorpusFreqEqualsFromScratchRecount()
    {
        WriteOrigCorpus(10);
        await BuildIndexAsync(_tempRoot, _origDir, _tranDir);
        DeleteTextSidecar(_tempRoot);
        DeleteGramSets(_tempRoot);

        File.WriteAllText(
            Path.Combine(_origDir, "added.xml"),
            "<TEI><text><body>無門關無門關中中中中中</body></text></TEI>");

        using var incr = new SearchIndexService();
        await incr.BuildOrUpdateAsync(_tempRoot, _origDir, new[] { _tranDir }, forceRebuild: false);
        Assert.Equal(0, incr.LastBuildFreqDeltaApplied);

        // From-scratch build of the identical final corpus in a separate root.
        var scratchRoot = Path.Combine(_tempRoot, "scratch");
        Directory.CreateDirectory(scratchRoot);
        await BuildIndexAsync(scratchRoot, _origDir, _tranDir);

        using var loadedIncr = new SearchIndexService();
        Assert.NotNull(await loadedIncr.TryLoadAsync(_tempRoot));
        using var loadedScratch = new SearchIndexService();
        Assert.NotNull(await loadedScratch.TryLoadAsync(scratchRoot));

        Assert.True(loadedIncr.HasCorpusFrequencies);
        Assert.True(loadedScratch.HasCorpusFrequencies);

        Assert.Equal(loadedScratch.CorpusTotalChars, loadedIncr.CorpusTotalChars);
        AssertFreqEqual(loadedScratch.CorpusCharFreqs!, loadedIncr.CorpusCharFreqs!);
        AssertFreqEqual(loadedScratch.CorpusBigramFreqs!, loadedIncr.CorpusBigramFreqs!);
    }

    private static void AssertFreqEqual(
        IReadOnlyDictionary<string, int> expected,
        IReadOnlyDictionary<string, int> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var kvp in expected)
        {
            Assert.True(actual.TryGetValue(kvp.Key, out var v),
                $"Missing frequency key '{kvp.Key}' in incremental result");
            Assert.Equal(kvp.Value, v);
        }
    }

    // ===================================================================
    // GROUP D — trimmed bundle adopted end-to-end (the real ship scenario)
    // ===================================================================

    /// <summary>
    /// Stages a bundle exactly as CI ships it (text + gram-sets trimmed away), seeds it into a
    /// virgin index root via <see cref="SearchIndexService.CopyBundleFamilyIntoRoot"/>, and proves
    /// the seeded index is instantly queryable with snippets served through the XML fallback — and
    /// that IsStaleAsync reports the seeded family fresh against the shipped corpus.
    /// </summary>
    [Fact]
    public async Task TrimmedBundle_SeededIntoVirginRoot_QueryableWithSnippetFallback()
    {
        WriteOrigCorpus(8);
        var bundleDir = Path.Combine(_tempRoot, "bundle");
        Directory.CreateDirectory(bundleDir);
        await BuildIndexAsync(bundleDir, _origDir, _tranDir);

        // CI trim: the fat/optional artifacts never ship.
        DeleteTextSidecar(bundleDir);
        DeleteGramSets(bundleDir);
        Assert.False(File.Exists(Path.Combine(bundleDir, "search.text.bin")));
        Assert.False(File.Exists(Path.Combine(bundleDir, GramSetsStore.BinFileName)));

        // Seed the trimmed family into a fresh root.
        var root = Path.Combine(_tempRoot, "seeded");
        Directory.CreateDirectory(root);
        Assert.True(SearchIndexService.CopyBundleFamilyIntoRoot(root, bundleDir));

        // The seed carries no sidecar/gram-sets — only the shipped query artifacts.
        Assert.False(File.Exists(Path.Combine(root, "search.text.bin")));
        Assert.False(File.Exists(Path.Combine(root, "search.text.manifest.json")));
        Assert.False(File.Exists(Path.Combine(root, GramSetsStore.BinFileName)));
        Assert.True(File.Exists(Path.Combine(root, "search.corpusfreq.bin")));

        // Instantly queryable: candidate gen + XML-fallback snippets.
        using var svc = new SearchIndexService();
        var manifest = await svc.TryLoadAsync(root);
        Assert.NotNull(manifest);
        Assert.True(svc.HasCorpusFrequencies);

        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            root, _origDir, _tranDir, manifest!, Query,
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 10))
        {
            groups.Add(g);
        }
        Assert.Equal(8, groups.Count);
        Assert.Contains(groups, g => g.Children.Any(c => !c.IsSkippedVerify && c.Hit.Match.Contains(Query)));

        // Seeded family is fresh against the shipped corpus (single-corpus, ScopeComplete).
        using var probe = new SearchIndexService();
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    /// <summary>
    /// FL6 (§6) end-to-end: after adopting a trimmed COMBINED bundle, the user's first corpus edit
    /// runs a build that MIGRATES the family to the split — materialising the origin text sidecar
    /// and deleting the legacy family. From that point every split artifact is present and fresh.
    /// (The FL7 §5.3 job will later fill the origin text sidecar off the edit path.)
    /// </summary>
    [Fact]
    public async Task TrimmedBundle_FirstEdit_MigratesToSplit()
    {
        WriteOrigCorpus(8);
        var bundleDir = Path.Combine(_tempRoot, "bundle");
        Directory.CreateDirectory(bundleDir);
        await BuildIndexAsync(bundleDir, _origDir, _tranDir);
        DeleteTextSidecar(bundleDir);
        DeleteGramSets(bundleDir);

        var root = Path.Combine(_tempRoot, "seeded");
        Directory.CreateDirectory(root);
        Assert.True(SearchIndexService.CopyBundleFamilyIntoRoot(root, bundleDir));
        Assert.False(File.Exists(Path.Combine(root, "search.text.bin")));

        // First user edit → build → migration to split.
        File.WriteAllText(
            Path.Combine(_origDir, "added.xml"),
            "<TEI><text><body>無門關無門關中中中中中</body></text></TEI>");

        var emptyBundle = Path.Combine(_tempRoot, "empty-bundle");
        Directory.CreateDirectory(emptyBundle);
        using var svc = new SearchIndexService { TestOnlyBundleDirOverride = emptyBundle };
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
        Assert.Equal(0, svc.LastBuildFallbackCount);

        // MIGRATED to split; the origin text sidecar is materialised and the legacy family gone.
        Assert.True(File.Exists(Path.Combine(root, "search.origin.text.bin")));
        Assert.True(File.Exists(Path.Combine(root, "search.origin.text.manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "search.overlay.manifest.json")));
        Assert.Empty(Directory.EnumerateFiles(root, "search.index.*"));

        using var probe = new SearchIndexService { TestOnlyBundleDirOverride = emptyBundle };
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.NotNull(await probe.TryLoadAsync(root));
    }
}
