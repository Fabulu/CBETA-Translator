using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-M3 (SPEC §6.1, §8): the CI-cheap LOCKSTEP GUARD over the committed shipping asset
/// <c>Assets/Data/master-corpus-index.json</c>. It deserializes the committed bundle's
/// composite v2 stamp, recomputes the ROSTER half
/// (<c>roster=count={M};hash={R16}</c>) from the committed <c>Assets/Data/master-dates.json</c>
/// with the SAME loader the bake and the app use (base only, NO community overlay), and
/// FAILS the build when they diverge — so a PR that edits the roster without rebaking the
/// bundle cannot merge.
///
/// The CORPUS half (files/bytes/pathsig/titles) is deliberately NOT checked: CI has no CBETA
/// corpus, and a corpus-half mismatch degrades safely to a runtime rebuild (SPEC §6.1). Only
/// the roster half is content-checkable from committed files, so only it is the unmergeable
/// guard.
///
/// Both committed files are read from the app project's Content copied next to the test
/// binary (identical mechanism to master-dates.json in LineageRosterServiceTests), with a
/// walk-up-to-repo-root fallback. Every assertion is over committed bytes + pure hashing —
/// fully deterministic, no wall-clock/random dependence.
/// </summary>
public sealed class MasterBundleLockstepTests : IClassFixture<MasterBundleLockstepTests.CommittedBundleFixture>
{
    private readonly CommittedBundleFixture _fx;
    public MasterBundleLockstepTests(CommittedBundleFixture fx) => _fx = fx;

    // The committed asset's pinned identity. These pin the SHIPPED bundle; a deliberate
    // rebake (roster or corpus change) must update them alongside the asset — that is the
    // point of a pinning guard. (SPEC §8 quoted ~122955 appearances as a pre-bake estimate;
    // the committed asset materialized 122900 — the real value is authoritative here.)
    private const string ExpectedCorpus = "Cbeta";
    private const int ExpectedMasterCount = 924;
    private const int ExpectedAppearanceCount = 122900;

    // ---- stamp helpers ----------------------------------------------------------------

    /// <summary>Roster half of a composite v2 stamp: everything from <c>roster=</c> to the
    /// end (<c>roster=count={M};hash={R16}</c>), i.e. exactly what
    /// <see cref="MasterCorpusSearchService.ComputeRosterIdentity"/> returns.</summary>
    private static string RosterHalfOf(string compositeStamp)
    {
        var idx = compositeStamp.IndexOf("roster=", StringComparison.Ordinal);
        Assert.True(idx >= 0, "committed stamp has no roster half: " + compositeStamp);
        return compositeStamp.Substring(idx);
    }

    private static int ParseRosterCount(string rosterHalf)
    {
        var m = Regex.Match(rosterHalf, @"count=(\d+)");
        Assert.True(m.Success, "roster half has no count: " + rosterHalf);
        return int.Parse(m.Groups[1].Value);
    }

    // =====================================================================================
    // Asset integrity — the committed bundle parses and carries the shipped identity
    // =====================================================================================

    [Fact]
    public void CommittedBundle_Exists_AndParsesAsMasterCorpusIndex()
    {
        Assert.True(File.Exists(_fx.BundlePath), "committed bundle missing: " + _fx.BundlePath);
        Assert.NotNull(_fx.Index);
        Assert.NotNull(_fx.Index.CorpusStamp);
        Assert.NotEmpty(_fx.Index.Appearances);
    }

    [Fact]
    public void CommittedBundle_Corpus_IsCbetaOnly_NotMultiCorpus()
    {
        // Guards an accidental Cbeta+Open bake: a shipped bundle scoped to more than CBETA
        // would carry a corpus stamp no CBETA-only fresh install could ever match (never
        // adopted, permanent rebuild churn). The label must be exactly "Cbeta", no '+'.
        Assert.Equal(ExpectedCorpus, _fx.Index.Corpus);
        Assert.DoesNotContain("+", _fx.Index.Corpus ?? "");
    }

    [Fact]
    public void CommittedBundle_MasterCount_Is924_AndMatchesDistinctAppearanceMasters()
    {
        Assert.Equal(ExpectedMasterCount, _fx.Index.MasterCount);

        // Cross-check the stored count against the actual appearance payload — a truncated or
        // tampered appearances array would desync these two.
        var distinct = _fx.Index.Appearances.Select(a => a.MasterName).Distinct().Count();
        Assert.Equal(ExpectedMasterCount, distinct);
    }

    [Fact]
    public void CommittedBundle_AppearanceCount_MatchesShippedAsset()
    {
        Assert.Equal(ExpectedAppearanceCount, _fx.Index.Appearances.Count);
    }

    [Fact]
    public void CommittedBundle_Stamp_IsV2Composite_WithNoTicks()
    {
        var stamp = _fx.Index.CorpusStamp!;
        Assert.StartsWith("v2;corpus=files=", stamp);
        foreach (var token in new[] { "corpus=files=", "bytes=", "pathsig=", "titles=", "roster=count=", "hash=" })
            Assert.Contains(token, stamp);
        // The v1 stamp keyed an mtime ("maxTicks"); the shipped v2 stamp must carry none —
        // otherwise it could never read fresh on a clone (the whole reason for the bundle).
        Assert.DoesNotContain("maxTicks", stamp);
        Assert.DoesNotContain("Ticks", stamp);
    }

    // =====================================================================================
    // THE lockstep guard — committed roster hash == recomputed hash of master-dates.json
    // =====================================================================================

    [Fact]
    public void RosterHalf_MatchesRecomputedIdentity_LockstepGuard()
    {
        // Recompute the roster identity from the committed master-dates.json using the SAME
        // loader the bake used (base only, no overlay). If a PR edited the roster without
        // rebaking master-corpus-index.json, the embedded roster half diverges and THIS fails.
        var recomputed = MasterCorpusSearchService.ComputeRosterIdentity(_fx.Catalog);
        var embedded = RosterHalfOf(_fx.Index.CorpusStamp!);

        Assert.Equal(embedded, recomputed);
    }

    [Fact]
    public void RosterCount_InStamp_EqualsLoadedCatalogCount()
    {
        var embeddedCount = ParseRosterCount(RosterHalfOf(_fx.Index.CorpusStamp!));
        Assert.Equal(_fx.Catalog.Records.Count, embeddedCount);
        // Sanity floor: the shipped roster is the full ~944-record set, never a stub.
        Assert.True(embeddedCount >= 900, "roster count unexpectedly small: " + embeddedCount);
    }

    [Fact]
    public async Task RecomputedRosterIdentity_IsDeterministic_AcrossReloads()
    {
        // A second independent load of the same committed file must yield a byte-identical
        // identity — proves the guard is stable (no ordering/wall-clock nondeterminism).
        var mgr = new ZenMasterManagerService(new MasterDatesService());
        var reloaded = await mgr.LoadAsync(repoRoot: null, baseFilePath: _fx.RosterPath);

        Assert.Equal(
            MasterCorpusSearchService.ComputeRosterIdentity(_fx.Catalog),
            MasterCorpusSearchService.ComputeRosterIdentity(reloaded));
    }

    [Fact]
    public void RosterIdentity_HasExpectedShape()
    {
        var recomputed = MasterCorpusSearchService.ComputeRosterIdentity(_fx.Catalog);
        Assert.StartsWith("roster=count=", recomputed);
        Assert.Matches(@"^roster=count=\d+;hash=[0-9a-f]{16}$", recomputed);
    }

    // =====================================================================================
    // Fresh-install simulation — the committed bundle adopts with ZERO build (SPEC §8 PR-M3)
    // =====================================================================================

    [Fact]
    public async Task FreshInstall_AdoptsCommittedBundle_ZeroBuild_Loads924()
    {
        // Simulate a fresh install where the live composite stamp == the shipped bundle's
        // stamp (shipped corpus + shipped roster). The virgin cache dir adopts the committed
        // asset by a byte copy — no BuildFullIndexAsync is ever invoked (zero build) — and the
        // immediately following TryLoadAsync serves the full 924/122900 index from the copy.
        var cacheDir = _fx.NewTempDir("fresh-cache");
        var liveStamp = _fx.Index.CorpusStamp!; // fresh install: live == bundle stamp

        var svc = new MasterCorpusSearchService();
        var adopted = await svc.TryAdoptBundleAsync(cacheDir, _fx.BundlePath, liveStamp, CancellationToken.None);
        Assert.True(adopted);

        // Freshness OFF (no CBETA corpus in CI to recompute the corpus half): prove the adopted
        // file is a valid, complete index the loader serves.
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parentRootForFreshness: null, rosterIdentity: null);
        Assert.NotNull(loaded);
        Assert.Equal(ExpectedMasterCount, loaded!.MasterCount);
        Assert.Equal(ExpectedAppearanceCount, loaded.Appearances.Count);
        Assert.Equal(liveStamp, loaded.CorpusStamp);
    }

    [Fact]
    public async Task StaleLocalCache_AdoptsCommittedBundle_OverLocal()
    {
        // The update case: a stale local cache (wrong stamp) is fully replaced by the committed
        // bundle when the live stamp matches the bundle — zero build, no merge.
        var cacheDir = _fx.NewTempDir("stale-cache");
        var cachePath = Path.Combine(cacheDir, "master-corpus-index.json");
        var stale = new MasterCorpusIndex
        {
            Corpus = "Cbeta",
            CorpusStamp = "v2;corpus=files=1;bytes=1;pathsig=0000000000000000;titles=0000000000000000;roster=count=1;hash=0000000000000000",
            MasterCount = 279, // the "279 of 944" stale-cache pathology
            Appearances = { new MasterTextAppearance { MasterName = "Stale", RelPath = "x.xml" } },
        };
        await new MasterCorpusSearchService().SaveAsync(cacheDir, stale);

        var liveStamp = _fx.Index.CorpusStamp!;
        var svc = new MasterCorpusSearchService();
        Assert.True(await svc.TryAdoptBundleAsync(cacheDir, _fx.BundlePath, liveStamp, CancellationToken.None));

        // The stale 279 content is gone; the cache is now the committed 924-master bundle.
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parentRootForFreshness: null, rosterIdentity: null);
        Assert.NotNull(loaded);
        Assert.Equal(ExpectedMasterCount, loaded!.MasterCount);
        Assert.True(File.ReadAllBytes(cachePath).AsSpan()
            .SequenceEqual(File.ReadAllBytes(_fx.BundlePath)));
    }

    [Fact]
    public async Task NonMatchingLiveStamp_DoesNotAdoptCommittedBundle()
    {
        // False-fresh guard: if the live stamp does NOT equal the committed bundle's stamp
        // (diverged corpus/roster/titles), the real asset must NOT be adopted — the caller
        // rebuilds. Proves adoption of the shipped asset is stamp-gated, not unconditional.
        var cacheDir = _fx.NewTempDir("nomatch-cache");
        var svc = new MasterCorpusSearchService();

        var adopted = await svc.TryAdoptBundleAsync(
            cacheDir, _fx.BundlePath,
            "v2;corpus=files=99;bytes=99;pathsig=1111111111111111;titles=1111111111111111;roster=count=1;hash=1111111111111111",
            CancellationToken.None);

        Assert.False(adopted);
        Assert.False(File.Exists(Path.Combine(cacheDir, "master-corpus-index.json")));
    }

    // =====================================================================================
    // Shared, once-per-class load of the committed 60 MB asset + roster catalog.
    // =====================================================================================

    public sealed class CommittedBundleFixture : IAsyncLifetime, IDisposable
    {
        public string AssetsDir { get; private set; } = "";
        public string BundlePath { get; private set; } = "";
        public string RosterPath { get; private set; } = "";
        public MasterCorpusIndex Index { get; private set; } = null!;
        public ZenMasterCatalog Catalog { get; private set; } = null!;

        private readonly string _tempRoot =
            Path.Combine(Path.GetTempPath(), "readzen-lockstep-" + Guid.NewGuid().ToString("N")[..8]);

        public async Task InitializeAsync()
        {
            AssetsDir = LocateAssetsDir();
            BundlePath = Path.Combine(AssetsDir, "master-corpus-index.json");
            RosterPath = Path.Combine(AssetsDir, "master-dates.json");
            Assert.True(File.Exists(RosterPath), "committed roster missing: " + RosterPath);

            var json = await File.ReadAllTextAsync(BundlePath, Encoding.UTF8);
            Index = JsonSerializer.Deserialize<MasterCorpusIndex>(json)
                    ?? throw new InvalidOperationException("committed bundle did not deserialize");

            // Base roster only (repoRoot null ⇒ no community overlay) — exactly the record set
            // the bake used and the shipped app resolves on a fresh install.
            var mgr = new ZenMasterManagerService(new MasterDatesService());
            Catalog = await mgr.LoadAsync(repoRoot: null, baseFilePath: RosterPath);
            Assert.True(Catalog.Records.Count > 0, "roster catalog empty");
        }

        public string NewTempDir(string tag)
        {
            var p = Path.Combine(_tempRoot, tag + "-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(p);
            return p;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public void Dispose()
        {
            try { Directory.Delete(_tempRoot, true); } catch { }
        }

        /// <summary>
        /// Directory holding the committed bundle. Primary: the app project's Content copied
        /// next to the test binary (<c>{BaseDirectory}/Assets/Data</c>, same as master-dates.json);
        /// fallback: walk up to the repo-root <c>Assets/Data</c> for source checkouts where the
        /// large asset was not copied. Roster is read from the SAME directory so both come from
        /// one commit.
        /// </summary>
        private static string LocateAssetsDir()
        {
            var binDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Data");
            if (File.Exists(Path.Combine(binDir, "master-corpus-index.json")))
                return binDir;

            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "Data", "master-corpus-index.json");
                if (File.Exists(candidate))
                    return Path.GetDirectoryName(candidate)!;
            }

            throw new FileNotFoundException(
                "Committed master-corpus-index.json not found next to the test binary or in any " +
                "parent Assets/Data. PR-M3 ships it via ReadZen.App.csproj Content — ensure the " +
                "asset is committed and copied to the test output.");
        }
    }
}
