using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for SearchIndexService.IsStaleAsync and TranslationAssistantBuildService.IsReferenceStaleAsync.
/// Uses real file system via temp directories.
/// </summary>
public class IndexStalenessTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;
    private readonly string _tranDir;

    public IndexStalenessTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-test-" + Guid.NewGuid().ToString("N")[..8]);
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

    // ===== SearchIndexService.IsStaleAsync =====

    [Fact]
    public async Task IsStaleAsync_ReturnsTrueWhenManifestMissing()
    {
        // No manifest file exists at all
        var svc = new SearchIndexService();

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    [Fact] // FL8 re-pin: a fresh SPLIT index (origin + overlay) over an unchanged corpus is not
           // stale. (A plain combined BuildAsync is now migration-owed on the launch probe — the
           // eager flip — so the "fresh index not stale" contract is asserted on the split reality.)
    public async Task IsStaleAsync_FreshSplitIndex_NotStale()
    {
        var svc = new SearchIndexService();

        var xmlFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, DateTime.UtcNow.AddHours(-2));

        await svc.BuildOriginLayerAsync(_tempRoot, _origDir);
        await svc.BuildOverlayLayerAsync(_tempRoot, new[] { _tranDir });

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.False(stale);
    }

    [Fact]
    public async Task IsStaleAsync_LegacyMtimePath_ReturnsTrueWhenOneTranslatedFileNewerThanManifest()
    {
        // This test exercises the LEGACY mtime path — manifests without InputHash. After
        // Wave 5 the hash basis is content-only, so a bare mtime bump on an otherwise
        // unchanged file does NOT trigger a rebuild via the hash path (that's the spec).
        // To validate the fallback we strip InputHash from the manifest after BuildAsync
        // writes it, mimicking an upgrade from a pre-PR3 binary.
        var svc = new SearchIndexService();

        var origFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(origFile, "<x/>");
        File.SetLastWriteTimeUtc(origFile, DateTime.UtcNow.AddHours(-2));

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddHours(-2));

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Force the legacy mtime path: strip the hash field from the manifest JSON.
        var manifestPath = svc.GetManifestPath(_tempRoot);
        StripInputHashFromManifest(manifestPath);

        // Now touch the translated XML file to be newer than the manifest's mtime.
        var manifestTime = File.GetLastWriteTimeUtc(manifestPath);
        File.SetLastWriteTimeUtc(tranFile, manifestTime.AddSeconds(5));

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    // ===== SearchIndexService.IsStaleAsync hash path (PR3) =====

    [Fact]
    public async Task IsStaleAsync_SameContent_NewerMtimes_ReturnsFalse()
    {
        // This is the spec criterion (Wave 5 fix): `git pull` / `git checkout` that bumps
        // file mtimes without changing file content must NOT trigger a rebuild.
        // The hash basis is content-based (SHA256 of bytes), so identical bytes after
        // any mtime mutation yield the same root hash.
        var svc = new SearchIndexService();

        // A non-empty origin corpus (an empty origin dir is treated as build-owed, not "fresh").
        var origFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(origFile, "<x>origin</x>");
        File.SetLastWriteTimeUtc(origFile, DateTime.UtcNow.AddHours(-2));

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddHours(-2));

        // FL8: build the SPLIT family; mtime immunity is a content-hash property of both layers.
        await svc.BuildOriginLayerAsync(_tempRoot, _origDir);
        await svc.BuildOverlayLayerAsync(_tempRoot, new[] { _tranDir });

        // Simulate a git pull: file content unchanged, mtime bumped forward.
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddMinutes(5));

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.False(stale);
    }

    [Fact]
    public async Task IsStaleAsync_DifferentHash_ReturnsTrue()
    {
        // Same file path/length/ticks would all need to differ to bust the hash. The cleanest
        // way: rewrite the file with different content + size, mtime bumps naturally.
        var svc = new SearchIndexService();

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Modify content (changes length -> changes hash).
        File.WriteAllText(tranFile, "<x><different/></x>");

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    [Fact]
    public async Task IsStaleAsync_NullHash_FallsBackToMtimeCheck()
    {
        // Simulate a legacy manifest: build, then strip the InputHash field on disk so the
        // hybrid path takes the legacy (mtime) branch.
        var svc = new SearchIndexService();

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddHours(-2));

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        StripInputHashFromManifest(svc.GetManifestPath(_tempRoot));

        // Now touch the XML to be NEWER than the manifest → legacy mtime path returns true.
        var manifestPath = svc.GetManifestPath(_tempRoot);
        var manifestTime = File.GetLastWriteTimeUtc(manifestPath);
        File.SetLastWriteTimeUtc(tranFile, manifestTime.AddSeconds(5));

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale); // legacy path detected the bumped mtime
    }

    // FL8 RETIRED: IsStaleAsync_NullHash_AllFilesOlder_ReturnsFalse. It asserted the legacy
    // null-InputHash combined-manifest mtime fallback returns "not stale". After the eager flip a
    // legacy combined root is always migration-owed (IsStaleAsync → NeedsMigration → true), so a
    // "null-hash combined root is not stale" verdict no longer exists — the path is dead.

    [Fact]
    public async Task IsStaleAsync_NullHash_OneFileNewer_ReturnsTrue()
    {
        var svc = new SearchIndexService();

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });
        StripInputHashFromManifest(svc.GetManifestPath(_tempRoot));

        // Force the file mtime past the manifest mtime.
        var manifestPath = svc.GetManifestPath(_tempRoot);
        File.SetLastWriteTimeUtc(tranFile, File.GetLastWriteTimeUtc(manifestPath).AddSeconds(5));

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    [Fact]
    public async Task ComputeInputHash_OrderingDeterministic()
    {
        // Write the same file set twice (clean temp dir between) and confirm the helper
        // returns identical hashes regardless of OS enumeration order.
        var dirA = Path.Combine(_tempRoot, "A");
        var dirB = Path.Combine(_tempRoot, "B");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);

        // Identical content, identical filenames — but written in different orders so the
        // OS enumeration may differ. The sort in ComputeInputHashAsync should equalize them.
        var paths = new[] { "zz.xml", "aa.xml", "mm.xml" };
        var anchorUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var p in paths)
        {
            File.WriteAllText(Path.Combine(dirA, p), "<x/>");
            File.SetLastWriteTimeUtc(Path.Combine(dirA, p), anchorUtc);
        }
        // Write to B in reverse order to perturb OS enumeration.
        for (int i = paths.Length - 1; i >= 0; i--)
        {
            File.WriteAllText(Path.Combine(dirB, paths[i]), "<x/>");
            File.SetLastWriteTimeUtc(Path.Combine(dirB, paths[i]), anchorUtc);
        }

        // ComputeInputHashAsync namespace-prefixes the relative path with "orig" / "tran0"
        // etc., so to get identical hashes we must pass the same dir as originalDir in both
        // calls. Compare hash(dirA) to hash(dirB) — they share rel paths and metadata.
        var hashA = await SearchIndexService.ComputeInputHashAsync(dirA, Array.Empty<string>(), CancellationToken.None);
        var hashB = await SearchIndexService.ComputeInputHashAsync(dirB, Array.Empty<string>(), CancellationToken.None);

        Assert.Equal(hashA, hashB);
        Assert.Equal(64, hashA.Length); // SHA256 hex
    }

    [Fact]
    public async Task ComputeInputHash_DetectsContentChange()
    {
        var dir = Path.Combine(_tempRoot, "C");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "test.xml");
        File.WriteAllText(file, "<x/>");

        var hashBefore = await SearchIndexService.ComputeInputHashAsync(dir, Array.Empty<string>(), CancellationToken.None);

        // Change file size + (naturally) mtime.
        File.WriteAllText(file, "<x><much-larger-content/></x>");

        var hashAfter = await SearchIndexService.ComputeInputHashAsync(dir, Array.Empty<string>(), CancellationToken.None);

        Assert.NotEqual(hashBefore, hashAfter);
    }

    [Fact]
    public async Task ComputeInputHash_PerfBudget_200Files_UnderOneSecond()
    {
        // Soft perf assertion: SPEC budget is ~500ms on a 200-file synthetic corpus.
        // We use 1s as the test-environment-safe ceiling (CI VM, AV scan jitter).
        var dir = Path.Combine(_tempRoot, "perf");
        Directory.CreateDirectory(dir);
        for (int i = 0; i < 200; i++)
            File.WriteAllText(Path.Combine(dir, $"f{i:D4}.xml"), "<x>" + new string('a', 200) + "</x>");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var hash = await SearchIndexService.ComputeInputHashAsync(dir, Array.Empty<string>(), CancellationToken.None);
        sw.Stop();

        Assert.Equal(64, hash.Length);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Hash compute took {sw.ElapsedMilliseconds}ms on 200 files; SPEC budget is 500ms (1s ceiling for test env).");
    }

    // ===== Gap-fill: empty corpus, malformed manifest, content collision =====

    [Fact]
    public async Task ComputeInputHash_EmptyCorpus_IsDeterministicConstant()
    {
        // No XML files in either directory: the hash must still be stable and
        // identical across runs. Locks in that no `DateTime.UtcNow` or other
        // wall-clock state seeps into the hash basis.
        var emptyA = Path.Combine(_tempRoot, "emptyA");
        var emptyB = Path.Combine(_tempRoot, "emptyB");
        Directory.CreateDirectory(emptyA);
        Directory.CreateDirectory(emptyB);

        var hashA = await SearchIndexService.ComputeInputHashAsync(emptyA, Array.Empty<string>(), CancellationToken.None);
        // Small artificial pause to surface any DateTime.UtcNow leakage in the hash.
        await Task.Delay(50);
        var hashB = await SearchIndexService.ComputeInputHashAsync(emptyB, Array.Empty<string>(), CancellationToken.None);

        Assert.Equal(64, hashA.Length);
        Assert.Equal(hashA, hashB); // empty-corpus hash is a constant
    }

    [Fact]
    public async Task IsStaleAsync_EmptyManifestFile_ReturnsTrue_NoThrow()
    {
        // Manifest file exists but is empty (truncated mid-write, disk-full event,
        // power loss). IsStaleAsync must NOT throw — it must return true so a rebuild
        // runs. TryLoadAsync returns null for whitespace-only JSON; the caller treats
        // that as "stale".
        var svc = new SearchIndexService();

        var manifestPath = svc.GetManifestPath(_tempRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(manifestPath, ""); // zero-byte manifest
        // Bin file is also expected; create an empty placeholder so existence checks pass.
        File.WriteAllText(svc.GetBinPath(_tempRoot), "");

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    [Fact]
    public async Task ComputeInputHash_SameTupleDifferentBytes_YieldsDifferentHash()
    {
        // Wave 5 fix: hash basis is now content-based (per-file SHA256). Two files
        // sharing path/length/mtime but differing in CONTENT must produce different
        // hashes — otherwise a CBETA editorial fix (e.g., correcting a typo within
        // an existing <lb> line, preserving file size) would silently leave the
        // search index unrebuilt against the new content.
        var dirA = Path.Combine(_tempRoot, "collA");
        var dirB = Path.Combine(_tempRoot, "collB");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(dirB);

        var anchorUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fileA = Path.Combine(dirA, "test.xml");
        var fileB = Path.Combine(dirB, "test.xml");

        // Same length (15 bytes), same name, same mtime — but different content bytes.
        File.WriteAllText(fileA, "<x>AAAAAAAA</x>");
        File.WriteAllText(fileB, "<x>BBBBBBBB</x>");
        Assert.Equal(new FileInfo(fileA).Length, new FileInfo(fileB).Length);
        File.SetLastWriteTimeUtc(fileA, anchorUtc);
        File.SetLastWriteTimeUtc(fileB, anchorUtc);

        var hashA = await SearchIndexService.ComputeInputHashAsync(dirA, Array.Empty<string>(), CancellationToken.None);
        var hashB = await SearchIndexService.ComputeInputHashAsync(dirB, Array.Empty<string>(), CancellationToken.None);

        // Content-hash basis: different bytes → different root hash.
        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public async Task ComputeInputHash_SameContentDifferentMtime_YieldsSameHash()
    {
        // The headline SPEC criterion for Wave 5: a git pull / git checkout that bumps
        // file mtimes without changing content MUST yield the same hash, so the index
        // is not invalidated.
        var dir = Path.Combine(_tempRoot, "samecontent");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "test.xml");
        File.WriteAllText(file, "<x>same bytes</x>");

        File.SetLastWriteTimeUtc(file, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var hashOld = await SearchIndexService.ComputeInputHashAsync(dir, Array.Empty<string>(), CancellationToken.None);

        // Simulate git pull / checkout: bump mtime forward, do not touch content.
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
        var hashNew = await SearchIndexService.ComputeInputHashAsync(dir, Array.Empty<string>(), CancellationToken.None);

        Assert.Equal(hashOld, hashNew);
    }

    /// <summary>
    /// Rewrites the manifest JSON on disk with <c>"InputHash": null</c> so we can exercise
    /// the legacy (pre-PR3) code path. Simulates upgrade from old binary that didn't write
    /// the field at all.
    /// </summary>
    private static void StripInputHashFromManifest(string manifestPath)
    {
        var json = File.ReadAllText(manifestPath);
        // Replace any value (hex string) with null.
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"InputHash\":\\s*\"[^\"]*\"", "\"InputHash\": null");
        File.WriteAllText(manifestPath, json);
    }

    // ===== TranslationAssistantBuildService.IsReferenceStaleAsync =====

    [Fact]
    public async Task IsReferenceStaleAsync_ReturnsTrueWhenReferenceFileMissing()
    {
        var svc = new TranslationAssistantBuildService();

        // No reference file exists
        bool stale = await svc.IsReferenceStaleAsync(_tempRoot, _tranDir);

        Assert.True(stale);
    }

    [Fact]
    public async Task IsReferenceStaleAsync_ReturnsFalseWhenReferenceNewerThanAllTranslatedFiles()
    {
        var svc = new TranslationAssistantBuildService();

        // Create a translated XML file with old timestamp
        var xmlFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, DateTime.UtcNow.AddHours(-2));

        // Create reference file newer than the XML
        var refPath = Path.Combine(_tempRoot, "translation-memory.reference.jsonl");
        File.WriteAllText(refPath, "{}");
        // Reference is created "now", which is newer than -2h

        bool stale = await svc.IsReferenceStaleAsync(_tempRoot, _tranDir);

        Assert.False(stale);
    }

    [Fact]
    public async Task IsReferenceStaleAsync_ReturnsTrueWhenOneTranslatedFileNewer()
    {
        var svc = new TranslationAssistantBuildService();

        // Create reference file
        var refPath = Path.Combine(_tempRoot, "translation-memory.reference.jsonl");
        File.WriteAllText(refPath, "{}");
        var refTime = File.GetLastWriteTimeUtc(refPath);

        // Create a translated XML file newer than the reference
        var xmlFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, refTime.AddSeconds(5));

        bool stale = await svc.IsReferenceStaleAsync(_tempRoot, _tranDir);

        Assert.True(stale);
    }

    // ===== IndexCacheService.TryGetGitHead (retained low-level helper) =====
    //
    // The git-HEAD *invalidation gate* was REMOVED in the v4/v5 nav content
    // gate (SPEC §4): a commit anywhere no longer discards the whole nav cache.
    // Freshness is now decided by RefreshAsync's content gate (TitlesHash +
    // per-entry file stats — see NavIncrementalTests), and SaveAsync no longer
    // stamps GitHead. TryGetGitHead itself is retained as a helper, so its
    // parsing is still covered below; the two TryLoadAsync tests now pin the
    // NEW behavior — a HEAD move does NOT gate the cache.

    private static void WriteFakeGit(string repoRoot, string headSha)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        var refsDir = Path.Combine(gitDir, "refs", "heads");
        Directory.CreateDirectory(refsDir);
        File.WriteAllText(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(refsDir, "main"), headSha + "\n");
    }

    [Fact]
    public void TryGetGitHead_ResolvesSymbolicHead_ToBranchSha()
    {
        var repo = Path.Combine(_tempRoot, "repo-symbolic");
        Directory.CreateDirectory(repo);
        WriteFakeGit(repo, "deadbeefcafef00d1234567890abcdef12345678");

        var head = IndexCacheService.TryGetGitHead(repo);

        Assert.Equal("deadbeefcafef00d1234567890abcdef12345678", head);
    }

    [Fact]
    public void TryGetGitHead_ResolvesDetachedHead_ToLiteralSha()
    {
        var repo = Path.Combine(_tempRoot, "repo-detached");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        File.WriteAllText(Path.Combine(repo, ".git", "HEAD"), "0123456789abcdef0123456789abcdef01234567\n");

        var head = IndexCacheService.TryGetGitHead(repo);

        Assert.Equal("0123456789abcdef0123456789abcdef01234567", head);
    }

    [Fact]
    public void TryGetGitHead_ResolvesPackedRef_WhenLooseRefMissing()
    {
        var repo = Path.Combine(_tempRoot, "repo-packed");
        var gitDir = Path.Combine(repo, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n");
        // No loose ref file — simulate fresh-clone state where everything is packed.
        File.WriteAllText(Path.Combine(gitDir, "packed-refs"),
            "# pack-refs with: peeled fully-peeled sorted\n" +
            "abcdef1234567890abcdef1234567890abcdef12 refs/heads/main\n" +
            "fedcba9876543210fedcba9876543210fedcba98 refs/tags/v1.0\n");

        var head = IndexCacheService.TryGetGitHead(repo);

        Assert.Equal("abcdef1234567890abcdef1234567890abcdef12", head);
    }

    [Fact]
    public void TryGetGitHead_ReturnsNull_WhenNoGitDir()
    {
        var repo = Path.Combine(_tempRoot, "repo-no-git");
        Directory.CreateDirectory(repo);

        var head = IndexCacheService.TryGetGitHead(repo);

        Assert.Null(head);
    }

    [Fact]
    public void TryGetGitHead_ReturnsNull_WhenHeadFileMissing()
    {
        var repo = Path.Combine(_tempRoot, "repo-no-head");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));

        var head = IndexCacheService.TryGetGitHead(repo);

        Assert.Null(head);
    }

    [Fact]
    public async Task TryLoadAsync_DoesNotRebuild_WhenGitHeadHasMoved()
    {
        // The git-HEAD invalidation gate was REMOVED (SPEC §4): a HEAD move
        // (e.g. a sync that pulled new commits) must NOT discard the nav cache.
        // Freshness is now the content gate's job (RefreshAsync recomputes only
        // the genuinely-changed entries), never a wholesale HEAD-triggered rescan.
        var repoRoot = Path.Combine(_tempRoot, "repo-head-moved");
        Directory.CreateDirectory(Path.Combine(repoRoot, "xml-p5", "T01"));
        Directory.CreateDirectory(Path.Combine(repoRoot, "xml-p5t"));
        WriteFakeGit(repoRoot, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        // One source file so the build produces a non-empty entry list.
        var origFile = Path.Combine(repoRoot, "xml-p5", "T01", "test.xml");
        File.WriteAllText(origFile, "<TEI/>");

        var svc = new IndexCacheService(new TranslationStatusService());
        var built = await svc.BuildAsync(
            Path.Combine(repoRoot, "xml-p5"),
            Path.Combine(repoRoot, "xml-p5t"),
            repoRoot);
        await svc.SaveAsync(repoRoot, built);

        // First load — HEAD unchanged — hits the cache. GitHead is retired:
        // SaveAsync no longer stamps it, so it is null (kept only for JSON tolerance).
        var fresh = await svc.TryLoadAsync(repoRoot);
        Assert.NotNull(fresh);
        Assert.Null(fresh!.GitHead);

        // Move HEAD (simulate sync that pulled new commits).
        File.WriteAllText(
            Path.Combine(repoRoot, ".git", "refs", "heads", "main"),
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n");

        // The cache SURVIVES the HEAD move — no discard, no forced rebuild.
        var afterMove = await svc.TryLoadAsync(repoRoot);
        Assert.NotNull(afterMove);
        Assert.Single(afterMove!.Entries);
    }

    [Fact]
    public async Task TryLoadAsync_DoesNotGate_WhenLiveRepoHasNoGitDir()
    {
        // Cache built in a git repo, then the .git dir disappears (e.g. user
        // moved the corpus into a non-git directory). The loader should NOT
        // throw the cache away — the BuildGuid + RootPath checks remain
        // authoritative when one side has no HEAD signal.
        var repoRoot = Path.Combine(_tempRoot, "repo-git-vanished");
        Directory.CreateDirectory(Path.Combine(repoRoot, "xml-p5", "T01"));
        Directory.CreateDirectory(Path.Combine(repoRoot, "xml-p5t"));
        WriteFakeGit(repoRoot, "1111111111111111111111111111111111111111");
        File.WriteAllText(Path.Combine(repoRoot, "xml-p5", "T01", "test.xml"), "<TEI/>");

        var svc = new IndexCacheService(new TranslationStatusService());
        var built = await svc.BuildAsync(
            Path.Combine(repoRoot, "xml-p5"),
            Path.Combine(repoRoot, "xml-p5t"),
            repoRoot);
        await svc.SaveAsync(repoRoot, built);

        // Nuke the .git dir.
        Directory.Delete(Path.Combine(repoRoot, ".git"), recursive: true);

        var loaded = await svc.TryLoadAsync(repoRoot);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Entries);
    }

    [Fact]
    public async Task TryLoadAsync_DoesNotGate_WhenCachedHeadIsNull()
    {
        // GitHead is retired (SPEC §4): SaveAsync no longer stamps it, so every
        // freshly-saved cache — like a legacy pre-field cache — deserializes with
        // GitHead == null. Loading it inside a real git repo with a valid HEAD
        // must NOT throw it away; the structural gates (BuildGuid + Version +
        // RootPath) are authoritative and the HEAD field is never compared.
        var repoRoot = Path.Combine(_tempRoot, "repo-legacy-cache");
        Directory.CreateDirectory(Path.Combine(repoRoot, "xml-p5", "T01"));
        Directory.CreateDirectory(Path.Combine(repoRoot, "xml-p5t"));
        WriteFakeGit(repoRoot, "2222222222222222222222222222222222222222");
        File.WriteAllText(Path.Combine(repoRoot, "xml-p5", "T01", "test.xml"), "<TEI/>");

        var svc = new IndexCacheService(new TranslationStatusService());
        var built = await svc.BuildAsync(
            Path.Combine(repoRoot, "xml-p5"),
            Path.Combine(repoRoot, "xml-p5t"),
            repoRoot);
        // No manual overwrite needed: SaveAsync leaves GitHead null on disk.
        await svc.SaveAsync(repoRoot, built);

        var loaded = await svc.TryLoadAsync(repoRoot);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.GitHead);
        Assert.Single(loaded.Entries);
    }
}
