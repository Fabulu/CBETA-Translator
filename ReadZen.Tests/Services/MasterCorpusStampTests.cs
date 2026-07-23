using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-M1: master roster-aware composite stamp v2
/// (<c>v2;corpus=files=..;bytes=..;pathsig=..;titles=..;roster=count=..;hash=..</c>).
/// Proves the stamp is mtime-immune, change-precise across every documented input
/// (corpus add/remove/resize/rename, titles.jsonl edit, roster add/rename/alias/date),
/// stable under reorder, and that <see cref="MasterCorpusSearchService.TryLoadAsync"/>
/// serves only on an exact live match — the guard that fixes the "279 of 944" stale-cache
/// bug class (SPEC §1.2, §8 PR-M1). Uses real temp corpora in the BundleSeedTests style.
/// All inputs are content/structure only, so every assertion is wall-clock independent.
/// </summary>
public class MasterCorpusStampTests : IDisposable
{
    private readonly string _tempRoot;

    public MasterCorpusStampTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-mstamp-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        AppPaths.InvalidateDiscoveryCache();
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private const string SampleXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><teiHeader><author>臨濟</author></teiHeader>" +
        "<text><body><p>臨濟義玄禪師示眾云</p></body></text></TEI>\n";

    // ---- corpus fixture helpers -------------------------------------------------------

    /// <summary>
    /// Builds a CBETA-shaped corpus under a fresh parent root:
    /// {parent}/CbetaZenTexts/xml-p5/*.xml (originals the stamp walks) and
    /// {parent}/CbetaZenTranslations/{xml-p5t, titles.jsonl}. Returns the paths and
    /// invalidates the discovery cache so the just-created layout is seen.
    /// </summary>
    private (string parent, string origDir, string transRepo) NewCorpus(
        int fileCount = 3, bool withTitles = true)
    {
        var parent = Path.Combine(_tempRoot, "p-" + Guid.NewGuid().ToString("N")[..8]);
        var origDir = Path.Combine(parent, "CbetaZenTexts", "xml-p5");
        var transRepo = Path.Combine(parent, "CbetaZenTranslations");
        var transDir = Path.Combine(transRepo, "xml-p5t");
        Directory.CreateDirectory(origDir);
        Directory.CreateDirectory(transDir);

        for (int i = 0; i < fileCount; i++)
            File.WriteAllText(Path.Combine(origDir, $"T{i:0000}.xml"), SampleXml, new UTF8Encoding(false));

        if (withTitles)
            WriteTitles(transRepo, "{\"path\":\"T0000.xml\",\"zh\":\"標題\",\"en\":\"Title\"}");

        AppPaths.InvalidateDiscoveryCache(parent);
        return (parent, origDir, transRepo);
    }

    private static void WriteTitles(string transRepo, params string[] lines)
        => File.WriteAllText(Path.Combine(transRepo, "titles.jsonl"),
            string.Join("\n", lines) + "\n", new UTF8Encoding(false));

    private static string FirstXml(string origDir)
        => Directory.EnumerateFiles(origDir, "*.xml").OrderBy(p => p, StringComparer.Ordinal).First();

    // ---- roster fixture helpers -------------------------------------------------------

    private static ZenMasterRecord Rec(string canonical, string[] aliases, int floruit, int death)
        => new()
        {
            CanonicalName = canonical,
            Aliases = aliases.ToList(),
            Variants = new()
            {
                new ZenMasterVariant
                {
                    Names = aliases.ToList(),
                    Floruit = floruit,
                    Death = death,
                    IsBase = true,
                },
            },
        };

    private static ZenMasterCatalog Catalog(params ZenMasterRecord[] records)
        => new() { Records = records.ToList() };

    private static ZenMasterCatalog SampleRosterA() => Catalog(
        Rec("Linji Yixuan", new[] { "臨濟", "義玄", "臨濟義玄" }, 810, 866),
        Rec("Zhaozhou Congshen", new[] { "趙州", "從諗" }, 778, 897));

    // =====================================================================================
    // Corpus half — mtime immunity + change precision
    // =====================================================================================

    [Fact]
    public void ComputeCorpusStamp_NullWhenNoCorpusDirs()
    {
        var empty = Path.Combine(_tempRoot, "no-corpus-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(empty);
        AppPaths.InvalidateDiscoveryCache(empty);
        Assert.Null(MasterCorpusSearchService.ComputeCorpusStamp(empty));
    }

    [Fact]
    public void Stamp_IsMtimeImmune()
    {
        var (parent, origDir, _) = NewCorpus();
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        Assert.NotNull(before);

        // Rewrite every file's mtime to an arbitrary distant time (the git-clone/pull effect).
        var poison = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        foreach (var f in Directory.EnumerateFiles(origDir, "*.xml"))
            File.SetLastWriteTimeUtc(f, poison);

        var after = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Stamp_IsDeterministic_AcrossIdenticalCorpora()
    {
        // Two independent corpora with byte-identical files and no titles must yield the
        // identical corpus stamp (proves cross-machine/clone reproducibility).
        var (parentA, _, _) = NewCorpus(fileCount: 3, withTitles: false);
        var (parentB, _, _) = NewCorpus(fileCount: 3, withTitles: false);
        Assert.Equal(
            MasterCorpusSearchService.ComputeCorpusStamp(parentA),
            MasterCorpusSearchService.ComputeCorpusStamp(parentB));
    }

    [Fact]
    public void Stamp_FlipsOnFileAdd()
    {
        var (parent, origDir, _) = NewCorpus();
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        File.WriteAllText(Path.Combine(origDir, "added.xml"), SampleXml, new UTF8Encoding(false));
        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    [Fact]
    public void Stamp_FlipsOnFileRemove()
    {
        var (parent, origDir, _) = NewCorpus();
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        File.Delete(FirstXml(origDir));
        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    [Fact]
    public void Stamp_FlipsOnFileResize()
    {
        var (parent, origDir, _) = NewCorpus();
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        File.WriteAllText(FirstXml(origDir), SampleXml + "<!-- grown -->\n", new UTF8Encoding(false));
        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    [Fact]
    public void Stamp_FlipsOnFileRename_AtEqualBytes()
    {
        var (parent, origDir, _) = NewCorpus();
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);

        // Rename keeping byte content identical: files count + total bytes unchanged, only
        // pathsig (P16) flips. files+bytes alone would falsely read fresh — P16 catches it.
        var src = FirstXml(origDir);
        var bytes = File.ReadAllBytes(src);
        File.Delete(src);
        File.WriteAllBytes(Path.Combine(origDir, "renamed-zzz.xml"), bytes);

        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    [Fact]
    public void Stamp_FlipsOnSameTotalRedistribution()
    {
        // Two files swapping bytes so the SUM is preserved but per-path sizes move —
        // files+bytes identical, P16 differs. Guards the "redistribution" case in SPEC §1.2.
        var (parent, origDir, _) = NewCorpus(fileCount: 2, withTitles: false);
        var files = Directory.EnumerateFiles(origDir, "*.xml").OrderBy(p => p, StringComparer.Ordinal).ToArray();
        File.WriteAllText(files[0], SampleXml + "AB", new UTF8Encoding(false));
        File.WriteAllText(files[1], SampleXml, new UTF8Encoding(false));
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);

        // Move the 2 bytes from file0 to file1: total bytes and file count unchanged.
        File.WriteAllText(files[0], SampleXml, new UTF8Encoding(false));
        File.WriteAllText(files[1], SampleXml + "AB", new UTF8Encoding(false));
        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    // =====================================================================================
    // Titles (T16) half
    // =====================================================================================

    [Fact]
    public void Stamp_FlipsOnTitlesJsonlEdit()
    {
        var (parent, _, transRepo) = NewCorpus(withTitles: true);
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        WriteTitles(transRepo, "{\"path\":\"T0000.xml\",\"zh\":\"新標題\",\"en\":\"Edited Title\"}");
        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    [Fact]
    public void Stamp_FlipsWhenTitlesRemoved()
    {
        var (parent, _, transRepo) = NewCorpus(withTitles: true);
        var before = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        File.Delete(Path.Combine(transRepo, "titles.jsonl"));
        Assert.NotEqual(before, MasterCorpusSearchService.ComputeCorpusStamp(parent));
    }

    [Fact]
    public void Stamp_StableWhenTitlesAbsent_AndDeterministic()
    {
        // Absent titles.jsonl contributes a fixed zero-byte hash: two title-less corpora
        // with identical files produce identical stamps, and recomputation is stable.
        var (parentA, _, _) = NewCorpus(withTitles: false);
        var (parentB, _, _) = NewCorpus(withTitles: false);
        var a1 = MasterCorpusSearchService.ComputeCorpusStamp(parentA);
        var a2 = MasterCorpusSearchService.ComputeCorpusStamp(parentA);
        Assert.Equal(a1, a2);
        Assert.Equal(a1, MasterCorpusSearchService.ComputeCorpusStamp(parentB));
    }

    // =====================================================================================
    // Format guarantees — v2, no ticks/mtime anywhere in the stamp
    // =====================================================================================

    [Fact]
    public void Stamp_IsV2_AndCarriesNoTicks()
    {
        var (parent, _, _) = NewCorpus();
        var stamp = MasterCorpusSearchService.ComputeCompositeStamp(parent, SampleRosterA());
        Assert.NotNull(stamp);
        Assert.StartsWith("v2;", stamp);
        foreach (var token in new[] { "corpus=files=", "bytes=", "pathsig=", "titles=", "roster=count=", "hash=" })
            Assert.Contains(token, stamp);
        // The v1 stamp keyed an mtime ("maxTicks"); the v2 stamp must contain no tick/mtime term.
        Assert.DoesNotContain("maxTicks", stamp);
        Assert.DoesNotContain("Ticks", stamp);
    }

    // =====================================================================================
    // Roster (R16) half
    // =====================================================================================

    [Fact]
    public void Roster_FlipsOnRecordAdd()
    {
        var baseId = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 810, 866)));
        var plusId = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 810, 866),
                    Rec("Zhaozhou Congshen", new[] { "趙州" }, 778, 897)));
        Assert.NotEqual(baseId, plusId);
        Assert.Contains("count=1;", baseId);
        Assert.Contains("count=2;", plusId);
    }

    [Fact]
    public void Roster_FlipsOnCanonicalRename()
    {
        var a = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 810, 866)));
        var b = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan (renamed)", new[] { "臨濟" }, 810, 866)));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Roster_FlipsOnAliasEdit()
    {
        var a = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 810, 866)));
        var b = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟", "義玄" }, 810, 866)));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Roster_FlipsOnDateEdit()
    {
        var a = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 810, 866)));
        var b = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 811, 866)));  // floruit changed
        var c = MasterCorpusSearchService.ComputeRosterIdentity(
            Catalog(Rec("Linji Yixuan", new[] { "臨濟" }, 810, 867)));  // death changed
        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
        Assert.NotEqual(b, c);
    }

    [Fact]
    public void Roster_StableUnderRecordReorder()
    {
        var r1 = Rec("Linji Yixuan", new[] { "臨濟", "義玄" }, 810, 866);
        var r2 = Rec("Zhaozhou Congshen", new[] { "趙州" }, 778, 897);
        Assert.Equal(
            MasterCorpusSearchService.ComputeRosterIdentity(Catalog(r1, r2)),
            MasterCorpusSearchService.ComputeRosterIdentity(Catalog(r2, r1)));
    }

    [Fact]
    public void Roster_StableUnderAliasReorder()
    {
        // Aliases are sorted inside the identity, so their input order must not matter.
        Assert.Equal(
            MasterCorpusSearchService.ComputeRosterIdentity(
                Catalog(Rec("Linji Yixuan", new[] { "臨濟", "義玄", "臨濟義玄" }, 810, 866))),
            MasterCorpusSearchService.ComputeRosterIdentity(
                Catalog(Rec("Linji Yixuan", new[] { "臨濟義玄", "義玄", "臨濟" }, 810, 866))));
    }

    [Fact]
    public void Roster_CoversMergedOverlay_AddedRecord()
    {
        // The roster half is computed from the MERGED catalog, so a community-overlay record
        // (present in merged but not in base) must change the identity — otherwise an overlay
        // edit would serve a stale index (the 279-bug for overlays, SPEC §1.2).
        var baseCat = SampleRosterA();
        var merged = Catalog(baseCat.Records
            .Append(Rec("Community Master", new[] { "某某" }, 900, 970)).ToArray());
        Assert.NotEqual(
            MasterCorpusSearchService.ComputeRosterIdentity(baseCat),
            MasterCorpusSearchService.ComputeRosterIdentity(merged));
    }

    // =====================================================================================
    // Composite stamp
    // =====================================================================================

    [Fact]
    public void CompositeStamp_NullWhenNoCorpusDirs()
    {
        var empty = Path.Combine(_tempRoot, "no-corpus2-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(empty);
        AppPaths.InvalidateDiscoveryCache(empty);
        Assert.Null(MasterCorpusSearchService.ComputeCompositeStamp(empty, SampleRosterA()));
    }

    [Fact]
    public void CompositeStamp_JoinsCorpusAndRosterHalves()
    {
        var (parent, _, _) = NewCorpus();
        var cat = SampleRosterA();
        var expected = MasterCorpusSearchService.ComputeCorpusStamp(parent)
            + ";" + MasterCorpusSearchService.ComputeRosterIdentity(cat);
        Assert.Equal(expected, MasterCorpusSearchService.ComputeCompositeStamp(parent, cat));
    }

    [Fact]
    public void CompositeStamp_IsDeterministic()
    {
        var (parent, _, _) = NewCorpus();
        var cat = SampleRosterA();
        Assert.Equal(
            MasterCorpusSearchService.ComputeCompositeStamp(parent, cat),
            MasterCorpusSearchService.ComputeCompositeStamp(parent, cat));
    }

    // =====================================================================================
    // TryLoadAsync freshness gate — the decision surface (SPEC §7 PR-M1 acceptance)
    // =====================================================================================

    private async Task WriteCacheAsync(string cacheDir, string? stamp)
    {
        Directory.CreateDirectory(cacheDir);
        var index = new MasterCorpusIndex
        {
            CorpusStamp = stamp,
            FileCount = 3,
            MasterCount = 2,
            Appearances = new() { new MasterTextAppearance { MasterName = "Linji Yixuan", RelPath = "T0000.xml" } },
        };
        await new MasterCorpusSearchService().SaveAsync(cacheDir, index);
    }

    [Fact]
    public async Task TryLoad_NullWhenCacheAbsent()
    {
        var (parent, _, _) = NewCorpus();
        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(
            MasterCorpusSearchService.GetCacheDir(parent), CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(SampleRosterA()));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryLoad_ServesOnExactMatch()
    {
        var (parent, _, _) = NewCorpus();
        var cat = SampleRosterA();
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, MasterCorpusSearchService.ComputeCompositeStamp(parent, cat));

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(cat));
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.MasterCount);
    }

    [Fact]
    public async Task TryLoad_RejectsLegacyV1Stamp()
    {
        var (parent, _, _) = NewCorpus();
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, "files=3;maxTicks=637000000000000000");  // v1 format

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(SampleRosterA()));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryLoad_NullWhenStampUnset()
    {
        // A cache from an older build with no corpus_stamp at all is stale under freshness.
        var (parent, _, _) = NewCorpus();
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, null);

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(SampleRosterA()));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task Stale279Bug_RosterMismatch_ReturnsNull()
    {
        // The regression: the cache was baked against roster A but the live merged roster is B.
        // Under the old "files=N;maxTicks=T" stamp this served the roster-A index as fresh
        // (the "279 of 944" bug). The composite stamp must reject it.
        var (parent, _, _) = NewCorpus();
        var rosterA = SampleRosterA();
        var rosterB = Catalog(rosterA.Records
            .Append(Rec("New Master", new[] { "新師" }, 950, 1010)).ToArray());

        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, MasterCorpusSearchService.ComputeCompositeStamp(parent, rosterA));

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(rosterB));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryLoad_NullOnCorpusEdit()
    {
        var (parent, origDir, _) = NewCorpus();
        var cat = SampleRosterA();
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, MasterCorpusSearchService.ComputeCompositeStamp(parent, cat));

        // Corpus grows after the cache was baked → live corpus half differs → stale.
        File.WriteAllText(Path.Combine(origDir, "new.xml"), SampleXml, new UTF8Encoding(false));

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(cat));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryLoad_NullOnTitlesEdit()
    {
        var (parent, _, transRepo) = NewCorpus(withTitles: true);
        var cat = SampleRosterA();
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, MasterCorpusSearchService.ComputeCompositeStamp(parent, cat));

        WriteTitles(transRepo, "{\"path\":\"T0000.xml\",\"zh\":\"改\",\"en\":\"Changed\"}");

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(cat));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryLoad_NullWhenLiveRosterHasOverlay_ButCacheBakedFromBase()
    {
        // Guards §1.2 "hashing only the base roster would serve a stale index for overlay
        // edits": cache baked with base-only identity, live identity includes the overlay.
        var (parent, _, _) = NewCorpus();
        var baseCat = SampleRosterA();
        var mergedCat = Catalog(baseCat.Records
            .Append(Rec("Overlay Master", new[] { "疊師" }, 880, 940)).ToArray());

        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, MasterCorpusSearchService.ComputeCompositeStamp(parent, baseCat));

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(mergedCat));
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryLoad_ServesRegardless_WhenFreshnessNotEnforced()
    {
        // parentRootForFreshness == null ⇒ legacy "no freshness enforcement" path: even a
        // bogus stamp is served (used by call sites that do not gate on freshness).
        var (parent, _, _) = NewCorpus();
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await WriteCacheAsync(cacheDir, "totally-bogus-stamp");

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parentRootForFreshness: null, rosterIdentity: null);
        Assert.NotNull(loaded);
    }

    [Fact]
    public async Task TryLoad_ServesLegacyStamp_WhenNoCorpusDirs()
    {
        // ComputeCorpusStamp is null with no corpus dirs, so freshness is NOT enforced and
        // even a v1-stamped cache is served (documented "no corpus ⇒ not enforced" behavior).
        var empty = Path.Combine(_tempRoot, "nc-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(empty);
        AppPaths.InvalidateDiscoveryCache(empty);
        var cacheDir = MasterCorpusSearchService.GetCacheDir(empty);
        await WriteCacheAsync(cacheDir, "files=3;maxTicks=1");

        var svc = new MasterCorpusSearchService();
        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            empty, MasterCorpusSearchService.ComputeRosterIdentity(SampleRosterA()));
        Assert.NotNull(loaded);
    }

    // =====================================================================================
    // End-to-end: a real build stamps the cache, and TryLoad serves it back unchanged.
    // =====================================================================================

    [Fact]
    public async Task Build_StampsCache_AndTryLoadServesLiveMatch()
    {
        var (parent, _, _) = NewCorpus();
        var cat = SampleRosterA();
        var svc = new MasterCorpusSearchService();

        var built = await svc.BuildFullIndexAsync(parent, cat);
        Assert.Equal(MasterCorpusSearchService.ComputeCompositeStamp(parent, cat), built.CorpusStamp);
        Assert.StartsWith("v2;", built.CorpusStamp);

        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await svc.SaveAsync(cacheDir, built);

        // Same corpus + same roster ⇒ served.
        var served = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(cat));
        Assert.NotNull(served);

        // A roster date edit afterwards ⇒ stale.
        var edited = Catalog(
            Rec("Linji Yixuan", new[] { "臨濟", "義玄", "臨濟義玄" }, 810, 867),  // death nudged
            Rec("Zhaozhou Congshen", new[] { "趙州", "從諗" }, 778, 897));
        var stale = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parent, MasterCorpusSearchService.ComputeRosterIdentity(edited));
        Assert.Null(stale);
    }
}
