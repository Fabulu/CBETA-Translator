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
/// Tests for the IndexStamp integrity binding on the corpusfreq sibling artifact
/// (D3 item 5, "search-v6-stamped-siblings").
///
/// The corpusfreq counts are derived data keyed to the main manifest of the SAME
/// build (they reflect that build's text sidecar). A crash after the main manifest
/// commits but before the sibling saves used to leave the PREVIOUS build's sibling
/// silently trusted. The stamp gate downgrades that failure from "wrong results" to
/// "artifact refused, slower fallback".
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
        var freq = DeserializeFile<CorpusFreqManifest>(CorpusFreqManifestPath);

        Assert.False(string.IsNullOrEmpty(main.IndexStamp));
        Assert.NotNull(freq.IndexStamp);
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
        var freq = DeserializeFile<CorpusFreqManifest>(CorpusFreqManifestPath);

        Assert.NotEqual(firstStamp, main.IndexStamp); // fresh stamp per build
        Assert.Equal(main.IndexStamp, freq.IndexStamp);
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
