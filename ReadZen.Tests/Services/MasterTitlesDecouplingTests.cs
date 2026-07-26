using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-M1: master-title decoupling. Proves the bundled corpus index no longer bakes text
/// display titles into the appearance shards — instead the shards carry only the stable
/// <see cref="MasterTextAppearance.RelPath"/> key and titles are JOINED AT LOAD TIME from
/// titles.jsonl. Consequence: a title-only edit changes only the (tiny) title map, never the
/// ~57 MB appearance shards, so it is zero corpus-index rebuild. Also proves the title-source
/// change is still DETECTABLE (the stamp flips), and that a rel path absent from the title
/// source degrades gracefully (null title, no crash). BundleSeedTests-style real temp corpora;
/// every assertion is content-only, wall-clock independent.
/// </summary>
public class MasterTitlesDecouplingTests : IDisposable
{
    private readonly string _tempRoot;

    public MasterTitlesDecouplingTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-mtitles-" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>Single-file CBETA-shaped corpus so the build yields exactly one appearance
    /// (deterministic ordering — no ConcurrentBag tie ambiguity across rebuilds).</summary>
    private (string parent, string origDir, string transRepo) NewCorpus(bool withTitles = true)
    {
        var parent = Path.Combine(_tempRoot, "p-" + Guid.NewGuid().ToString("N")[..8]);
        var origDir = Path.Combine(parent, "CbetaZenTexts", "xml-p5");
        var transRepo = Path.Combine(parent, "CbetaZenTranslations");
        Directory.CreateDirectory(origDir);
        Directory.CreateDirectory(Path.Combine(transRepo, "xml-p5t"));

        File.WriteAllText(Path.Combine(origDir, "T0000.xml"), SampleXml, new UTF8Encoding(false));
        if (withTitles)
            WriteTitles(transRepo, "{\"path\":\"T0000.xml\",\"zh\":\"標題\",\"en\":\"Title\"}");

        AppPaths.InvalidateDiscoveryCache(parent);
        return (parent, origDir, transRepo);
    }

    private static void WriteTitles(string transRepo, params string[] lines)
        => File.WriteAllText(Path.Combine(transRepo, "titles.jsonl"),
            string.Join("\n", lines) + "\n", new UTF8Encoding(false));

    private static ZenMasterCatalog SampleRoster() => new()
    {
        Records =
        {
            new ZenMasterRecord
            {
                CanonicalName = "Linji Yixuan",
                Aliases = new() { "臨濟", "義玄", "臨濟義玄" },
                Variants = new() { new ZenMasterVariant
                    { Names = new() { "臨濟", "義玄", "臨濟義玄" }, Floruit = 810, Death = 866, IsBase = true } },
            },
        },
    };

    /// <summary>SHA256 over the ordered concatenation of the appearance SHARD files (the big
    /// data) — deliberately NOT the manifest, whose corpus_stamp legitimately carries the
    /// titles token and so changes on a title edit.</summary>
    private static string HashAppearanceShards(string cacheDir)
    {
        var shards = Directory
            .EnumerateFiles(cacheDir, "master-corpus-index.appearances.*.json")
            .OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(shards); // the fixture must actually produce shard files
        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var s in shards)
            ih.AppendData(File.ReadAllBytes(s));
        return Convert.ToHexString(ih.GetHashAndReset());
    }

    // =====================================================================================
    // Decoupling: a title-only edit leaves the appearance shards byte-identical
    // =====================================================================================

    [Fact]
    public async Task SaveAsync_OmitsTitles_ShardBytesInvariantToTitleValue()
    {
        // Direct serialization proof: two indices that differ ONLY in the (in-memory) title
        // fields serialize to byte-identical appearance shards — titles never reach disk.
        MasterCorpusIndex Make(string? en, string? zh) => new()
        {
            Corpus = "Cbeta",
            CorpusStamp = "v2;corpus=files=1;bytes=1;pathsig=0000000000000000;titles=0000000000000000;roster=count=1;hash=0000000000000000",
            FileCount = 1,
            MasterCount = 1,
            Appearances =
            {
                new MasterTextAppearance { MasterName = "Linji Yixuan", MatchedName = "臨濟",
                    RelPath = "T0000.xml", AppearanceType = "primary", MentionCount = 1,
                    Snippet = "臨濟義玄禪師示眾云", TextTitle = en, TextTitleZh = zh },
            },
        };

        var dirA = Path.Combine(_tempRoot, "sa-a");
        var dirB = Path.Combine(_tempRoot, "sa-b");
        var svc = new MasterCorpusSearchService();
        await svc.SaveAsync(dirA, Make("Title", "標題"));
        await svc.SaveAsync(dirB, Make("A COMPLETELY DIFFERENT TITLE", "另一個標題"));

        Assert.Equal(HashAppearanceShards(dirA), HashAppearanceShards(dirB));
    }

    [Fact]
    public async Task BuildThenSave_ShardBytesIdentical_AfterTitleEdit()
    {
        // End-to-end: build+save the real index, edit titles.jsonl, build+save again. The
        // appearance shards are byte-identical across the edit (the whole point of M1).
        var (parent, _, transRepo) = NewCorpus(withTitles: true);
        var cat = SampleRoster();
        var svc = new MasterCorpusSearchService();

        var dirA = Path.Combine(parent, ".cache-a");
        var built1 = await svc.BuildFullIndexAsync(parent, cat);
        await svc.SaveAsync(dirA, built1);
        var hashBefore = HashAppearanceShards(dirA);

        WriteTitles(transRepo, "{\"path\":\"T0000.xml\",\"zh\":\"新標題\",\"en\":\"Edited\"}");

        var dirB = Path.Combine(parent, ".cache-b");
        var built2 = await svc.BuildFullIndexAsync(parent, cat);
        await svc.SaveAsync(dirB, built2);
        var hashAfter = HashAppearanceShards(dirB);

        Assert.Equal(hashBefore, hashAfter);
    }

    // =====================================================================================
    // Load-time join: parity with the freshly built index; still detectable; graceful
    // =====================================================================================

    [Fact]
    public async Task LoadTimeJoin_ProducesSameTitles_AsFreshBuild()
    {
        // The title a consumer sees from a CACHED load must equal the title the fresh BUILD
        // produced (both go through the same JoinTitles path) — no consumer regresses.
        var (parent, _, _) = NewCorpus(withTitles: true);
        var cat = SampleRoster();
        var svc = new MasterCorpusSearchService();

        var built = await svc.BuildFullIndexAsync(parent, cat);
        var builtAppearance = Assert.Single(built.Appearances);
        Assert.Equal("Title", builtAppearance.TextTitle);       // build-time join populated it
        Assert.Equal("標題", builtAppearance.TextTitleZh);

        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await svc.SaveAsync(cacheDir, built);

        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parentRootForFreshness: parent,
            rosterIdentity: MasterCorpusSearchService.ComputeRosterIdentity(cat));
        Assert.NotNull(loaded);
        var loadedAppearance = Assert.Single(loaded!.Appearances);
        Assert.Equal(builtAppearance.TextTitle, loadedAppearance.TextTitle);
        Assert.Equal(builtAppearance.TextTitleZh, loadedAppearance.TextTitleZh);
    }

    [Fact]
    public async Task TitleEdit_IsDetectableInStamp_ButServesWithoutRebuild()
    {
        // Detection (stamp flips on a title edit) AND zero-rebuild (TryLoadAsync still serves)
        // hold simultaneously — the freshness DECISION ignores the titles token that the
        // stamp nonetheless records, mirroring the search/nav re-derive-display-keep-verdict
        // pattern.
        var (parent, _, transRepo) = NewCorpus(withTitles: true);
        var cat = SampleRoster();
        var svc = new MasterCorpusSearchService();

        var stampBefore = MasterCorpusSearchService.ComputeCorpusStamp(parent);
        var cacheDir = MasterCorpusSearchService.GetCacheDir(parent);
        await svc.SaveAsync(cacheDir, await svc.BuildFullIndexAsync(parent, cat));

        WriteTitles(transRepo, "{\"path\":\"T0000.xml\",\"zh\":\"改\",\"en\":\"Changed\"}");
        var stampAfter = MasterCorpusSearchService.ComputeCorpusStamp(parent);

        Assert.NotEqual(stampBefore, stampAfter);   // DETECTION: the title source changed

        var loaded = await svc.TryLoadAsync(cacheDir, CancellationToken.None,
            parentRootForFreshness: parent,
            rosterIdentity: MasterCorpusSearchService.ComputeRosterIdentity(cat));
        Assert.NotNull(loaded);                     // NO REBUILD: the stale-titles cache serves
        Assert.Equal("Changed", Assert.Single(loaded!.Appearances).TextTitle); // re-joined live
    }

    [Fact]
    public void JoinTitles_MissingRelPath_LeavesNullTitle_NoCrash()
    {
        // A master present in the appearances but whose rel path is absent from the title
        // source keeps null titles (the rel path remains as the stable identity) — never a crash.
        var (parent, _, transRepo) = NewCorpus(withTitles: true);
        WriteTitles(transRepo, "{\"path\":\"SOME-OTHER-FILE.xml\",\"zh\":\"標題\",\"en\":\"Title\"}");

        var index = new MasterCorpusIndex
        {
            Appearances =
            {
                new MasterTextAppearance { MasterName = "Linji Yixuan", RelPath = "T0000.xml",
                    TextTitle = "STALE", TextTitleZh = "殘留" },
            },
        };

        MasterCorpusSearchService.JoinTitles(index, parent); // must not throw

        var a = Assert.Single(index.Appearances);
        Assert.Equal("T0000.xml", a.RelPath); // identity preserved
        Assert.Null(a.TextTitle);             // no match ⇒ cleared to null, not left stale
        Assert.Null(a.TextTitleZh);
    }

    [Fact]
    public void StripTitlesToken_RemovesTitlesComponent_LeavesCorpusAndRosterIntact()
    {
        // The compare-normalizer used by the freshness/adoption gates: a title-embedded stamp
        // and an otherwise-identical one differing only in titles normalize to the same string.
        const string withTitlesA =
            "v2;corpus=files=1;bytes=2;pathsig=aaaaaaaaaaaaaaaa;titles=1111111111111111;roster=count=1;hash=cccccccccccccccc";
        const string withTitlesB =
            "v2;corpus=files=1;bytes=2;pathsig=aaaaaaaaaaaaaaaa;titles=2222222222222222;roster=count=1;hash=cccccccccccccccc";

        Assert.Equal(
            MasterCorpusSearchService.StripTitlesToken(withTitlesA),
            MasterCorpusSearchService.StripTitlesToken(withTitlesB));
        // A corpus or roster difference must survive normalization.
        Assert.NotEqual(
            MasterCorpusSearchService.StripTitlesToken(withTitlesA),
            MasterCorpusSearchService.StripTitlesToken(
                "v2;corpus=files=9;bytes=2;pathsig=aaaaaaaaaaaaaaaa;titles=1111111111111111;roster=count=1;hash=cccccccccccccccc"));
    }
}
