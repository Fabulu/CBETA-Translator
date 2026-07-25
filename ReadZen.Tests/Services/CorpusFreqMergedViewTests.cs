using System.Collections.Generic;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL2 (frozen/live index split, design §2.3 consumer #5 "additive decomposition"): pins
/// that the in-memory additive fold of per-family corpusfreq partials
/// (<see cref="SearchIndexService.MergeCorpusFreqPartials"/>) equals a direct global
/// recount over all documents — exact dictionary equality for chars + bigrams, exact
/// totalChars. Char/bigram counts and totalChars are sums over per-document texts, so the
/// corpus-global value is the family-wise sum; integer addition is associative/commutative,
/// so merged == combined recount exactly.
/// </summary>
public class CorpusFreqMergedViewTests
{
    // Deterministic per-document count of CJK chars + adjacent CJK-CJK bigrams. The exact
    // classification is not what is under test — additivity is — so the SAME function is
    // used for every family partial AND for the global recount. Counting is strictly
    // per-document (never over a concatenation), mirroring the real build which calls its
    // CountCorpusFreqs once per entry/text block (SearchIndexService.cs ~:3741): no bigram
    // ever spans a document boundary, so Σ_family == recount(all docs) with no boundary skew.
    private static (Dictionary<string, int> chars, Dictionary<string, int> bigrams, long total)
        Count(string doc)
    {
        var chars = new Dictionary<string, int>();
        var bigrams = new Dictionary<string, int>();
        long total = 0;

        for (int i = 0; i < doc.Length; i++)
        {
            char c = doc[i];
            if (!IsCjk(c)) continue;

            var key = c.ToString();
            chars[key] = (chars.TryGetValue(key, out var e) ? e : 0) + 1;
            total++;

            if (i + 1 < doc.Length && IsCjk(doc[i + 1]))
            {
                var bg = doc.Substring(i, 2);
                bigrams[bg] = (bigrams.TryGetValue(bg, out var eb) ? eb : 0) + 1;
            }
        }

        return (chars, bigrams, total);
    }

    private static bool IsCjk(char ch)
        => (ch >= 0x3400 && ch <= 0x4DBF)
        || (ch >= 0x4E00 && ch <= 0x9FFF)
        || (ch >= 0xF900 && ch <= 0xFAFF);

    private static void Accumulate(
        Dictionary<string, int> into,
        IReadOnlyDictionary<string, int> from)
    {
        foreach (var kv in from)
            into[kv.Key] = (into.TryGetValue(kv.Key, out var e) ? e : 0) + kv.Value;
    }

    [Fact]
    public void MergedView_EqualsGlobalRecount()
    {
        // Synthetic multi-doc corpus with deliberate cross-document char/bigram overlap so
        // the fold must SUM (not just union) shared keys. Partitioned into two families.
        var docsFamilyA = new[]
        {
            "禪心佛心禪",   // 禪 twice, 心 twice, bigrams 禪心/心佛/佛心/心禪
            "祖師禪心",     // 心 again, 禪 again, shared with above
            "無門關無門",   // 無 twice, 門 twice
        };
        var docsFamilyB = new[]
        {
            "禪心一如",     // 禪/心 shared with family A
            "佛法無邊無",   // 無 shared with family A
            "心經般若",
        };

        // Build each family's partial: Σ over its own documents.
        var famAChars = new Dictionary<string, int>();
        var famABigrams = new Dictionary<string, int>();
        long famATotal = 0;
        foreach (var d in docsFamilyA)
        {
            var (c, b, t) = Count(d);
            Accumulate(famAChars, c);
            Accumulate(famABigrams, b);
            famATotal += t;
        }

        var famBChars = new Dictionary<string, int>();
        var famBBigrams = new Dictionary<string, int>();
        long famBTotal = 0;
        foreach (var d in docsFamilyB)
        {
            var (c, b, t) = Count(d);
            Accumulate(famBChars, c);
            Accumulate(famBBigrams, b);
            famBTotal += t;
        }

        // Fold the two partials (the FL2 additive machinery under test).
        var (mergedChars, mergedBigrams, mergedTotal) =
            SearchIndexService.MergeCorpusFreqPartials(new[]
            {
                ((IReadOnlyDictionary<string, int>?)famAChars, (IReadOnlyDictionary<string, int>?)famABigrams, famATotal),
                ((IReadOnlyDictionary<string, int>?)famBChars, (IReadOnlyDictionary<string, int>?)famBBigrams, famBTotal),
            });

        // Direct global recount: Σ over ALL documents at once.
        var globalChars = new Dictionary<string, int>();
        var globalBigrams = new Dictionary<string, int>();
        long globalTotal = 0;
        foreach (var d in docsFamilyA)
        {
            var (c, b, t) = Count(d);
            Accumulate(globalChars, c);
            Accumulate(globalBigrams, b);
            globalTotal += t;
        }
        foreach (var d in docsFamilyB)
        {
            var (c, b, t) = Count(d);
            Accumulate(globalChars, c);
            Accumulate(globalBigrams, b);
            globalTotal += t;
        }

        Assert.NotNull(mergedChars);
        Assert.NotNull(mergedBigrams);

        // Exact dictionary equality (same key set, same summed counts) + exact totalChars.
        Assert.Equal(globalTotal, mergedTotal);
        AssertDictEqual(globalChars, mergedChars!);
        AssertDictEqual(globalBigrams, mergedBigrams!);

        // Sanity: the corpus really does have cross-family overlap the sum had to reconcile
        // (otherwise the test would degenerate to a plain union and prove nothing about SUM).
        Assert.True(famAChars.ContainsKey("禪") && famBChars.ContainsKey("禪"));
        Assert.Equal(famAChars["禪"] + famBChars["禪"], mergedChars!["禪"]);
    }

    [Fact]
    public void Merge_TwoPartials_SumsDisjointAndOverlappingCharSets()
    {
        // Disjoint keys (甲/乙 vs 丙/丁) carry through unchanged; overlapping keys (禪, and
        // bigram 禪心) sum their counts. totalChars adds.
        var pA = (
            (IReadOnlyDictionary<string, int>?)new Dictionary<string, int> { ["甲"] = 2, ["禪"] = 3 },
            (IReadOnlyDictionary<string, int>?)new Dictionary<string, int> { ["禪心"] = 4, ["甲乙"] = 1 },
            10L);
        var pB = (
            (IReadOnlyDictionary<string, int>?)new Dictionary<string, int> { ["丙"] = 5, ["禪"] = 7 },
            (IReadOnlyDictionary<string, int>?)new Dictionary<string, int> { ["禪心"] = 6, ["丙丁"] = 2 },
            20L);

        var (chars, bigrams, total) =
            SearchIndexService.MergeCorpusFreqPartials(new[] { pA, pB });

        Assert.NotNull(chars);
        Assert.NotNull(bigrams);

        // Disjoint chars unchanged.
        Assert.Equal(2, chars!["甲"]);
        Assert.Equal(5, chars["丙"]);
        // Overlapping char summed.
        Assert.Equal(3 + 7, chars["禪"]);
        Assert.Equal(3, chars.Count); // 甲, 禪, 丙

        // Disjoint bigrams unchanged; overlapping bigram summed.
        Assert.Equal(1, bigrams!["甲乙"]);
        Assert.Equal(2, bigrams["丙丁"]);
        Assert.Equal(4 + 6, bigrams["禪心"]);
        Assert.Equal(3, bigrams.Count);

        // totalChars adds.
        Assert.Equal(30L, total);
    }

    [Fact]
    public void Merge_NoLoadedPartials_ReturnsNull()
    {
        // Preserves the pre-FL2 null semantics of CorpusCharFreqs/BigramFreqs (and 0 total)
        // when no family has loaded a partial.
        var (chars, bigrams, total) =
            SearchIndexService.MergeCorpusFreqPartials(new (IReadOnlyDictionary<string, int>?, IReadOnlyDictionary<string, int>?, long)[]
            {
                (null, null, 0L),
                (null, null, 0L),
            });

        Assert.Null(chars);
        Assert.Null(bigrams);
        Assert.Equal(0L, total);
    }

    [Fact]
    public void Merge_SingleLoadedPartial_ReturnsSameReferences()
    {
        // One-family == identical: the sole-family fold returns the SAME dictionary
        // references (no copy), so with today's single family the getters are byte/reference
        // identical to the pre-FL2 code that read the family partial directly. A null-partial
        // sibling family does not participate.
        var cf = (IReadOnlyDictionary<string, int>?)new Dictionary<string, int> { ["禪"] = 9 };
        var bf = (IReadOnlyDictionary<string, int>?)new Dictionary<string, int> { ["禪心"] = 4 };

        var (chars, bigrams, total) =
            SearchIndexService.MergeCorpusFreqPartials(new[]
            {
                (cf, bf, 42L),
                ((IReadOnlyDictionary<string, int>?)null, (IReadOnlyDictionary<string, int>?)null, 0L),
            });

        Assert.Same(cf, chars);
        Assert.Same(bf, bigrams);
        Assert.Equal(42L, total);
    }

    private static void AssertDictEqual(
        IReadOnlyDictionary<string, int> expected,
        IReadOnlyDictionary<string, int> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var kv in expected)
        {
            Assert.True(actual.TryGetValue(kv.Key, out var got),
                $"missing key '{kv.Key}' in merged view");
            Assert.Equal(kv.Value, got);
        }
    }
}
