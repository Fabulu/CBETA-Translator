using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL7 (design §5.3): the ORIGIN-TEXT MATERIALIZATION job. The frozen origin family is
/// adopt-preferred and the shipped origin bundle is TRIMMED (no <c>search.origin.text.*</c>),
/// so after an adopt the origin text sidecar is absent and every origin-side verify/snippet
/// parses XML from disk forever. <see cref="SearchIndexService.MaterializeOriginTextAsync"/> is
/// the deliberate, background, best-effort, idempotent job that fills it — stamped with the
/// current origin <c>IndexStamp</c> (accelerator binding: a stamp-mismatched sidecar is loader-
/// refused, and search stays fully correct WITHOUT the sidecar via the XML fallback).
///
/// Fixtures are tiny synthetic CJK corpora in real temp dirs (pattern mirrors
/// TrimmedSidecarTests / SearchSplitZeroRebuildTests). A building/loading service is disposed
/// before any file delete so its memory-mapped bin handles never lock a file (Windows mmap locks).
/// </summary>
public sealed class OriginTextMaterializationTests : IDisposable
{
    private readonly string _temp;
    private readonly string _root;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string Query = "無門";

    public OriginTextMaterializationTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-otm-" + Guid.NewGuid().ToString("N")[..8]);
        _root = Path.Combine(_temp, "install");
        _origDir = Path.Combine(_temp, "xml-p5");
        _tranDir = Path.Combine(_temp, "xml-p5t");
        foreach (var d in new[] { _root, _origDir, _tranDir })
            Directory.CreateDirectory(d);
    }

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch { }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static string Xml(string body) =>
        $"<TEI><text><body>{body}</body></text></TEI>";

    private void WriteOriginCorpus(int fileCount)
    {
        for (int i = 0; i < fileCount; i++)
        {
            var body = new System.Text.StringBuilder();
            for (int k = 0; k <= i; k++)
                body.Append("無門關");
            body.Append(new string('中', (i + 1) * 40));
            File.WriteAllText(Path.Combine(_origDir, $"o{i:D3}.xml"), Xml(body.ToString()));
        }
    }

    /// <summary>Builds a full SPLIT root locally (origin family carries its text sidecar).</summary>
    private async Task BuildSplitLocalAsync()
    {
        using var svc = new SearchIndexService();
        await svc.BuildOriginLayerAsync(_root, _origDir);
        await svc.BuildOverlayLayerAsync(_root, new[] { _tranDir });
    }

    private string OriginTextBin => Path.Combine(_root, "search.origin.text.bin");
    private string OriginTextManifest => Path.Combine(_root, "search.origin.text.manifest.json");

    private static string? StampOf(string manifestFile) =>
        File.Exists(manifestFile)
            ? JsonNode.Parse(File.ReadAllText(manifestFile))?["IndexStamp"]?.GetValue<string>()
            : null;

    private string? OriginStamp() => StampOf(Path.Combine(_root, "search.origin.manifest.json"));
    private string? TextSidecarStamp() => StampOf(OriginTextManifest);

    private void DeleteOriginTextSidecar()
    {
        if (File.Exists(OriginTextBin)) File.Delete(OriginTextBin);
        if (File.Exists(OriginTextManifest)) File.Delete(OriginTextManifest);
    }

    /// <summary>A per-rel result snapshot: origin hit count + the ordered KWIC snippet strings.</summary>
    private sealed record RelResult(int HitsOriginal, List<string> Snippets);

    private async Task<Dictionary<string, RelResult>> SearchOriginAsync(SearchIndexService svc)
    {
        var manifest = await svc.TryLoadAsync(_root);
        Assert.NotNull(manifest);
        var map = new Dictionary<string, RelResult>(StringComparer.Ordinal);
        await foreach (var g in svc.SearchAllAsync(
            _root, _origDir, _tranDir, manifest!, Query,
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 10))
        {
            map[g.RelPath] = new RelResult(g.HitsOriginal, g.Children.Select(c => c.Hit.SnippetText).ToList());
        }
        return map;
    }

    private static void AssertResultsEqual(Dictionary<string, RelResult> expected, Dictionary<string, RelResult> actual)
    {
        Assert.Equal(
            expected.Keys.OrderBy(k => k, StringComparer.Ordinal),
            actual.Keys.OrderBy(k => k, StringComparer.Ordinal));
        foreach (var kv in expected)
        {
            Assert.True(actual.TryGetValue(kv.Key, out var a), $"missing rel {kv.Key}");
            Assert.Equal(kv.Value.HitsOriginal, a!.HitsOriginal);
            Assert.Equal(kv.Value.Snippets, a.Snippets);
        }
    }

    // ===================================================================
    // (1) The job produces a loadable, stamp-current sidecar; a second run no-ops.
    // ===================================================================

    [Fact]
    public async Task Materialize_ProducesLoadableStampCurrentSidecar_ByteIdenticalToLocalBuild_SecondRunNoOps()
    {
        WriteOriginCorpus(6);
        await BuildSplitLocalAsync();

        // The locally-built origin already wrote a text sidecar — capture it as the byte-parity
        // baseline, then simulate the trimmed-adopt on-disk state (sidecar removed).
        Assert.True(File.Exists(OriginTextBin));
        var expectedBin = File.ReadAllBytes(OriginTextBin);
        var originStamp = OriginStamp();
        Assert.False(string.IsNullOrEmpty(originStamp));
        DeleteOriginTextSidecar();
        Assert.False(File.Exists(OriginTextBin));

        using var svc = new SearchIndexService();
        await svc.MaterializeOriginTextAsync(_root, _origDir, default);

        // Restored, loadable, byte-identical text, stamped with the current origin IndexStamp.
        Assert.True(File.Exists(OriginTextBin));
        Assert.True(File.Exists(OriginTextManifest));
        Assert.True(expectedBin.AsSpan().SequenceEqual(File.ReadAllBytes(OriginTextBin)),
            "materialized origin text.bin must be byte-identical to the locally-built one");
        Assert.Equal(originStamp, TextSidecarStamp());
        Assert.Equal(1, svc.LastOriginTextMaterializeCount);

        // Idempotent: a stamp-current sidecar already present ⇒ second run is a no-op.
        await svc.MaterializeOriginTextAsync(_root, _origDir, default);
        Assert.Equal(1, svc.LastOriginTextMaterializeCount);
    }

    // ===================================================================
    // (2) Absent sidecar ⇒ correct via XML fallback; present ⇒ served from sidecar (no XML read).
    // ===================================================================

    [Fact]
    public async Task AbsentSidecar_XmlFallbackCorrect_PresentSidecar_ServedWithoutXmlRead()
    {
        WriteOriginCorpus(6);
        await BuildSplitLocalAsync();

        // Run 1 — sidecar PRESENT: origin verify/snippets are served from search.origin.text.bin,
        // so the verify XML-parse counter stays 0.
        Dictionary<string, RelResult> baseline;
        using (var svc = new SearchIndexService { Options = { SkipVerifySnippetTopN = 1000 } })
        {
            svc.ResetVerifyXmlReadCount();
            baseline = await SearchOriginAsync(svc);
            Assert.Equal(6, baseline.Count);
            Assert.All(baseline.Values, r => Assert.True(r.HitsOriginal > 0));
            Assert.Equal(0, svc.LastVerifyXmlReadCount);
        }

        // Trim the sidecar (the adopt-from-trimmed-bundle on-disk state).
        DeleteOriginTextSidecar();

        // Run 2 — sidecar ABSENT: identical results, but origin verify now parses XML.
        using (var svc = new SearchIndexService { Options = { SkipVerifySnippetTopN = 1000 } })
        {
            svc.ResetVerifyXmlReadCount();
            var absent = await SearchOriginAsync(svc);
            AssertResultsEqual(baseline, absent);
            Assert.True(svc.LastVerifyXmlReadCount > 0,
                "with the origin text sidecar absent, origin verify must fall back to XML");
        }

        // Materialize the sidecar off the origin manifest.
        using (var mat = new SearchIndexService())
            await mat.MaterializeOriginTextAsync(_root, _origDir, default);
        Assert.True(File.Exists(OriginTextBin));

        // Run 3 — sidecar PRESENT again: served from the sidecar, no XML read, identical results.
        using (var svc = new SearchIndexService { Options = { SkipVerifySnippetTopN = 1000 } })
        {
            svc.ResetVerifyXmlReadCount();
            var served = await SearchOriginAsync(svc);
            AssertResultsEqual(baseline, served);
            Assert.Equal(0, svc.LastVerifyXmlReadCount);
        }
    }

    // ===================================================================
    // (3) Stamp-mismatched sidecar ⇒ loader-refused (accelerator binding); search still correct.
    // ===================================================================

    [Fact]
    public async Task StampMismatchedSidecar_LoaderRefused_SearchStillCorrect()
    {
        WriteOriginCorpus(6);
        await BuildSplitLocalAsync();

        Dictionary<string, RelResult> baseline;
        using (var svc = new SearchIndexService { Options = { SkipVerifySnippetTopN = 1000 } })
            baseline = await SearchOriginAsync(svc);

        // Materialize a stamp-current sidecar (served), then corrupt ONLY its IndexStamp so it no
        // longer matches the origin manifest — the accelerator-binding mismatch.
        using (var mat = new SearchIndexService())
            await mat.MaterializeOriginTextAsync(_root, _origDir, default);
        Assert.Equal(OriginStamp(), TextSidecarStamp());

        var node = JsonNode.Parse(File.ReadAllText(OriginTextManifest))!;
        node["IndexStamp"] = "stale-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(OriginTextManifest, node.ToJsonString());

        // The stale-stamped sidecar is REFUSED ⇒ origin verify XML-falls-back ⇒ results correct.
        using (var svc = new SearchIndexService { Options = { SkipVerifySnippetTopN = 1000 } })
        {
            svc.ResetVerifyXmlReadCount();
            var refused = await SearchOriginAsync(svc);
            AssertResultsEqual(baseline, refused);
            Assert.True(svc.LastVerifyXmlReadCount > 0,
                "a stamp-mismatched origin sidecar must be refused (XML fallback), not served");
        }
    }

    // ===================================================================
    // (4) End-to-end: adopt a trimmed origin bundle ⇒ the trigger fires the job at idle.
    // ===================================================================

    [Fact]
    public async Task AdoptTrimmedOriginBundle_TriggerMaterializesSidecar_EndToEnd()
    {
        WriteOriginCorpus(5);
        File.WriteAllText(Path.Combine(_tranDir, "o000.xml"), Xml("commentary 無門"));

        // Stage a CI-style origin bundle, trimmed of its text sidecar (as release.yml ships it).
        var bundleDir = Path.Combine(_temp, "PrebuiltIndex");
        Directory.CreateDirectory(bundleDir);
        using (var bake = new SearchIndexService())
            await bake.BuildOriginLayerAsync(bundleDir, _origDir);
        foreach (var f in Directory.EnumerateFiles(bundleDir, "search.origin.text.*").ToList())
            File.Delete(f);

        using var svc = new SearchIndexService { TestOnlyBundleDirOverride = bundleDir };

        // Adopt origin (zero build) + build the overlay; the split build orchestration fires the
        // idle materialization job for the absent origin text sidecar.
        Assert.True(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }));   // overlay owed; origin adopted
        Assert.True(File.Exists(Path.Combine(_root, "search.origin.bin")));         // origin adopted
        Assert.False(File.Exists(OriginTextBin));                                   // ... trimmed ⇒ absent

        await svc.BuildOrUpdateAsync(_root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // Await the fired-at-idle job deterministically, then assert the sidecar is materialized,
        // loadable, and stamp-current against the ADOPTED origin manifest.
        await svc.WhenOriginTextMaterializedAsync();
        Assert.Equal(1, svc.LastOriginTextMaterializeCount);
        Assert.True(File.Exists(OriginTextBin));
        Assert.True(File.Exists(OriginTextManifest));
        Assert.Equal(OriginStamp(), TextSidecarStamp());

        // Search is correct, and now served from the sidecar (no origin XML parse).
        using var probe = new SearchIndexService { Options = { SkipVerifySnippetTopN = 1000 } };
        probe.ResetVerifyXmlReadCount();
        var results = await SearchOriginAsync(probe);
        Assert.Equal(5, results.Count);
        Assert.Equal(0, probe.LastVerifyXmlReadCount);
    }
}
