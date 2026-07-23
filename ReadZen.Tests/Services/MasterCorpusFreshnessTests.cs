using System;
using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Staleness contract for the master-corpus index cache (audit P4.6): the cache
/// records a corpus stat-stamp at build time; TryLoadAsync with a freshness root
/// refuses caches whose stamp no longer matches the live corpus (and legacy caches
/// with no stamp), so the auto-build path rebuilds them.
/// </summary>
public class MasterCorpusFreshnessTests : IDisposable
{
    private readonly string _root;
    private readonly string _cacheDir;

    public MasterCorpusFreshnessTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "rz-corpusfresh-" + Guid.NewGuid().ToString("N")[..8]);
        // AppPaths.DiscoverAllCorpora requires the two-repo layout: an originals
        // subdir (xml-p5) AND a separate translations subdir (xml-p5t).
        Directory.CreateDirectory(Path.Combine(_root, "CbetaZenTexts", "xml-p5", "T"));
        Directory.CreateDirectory(Path.Combine(_root, "CbetaZenTranslations", "xml-p5t"));
        File.WriteAllText(SourceFile("a.xml"), "<TEI/>");
        _cacheDir = MasterCorpusSearchService.GetCacheDir(_root);
        Directory.CreateDirectory(_cacheDir);
    }

    private string SourceFile(string name) =>
        Path.Combine(_root, "CbetaZenTexts", "xml-p5", "T", name);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private async Task WriteCacheAsync(string? stamp)
    {
        var index = new MasterCorpusIndex { BuiltUtc = "2026-01-01", FileCount = 1, CorpusStamp = stamp };
        await new MasterCorpusSearchService().SaveAsync(_cacheDir, index);
    }

    [Fact]
    public async Task FreshCache_Loads()
    {
        // v2 contract: a real cache stores the COMPOSITE stamp (corpus half + roster half),
        // and the freshness compare reconstructs it from the live corpus + the caller-supplied
        // rosterIdentity. A cache written with the composite and loaded with the matching
        // roster identity must read fresh.
        var catalog = new ZenMasterCatalog();
        var stamp = MasterCorpusSearchService.ComputeCompositeStamp(_root, catalog);
        Assert.NotNull(stamp); // sanity: the fake corpus layout was discovered
        await WriteCacheAsync(stamp);

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(
            _cacheDir, default,
            parentRootForFreshness: _root,
            rosterIdentity: MasterCorpusSearchService.ComputeRosterIdentity(catalog));

        Assert.NotNull(loaded);
    }

    [Fact]
    public async Task CorpusChange_MakesCacheStale()
    {
        await WriteCacheAsync(MasterCorpusSearchService.ComputeCorpusStamp(_root));

        File.WriteAllText(SourceFile("b.xml"), "<TEI/>"); // corpus grew

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(_cacheDir, default, parentRootForFreshness: _root);

        Assert.Null(loaded); // stale → caller rebuilds
    }

    [Fact]
    public async Task LegacyUnstampedCache_IsStale_WhenFreshnessRequested()
    {
        await WriteCacheAsync(stamp: null);

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(_cacheDir, default, parentRootForFreshness: _root);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task NoFreshnessRoot_LoadsRegardless_LegacyBehavior()
    {
        await WriteCacheAsync(stamp: null);

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(_cacheDir);

        Assert.NotNull(loaded); // the display-only call site keeps working
    }
}
