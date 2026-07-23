using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-S1 — exhaustive adopt-best-bundle decision-table coverage for the SEARCH index
/// (<see cref="SearchIndexService.EvaluateBundleAdoption"/> +
/// <see cref="SearchIndexService.CopyBundleFamilyIntoRoot"/> +
/// <see cref="SearchIndexService.IsStaleAsync"/>). The pure decision layer and the copy
/// mechanics are already exercised by <c>BundleSeedTests</c>; this file drives the WHOLE
/// launch-probe flow through <see cref="SearchIndexService.IsStaleAsync"/> with a staged
/// bundle (via <see cref="SearchIndexService.TestOnlyBundleDirOverride"/>) to prove every
/// row of the §2.1 decision table, the §2.1a ScopeComplete data-loss guard, the §2.2a
/// family-guid gate, the §2.2 leftover-delete (bins AND manifests), the seed-before-hashing
/// ordering invariant, and the stale-detection truth table.
///
/// All fixtures are tiny synthetic corpora in real temp dirs (existing project pattern).
/// Every probe sets <see cref="SearchIndexService.TestOnlyBundleDirOverride"/> explicitly —
/// including to an empty dir for the "no bundle" cases — so no test ever depends on the
/// exe-adjacent Assets/PrebuiltIndex and the suite is deterministic. Corpus mutation drives
/// staleness through content (hash), never wall-clock; mtime is manipulated explicitly.
/// </summary>
public sealed class SearchAdoptionTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;   // active corpus A originals (xml-p5)
    private readonly string _tranDir;   // active corpus A translations (xml-p5t)

    // Family filenames the trim / leftover-delete assertions name explicitly.
    private const string IndexBin = "search.index.bin";
    private const string IndexManifest = "search.index.manifest.json";
    private const string InvertedBin = "search.inverted.bin";
    private const string CorpusFreqBin = "search.corpusfreq.bin";
    private const string CorpusFreqManifest = "search.corpusfreq.manifest.json";
    private const string TextBin = "search.text.bin";
    private const string TextManifest = "search.text.manifest.json";
    private const string GramBin = "search.gramsets.bin";
    private const string GramManifest = "search.gramsets.manifest.json";

    private static string Xml(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        $"<p>{body}</p>\n</body></text></TEI>\n";

    // Distinct CJK bodies so bigram sets differ across corpus states / corpora.
    private const string BodyA = "禪宗祖師傳法心印無門關公案趙州狗子";
    private const string BodyA2 = "洞山五位偏正回互寶鏡三昧歌訣";      // corpus-A "v2" delta file
    private const string BodyB = "臨濟義玄黃檗希運栽松道者一喝";      // additional corpus B (unique)
    private const string BodyC = "雲門文偃日日是好日胡餅一字關";      // unrelated corpus for a live-mismatching bundle

    public SearchAdoptionTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-adopt-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_temp, "xml-p5");
        _tranDir = Path.Combine(_temp, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch { }
    }

    // ---------- helpers ----------

    private string NewDir(string label)
    {
        var p = Path.Combine(_temp, label + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(p);
        return p;
    }

    private static void Write(string dir, string name, string body) =>
        File.WriteAllText(Path.Combine(dir, name), Xml(body));

    /// <summary>Writes the base single-file active corpus A (orig + tran).</summary>
    private void WriteBaseCorpus()
    {
        Write(_origDir, "a.xml", BodyA);
        Write(_tranDir, "a.xml", BodyA);
    }

    /// <summary>A real full rebuild of <paramref name="src"/>/<paramref name="tran"/> into
    /// <paramref name="outDir"/> — the shape CI produces for a bundle and the app produces for a
    /// local index. Disposes the builder; the artifacts on disk persist.</summary>
    private static async Task BuildFamily(string outDir, string src, string tran)
    {
        using var svc = new SearchIndexService();
        await svc.BuildAsync(outDir, src, new[] { tran });
    }

    /// <summary>An empty directory that <see cref="SearchIndexService.ResolveBundleDir"/> can
    /// return so a probe deterministically sees "no bundle" (never the exe-adjacent one).</summary>
    private string EmptyBundle() => NewDir("empty-bundle");

    private static string? InputHashOf(string root)
    {
        var f = Path.Combine(root, IndexManifest);
        if (!File.Exists(f)) return null;
        return JsonNode.Parse(File.ReadAllText(f))?["InputHash"]?.GetValue<string>();
    }

    private static string RootPathOf(string manifestFile) =>
        JsonNode.Parse(File.ReadAllText(manifestFile))!["RootPath"]!.GetValue<string>();

    /// <summary>Rewrites the <c>BuildGuid</c> of any family manifest to <paramref name="guid"/>
    /// (poisons a guid to simulate a format mismatch), preserving every other field.</summary>
    private static void PoisonGuid(string manifestFile, string guid)
    {
        var node = JsonNode.Parse(File.ReadAllText(manifestFile))!;
        node["BuildGuid"] = guid;
        File.WriteAllText(manifestFile, node.ToJsonString());
    }

    private static bool Has(string root, string name) => File.Exists(Path.Combine(root, name));

    private static SearchIndexService Probe(string bundleDir)
        => new SearchIndexService { TestOnlyBundleDirOverride = bundleDir };

    private async Task<List<SearchResultGroup>> SearchOriginal(
        SearchIndexService svc, string root, string query,
        IReadOnlyList<string>? addOrig = null, IReadOnlyList<string>? addTran = null)
    {
        var manifest = await svc.TryLoadAsync(root);
        Assert.NotNull(manifest);
        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            root, _origDir, _tranDir, manifest!, query,
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null), contextWidth: 30,
            additionalOriginalDirs: addOrig, additionalTranslatedDirs: addTran))
        {
            groups.Add(g);
        }
        return groups;
    }

    // ==================================================================================
    // Decision table (§2.1) — one row per test, single-corpus (ScopeComplete = true)
    // ==================================================================================

    [Fact] // Row 1: FRESH local, any bundle → keep local, no copy, no build.
    public async Task Row1_FreshLocal_KeepsLocal_ZeroBuild_BundleUntouched()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);   // local == live

        // A MATCHING bundle staged; a sentinel non-family file in the root would be removed by
        // the leftover-delete if adoption fired. On a fresh local, adoption must never run.
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        var sentinel = Path.Combine(root, "search.SENTINEL.marker");
        File.WriteAllText(sentinel, "keep-me");
        var hashBefore = InputHashOf(root);

        using var probe = Probe(bundle);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        Assert.Equal(0, probe.LastBuildXmlReadCount);          // no build ran
        Assert.True(File.Exists(sentinel));                    // leftover-delete never ran
        Assert.Equal(hashBefore, InputHashOf(root));           // manifest untouched (not adopted)
    }

    [Fact] // Row 2: STALE local, bundle MATCHES live + ScopeComplete → ADOPT over local, zero build.
    public async Task Row2_StaleLocal_MatchingBundle_AdoptsOverLocal_ZeroBuild()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);           // local == corpus v1
        var localHash = InputHashOf(root);

        // Advance the corpus to v2 (add a file) and cut a bundle from v2 → bundle == live.
        Write(_origDir, "b.xml", BodyA2);
        Write(_tranDir, "b.xml", BodyA2);
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        var bundleHash = InputHashOf(bundle);
        Assert.NotEqual(localHash, bundleHash);                // sanity: local really is stale

        using var probe = Probe(bundle);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir })); // adopt → zero build
        Assert.Equal(0, probe.LastBuildXmlReadCount);

        // The bundle's manifest (v2 InputHash) now owns the root — adoption over local happened.
        Assert.Equal(bundleHash, InputHashOf(root));
        // A second probe confirms the adopted root is genuinely fresh (idempotent, still zero build).
        using var probe2 = Probe(bundle);
        Assert.False(await probe2.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    [Fact] // Row 3: STALE local, bundle ≠ live → keep local (never lose local entries), verdict stale.
    public async Task Row3_StaleLocal_BundleDiffersLive_KeepsLocal_Stale()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);           // local == v1
        var localHash = InputHashOf(root);

        // Corpus advances to live v2, but the staged bundle is cut from an UNRELATED corpus C,
        // so bundleHash ≠ live: adopting it would lose the user's real state.
        Write(_origDir, "b.xml", BodyA2);
        Write(_tranDir, "b.xml", BodyA2);
        var cOrig = NewDir("cOrig"); var cTran = NewDir("cTran");
        Write(cOrig, "c.xml", BodyC); Write(cTran, "c.xml", BodyC);
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, cOrig, cTran);
        Assert.NotEqual(InputHashOf(bundle), InputHashOf(root)); // bundle ≠ live-ish (different corpus)

        using var probe = Probe(bundle);
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));  // keep local → stale

        // Local manifest preserved exactly — NOT clobbered by the mismatching bundle.
        Assert.Equal(localHash, InputHashOf(root));
    }

    [Fact] // Row 4: ABSENT local, bundle MATCHES live + ScopeComplete → SEED (adopt), zero build.
    public async Task Row4_VirginLocal_MatchingBundle_Seeds_ZeroBuild()
    {
        WriteBaseCorpus();
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);         // bundle == live
        var bundleHash = InputHashOf(bundle);

        var root = NewDir("root");                             // virgin — no local index
        using var probe = Probe(bundle);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir })); // seed → zero build
        Assert.Equal(0, probe.LastBuildXmlReadCount);

        Assert.True(Has(root, IndexBin));                      // seeded family landed
        Assert.Equal(bundleHash, InputHashOf(root));
        // The seeded, re-homed family is genuinely loadable at the new root.
        using var loader = new SearchIndexService();
        Assert.NotNull(await loader.TryLoadAsync(root));
    }

    [Fact] // Row 5: ABSENT local, current-guid bundle but ≠ live → SEED, but verdict STALE (catch-up owed).
    public async Task Row5_VirginLocal_BundleDiffersLive_Seeds_ButStale()
    {
        WriteBaseCorpus();
        // Bundle cut from v1; live corpus then advances to v2 → seeded family is behind live.
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        Write(_origDir, "b.xml", BodyA2);
        Write(_tranDir, "b.xml", BodyA2);

        var root = NewDir("root");
        using var probe = Probe(bundle);
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));  // seeded, but stale

        // The seed happened BEFORE the hash produced the stale verdict — bins are on disk even
        // though the verdict is stale (the ordering invariant, §2.2). Search is instant meanwhile;
        // the catch-up then reconciles to fresh.
        Assert.True(Has(root, IndexBin));
        using var catchUp = new SearchIndexService();
        await catchUp.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
        using var after = Probe(bundle);
        Assert.False(await after.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    [Fact] // Row 6: ABSENT local, no usable bundle → cold full build (stale), nothing seeded.
    public async Task Row6_VirginLocal_NoBundle_ColdBuild()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        using var probe = Probe(EmptyBundle());
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.False(Has(root, IndexBin));                     // nothing seeded — a real build must run
    }

    // ==================================================================================
    // Leftover-delete (§2.2) + re-home (three path-bound manifests)
    // ==================================================================================

    [Fact] // Adopting a TRIMMED bundle over a full local family deletes the orphaned
           // text + gramsets — bins AND manifests — so the root is canonical, not merely safe.
    public async Task AdoptOverStale_ReplacesWholeFamily_AndDeletesLeftovers()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);           // full local family (has text + gramsets)
        Assert.True(Has(root, TextBin) && Has(root, GramBin)); // precondition: leftovers exist

        // Advance corpus to v2 and cut a bundle from v2, then TRIM it (the shipped shape:
        // bloom + inverted + corpusfreq only — no text sidecar, no gramsets).
        Write(_origDir, "b.xml", BodyA2);
        Write(_tranDir, "b.xml", BodyA2);
        var full = NewDir("full-bundle");
        await BuildFamily(full, _origDir, _tranDir);
        var trimmed = NewDir("trimmed-bundle");
        foreach (var f in Directory.EnumerateFiles(full, "search.*"))
        {
            var name = Path.GetFileName(f);
            if (name.StartsWith("search.text.", StringComparison.Ordinal) ||
                name.StartsWith("search.gramsets.", StringComparison.Ordinal)) continue;
            File.Copy(f, Path.Combine(trimmed, name));
        }
        var bundleHash = InputHashOf(trimmed);

        using var probe = Probe(trimmed);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir })); // adopt-over-local

        // The whole trimmed family is present…
        Assert.True(Has(root, IndexBin) && Has(root, InvertedBin) && Has(root, CorpusFreqBin));
        Assert.Equal(bundleHash, InputHashOf(root));
        // …and every orphaned sidecar file the bundle did NOT bring is gone — MANIFESTS included.
        Assert.False(Has(root, TextBin), "stale text.bin must be deleted");
        Assert.False(Has(root, TextManifest), "stale text.manifest.json must be deleted");
        Assert.False(Has(root, GramBin), "stale gramsets.bin must be deleted");
        Assert.False(Has(root, GramManifest), "stale gramsets.manifest.json must be deleted");
    }

    [Fact] // The copy re-homes the RootPath of ALL THREE path-bound manifests (bloom, text,
           // gramsets) to the new root so each loader accepts them at the adopted location.
    public async Task RehomeAppliedToAllThreeManifests()
    {
        WriteBaseCorpus();
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        var root = NewDir("root");

        Assert.True(SearchIndexService.CopyBundleFamilyIntoRoot(root, bundle));

        var full = Path.GetFullPath(root);
        Assert.Equal(full, Path.GetFullPath(RootPathOf(Path.Combine(root, IndexManifest))));
        Assert.Equal(full, Path.GetFullPath(RootPathOf(Path.Combine(root, TextManifest))));
        Assert.Equal(full, Path.GetFullPath(RootPathOf(Path.Combine(root, GramManifest))));
        // None still points at the bundle build dir.
        Assert.NotEqual(Path.GetFullPath(bundle), Path.GetFullPath(RootPathOf(Path.Combine(root, IndexManifest))));
    }

    // ==================================================================================
    // False-fresh / adopt-never-fires guards
    // ==================================================================================

    [Fact] // A live-mismatching bundle must never overwrite a FRESH local index (false-positive guard).
    public async Task AdoptNeverFiresOnFreshLocal()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);           // local == live (fresh)
        var localHash = InputHashOf(root);

        // A bundle from an unrelated corpus (≠ live). Even so, a fresh local short-circuits
        // BEFORE the adoption branch — the bundle is never consulted.
        var cOrig = NewDir("cOrig"); var cTran = NewDir("cTran");
        Write(cOrig, "c.xml", BodyC); Write(cTran, "c.xml", BodyC);
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, cOrig, cTran);

        using var probe = Probe(bundle);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.Equal(localHash, InputHashOf(root));            // untouched
    }

    // ==================================================================================
    // Family-guid gate (§2.2a)
    // ==================================================================================

    [Fact] // A bundle whose bloom BuildGuid is not current is ignored (older index format).
    public async Task GuidMismatchBundleIgnored()
    {
        WriteBaseCorpus();
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        PoisonGuid(Path.Combine(bundle, IndexManifest), "search-vSTALE-not-current");

        var root = NewDir("root");                             // virgin
        using var probe = Probe(bundle);
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.False(Has(root, IndexBin));                     // not seeded from a stale-format bundle
    }

    [Fact] // A corrupt bundle manifest is ignored (never a partial seed); a clean cold build is owed.
    public async Task CorruptBundleManifest_Ignored_ColdBuild_NoPartialSeed()
    {
        WriteBaseCorpus();
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        File.WriteAllText(Path.Combine(bundle, IndexManifest), "{ this is not valid json");

        var root = NewDir("root");
        using var probe = Probe(bundle);
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        // No family files were copied into the root from the unusable bundle.
        Assert.Empty(Directory.EnumerateFiles(root, "search.*"));
    }

    [Fact] // A current bloom but wrong-guid CORPUSFREQ sibling is a FAMILY mismatch → Branch B
           // (reseed/rebuild), never served fresh-with-degraded-ranking (§2.2a).
    public async Task CorpusFreqGuidMismatch_TreatedAsFamilyMismatch()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);
        var empty = EmptyBundle();

        // Baseline: the intact family on an unchanged corpus is NOT stale.
        using (var baseline = Probe(empty))
            Assert.False(await baseline.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        // Poison ONLY the corpusfreq guid — bloom stays current, corpus unchanged. A bloom-only
        // freshness check would still say "fresh"; the family gate must instead force stale.
        PoisonGuid(Path.Combine(root, CorpusFreqManifest), "corpusfreq-vSTALE");
        using var probe = Probe(empty);
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    [Fact] // The optional TEXT sidecar's guid is NOT part of the family gate — a mismatch is
           // ignored (equivalent to an absent sidecar), never fatal (§2.2a / §5).
    public async Task TextGuidMismatch_IsIgnoredNotFatal()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);

        PoisonGuid(Path.Combine(root, TextManifest), "text-vSTALE");
        using var probe = Probe(EmptyBundle());
        // Corpus unchanged, bloom + corpusfreq current → still fresh despite the stale sidecar guid.
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    // ==================================================================================
    // Seed-before-hashing ordering invariant (§2.2)
    // ==================================================================================

    [Fact] // Fresh install: the bundle family is copied in and loadable with ZERO corpus-build
           // work — seeding does not wait on (nor is gated by) the live InputHash pass.
    public async Task FreshInstall_SeedPrecedesHashing()
    {
        WriteBaseCorpus();
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);         // matching bundle

        var root = NewDir("root");                             // virgin
        using var probe = Probe(bundle);
        var verdict = await probe.IsStaleAsync(root, _origDir, new[] { _tranDir });

        // Seeded (query path can load immediately) and no build/extraction ran during the probe.
        Assert.True(Has(root, IndexBin));
        using var loader = new SearchIndexService();
        Assert.NotNull(await loader.TryLoadAsync(root));
        Assert.Equal(0, probe.LastBuildXmlReadCount);
        Assert.Equal(0, probe.LastBuildFallbackCount);
        Assert.False(verdict);                                 // matching → not stale (row 4)
    }

    // ==================================================================================
    // Multi-corpus (§2.1a ScopeComplete) — never a zero-build verdict; never clobber corpus B
    // ==================================================================================

    [Fact] // Additional corpora present + a stamp-matching (active-corpus) bundle over a LOADABLE
           // local index ⇒ adoption REFUSED (would drop corpus B's postings) → keep local, stale.
    public async Task MultiCorpus_AdoptOverLocal_Refused()
    {
        WriteBaseCorpus();
        var bOrig = NewDir("bOrig"); var bTran = NewDir("bTran");
        Write(bOrig, "b.xml", BodyB); Write(bTran, "b.xml", BodyB);

        // Local index covers A + B.
        var root = NewDir("root");
        using (var build = new SearchIndexService())
            await build.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: true,
                additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran });

        // Corpus A drifts (local now stale for the active corpus). Cut a bundle from A′
        // (active-corpus only) so its stamp WOULD equal live — the trap the gate must resist.
        Write(_origDir, "a2.xml", BodyA2);
        Write(_tranDir, "a2.xml", BodyA2);
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);

        using var probe = Probe(bundle);
        var stale = await probe.IsStaleAsync(root, _origDir, new[] { _tranDir },
            additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran });
        Assert.True(stale);                                    // never a zero-build verdict here

        // The decisive data-loss guard: corpus B is STILL searchable — the local index was kept,
        // not clobbered by the B-less bundle.
        using var query = new SearchIndexService();
        var hits = await SearchOriginal(query, root, "臨濟義玄",
            addOrig: new[] { bOrig }, addTran: new[] { bTran });
        Assert.NotEmpty(hits);
    }

    [Fact] // Fresh install with additional corpora + matching (active) bundle ⇒ SEED happens
           // (active corpus instant) but the verdict is STALE so the catch-up walks the
           // additional dirs — corpus B is never silently unsearchable.
    public async Task MultiCorpus_FreshInstall_SeedsButReturnsStale()
    {
        WriteBaseCorpus();
        var bOrig = NewDir("bOrig"); var bTran = NewDir("bTran");
        Write(bOrig, "b.xml", BodyB); Write(bTran, "b.xml", BodyB);

        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);         // active-corpus bundle only (no B)

        var root = NewDir("root");                             // virgin
        using var probe = Probe(bundle);
        var stale = await probe.IsStaleAsync(root, _origDir, new[] { _tranDir },
            additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran });
        Assert.True(stale);                                    // seeded but NOT zero-build
        Assert.True(Has(root, IndexBin));                      // active corpus searchable from the seed

        // Before the catch-up the seeded (A-only) index cannot find corpus B — proving that a
        // wrongly-fresh verdict would have left B permanently unsearchable.
        using (var pre = new SearchIndexService())
            Assert.Empty(await SearchOriginal(pre, root, "臨濟義玄",
                addOrig: new[] { bOrig }, addTran: new[] { bTran }));

        // The catch-up build DOES walk the additional dirs → corpus B becomes searchable.
        using (var catchUp = new SearchIndexService())
            await catchUp.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false,
                additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran });

        using var after = new SearchIndexService();
        Assert.NotEmpty(await SearchOriginal(after, root, "臨濟義玄",
            addOrig: new[] { bOrig }, addTran: new[] { bTran }));
    }

    [Fact] // Control: the SAME matching bundle + virgin root with NO additional dirs IS a
           // zero-build seed (row 4) — proving the stale verdict above is caused by the
           // additional dirs, not the bundle.
    public async Task SingleCorpus_ZeroBuild_Unaffected()
    {
        WriteBaseCorpus();
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);

        var root = NewDir("root");
        using var probe = Probe(bundle);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir })); // no additional dirs
        Assert.True(Has(root, IndexBin));
    }

    // ==================================================================================
    // Update simulation + stale-detection truth table (hash path, mtime-immune)
    // ==================================================================================

    [Fact] // The headline update case: a content EDIT (not just an added file) advances the
           // corpus to the bundle's state ⇒ the probe adopts, IsStaleAsync false, zero build.
    public async Task UpdateSimulation_CorpusAdvancesToBundle_AdoptsZeroBuild()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);           // local == v1

        // Edit an existing file's content (a genuine translation change) → v2, then bundle v2.
        Write(_origDir, "a.xml", BodyA + BodyA2);
        var bundle = NewDir("bundle");
        await BuildFamily(bundle, _origDir, _tranDir);
        var bundleHash = InputHashOf(bundle);
        Assert.NotEqual(InputHashOf(root), bundleHash);

        using var probe = Probe(bundle);
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.Equal(0, probe.LastBuildXmlReadCount);
        Assert.Equal(bundleHash, InputHashOf(root));           // adopted
    }

    [Fact] // Content changed but mtime forced back to the original ⇒ STALE (freshness is content-
           // hash driven, not mtime driven — the whole point of the InputHash gate).
    public async Task StaleTruthTable_ContentChangeNoMtime_IsStale()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);

        var file = Path.Combine(_origDir, "a.xml");
        var origTicks = File.GetLastWriteTimeUtc(file);
        File.WriteAllText(file, Xml(BodyA + BodyA2));           // different content AND length
        File.SetLastWriteTimeUtc(file, origTicks);             // but restore the original mtime

        using var probe = Probe(EmptyBundle());
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    [Fact] // mtime bumped but content identical ⇒ NOT stale (git pull / checkout mtime churn is
           // ignored). The hash is recomputed on the cache miss and matches.
    public async Task StaleTruthTable_MtimeChangeNoContent_NotStale()
    {
        WriteBaseCorpus();
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);

        var file = Path.Combine(_origDir, "a.xml");
        File.SetLastWriteTimeUtc(file, File.GetLastWriteTimeUtc(file).AddDays(3)); // mtime only

        using var probe = Probe(EmptyBundle());
        Assert.False(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    // ==================================================================================
    // Incremental catch-up is delta-only (row 3/5 "delta-only (search)")
    // ==================================================================================

    [Fact] // Keep-local (no usable bundle) then incremental catch-up reads ONLY the changed
           // delta — not the whole corpus — and reconciles to fresh.
    public async Task StaleLocal_NoBundle_IncrementalCatchUp_ReadsOnlyDelta()
    {
        // A corpus big enough that a 1-file delta stays under the 20% full-rebuild threshold.
        for (int i = 0; i < 12; i++)
        {
            Write(_origDir, $"f{i:D2}.xml", BodyA + i);
            Write(_tranDir, $"f{i:D2}.xml", BodyA + i);
        }
        var root = NewDir("root");
        await BuildFamily(root, _origDir, _tranDir);

        // Add exactly one file (1/24 files ≈ 4% delta).
        Write(_origDir, "new.xml", BodyA2);
        Write(_tranDir, "new.xml", BodyA2);

        using var probe = Probe(EmptyBundle());
        Assert.True(await probe.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        using var build = new SearchIndexService();
        await build.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
        Assert.Equal(0, build.LastBuildDeltaGuardTripped);     // stayed on the incremental path
        Assert.Equal(0, build.LastBuildFallbackCount);         // no fault-retry
        Assert.Equal(2, build.LastBuildXmlReadCount);          // only the 2 added (orig+tran) files read

        using var after = Probe(EmptyBundle());
        Assert.False(await after.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }
}
