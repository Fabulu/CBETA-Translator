using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-M2 (SPEC §2.3, §8): master bundle adoption
/// (<see cref="MasterCorpusSearchService.TryAdoptBundleAsync"/>). Proves the adopt
/// decision permutations from the SPEC decision table — cache absent/stale/fresh ×
/// bundle match/mismatch/corrupt/absent — plus the data-loss guards (never overwrite a
/// FRESH local cache, fully replace a STALE one), atomicity (tmp+rename, no partial file
/// on failure), and that the stamp read is CHEAP (a bounded-prefix scan that never
/// full-deserializes the ~57 MB appearances array).
///
/// TryAdoptBundleAsync compares stamps by STRING EQUALITY only (it does not recompute a
/// live stamp), so these tests use synthetic stamp strings and real temp files — fully
/// deterministic, no wall-clock or corpus dependence.
/// </summary>
public class MasterCorpusAdoptionTests : IDisposable
{
    private const string CacheFileName = "master-corpus-index.json"; // mirrors the private const

    // Plausible v2 composite stamps. Only string identity matters to the adopt decision.
    private const string LiveStamp =
        "v2;corpus=files=924;bytes=123456;pathsig=aaaaaaaaaaaaaaaa;titles=bbbbbbbbbbbbbbbb;roster=count=924;hash=cccccccccccccccc";
    private const string OtherStamp =
        "v2;corpus=files=924;bytes=999999;pathsig=dddddddddddddddd;titles=eeeeeeeeeeeeeeee;roster=count=924;hash=ffffffffffffffff";

    private readonly string _tempRoot;

    public MasterCorpusAdoptionTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-madopt-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    // ---- fixture helpers --------------------------------------------------------------

    private string NewDir(string tag)
    {
        var p = Path.Combine(_tempRoot, tag + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(p);
        return p;
    }

    private static string CachePath(string cacheDir) => Path.Combine(cacheDir, CacheFileName);

    /// <summary>Serializes a real MasterCorpusIndex (compact, as the shipped bundle is) to
    /// <paramref name="path"/>, carrying <paramref name="stamp"/> and a distinguishing
    /// <paramref name="masterCount"/> so a byte/content compare can tell two files apart.</summary>
    private static void WriteIndexFile(string path, string? stamp, int masterCount = 2, int appearances = 1)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

        var index = new MasterCorpusIndex
        {
            BuiltUtc = "2026-07-23T00:00:00.0000000Z",
            Corpus = "Cbeta",
            CorpusStamp = stamp,
            FileCount = 924,
            MasterCount = masterCount,
        };
        for (int i = 0; i < appearances; i++)
        {
            index.Appearances.Add(new MasterTextAppearance
            {
                MasterName = "Master " + i,
                MatchedName = "M" + i,
                RelPath = $"T/T48/T48n{2000 + i}.xml",
                AppearanceType = i % 2 == 0 ? "primary" : "secondary",
                MentionCount = i + 1,
                Snippet = "context snippet for appearance number " + i + " padded padded padded",
            });
        }

        var json = JsonSerializer.Serialize(index);
        File.WriteAllText(path, json, new UTF8Encoding(false));
    }

    private static int LoadedMasterCount(string path)
    {
        var idx = JsonSerializer.Deserialize<MasterCorpusIndex>(File.ReadAllText(path));
        return idx!.MasterCount;
    }

    private static bool BytesEqual(string a, string b)
        => File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));

    private static int TmpFileCount(string cacheDir)
        => Directory.Exists(cacheDir)
            ? Directory.EnumerateFiles(cacheDir, CacheFileName + ".tmp-*").Count()
            : 0;

    // =====================================================================================
    // Decision table — cache ABSENT
    // =====================================================================================

    [Fact]
    public async Task CacheAbsent_BundleMatchesLive_Adopts()
    {
        var cacheDir = Path.Combine(_tempRoot, "absent-cache-" + Guid.NewGuid().ToString("N")[..8]); // not created
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.True(adopted);
        Assert.True(File.Exists(CachePath(cacheDir)));
        Assert.True(BytesEqual(bundle, CachePath(cacheDir)));   // byte-identical copy
        Assert.Equal(0, TmpFileCount(cacheDir));                // tmp cleaned up
    }

    [Fact]
    public async Task CacheAbsent_BundleStampMismatch_DoesNotAdopt()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, OtherStamp);   // bundle is for a DIFFERENT corpus/roster

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);
        Assert.False(File.Exists(CachePath(cacheDir)));   // nothing written → falls to rebuild
    }

    [Fact]
    public async Task CacheAbsent_BundleFileMissing_DoesNotAdopt()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(_tempRoot, "does-not-exist", CacheFileName);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    [Fact]
    public async Task CacheAbsent_BundleCorruptBytes_DoesNotAdopt()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        File.WriteAllBytes(bundle, new byte[] { 0x00, 0x01, 0x02, 0xFF, 0x7B, 0x7B }); // not JSON

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);   // cheap stamp read yields null ≠ live → skip
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    [Fact]
    public async Task CacheAbsent_BundleValidJsonButNoStamp_DoesNotAdopt()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        File.WriteAllText(bundle, "{\"built_utc\":\"x\",\"corpus\":\"Cbeta\",\"file_count\":1}",
            new UTF8Encoding(false));

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);   // no corpus_stamp property → null ≠ live → skip
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    // =====================================================================================
    // Decision table — cache STALE
    // =====================================================================================

    [Fact]
    public async Task CacheStale_BundleMatchesLive_AdoptsOverLocal()
    {
        var cacheDir = NewDir("cache");
        WriteIndexFile(CachePath(cacheDir), OtherStamp, masterCount: 279); // the stale local cache
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.True(adopted);
        Assert.True(BytesEqual(bundle, CachePath(cacheDir)));
    }

    [Fact]
    public async Task CacheStale_BundleStampMismatch_DoesNotAdopt_LocalUntouched()
    {
        var cacheDir = NewDir("cache");
        WriteIndexFile(CachePath(cacheDir), OtherStamp, masterCount: 279);
        var before = File.ReadAllBytes(CachePath(cacheDir));

        // Bundle is ALSO not the live index (a third stamp): neither adopt nor overwrite.
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, "v2;some=third;stamp=entirely;roster=count=1;hash=0000000000000000");

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);   // → caller keeps local + rebuild path (rows 3/6)
        Assert.Equal(before, File.ReadAllBytes(CachePath(cacheDir)));
    }

    // =====================================================================================
    // Decision table — cache FRESH (data-loss guard: never overwrite a live-matching cache)
    // =====================================================================================

    [Fact]
    public async Task CacheFresh_BundleMatchesLive_Untouched_Row1()
    {
        var cacheDir = NewDir("cache");
        WriteIndexFile(CachePath(cacheDir), LiveStamp, masterCount: 924); // local is already fresh
        var before = File.ReadAllBytes(CachePath(cacheDir));

        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924, appearances: 5); // different bytes, same stamp

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);   // keep local, no copy (decision-table row 1)
        Assert.Equal(before, File.ReadAllBytes(CachePath(cacheDir)));
    }

    [Fact]
    public async Task CacheFresh_BundleStampMismatch_Untouched()
    {
        var cacheDir = NewDir("cache");
        WriteIndexFile(CachePath(cacheDir), LiveStamp, masterCount: 924);
        var before = File.ReadAllBytes(CachePath(cacheDir));

        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, OtherStamp);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);
        Assert.Equal(before, File.ReadAllBytes(CachePath(cacheDir)));
    }

    // =====================================================================================
    // Guards — no live stamp / no bundle path (mirror TryLoadAsync's "not enforced" branch)
    // =====================================================================================

    [Fact]
    public async Task NullLiveStamp_NeverAdopts()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, null, CancellationToken.None);

        Assert.False(adopted);
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    [Fact]
    public async Task EmptyLiveStamp_NeverAdopts()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, "");   // even a bundle whose stamp is "" must not adopt

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, "", CancellationToken.None);

        Assert.False(adopted);
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    [Fact]
    public async Task EmptyBundlePath_NeverAdopts()
    {
        var cacheDir = NewDir("cache");

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, "", LiveStamp, CancellationToken.None);

        Assert.False(adopted);
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    // =====================================================================================
    // Data-loss / correctness guards
    // =====================================================================================

    [Fact]
    public async Task AdoptOverStale_FullyReplacesLocalContent_NotMerged()
    {
        var cacheDir = NewDir("cache");
        WriteIndexFile(CachePath(cacheDir), OtherStamp, masterCount: 279, appearances: 10);
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924, appearances: 3);

        Assert.True(await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None));

        // The adopted cache is the bundle in full — the stale 279 content is gone entirely.
        Assert.Equal(924, LoadedMasterCount(CachePath(cacheDir)));
        Assert.True(BytesEqual(bundle, CachePath(cacheDir)));
    }

    [Fact]
    public async Task AdoptedCache_DeserializesAndServesViaTryLoad()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924, appearances: 4);

        var svc = new MasterCorpusSearchService();
        Assert.True(await svc.TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None));

        // The adopted file is a valid MasterCorpusIndex the loader serves (freshness off).
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parentRootForFreshness: null, rosterIdentity: null);
        Assert.NotNull(loaded);
        Assert.Equal(924, loaded!.MasterCount);
        Assert.Equal(4, loaded.Appearances.Count);
        Assert.Equal(LiveStamp, loaded.CorpusStamp);
    }

    [Fact]
    public async Task SecondAdopt_AfterFirst_IsNoOp_LocalNowFresh()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924);
        var svc = new MasterCorpusSearchService();

        Assert.True(await svc.TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None));
        var afterFirst = File.ReadAllBytes(CachePath(cacheDir));

        // Second launch: local cache now equals live → row 1 keep-local, no copy.
        Assert.False(await svc.TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None));
        Assert.Equal(afterFirst, File.ReadAllBytes(CachePath(cacheDir)));
        Assert.Equal(0, TmpFileCount(cacheDir));
    }

    // =====================================================================================
    // Atomicity — tmp+rename, no partial file, tmp always cleaned
    // =====================================================================================

    [Fact]
    public async Task SuccessfulAdopt_LeavesNoTmpFiles()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, appearances: 50);

        Assert.True(await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None));
        Assert.Equal(0, TmpFileCount(cacheDir));
    }

    [Fact]
    public async Task CopyFailure_LeavesNoPartialCache_AndCleansTmp()
    {
        var cacheDir = NewDir("cache");
        // Inject a failure at the final rename by occupying the cache PATH with a directory:
        // ReadCorpusStampCheap sees no file (null stamp → proceeds), the tmp copy succeeds,
        // but File.Move onto an existing directory throws → catch deletes tmp → returns false.
        Directory.CreateDirectory(CachePath(cacheDir));

        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);
        Assert.True(Directory.Exists(CachePath(cacheDir)));  // pre-existing state untouched
        Assert.Equal(0, TmpFileCount(cacheDir));             // no partial tmp left behind
    }

    [Fact]
    public async Task CancelledToken_DoesNotAdopt_StaleLocalUntouched_NoTmp()
    {
        var cacheDir = NewDir("cache");
        WriteIndexFile(CachePath(cacheDir), OtherStamp, masterCount: 279);
        var before = File.ReadAllBytes(CachePath(cacheDir));

        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, appearances: 200);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, cts.Token);

        Assert.False(adopted);                                   // CopyToAsync observes cancellation
        Assert.Equal(before, File.ReadAllBytes(CachePath(cacheDir)));
        Assert.Equal(0, TmpFileCount(cacheDir));
    }

    // =====================================================================================
    // Cheap stamp read — decision never full-deserializes the appearances array
    // =====================================================================================

    [Fact]
    public async Task MatchBundle_WithMalformedTail_AdoptsViaByteCopy_NotDeserialize()
    {
        // corpus_stamp precedes a deliberately BROKEN appearances tail. A full deserialize
        // would throw here; the cheap prefix reader returns the stamp before reaching it, and
        // adoption copies raw bytes without parsing. Proves "never full-deserialize" (SPEC §2.3).
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        File.WriteAllText(bundle,
            "{\"built_utc\":\"x\",\"corpus\":\"Cbeta\",\"corpus_stamp\":\"" + LiveStamp +
            "\",\"appearances\":[ this is not valid json at all {{{{ ",
            new UTF8Encoding(false));

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.True(adopted);
        Assert.True(BytesEqual(bundle, CachePath(cacheDir)));  // raw byte copy of the malformed file
    }

    [Fact]
    public async Task MismatchBundle_WithMalformedTail_DecidesWithoutThrowing()
    {
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        File.WriteAllText(bundle,
            "{\"built_utc\":\"x\",\"corpus\":\"Cbeta\",\"corpus_stamp\":\"" + OtherStamp +
            "\",\"appearances\":[ garbage {{{{ ",
            new UTF8Encoding(false));

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.False(adopted);   // stamp read succeeds (≠ live), no exception, no adopt
        Assert.False(File.Exists(CachePath(cacheDir)));
    }

    [Fact]
    public async Task LargeBundle_StampFoundInPrefix_Adopts()
    {
        // A realistically large bundle (appearances array well over the 256 KB prefix window):
        // corpus_stamp is the 3rd top-level property, so the bounded-prefix scan finds it
        // long before the array — adoption still succeeds without reading the whole file.
        var cacheDir = NewDir("cache");
        var bundle = Path.Combine(NewDir("bundle"), CacheFileName);
        WriteIndexFile(bundle, LiveStamp, masterCount: 924, appearances: 4000);
        Assert.True(new FileInfo(bundle).Length > 256 * 1024);

        var adopted = await new MasterCorpusSearchService()
            .TryAdoptBundleAsync(cacheDir, bundle, LiveStamp, CancellationToken.None);

        Assert.True(adopted);
        Assert.True(BytesEqual(bundle, CachePath(cacheDir)));
    }
}
