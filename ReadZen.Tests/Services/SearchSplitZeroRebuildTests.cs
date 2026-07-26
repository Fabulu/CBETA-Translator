using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL6 (design §5.2/§6.3): the end-to-end ZERO-REBUILD cert for the search split — the analogue
/// of NavZeroRebuildEndToEndTests. A fresh install with a CI-style origin bundle whose per-entry
/// stamps carry FOREIGN (build-machine) ticks: the origin family is ADOPTED (no build), its
/// stat-cache HEALS to local ticks on the first probe (the FL5 §5.2 heal, now proven with a
/// genuinely-foreign bundle) and stays stat-only thereafter; the overlay is built once. Then a
/// single translation edit rebuilds ONLY the overlay — the frozen origin family is byte- and
/// mtime-stable throughout.
/// </summary>
public sealed class SearchSplitZeroRebuildTests : IDisposable
{
    private readonly string _temp;
    private readonly string _root;      // the user install root (holds the local index)
    private readonly string _origDir;
    private readonly string _tranDir;
    private readonly string _bundleDir; // the staged CI bundle (origin-only)

    private static readonly DateTime BuildAnchor = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CloneAnchor = new(2026, 7, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EditAnchor  = new(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc);

    public SearchSplitZeroRebuildTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-searche2e-" + Guid.NewGuid().ToString("N")[..8]);
        _root = Path.Combine(_temp, "install");
        _origDir = Path.Combine(_temp, "xml-p5");
        _tranDir = Path.Combine(_temp, "xml-p5t");
        foreach (var d in new[] { _root, _origDir, _tranDir })
            Directory.CreateDirectory(d);
        _bundleDir = Path.Combine(_temp, "PrebuiltIndex");
        Directory.CreateDirectory(_bundleDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch { }
    }

    private static string Xml(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        $"<p>{body}</p>\n</body></text></TEI>\n";

    private static void WriteAt(string dir, string name, string body, DateTime mtimeUtc)
    {
        var p = Path.Combine(dir, name);
        File.WriteAllText(p, Xml(body));
        File.SetLastWriteTimeUtc(p, mtimeUtc);
    }

    private static string? StampField(string manifestFile, string field)
    {
        if (!File.Exists(manifestFile)) return null;
        return JsonNode.Parse(File.ReadAllText(manifestFile))?[field]?.GetValue<string>();
    }

    private static Dictionary<string, (byte[] bytes, DateTime mtime)> SnapshotOrigin(string root)
    {
        var snap = new Dictionary<string, (byte[], DateTime)>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Directory.EnumerateFiles(root, "search.origin.*", SearchOption.TopDirectoryOnly))
            snap[Path.GetFileName(f)] = (File.ReadAllBytes(f), File.GetLastWriteTimeUtc(f));
        return snap;
    }

    private static void AssertOriginUnchanged(string root, Dictionary<string, (byte[] bytes, DateTime mtime)> before, string when)
    {
        var after = SnapshotOrigin(root);
        Assert.Equal(before.Keys.OrderBy(k => k, StringComparer.Ordinal),
                     after.Keys.OrderBy(k => k, StringComparer.Ordinal));
        foreach (var kv in before)
        {
            Assert.True(after.TryGetValue(kv.Key, out var a), $"origin file {kv.Key} disappeared ({when})");
            Assert.True(kv.Value.bytes.AsSpan().SequenceEqual(a.bytes), $"origin file {kv.Key} BYTES changed ({when})");
            Assert.Equal(kv.Value.mtime, a.mtime); // mtime stable — the origin was never rewritten
        }
    }

    [Fact]
    public async Task FreshInstall_ForeignBundle_AdoptZeroBuild_HealsOnce_ThenEdit_OverlayOnly()
    {
        const int N = 5;
        for (int i = 0; i < N; i++)
            WriteAt(_origDir, $"o{i:D2}.xml", "趙州狗子佛性" + i, BuildAnchor);
        WriteAt(_tranDir, "o00.xml", "Zhaozhou dog Buddha nature commentary", BuildAnchor);
        WriteAt(_tranDir, "o01.xml", "second translation 佛性", BuildAnchor);

        // Stage the CI bundle: an origin-only family baked over the corpus at BuildAnchor, trimmed
        // of its text sidecar (as release.yml does).
        using (var bake = new SearchIndexService())
            await bake.BuildOriginLayerAsync(_bundleDir, _origDir);
        foreach (var f in Directory.EnumerateFiles(_bundleDir, "search.origin.text.*").ToList())
            File.Delete(f);
        var bundleOriginStamp = StampField(Path.Combine(_bundleDir, "search.origin.manifest.json"), "IndexStamp");
        Assert.NotNull(bundleOriginStamp);

        // FOREIGN ticks: a fresh clone rewrites every working-tree mtime (content identical), so the
        // bundle manifest's (build-machine) ticks no longer match the local corpus ticks.
        foreach (var f in Directory.EnumerateFiles(_origDir, "*.xml"))
            File.SetLastWriteTimeUtc(f, CloneAnchor);
        foreach (var f in Directory.EnumerateFiles(_tranDir, "*.xml"))
            File.SetLastWriteTimeUtc(f, CloneAnchor);

        Assert.False(File.Exists(Path.Combine(_root, "search.origin.manifest.json"))); // virgin

        using var svc = new SearchIndexService { TestOnlyBundleDirOverride = _bundleDir };

        // ── Phase 1: fresh-install probe — adopt origin (zero build) + heal foreign ticks ──
        long healBefore = svc.LastContentHashBackfillCount;
        Assert.True(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }));   // overlay owed
        Assert.True(File.Exists(Path.Combine(_root, "search.origin.bin")));         // origin seeded
        Assert.Equal(bundleOriginStamp, StampField(Path.Combine(_root, "search.origin.manifest.json"), "IndexStamp")); // ADOPTED, not built
        Assert.True(svc.LastContentHashBackfillCount > healBefore, "§5.2 heal must fire on the foreign-tick origin");

        // ── Build the overlay (origin already adopted — overlay-only work) ──
        await svc.BuildOrUpdateAsync(_root, _origDir, new[] { _tranDir }, forceRebuild: false);
        Assert.True(File.Exists(Path.Combine(_root, "search.overlay.manifest.json")));
        Assert.Equal(2, svc.LastBuildXmlReadCount);   // only the 2 translations — origin never read
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.Equal(bundleOriginStamp, StampField(Path.Combine(_root, "search.origin.manifest.json"), "IndexStamp"));
        Assert.False(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }));  // fresh, no rebuild loop

        // Heal is now HEALED: from here the origin stat-cache is stat-only — repeated probes do
        // NOT re-heal (the FL5 §5.2 "heals once then stays stat-only" finding, with a foreign bundle).
        long settled = svc.LastContentHashBackfillCount;
        Assert.True(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }) == false);
        Assert.False(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }));
        Assert.Equal(settled, svc.LastContentHashBackfillCount);   // no further heals

        // ── Phase 2: one translation edit ⇒ overlay-only rebuild, origin byte+mtime stable ──
        var originBefore = SnapshotOrigin(_root);
        WriteAt(_tranDir, "o00.xml", "Zhaozhou dog Buddha nature REVISED 佛性", EditAnchor);

        Assert.True(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }));   // overlay stale
        await svc.BuildOrUpdateAsync(_root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // The overlay rebuilds its 2 translations (a full per-layer overlay build) — crucially NOT
        // the 5 FROZEN origin files. If the origin were rebuilt the count would be 7; that it is 2
        // is the zero-rebuild cert, reinforced by the byte+mtime snapshot below.
        Assert.Equal(2, svc.LastBuildXmlReadCount);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        AssertOriginUnchanged(_root, originBefore, "after translation edit");
        Assert.False(await svc.IsStaleAsync(_root, _origDir, new[] { _tranDir }));
    }
}
