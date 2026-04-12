using System;
using System.IO;
using System.Threading.Tasks;
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

    [Fact]
    public async Task IsStaleAsync_ReturnsFalseWhenAllFilesOlderThanManifest()
    {
        var svc = new SearchIndexService();

        // Create an XML file with old timestamp
        var xmlFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, DateTime.UtcNow.AddHours(-2));

        // Build the index (creates manifest)
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Manifest should now be newer than the XML file
        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.False(stale);
    }

    [Fact]
    public async Task IsStaleAsync_ReturnsTrueWhenOneTranslatedFileNewerThanManifest()
    {
        var svc = new SearchIndexService();

        // Create initial XML files
        var origFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(origFile, "<x/>");
        File.SetLastWriteTimeUtc(origFile, DateTime.UtcNow.AddHours(-2));

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddHours(-2));

        // Build the index
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Now touch the translated XML file to be newer than the manifest
        var manifestPath = svc.GetManifestPath(_tempRoot);
        var manifestTime = File.GetLastWriteTimeUtc(manifestPath);
        File.SetLastWriteTimeUtc(tranFile, manifestTime.AddSeconds(5));

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
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

    // ===== IndexCacheService.TryGetGitHead + GitHead invalidation =====
    //
    // Regression coverage for the "user synced new files but the desktop
    // app's nav cache is stale and hides them" bug. The fix is to snapshot
    // the corpus translations repo's HEAD SHA into IndexCache.GitHead at
    // build time and gate TryLoadAsync on it.

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
    public async Task TryLoadAsync_RebuildsWhenGitHeadHasMoved()
    {
        // Setup: a real corpus layout under _tempRoot with a fake .git/HEAD,
        // build the cache once at HEAD-A, then rewrite .git/HEAD to HEAD-B
        // and confirm the loader returns null instead of the stale cache.
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

        // First load — HEAD unchanged — should hit the cache.
        var fresh = await svc.TryLoadAsync(repoRoot);
        Assert.NotNull(fresh);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", fresh!.GitHead);

        // Move HEAD (simulate sync that pulled new commits).
        File.WriteAllText(
            Path.Combine(repoRoot, ".git", "refs", "heads", "main"),
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n");

        var stale = await svc.TryLoadAsync(repoRoot);

        Assert.Null(stale);
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
    public async Task TryLoadAsync_DoesNotGate_WhenCachedHeadIsNullFromOlderBuild()
    {
        // A cache built before the GitHead field existed will deserialize
        // with GitHead == null. Loading it inside a real git repo with a
        // valid HEAD must NOT throw it away — the field is for forward
        // compatibility, not a forced re-bake on first launch after upgrade.
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
        // Force the GitHead back to null on the saved file to simulate a
        // legacy cache from an older app version.
        built.GitHead = null;
        await svc.SaveAsync(repoRoot, built);
        // SaveAsync re-stamps GitHead from the live repo, so we have to
        // overwrite the on-disk file directly.
        var cachePath = svc.GetCachePath(repoRoot);
        var json = File.ReadAllText(cachePath);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"GitHead\":\\s*\"[^\"]*\"", "\"GitHead\": null");
        File.WriteAllText(cachePath, json);

        var loaded = await svc.TryLoadAsync(repoRoot);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.GitHead);
        Assert.Single(loaded.Entries);
    }
}
