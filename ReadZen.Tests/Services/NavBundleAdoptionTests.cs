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
/// PR-NV5 (NAV_CACHE_REDESIGN §4.2 row 3, §7): bundle adopt-on-launch + the headless bake
/// mode. The SEARCH and MASTER indexes already ship prebuilt and adopt on launch; nav now
/// does too. These tests exercise the adoption rung at the SERVICE level (a
/// <see cref="RunLadder"/> helper mirrors the exact precedence of
/// <c>MainWindowViewModel.LoadFileListFromCacheOrBuildAsync</c>) so the evaluator-call
/// counters certify the zero-rebuild claims without launching the GUI. The bundle is a
/// synthetic staged fixture, never a real committed asset.
///
/// Recomputes are counted with a spy <see cref="INavStatusEvaluator"/> so
/// <c>CandidateCalls</c> is the exact number of per-candidate evaluations performed.
/// </summary>
public sealed class NavBundleAdoptionTests : IDisposable
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

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public NavBundleAdoptionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-navbundle-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        _bundleDir = Path.Combine(Path.GetTempPath(), "readzen-navbundle-src-" + Guid.NewGuid().ToString("N")[..8]);
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

    // ================================================================ Cert 1: fresh-install

    /// <summary>
    /// ZERO-REBUILD FRESH-INSTALL cert (NAV_CACHE_REDESIGN §7). A virgin install root (no
    /// local cache) + a staged bundle whose corpus IS the shipped corpus ⇒ the ladder ADOPTS
    /// the bundle and the gated catch-up refresh hits the FAST PATH: evaluator calls == 0,
    /// even after every mtime is reset (the fresh-clone case — hints self-heal via re-hash,
    /// never a status recompute). The adopted cache lands at {root}/index.cache.json.
    /// </summary>
    [Fact]
    public async Task FreshInstall_StagedBundle_Adopted_ZeroEvaluatorCalls()
    {
        MakeGreenCorpus(6);
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

        var (cache, via) = await RunLadder(svc, _root, _origDir, _tranDir, CorpusKind.Cbeta);

        Assert.Equal("bundle", via);                 // adopted, not cold-built
        Assert.Equal(0, eval.CandidateCalls);        // fast path ⇒ ZERO recomputes — the cert
        Assert.Equal(6, cache.Entries.Count);
        Assert.True(File.Exists(CachePath));         // adopted into the local cache path
        Assert.All(cache.Entries, e => Assert.Equal(TranslationStatus.Green, e.Status));
    }

    // ================================================================ Cert 2: stale bundle

    /// <summary>
    /// STALE-BUNDLE cert (NAV_CACHE_REDESIGN §7): the shipped bundle predates the live corpus
    /// by K diverged translations. The ladder still adopts (better than a cold build), and the
    /// gated catch-up refresh recomputes EXACTLY those K entries (K evaluator calls) and reuses
    /// the rest — the graceful "adopt + K-entry catch-up" degrade, no mass rebuild cliff.
    /// </summary>
    [Fact]
    public async Task StaleBundle_DivergedCorpus_AdoptPlusExactlyKCatchup()
    {
        const int N = 8;
        const int K = 3;
        MakeGreenCorpus(N);
        await StageBundleFromCurrentCorpusAsync();

        // Corpus advances after the bake: K translations get new (still-green) content.
        for (int i = 0; i < K; i++)
            WriteFile(TranPath(Rel(i)), GreenXml2, CloneAnchor);

        Assert.False(File.Exists(CachePath));

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService())
        {
            TestOnlyBundlePathOverride = BundlePath
        };

        var (cache, via) = await RunLadder(svc, _root, _origDir, _tranDir, CorpusKind.Cbeta);

        Assert.Equal("bundle", via);
        Assert.Equal(K, eval.CandidateCalls);        // exactly the diverged set — the cert
        Assert.Equal(N, cache.Entries.Count);
        Assert.All(cache.Entries, e => Assert.Equal(TranslationStatus.Green, e.Status));
    }

    // ================================================================ Cert 3: mismatch / corrupt

    /// <summary>
    /// KIND-MISMATCH cert (NAV_CACHE_REDESIGN §4.3): an Open root never adopts the CBETA
    /// bundle. <see cref="IIndexCacheService.TryAdoptBundle"/> returns null and writes NO cache
    /// file, so the ladder falls through to the next rung (a cold build).
    /// </summary>
    [Fact]
    public async Task KindMismatch_OpenRoot_CbetaBundle_Ignored()
    {
        MakeGreenCorpus(2);
        await StageBundleFromCurrentCorpusAsync(); // CorpusKind = "Cbeta"

        var svc = new IndexCacheService(new TranslationStatusService())
        {
            TestOnlyBundlePathOverride = BundlePath
        };

        var adopted = await svc.TryAdoptBundle(_root, CorpusKind.Open);

        Assert.Null(adopted);
        Assert.False(File.Exists(CachePath)); // adoption wrote nothing ⇒ ladder cold-builds
    }

    /// <summary>
    /// CORRUPT-BUNDLE cert (NAV_CACHE_REDESIGN §7): a bundle file that does not parse is
    /// ignored — <see cref="IIndexCacheService.TryAdoptBundle"/> returns null, writes no cache,
    /// and the ladder falls through to the cold build.
    /// </summary>
    [Fact]
    public async Task CorruptBundle_Ignored()
    {
        MakeGreenCorpus(2);
        File.WriteAllText(BundlePath, "{ this is not valid nav cache json ]]");

        var svc = new IndexCacheService(new TranslationStatusService())
        {
            TestOnlyBundlePathOverride = BundlePath
        };

        var adopted = await svc.TryAdoptBundle(_root, CorpusKind.Cbeta);

        Assert.Null(adopted);
        Assert.False(File.Exists(CachePath));
    }

    /// <summary>Absent bundle (raw source build) ⇒ null, cold-build fall-through.</summary>
    [Fact]
    public async Task AbsentBundle_Ignored()
    {
        MakeGreenCorpus(2);
        var svc = new IndexCacheService(new TranslationStatusService())
        {
            TestOnlyBundlePathOverride = Path.Combine(_bundleDir, "does-not-exist.json")
        };

        Assert.Null(await svc.TryAdoptBundle(_root, CorpusKind.Cbeta));
        Assert.False(File.Exists(CachePath));
    }

    // ================================================================ Cert 4: local wins

    /// <summary>
    /// LOCAL-WINS cert (NAV_CACHE_REDESIGN §4.2 rows 1-3): a usable local v5 cache is refreshed
    /// and the bundle is NEVER touched — the local cache's distinctive marker (a forced Yellow
    /// the all-green bundle would flip to Green) survives, the refresh does ZERO recomputes
    /// (fast path), and the bundle file is byte-identical before and after.
    /// </summary>
    [Fact]
    public async Task LocalV5Present_BundleNeverTouched_ByteCompare()
    {
        MakeGreenCorpus(4);

        // Seed a local v5 cache, then stamp a distinctive Yellow on Rel(0) (content is green,
        // so only a stray recompute — or a bundle clobber — would flip it back to Green).
        var seedSvc = new IndexCacheService(new TranslationStatusService());
        var local = await seedSvc.BuildAsync(_origDir, _tranDir, _root);
        var e0 = Entry(local, Rel(0));
        e0.Status = TranslationStatus.Yellow;
        if (e0.Sources.Count > 0) e0.Sources[0].Status = TranslationStatus.Yellow;
        await seedSvc.SaveAsync(_root, local);
        Assert.True(File.Exists(CachePath));

        // Stage an all-green bundle and record its bytes.
        await StageBundleFromCurrentCorpusAsync();
        var bundleBefore = File.ReadAllBytes(BundlePath);

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService())
        {
            TestOnlyBundlePathOverride = BundlePath
        };

        var (cache, via) = await RunLadder(svc, _root, _origDir, _tranDir, CorpusKind.Cbeta);

        Assert.Equal("local-v5", via);                                   // local truth won
        Assert.Equal(0, eval.CandidateCalls);                           // fast path, no recompute
        Assert.Equal(TranslationStatus.Yellow, Entry(cache, Rel(0)).Status); // marker survived
        Assert.Equal(bundleBefore, File.ReadAllBytes(BundlePath));      // bundle untouched
    }

    // ================================================================ headless bake mode

    /// <summary>
    /// BAKE-MODE cert (NAV_CACHE_REDESIGN §4.1): the headless
    /// <c>--build-nav-cache &lt;parentRoot&gt; &lt;outFile&gt;</c> path discovers the CBETA
    /// corpus under a parent root (separate originals/translations repos), runs the v5 pipeline,
    /// and writes a machine-independent <c>nav-cache.cbeta.json</c> that loads back as a usable
    /// v5. Exercised over a SYNTHETIC corpus only — never the real committed asset (that is NV6).
    /// </summary>
    [Fact]
    public void HeadlessBake_SyntheticCorpus_WritesLoadableV5()
    {
        // DiscoverAllCorpora needs originals and translations in SEPARATE subdirs of parentRoot.
        var parentRoot = Path.Combine(Path.GetTempPath(), "readzen-navbake-" + Guid.NewGuid().ToString("N")[..8]);
        var origRepo = Path.Combine(parentRoot, "CbetaZenTexts", "xml-p5");
        var tranRepo = Path.Combine(parentRoot, "CbetaZenTranslations", "xml-p5t");
        Directory.CreateDirectory(origRepo);
        Directory.CreateDirectory(tranRepo);
        try
        {
            for (int i = 0; i < 3; i++)
            {
                WriteFile(Path.Combine(origRepo, Rel(i)), OrigXml, BaseAnchor);
                WriteFile(Path.Combine(tranRepo, Rel(i)), GreenXml, BaseAnchor);
            }

            var outFile = Path.Combine(_bundleDir, "baked-nav-cache.cbeta.json");
            using var log = new StringWriter();

            var code = IndexCacheService.RunHeadlessBuild(
                new[] { "--build-nav-cache", parentRoot, outFile }, log);

            Assert.Equal(0, code);
            Assert.True(File.Exists(outFile));

            // The baked file itself is a usable v5 with the right kind + entry count.
            var baked = JsonSerializer.Deserialize<IndexCache>(File.ReadAllText(outFile), JsonOpts)!;
            Assert.Equal(5, baked.Version);
            Assert.Equal("Cbeta", baked.CorpusKind);
            Assert.Equal(3, baked.Entries.Count);
            Assert.All(baked.Entries, e => Assert.Equal(TranslationStatus.Green, e.Status));
        }
        finally
        {
            try { Directory.Delete(parentRoot, true); } catch { }
        }
    }

    /// <summary>Bad args (missing outFile) ⇒ usage + non-zero exit, no file written.</summary>
    [Fact]
    public void HeadlessBake_MissingArgs_ReturnsUsageCode()
    {
        using var log = new StringWriter();
        var code = IndexCacheService.RunHeadlessBuild(new[] { "--build-nav-cache", _root }, log);
        Assert.Equal(1, code);
        Assert.Contains("usage:", log.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
