using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL6 (design §6): combined→split MIGRATION. A pre-existing legacy combined root
/// (search.index.* family) is converted to the two-layer split — Path A (adopt the shipped
/// origin bundle by file copy + build the overlay fresh) or Path B (carve from the legacy
/// artifacts only, no XML for stat-unchanged entries) — WITHOUT a mass rebuild. The keystone
/// assertion: the migrated (adopted/carved) split serves byte-identical search results to the
/// pre-migration combined index (reusing the SplitParity comparison), plus the legacy family is
/// deleted on success and a torn carve rolls back to a still-servable legacy family.
/// </summary>
public sealed class SplitMigrationTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;
    private readonly string _tranDir;

    public SplitMigrationTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-splitmig-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_temp);
        _origDir = Path.Combine(_temp, "xml-p5");
        _tranDir = Path.Combine(_temp, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch { }
    }

    // ── corpus primitives ──────────────────────────────────────────────────────────────
    private static string Xml(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        $"<p>{body}</p>\n</body></text></TEI>\n";

    private static void Write(string dir, string name, string body) =>
        File.WriteAllText(Path.Combine(dir, name), Xml(body));

    private string NewDir(string label)
    {
        var p = Path.Combine(_temp, label + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(p);
        return p;
    }

    private string EmptyBundle() => NewDir("empty-bundle");

    private static SearchIndexService Probe(string bundleDir)
        => new SearchIndexService { TestOnlyBundleDirOverride = bundleDir };

    private static bool Has(string root, string name) => File.Exists(Path.Combine(root, name));

    private static string? StampField(string manifestFile, string field)
    {
        if (!File.Exists(manifestFile)) return null;
        return JsonNode.Parse(File.ReadAllText(manifestFile))?[field]?.GetValue<string>();
    }

    private static SearchIndexManifest ReadManifest(string root, string fileName) =>
        JsonSerializer.Deserialize<SearchIndexManifest>(File.ReadAllText(Path.Combine(root, fileName)))!;

    /// <summary>A CJK-rich single-corpus test corpus: three origin files (one untranslated),
    /// two translations, and a translation-only rel — enough to hit every partition case the
    /// migration carve must reproduce.</summary>
    private void WriteCorpus()
    {
        Write(_origDir, "a.xml", "無門關第一則趙州狗子佛性");
        Write(_origDir, "b.xml", "臨濟錄示眾四料簡奪人");
        Write(_origDir, "c.xml", "碧巖錄第二則趙州至道");   // no translation
        Write(_tranDir, "a.xml", "The Gateless Gate case one Zhaozhou dog Buddha nature 佛性");
        Write(_tranDir, "b.xml", "Record of Linji four propositions 四料簡");
        Write(_tranDir, "z-only.xml", "Translation-only commentary 趙州 dog koan");
    }

    private static async Task BuildCombined(string root, string origDir, string tranDir)
    {
        using var svc = new SearchIndexService();
        await svc.BuildAsync(root, origDir, new[] { tranDir });   // forceRebuild ⇒ combined family
    }

    private static async Task BuildOriginBundle(string bundleDir, string origDir)
    {
        using var svc = new SearchIndexService();
        await svc.BuildOriginLayerAsync(bundleDir, origDir);
    }

    // ── search-result parity (reused from SplitParityTests' comparison contract) ────────
    private sealed class SearchOutcome
    {
        public Dictionary<string, (int hitsO, int hitsT, bool skipVerify, List<string> snippets)> Groups =
            new(StringComparer.OrdinalIgnoreCase);
        public int SkippedVerifyGroups;
        public int VerifiedGroups;
    }

    private async Task<SearchOutcome> RunSearchAsync(string root, string query, bool includeO, bool includeT)
    {
        // Dispose after collecting so the bloom/text mmap handles are released — otherwise a
        // later migration cannot delete the (still-mapped) legacy family on Windows.
        using var svc = new SearchIndexService();
        var manifest = await svc.TryLoadAsync(root);
        Assert.NotNull(manifest);

        var outcome = new SearchOutcome();
        await foreach (var g in svc.SearchAllAsync(
            root, _origDir, _tranDir, manifest!, query,
            includeOriginal: includeO, includeTranslated: includeT,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 20))
        {
            bool skip = g.Children.Count > 0 && g.Children[0].IsSkippedVerify;
            var snippets = g.Children
                .Select(c => $"{c.Side}|{c.Hit.Left}|{c.Hit.Match}|{c.Hit.Right}")
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            outcome.Groups[g.RelPath] = (g.HitsOriginal, g.HitsTranslated, skip, snippets);
        }
        outcome.SkippedVerifyGroups = svc.LastSearchSkippedVerifyGroups;
        outcome.VerifiedGroups = svc.LastSearchVerifiedGroups;
        return outcome;
    }

    private static void AssertOutcomesIdentical(string label, SearchOutcome combined, SearchOutcome split)
    {
        Assert.True(
            combined.Groups.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(split.Groups.Keys),
            $"[{label}] group rel set differs. combined=[{string.Join(",", combined.Groups.Keys.OrderBy(k => k))}] " +
            $"split=[{string.Join(",", split.Groups.Keys.OrderBy(k => k))}]");

        foreach (var rel in combined.Groups.Keys)
        {
            var c = combined.Groups[rel];
            var s = split.Groups[rel];
            Assert.True(c.hitsO == s.hitsO, $"[{label}] {rel} HitsOriginal {c.hitsO} != {s.hitsO}");
            Assert.True(c.hitsT == s.hitsT, $"[{label}] {rel} HitsTranslated {c.hitsT} != {s.hitsT}");
            Assert.True(c.skipVerify == s.skipVerify, $"[{label}] {rel} skipVerify {c.skipVerify} != {s.skipVerify}");
            Assert.True(c.snippets.SequenceEqual(s.snippets, StringComparer.Ordinal),
                $"[{label}] {rel} snippets differ.\n  combined: {string.Join(" ;; ", c.snippets)}\n  split:    {string.Join(" ;; ", s.snippets)}");
        }
        Assert.True(combined.SkippedVerifyGroups == split.SkippedVerifyGroups,
            $"[{label}] SkippedVerifyGroups {combined.SkippedVerifyGroups} != {split.SkippedVerifyGroups}");
        Assert.True(combined.VerifiedGroups == split.VerifiedGroups,
            $"[{label}] VerifiedGroups {combined.VerifiedGroups} != {split.VerifiedGroups}");
    }

    private async Task AssertServesIdenticalAsync(string root, SearchOutcome[] before)
    {
        var queries = new (string q, bool o, bool t)[]
        {
            ("趙州", true, false),          // CJK bigram in origin (a + c)
            ("佛性", true, true),           // CJK bigram in origin + translation
            ("Zhaozhou", false, true),      // English term in translation
            ("四料簡", true, true),          // multi-char CJK phrase
        };
        for (int i = 0; i < queries.Length; i++)
        {
            var (q, o, t) = queries[i];
            var after = await RunSearchAsync(root, q, o, t);
            Assert.True(before[i].Groups.Count > 0, $"query '{q}' produced no combined groups — corpus/query drifted");
            AssertOutcomesIdentical($"post-migration '{q}'", before[i], after);
        }
    }

    private async Task<SearchOutcome[]> CaptureCombinedAsync(string root)
    {
        var queries = new (string q, bool o, bool t)[]
        {
            ("趙州", true, false), ("佛性", true, true), ("Zhaozhou", false, true), ("四料簡", true, true),
        };
        var outs = new SearchOutcome[queries.Length];
        for (int i = 0; i < queries.Length; i++)
            outs[i] = await RunSearchAsync(root, queries[i].q, queries[i].o, queries[i].t);
        return outs;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // Path A — bundle-first: adopt origin + fresh overlay, zero origin XML reads.
    // ══════════════════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task PathA_BundleMatchesLive_AdoptsOrigin_BuildsOverlay_DeletesLegacy_ServesIdentical()
    {
        WriteCorpus();
        var root = NewDir("root");
        await BuildCombined(root, _origDir, _tranDir);
        Assert.True(Has(root, "search.index.manifest.json"));
        Assert.False(Has(root, "search.origin.manifest.json"));

        // A shipped origin bundle built over the SAME origin corpus ⇒ OriginHash matches live.
        var bundleDir = NewDir("origin-bundle");
        await BuildOriginBundle(bundleDir, _origDir);
        var bundleOriginStamp = StampField(Path.Combine(bundleDir, "search.origin.manifest.json"), "IndexStamp");
        Assert.NotNull(bundleOriginStamp);

        var combinedOutcomes = await CaptureCombinedAsync(root);

        using var svc = Probe(bundleDir);
        // FL8 (eager): IsStaleAsync now reports the legacy combined root migration-owed; the MIGRATION
        // runs here on the build (Path A adopt, since the bundle matches live), converting it to split.
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // Origin ADOPTED (not built): its IndexStamp equals the bundle's, and only the overlay
        // files were read (origin contributed ZERO XML reads to the migration).
        Assert.True(Has(root, "search.origin.manifest.json"));
        Assert.True(Has(root, "search.overlay.manifest.json"));
        Assert.Equal(bundleOriginStamp, StampField(Path.Combine(root, "search.origin.manifest.json"), "IndexStamp"));
        Assert.Equal("origin", ReadManifest(root, "search.origin.manifest.json").LayerRole);
        // Overlay build read exactly the 3 translation files; origin was adopted, never read.
        Assert.Equal(3, svc.LastBuildXmlReadCount);
        Assert.Equal(0, svc.LastBuildFallbackCount);

        // Legacy combined family deleted.
        Assert.False(Has(root, "search.index.manifest.json"));
        Assert.False(Has(root, "search.index.bin"));
        Assert.False(Has(root, "search.text.bin"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.index.*"));

        // After migration the root is fresh (no rebuild loop) and serves identical results.
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        await AssertServesIdenticalAsync(root, combinedOutcomes);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // Path B — carve: read ONLY the legacy artifacts, zero XML for stat-unchanged entries.
    // ══════════════════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task PathB_NoBundle_CarvesFromLegacy_NoXmlReads_DeletesLegacy_ServesIdentical()
    {
        WriteCorpus();
        var root = NewDir("root");
        await BuildCombined(root, _origDir, _tranDir);
        Assert.True(Has(root, "search.index.bin"));
        Assert.True(Has(root, "search.text.bin"));   // local build ⇒ text sidecar present to carry

        var combinedOutcomes = await CaptureCombinedAsync(root);

        using var svc = Probe(EmptyBundle());   // no bundle ⇒ carve on the next build
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // Carve carried every stat-unchanged entry from the legacy bin/text.bin — NO XML parse.
        Assert.Equal(0, svc.LastBuildXmlReadCount);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.Equal(0, svc.LastBuildDeltaGuardTripped);   // delta guard bypassed, not tripped

        // Split family present + correctly roled; legacy deleted.
        Assert.True(Has(root, "search.origin.manifest.json"));
        Assert.True(Has(root, "search.overlay.manifest.json"));
        Assert.Equal("origin", ReadManifest(root, "search.origin.manifest.json").LayerRole);
        Assert.Equal("overlay", ReadManifest(root, "search.overlay.manifest.json").LayerRole);
        Assert.NotNull(ReadManifest(root, "search.origin.manifest.json").OriginHash);
        Assert.Empty(Directory.EnumerateFiles(root, "search.index.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.text.*"));

        // Fresh (no rebuild loop) + byte-identical search results — the keystone.
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        await AssertServesIdenticalAsync(root, combinedOutcomes);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // Torn carve — S5 rollback: partial split removed, legacy kept + still servable.
    // ══════════════════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task TornCarve_RollsBack_KeepsServableLegacy_NoTornSplit()
    {
        WriteCorpus();
        var root = NewDir("root");
        await BuildCombined(root, _origDir, _tranDir);
        var combinedOutcomes = await CaptureCombinedAsync(root);

        using var svc = Probe(EmptyBundle());
        // Fault on the SECOND carve invocation (the overlay layer) — origin carve commits, overlay
        // carve tears. The migration must roll back both split partials and keep the legacy family.
        int carveInvokes = 0;
        svc.TestOnlyIncrementalFault = () =>
        {
            if (++carveInvokes == 2)
                throw new InvalidOperationException("injected torn carve (overlay)");
        };

        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // No torn/hybrid split root: neither split manifest survives.
        Assert.False(Has(root, "search.origin.manifest.json"));
        Assert.False(Has(root, "search.overlay.manifest.json"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.origin.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.overlay.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.*.tmp"));

        // Legacy family kept and still SERVABLE (identical results to before the torn migration).
        Assert.True(Has(root, "search.index.manifest.json"));
        svc.TestOnlyIncrementalFault = null;   // clear before serving
        await AssertServesIdenticalAsync(root, combinedOutcomes);
    }

    // A trimmed/edited combined root re-migrates cleanly on a later launch (idempotent trigger).
    [Fact]
    public async Task TornCarve_ThenRetry_Succeeds()
    {
        WriteCorpus();
        var root = NewDir("root");
        await BuildCombined(root, _origDir, _tranDir);

        using (var torn = Probe(EmptyBundle()))
        {
            int n = 0;
            torn.TestOnlyIncrementalFault = () => { if (++n == 2) throw new InvalidOperationException("tear"); };
            await torn.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
        }
        Assert.True(Has(root, "search.index.manifest.json"));   // legacy kept after tear

        using var retry = Probe(EmptyBundle());
        await retry.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        Assert.True(Has(root, "search.origin.manifest.json"));
        Assert.True(Has(root, "search.overlay.manifest.json"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.index.*"));
        Assert.False(await retry.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // FL8 EAGER migration: the launch probe (IsStaleAsync) itself reports a legacy combined root
    // migration-owed WITHOUT any translation edit — so the launch auto-index drives the migration
    // on the NEXT launch. (FL6 was LAZY: only a build/edit triggered it.) The migrated split then
    // serves byte-identical results to the pre-migration combined index.
    // ══════════════════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task EagerMigration_LegacyCombined_IsStaleWithoutEdit_ThenMigrates_ServesIdentical()
    {
        WriteCorpus();
        var root = NewDir("root");
        await BuildCombined(root, _origDir, _tranDir);
        Assert.True(Has(root, "search.index.manifest.json"));
        Assert.False(Has(root, "search.origin.manifest.json"));

        var combinedOutcomes = await CaptureCombinedAsync(root);

        // EAGER: a FRESH combined root (no edit, corpus unchanged) is reported migration-owed by the
        // probe itself — this is the signal the launch auto-index uses to drive migration eagerly.
        using var svc = Probe(EmptyBundle());
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        // The launch auto-index then migrates (Path B carve here — no bundle staged).
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        Assert.True(Has(root, "search.origin.manifest.json"));
        Assert.True(Has(root, "search.overlay.manifest.json"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.index.*"));   // legacy migrated away
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        await AssertServesIdenticalAsync(root, combinedOutcomes);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // FL8 (FL6 review MINOR-1): the legacy-family SWEEP. Once a SERVABLE split is on disk, a later
    // BuildSplitAsync best-effort deletes any lingering legacy combined family (the ~1 GB power-loss-
    // between-layers window / a forceRebuild-over-legacy leftover). Gated on a servable split present
    // so the fallback-read is never removed while the split is absent; never throws.
    // ══════════════════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task LegacySweep_RemovesLingeringCombinedFamily_WhenSplitServable()
    {
        WriteCorpus();
        var root = NewDir("root");
        using (var build = new SearchIndexService())
        {
            await build.BuildOriginLayerAsync(root, _origDir);
            await build.BuildOverlayLayerAsync(root, new[] { _tranDir });
        }
        Assert.True(Has(root, "search.origin.manifest.json"));

        // Plant a lingering legacy combined family, as a power-loss-between-layers window would leave.
        foreach (var name in new[]
                 {
                     "search.index.bin", "search.index.manifest.json", "search.text.bin",
                     "search.text.manifest.json", "search.corpusfreq.bin", "search.inverted.bin",
                     "search.gramsets.bin",
                 })
            File.WriteAllText(Path.Combine(root, name), "stale-legacy-leftover");
        Assert.True(Has(root, "search.index.bin"));

        // Edit a translation so the next build rebuilds the overlay → BuildSplitAsync runs → sweeps.
        Write(_tranDir, "a.xml", "edited overlay " + Guid.NewGuid().ToString("N")[..8]);
        using var svc = Probe(EmptyBundle());
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // Lingering legacy family swept; the split remains servable (its own artifacts untouched).
        Assert.Empty(Directory.EnumerateFiles(root, "search.index.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.text.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.corpusfreq.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.inverted.*"));
        Assert.Empty(Directory.EnumerateFiles(root, "search.gramsets.*"));
        Assert.True(Has(root, "search.origin.manifest.json"));
        Assert.True(Has(root, "search.overlay.manifest.json"));
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }
}
