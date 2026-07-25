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
/// FL3 (frozen/live index split, design §1.2 entry-set partition invariant + §3.2 overlay
/// inverted exclusion): directly invokes the origin + overlay layer builds on a synthetic
/// corpus and asserts the partition every FL4+ merge leans on:
/// <list type="bullet">
/// <item>a rel with BOTH sides appears in the ORIGIN inverted index only (its translated
///   doc is excluded from the overlay inverted feed — kept out to avoid a duplicate);</item>
/// <item>a translation-only rel appears in the OVERLAY inverted index only;</item>
/// <item>an additional-orig rel shadowed by an active-origin rel is excluded from the
///   overlay entirely (reproducing the combined keep-first dedup);</item>
/// <item>every <c>(rel, side)</c> lands in exactly ONE layer manifest.</item>
/// </list>
/// These are on-disk layer builds (search.origin.* / search.overlay.*); the serving path is
/// untouched (still combined v8) — FL3 only makes the layer builds directly invokable.
/// </summary>
public class LayerPartitionTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _root;       // index output root (both layer families land here)
    private readonly string _origDir;    // active origin corpus (xml-p5)
    private readonly string _tranDir;    // translations (xml-p5t)
    private readonly string _addOrigDir; // an additional original corpus (e.g. OpenZen)

    private const string RelBoth = "both.xml";        // origin + translation (both sides)
    private const string RelOriginOnly = "oonly.xml"; // origin only, no translation
    private const string RelShadow = "shadow.xml";    // in origin AND additional-orig (shadowed)
    private const string RelTranOnly = "tonly.xml";   // translation only (no origin)
    private const string RelAddOrig = "addonly.xml";  // additional-orig only (not in origin)

    public LayerPartitionTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-layerpart-" + Guid.NewGuid().ToString("N")[..8]);
        _root = Path.Combine(_tempRoot, "index");
        _origDir = Path.Combine(_tempRoot, "xml-p5");
        _tranDir = Path.Combine(_tempRoot, "xml-p5t");
        _addOrigDir = Path.Combine(_tempRoot, "xml-open");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
        Directory.CreateDirectory(_addOrigDir);

        // Distinct CJK phrases per file so each yields at least one bigram (indexable) and
        // the docs are individually attributable in the inverted index.
        Write(_origDir, RelBoth, "禪宗祖師傳法");
        Write(_origDir, RelOriginOnly, "山水清音妙道");
        Write(_origDir, RelShadow, "無門關公案語");

        Write(_tranDir, RelBoth, "Zen ancestral 心印妙義");   // translation of the both-sides rel
        Write(_tranDir, RelTranOnly, "般若波羅蜜多");         // translation-only rel

        Write(_addOrigDir, RelAddOrig, "白雲深處禪居");        // additional-orig only rel
        Write(_addOrigDir, RelShadow, "此文本應被遮蔽");        // collides with origin → shadowed out
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private static void Write(string dir, string rel, string cjk)
    {
        var xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
            $"<p>{cjk}</p>\n" +
            "</body></text></TEI>\n";
        File.WriteAllText(Path.Combine(dir, rel), xml);
    }

    [Fact]
    public async Task OriginAndOverlayBuilds_PartitionEntriesAndInvertedDocs()
    {
        var svc = new SearchIndexService();

        // Build the origin family (originalDir only), then the overlay family (translations +
        // additional-orig). The overlay reads the just-written origin manifest's rel-set for
        // §1.2 shadowing + §3.2 inverted exclusion.
        await svc.BuildOriginLayerAsync(_root, _origDir);
        await svc.BuildOverlayLayerAsync(_root, new[] { _tranDir }, new[] { _addOrigDir }, null);

        var originManifest = ReadManifest("search.origin.manifest.json");
        var overlayManifest = ReadManifest("search.overlay.manifest.json");

        Assert.Equal("origin", originManifest.LayerRole);
        Assert.Equal("overlay", overlayManifest.LayerRole);
        Assert.Equal("search-v9-origin", originManifest.BuildGuid);
        Assert.Equal("search-v9-overlay", overlayManifest.BuildGuid);
        // §1.5 binding: overlay records the origin manifest's IndexStamp it was built against.
        Assert.False(string.IsNullOrEmpty(originManifest.IndexStamp));
        Assert.Equal(originManifest.IndexStamp, overlayManifest.BasedOnOriginStamp);
        // Layer stamp fields carry the layer's own content hash (design §1.4).
        Assert.False(string.IsNullOrEmpty(originManifest.OriginHash));
        Assert.False(string.IsNullOrEmpty(overlayManifest.OverlayHash));

        // ── Manifest entry-set partition (§1.2) ──
        var originPairs = originManifest.Entries.Select(e => (e.RelPath, e.Side)).ToHashSet();
        var overlayPairs = overlayManifest.Entries.Select(e => (e.RelPath, e.Side)).ToHashSet();

        // Origin holds exactly the originalDir Original-side entries.
        Assert.Equal(
            new HashSet<(string, SearchSide)>
            {
                (RelBoth, SearchSide.Original),
                (RelOriginOnly, SearchSide.Original),
                (RelShadow, SearchSide.Original),
            },
            originPairs);

        // Overlay holds: both translations + the non-shadowed additional-orig Original.
        Assert.Equal(
            new HashSet<(string, SearchSide)>
            {
                (RelBoth, SearchSide.Translated),
                (RelTranOnly, SearchSide.Translated),
                (RelAddOrig, SearchSide.Original),
            },
            overlayPairs);

        // The shadowed additional-orig rel is NOT re-indexed by the overlay (it is an active
        // origin rel, indexed by the origin layer as its Original entry).
        Assert.DoesNotContain((RelShadow, SearchSide.Original), overlayPairs);
        Assert.DoesNotContain((RelShadow, SearchSide.Translated), overlayPairs);

        // Every (rel, side) is in EXACTLY one layer manifest (disjoint pair sets).
        Assert.Empty(originPairs.Intersect(overlayPairs));

        // ── Inverted-index doc partition (§2.2 / §3.2) ──
        var originInvertedRels = await LoadInvertedRelsAsync(svc, "search.origin.inverted.bin", originManifest.IndexStamp!);
        var overlayInvertedRels = await LoadInvertedRelsAsync(svc, "search.overlay.inverted.bin", overlayManifest.IndexStamp!);

        // A both-sides rel appears in the ORIGIN inverted index only.
        Assert.Contains(RelBoth, originInvertedRels);
        Assert.DoesNotContain(RelBoth, overlayInvertedRels);

        // A translation-only rel appears in the OVERLAY inverted index only.
        Assert.Contains(RelTranOnly, overlayInvertedRels);
        Assert.DoesNotContain(RelTranOnly, originInvertedRels);

        // The non-shadowed additional-orig rel is an overlay inverted doc; the shadowed rel
        // is an origin inverted doc (never an overlay one).
        Assert.Contains(RelAddOrig, overlayInvertedRels);
        Assert.Contains(RelShadow, originInvertedRels);
        Assert.DoesNotContain(RelShadow, overlayInvertedRels);

        // Origin-only rel is an origin inverted doc, never an overlay one.
        Assert.Contains(RelOriginOnly, originInvertedRels);
        Assert.DoesNotContain(RelOriginOnly, overlayInvertedRels);

        // The inverted doc sets are disjoint (the union-partitions-the-answer invariant FL4
        // relies on: no rel is a doc in both layers' inverted indexes).
        Assert.Empty(originInvertedRels.Intersect(overlayInvertedRels));
    }

    private SearchIndexManifest ReadManifest(string fileName)
    {
        var path = Path.Combine(_root, fileName);
        Assert.True(File.Exists(path), $"expected layer manifest {fileName}");
        var json = File.ReadAllText(path);
        var man = JsonSerializer.Deserialize<SearchIndexManifest>(json);
        Assert.NotNull(man);
        return man!;
    }

    private async Task<HashSet<string>> LoadInvertedRelsAsync(SearchIndexService svc, string fileName, string stamp)
    {
        var path = Path.Combine(_root, fileName);
        Assert.True(File.Exists(path), $"expected layer inverted index {fileName}");
        var idx = new InvertedSearchIndex();
        Assert.True(await idx.TryLoadAsync(path, stamp, CancellationToken.None),
            $"inverted index {fileName} failed to load with its manifest stamp");

        var rels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int d = 0; d < idx.DocCount; d++)
        {
            var rel = idx.GetRelPath((ushort)d);
            if (rel != null) rels.Add(rel);
        }
        return rels;
    }
}
