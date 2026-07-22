using System;
using System.Linq;
using System.Text;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Direct coverage for <see cref="CjkMatchNormalizer.NormalizeStringOnly"/> — the function
/// Option C added and then wired into all three inverted-index gram producers, making it
/// the single choke point that decides which CJK bigrams exist in the index at all.
/// It had no dedicated tests (only indirect exercise through the gram producers), yet its
/// contract carries two load-bearing claims:
///
///   1. it applies the SAME strip policy as <see cref="CjkMatchNormalizer.Normalize"/>, so
///      the index, the query path, verify and KWIC cannot drift apart; and
///   2. its output is byte-for-byte equal to <c>Normalize(raw)</c> for EVERY input.
///
/// Claim 2 is not obvious from the code: <c>NormalizeWithMap</c> (behind <c>Normalize</c>)
/// begins with <c>raw.Replace('　', ' ')</c>, while <c>NormalizeStringOnly</c> has no
/// such line and relies on <see cref="char.IsWhiteSpace(char)"/> already covering U+3000.
/// The two agree only by that coincidence, so it is pinned here over the whole BMP.
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class CjkMatchNormalizerStringOnlyTests
{
    [Fact]
    public void NullAndEmpty_ReturnEmptyString()
    {
        Assert.Equal("", CjkMatchNormalizer.NormalizeStringOnly(null));
        Assert.Equal("", CjkMatchNormalizer.NormalizeStringOnly(""));
    }

    [Fact]
    public void AllStrippedInput_CollapsesToEmptyString()
    {
        // Every char strippable → "" (not null, not the input). The gram producers walk
        // the result with `i < len - 1`, so "" is the safe degenerate case.
        Assert.Equal("", CjkMatchNormalizer.NormalizeStringOnly("  \t\n　、。，「」"));
    }

    [Fact]
    public void StripsWhitespace_IncludingIdeographicSpaceU3000()
    {
        // U+3000 must strip exactly like an ASCII space — this is what bridges a
        // full-width-spaced phrase into a single gram at index time.
        Assert.Equal("山河", CjkMatchNormalizer.NormalizeStringOnly("山 河"));
        Assert.Equal("山河", CjkMatchNormalizer.NormalizeStringOnly("山　河"));
        Assert.Equal("山河", CjkMatchNormalizer.NormalizeStringOnly("山\r\n\t 河"));
    }

    [Fact]
    public void StripsEditorialCjkPunctuation()
    {
        // The modern editorial layer CBETA overlays on the unpunctuated canon.
        Assert.Equal("甲乙", CjkMatchNormalizer.NormalizeStringOnly("甲、乙"));
        Assert.Equal("如是我聞", CjkMatchNormalizer.NormalizeStringOnly("如是，我聞。"));
        Assert.Equal("甲乙", CjkMatchNormalizer.NormalizeStringOnly("「甲」《乙》"));
        Assert.Equal("甲乙", CjkMatchNormalizer.NormalizeStringOnly("甲・乙"));
    }

    [Fact]
    public void StripsSuperscriptAnnotationMarkers()
    {
        // Markers injected by AnnotationMarkerInserter must not block a bigram.
        Assert.Equal("甲乙", CjkMatchNormalizer.NormalizeStringOnly("甲¹乙"));
        Assert.Equal("甲乙", CjkMatchNormalizer.NormalizeStringOnly("甲⁰⁹乙"));
    }

    [Fact]
    public void KeepsNonStrippedCharacters_IncludingLatinAndDigits()
    {
        // Only whitespace + the editorial set strip. Everything else survives verbatim —
        // that is why a retained Latin letter still BLOCKS a straddling bigram.
        Assert.Equal("甲a乙", CjkMatchNormalizer.NormalizeStringOnly("甲a乙"));
        Assert.Equal("abc123", CjkMatchNormalizer.NormalizeStringOnly("abc 123"));
        Assert.Equal("甲.乙", CjkMatchNormalizer.NormalizeStringOnly("甲.乙")); // ASCII dot is NOT editorial
    }

    /// <summary>
    /// The surrogate rule (<c>char.IsSurrogate(c) &amp;&amp; c >= '\uDB00'</c>) is applied PER
    /// CODE UNIT, and its comment reasons only about LEAD surrogates ("CJK Extension B uses
    /// U+D840-U+D869; PUA starts at U+DB00+"). But TRAIL surrogates are ALWAYS U+DC00-U+DFFF
    /// — every one of them is >= U+DB00. So the real behavior is:
    ///   - a PUA annotation icon strips COMPLETELY (both halves are >= U+DB00), as intended;
    ///   - a CJK Ext-B character loses its TRAIL half and leaves a LONE LEAD SURROGATE.
    ///
    /// Pre-existing (it predates Option C — <c>Normalize</c> has always done this) and
    /// harmless for the index, in the conservative direction: the lone lead surrogate fails
    /// the ideograph test, so Ext-B forms no grams AND blocks its neighbours from bridging,
    /// meaning no FALSE bigram is ever invented across it. The Option C plan names this the
    /// "Ext-B surrogate-strip quirk" and accepts it. Pinned here so it is visible behavior
    /// rather than folklore — the corpus is BMP-only in practice.
    /// </summary>
    [Fact]
    public void SupplementarySurrogates_TrailHalfAlwaysStrips_LeavingExtBAsLoneLeadSurrogate()
    {
        // PUA U+F1598 = D85E.. -> lead U+DB85, trail U+DD98: both >= U+DB00 -> vanishes.
        Assert.Equal("甲乙", CjkMatchNormalizer.NormalizeStringOnly("甲\U000F1598乙"));

        // Ext-B U+20000 = D840 DC00: lead D840 < DB00 kept, trail DC00 >= DB00 stripped.
        Assert.Equal("甲\uD840乙", CjkMatchNormalizer.NormalizeStringOnly("甲\U00020000乙"));

        // The payoff: that lone lead surrogate BLOCKS the straddling bigram, so Ext-B never
        // bridges 無/門 into a false 無門 the way a stripped space or comma would.
        Assert.Empty(InvertedSearchIndex.ComputeGramSet("無\U00020000門"));

        // KNOWN CONSEQUENCE (pre-existing, reported not fixed): because only the lead half
        // survives, DISTINCT Ext-B characters sharing a lead surrogate normalize to the
        // SAME string. Verify/KWIC (which normalize both sides) would treat them as equal.
        // The index is unaffected — neither forms a gram — and an Ext-B query is never
        // "authoritative" (surrogates fail IsIndexableCjk), so it still reaches bloom.
        Assert.Equal(
            CjkMatchNormalizer.NormalizeStringOnly("\U00020000"),
            CjkMatchNormalizer.NormalizeStringOnly("\U00020001"));
    }

    // ---- The load-bearing equivalence claim ----

    [Fact]
    public void MatchesNormalize_OnRepresentativeInputs()
    {
        foreach (var raw in new[]
        {
            "", "無門關", "山 河", "山　河", "如是，我聞。一時、佛在「舍衛」國",
            "甲a乙", "  \t\n　、。", "abc 123 無門", "甲¹乙", "甲\U00020000乙",
            "甲\U000F1598乙", "峨\n\t 而", "如如不動", "上堂說法而退",
        })
        {
            Assert.Equal(CjkMatchNormalizer.Normalize(raw), CjkMatchNormalizer.NormalizeStringOnly(raw));
        }
    }

    [Fact]
    public void MatchesNormalize_ForEveryBmpCodeUnit()
    {
        // Full BMP sweep: for every code unit, embed it between two ideographs and require
        // the two normalizers to agree byte-for-byte. This is the real guard on the
        // U+3000 Replace asymmetry described in the class summary, and on any future edit
        // that touches only one of the two functions.
        var sb = new StringBuilder(3);
        for (int cp = 0; cp <= 0xFFFF; cp++)
        {
            sb.Clear();
            sb.Append('無').Append((char)cp).Append('門');
            string raw = sb.ToString();
            string viaMap = CjkMatchNormalizer.Normalize(raw);
            string viaStringOnly = CjkMatchNormalizer.NormalizeStringOnly(raw);
            Assert.True(viaMap == viaStringOnly,
                $"U+{cp:X4}: Normalize -> '{viaMap}' but NormalizeStringOnly -> '{viaStringOnly}'");
        }
    }

    [Fact]
    public void MatchesNormalize_ForEveryBmpCodeUnit_AsSoleCharacter()
    {
        // Same sweep without ideograph neighbours, so a char that strips leaves "" — this
        // catches an asymmetry that only shows at the string boundaries (e.g. a leading /
        // trailing space rule appearing in one function but not the other).
        for (int cp = 0; cp <= 0xFFFF; cp++)
        {
            string raw = ((char)cp).ToString();
            Assert.True(
                CjkMatchNormalizer.Normalize(raw) == CjkMatchNormalizer.NormalizeStringOnly(raw),
                $"U+{cp:X4}: single-char normalization diverged");
        }
    }
}
