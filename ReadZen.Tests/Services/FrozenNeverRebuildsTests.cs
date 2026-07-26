using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL5 (frozen/live index split, design §4 / §7.3): the FROZEN origin family is never touched
/// by a translation edit/delete — no byte and no mtime of any <c>search.origin.*</c> artifact
/// changes across overlay-only rebuilds, and the build reads ONLY the overlay files (never the
/// ~4,990-file origin corpus). Also pins the #1 FL5 constraint: an existing LEGACY COMBINED
/// root with no split bundle stays on the combined incremental path and is NOT mass-rebuilt
/// into a split family (combined→split migration is FL6).
///
/// All fixtures are tiny synthetic corpora in real temp dirs. The probe/build service always
/// sets <see cref="SearchIndexService.TestOnlyBundleDirOverride"/> to a controlled dir so no
/// test depends on the exe-adjacent Assets/PrebuiltIndex.
/// </summary>
public sealed class FrozenNeverRebuildsTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string Body0 = "禪宗祖師傳法心印無門關公案";
    private const string BodyEdit = "洞山五位偏正回互寶鏡三昧";

    public FrozenNeverRebuildsTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-frozen-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_temp, "xml-p5");
        _tranDir = Path.Combine(_temp, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch { }
    }

    private static string Xml(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        $"<p>{body}</p>\n</body></text></TEI>\n";

    private static void Write(string dir, string name, string body) =>
        File.WriteAllText(Path.Combine(dir, name), Xml(body));

    private string NewDir(string label)
    {
        var p = Path.Combine(_temp, label + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(p);
        return p;
    }

    private string EmptyBundle() => NewDir("empty-bundle");

    private static SearchIndexService Probe(string bundleDir)
        => new SearchIndexService { TestOnlyBundleDirOverride = bundleDir };

    /// <summary>Writes a multi-file origin corpus (proves the many-file origin is NOT read when
    /// only the overlay rebuilds).</summary>
    private void WriteOriginCorpus(int nFiles)
    {
        for (int i = 0; i < nFiles; i++)
            Write(_origDir, $"o{i:D2}.xml", Body0 + i);
    }

    /// <summary>Builds a SPLIT root (origin + overlay) into <paramref name="root"/>.</summary>
    private static async Task BuildSplit(string root, string origDir, IReadOnlyList<string> tranDirs)
    {
        using var svc = new SearchIndexService();
        await svc.BuildOriginLayerAsync(root, origDir);
        await svc.BuildOverlayLayerAsync(root, tranDirs);
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

    private static bool Has(string root, string name) => File.Exists(Path.Combine(root, name));

    // ==================================================================================
    // (a) Translation EDIT → origin byte + mtime stable; overlay-only rebuild.
    // ==================================================================================

    [Fact]
    public async Task TranslationEdit_OriginByteAndMtimeStable_OverlayOnly()
    {
        WriteOriginCorpus(4);
        Write(_tranDir, "o00.xml", Body0); // one translation (of a both-sides rel)
        var root = NewDir("root");
        await BuildSplit(root, _origDir, new[] { _tranDir });

        var before = SnapshotOrigin(root);
        Assert.True(before.Count >= 4, "origin family should have several artifacts");

        // Edit the translation's content (a genuine community translation change).
        Write(_tranDir, "o00.xml", Body0 + BodyEdit);

        using var svc = Probe(EmptyBundle());
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir })); // overlay stale
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        AssertOriginUnchanged(root, before, "after translation edit");
        Assert.Equal(1, svc.LastBuildXmlReadCount);   // only the 1 overlay file read — never the 4 origin files
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir })); // reconciled to fresh
    }

    // ==================================================================================
    // (b) Translation DELETE → origin byte + mtime stable; overlay-only rebuild.
    // ==================================================================================

    [Fact]
    public async Task TranslationDelete_OriginByteAndMtimeStable_OverlayOnly()
    {
        WriteOriginCorpus(4);
        Write(_tranDir, "o00.xml", Body0);
        Write(_tranDir, "o01.xml", Body0 + "一");
        var root = NewDir("root");
        await BuildSplit(root, _origDir, new[] { _tranDir });

        var before = SnapshotOrigin(root);

        File.Delete(Path.Combine(_tranDir, "o01.xml")); // remove one translation

        using var svc = Probe(EmptyBundle());
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        AssertOriginUnchanged(root, before, "after translation delete");
        Assert.Equal(1, svc.LastBuildXmlReadCount);   // only the 1 surviving overlay file
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    // ==================================================================================
    // (c) 50 sequential translation edits → origin family byte-stable THROUGHOUT.
    // ==================================================================================

    [Fact]
    public async Task FiftyTranslationEdits_OriginFamilyByteStableThroughout()
    {
        WriteOriginCorpus(4);
        Write(_tranDir, "o00.xml", Body0);
        var root = NewDir("root");
        await BuildSplit(root, _origDir, new[] { _tranDir });

        var before = SnapshotOrigin(root);

        using var svc = Probe(EmptyBundle());
        for (int i = 0; i < 50; i++)
        {
            Write(_tranDir, "o00.xml", Body0 + BodyEdit + i);
            await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
            AssertOriginUnchanged(root, before, $"after edit {i}");
        }
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }

    // ==================================================================================
    // #1 CONSTRAINT: a LEGACY COMBINED root with no split bundle is NOT mass-rebuilt.
    // ==================================================================================

    [Fact]
    public async Task LegacyCombinedRoot_NoSplitBundle_StaysCombinedIncremental_NotMassRebuiltIntoSplit()
    {
        // A corpus big enough that a 1-file delta stays well under the 20% full-rebuild threshold.
        for (int i = 0; i < 12; i++)
        {
            Write(_origDir, $"c{i:D2}.xml", Body0 + i);
            Write(_tranDir, $"c{i:D2}.xml", Body0 + i);
        }
        var root = NewDir("root");
        using (var build = new SearchIndexService())
            await build.BuildAsync(root, _origDir, new[] { _tranDir }); // COMBINED family (search.index.*)

        Assert.True(Has(root, "search.index.manifest.json"));
        Assert.False(Has(root, "search.origin.manifest.json")); // precondition: NOT a split root

        // A single translation edit.
        Write(_tranDir, "c00.xml", Body0 + BodyEdit);

        using var svc = Probe(EmptyBundle()); // deterministically NO split bundle
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir })); // combined stale
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        // Stayed on the COMBINED incremental path — NOT promoted to a split family, NOT a mass rebuild.
        Assert.False(Has(root, "search.origin.manifest.json"), "must not create an origin family");
        Assert.False(Has(root, "search.overlay.manifest.json"), "must not create an overlay family");
        Assert.True(Has(root, "search.index.manifest.json"), "combined family must remain");
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.Equal(0, svc.LastBuildDeltaGuardTripped);          // stayed incremental (no full rebuild)
        Assert.True(svc.LastBuildXmlReadCount <= 2,               // only the edited delta — NOT all 24 files
            $"expected an incremental delta read, got {svc.LastBuildXmlReadCount} XML reads (mass rebuild?)");

        using var after = Probe(EmptyBundle());
        Assert.False(await after.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }
}
