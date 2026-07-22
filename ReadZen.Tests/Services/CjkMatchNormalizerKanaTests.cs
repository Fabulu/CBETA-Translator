using System.Text;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Direct coverage for the kana-classification helpers <see cref="CjkMatchNormalizer.IsHiragana"/>,
/// <see cref="CjkMatchNormalizer.IsKatakana"/> and <see cref="CjkMatchNormalizer.CountKana"/> —
/// the block added to the shared CJK match policy this cycle (v8.0.0 → HEAD, +35 lines).
///
/// These decide whether a commentary line reads as Japanese prose rather than pure-CJK Chinese,
/// so their range boundaries and the deliberate U+30FB carve-out are load-bearing. Before this
/// file they were exercised by a SINGLE assertion in CommentaryLanguageClassifierTests
/// (<c>CountKana(entry.Body) == 2</c>) — nothing pinned the code-point boundaries, the disjointness
/// from CJK Unified, or the middle-dot exclusion. This file does.
///
/// The two sweep tests restate the accepted ranges as independent integer literals (not by calling
/// the production predicate), so an accidental off-by-one at a boundary or a widening of the range
/// is caught, not merely echoed.
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class CjkMatchNormalizerKanaTests
{
    // ----------------------------------------------------------------- IsHiragana

    [Theory]
    [InlineData('぀')] // range start (an unassigned code point, still classified by range)
    [InlineData('あ')] // あ
    [InlineData('ん')] // ん
    [InlineData('ゟ')] // range end
    public void IsHiragana_TrueInsideRange(char c) => Assert.True(CjkMatchNormalizer.IsHiragana(c));

    [Theory]
    [InlineData('〿')] // one below the range
    [InlineData('゠')] // first katakana — not hiragana
    [InlineData('一')] // 一 CJK Unified — disjoint script
    [InlineData('A')]
    [InlineData(' ')]
    public void IsHiragana_FalseOutsideRange(char c) => Assert.False(CjkMatchNormalizer.IsHiragana(c));

    // ----------------------------------------------------------------- IsKatakana

    [Theory]
    [InlineData('゠')] // range start
    [InlineData('カ')] // カ
    [InlineData('ナ')] // ナ
    [InlineData('ー')] // ー prolonged sound mark — genuinely Japanese, must count
    [InlineData('ヿ')] // range end
    public void IsKatakana_TrueInsideRange(char c) => Assert.True(CjkMatchNormalizer.IsKatakana(c));

    [Fact]
    public void IsKatakana_ExcludesMiddleDotU30FB()
    {
        // U+30FB (KATAKANA MIDDLE DOT '・') sits INSIDE the katakana block but is deliberately
        // excluded: it separates syllables in transliterated Chinese personal names, so counting
        // it as katakana would misclassify pure-Chinese text as Japanese. This carve-out is the
        // subtle part of the helper — pin it explicitly.
        Assert.False(CjkMatchNormalizer.IsKatakana('・'));
        // Its immediate neighbours inside the block are still katakana.
        Assert.True(CjkMatchNormalizer.IsKatakana('ヺ'));
        Assert.True(CjkMatchNormalizer.IsKatakana('ー'));
    }

    [Theory]
    [InlineData('ゟ')] // last hiragana — not katakana
    [InlineData('㄀')] // one above the range (Bopomofo)
    [InlineData('一')] // 一 CJK Unified — disjoint script
    [InlineData('a')]
    public void IsKatakana_FalseOutsideRange(char c) => Assert.False(CjkMatchNormalizer.IsKatakana(c));

    // ----------------------------------------------------------- range boundary sweeps

    [Fact]
    public void IsHiragana_MatchesExactRange_OverWholeBmp()
    {
        for (int cp = 0; cp <= 0xFFFF; cp++)
        {
            bool expected = cp >= 0x3040 && cp <= 0x309F;
            Assert.True(expected == CjkMatchNormalizer.IsHiragana((char)cp),
                $"U+{cp:X4}: IsHiragana disagreed with the [U+3040,U+309F] spec");
        }
    }

    [Fact]
    public void IsKatakana_MatchesExactRange_MinusMiddleDot_OverWholeBmp()
    {
        for (int cp = 0; cp <= 0xFFFF; cp++)
        {
            bool expected = cp >= 0x30A0 && cp <= 0x30FF && cp != 0x30FB;
            Assert.True(expected == CjkMatchNormalizer.IsKatakana((char)cp),
                $"U+{cp:X4}: IsKatakana disagreed with the [U+30A0,U+30FF]\\{{U+30FB}} spec");
        }
    }

    // ----------------------------------------------------------------- CountKana

    [Fact]
    public void CountKana_NullAndEmpty_ReturnZero()
    {
        Assert.Equal(0, CjkMatchNormalizer.CountKana(null));
        Assert.Equal(0, CjkMatchNormalizer.CountKana(""));
    }

    [Fact]
    public void CountKana_PureChinese_ReturnsZero()
    {
        // CJK Unified is disjoint from both kana blocks — a purely Chinese line must score 0,
        // which is exactly what lets the classifier tell Chinese apart from Japanese.
        Assert.Equal(0, CjkMatchNormalizer.CountKana("無門關祖師西來意"));
    }

    [Fact]
    public void CountKana_CountsHiraganaAndKatakana_IgnoringOtherScripts()
    {
        // 漢字(CJK,0) と(hiragana,1) カナ(katakana,2) → 3 kana total.
        Assert.Equal(3, CjkMatchNormalizer.CountKana("漢字とカナ"));
        Assert.Equal(4, CjkMatchNormalizer.CountKana("ひらがな"));   // 4 hiragana
        Assert.Equal(4, CjkMatchNormalizer.CountKana("カタカナ"));   // 4 katakana
        Assert.Equal(0, CjkMatchNormalizer.CountKana("Latin 123 ！？"));
    }

    [Fact]
    public void CountKana_DoesNotCountMiddleDot_EvenAmongChinese()
    {
        // A Chinese phrase using the middle dot as a separator scores 0 kana — the whole point of
        // excluding U+30FB from IsKatakana. If it counted, "無・門" would look Japanese.
        Assert.Equal(0, CjkMatchNormalizer.CountKana("無・門"));
        Assert.Equal(0, CjkMatchNormalizer.CountKana("・・・"));
    }

    [Fact]
    public void CountKana_EqualsPerCharClassification_OverRandomizedBmp()
    {
        // Whole-string count must equal the sum of the per-char predicates for every character —
        // guards against a future short-circuit or off-by-one in the CountKana loop.
        var sb = new StringBuilder();
        int expected = 0;
        for (int cp = 0x3000; cp <= 0x3200; cp++) // spans both kana blocks + neighbours + U+30FB
        {
            char c = (char)cp;
            sb.Append(c);
            if (CjkMatchNormalizer.IsHiragana(c) || CjkMatchNormalizer.IsKatakana(c))
                expected++;
        }
        Assert.Equal(expected, CjkMatchNormalizer.CountKana(sb.ToString()));
    }
}
