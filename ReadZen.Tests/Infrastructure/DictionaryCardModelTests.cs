// DictionaryCardModelTests — pure presentation logic for the reader-facing Zen dictionary card
// (wave 2 renderer). Exercises the render-time normalizations from DICTIONARY_DISPLAY_DESIGN.md:
// §0 AttributionNote split, §3/§4 single-vs-multi sense, §7 curated-first + collapse gate,
// §5.4 nested-master provenance, §6 validation signal, §4.3 related-region uniformity.

using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Termbase")]
public class DictionaryCardModelTests
{
    // ---- §0 AttributionNote split ----

    [Fact]
    public void SplitAttributionNote_StandardShape_PromotesTitleAndGloss()
    {
        // The 無字 gold-standard witness note.
        const string note =
            "Source record (T/T48/T48n2005.xml). The Gateless Barrier (無門關): " +
            "Wumen Huikai calls this one word ‘no’ the single barrier of the school.";

        var parts = DictionaryCardModel.SplitAttributionNote(note);

        Assert.True(parts.Matched);
        Assert.Equal("The Gateless Barrier", parts.WorkTitleEnglish);
        Assert.Equal("無門關", parts.WorkTitleChinese);
        Assert.Equal("Wumen Huikai calls this one word ‘no’ the single barrier of the school.", parts.Gloss);
        // The redundant path prefix is dropped, not surfaced.
        Assert.DoesNotContain("Source record", parts.Gloss);
    }

    [Fact]
    public void SplitAttributionNote_MissingPattern_FallsBackToWholeNote()
    {
        const string note = "Just some free-form prose with no colon-delimited title header at all";
        var parts = DictionaryCardModel.SplitAttributionNote(note);

        Assert.False(parts.Matched);
        Assert.Null(parts.WorkTitleEnglish);
        Assert.Equal(note, parts.Gloss); // whole note printed, never a blank gloss (§9)
    }

    [Fact]
    public void SplitAttributionNote_Null_IsEmptyAndUnmatched()
    {
        var parts = DictionaryCardModel.SplitAttributionNote(null);
        Assert.False(parts.Matched);
        Assert.Equal("", parts.Gloss);
    }

    [Fact]
    public void SplitAttributionNote_AllChineseTitle_KeepsTitleVisible()
    {
        // A record-owner note whose title carries only a Chinese work name in parens.
        const string note =
            "Source record (C/C077/C077n1710.xml). Recorded Sayings of Ancient Venerable Masters (古尊宿語錄): " +
            "Baizhang Huaihai owns the exact headword-bearing wording in the complete source unit.";
        var parts = DictionaryCardModel.SplitAttributionNote(note);

        Assert.True(parts.Matched);
        Assert.Equal("Recorded Sayings of Ancient Venerable Masters", parts.WorkTitleEnglish);
        Assert.Equal("古尊宿語錄", parts.WorkTitleChinese);
    }

    // ---- §3/§4 sense selection ----

    [Fact]
    public void IsSingleSense_Distinguishes()
    {
        Assert.True(DictionaryCardModel.IsSingleSense(Entry(Sense())));
        Assert.False(DictionaryCardModel.IsSingleSense(Entry(Sense(), Sense())));
        Assert.False(DictionaryCardModel.IsSingleSense(null));
    }

    // ---- §7.2 curated-first ordering (stable) ----

    [Fact]
    public void OrderCuratedFirst_PutsCuratedFirst_PreservingStoredOrderWithinTier()
    {
        var a = Occ("A", curated: false);
        var b = Occ("B", curated: true);
        var c = Occ("C", curated: false);
        var d = Occ("D", curated: true);

        var ordered = DictionaryCardModel.OrderCuratedFirst(new[] { a, b, c, d });

        Assert.Equal(new[] { "B", "D", "A", "C" }, System.Linq.Enumerable.Select(ordered, o => o.RelPath));
    }

    // ---- §7.3 collapse gate (> ~6 witnesses) ----

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, false)] // at the median — no wall to fold
    [InlineData(7, true)]  // the 無字 case (7 witnesses) earns the control
    [InlineData(14, true)]
    public void ShouldOfferCollapse_OnlyForLongSenses(int witnessCount, bool expected)
    {
        var sense = Sense();
        for (int i = 0; i < witnessCount; i++)
            sense.Occurrences.Add(Occ("W" + i, curated: true));

        Assert.Equal(expected, DictionaryCardModel.ShouldOfferCollapse(sense));
    }

    // ---- §5.4 nested-master provenance ----

    [Fact]
    public void BuildProvenanceLine_SingleContextMaster_IsNull()
    {
        var occ = Occ("T/x.xml", curated: true);
        occ.ContextMasters.Add(new DictContextMaster
        {
            MasterName = "Wumen Huikai",
            Roles = new List<string> { "utterer", "commentator", "record-owner" },
        });

        Assert.Null(DictionaryCardModel.BuildProvenanceLine(occ));
    }

    [Fact]
    public void BuildProvenanceLine_VoicePlusCaseFigure_ReadsHumanly()
    {
        // 無字 witness 3: Dahui (utterer/record-owner) raising Zhaozhou (case-figure).
        var occ = Occ("T/x.xml", curated: true);
        occ.ContextMasters.Add(new DictContextMaster
        {
            MasterName = "Dahui Zonggao",
            Roles = new List<string> { "utterer", "record-owner" },
        });
        occ.ContextMasters.Add(new DictContextMaster
        {
            MasterName = "Zhaozhou Congshen",
            Roles = new List<string> { "case-figure" },
        });

        Assert.Equal(
            "raised by Dahui Zonggao · on Zhaozhou Congshen's case",
            DictionaryCardModel.BuildProvenanceLine(occ));
    }

    [Fact]
    public void BuildProvenanceLine_OrdersVoiceBeforeSubject_RegardlessOfStoredOrder()
    {
        var occ = Occ("T/x.xml", curated: true);
        // Subject stored first, voice second — output must still lead with the voice.
        occ.ContextMasters.Add(new DictContextMaster
        {
            MasterName = "Fenyang Shanzhao",
            Roles = new List<string> { "person-discussed" },
        });
        occ.ContextMasters.Add(new DictContextMaster
        {
            MasterName = "Yongjue Yuanxian",
            Roles = new List<string> { "utterer", "record-owner", "commentator" },
        });

        Assert.Equal(
            "raised by Yongjue Yuanxian · on Fenyang Shanzhao",
            DictionaryCardModel.BuildProvenanceLine(occ));
    }

    // ---- §6 validation signal ----

    [Theory]
    [InlineData("multi-source", null)]
    [InlineData("", null)]
    [InlineData("single-source", "attested in a single source")]
    [InlineData("single-source-explicit", "attested in a single source")]
    [InlineData("provisional", "provisional")]
    [InlineData("disputed", "disputed reading")]
    public void GetValidationSignal_MapsPerDesign(string validation, string? expectedLabel)
    {
        var signal = DictionaryCardModel.GetValidationSignal(validation);
        if (expectedLabel == null)
        {
            Assert.Null(signal); // multi-source shows NOTHING (§6)
        }
        else
        {
            Assert.NotNull(signal);
            Assert.Equal(expectedLabel, signal!.Label);
        }
    }

    [Fact]
    public void GetValidationSignal_DisputedIsTheOnlyTint()
    {
        Assert.Equal(ValidationTier.Disputed, DictionaryCardModel.GetValidationSignal("disputed")!.Tier);
        Assert.Equal(ValidationTier.Quiet, DictionaryCardModel.GetValidationSignal("provisional")!.Tier);
        Assert.Equal(ValidationTier.Quiet, DictionaryCardModel.GetValidationSignal("single-source")!.Tier);
    }

    // ---- §4.3 related-region uniformity ----

    [Fact]
    public void RelatedRegionsUniform_SingleSense_IsTrue()
    {
        Assert.True(DictionaryCardModel.RelatedRegionsUniform(Entry(Sense())));
    }

    [Fact]
    public void RelatedRegionsUniform_DifferingSenses_IsFalse()
    {
        var s1 = Sense();
        s1.RelatedTerms = new List<string> { "一句" };
        var s2 = Sense();
        s2.RelatedTerms = new List<string> { "末後句" };

        Assert.False(DictionaryCardModel.RelatedRegionsUniform(Entry(s1, s2)));
    }

    [Fact]
    public void RelatedRegionsUniform_IdenticalSenses_IgnoresOrder()
    {
        var s1 = Sense();
        s1.RelatedTerms = new List<string> { "A", "B" };
        s1.RelatedMasters = new List<string> { "M1" };
        var s2 = Sense();
        s2.RelatedTerms = new List<string> { "B", "A" }; // same set, different order
        s2.RelatedMasters = new List<string> { "M1" };

        Assert.True(DictionaryCardModel.RelatedRegionsUniform(Entry(s1, s2)));
    }

    // ---- §5.1 line ref ----

    [Theory]
    [InlineData("0292c27", "0292c28", "0292c27–0292c28")]
    [InlineData("0619a18", "0619a18", "0619a18")] // From==To collapses
    [InlineData("0292c27", "", "0292c27")]
    [InlineData("", "", "")]
    public void FormatLineRef_Formats(string from, string to, string expected)
    {
        Assert.Equal(expected, DictionaryCardModel.FormatLineRef(from, to));
    }

    // ---- helpers ----

    private static DictionaryEntry Entry(params DictionarySense[] senses)
    {
        var e = new DictionaryEntry { SourceTerm = "測", Id = "t_test" };
        e.Senses.AddRange(senses);
        return e;
    }

    private static DictionarySense Sense() => new() { PreferredTarget = "gloss" };

    private static DictOccurrence Occ(string relPath, bool curated) =>
        new() { RelPath = relPath, Curated = curated };
}
