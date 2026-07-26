using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL5 (design §3.1/§4.1): on a split root, a translation edit rewrites ONLY the live overlay
/// family — the overlay artifacts change while the frozen origin stays byte-stable — and the
/// overlay build's full-fallback is confined to overlay files. Also pins the fixed latent bug
/// (§7.2 #2): an edit confined to an ADDITIONAL corpus flips <c>OverlayHash</c> (overlay stale)
/// while the origin stays fresh.
/// </summary>
public sealed class OverlayOnlyOnEditTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string BodyA = "禪宗祖師傳法心印無門關公案";
    private const string BodyB = "臨濟義玄黃檗希運栽松道者一喝";
    private const string BodyEdit = "洞山五位偏正回互寶鏡三昧";

    public OverlayOnlyOnEditTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-overlayedit-" + Guid.NewGuid().ToString("N")[..8]);
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

    private static async Task BuildSplit(string root, string origDir, IReadOnlyList<string> tranDirs,
        IReadOnlyList<string>? addOrig = null, IReadOnlyList<string>? addTran = null)
    {
        using var svc = new SearchIndexService();
        await svc.BuildOriginLayerAsync(root, origDir);
        await svc.BuildOverlayLayerAsync(root, tranDirs, addOrig, addTran);
    }

    private static Dictionary<string, byte[]> SnapshotFamily(string root, string glob)
    {
        var snap = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Directory.EnumerateFiles(root, glob, SearchOption.TopDirectoryOnly))
            snap[Path.GetFileName(f)] = File.ReadAllBytes(f);
        return snap;
    }

    private static bool AnyBytesDiffer(Dictionary<string, byte[]> before, Dictionary<string, byte[]> after)
    {
        if (before.Count != after.Count) return true;
        foreach (var kv in before)
        {
            if (!after.TryGetValue(kv.Key, out var a)) return true;
            if (!kv.Value.AsSpan().SequenceEqual(a)) return true;
        }
        return false;
    }

    private static void AssertUnchanged(Dictionary<string, byte[]> before, Dictionary<string, byte[]> after, string which)
    {
        Assert.False(AnyBytesDiffer(before, after), $"{which} family must be byte-stable");
    }

    // ==================================================================================
    // Translation edit rewrites the overlay, leaves the origin byte-stable.
    // ==================================================================================

    [Fact]
    public async Task TranslationEdit_RewritesOverlay_LeavesOriginStable()
    {
        Write(_origDir, "a.xml", BodyA);
        Write(_origDir, "b.xml", BodyA + "山");
        Write(_tranDir, "a.xml", BodyA);
        var root = NewDir("root");
        await BuildSplit(root, _origDir, new[] { _tranDir });

        var originBefore = SnapshotFamily(root, "search.origin.*");
        var overlayBefore = SnapshotFamily(root, "search.overlay.*");

        Write(_tranDir, "a.xml", BodyA + BodyEdit); // edit the translation

        using var svc = Probe(EmptyBundle());
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);

        var originAfter = SnapshotFamily(root, "search.origin.*");
        var overlayAfter = SnapshotFamily(root, "search.overlay.*");

        AssertUnchanged(originBefore, originAfter, "origin");
        Assert.True(AnyBytesDiffer(overlayBefore, overlayAfter), "overlay family must be rewritten on a translation edit");
        Assert.Equal(0, svc.LastBuildFallbackCount); // overlay full-fallback confined to overlay files, no fault
    }

    // ==================================================================================
    // An ADDITIONAL-corpus edit flips OverlayHash (overlay stale) — origin stays fresh.
    // (Fixes the latent "additional-corpus edit is invisible to staleness" bug, §7.2 #2.)
    // ==================================================================================

    [Fact]
    public async Task AdditionalCorpusEdit_OverlayStale_OriginFresh()
    {
        Write(_origDir, "a.xml", BodyA);
        Write(_tranDir, "a.xml", BodyA);
        var bOrig = NewDir("bOrig");
        var bTran = NewDir("bTran");
        Write(bOrig, "b.xml", BodyB);
        Write(bTran, "b.xml", BodyB);

        var root = NewDir("root");
        await BuildSplit(root, _origDir, new[] { _tranDir }, new[] { bOrig }, new[] { bTran });

        // Baseline: nothing changed → not stale.
        using (var baseline = Probe(EmptyBundle()))
            Assert.False(await baseline.IsStaleAsync(root, _origDir, new[] { _tranDir },
                additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran }));

        var originBefore = SnapshotFamily(root, "search.origin.*");

        // Edit ONLY the additional corpus — under the combined model this was invisible to
        // staleness; under the split it flips the overlay hash basis.
        Write(bOrig, "b.xml", BodyB + BodyEdit);

        using var svc = Probe(EmptyBundle());
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir },
            additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran }));

        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false,
            additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran });

        // Origin never touched by the additional-corpus edit; only the overlay caught up.
        AssertUnchanged(originBefore, SnapshotFamily(root, "search.origin.*"), "origin");
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir },
            additionalOriginalDirs: new[] { bOrig }, additionalTranslatedDirs: new[] { bTran }));
    }
}
