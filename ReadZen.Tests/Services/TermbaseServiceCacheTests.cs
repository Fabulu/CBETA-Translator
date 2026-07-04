using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Characterization tests for the TermbaseService caches introduced in audit item
/// P2.6 (stat-stamp community cache + mtime personal cache), which shipped untested
/// (P3.7). They pin: the community jsonl is NOT re-read on every segment change, the
/// stat stamp re-reads when files change, the personal termbase is served from cache
/// while its mtime is unchanged, and InvalidateCache drops both.
/// </summary>
[Trait("Domain", "Termbase")]
public sealed class TermbaseServiceCacheTests : IDisposable
{
    private readonly string _root;

    public TermbaseServiceCacheTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-termcache-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static CurrentSegmentContext Ctx(string zh) => new()
    {
        RelPath = "T/T48/T48n2005.xml",
        ZhText = zh,
        BlockNumber = 1,
    };

    // TermbaseService calls _storage.LoadAllCommunityJsonlAsync through the interface;
    // this direct interface implementation counts those calls (StubTermbaseStorageService's
    // method is non-virtual, so subclassing + `new` would not intercept the interface slot).
    private sealed class CountingStorage2 : ITermbaseStorageService
    {
        public Dictionary<string, List<TermbaseEntry>> Community { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int CommunityLoadCount { get; private set; }

        public Task<Dictionary<string, List<TermbaseEntry>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default)
        {
            CommunityLoadCount++;
            return Task.FromResult(Community.ToDictionary(kv => kv.Key, kv => new List<TermbaseEntry>(kv.Value), StringComparer.OrdinalIgnoreCase));
        }

        // Unused by these tests.
        public Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default) => Task.FromResult(new List<TermbaseEntry>());
        public Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<TermbaseEntry>> LoadUserAsync(string root, string username, CancellationToken ct = default) => Task.FromResult(new List<TermbaseEntry>());
        public Task SaveUserAsync(string root, string username, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
        public Task WriteUserJsonlAsync(string communityDir, string username, List<TermbaseEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
    }

    private string MakeCommunityFile(string username, string content = "{}")
    {
        var dir = Path.Combine(_root, "community", "termbases");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, username + ".jsonl");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task FindCommunityTerms_DoesNotRescanDiskOnEveryCall()
    {
        var file = MakeCommunityFile("alice");
        var storage = new CountingStorage2();
        storage.Community["alice"] = new() { new TermbaseEntry { SourceTerm = "甲乙", PreferredTarget = "X" } };
        var svc = new TermbaseService(storage);

        var first = await svc.FindCommunityTermsAsync(Ctx("甲乙丙"), _root);
        var second = await svc.FindCommunityTermsAsync(Ctx("甲乙丙"), _root);

        Assert.Contains(first, h => h.SourceTerm == "甲乙");
        Assert.Contains(second, h => h.SourceTerm == "甲乙");
        // The community jsonl was loaded ONCE; the second call served from cache
        // (audit P2.6 / R3-M8: this used to re-read every jsonl on every segment change).
        Assert.Equal(1, storage.CommunityLoadCount);
    }

    [Fact]
    public async Task FindCommunityTerms_ReloadsWhenAFileChanges()
    {
        var file = MakeCommunityFile("alice");
        var storage = new CountingStorage2();
        storage.Community["alice"] = new() { new TermbaseEntry { SourceTerm = "甲乙", PreferredTarget = "X" } };
        var svc = new TermbaseService(storage);

        await svc.FindCommunityTermsAsync(Ctx("甲乙"), _root);
        Assert.Equal(1, storage.CommunityLoadCount);

        // A newer mtime on a community file invalidates the stat stamp.
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddHours(1));
        await svc.FindCommunityTermsAsync(Ctx("甲乙"), _root);
        Assert.Equal(2, storage.CommunityLoadCount);

        // Adding a file also changes the stamp (file count).
        MakeCommunityFile("bob");
        await svc.FindCommunityTermsAsync(Ctx("甲乙"), _root);
        Assert.Equal(3, storage.CommunityLoadCount);
    }

    [Fact]
    public async Task FindCommunityTerms_InvalidateCache_ForcesReload()
    {
        MakeCommunityFile("alice");
        var storage = new CountingStorage2();
        storage.Community["alice"] = new() { new TermbaseEntry { SourceTerm = "甲乙", PreferredTarget = "X" } };
        var svc = new TermbaseService(storage);

        await svc.FindCommunityTermsAsync(Ctx("甲乙"), _root);
        await svc.FindCommunityTermsAsync(Ctx("甲乙"), _root);
        Assert.Equal(1, storage.CommunityLoadCount);

        svc.InvalidateCache();
        await svc.FindCommunityTermsAsync(Ctx("甲乙"), _root);
        Assert.Equal(2, storage.CommunityLoadCount);
    }

    [Fact]
    public async Task FindTerms_PersonalTermbase_IsServedFromCacheWhileMtimeUnchanged()
    {
        var path = Path.Combine(_root, "termbase.json");
        File.WriteAllText(path,
            "[{\"sourceTerm\":\"甲乙\",\"preferredTarget\":\"CACHED\",\"alternateTargets\":[],\"status\":\"\",\"note\":\"\"}]");
        var mtime = File.GetLastWriteTimeUtc(path);

        var svc = new TermbaseService(new CountingStorage2());
        var first = await svc.FindTermsAsync(Ctx("甲乙丙"), _root);
        Assert.Contains(first, h => h.PreferredTarget == "CACHED");

        // Corrupt the file but RESTORE its mtime: a cache HIT returns the cached rows
        // and ignores the now-unparseable file; a re-read would yield nothing.
        File.WriteAllText(path, "not valid json {{{");
        File.SetLastWriteTimeUtc(path, mtime);

        var second = await svc.FindTermsAsync(Ctx("甲乙丙"), _root);
        Assert.Contains(second, h => h.PreferredTarget == "CACHED");

        // After InvalidateCache the corrupt file IS re-read -> no terms.
        svc.InvalidateCache();
        var third = await svc.FindTermsAsync(Ctx("甲乙丙"), _root);
        Assert.DoesNotContain(third, h => h.PreferredTarget == "CACHED");
    }
}
