using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL5 (design §5.2): the ADOPTED-ORIGIN stat-cache heal. A bundle-seeded / adopted origin
/// manifest carries the CI machine's <c>(ticks,len)</c> stamps, so the first local probe
/// cache-misses on every origin entry and does one content-hash pass. The origin probe must
/// then write the fresh LOCAL stamps back to <c>search.origin.manifest.json</c> so every
/// subsequent probe is stat-only — otherwise every launch would re-read + SHA-256 the whole
/// ~4,990-file origin corpus forever. Mirrors
/// <c>BundleSeedTests.SeededManifest_StatCache_HealsToLocalTicks_AfterFirstProbe</c>, but
/// origin-scoped (the existing FL5 tests can't catch this — they bake the bundle from the same
/// dir they hash, so ticks already match).
/// </summary>
public sealed class OriginHealTests : IDisposable
{
    private readonly string _temp;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string Body0 = "禪宗祖師傳法心印無門關公案";

    public OriginHealTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "readzen-originheal-" + Guid.NewGuid().ToString("N")[..8]);
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

    private static async Task BuildSplit(string root, string origDir, IReadOnlyList<string> tranDirs)
    {
        using var svc = new SearchIndexService();
        await svc.BuildOriginLayerAsync(root, origDir);
        await svc.BuildOverlayLayerAsync(root, tranDirs);
    }

    private static SearchIndexManifest ReadOriginManifest(string root) =>
        JsonSerializer.Deserialize<SearchIndexManifest>(
            File.ReadAllText(Path.Combine(root, "search.origin.manifest.json")))!;

    [Fact]
    public async Task AdoptedOriginManifest_StatCache_HealsToLocalTicks_AfterFirstProbe()
    {
        // A multi-file origin so the "no re-read on the 2nd probe" claim is meaningful.
        for (int i = 0; i < 5; i++) Write(_origDir, $"o{i:D2}.xml", Body0 + i);
        Write(_tranDir, "o00.xml", Body0);

        var root = NewDir("root");
        await BuildSplit(root, _origDir, new[] { _tranDir });

        var originMp = Path.Combine(root, "search.origin.manifest.json");

        // Simulate a bundle cut on a different machine: poison every origin entry's mtime ticks so
        // they no longer match the local files. ContentHash + OriginHash stay intact → still fresh.
        const long PoisonTicks = 12345L;
        var poisoned = ReadOriginManifest(root);
        Assert.NotEmpty(poisoned.Entries);
        foreach (var e in poisoned.Entries) e.LastWriteUtcTicks = PoisonTicks;
        File.WriteAllText(originMp, JsonSerializer.Serialize(poisoned));

        var svc = new SearchIndexService { TestOnlyBundleDirOverride = EmptyBundle() };
        long healBefore = svc.LastContentHashBackfillCount;

        // First probe: fresh, but every origin entry cache-misses on the poisoned ticks → hash
        // pass + heal rewrites the origin manifest with the real LOCAL ticks.
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.Equal(healBefore + 1, svc.LastContentHashBackfillCount); // exactly one heal fired

        var healed = ReadOriginManifest(root);
        foreach (var e in healed.Entries)
        {
            var filePath = Path.Combine(_origDir, e.RelPath.Replace('/', Path.DirectorySeparatorChar));
            var actualTicks = File.GetLastWriteTimeUtc(filePath).Ticks;
            Assert.NotEqual(PoisonTicks, e.LastWriteUtcTicks); // poison healed away
            Assert.Equal(actualTicks, e.LastWriteUtcTicks);    // to the real local mtime
        }

        // Second probe: entries now carry local ticks → stat-only hits → ZERO origin XML re-read.
        // Proven two ways: no further heal fires, and the origin manifest bytes are untouched.
        var bytesBefore = File.ReadAllBytes(originMp);
        long healAfterFirst = svc.LastContentHashBackfillCount;
        Assert.False(await svc.IsStaleAsync(root, _origDir, new[] { _tranDir }));
        Assert.Equal(healAfterFirst, svc.LastContentHashBackfillCount); // no re-heal → all stat-hits
        Assert.Equal(bytesBefore, File.ReadAllBytes(originMp));         // manifest not rewritten
    }
}
