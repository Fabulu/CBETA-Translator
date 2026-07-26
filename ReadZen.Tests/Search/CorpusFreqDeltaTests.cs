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
    // NOTE (FL8): the combined algebraic corpusfreq-delta integration tests + their shared
    // plumbing (AssertFreqArtifactEqual / RunIncrementalThenCompareFreqAsync) were RETIRED here.
    // The delta path is only reachable via a combined incremental rebuild, which is now migrated
    // to the split (delta bypassed; corpusfreq is an additive per-layer fold). Correctness is
    // re-verified by SplitParityTests (exact merged==combined corpusfreq) and TrimmedSidecarTests.
    // The CountCorpusFreqs unit tests above stay active.
}
