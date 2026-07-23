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
/// PR-N1: nav content-gate + incremental refresh (SPEC §1.3, §3.2, §8 PR-N1).
///
/// Covers <see cref="IndexCacheService.RefreshAsync"/> — the content-gated
/// incremental refresh that replaced the all-or-nothing git-HEAD gate — plus the
/// structural <see cref="IndexCacheService.TryLoadAsync"/> load gate.
///
/// Recomputes are counted with a spy <see cref="ITranslationStatusService"/> that
/// wraps the real <see cref="TranslationStatusService"/> so statuses are genuine
/// while every <c>ComputeStatusForPairLive</c> call is tallied. A full rebuild
/// touches every entry (CallCount == fileCount); a correct incremental touches
/// only genuinely-changed entries.
///
/// BLOCKING audit (SPEC §1.3): <c>ComputeStatusForPairLive</c> →
/// <c>TranslationStatusService.ComputeStatus(origPath, tranPath, …)</c> reads ONLY
/// the original and translated file bytes (the <c>root</c>/<c>relKey</c> args are
/// log-only). Nav status is therefore a pure function of the (orig, tran) pair —
/// there is no community-note / QA / termbase / config input. The audit conclusion
/// is pinned by <see cref="StatusInputSurface_IsPureFunctionOfOrigTranPair"/>
/// (unrelated sidecars ⇒ zero recompute) plus the positive orig/tran-edit tests.
///
/// All fixtures use real temp dirs (BundleSeedTests pattern) and fixed timestamp
/// anchors so nothing depends on the wall clock or filesystem enumeration order.
/// </summary>
public class NavIncrementalTests : IDisposable
{
    private readonly string _root;
    private readonly string _origDir;
    private readonly string _tranDir;

    // Original: Chinese only.
    private const string OrigXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>禪宗祖師傳法心印無門關</p>\n" +
        "</body></text></TEI>\n";

    // Fully translated: no CJK remaining → Green.
    private const string GreenXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>The gateless gate of the ancestors transmitting the mind seal.</p>\n" +
        "</body></text></TEI>\n";

    // Fixed anchors — deterministic, independent of the wall clock. Build-time
    // files use BaseAnchor; a "changed" mutation stamps FutureAnchor (guaranteed
    // distinct); the clone simulation uses CloneAnchor.
    private static readonly DateTime BaseAnchor = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FutureAnchor = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CloneAnchor = new(2031, 3, 3, 3, 3, 3, DateTimeKind.Utc);

    public NavIncrementalTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-nav-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Counts every status recompute and records which relKeys were touched.</summary>
    private sealed class CountingStatusService : ITranslationStatusService
    {
        private readonly ITranslationStatusService _inner = new TranslationStatusService();
        private int _callCount;
        public int CallCount => Volatile.Read(ref _callCount);
        public readonly List<string> ComputedRelKeys = new();

        public TranslationStatus ComputeStatusForPairLive(
            string origAbs, string tranAbs, string rootForLogs, string relKeyForLogs, bool verboseLog = true)
        {
            Interlocked.Increment(ref _callCount);
            lock (ComputedRelKeys) ComputedRelKeys.Add(relKeyForLogs);
            return _inner.ComputeStatusForPairLive(origAbs, tranAbs, rootForLogs, relKeyForLogs, verboseLog);
        }

        public void Reset()
        {
            Volatile.Write(ref _callCount, 0);
            lock (ComputedRelKeys) ComputedRelKeys.Clear();
        }
    }

    /// <summary>Synchronous IProgress so reports are captured deterministically
    /// (RefreshAsync calls Report inline; a Progress&lt;T&gt; would post async).</summary>
    private sealed class SyncProgress : IProgress<(int done, int total)>
    {
        public readonly List<(int done, int total)> Reports = new();
        public void Report((int done, int total) value) => Reports.Add(value);
    }

    private static void WriteFile(string path, string content, DateTime mtimeUtc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, mtimeUtc);
    }

    private string OrigPath(string rel) => Path.Combine(_origDir, rel);
    private string TranPath(string rel) => Path.Combine(_tranDir, rel);
    private static string Rel(int i) => $"t{i:D4}.xml";
    private string CommunityPath(string user, string rel)
        => Path.Combine(_root, "community", "translations", user, rel);

    /// <summary>Creates n Green (orig=Chinese, tran=English) pairs stamped BaseAnchor.</summary>
    private void MakeGreenCorpus(int n)
    {
        for (int i = 0; i < n; i++)
        {
            WriteFile(OrigPath(Rel(i)), OrigXml, BaseAnchor);
            WriteFile(TranPath(Rel(i)), GreenXml, BaseAnchor);
        }
    }

    private async Task<(IndexCacheService svc, CountingStatusService spy, IndexCache loaded)> BuildAndLoadAsync()
    {
        var spy = new CountingStatusService();
        var svc = new IndexCacheService(spy);
        var built = await svc.BuildAsync(_origDir, _tranDir, _root);
        await svc.SaveAsync(_root, built);
        var loaded = await svc.TryLoadAsync(_root);
        Assert.NotNull(loaded);
        spy.Reset(); // ignore the build's recomputes; count only the refresh
        return (svc, spy, loaded!);
    }

    private static FileNavItem Entry(IndexCache cache, string rel)
        => cache.Entries.Single(e => string.Equals(
            e.RelPath.Replace('\\', '/'), rel.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

    // ============================================================ happy path

    /// <summary>
    /// The headline PR-N1 guarantee: an unrelated commit (nothing on disk changed)
    /// recomputes ZERO entries and does not even re-save the cache. The old git-HEAD
    /// gate would have discarded the whole cache and rescanned all files.
    /// </summary>
    [Fact]
    public async Task UnchangedCorpus_ZeroRecomputes_AndNoResave()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        var cachePath = svc.GetCachePath(_root);
        var bytesBefore = File.ReadAllBytes(cachePath);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, spy.CallCount);
        Assert.Equal(3, refreshed.Entries.Count);
        // Nothing changed ⇒ no save ⇒ on-disk bytes identical.
        Assert.Equal(bytesBefore, File.ReadAllBytes(cachePath));
    }

    /// <summary>
    /// A git pull touching K translations recomputes EXACTLY those K entries — and
    /// exactly those relKeys — leaving the rest reused untouched.
    /// </summary>
    [Fact]
    public async Task PullTouchingKFiles_RecomputesExactlyK()
    {
        MakeGreenCorpus(5);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        // Touch translations 1 and 3: different content (size) + a distinct mtime.
        WriteFile(TranPath(Rel(1)), GreenXml + "<!-- edit -->\n", FutureAnchor);
        WriteFile(TranPath(Rel(3)), GreenXml + "<!-- edit -->\n", FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(2, spy.CallCount);
        Assert.Equal(new[] { Rel(1), Rel(3) }, spy.ComputedRelKeys.OrderBy(x => x).ToArray());
        Assert.Equal(5, refreshed.Entries.Count);
    }

    /// <summary>Progress is reported over the recompute set only — an ordinary launch
    /// shows "K/K", never the full corpus size (SPEC §3.2).</summary>
    [Fact]
    public async Task Progress_ReportsOverRecomputeSetOnly()
    {
        MakeGreenCorpus(6);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        WriteFile(TranPath(Rel(2)), GreenXml + "<!-- a -->\n", FutureAnchor);
        WriteFile(TranPath(Rel(5)), GreenXml + "<!-- b -->\n", FutureAnchor);

        var progress = new SyncProgress();
        await svc.RefreshAsync(loaded, _origDir, _tranDir, _root, progress);

        Assert.Equal(2, progress.Reports.Count);                 // one per recompute, not 6
        Assert.All(progress.Reports, r => Assert.Equal(2, r.total));
        Assert.Equal((2, 2), progress.Reports[^1]);
    }

    [Fact]
    public async Task UnchangedCorpus_NoRecomputes_ReportsNothing()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        var progress = new SyncProgress();
        await svc.RefreshAsync(loaded, _origDir, _tranDir, _root, progress);

        Assert.Empty(progress.Reports);
    }

    // ============================================================ add / remove

    [Fact]
    public async Task AddedOriginal_GetsEntry_RecomputedOnce()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        // A newly-synced original (with translation) appears.
        WriteFile(OrigPath(Rel(9)), OrigXml, FutureAnchor);
        WriteFile(TranPath(Rel(9)), GreenXml, FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, spy.CallCount);                          // only the added file
        Assert.Equal(new[] { Rel(9) }, spy.ComputedRelKeys.ToArray());
        Assert.Equal(4, refreshed.Entries.Count);
        Assert.Equal(TranslationStatus.Green, Entry(refreshed, Rel(9)).Status);

        // Persisted: a follow-up structural load sees 4 entries.
        var reloaded = await svc.TryLoadAsync(_root);
        Assert.Equal(4, reloaded!.Entries.Count);
    }

    [Fact]
    public async Task RemovedOriginal_Dropped_NoRecompute()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        File.Delete(OrigPath(Rel(1)));

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, spy.CallCount);                          // removals aren't recomputed
        Assert.Equal(2, refreshed.Entries.Count);
        Assert.DoesNotContain(refreshed.Entries, e =>
            string.Equals(e.RelPath.Replace('\\', '/'), Rel(1), StringComparison.OrdinalIgnoreCase));

        // Count drift forces a save.
        var reloaded = await svc.TryLoadAsync(_root);
        Assert.Equal(2, reloaded!.Entries.Count);
    }

    // ============================================================ structural gate → full rebuild

    /// <summary>A titles.jsonl edit changes display fields for every entry, so the
    /// whole cache is discarded and fully rebuilt (all entries recomputed).</summary>
    [Fact]
    public async Task TitlesJsonlEdit_TriggersFullRebuild()
    {
        WriteFile(Path.Combine(_root, "titles.jsonl"),
            "{\"path\":\"t0000.xml\",\"en\":\"One\",\"zh\":\"一\",\"enShort\":\"One\"}\n", BaseAnchor);
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        // Edit titles → TitlesHash flips → structural gate fails.
        WriteFile(Path.Combine(_root, "titles.jsonl"),
            "{\"path\":\"t0000.xml\",\"en\":\"One EDITED\",\"zh\":\"一\",\"enShort\":\"One\"}\n", FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(3, spy.CallCount);                          // full rebuild = every entry
        Assert.Equal(3, refreshed.Entries.Count);
    }

    /// <summary>titles.jsonl appearing (was absent at build, "no-titles" sentinel)
    /// also flips the hash and forces a full rebuild.</summary>
    [Fact]
    public async Task TitlesJsonlAppears_TriggersFullRebuild()
    {
        MakeGreenCorpus(2);                                       // no titles.jsonl yet
        var (svc, spy, loaded) = await BuildAndLoadAsync();
        Assert.Equal("no-titles", loaded.TitlesHash);

        WriteFile(Path.Combine(_root, "titles.jsonl"),
            "{\"path\":\"t0000.xml\",\"enShort\":\"One\"}\n", FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(2, spy.CallCount);
        Assert.NotEqual("no-titles", refreshed.TitlesHash);
    }

    /// <summary>An old-nav-guid cache (pre-v5 content gate) is not stat-comparable, so
    /// RefreshAsync discards it and rebuilds from scratch (SPEC §8: GuidV4Cache).</summary>
    [Fact]
    public async Task LegacyNavGuidCache_TriggersFullRebuild()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        // Simulate a cache from a prior nav build guid: everything else valid.
        loaded.BuildGuid = "phase4-nav-v4-legacy";

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(3, spy.CallCount);                          // full rebuild
        Assert.Equal("phase4-nav-v5-content-gate", refreshed.BuildGuid); // rebuilt at current guid
    }

    [Fact]
    public async Task OlderVersionCache_TriggersFullRebuild()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        loaded.Version = 3; // pre-v4: lacks the per-entry stat fields

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(3, spy.CallCount);
        Assert.Equal(4, refreshed.Version);
    }

    /// <summary>An empty old cache cannot be refreshed incrementally — full rebuild.</summary>
    [Fact]
    public async Task EmptyOldCache_TriggersFullRebuild()
    {
        MakeGreenCorpus(2);
        var spy = new CountingStatusService();
        var svc = new IndexCacheService(spy);

        var empty = new IndexCache
        {
            Version = 4,
            RootPath = _root,
            BuildGuid = "phase4-nav-v5-content-gate",
            TitlesHash = "no-titles",
            Entries = new List<FileNavItem>(),
        };

        var refreshed = await svc.RefreshAsync(empty, _origDir, _tranDir, _root);

        Assert.Equal(2, spy.CallCount);
        Assert.Equal(2, refreshed.Entries.Count);
    }

    // ============================================================ TryLoadAsync structural load gate

    [Fact]
    public async Task TryLoad_RejectsOlderVersion()
    {
        MakeGreenCorpus(2);
        var svc = new IndexCacheService(new CountingStatusService());
        var built = await svc.BuildAsync(_origDir, _tranDir, _root);
        await svc.SaveAsync(_root, built);

        // Downgrade Version on disk.
        var path = svc.GetCachePath(_root);
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        node["Version"] = 3;
        File.WriteAllText(path, node.ToJsonString());

        Assert.Null(await svc.TryLoadAsync(_root));
    }

    [Fact]
    public async Task TryLoad_RejectsMismatchedBuildGuid()
    {
        MakeGreenCorpus(2);
        var svc = new IndexCacheService(new CountingStatusService());
        var built = await svc.BuildAsync(_origDir, _tranDir, _root);
        await svc.SaveAsync(_root, built);

        var path = svc.GetCachePath(_root);
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        node["BuildGuid"] = "some-old-guid";
        File.WriteAllText(path, node.ToJsonString());

        Assert.Null(await svc.TryLoadAsync(_root));
    }

    [Fact]
    public async Task TryLoad_RejectsRootPathMismatch()
    {
        MakeGreenCorpus(2);
        var svc = new IndexCacheService(new CountingStatusService());
        var built = await svc.BuildAsync(_origDir, _tranDir, _root);
        await svc.SaveAsync(_root, built);

        var path = svc.GetCachePath(_root);
        var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        node["RootPath"] = Path.Combine(_root, "somewhere-else");
        File.WriteAllText(path, node.ToJsonString());

        Assert.Null(await svc.TryLoadAsync(_root));
    }

    // ============================================================ clone / mtime-only

    /// <summary>
    /// Clone simulation (SPEC §8: MtimeOnlyRewrite): git rewrites every working-tree
    /// mtime with content unchanged. The first refresh recomputes all entries (ticks
    /// differ) but STATUSES are unchanged and the cache is re-saved with local ticks;
    /// the SECOND refresh then recomputes zero — steady state restored.
    /// </summary>
    [Fact]
    public async Task MtimeOnlyRewrite_RecomputesButKeepsStatuses_ThenSecondRefreshIsZero()
    {
        MakeGreenCorpus(4);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        var before = loaded.Entries.ToDictionary(
            e => e.RelPath.Replace('\\', '/'), e => e.Status, StringComparer.OrdinalIgnoreCase);

        // Rewrite every file's mtime forward, content identical (clone / checkout).
        for (int i = 0; i < 4; i++)
        {
            File.SetLastWriteTimeUtc(OrigPath(Rel(i)), CloneAnchor);
            File.SetLastWriteTimeUtc(TranPath(Rel(i)), CloneAnchor);
        }

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(4, spy.CallCount);                          // all recompute (ticks moved)
        foreach (var e in refreshed.Entries)                     // …but statuses unchanged
            Assert.Equal(before[e.RelPath.Replace('\\', '/')], e.Status);

        // Second launch: cache now carries local ticks → stat-only hits → zero work.
        spy.Reset();
        var reloaded = await svc.TryLoadAsync(_root);
        var again = await svc.RefreshAsync(reloaded!, _origDir, _tranDir, _root);
        Assert.Equal(0, spy.CallCount);
        Assert.Equal(4, again.Entries.Count);
    }

    // ============================================================ false-fresh guards

    /// <summary>The realistic edit channel: content changes WITH a size + mtime change
    /// ⇒ recompute fires and the status flips (Red→Green here). This is the case the
    /// content gate must catch (SPEC §8 false-fresh guard).</summary>
    [Fact]
    public async Task ContentEditWithSizeAndMtimeChange_Recomputes_AndFlipsStatus()
    {
        // Start "untranslated": tran is byte-identical to orig ⇒ Red.
        WriteFile(OrigPath(Rel(0)), OrigXml, BaseAnchor);
        WriteFile(TranPath(Rel(0)), OrigXml, BaseAnchor);
        var (svc, spy, loaded) = await BuildAndLoadAsync();
        Assert.Equal(TranslationStatus.Red, Entry(loaded, Rel(0)).Status);

        // Real translation lands: different bytes, different size, new mtime.
        WriteFile(TranPath(Rel(0)), GreenXml, FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, spy.CallCount);
        Assert.Equal(TranslationStatus.Green, Entry(refreshed, Rel(0)).Status);
    }

    /// <summary>The original side is a reuse input too: an original edit (size + mtime)
    /// forces a recompute even when the translation is untouched.</summary>
    [Fact]
    public async Task OriginalEditWithSizeAndMtimeChange_Recomputes()
    {
        MakeGreenCorpus(2);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        WriteFile(OrigPath(Rel(0)), OrigXml.Replace("</body>", "<p>增訂</p></body>"), FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, spy.CallCount);
        Assert.Equal(new[] { Rel(0) }, spy.ComputedRelKeys.ToArray());
    }

    /// <summary>
    /// The ACCEPTED blind spot (SPEC §1.3, §9): a content change that keeps BOTH size
    /// and mtime identical is invisible to the nav content gate — it reuses the stored
    /// entry. This is documented as not-a-real-scenario (git changes mtime on any real
    /// edit). Pinned so the trade-off is explicit, not an accident.
    /// </summary>
    [Fact]
    public async Task SameSizeSameMtimeDifferentBytes_NotRecomputed_AcceptedBlindSpot()
    {
        // 14-byte translation, stamped at a known tick.
        WriteFile(OrigPath(Rel(0)), OrigXml, BaseAnchor);
        WriteFile(TranPath(Rel(0)), "<t>AAAAAAAA</t>", BaseAnchor);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        // Same length, different bytes, mtime forced back to the exact stored tick.
        File.WriteAllText(TranPath(Rel(0)), "<t>BBBBBBBB</t>");
        File.SetLastWriteTimeUtc(TranPath(Rel(0)), BaseAnchor);
        Assert.Equal(new FileInfo(TranPath(Rel(0))).Length, Entry(loaded, Rel(0)).TranSizeBytes);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, spy.CallCount); // blind spot: reused (documented, accepted)
    }

    // ============================================================ community fallback (SPEC §3.2, §4)

    /// <summary>A community translation appearing (canonical still absent) flips the
    /// resolved path and forces a recompute.</summary>
    [Fact]
    public async Task CommunityTranslationAppears_Recomputes()
    {
        // Original with NO canonical translation ⇒ resolved = canonical (absent), Red.
        WriteFile(OrigPath(Rel(0)), OrigXml, BaseAnchor);
        var (svc, spy, loaded) = await BuildAndLoadAsync();
        Assert.Equal(TranslationStatus.Red, Entry(loaded, Rel(0)).Status);
        Assert.Equal(TranPath(Rel(0)), Entry(loaded, Rel(0)).TranResolvedPath);

        // A community contributor's translation shows up.
        WriteFile(CommunityPath("alice", Rel(0)), GreenXml, FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, spy.CallCount);
        var e = Entry(refreshed, Rel(0));
        Assert.Equal(TranslationStatus.Green, e.Status);
        Assert.Equal(CommunityPath("alice", Rel(0)), e.TranResolvedPath);
    }

    /// <summary>A community translation disappearing flips the resolved path back to the
    /// (absent) canonical path and recomputes (status → Red).</summary>
    [Fact]
    public async Task CommunityTranslationDisappears_Recomputes()
    {
        WriteFile(OrigPath(Rel(0)), OrigXml, BaseAnchor);
        WriteFile(CommunityPath("bob", Rel(0)), GreenXml, BaseAnchor);
        var (svc, spy, loaded) = await BuildAndLoadAsync();
        Assert.Equal(TranslationStatus.Green, Entry(loaded, Rel(0)).Status);
        Assert.Equal(CommunityPath("bob", Rel(0)), Entry(loaded, Rel(0)).TranResolvedPath);

        File.Delete(CommunityPath("bob", Rel(0)));

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, spy.CallCount);
        var e = Entry(refreshed, Rel(0));
        Assert.Equal(TranslationStatus.Red, e.Status);
        Assert.Equal(TranPath(Rel(0)), e.TranResolvedPath);
    }

    /// <summary>When a canonical translation appears over an existing community one, the
    /// resolved path flips from community → canonical (canonical wins) and recomputes.</summary>
    [Fact]
    public async Task CanonicalTranslationTakesPrecedenceOverCommunity_Recomputes()
    {
        WriteFile(OrigPath(Rel(0)), OrigXml, BaseAnchor);
        WriteFile(CommunityPath("carol", Rel(0)), GreenXml, BaseAnchor);
        var (svc, spy, loaded) = await BuildAndLoadAsync();
        Assert.Equal(CommunityPath("carol", Rel(0)), Entry(loaded, Rel(0)).TranResolvedPath);

        // Canonical xml-p5t translation now exists → it wins the resolution.
        WriteFile(TranPath(Rel(0)), GreenXml, FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, spy.CallCount);
        Assert.Equal(TranPath(Rel(0)), Entry(refreshed, Rel(0)).TranResolvedPath);
    }

    // ============================================================ BLOCKING audit pin (SPEC §1.3)

    /// <summary>
    /// BLOCKING input-surface audit (SPEC §1.3): nav status is a pure function of the
    /// (orig, tran) file pair. Introducing unrelated inputs — a review/QA sidecar, a
    /// termbase, config.json, and a community *note* (distinct from a community
    /// *translation*) — recomputes ZERO entries, proving none of them feed status. The
    /// positive direction (orig/tran edits DO recompute) is pinned by the edit tests
    /// above. If ComputeStatusForPairLive ever grows a hidden input, this test fires.
    /// </summary>
    [Fact]
    public async Task StatusInputSurface_IsPureFunctionOfOrigTranPair()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        // Sidecars that are NOT the (orig, tran) pair and must not affect status.
        WriteFile(Path.Combine(_root, "config.json"), "{\"x\":1}", FutureAnchor);
        WriteFile(Path.Combine(_root, "termbase.v2.json"), "{\"terms\":[]}", FutureAnchor);
        WriteFile(Path.Combine(_root, "review", "qa.jsonl"), "{\"note\":\"q\"}\n", FutureAnchor);
        // A community NOTE (community/notes/…), explicitly not community/translations/…
        WriteFile(Path.Combine(_root, "community", "notes", "dave", Rel(0)), "<note/>", FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, spy.CallCount);
        Assert.Equal(3, refreshed.Entries.Count);
    }

    // ============================================================ round-trip integrity

    /// <summary>After a refresh that recomputed a delta and saved, a follow-up refresh
    /// with no further change recomputes zero — the persisted per-entry stats are
    /// self-consistent (no perpetual-recompute bug).</summary>
    [Fact]
    public async Task RefreshPersistsStats_SubsequentRefreshIsZero()
    {
        MakeGreenCorpus(4);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        WriteFile(TranPath(Rel(2)), GreenXml + "<!-- edit -->\n", FutureAnchor);
        await svc.RefreshAsync(loaded, _origDir, _tranDir, _root); // recomputes 1, saves

        spy.Reset();
        var reloaded = await svc.TryLoadAsync(_root);
        Assert.NotNull(reloaded);
        var again = await svc.RefreshAsync(reloaded!, _origDir, _tranDir, _root);

        Assert.Equal(0, spy.CallCount);
        Assert.Equal(4, again.Entries.Count);
    }

    // ============================================================ cancellation

    [Fact]
    public async Task Refresh_HonorsCancellation()
    {
        MakeGreenCorpus(3);
        var (svc, spy, loaded) = await BuildAndLoadAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.RefreshAsync(loaded, _origDir, _tranDir, _root, null, cts.Token));
    }
}
