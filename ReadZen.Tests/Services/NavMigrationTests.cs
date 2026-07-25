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
/// PR-NV3 (NAV_CACHE_REDESIGN §4.4, §8 row 3): the one-time v4 -&gt; v5 migration. A local v4
/// cache carries the user's OWN locally-computed statuses, so migration converts its absolute
/// <c>TranResolvedPath</c> values into relative source tokens + content sigs and hands the
/// result to the gated <see cref="IndexCacheService.RefreshAsync"/> — which recomputes ONLY
/// the manifest overlap/dropped set (a handful), never the whole corpus, and preserves every
/// single-source status.
///
/// Recomputes are counted with a spy <see cref="INavStatusEvaluator"/> so
/// <c>CandidateCalls</c> is the exact number of per-candidate evaluations the migration's
/// gated refresh performed.
/// </summary>
public sealed class NavMigrationTests : IDisposable
{
    private readonly string _root;
    private readonly string _origDir;
    private readonly string _tranDir;

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

    private static readonly DateTime BaseAnchor = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public NavMigrationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-navmig-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
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
    private string CommunityPath(string user, string rel)
        => Path.Combine(_root, "community", "translations", user, rel);

    private void MakeGreenCorpus(int n)
    {
        for (int i = 0; i < n; i++)
        {
            WriteFile(OrigPath(Rel(i)), OrigXml, BaseAnchor);
            WriteFile(TranPath(Rel(i)), GreenXml, BaseAnchor);
        }
    }

    private static FileNavItem Entry(IndexCache cache, string rel)
        => cache.Entries.Single(e => string.Equals(
            e.RelPath.Replace('\\', '/'), rel.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

    /// <summary>Builds a real v5 cache, then downgrades it to a plausible on-machine v4 cache:
    /// Version=4, an old build guid, empty <c>Sources</c>, and an ABSOLUTE
    /// <c>TranResolvedPath</c> pointing at the file the v4 resolver would have chosen.</summary>
    private async Task<IndexCache> BuildV4CacheAsync(
        TranslationStatus? forceStatus = null,
        Func<FileNavItem, string?>? resolveOverride = null)
    {
        var svc = new IndexCacheService(new TranslationStatusService());
        var v5 = await svc.BuildAsync(_origDir, _tranDir, _root);

        string? DefaultResolve(FileNavItem e)
        {
            var s = e.Sources.FirstOrDefault();
            if (s == null) return null;
            return s.Token == "canonical"
                ? TranPath(e.RelPath)
                : CommunityPath(s.Token.Substring("user:".Length), e.RelPath);
        }
        var resolve = resolveOverride ?? DefaultResolve;

        var v4 = new IndexCache
        {
            Version = 4,
            RootPath = _root,
            BuildGuid = "phase4-nav-v5-content-gate",
            TitlesHash = v5.TitlesHash,
            Entries = new List<FileNavItem>(),
        };
        foreach (var e in v5.Entries)
        {
            v4.Entries.Add(new FileNavItem
            {
                RelPath = e.RelPath,
                FileName = e.FileName,
                DisplayShort = e.DisplayShort,
                Tooltip = e.Tooltip,
                Status = forceStatus ?? e.Status,
                OrigSizeBytes = e.OrigSizeBytes,
                OrigMtimeTicks = e.OrigMtimeTicks,
                TranSizeBytes = e.TranSizeBytes,
                TranslatedMtimeTicks = e.TranslatedMtimeTicks,
                TranResolvedPath = resolve(e),
                // Sources intentionally empty — v4 had no per-candidate records.
            });
        }
        return v4;
    }

    // ================================================================ classification

    /// <summary>A v4 cache on disk is classified <see cref="NavCacheLoadStatus.V4NeedsMigration"/>
    /// (not Unusable) so the launch ladder can migrate rather than rebuild.</summary>
    [Fact]
    public async Task LoadAsync_V4Cache_ClassifiedAsNeedsMigration()
    {
        MakeGreenCorpus(2);
        var v4 = await BuildV4CacheAsync();

        var svc = new IndexCacheService(new TranslationStatusService());
        File.WriteAllText(svc.GetCachePath(_root),
            JsonSerializer.Serialize(v4, new JsonSerializerOptions { WriteIndented = true }));

        var result = await svc.LoadAsync(_root);

        Assert.Equal(NavCacheLoadStatus.V4NeedsMigration, result.Status);
        Assert.NotNull(result.Cache);
        Assert.Equal(4, result.Cache!.Version);
        // TryLoadAsync (V5-only) still refuses it — the ladder must route via LoadAsync.
        Assert.Null(await svc.TryLoadAsync(_root));
    }

    // ================================================================ single-source preservation

    /// <summary>
    /// Migration cert (single-source): every entry resolves to ONE canonical file, so the
    /// gate reuses all of them — ZERO evaluator calls — and the user's own stored statuses
    /// carry over verbatim (a deliberately-distinctive Yellow is kept, NOT re-derived to
    /// Green). The result is a saved v5 cache with per-candidate source records.
    /// </summary>
    [Fact]
    public async Task MigrateV4_AllSingleSource_PreservesStatuses_ZeroEvaluatorCalls()
    {
        MakeGreenCorpus(4);
        // Force a status the evaluator would NEVER produce for these fully-green files, so a
        // stray recompute would visibly flip it.
        var v4 = await BuildV4CacheAsync(forceStatus: TranslationStatus.Yellow);

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService());

        var migrated = await svc.MigrateV4(v4, _origDir, _tranDir, _root);

        Assert.Equal(0, eval.CandidateCalls);                       // no recompute
        Assert.Equal(5, migrated.Version);
        Assert.Equal(4, migrated.Entries.Count);
        Assert.All(migrated.Entries, e =>
        {
            Assert.Equal(TranslationStatus.Yellow, e.Status);        // carried, not re-derived
            Assert.Equal(new[] { "canonical" }, e.Sources.Select(s => s.Token).ToArray());
            Assert.Equal(TranslationStatus.Yellow, e.Sources[0].Status);
            Assert.False(string.IsNullOrEmpty(e.Sources[0].ContentSig));
        });

        // Persisted as v5: a follow-up structural load succeeds.
        var reloaded = await svc.TryLoadAsync(_root);
        Assert.NotNull(reloaded);
        Assert.Equal(4, reloaded!.Entries.Count);
    }

    /// <summary>An untranslated original in a v4 cache (no <c>TranResolvedPath</c>) migrates to
    /// an empty-source Red entry with zero I/O and zero evaluator calls.</summary>
    [Fact]
    public async Task MigrateV4_UntranslatedEntry_NoSources_NoEvaluatorCalls()
    {
        WriteFile(OrigPath(Rel(0)), OrigXml, BaseAnchor);   // orig only, no translation
        WriteFile(OrigPath(Rel(1)), OrigXml, BaseAnchor);
        WriteFile(TranPath(Rel(1)), GreenXml, BaseAnchor);
        var v4 = await BuildV4CacheAsync();

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService());

        var migrated = await svc.MigrateV4(v4, _origDir, _tranDir, _root);

        Assert.Equal(0, eval.CandidateCalls);
        Assert.Empty(Entry(migrated, Rel(0)).Sources);
        Assert.Equal(TranslationStatus.Red, Entry(migrated, Rel(0)).Status);
        Assert.Equal(new[] { "canonical" }, Entry(migrated, Rel(1)).Sources.Select(s => s.Token).ToArray());
    }

    // ================================================================ the update-cert (overlap)

    /// <summary>
    /// The zero-rebuild UPDATE cert (NAV_CACHE_REDESIGN §8 row 3): a v4 cache resolved each rel
    /// to a SINGLE source. Where the live manifest shows a rel in more than one source dir
    /// (canonical + a community user), the migrated single value could understate the
    /// multi-source max — so the gate recomputes EXACTLY that overlap entry (one evaluator
    /// call, for the newly-visible candidate) and nothing else. The stale v4 Red is corrected
    /// to the true Green max; every non-overlap status is preserved.
    /// </summary>
    [Fact]
    public async Task MigrateV4_MultiSourceOverlap_RecomputesOnlyOverlap()
    {
        MakeGreenCorpus(4);
        WriteFile(CommunityPath("alice", Rel(0)), GreenXml, BaseAnchor);  // Rel(0) now overlaps

        // v4 resolved Rel(0) to canonical only, and (say) understated it as Red.
        var v4 = await BuildV4CacheAsync();
        Entry(v4, Rel(0)).Status = TranslationStatus.Red;

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService());

        var migrated = await svc.MigrateV4(v4, _origDir, _tranDir, _root);

        Assert.Equal(1, eval.CandidateCalls);                        // ≈ overlap set (just user:alice)
        var e0 = Entry(migrated, Rel(0));
        Assert.Equal(new[] { "canonical", "user:alice" }, e0.Sources.Select(s => s.Token).ToArray());
        Assert.Equal(TranslationStatus.Green, e0.Status);            // stale Red corrected to true max
        // The other three single-source entries were reused untouched.
        for (int i = 1; i < 4; i++)
            Assert.Equal(TranslationStatus.Green, Entry(migrated, Rel(i)).Status);
    }

    // ================================================================ foreign-prefix drop

    /// <summary>A v4 entry whose absolute <c>TranResolvedPath</c> has a foreign/unparseable
    /// prefix (a stale machine path) drops its source record; the gate then recomputes that
    /// entry from the live manifest, picking up the real local candidate.</summary>
    [Fact]
    public async Task MigrateV4_ForeignPrefix_DroppedAndRecomputed()
    {
        MakeGreenCorpus(2);
        var foreign = Path.Combine(Path.GetTempPath(), "foreign-machine", "xml-p5t", Rel(0));

        var v4 = await BuildV4CacheAsync(resolveOverride: e =>
            string.Equals(e.RelPath.Replace('\\', '/'), Rel(0), StringComparison.OrdinalIgnoreCase)
                ? foreign
                : TranPath(e.RelPath));
        // Understate the foreign-prefixed entry so a successful recompute is observable.
        Entry(v4, Rel(0)).Status = TranslationStatus.Red;

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService());

        var migrated = await svc.MigrateV4(v4, _origDir, _tranDir, _root);

        Assert.Equal(1, eval.CandidateCalls);                        // only the dropped/recomputed entry
        var e0 = Entry(migrated, Rel(0));
        Assert.Equal(new[] { "canonical" }, e0.Sources.Select(s => s.Token).ToArray());
        Assert.Equal(TranslationStatus.Green, e0.Status);            // recovered from the live file
        Assert.Equal(TranslationStatus.Green, Entry(migrated, Rel(1)).Status);
    }

    // ================================================================ phantom-path regression (NV4 fix)

    /// <summary>
    /// PHANTOM-PATH regression (NAV_CACHE_REDESIGN §4.4; the "3-minute migration hang" fix,
    /// <c>IndexCacheService.MigrateV4</c> ~:973). The real on-machine v4 cache stored a
    /// <c>TranResolvedPath</c> on EVERY entry — including the thousands of UNtranslated texts,
    /// whose path is the never-created canonical <c>xml-p5t</c> path. The bug: migration built a
    /// bogus source record for each non-existent path, hashed thousands of phantom files, and
    /// then mismatched the fresh (empty) manifest for all of them ⇒ a full-corpus recompute
    /// (the multi-minute hang). The fix guards source creation with <c>File.Exists</c>.
    ///
    /// This pins it: MOST entries carry a canonical path to a NON-EXISTENT file (untranslated),
    /// plus a few real ones. Migration must produce empty <c>Sources</c> + Red for the phantom
    /// entries, real single-source records only for the existing files, and — critically — keep
    /// the evaluator-call count bounded by the REAL-file set (≈ K), NOT ~all N entries. Pre-fix
    /// this count was ≈ N−K; post-fix it is ≤ K.
    /// </summary>
    [Fact]
    public async Task MigrateV4_PhantomCanonicalPaths_EmptySourcesRed_BoundedRecompute()
    {
        const int N = 40;   // originals
        const int K = 3;    // actually translated (Rel 0..K-1)

        for (int i = 0; i < N; i++)
            WriteFile(OrigPath(Rel(i)), OrigXml, BaseAnchor);
        for (int i = 0; i < K; i++)
            WriteFile(TranPath(Rel(i)), GreenXml, BaseAnchor);

        // The v4 disease: EVERY entry (translated or not) carries the canonical xml-p5t path,
        // which exists only for the K translated rels. This is what the live v4 cache looked
        // like on the user's machine.
        var v4 = await BuildV4CacheAsync(resolveOverride: e => TranPath(e.RelPath));
        Assert.All(v4.Entries, e => Assert.False(string.IsNullOrEmpty(e.TranResolvedPath)));

        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService());

        var migrated = await svc.MigrateV4(v4, _origDir, _tranDir, _root);

        // The anti-hang cert: recompute is bounded by the real-file set, not the phantom mass.
        Assert.True(eval.CandidateCalls <= K,
            $"expected <= {K} evaluator calls (real-file set), got {eval.CandidateCalls} " +
            "— the phantom-path guard regressed and migration is recomputing the corpus.");

        Assert.Equal(N, migrated.Entries.Count);

        // Phantom entries: no candidate ⇒ empty Sources, forced Red.
        for (int i = K; i < N; i++)
        {
            var e = Entry(migrated, Rel(i));
            Assert.Empty(e.Sources);
            Assert.Equal(TranslationStatus.Red, e.Status);
        }

        // Real entries: exactly one canonical source record for the file that exists.
        for (int i = 0; i < K; i++)
        {
            var e = Entry(migrated, Rel(i));
            Assert.Equal(new[] { "canonical" }, e.Sources.Select(s => s.Token).ToArray());
            Assert.False(string.IsNullOrEmpty(e.Sources[0].ContentSig));
            Assert.Equal(TranslationStatus.Green, e.Status);
        }
    }

    // ================================================================ delimiter hardening (NV2 review)

    /// <summary>
    /// NV2-review hardening: the runtime candidate-set / hint keys use an explicit delimiter,
    /// so an empty/short/malformed <see cref="NavSourceRecord.ContentSig"/> can never let two
    /// DISTINCT (token, sig) pairs concatenate to the same string and falsely SetEqual. Without
    /// the separator, ("canonical","ab") and ("canonicala","b") would both be "canonicalab".
    /// </summary>
    [Fact]
    public void ComposeKey_EmptyOrShortSig_DoesNotCollide()
    {
        // The classic no-delimiter collision — must stay distinct.
        Assert.NotEqual(
            IndexCacheService.ComposeKey("canonical", "ab"),
            IndexCacheService.ComposeKey("canonicala", "b"));

        // An empty sig on a token is distinct from any real sig on the same token…
        Assert.NotEqual(
            IndexCacheService.ComposeKey("canonical", ""),
            IndexCacheService.ComposeKey("canonical", "x"));

        // …and from a different token whose name absorbs the sig characters.
        Assert.NotEqual(
            IndexCacheService.ComposeKey("user:a", ""),
            IndexCacheService.ComposeKey("user:", "a"));

        // Identical pairs still map to the same key (no false NEGATIVES either).
        Assert.Equal(
            IndexCacheService.ComposeKey("canonical", "deadbeefdeadbeef"),
            IndexCacheService.ComposeKey("canonical", "deadbeefdeadbeef"));
    }
}
