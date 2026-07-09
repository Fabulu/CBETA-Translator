using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// PERF (E): the >20% delta guard. When an incremental build's changed+removed set
/// exceeds <c>IncrementalFullRebuildDeltaThreshold</c> (0.20) of the corpus, the
/// incremental path is abandoned and a clean full rebuild runs instead — the per-entry
/// incremental overhead no longer pays off near a wholesale change. The guard is never
/// wrong (a full rebuild is always a correct index), only a speed choice, so these tests
/// assert the ROUTING (which path ran) via the test-observable counters, not artifact
/// contents (equivalence of the two paths is already covered by IncrementalEquivalenceTests).
///
/// Fixture: 36 files. 0.20 * 36 = 7.2, so &gt;7 changed/removed entries trip the guard.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class IncrementalDeltaGuardTests
{
    // ── Above threshold: 10 changed files (10/36 = 28% > 20%) → full rebuild ──
    [Fact]
    public async Task LargeDelta_TripsGuard_RunsFullRebuild()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        Assert.Equal(fx.TotalFileCount, svc.LastBuildXmlReadCount); // full baseline read everything

        // Change 10 distinct both-sides rels (orig side each) — 10 of 36 files.
        for (int i = 0; i < 10; i++)
            fx.ChangeFile(fx.BothSidesRels[i]);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

        // Guard tripped → clean full rebuild ran (every XML re-read), and it is NOT counted
        // as an S5 fault fallback.
        Assert.Equal(1, svc.LastBuildDeltaGuardTripped);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.Equal(fx.TotalFileCount, svc.LastBuildXmlReadCount);
    }

    // ── Below threshold: 1 changed file (1/36 ≈ 3% < 20%) → incremental path ──
    [Fact]
    public async Task SmallDelta_DoesNotTripGuard_RunsIncremental()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.ChangeFile(fx.BothSidesRels[0]); // exactly one file

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

        // Guard did NOT trip; the incremental skip-read path ran (only the changed XML read).
        Assert.Equal(0, svc.LastBuildDeltaGuardTripped);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        Assert.Equal(1, svc.LastBuildXmlReadCount);
    }
}
