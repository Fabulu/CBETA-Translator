using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL5 (design §1.5 / finding A / risk #3): the cross-layer <c>BasedOnOriginStamp</c> binding.
/// When the origin family gets a NEW IndexStamp (a rebuild or an adopt) the overlay it was
/// bound to is stale — the load-time guard refuses to serve the mismatched pair (preventing the
/// double-serve tf-collision), the overlay probe reports stale, and the fix is an OVERLAY-ONLY
/// rebuild against the current origin. The origin is never touched by that overlay rebuild.
/// </summary>
public sealed class OriginOverlayBindingTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string BodyA = "禪宗祖師傳法心印無門關公案";

    public OriginOverlayBindingTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-binding-" + Guid.NewGuid().ToString("N")[..8]);
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

    private static SearchIndexManifest ReadManifest(string root, string fileName)
    {
        var json = File.ReadAllText(Path.Combine(root, fileName));
        return JsonSerializer.Deserialize<SearchIndexManifest>(json)!;
    }

    private static Dictionary<string, byte[]> SnapshotOrigin(string root)
    {
        var snap = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Directory.EnumerateFiles(root, "search.origin.*", SearchOption.TopDirectoryOnly))
            snap[Path.GetFileName(f)] = File.ReadAllBytes(f);
        return snap;
    }

    private static void AssertOriginUnchanged(string root, Dictionary<string, byte[]> before)
    {
        var after = SnapshotOrigin(root);
        Assert.Equal(before.Keys.OrderBy(k => k, StringComparer.Ordinal),
                     after.Keys.OrderBy(k => k, StringComparer.Ordinal));
        foreach (var kv in before)
            Assert.True(after[kv.Key].AsSpan().SequenceEqual(kv.Value), $"origin file {kv.Key} changed");
    }

    [Fact]
    public async Task OriginRestamped_OverlayBindingMismatch_LoadRefused_ThenOverlayOnlyRebuild()
    {
        Write(_origDir, "a.xml", BodyA);
        Write(_origDir, "b.xml", BodyA + "山");
        Write(_tranDir, "a.xml", BodyA);
        var root = NewDir("root");

        // Build the split root: origin (stamp S1) + overlay bound to S1.
        using (var build = new SearchIndexService())
        {
            await build.BuildOriginLayerAsync(root, _origDir);
            await build.BuildOverlayLayerAsync(root, new[] { _tranDir });
        }
        var stampS1 = ReadManifest(root, "search.origin.manifest.json").IndexStamp;
        Assert.Equal(stampS1, ReadManifest(root, "search.overlay.manifest.json").BasedOnOriginStamp);

        // Rebuild ONLY the origin → a new IndexStamp S2 (same corpus content, fresh stamp/GUID).
        // The overlay is now bound to a superseded origin stamp.
        using (var reorigin = new SearchIndexService())
            await reorigin.BuildOriginLayerAsync(root, _origDir);
        var stampS2 = ReadManifest(root, "search.origin.manifest.json").IndexStamp;
        Assert.NotEqual(stampS1, stampS2);
        Assert.Equal(stampS1, ReadManifest(root, "search.overlay.manifest.json").BasedOnOriginStamp); // still bound to S1

        // Finding A — the load-time guard REFUSES the mismatched split pair (would otherwise
        // double-serve / tf-collide).
        using (var loader = new SearchIndexService())
            Assert.Null(await loader.TryLoadAsync(root));

        // The overlay probe reports stale for exactly this binding mismatch (origin fresh).
        var originBefore = SnapshotOrigin(root);
        using var svc = Probe(EmptyBundle());
        Assert.True(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));

        // The fix is an OVERLAY-ONLY rebuild — origin is never touched.
        await svc.BuildOrUpdateAsync(root, _origDir, new[] { _tranDir }, forceRebuild: false);
        AssertOriginUnchanged(root, originBefore);

        // Overlay is now re-bound to S2 and the split root loads again.
        Assert.Equal(stampS2, ReadManifest(root, "search.overlay.manifest.json").BasedOnOriginStamp);
        using (var after = new SearchIndexService())
            Assert.NotNull(await after.TryLoadAsync(root));
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
    }
}
