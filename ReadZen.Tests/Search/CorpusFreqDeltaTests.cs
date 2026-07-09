using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// INC-3A (D3 item 4): corpus-frequency algebraic delta with full-recount fallback.
///
/// The incremental build must produce a corpusfreq artifact (bin parsed AS MAPS, plus
/// the TotalCharacters / UniqueCharacters / UniqueBigrams manifest fields) equal to a
/// from-scratch full rebuild of the same corpus state. Proven here:
///   - the extracted <see cref="SearchIndexService.CountCorpusFreqs"/> helper matches a
///     literal reimplementation of the original counting loop (CJK runs, punctuation
///     breaks resetting the bigram chain, Latin, empty);
///   - the delta path (adds + removals + changes) is map-equal to full, and keys whose
///     count reaches exactly 0 are PRUNED (a char existing ONLY in the removed file is
///     absent afterwards);
///   - missing old corpusfreq bin → delta refused, full recount, still equivalent;
///   - old corpusfreq IndexStamp mismatching the OLD main manifest → delta refused
///     (stamp-gated trust), full recount, still equivalent.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class CorpusFreqDeltaTests
{
    // =====================================================================
    // (a) helper unit tests: CountCorpusFreqs vs literal original loop
    // =====================================================================

    /// <summary>
    /// Literal reimplementation of the ORIGINAL corpusfreq counting loop (pre-extraction
    /// shape from BuildOrUpdateCoreAsync): chars = every IsIndexableCjk code unit counted
    /// with multiplicity; bigrams = adjacent pairs where hasPrev resets on ANY non-CJK char.
    /// </summary>
    private static (Dictionary<string, int> chars, Dictionary<string, int> bigrams, long total)
        LiteralOriginalLoop(string searchable)
    {
        var corpusCharFreqs = new Dictionary<string, int>();
        var corpusBigramFreqs = new Dictionary<string, int>();
        long corpusTotalChars = 0;

        char prev = '\0';
        bool hasPrev = false;
        for (int ci = 0; ci < searchable.Length; ci++)
        {
            char ch = searchable[ci];
            if (!SearchIndexService.IsIndexableCjk(ch)) { hasPrev = false; continue; }

            var ck = ch.ToString();
            corpusCharFreqs[ck] = corpusCharFreqs.TryGetValue(ck, out var cv) ? cv + 1 : 1;
            corpusTotalChars++;

            if (hasPrev)
            {
                var bk = string.Concat(prev, ch);
                corpusBigramFreqs[bk] = corpusBigramFreqs.TryGetValue(bk, out var bv) ? bv + 1 : 1;
            }
            prev = ch;
            hasPrev = true;
        }

        return (corpusCharFreqs, corpusBigramFreqs, corpusTotalChars);
    }

    public static IEnumerable<object[]> CountingSamples() => new[]
    {
        // Plain CJK run (repeated chars and bigrams).
        new object[] { "無門無門関曰其中無門" },
        // Punctuation breaks reset the bigram chain (。、： are not IsIndexableCjk).
        new object[] { "無門。曰：其中、無門関" },
        // Latin interleaved: resets hasPrev; Latin itself never counted.
        new object[] { "abc無x門 xyz門無qrs" },
        // Latin only.
        new object[] { "the gateless barrier 123" },
        // Empty.
        new object[] { "" },
        // Ext-A (U+3400) and Compat (U+F900) ranges count; ASCII digits do not.
        new object[] { "㐀㐁無7豈更" },
        // Non-BMP CJK (surrogate pair): halves are NOT IsIndexableCjk, so they reset
        // the chain, exactly like the original loop.
        new object[] { "無\U00020000門無" },
    };

    [Theory]
    [MemberData(nameof(CountingSamples))]
    public void CountCorpusFreqs_MatchesLiteralOriginalLoop(string sample)
    {
        var expected = LiteralOriginalLoop(sample);

        var chars = new Dictionary<string, int>();
        var bigrams = new Dictionary<string, int>();
        long total = 0;
        SearchIndexService.CountCorpusFreqs(sample, chars, bigrams, +1, ref total);

        Assert.Equal(expected.total, total);
        Assert.Equal(expected.chars.Count, chars.Count);
        foreach (var kv in expected.chars)
            Assert.True(chars.TryGetValue(kv.Key, out var v) && v == kv.Value,
                $"char '{kv.Key}' expected {kv.Value}, got {(chars.TryGetValue(kv.Key, out var v2) ? v2.ToString() : "<absent>")}");
        Assert.Equal(expected.bigrams.Count, bigrams.Count);
        foreach (var kv in expected.bigrams)
            Assert.True(bigrams.TryGetValue(kv.Key, out var v) && v == kv.Value,
                $"bigram '{kv.Key}' expected {kv.Value}, got {(bigrams.TryGetValue(kv.Key, out var v2) ? v2.ToString() : "<absent>")}");
    }

    [Theory]
    [MemberData(nameof(CountingSamples))]
    public void CountCorpusFreqs_AddThenSubtract_YieldsAllZeroValues(string sample)
    {
        var chars = new Dictionary<string, int>();
        var bigrams = new Dictionary<string, int>();
        long total = 0;
        SearchIndexService.CountCorpusFreqs(sample, chars, bigrams, +1, ref total);
        SearchIndexService.CountCorpusFreqs(sample, chars, bigrams, -1, ref total);

        Assert.Equal(0, total);
        Assert.All(chars.Values, v => Assert.Equal(0, v));
        Assert.All(bigrams.Values, v => Assert.Equal(0, v));
    }

    // =====================================================================
    // Shared integration plumbing
    // =====================================================================

    /// <summary>
    /// Asserts the corpusfreq artifact of <paramref name="a"/> equals that of
    /// <paramref name="b"/>: bins compared AS MAPS (dictionary insertion order
    /// legitimately differs between the delta and full paths) plus the manifest's
    /// TotalCharacters / UniqueCharacters / UniqueBigrams fields.
    /// </summary>
    private static void AssertFreqArtifactEqual(
        ArtifactFamilyAssert.FamilySnapshot a, ArtifactFamilyAssert.FamilySnapshot b)
    {
        Assert.Equal(a.FreqTotalChars, b.FreqTotalChars);

        Assert.Equal(a.FreqChars.Count, b.FreqChars.Count);
        foreach (var kv in a.FreqChars)
            Assert.True(b.FreqChars.TryGetValue(kv.Key, out var v) && v == kv.Value,
                $"corpusfreq char U+{(int)kv.Key:X4} count differs: {kv.Value} vs {(b.FreqChars.TryGetValue(kv.Key, out var v2) ? v2.ToString() : "<absent>")}");

        Assert.Equal(a.FreqBigrams.Count, b.FreqBigrams.Count);
        foreach (var kv in a.FreqBigrams)
            Assert.True(b.FreqBigrams.TryGetValue(kv.Key, out var v) && v == kv.Value,
                $"corpusfreq bigram U+{(int)kv.Key.c0:X4} U+{(int)kv.Key.c1:X4} count differs: {kv.Value} vs {(b.FreqBigrams.TryGetValue(kv.Key, out var v2) ? v2.ToString() : "<absent>")}");

        Assert.Equal(a.FreqManifest.TotalCharacters, b.FreqManifest.TotalCharacters);
        Assert.Equal(a.FreqManifest.UniqueCharacters, b.FreqManifest.UniqueCharacters);
        Assert.Equal(a.FreqManifest.UniqueBigrams, b.FreqManifest.UniqueBigrams);

        // Manifest fields are consistent with the parsed bin.
        Assert.Equal(a.FreqChars.Count, a.FreqManifest.UniqueCharacters);
        Assert.Equal(a.FreqBigrams.Count, a.FreqManifest.UniqueBigrams);
        Assert.Equal(a.FreqTotalChars, a.FreqManifest.TotalCharacters);
    }

    /// <summary>
    /// Incremental build over the current fixture state, snapshot, then comparison full
    /// rebuild of the SAME state (no file touched in between), snapshot, and corpusfreq
    /// equality. Returns the incremental snapshot for extra assertions.
    /// </summary>
    private static async Task<ArtifactFamilyAssert.FamilySnapshot> RunIncrementalThenCompareFreqAsync(
        IndexFixtureCorpus fx, SearchIndexService svc)
    {
        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        var incremental = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        AssertFreqArtifactEqual(incremental, full);
        return incremental;
    }

    // =====================================================================
    // (b) integration: delta path, map-equal + zero-prune proven
    // =====================================================================

    [Fact]
    public async Task DeltaPath_AddRemoveChange_MapEqualToFull_RemovedOnlyKeysPruned()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        // The fresh full build never uses the delta.
        Assert.Equal(0, svc.LastBuildFreqDeltaApplied);

        var added = fx.AddFileMidCorpus();
        var removed = fx.RemoveFile(fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase)));
        var changed = fx.ChangeFile(fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase)));

        // The removed rel's unique grams (orig U+5100-range pair, tran U+5800-range
        // pair) exist ONLY in the removed file corpus-wide — their counts must reach
        // exactly 0 and be pruned from the maps.
        var removedOrigGram = fx.UniqueOrigGram(removed);
        var removedTranGram = fx.UniqueTranGram(removed);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        // The algebraic delta actually ran (not the full recount).
        Assert.Equal(1, svc.LastBuildFreqDeltaApplied);
        Assert.Equal(0, svc.LastBuildFallbackCount);

        var inc = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        // Zero-prune proven: the removed-file-only char and bigram keys are ABSENT.
        Assert.False(inc.FreqChars.ContainsKey(removedOrigGram[0]), "removed-only orig char[0] must be pruned");
        Assert.False(inc.FreqChars.ContainsKey(removedOrigGram[1]), "removed-only orig char[1] must be pruned");
        Assert.False(inc.FreqBigrams.ContainsKey((removedOrigGram[0], removedOrigGram[1])), "removed-only orig bigram must be pruned");
        Assert.False(inc.FreqChars.ContainsKey(removedTranGram[0]), "removed-only tran char[0] must be pruned");
        Assert.False(inc.FreqBigrams.ContainsKey((removedTranGram[0], removedTranGram[1])), "removed-only tran bigram must be pruned");

        // Added and changed content is counted.
        var addedGram = fx.UniqueOrigGram(added);
        Assert.True(inc.FreqChars.ContainsKey(addedGram[0]), "added file's unique char must be counted");
        Assert.True(inc.FreqBigrams.ContainsKey((addedGram[0], addedGram[1])), "added file's unique bigram must be counted");
        // ChangeFile appended '改動之文' (plus marker chars) to the changed file.
        Assert.True(inc.FreqBigrams.ContainsKey(('改', '動')), "changed file's new bigram must be counted");
        _ = changed;

        // Map-equal to a from-scratch rebuild of the same corpus state.
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        Assert.Equal(0, svc.LastBuildFreqDeltaApplied); // forceRebuild never deltas
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        AssertFreqArtifactEqual(inc, full);
    }

    // =====================================================================
    // (c) fallback: old corpusfreq bin missing → full recount, still equivalent
    // =====================================================================

    [Fact]
    public async Task MissingOldFreqBin_FallsBackToFullRecount_StillEquivalent()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.ChangeFile(fx.BothSidesRels[2]);
        File.Delete(Path.Combine(fx.Root, "search.corpusfreq.bin"));

        var inc = await RunIncrementalThenCompareFreqAsync(fx, svc);
        Assert.NotEmpty(inc.FreqChars);
        // Delta preconditions unmet → the full recount branch produced the artifact.
        // (LastBuildFreqDeltaApplied reflects the LAST core run, which is the comparison
        // full rebuild here, so the assertion on the incremental run happens first.)
    }

    [Fact]
    public async Task MissingOldFreqBin_IncrementalRun_DoesNotApplyDelta()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.ChangeFile(fx.BothSidesRels[2]);
        File.Delete(Path.Combine(fx.Root, "search.corpusfreq.bin"));

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        Assert.Equal(0, svc.LastBuildFreqDeltaApplied);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        // The build still committed a fresh, stamped corpusfreq artifact.
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);
        Assert.True(File.Exists(Path.Combine(fx.Root, "search.corpusfreq.bin")));
    }

    // =====================================================================
    // (d) stamp-gated trust: old stamp mismatch → full recount, still equivalent
    // =====================================================================

    [Fact]
    public async Task OldFreqStampMismatch_FallsBackToFullRecount_StillEquivalent()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.ChangeFile(fx.BothSidesRels[2]);

        // Rewrite the OLD corpusfreq manifest's IndexStamp to a bogus value: the delta
        // must refuse to seed from an artifact not provably of the OLD build family.
        var manPath = Path.Combine(fx.Root, "search.corpusfreq.manifest.json");
        var man = JsonSerializer.Deserialize<CorpusFreqManifest>(File.ReadAllText(manPath))!;
        man.IndexStamp = "bogus-stamp-not-of-this-family";
        File.WriteAllText(manPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }));

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        Assert.Equal(0, svc.LastBuildFreqDeltaApplied);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        var inc = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        AssertFreqArtifactEqual(inc, full);
    }
}
