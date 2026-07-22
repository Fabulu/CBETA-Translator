using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Behavior tests for <see cref="TranslationQaService"/>.Check — the per-segment
/// QA rule engine. Each rule is exercised in isolation plus a clean-segment
/// negative case. The "chinese-in-en" rule routes through CjkText.ContainsIdeograph
/// since v8.0.0; a boundary case (Latin-only EN with punctuation) pins the negative.
/// </summary>
public sealed class TranslationQaServiceTests
{
    private static readonly TranslationQaService Svc = new();

    private static CurrentSegmentContext Ctx(string zh, string en)
        => new() { RelPath = "T/T48/T48n2005.xml", TextId = "T48n2005", ZhText = zh, EnText = en };

    private static List<QaIssue> Check(string zh, string en, List<TermHit>? terms = null)
        => Svc.Check(Ctx(zh, en), terms ?? new List<TermHit>());

    private static bool Has(List<QaIssue> issues, string ruleId)
        => issues.Any(i => i.RuleId == ruleId);

    [Fact]
    public void CleanSegment_ProducesNoIssues()
    {
        var issues = Check("菩提本無樹", "Bodhi originally has no tree.");
        Assert.Empty(issues);
    }

    [Fact]
    public void EmptyEn_NonEmptyZh_FlagsEmptyEn()
    {
        var issues = Check("菩提本無樹", "   ");
        Assert.True(Has(issues, "empty-en"));
        Assert.Equal(QaSeverity.Warning, issues.Single(i => i.RuleId == "empty-en").Severity);
    }

    [Theory]
    [InlineData("a < b")]
    [InlineData("a > b")]
    public void AngleBrackets_InEn_FlagsError(string en)
    {
        var issues = Check("甲乙", en);
        var issue = issues.Single(i => i.RuleId == "illegal-angle-brackets");
        Assert.Equal(QaSeverity.Error, issue.Severity);
    }

    [Fact]
    public void EnIdenticalToZh_IgnoringSpaces_FlagsSameAsSource()
    {
        var issues = Check("菩提本無樹", "菩提 本 無樹");
        Assert.True(Has(issues, "same-as-source"));
        // Note: this segment also trips chinese-in-en, which is expected.
        Assert.True(Has(issues, "chinese-in-en"));
    }

    [Fact]
    public void ChineseCharsInEn_FlagsError()
    {
        var issues = Check("甲乙丙", "This has a stray 字 character.");
        var issue = issues.Single(i => i.RuleId == "chinese-in-en");
        Assert.Equal(QaSeverity.Error, issue.Severity);
    }

    [Fact]
    public void LatinEnWithPunctuation_DoesNotFlagChineseInEn()
    {
        // Punctuation/digits are outside CjkText's ideograph ranges.
        var issues = Check("甲乙丙丁戊己庚辛壬癸子丑寅卯", "Ten stems, twelve branches (60-cycle).");
        Assert.False(Has(issues, "chinese-in-en"));
    }

    [Fact]
    public void LongZh_ShortEn_FlagsTooShort()
    {
        // zh length (no spaces) > 15 and en words <= 2.
        var issues = Check("菩提本無樹明鏡亦非臺本來無一物何處惹塵埃", "Enlightenment.");
        var issue = issues.Single(i => i.RuleId == "too-short");
        Assert.Equal(QaSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void ShortZh_ShortEn_DoesNotFlagTooShort()
    {
        var issues = Check("甲乙丙", "Two words");
        Assert.False(Has(issues, "too-short"));
    }

    [Fact]
    public void Term_PreferredUsed_NoTermIssue()
    {
        var terms = new List<TermHit>
        {
            new() { SourceTerm = "佛", PreferredTarget = "Buddha", AlternateTargets = { "Awakened One" } }
        };
        var issues = Check("念佛", "Chanting the name of the Buddha.", terms);
        Assert.False(Has(issues, "preferred-term-missing"));
        Assert.False(Has(issues, "recognized-term-unmatched"));
    }

    [Fact]
    public void Term_AlternateUsedNotPreferred_FlagsPreferredMissing()
    {
        var terms = new List<TermHit>
        {
            new() { SourceTerm = "佛", PreferredTarget = "Buddha", AlternateTargets = { "Awakened One" } }
        };
        var issues = Check("念佛", "Chanting the name of the Awakened One.", terms);
        var issue = issues.Single(i => i.RuleId == "preferred-term-missing");
        Assert.Equal(QaSeverity.Warning, issue.Severity);
        Assert.Equal("佛", issue.RelatedTerm);
    }

    [Fact]
    public void Term_NeitherPreferredNorAlternate_FlagsUnmatchedInfo()
    {
        var terms = new List<TermHit>
        {
            new() { SourceTerm = "佛", PreferredTarget = "Buddha", AlternateTargets = { "Awakened One" } }
        };
        var issues = Check("念佛", "Reciting mindfully.", terms);
        var issue = issues.Single(i => i.RuleId == "recognized-term-unmatched");
        Assert.Equal(QaSeverity.Info, issue.Severity);
        Assert.Equal("佛", issue.RelatedTerm);
    }

    [Fact]
    public void Term_BlankPreferredTarget_IsSkipped()
    {
        var terms = new List<TermHit>
        {
            new() { SourceTerm = "佛", PreferredTarget = "", AlternateTargets = { "x" } }
        };
        var issues = Check("念佛", "Something.", terms);
        Assert.False(Has(issues, "preferred-term-missing"));
        Assert.False(Has(issues, "recognized-term-unmatched"));
    }

    [Fact]
    public void Term_UnmatchedButEmptyEn_DoesNotFlagUnmatched()
    {
        // recognized-term-unmatched requires non-empty EN; empty EN should suppress it.
        var terms = new List<TermHit>
        {
            new() { SourceTerm = "佛", PreferredTarget = "Buddha" }
        };
        var issues = Check("念佛", "", terms);
        Assert.False(Has(issues, "recognized-term-unmatched"));
    }
}
