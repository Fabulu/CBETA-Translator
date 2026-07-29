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
/// Characterization test for the TermbaseService personal-termbase cache (audit item
/// P2.6, mtime-stamped). It pins: the personal termbase is served from cache while its
/// mtime is unchanged, and InvalidateCache drops it.
/// The community-termbase cache tests were retired — personal termbases are now
/// local-only, so community termbase reading (FindCommunityTermsAsync) was removed.
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

    // Minimal ITermbaseStorageService stub for the personal-termbase cache test.
    // The community-term READING tests (FindCommunityTerms_*) were retired: personal
    // termbases are local-only, so FindCommunityTermsAsync / LoadAllCommunityJsonlAsync
    // no longer exist.
    private sealed class CountingStorage2 : ITermbaseStorageService
    {
        public Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default) => Task.FromResult(new List<TermbaseEntry>());
        public Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<TermbaseEntry>> LoadUserAsync(string root, string username, CancellationToken ct = default) => Task.FromResult(new List<TermbaseEntry>());
        public Task SaveUserAsync(string root, string username, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
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
