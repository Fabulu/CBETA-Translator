using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Bundle adopt/seed decision + copy logic (SearchIndexService.EvaluateBundleAdoption /
/// CopyBundleFamilyIntoRoot). Uses real temp dirs: a prebuilt "bundle" is produced once by a
/// real full-rebuild, then adopted/seeded into fresh index roots to prove the decision guards
/// and the RootPath re-home that lets the copied family load at its new location.
/// The exhaustive PR-S1 decision-table permutations live in SearchBundleAdoptionTests.
/// </summary>
public class BundleSeedTests : IAsyncLifetime, IDisposable
{
    private readonly string _tempRoot;
    private readonly string _bundleDir;   // stands in for the CI build output (Assets/PrebuiltIndex)
    private readonly string _origDir;
    private readonly string _tranDir;

    private static readonly string SampleXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>禪宗祖師傳法心印無門關公案</p>\n" +
        "</body></text></TEI>\n";

    public BundleSeedTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-seed-" + Guid.NewGuid().ToString("N")[..8]);
        _bundleDir = Path.Combine(_tempRoot, "bundle");
        _origDir = Path.Combine(_tempRoot, "xml-p5");
        _tranDir = Path.Combine(_tempRoot, "xml-p5t");
        Directory.CreateDirectory(_bundleDir);
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    // Build the prebuilt bundle once (a real full rebuild against the sample corpus).
    public async Task InitializeAsync()
    {
        File.WriteAllText(Path.Combine(_origDir, "sample.xml"), SampleXml);
        File.WriteAllText(Path.Combine(_tranDir, "sample.xml"), SampleXml);

        var svc = new SearchIndexService();
        await svc.BuildAsync(_bundleDir, _origDir, new[] { _tranDir });

        // Sanity: the bundle carries the core family and stamps the current BuildGuid.
        Assert.True(File.Exists(Path.Combine(_bundleDir, "search.index.bin")));
        Assert.True(File.Exists(Path.Combine(_bundleDir, "search.index.manifest.json")));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private string NewIndexRoot()
    {
        var p = Path.Combine(_tempRoot, "root-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(p);
        return p;
    }

    // ===== Decision guards =====

    [Fact]
    public void Evaluate_BundlePresent_NoLocalIndex_SeedsVirgin()
    {
        var root = NewIndexRoot();
        // Virgin root, pre-hash probe (null live hash) → seed the bundle unconditionally.
        Assert.Equal(SearchIndexService.BundleAdoptionDecision.SeedVirgin,
            SearchIndexService.EvaluateBundleAdoption(root, _bundleDir, null));
    }

    [Fact]
    public void Evaluate_LocalPresent_BundleDiffersLive_KeepsLocal()
    {
        var root = NewIndexRoot();
        // A local index bin exists and the live hash does NOT match the bundle → keep local
        // (adopting would lose the user's local/additional-corpus entries the bundle lacks).
        File.WriteAllText(Path.Combine(root, "search.index.bin"), "existing");
        Assert.Equal(SearchIndexService.BundleAdoptionDecision.KeepLocal,
            SearchIndexService.EvaluateBundleAdoption(root, _bundleDir, "live-hash-that-differs"));
    }

    [Fact]
    public void Evaluate_NoBundle_ReturnsNoBundle()
    {
        var root = NewIndexRoot();
        var emptyBundle = Path.Combine(_tempRoot, "empty-bundle");
        Directory.CreateDirectory(emptyBundle);
        Assert.Equal(SearchIndexService.BundleAdoptionDecision.NoBundle,
            SearchIndexService.EvaluateBundleAdoption(root, emptyBundle, null));
    }

    [Fact]
    public void Evaluate_StaleBuildGuidBundle_ReturnsNoBundle()
    {
        var root = NewIndexRoot();

        // Copy the bundle, then poison the manifest's BuildGuid to simulate a bundle cut
        // for an older index format. It must be ignored (→ normal full build instead).
        var staleBundle = Path.Combine(_tempRoot, "stale-bundle");
        Directory.CreateDirectory(staleBundle);
        foreach (var f in Directory.EnumerateFiles(_bundleDir))
            File.Copy(f, Path.Combine(staleBundle, Path.GetFileName(f)), true);

        var manifestPath = Path.Combine(staleBundle, "search.index.manifest.json");
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(manifestPath))!;
        node["BuildGuid"] = "search-vSTALE-not-current";
        File.WriteAllText(manifestPath, node.ToJsonString());

        Assert.Equal(SearchIndexService.BundleAdoptionDecision.NoBundle,
            SearchIndexService.EvaluateBundleAdoption(root, staleBundle, null));
    }

    [Fact]
    public void CurrentBuildGuid_MatchesBundleManifest()
    {
        var manifestPath = Path.Combine(_bundleDir, "search.index.manifest.json");
        var man = JsonSerializer.Deserialize<SearchIndexManifest>(File.ReadAllText(manifestPath))!;
        Assert.Equal(SearchIndexService.CurrentSearchBuildGuid, man.BuildGuid);
    }

    // ===== Copy family + RootPath re-home =====

    [Fact]
    public void CopyBundleFamilyIntoRoot_CopiesFamily_AndRehomesRootPath()
    {
        var root = NewIndexRoot();

        var seeded = SearchIndexService.CopyBundleFamilyIntoRoot(root, _bundleDir);
        Assert.True(seeded);

        // Every search.* file in the bundle landed in the index root.
        var bundleFiles = Directory.EnumerateFiles(_bundleDir, "search.*")
            .Select(Path.GetFileName).OrderBy(x => x).ToArray();
        var rootFiles = Directory.EnumerateFiles(root, "search.*")
            .Select(Path.GetFileName).OrderBy(x => x).ToArray();
        Assert.Equal(bundleFiles, rootFiles);

        // The path-bound manifest's RootPath now points at the new index root, not the
        // bundle build dir — otherwise the loader would reject it.
        var man = JsonSerializer.Deserialize<SearchIndexManifest>(
            File.ReadAllText(Path.Combine(root, "search.index.manifest.json")))!;
        Assert.Equal(Path.GetFullPath(root), Path.GetFullPath(man.RootPath));
        Assert.NotEqual(Path.GetFullPath(_bundleDir), Path.GetFullPath(man.RootPath));
    }

    [Fact]
    public async Task SeededIndex_LoadsAndIsNotStaleForSameCorpus()
    {
        // FL6: the served shape is the SPLIT family (a migrated/seeded install). Build it, then
        // confirm a fresh service loads the merged view and reports NOT stale for the same corpus.
        var root = NewIndexRoot();
        using (var build = new SearchIndexService())
        {
            await build.BuildOriginLayerAsync(root, _origDir);
            await build.BuildOverlayLayerAsync(root, new[] { _tranDir });
        }

        var svc = new SearchIndexService();
        var loaded = await svc.TryLoadAsync(root);
        Assert.NotNull(loaded);

        var stale = await svc.IsStaleAsync(root, _origDir, new[] { _tranDir });
        Assert.False(stale);
    }

    [Fact]
    public async Task SeededIndex_IsStaleAfterCorpusDrift_ThenIncrementalCatchUp()
    {
        var root = NewIndexRoot();
        Assert.True(SearchIndexService.CopyBundleFamilyIntoRoot(root, _bundleDir));

        // Drift the corpus past the bundle: add a new file.
        File.WriteAllText(Path.Combine(_origDir, "added.xml"), SampleXml);

        var svc = new SearchIndexService();
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        // The catch-up build reconciles; afterwards the index is fresh again.
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    // ===== Headless CLI (--build-search-index) =====

    [Fact]
    public void RunHeadlessBuild_ProducesIndex_AndReturnsZero()
    {
        var outDir = Path.Combine(_tempRoot, "cli-out");
        var args = new[]
        {
            "--build-search-index",
            "--source-dir", _origDir,
            "--trans-dir", _tranDir,
            "--out-dir", outDir,
        };

        var log = new StringWriter();
        var code = SearchIndexService.RunHeadlessBuild(args, log);

        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(outDir, "search.index.bin")));
        Assert.True(File.Exists(Path.Combine(outDir, "search.index.manifest.json")));
    }

    [Fact]
    public void RunHeadlessBuild_MissingArgs_ReturnsOne()
    {
        var code = SearchIndexService.RunHeadlessBuild(
            new[] { "--build-search-index", "--source-dir", _origDir }, new StringWriter());
        Assert.Equal(1, code);
    }

    [Fact]
    public void RunHeadlessBuild_NonexistentDirs_ReturnsOne()
    {
        var args = new[]
        {
            "--build-search-index",
            "--source-dir", Path.Combine(_tempRoot, "nope-src"),
            "--trans-dir", Path.Combine(_tempRoot, "nope-trn"),
            "--out-dir", Path.Combine(_tempRoot, "cli-out2"),
        };
        Assert.Equal(1, SearchIndexService.RunHeadlessBuild(args, new StringWriter()));
    }

    [Fact]
    public void Adoption_OverLocalIndex_RefusedWhenBundleDiffersLive()
    {
        // The "never clobber a good local index" guard now lives in the decision layer:
        // CopyBundleFamilyIntoRoot is unconditional, so the safety comes from
        // EvaluateBundleAdoption returning KeepLocal when the bundle does not match live.
        var root = NewIndexRoot();
        File.WriteAllText(Path.Combine(root, "search.index.bin"), "existing");

        var decision = SearchIndexService.EvaluateBundleAdoption(root, _bundleDir, "live-hash-that-differs");
        Assert.Equal(SearchIndexService.BundleAdoptionDecision.KeepLocal, decision);
        // The caller only copies on AdoptOverLocal, so the local bin is untouched.
        Assert.Equal("existing", File.ReadAllText(Path.Combine(root, "search.index.bin")));
    }

    // ===== gramsets sidecar (6th artifact) must load after copy =====

    [Fact]
    public async Task CopyBundleFamilyIntoRoot_RehomesGramSetsSidecar_SoItLoadsAtNewRoot()
    {
        var root = NewIndexRoot();
        Assert.True(SearchIndexService.CopyBundleFamilyIntoRoot(root, _bundleDir));

        // The bundle shipped the 6th artifact and both halves landed in the index root.
        Assert.True(File.Exists(Path.Combine(root, GramSetsStore.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(root, GramSetsStore.BinFileName)));

        // Regression: without the RootPath re-home for the gramsets manifest, TryLoadAsync
        // rejects the sidecar (embedded RootPath == CI build dir) and it is dead weight.
        var loaded = await GramSetsStore.TryLoadAsync(root, System.Threading.CancellationToken.None);
        Assert.NotNull(loaded);
    }

    // ===== S7 fix: seeded manifest's stat cache heals after the first probe =====

    [Fact]
    public async Task SeededManifest_StatCache_HealsToLocalTicks_AfterFirstProbe()
    {
        // FL6: the frozen ORIGIN manifest is what a seeded/adopted install carries with foreign
        // (CI-machine) ticks. Build a split root, poison the ORIGIN manifest's ticks, and confirm
        // the first probe heals them to local, then is stat-only. (The overlay is local-built.)
        var root = NewIndexRoot();
        using (var build = new SearchIndexService())
        {
            await build.BuildOriginLayerAsync(root, _origDir);
            await build.BuildOverlayLayerAsync(root, new[] { _tranDir });
        }

        var manifestPath = Path.Combine(root, "search.origin.manifest.json");
        const long PoisonTicks = 12345L;
        var poisoned = JsonSerializer.Deserialize<SearchIndexManifest>(File.ReadAllText(manifestPath))!;
        foreach (var e in poisoned.Entries) e.LastWriteUtcTicks = PoisonTicks;
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(poisoned));

        var emptyBundle = Path.Combine(_tempRoot, "empty-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(emptyBundle);
        var svc = new SearchIndexService { TestOnlyBundleDirOverride = emptyBundle };

        // First probe: not stale, but every origin file cache-misses on the poisoned ticks, so the
        // backfill refreshes ticks/length (not just ContentHash) and rewrites the origin manifest.
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        var healed = JsonSerializer.Deserialize<SearchIndexManifest>(File.ReadAllText(manifestPath))!;
        foreach (var e in healed.Entries)
        {
            // Origin entries are all Original-side ⇒ resolve under _origDir.
            var filePath = Path.Combine(_origDir, e.RelPath.Replace('/', Path.DirectorySeparatorChar));
            var actualTicks = File.GetLastWriteTimeUtc(filePath).Ticks;
            Assert.NotEqual(PoisonTicks, e.LastWriteUtcTicks); // poison healed away
            Assert.Equal(actualTicks, e.LastWriteUtcTicks);    // to the real local mtime
        }

        // Second probe: entries now carry local ticks → stat-only hits → no rewrite at all.
        var bytesBefore = File.ReadAllBytes(manifestPath);
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        var bytesAfter = File.ReadAllBytes(manifestPath);
        Assert.Equal(bytesBefore, bytesAfter);
    }
}
