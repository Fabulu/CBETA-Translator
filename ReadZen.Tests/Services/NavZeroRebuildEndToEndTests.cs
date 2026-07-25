using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-NV6 (NAV_CACHE_REDESIGN §7, §8 row 6): the end-to-end ZERO-REBUILD cert for the nav leg.
/// The search and master indexes already prove their own fresh-install adoption in their bundle
/// tests; this class is the nav analogue — a single flow that exercises the full launch ladder
/// the way <c>MainWindowViewModel.LoadFileListFromCacheOrBuildAsync</c> does and certifies:
///
///   1. a fresh install (virgin root, no local cache) + a matching shipped bundle ⇒ nav is
///      ADOPTED and the gated catch-up refresh hits the FAST PATH — every build/evaluator
///      counter is ZERO, even after a full mtime reset (the fresh-clone case); then
///   2. exactly ONE translation edit ⇒ the next gated refresh recomputes EXACTLY ONE entry.
///
/// Recomputes are counted with a spy <see cref="INavStatusEvaluator"/> so
/// <c>CandidateCalls</c> is the exact number of per-candidate evaluations performed — the same
/// instrument the sibling adoption/migration certs use.
/// </summary>
public sealed class NavZeroRebuildEndToEndTests : IDisposable
{
    private readonly string _root;      // the "user" install root (translations repo root)
    private readonly string _origDir;
    private readonly string _tranDir;
    private readonly string _bundleDir; // scratch dir holding the staged bundle FILE

    private const string OrigXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>禪宗祖師傳法心印無門關</p>\n" +
        "</body></text></TEI>\n";

    private const string GreenXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>The gateless gate of the ancestors transmitting the mind seal.</p>\n" +
        "</body></text></TEI>\n";

    // A DIFFERENT green translation (distinct bytes ⇒ distinct ContentSig, still Green).
    private const string GreenXml2 =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>A revised rendering of the ancestral transmission of the mind seal.</p>\n" +
        "</body></text></TEI>\n";

    private static readonly DateTime BaseAnchor = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CloneAnchor = new(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EditAnchor = new(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public NavZeroRebuildEndToEndTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-nave2e-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        _bundleDir = Path.Combine(Path.GetTempPath(), "readzen-nave2e-src-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
        Directory.CreateDirectory(_bundleDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
        try { Directory.Delete(_bundleDir, true); } catch { }
    }

    // ----------------------------------------------------------------- helpers

    private sealed class CountingNavEvaluator : INavStatusEvaluator
    {
        private readonly INavStatusEvaluator _inner =
            new NavStatusEvaluator(new TranslationStatusService(), new IndexedTranslationService());
        private int _calls;
        public int CandidateCalls => Volatile.Read(ref _calls);
        public void Reset() => Volatile.Write(ref _calls, 0);

        public TranslationStatus ComputeCandidateStatus(string origAbs, string tranAbs)
        {
            Interlocked.Increment(ref _calls);
            return _inner.ComputeCandidateStatus(origAbs, tranAbs);
        }
        public TranslationStatus EvaluateEntry(string origAbs, IReadOnlyList<string> candidates)
            => _inner.EvaluateEntry(origAbs, candidates);
        public bool IsMeaningfullyTranslated(string origAbs, string tranAbs)
            => _inner.IsMeaningfullyTranslated(origAbs, tranAbs);
        public void ClearCache() => _inner.ClearCache();
    }

    private static void WriteFile(string path, string content, DateTime mtimeUtc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, mtimeUtc);
    }

    private static string Rel(int i) => $"t{i:D4}.xml";
    private string OrigPath(string rel) => Path.Combine(_origDir, rel);
    private string TranPath(string rel) => Path.Combine(_tranDir, rel);
    private string CachePath => Path.Combine(_root, "index.cache.json");
    private string BundlePath => Path.Combine(_bundleDir, "nav-cache.cbeta.json");

    private void MakeGreenCorpus(int n)
    {
        for (int i = 0; i < n; i++)
        {
            WriteFile(OrigPath(Rel(i)), OrigXml, BaseAnchor);
            WriteFile(TranPath(Rel(i)), GreenXml, BaseAnchor);
        }
    }

    /// <summary>Bakes a v5 cache over the CURRENT corpus and stages it as the bundle FILE
    /// (never as the local index.cache.json). This is the synthetic "shipped asset".</summary>
    private async Task StageBundleFromCurrentCorpusAsync()
    {
        var bakeSvc = new IndexCacheService(new TranslationStatusService());
        var bundle = await bakeSvc.BuildAsync(_origDir, _tranDir, _root);
        File.WriteAllText(BundlePath, JsonSerializer.Serialize(bundle, JsonOpts));
    }

    private static FileNavItem Entry(IndexCache cache, string rel)
        => cache.Entries.Single(e => string.Equals(
            e.RelPath.Replace('\\', '/'), rel.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Mirrors the EXACT launch ladder of
    /// <c>MainWindowViewModel.LoadFileListFromCacheOrBuildAsync</c>: (1) local v5 ⇒ gated
    /// refresh; (2) local v4 ⇒ migrate; (3) no usable local + a matching bundle ⇒ adopt +
    /// gated catch-up refresh; (4) nothing ⇒ cold build. Returns the resulting cache and the
    /// rung that produced it.
    /// </summary>
    private static async Task<(IndexCache cache, string via)> RunLadder(
        IndexCacheService svc, string root, string origDir, string tranDir, CorpusKind kind)
    {
        var load = await svc.LoadAsync(root);

        if (load.Cache?.Entries is { Count: > 0 } && load.Status == NavCacheLoadStatus.V5)
            return (await svc.RefreshAsync(load.Cache, origDir, tranDir, root), "local-v5");

        if (load.Cache?.Entries is { Count: > 0 } && load.Status == NavCacheLoadStatus.V4NeedsMigration)
            return (await svc.MigrateV4(load.Cache, origDir, tranDir, root), "migrate-v4");

        var adopted = await svc.TryAdoptBundle(root, kind);
        if (adopted?.Entries is { Count: > 0 })
            return (await svc.RefreshAsync(adopted, origDir, tranDir, root), "bundle");

        var built = await svc.BuildAsync(origDir, tranDir, root);
        await svc.SaveAsync(root, built);
        return (built, "cold");
    }

    // ================================================================ the end-to-end nav leg

    /// <summary>
    /// ZERO-REBUILD end-to-end nav cert (NAV_CACHE_REDESIGN §7). Phase 1: a virgin install root
    /// + a matching shipped bundle ⇒ the ladder ADOPTS the bundle (via == "bundle") and the
    /// gated catch-up refresh does ZERO evaluator work even after every mtime is reset (the
    /// fresh-clone case; hints self-heal via re-hash, never a status recompute). The adopted
    /// cache lands at {root}/index.cache.json with all statuses intact. Phase 2: the user edits
    /// exactly ONE translation ⇒ the next gated refresh recomputes EXACTLY ONE entry and reuses
    /// the rest — nav recompute == 1.
    /// </summary>
    [Fact]
    public async Task FreshInstall_NavAdopted_ZeroCounters_ThenOneEdit_RecomputesExactlyOne()
    {
        const int N = 6;
        MakeGreenCorpus(N);
        await StageBundleFromCurrentCorpusAsync();

        // Simulate a fresh clone: every working-tree mtime is rewritten (content unchanged).
        foreach (var f in Directory.EnumerateFiles(_root, "*.xml", SearchOption.AllDirectories))
            File.SetLastWriteTimeUtc(f, CloneAnchor);

        Assert.False(File.Exists(CachePath)); // virgin: no local cache

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService())
        {
            TestOnlyBundlePathOverride = BundlePath
        };

        // ---- Phase 1: fresh-install adoption, zero rebuild ---------------------------------
        var (cache, via) = await RunLadder(svc, _root, _origDir, _tranDir, CorpusKind.Cbeta);

        Assert.Equal("bundle", via);              // nav ADOPTED, not cold-built
        Assert.Equal(0, eval.CandidateCalls);     // fast path ⇒ ZERO recomputes — the cert
        Assert.Equal(N, cache.Entries.Count);
        Assert.True(File.Exists(CachePath));       // adopted into the local cache path
        Assert.All(cache.Entries, e => Assert.Equal(TranslationStatus.Green, e.Status));

        // ---- Phase 2: exactly one translation edit ⇒ exactly one recompute -----------------
        eval.Reset();
        WriteFile(TranPath(Rel(0)), GreenXml2, EditAnchor); // one translation's bytes change

        var refreshed = await svc.RefreshAsync(cache, _origDir, _tranDir, _root);

        Assert.Equal(1, eval.CandidateCalls);      // nav recompute == 1 — the incremental cert
        Assert.Equal(N, refreshed.Entries.Count);
        Assert.All(refreshed.Entries, e => Assert.Equal(TranslationStatus.Green, e.Status));
    }
}
