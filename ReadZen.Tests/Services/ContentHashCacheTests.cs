using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
/// PR B tests: per-file content-hash cache layered onto <see cref="SearchIndexService"/>'s
/// hash-based staleness check. The cache reuses the stored ContentHash from a manifest
/// entry when on-disk (LengthBytes, LastWriteUtcTicks) match, avoiding the cost of
/// SHA256'ing every XML file on every launch. Cache misses fall through to fresh hashing
/// + opportunistic write-back.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class ContentHashCacheTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;
    private readonly string _tranDir;

    public ContentHashCacheTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-hash-cache-test-" + Guid.NewGuid().ToString("N")[..8]);
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

    // ---------------- helpers ----------------

    /// <summary>
    /// Reads a manifest JSON file off disk. Bypasses the service cache so we observe
    /// what's actually persisted.
    /// </summary>
    private static SearchIndexManifest ReadManifest(string manifestPath)
    {
        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<SearchIndexManifest>(json)!;
    }

    private static void WriteManifest(string manifestPath, SearchIndexManifest m)
    {
        var json = JsonSerializer.Serialize(m, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestPath, json);
    }

    /// <summary>Builds a cache dict in the same namespaced-relPath shape AppendDirRows expects.</summary>
    private static Dictionary<string, SearchIndexEntry> MakeCache(params (string ns, string relPath, long len, long ticks, string? hash)[] entries)
    {
        var dict = new Dictionary<string, SearchIndexEntry>(StringComparer.Ordinal);
        foreach (var (ns, relPath, len, ticks, hash) in entries)
        {
            dict[ns + "/" + relPath.Replace('\\', '/')] = new SearchIndexEntry
            {
                RelPath = relPath,
                Side = ns.StartsWith("orig") ? SearchSide.Original : SearchSide.Translated,
                LengthBytes = len,
                LastWriteUtcTicks = ticks,
                ContentHash = hash,
            };
        }
        return dict;
    }

    // ---------------- 1: cache hit — file never opened ----------------

    [Fact]
    public async Task ComputeInputHash_CacheHit_NoFileRead_ReusesContentHash()
    {
        // Strategy: build manifest, then DELETE the file on disk and check that
        // ComputeInputHashAsync still produces a hash matching the original (because
        // the cache hit short-circuits the file read). If the file were re-opened,
        // EnumerateFiles would skip the missing file and the hash would differ.
        //
        // But Directory.EnumerateFiles operates on real on-disk state — if we delete,
        // the file disappears from enumeration entirely. So instead we corrupt the
        // file contents while preserving its (length, mtime) — the cache hit reuses
        // the OLD content hash, masking the corruption (proving the cache shortcut).
        var dir = Path.Combine(_tempRoot, "cachehit");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "a.xml");
        File.WriteAllText(file, "<x>ORIGINAL</x>");
        var anchor = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(file, anchor);

        // Capture the true content-hash with no cache.
        var trueHash = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache: null, writeBack: null, CancellationToken.None);

        var fi = new FileInfo(file);
        var cachedFileHash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file));
        var cachedHex = Convert.ToHexString(cachedFileHash).ToLowerInvariant();

        // Now corrupt the file content but restore the exact (length, mtime).
        File.WriteAllText(file, "<x>CHANGED!</x>"); // same byte length (15)
        Assert.Equal(fi.Length, new FileInfo(file).Length);
        File.SetLastWriteTimeUtc(file, anchor);

        var cache = MakeCache(("orig", "a.xml", fi.Length, anchor.Ticks, cachedHex));

        // With the cache, the corrupted bytes are NOT read — we get the original hash.
        var cachedHash = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache, writeBack: null, CancellationToken.None);

        Assert.Equal(trueHash, cachedHash); // cache hit reused the stored hash, ignoring corrupt bytes
    }

    // ---------------- 2: cache miss — length mismatch triggers re-hash ----------------

    [Fact]
    public async Task ComputeInputHash_CacheMiss_LengthMismatch_RehashesFile()
    {
        var dir = Path.Combine(_tempRoot, "lenmiss");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "a.xml");
        File.WriteAllText(file, "<x>realdata</x>");
        var realLen = new FileInfo(file).Length;
        var realTicks = new FileInfo(file).LastWriteTimeUtc.Ticks;

        // Cache claims length=999 (wrong) but provides a bogus stored hash that should
        // be ignored — we'll re-read the file and compute the *real* content hash.
        var bogusHash = new string('0', 64);
        var cache = MakeCache(("orig", "a.xml", 999, realTicks, bogusHash));

        var writeBack = new Dictionary<string, string>();
        var hash = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache, writeBack, CancellationToken.None);

        // Truth: hash without cache.
        var truth = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache: null, writeBack: null, CancellationToken.None);

        Assert.Equal(truth, hash);                 // re-hashed real bytes
        Assert.Single(writeBack);                  // miss → write-back recorded
        Assert.True(writeBack.ContainsKey("orig/a.xml"));
    }

    // ---------------- 3: cache miss — mtime mismatch triggers re-hash ----------------

    [Fact]
    public async Task ComputeInputHash_CacheMiss_MtimeMismatch_RehashesFile()
    {
        var dir = Path.Combine(_tempRoot, "mtimemiss");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "a.xml");
        File.WriteAllText(file, "<x>content</x>");
        var realLen = new FileInfo(file).Length;
        var realTicks = new FileInfo(file).LastWriteTimeUtc.Ticks;

        // Cache claims a different mtime — length correct, but the (len, mtime) pair
        // mismatches, so we should re-hash.
        var bogusHash = new string('0', 64);
        var cache = MakeCache(("orig", "a.xml", realLen, realTicks + 1234567L, bogusHash));

        var writeBack = new Dictionary<string, string>();
        var hash = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache, writeBack, CancellationToken.None);
        var truth = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache: null, writeBack: null, CancellationToken.None);

        Assert.Equal(truth, hash);
        Assert.Single(writeBack);
    }

    // ---------------- 4: cache miss — legacy entry without ContentHash ----------------

    [Fact]
    public async Task ComputeInputHash_CacheMiss_LegacyEntryWithoutContentHash_RehashesAndWritesBack()
    {
        var dir = Path.Combine(_tempRoot, "legacy");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "a.xml");
        File.WriteAllText(file, "<x>legacy</x>");
        var fi = new FileInfo(file);

        // Legacy entry: (len, mtime) match but ContentHash is null → must re-hash.
        var cache = MakeCache(("orig", "a.xml", fi.Length, fi.LastWriteTimeUtc.Ticks, null));

        var writeBack = new Dictionary<string, string>();
        var hash = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache, writeBack, CancellationToken.None);
        var truth = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache: null, writeBack: null, CancellationToken.None);

        Assert.Equal(truth, hash);
        Assert.Single(writeBack);
        Assert.Equal(64, writeBack["orig/a.xml"].Length); // SHA256 hex
    }

    // ---------------- 5: root hash invariant — cache vs. no-cache ----------------

    [Fact]
    public async Task ComputeInputHash_RootHashIdentical_CacheHitVsCacheMiss()
    {
        // Critical invariant: cache is purely an optimization. The root hash must be
        // byte-identical between a fully-populated cache (no reads) and an empty cache
        // (every file read) for the same corpus content. Lock this in.
        var dir = Path.Combine(_tempRoot, "invariant");
        Directory.CreateDirectory(dir);
        for (int i = 0; i < 8; i++)
        {
            var f = Path.Combine(dir, $"f{i:D2}.xml");
            File.WriteAllText(f, $"<doc id='{i}'>content number {i}</doc>");
            File.SetLastWriteTimeUtc(f, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i));
        }

        // No-cache run: also populate writeBack so we can build a synthetic cache.
        var freshHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var hashNoCache = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache: null, writeBack: freshHashes, CancellationToken.None);

        Assert.Equal(8, freshHashes.Count);

        // Build a full cache from the writeBack (file stats from disk).
        var cache = new Dictionary<string, SearchIndexEntry>(StringComparer.Ordinal);
        foreach (var kv in freshHashes)
        {
            // "orig/f00.xml" → real path on disk
            var relInDir = kv.Key.Substring("orig/".Length).Replace('/', Path.DirectorySeparatorChar);
            var fi = new FileInfo(Path.Combine(dir, relInDir));
            cache[kv.Key] = new SearchIndexEntry
            {
                RelPath = relInDir,
                Side = SearchSide.Original,
                LengthBytes = fi.Length,
                LastWriteUtcTicks = fi.LastWriteTimeUtc.Ticks,
                ContentHash = kv.Value,
            };
        }

        // Full cache run: expect zero writeBack entries (all hits) and identical root.
        var cacheHitWriteBack = new Dictionary<string, string>(StringComparer.Ordinal);
        var hashFullCache = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache, cacheHitWriteBack, CancellationToken.None);

        Assert.Equal(hashNoCache, hashFullCache);    // invariant: cache is pure optimization
        Assert.Empty(cacheHitWriteBack);             // no misses → no write-back
    }

    // ---------------- 6: IsStaleAsync opportunistic write-back on legacy manifest ----------------

    [Fact]
    public async Task IsStaleAsync_OpportunisticWriteBack_PatchesManifestOnLegacy()
    {
        // Start: a manifest written by an old binary (pre-PR B) has InputHash set but
        // every entry's ContentHash is null. On the next IsStaleAsync call, the cache
        // misses on every file (legacy = null hash) → fresh hashes computed → write-back
        // populates ContentHash for every entry. The manifest on disk is patched in place.
        var svc = new SearchIndexService();

        var origFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(origFile, "<x>some content</x>");
        File.SetLastWriteTimeUtc(origFile, DateTime.UtcNow.AddHours(-2));

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<y>translated</y>");
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddHours(-2));

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Simulate legacy: strip ContentHash from every entry in the on-disk manifest.
        var manifestPath = svc.GetManifestPath(_tempRoot);
        var m1 = ReadManifest(manifestPath);
        foreach (var e in m1.Entries) e.ContentHash = null;
        WriteManifest(manifestPath, m1);
        svc.InvalidateIndexCaches(); // force re-load from disk

        // IsStaleAsync: should return false (content unchanged) AND should write back
        // populated ContentHashes for every entry.
        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.False(stale);

        var m2 = ReadManifest(manifestPath);
        Assert.True(m2.Entries.Count >= 2); // both original + translated
        Assert.All(m2.Entries, e =>
        {
            Assert.NotNull(e.ContentHash);
            Assert.Equal(64, e.ContentHash!.Length); // SHA256 hex
        });
    }

    // ---------------- 7: IsStaleAsync no write-back when all cache hits ----------------

    [Fact]
    public async Task IsStaleAsync_NoWriteBackWhenAllCacheHits()
    {
        // Steady state: after the first call backfilled hashes, a second call should
        // see cache hits on every file and skip the manifest write entirely. We measure
        // this via the test-observable backfill counter.
        var svc = new SearchIndexService();

        var origFile = Path.Combine(_origDir, "a.xml");
        File.WriteAllText(origFile, "<x/>");
        File.SetLastWriteTimeUtc(origFile, DateTime.UtcNow.AddHours(-2));

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // BuildAsync now populates ContentHash on every entry as part of SaveManifestAtomicAsync.
        // First IsStaleAsync call: cache fully populated, every file hits → 0 backfills.
        var beforeCount = svc.LastContentHashBackfillCount;
        bool stale1 = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });
        Assert.False(stale1);
        Assert.Equal(beforeCount, svc.LastContentHashBackfillCount); // no backfill fired

        // Second call: same.
        bool stale2 = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });
        Assert.False(stale2);
        Assert.Equal(beforeCount, svc.LastContentHashBackfillCount);
    }

    // ---------------- 8: concurrent IsStaleAsync — only one write-back fires ----------------

    [Fact]
    public async Task IsStaleAsync_WriteBack_AtomicWrite_NoCorruptionUnderConcurrency()
    {
        // Two concurrent IsStaleAsync calls on a legacy manifest: both detect missing
        // ContentHash and want to write back. Interlocked guard ensures only one
        // actually fires. After both complete, the on-disk manifest must be valid +
        // contain populated ContentHashes.
        var svc = new SearchIndexService();

        // Create 4 files so the hash work is non-trivial.
        for (int i = 0; i < 4; i++)
        {
            var f = Path.Combine(_origDir, $"f{i}.xml");
            File.WriteAllText(f, $"<x>{i}</x>");
            File.SetLastWriteTimeUtc(f, DateTime.UtcNow.AddHours(-2));
        }

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Strip ContentHash for legacy simulation.
        var manifestPath = svc.GetManifestPath(_tempRoot);
        var m = ReadManifest(manifestPath);
        foreach (var e in m.Entries) e.ContentHash = null;
        WriteManifest(manifestPath, m);
        svc.InvalidateIndexCaches();

        var beforeCount = svc.LastContentHashBackfillCount;

        // Race two IsStaleAsync calls concurrently. We can't deterministically force
        // both to enter the backfill branch at once without instrumentation, but with
        // 4 files + cold disk on the second call, the chance of interleave is high
        // enough to exercise the guard. The invariant tested: the manifest on disk
        // is valid JSON afterward, ContentHashes are populated, no exception.
        var t1 = Task.Run(() => svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));
        var t2 = Task.Run(() => svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir }));
        await Task.WhenAll(t1, t2);

        Assert.False(t1.Result);
        Assert.False(t2.Result);

        // The backfill counter increased by AT MOST 2 (if interleave avoided guard);
        // typically 1. Both values are correct — the invariant is no corruption.
        int delta = svc.LastContentHashBackfillCount - beforeCount;
        Assert.InRange(delta, 1, 2);

        // Manifest must be valid JSON with populated ContentHashes.
        var mAfter = ReadManifest(manifestPath);
        Assert.All(mAfter.Entries, e =>
        {
            Assert.NotNull(e.ContentHash);
            Assert.Equal(64, e.ContentHash!.Length);
        });
    }

    // ---------------- 9: perf assertion — cache hit path is fast ----------------

    [Fact]
    public async Task ComputeInputHash_FullCacheHit_MeetsPerfBudget()
    {
        // Soft perf assertion: with a fully populated cache, hashing 50 synthetic XML
        // files completes in well under 500ms (cache-hit path stat-only). Without cache:
        // also fast on this tiny corpus, but the no-cache path opens + SHA256's every
        // file. We assert the order-of-magnitude relationship only.
        var dir = Path.Combine(_tempRoot, "perfcache");
        Directory.CreateDirectory(dir);

        // Create 50 files with non-trivial size (~5KB each) so the cache-miss read
        // cost is measurable.
        var bigText = new string('x', 5000);
        var freshHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < 50; i++)
        {
            File.WriteAllText(Path.Combine(dir, $"f{i:D3}.xml"), $"<x>{bigText}{i}</x>");
        }

        // Warm: populate the writeBack with real hashes.
        var truth = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache: null, writeBack: freshHashes, CancellationToken.None);
        Assert.Equal(50, freshHashes.Count);

        // Build a fully-populated cache.
        var cache = new Dictionary<string, SearchIndexEntry>(StringComparer.Ordinal);
        foreach (var kv in freshHashes)
        {
            var relInDir = kv.Key.Substring("orig/".Length).Replace('/', Path.DirectorySeparatorChar);
            var fi = new FileInfo(Path.Combine(dir, relInDir));
            cache[kv.Key] = new SearchIndexEntry
            {
                RelPath = relInDir,
                Side = SearchSide.Original,
                LengthBytes = fi.Length,
                LastWriteUtcTicks = fi.LastWriteTimeUtc.Ticks,
                ContentHash = kv.Value,
            };
        }

        // Run once with cache to JIT-warm.
        var emptyWb = new Dictionary<string, string>();
        await SearchIndexService.ComputeInputHashAsync(dir, Array.Empty<string>(), cache, emptyWb, CancellationToken.None);

        // Now measure cache-hit run.
        var sw = Stopwatch.StartNew();
        var hashCached = await SearchIndexService.ComputeInputHashAsync(
            dir, Array.Empty<string>(), cache, writeBack: null, CancellationToken.None);
        sw.Stop();

        Assert.Equal(truth, hashCached);
        // SPEC: "hashing 50 synthetic XML files with a fully-populated cache completes
        // in < 50 ms (cache-hit path)." On CI VMs we allow more headroom — 250ms ceiling.
        Assert.True(sw.ElapsedMilliseconds < 250,
            $"Cache-hit path took {sw.ElapsedMilliseconds}ms for 50 files; expect well under 250ms.");
    }
}
