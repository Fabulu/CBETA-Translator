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
/// Tests for the IndexStamp integrity binding on the cjk2 and corpusfreq sibling
/// artifacts (D3 item 5, "search-v6-stamped-siblings").
///
/// Both siblings are positional/derived data keyed to the main manifest of the SAME
/// build: cjk2 EntryIds index into the main manifest's entry list, and corpusfreq
/// counts reflect that build's text sidecar. A crash after the main manifest commits
/// but before a sibling saves used to leave the PREVIOUS build's sibling silently
/// trusted — cjk2 then prefilters with shifted entry Ids (missed search results).
/// The stamp gate downgrades that failure from "wrong results" to "artifact refused,
/// slower fallback".
///
/// Uses real on-disk synthetic corpora in temp dirs (never the real corpus), modeled
/// on SkipVerifyHybridTests / InvertedSearchIndexIntegrityTests.
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class SiblingStampTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;
    private readonly string _tranDir;

    private string CorpusFreqManifestPath => Path.Combine(_tempRoot, "search.corpusfreq.manifest.json");

    public SiblingStampTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-stamp-" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>Writes a small synthetic TEI-ish XML corpus with CJK bodies.</summary>
    private void WriteCorpus(int fileCount)
    {
        for (int i = 0; i < fileCount; i++)
        {
            var body = "無門關" + new string('中', (i + 1) * 20);
            File.WriteAllText(
                Path.Combine(_origDir, $"f{i:D3}.xml"),
                $"<TEI><text><body>{body}</body></text></TEI>");
        }
    }

    private async Task<SearchIndexService> BuildAsync()
    {
        var svc = new SearchIndexService();
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });
        return svc;
    }

    private static T DeserializeFile<T>(string path)
    {
        var json = File.ReadAllText(path);
        var obj = JsonSerializer.Deserialize<T>(json);
        Assert.NotNull(obj);
        return obj!;
    }

    // ===== (a) Stamps minted on every build, equal to the family IndexStamp =====

    [Fact]
    public async Task Build_MintsSiblingStamps_EqualToMainManifestIndexStamp()
    {
        WriteCorpus(3);
        var svc = await BuildAsync();

        var main = DeserializeFile<SearchIndexManifest>(svc.GetManifestPath(_tempRoot));
        var cjk2 = DeserializeFile<SearchCjkBigramManifest>(svc.GetCjk2ManifestPath(_tempRoot));
        var freq = DeserializeFile<CorpusFreqManifest>(CorpusFreqManifestPath);

        Assert.False(string.IsNullOrEmpty(main.IndexStamp));
        Assert.NotNull(cjk2.IndexStamp);
        Assert.NotNull(freq.IndexStamp);
        Assert.Equal(main.IndexStamp, cjk2.IndexStamp);
        Assert.Equal(main.IndexStamp, freq.IndexStamp);
    }

    [Fact]
    public async Task Rebuild_MintsFreshStamps_SiblingsFollow()
    {
        WriteCorpus(2);
        var svc = await BuildAsync();
        var firstStamp = DeserializeFile<SearchIndexManifest>(svc.GetManifestPath(_tempRoot)).IndexStamp;

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        var main = DeserializeFile<SearchIndexManifest>(svc.GetManifestPath(_tempRoot));
        var cjk2 = DeserializeFile<SearchCjkBigramManifest>(svc.GetCjk2ManifestPath(_tempRoot));
        var freq = DeserializeFile<CorpusFreqManifest>(CorpusFreqManifestPath);

        Assert.NotEqual(firstStamp, main.IndexStamp); // fresh stamp per build
        Assert.Equal(main.IndexStamp, cjk2.IndexStamp);
        Assert.Equal(main.IndexStamp, freq.IndexStamp);
    }

    // ===== (b) IsCjk2Usable predicate =====

    private static SearchIndexManifest MakeMainManifest(int entryCount, string? stamp)
    {
        var m = new SearchIndexManifest { IndexStamp = stamp };
        for (int i = 0; i < entryCount; i++)
            m.Entries.Add(new SearchIndexEntry { Id = i, RelPath = $"f{i}.xml" });
        return m;
    }

    [Fact]
    public void IsCjk2Usable_MatchingStampAndCount_True()
    {
        var main = MakeMainManifest(2, "abc123");
        var cjk2 = new SearchCjkBigramManifest { EntryCount = 2, IndexStamp = "abc123" };
        Assert.True(SearchIndexService.IsCjk2Usable(cjk2, main));
    }

    [Fact]
    public void IsCjk2Usable_NullCjk2Stamp_False()
    {
        // Legacy unstamped cjk2 manifest must be refused even when counts match.
        var main = MakeMainManifest(2, "abc123");
        var cjk2 = new SearchCjkBigramManifest { EntryCount = 2, IndexStamp = null };
        Assert.False(SearchIndexService.IsCjk2Usable(cjk2, main));
    }

    [Fact]
    public void IsCjk2Usable_MismatchedStamp_False()
    {
        var main = MakeMainManifest(2, "abc123");
        var cjk2 = new SearchCjkBigramManifest { EntryCount = 2, IndexStamp = "zzz999" };
        Assert.False(SearchIndexService.IsCjk2Usable(cjk2, main));
    }

    [Fact]
    public void IsCjk2Usable_EntryCountMismatch_False()
    {
        var main = MakeMainManifest(3, "abc123");
        var cjk2 = new SearchCjkBigramManifest { EntryCount = 2, IndexStamp = "abc123" };
        Assert.False(SearchIndexService.IsCjk2Usable(cjk2, main));
    }

    [Fact]
    public void IsCjk2Usable_BothStampsNull_False()
    {
        // Null never matches null — a legacy pair must not be trusted by accident.
        var main = MakeMainManifest(2, null);
        var cjk2 = new SearchCjkBigramManifest { EntryCount = 2, IndexStamp = null };
        Assert.False(SearchIndexService.IsCjk2Usable(cjk2, main));
    }

    // ===== (c) Crash-window simulation: old cjk2 sibling + new main manifest =====

    [Fact]
    public async Task CrashWindow_OldCjk2AfterCorpusMutation_RefusedByUsabilityGate()
    {
        // Build v1, save the cjk2 manifest aside (simulating the file a crash would
        // leave behind), mutate the corpus so entry Ids shift, build v2, then restore
        // the OLD cjk2 manifest over the new one — exactly the state after a crash
        // between the main manifest commit and the cjk2 save.
        WriteCorpus(3);
        var svc = await BuildAsync();

        var cjk2Path = svc.GetCjk2ManifestPath(_tempRoot);
        var asidePath = cjk2Path + ".v1-aside";
        File.Copy(cjk2Path, asidePath, overwrite: true);

        // Add a file that sorts FIRST so every existing entry's Id shifts by one.
        File.WriteAllText(
            Path.Combine(_origDir, "a000.xml"),
            "<TEI><text><body>無門關中中中</body></text></TEI>");
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        File.Copy(asidePath, cjk2Path, overwrite: true);
        svc.InvalidateIndexCaches();

        var manifest = await svc.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);
        var staleCjk2 = await svc.TryLoadCjk2ManifestAsync(_tempRoot);
        Assert.NotNull(staleCjk2); // structurally valid, so it still loads...

        Assert.NotEqual(manifest!.IndexStamp, staleCjk2!.IndexStamp); // ...but is a different build
        Assert.False(SearchIndexService.IsCjk2Usable(staleCjk2, manifest)); // and the gate refuses it
    }

    [Fact]
    public async Task CrashWindow_OldCjk2SameEntryCount_RefusedByStampAlone()
    {
        // Rebuild over an UNCHANGED corpus: EntryCount stays identical, so the legacy
        // count-only check would happily accept the stale sibling — only the stamp
        // catches it. This is the exact hole item 5 closes.
        WriteCorpus(3);
        var svc = await BuildAsync();

        var cjk2Path = svc.GetCjk2ManifestPath(_tempRoot);
        var asidePath = cjk2Path + ".v1-aside";
        File.Copy(cjk2Path, asidePath, overwrite: true);

        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir }); // fresh stamp, same entries

        File.Copy(asidePath, cjk2Path, overwrite: true);
        svc.InvalidateIndexCaches();

        var manifest = await svc.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);
        var staleCjk2 = await svc.TryLoadCjk2ManifestAsync(_tempRoot);
        Assert.NotNull(staleCjk2);

        Assert.Equal(manifest!.Entries.Count, staleCjk2!.EntryCount); // count check alone would pass
        Assert.False(SearchIndexService.IsCjk2Usable(staleCjk2, manifest)); // stamp refuses
    }

    // ===== (d) Corpusfreq stamp validation =====

    [Fact]
    public async Task CorpusFreq_FreshService_LoadsWhenStampMatches()
    {
        WriteCorpus(3);
        await BuildAsync();

        using var fresh = new SearchIndexService();
        var manifest = await fresh.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);
        Assert.True(fresh.HasCorpusFrequencies);
    }

    [Fact]
    public async Task CorpusFreq_StrippedStamp_RefusedButSearchStillWorks()
    {
        WriteCorpus(3);
        await BuildAsync();

        // Simulate a legacy/unstamped corpusfreq manifest on disk.
        StripIndexStampFromJson(CorpusFreqManifestPath);

        using var fresh = new SearchIndexService();
        var manifest = await fresh.TryLoadAsync(_tempRoot);
        Assert.NotNull(manifest);
        Assert.False(fresh.HasCorpusFrequencies); // refused

        // Search must remain fully functional without the frequency accelerator.
        var groups = new List<SearchResultGroup>();
        await foreach (var g in fresh.SearchAllAsync(
            _tempRoot, _origDir, _tranDir, manifest!, "無門",
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 30))
        {
            groups.Add(g);
        }
        Assert.Equal(3, groups.Count);
    }

    [Fact]
    public async Task CorpusFreq_DirectLoad_RefusesNullOrMismatchedExpectedStamp()
    {
        WriteCorpus(2);
        var svc = await BuildAsync();
        var main = DeserializeFile<SearchIndexManifest>(svc.GetManifestPath(_tempRoot));

        using var fresh = new SearchIndexService();
        Assert.False(await fresh.TryLoadCorpusFrequenciesAsync(_tempRoot, null));
        Assert.False(fresh.HasCorpusFrequencies);

        Assert.False(await fresh.TryLoadCorpusFrequenciesAsync(_tempRoot, "not-the-stamp"));
        Assert.False(fresh.HasCorpusFrequencies);

        Assert.True(await fresh.TryLoadCorpusFrequenciesAsync(_tempRoot, main.IndexStamp));
        Assert.True(fresh.HasCorpusFrequencies);
    }

    /// <summary>
    /// Rewrites a JSON file with <c>"IndexStamp": null</c>, simulating an artifact
    /// written by an older binary (regex precedent: IndexStalenessTests.StripInputHashFromManifest).
    /// </summary>
    private static void StripIndexStampFromJson(string path)
    {
        var json = File.ReadAllText(path);
        json = System.Text.RegularExpressions.Regex.Replace(
            json, "\"IndexStamp\":\\s*\"[^\"]*\"", "\"IndexStamp\": null");
        File.WriteAllText(path, json);
    }
}
