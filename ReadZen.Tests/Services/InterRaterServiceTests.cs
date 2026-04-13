using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for InterRaterService: Cohen's kappa, percent agreement, lb extraction,
/// and multi-code comparison.
/// </summary>
public class InterRaterServiceTests
{
    private const string RelPath = "T48/T48n2005.xml";

    // ── Helpers ─────────────────────────────────────────────────────────

    private static List<string> MakeLbs(int count)
    {
        var lbs = new List<string>(count);
        for (int i = 1; i <= count; i++)
            lbs.Add($"p0{i:D3}a01");
        return lbs;
    }

    private static DocumentTag MakeTag(string tagId, string fromLb, string toLb)
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = RelPath,
            FromLb = fromLb,
            ToLb = toLb,
            TagId = tagId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

    private static TagVocabulary MakeVocab(params (string id, string name)[] defs)
    {
        var v = new TagVocabulary();
        foreach (var (id, name) in defs)
            v.Tags.Add(new TagDefinition { Id = id, Name = name, CreatedUtc = DateTimeOffset.UtcNow });
        return v;
    }

    // ── Cohen's kappa unit tests ────────────────────────────────────────

    [Fact]
    public void PerfectAgreement_KappaIs1()
    {
        // a=50, b=0, c=0, d=0 (all both-present)
        double kappa = InterRaterService.ComputeKappa(50, 0, 0, 0);
        Assert.Equal(1.0, kappa, 5);
    }

    [Fact]
    public void PerfectAgreement_AllNeither_KappaIs1()
    {
        // a=0, b=0, c=0, d=50 (all neither)
        double kappa = InterRaterService.ComputeKappa(0, 0, 0, 50);
        Assert.Equal(1.0, kappa, 5);
    }

    [Fact]
    public void KnownContingencyTable_KappaIs0Point4()
    {
        // a=20, b=5, c=10, d=15 -> po=0.7, pe=0.5, kappa=0.4
        double kappa = InterRaterService.ComputeKappa(20, 5, 10, 15);
        Assert.Equal(0.4, kappa, 5);
    }

    [Fact]
    public void EmptyTable_KappaIs1()
    {
        double kappa = InterRaterService.ComputeKappa(0, 0, 0, 0);
        Assert.Equal(1.0, kappa, 5);
    }

    [Fact]
    public void PercentAgreement_Known()
    {
        // (20 + 15) / 50 = 0.7
        double pa = InterRaterService.ComputePercentAgreement(20, 5, 10, 15);
        Assert.Equal(0.7, pa, 5);
    }

    // ── Full Compare tests ──────────────────────────────────────────────

    [Fact]
    public void Compare_PerfectAgreement_BothCoders()
    {
        var lbs = MakeLbs(5);
        var vocab = MakeVocab(("t1", "Theme"));

        // Both coders tag lb 1-3
        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };
        var tags2 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        Assert.Equal(1.0, result.OverallCohensKappa, 5);
        Assert.Equal(1.0, result.OverallPercentAgreement, 5);
        Assert.Single(result.PerCode);
        Assert.Equal("t1", result.PerCode[0].TagId);
        Assert.Equal(3, result.PerCode[0].BothPresent);
        Assert.Equal(2, result.PerCode[0].NeitherPresent);
    }

    [Fact]
    public void Compare_EmptyTagLists_KappaIs1()
    {
        var lbs = MakeLbs(10);
        var result = InterRaterService.Compare(
            RelPath, "alice", "bob", lbs,
            new List<DocumentTag>(), new List<DocumentTag>(),
            null, null);

        // No tags at all => no tag IDs => no per-code entries => overall uses 0/0/0/0 = 1.0
        Assert.Equal(1.0, result.OverallCohensKappa, 5);
        Assert.Empty(result.PerCode);
    }

    [Fact]
    public void Compare_MultipleCodes_PerCodeAndOverall()
    {
        var lbs = MakeLbs(4);
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        // Coder1: t1 on lb1-2, t2 on lb3-4
        // Coder2: t1 on lb1-2, t2 on lb3 only
        var tags1 = new List<DocumentTag>
        {
            MakeTag("t1", lbs[0], lbs[1]),
            MakeTag("t2", lbs[2], lbs[3])
        };
        var tags2 = new List<DocumentTag>
        {
            MakeTag("t1", lbs[0], lbs[1]),
            MakeTag("t2", lbs[2], lbs[2])
        };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        Assert.Equal(2, result.PerCode.Count);
        Assert.Equal(4, result.TotalUnits);

        // t1: a=2, b=0, c=0, d=2 -> perfect agreement
        var t1 = result.PerCode.Find(p => p.TagId == "t1")!;
        Assert.Equal(2, t1.BothPresent);
        Assert.Equal(0, t1.OnlyCoder1);
        Assert.Equal(0, t1.OnlyCoder2);
        Assert.Equal(2, t1.NeitherPresent);
        Assert.Equal(1.0, t1.CohensKappa, 5);

        // t2: a=1 (lb3), b=1 (lb4, only coder1), c=0, d=2 (lb1,lb2)
        var t2 = result.PerCode.Find(p => p.TagId == "t2")!;
        Assert.Equal(1, t2.BothPresent);
        Assert.Equal(1, t2.OnlyCoder1);
        Assert.Equal(0, t2.OnlyCoder2);
        Assert.Equal(2, t2.NeitherPresent);

        // Overall micro-average: a=3, b=1, c=0, d=4
        Assert.Equal(8, result.TotalUnits * 2); // 4 units * 2 codes - but that's not what TotalUnits is
        Assert.Equal(4, result.TotalUnits); // just lb count
    }

    [Fact]
    public void Compare_IgnoresTagsFromOtherDocument()
    {
        var lbs = MakeLbs(3);
        var vocab = MakeVocab(("t1", "Theme"));

        var tags1 = new List<DocumentTag>
        {
            MakeTag("t1", lbs[0], lbs[2]),
            new DocumentTag
            {
                Id = Guid.NewGuid().ToString("N"),
                RelPath = "OTHER/file.xml", // different document
                FromLb = lbs[0],
                ToLb = lbs[2],
                TagId = "t1",
                CreatedUtc = DateTimeOffset.UtcNow
            }
        };
        var tags2 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);
        Assert.Equal(1.0, result.OverallCohensKappa, 5);
    }

    // ── Additional edge-case tests ─────────────────────────────────────

    [Fact]
    public void Compare_SingleLb_BothSameCode_KappaIs1()
    {
        var lbs = MakeLbs(1);
        var vocab = MakeVocab(("t1", "Theme"));

        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[0]) };
        var tags2 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[0]) };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        Assert.Equal(1.0, result.OverallCohensKappa, 5);
        Assert.Equal(1.0, result.OverallPercentAgreement, 5);
        Assert.Single(result.PerCode);
        Assert.Equal(1, result.PerCode[0].BothPresent);
        Assert.Equal(0, result.PerCode[0].NeitherPresent);
    }

    [Fact]
    public void Compare_SingleLb_DifferentCodes_ReflectsDisagreement()
    {
        var lbs = MakeLbs(1);
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        // Coder1 tags with t1, coder2 tags with t2 — different codes on same lb
        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[0]) };
        var tags2 = new List<DocumentTag> { MakeTag("t2", lbs[0], lbs[0]) };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        Assert.Equal(2, result.PerCode.Count);

        // t1: a=0 (only coder1 has it), b=1, c=0, d=0
        var t1 = result.PerCode.Find(p => p.TagId == "t1")!;
        Assert.Equal(0, t1.BothPresent);
        Assert.Equal(1, t1.OnlyCoder1);
        Assert.Equal(0, t1.OnlyCoder2);
        Assert.Equal(0, t1.NeitherPresent);

        // t2: a=0, b=0, c=1 (only coder2), d=0
        var t2 = result.PerCode.Find(p => p.TagId == "t2")!;
        Assert.Equal(0, t2.BothPresent);
        Assert.Equal(0, t2.OnlyCoder1);
        Assert.Equal(1, t2.OnlyCoder2);
        Assert.Equal(0, t2.NeitherPresent);

        // Overall: sumA=0, sumB=1, sumC=1, sumD=0 -> po=0, pe=0.5, kappa=-1
        Assert.Equal(0.0, result.OverallPercentAgreement, 5);
        Assert.True(result.OverallCohensKappa < 0, "Kappa should be negative for total disagreement");
    }

    [Fact]
    public void Compare_OverlappingLbRanges_Lb4InsideBoth()
    {
        // 7 lbs: tag1 covers lb1-lb5, tag2 covers lb3-lb7
        var lbs = MakeLbs(7);
        var vocab = MakeVocab(("t1", "Theme"));

        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[4]) }; // lb1-lb5
        var tags2 = new List<DocumentTag> { MakeTag("t1", lbs[2], lbs[6]) }; // lb3-lb7

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        var t1 = result.PerCode.Find(p => p.TagId == "t1")!;
        // lb1,lb2: only coder1 (b=2)
        // lb3,lb4,lb5: both (a=3) — lb4 is inside both ranges
        // lb6,lb7: only coder2 (c=2)
        Assert.Equal(3, t1.BothPresent);
        Assert.Equal(2, t1.OnlyCoder1);
        Assert.Equal(2, t1.OnlyCoder2);
        Assert.Equal(0, t1.NeitherPresent);
    }

    [Fact]
    public void Compare_CaseSensitiveLbs()
    {
        // Ordinal comparison: "A" < "a" in ordinal, so "Abc" != "abc"
        // Use mixed-case lbs and verify they're treated as distinct
        var lbs = new List<string> { "pA01", "pa01", "pB01" };
        var vocab = MakeVocab(("t1", "Theme"));

        // Coder1 tags "pA01" to "pA01", coder2 tags "pa01" to "pa01"
        // These are different lb values; no overlap expected
        var tags1 = new List<DocumentTag> { MakeTag("t1", "pA01", "pA01") };
        var tags2 = new List<DocumentTag> { MakeTag("t1", "pa01", "pa01") };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        var t1 = result.PerCode.Find(p => p.TagId == "t1")!;
        // "pA01" only coder1, "pa01" only coder2, "pB01" neither
        Assert.Equal(0, t1.BothPresent);
        Assert.Equal(1, t1.OnlyCoder1);
        Assert.Equal(1, t1.OnlyCoder2);
        Assert.Equal(1, t1.NeitherPresent);
    }

    [Fact]
    public void Compare_LargeDataset_KappaBetweenMinus1And1()
    {
        var rng = new Random(42); // seeded for reproducibility
        int lbCount = 100;
        var lbs = MakeLbs(lbCount);
        var codes = new[] { "c1", "c2", "c3", "c4", "c5" };
        var vocabDefs = codes.Select(c => (c, c.ToUpperInvariant())).ToArray();
        var vocab = MakeVocab(vocabDefs);

        var tags1 = new List<DocumentTag>();
        var tags2 = new List<DocumentTag>();

        // Randomly assign tags: each coder has ~30% chance of tagging each lb with each code
        for (int i = 0; i < lbCount; i++)
        {
            foreach (var code in codes)
            {
                if (rng.NextDouble() < 0.3)
                    tags1.Add(MakeTag(code, lbs[i], lbs[i]));
                if (rng.NextDouble() < 0.3)
                    tags2.Add(MakeTag(code, lbs[i], lbs[i]));
            }
        }

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        Assert.InRange(result.OverallCohensKappa, -1.0, 1.0);
        Assert.InRange(result.OverallPercentAgreement, 0.0, 1.0);
        Assert.Equal(5, result.PerCode.Count);
        foreach (var pc in result.PerCode)
        {
            Assert.InRange(pc.CohensKappa, -1.0, 1.0);
            Assert.InRange(pc.PercentAgreement, 0.0, 1.0);
        }
    }

    [Fact]
    public void Compare_AllDisagreement_KappaIsNegativeOrZero()
    {
        var lbs = MakeLbs(10);
        var vocab = MakeVocab(("cA", "Code A"), ("cB", "Code B"));

        // Coder1 tags everything with cA, coder2 tags everything with cB
        var tags1 = new List<DocumentTag>();
        var tags2 = new List<DocumentTag>();
        foreach (var lb in lbs)
        {
            tags1.Add(MakeTag("cA", lb, lb));
            tags2.Add(MakeTag("cB", lb, lb));
        }

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        // Per code: cA has b=10,c=0,a=0,d=0 ; cB has b=0,c=10,a=0,d=0
        // Overall: sumA=0, sumB=10, sumC=10, sumD=0 -> po=0, pe=0.5, kappa=-1
        Assert.True(result.OverallCohensKappa <= 0,
            $"Expected kappa <= 0 for total disagreement, got {result.OverallCohensKappa}");
        Assert.Equal(0.0, result.OverallPercentAgreement, 5);
    }

    [Fact]
    public void Compare_OpenZenSyntheticLbs_ContainmentWorks()
    {
        // Dot-separated IDs like "wm32.case01.l01" — ordinal compare should still work
        var lbs = new List<string>
        {
            "wm32.case01.l01",
            "wm32.case01.l02",
            "wm32.case01.l03",
            "wm32.case01.l04",
            "wm32.case01.l05"
        };
        var vocab = MakeVocab(("t1", "Koan"));

        // Range from l02 to l04
        var tags1 = new List<DocumentTag> { MakeTag("t1", "wm32.case01.l02", "wm32.case01.l04") };
        var tags2 = new List<DocumentTag> { MakeTag("t1", "wm32.case01.l02", "wm32.case01.l04") };

        var result = InterRaterService.Compare(RelPath, "alice", "bob", lbs, tags1, tags2, vocab, vocab);

        Assert.Equal(1.0, result.OverallCohensKappa, 5);

        var t1 = result.PerCode[0];
        Assert.Equal(3, t1.BothPresent);     // l02, l03, l04
        Assert.Equal(2, t1.NeitherPresent);  // l01, l05
    }
}
