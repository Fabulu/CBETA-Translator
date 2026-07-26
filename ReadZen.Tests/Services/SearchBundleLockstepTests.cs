using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL6 (design §6.3): the search-bundle LOCKSTEP contract, in the style of
/// MasterBundleLockstepTests. The release's <c>prebuild-index</c> job bakes the frozen ORIGIN
/// family (<c>--layer origin</c>) into <c>Assets/PrebuiltIndex</c>, then trims the origin text
/// sidecar (the 779 MB trim constraint). The shipped bundle must therefore be EXACTLY the
/// <c>search.origin.*</c> family MINUS text — with a current-guid, <c>LayerRole=="origin"</c>,
/// non-null-<c>OriginHash</c> manifest and the origin corpusfreq sibling, and NO overlay/text
/// files. This test STAGES that bundle the same way CI does (the real bundle bytes are CI-
/// populated / gitignored) and pins the allow/deny shape the release + guid-guard assume.
/// </summary>
public sealed class SearchBundleLockstepTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;

    public SearchBundleLockstepTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-searchlockstep-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_temp);
        _origDir = Path.Combine(_temp, "xml-p5");
        Directory.CreateDirectory(_origDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch { }
    }

    private static string Xml(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        $"<p>{body}</p>\n</body></text></TEI>\n";

    private void WriteOrigin(int n)
    {
        for (int i = 0; i < n; i++)
            File.WriteAllText(Path.Combine(_origDir, $"o{i:D2}.xml"), Xml("趙州狗子佛性" + i));
    }

    /// <summary>Bakes the bundle exactly as release.yml does: build the origin-only family, then
    /// remove the origin text sidecar (the trimmed-bundle constraint).</summary>
    private async Task<string> BakeTrimmedOriginBundleAsync()
    {
        var bundle = Path.Combine(_temp, "PrebuiltIndex");
        Directory.CreateDirectory(bundle);
        using (var svc = new SearchIndexService())
            await svc.BuildOriginLayerAsync(bundle, _origDir);

        // Release trim: origin bundle = search.origin.* MINUS text.
        foreach (var f in Directory.EnumerateFiles(bundle, "search.origin.text.*").ToList())
            File.Delete(f);
        return bundle;
    }

    [Fact]
    public async Task StagedOriginBundle_HasOriginManifestAndCorpusFreq_NoOverlayNoText()
    {
        WriteOrigin(6);
        var bundle = await BakeTrimmedOriginBundleAsync();

        // ── ALLOW list: the origin family CI ships (minus text) ──
        foreach (var required in new[]
        {
            "search.origin.manifest.json",
            "search.origin.bin",
            "search.origin.inverted.bin",
            "search.origin.inverted.bin.paths",
            "search.origin.corpusfreq.bin",
            "search.origin.corpusfreq.manifest.json",
        })
            Assert.True(File.Exists(Path.Combine(bundle, required)), $"origin bundle missing required file: {required}");

        // ── DENY list: no text sidecar, no overlay family, no legacy combined family ──
        Assert.Empty(Directory.EnumerateFiles(bundle, "search.origin.text.*"));
        Assert.Empty(Directory.EnumerateFiles(bundle, "search.overlay.*"));
        Assert.Empty(Directory.EnumerateFiles(bundle, "search.index.*"));
        Assert.Empty(Directory.EnumerateFiles(bundle, "search.text.*"));
        Assert.Empty(Directory.EnumerateFiles(bundle, "search.corpusfreq.*"));   // combined-named
        Assert.Empty(Directory.EnumerateFiles(bundle, "search.gramsets.*"));     // origin emits none

        // ── Manifest identity the guid-bundle-guard + adoption gate rely on ──
        var man = System.Text.Json.JsonSerializer.Deserialize<SearchIndexManifest>(
            File.ReadAllText(Path.Combine(bundle, "search.origin.manifest.json")))!;
        Assert.Equal("origin", man.LayerRole);
        Assert.Equal(SearchIndexService.CurrentOriginBuildGuid, man.BuildGuid);
        Assert.False(string.IsNullOrEmpty(man.OriginHash));
        Assert.True(man.Entries.Count > 0);
        Assert.All(man.Entries, e => Assert.Equal(SearchSide.Original, e.Side));

        // Corpusfreq sibling stamps the current corpusfreq guid (§2.2a family gate).
        var cf = System.Text.Json.JsonSerializer.Deserialize<SearchIndexManifest>(
            File.ReadAllText(Path.Combine(bundle, "search.origin.corpusfreq.manifest.json")))!;
        Assert.Equal(SearchIndexService.CurrentCorpusFreqBuildGuid, cf.BuildGuid);
    }

    [Fact]
    public async Task StagedOriginBundle_AdoptsIntoVirginRoot_ZeroBuild()
    {
        WriteOrigin(6);
        var bundle = await BakeTrimmedOriginBundleAsync();

        var root = Path.Combine(_temp, "install");
        Directory.CreateDirectory(root);

        using var svc = new SearchIndexService { TestOnlyBundleDirOverride = bundle };
        // Virgin + matching origin bundle ⇒ the probe SEEDS the origin family (zero origin build);
        // the overlay is still owed (no translations built yet), so the probe reports stale.
        Assert.True(await svc.IsStaleAsync(root, _origDir, Array.Empty<string>()));
        Assert.True(File.Exists(Path.Combine(root, "search.origin.bin")));   // origin seeded from bundle

        // Building materialises the (empty) overlay; the root is then fresh (no rebuild loop).
        await svc.BuildOrUpdateAsync(root, _origDir, Array.Empty<string>(), forceRebuild: false);
        Assert.True(File.Exists(Path.Combine(root, "search.overlay.manifest.json")));
        Assert.False(await svc.IsStaleAsync(root, _origDir, Array.Empty<string>()));
    }
}
