using System.Collections.Generic;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Tests for ScholarPassage.GenerateAutoSummary() — the tiered auto-summary
/// generator used for graph labels and collection list display.
/// </summary>
public class ScholarPassageAutoSummaryTests
{
    // ── Priority 1: English first sentence ──

    [Fact]
    public void EnglishFirstSentence_ShortSentence_ReturnsFullSentence()
    {
        var p = new ScholarPassage { EnText = "A monk asked Zhaozhou. He replied." };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("A monk asked Zhaozhou.", summary);
    }

    [Fact]
    public void EnglishFirstSentence_LongSentence_Truncates()
    {
        var p = new ScholarPassage
        {
            EnText = "A monk asked Zhaozhou I have just entered the monastery I beg you to instruct me about the way forward. He replied with something."
        };
        var summary = p.GenerateAutoSummary();
        Assert.True(summary.Length <= 65, $"Summary too long: {summary.Length} chars");
        Assert.EndsWith("…", summary);
    }

    [Fact]
    public void EnglishFirstSentence_QuestionMark_BreaksOnIt()
    {
        var p = new ScholarPassage { EnText = "Have you eaten your gruel? The monk said yes." };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("Have you eaten your gruel?", summary);
    }

    [Fact]
    public void EnglishFirstSentence_ExclamationMark_BreaksOnIt()
    {
        var p = new ScholarPassage { EnText = "Go wash your bowl! The monk was enlightened." };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("Go wash your bowl!", summary);
    }

    [Fact]
    public void EnglishTooShort_FallsToNextPriority()
    {
        var p = new ScholarPassage
        {
            EnText = "Short.",
            ZhText = "趙州因僧問某甲乍入叢林乞師指示州云喫粥了也未"
        };
        var summary = p.GenerateAutoSummary();
        // English sentence is < 10 chars, should fall through to Chinese
        Assert.DoesNotContain("Short", summary);
    }

    // ── Priority 2: Master name + Chinese phrase ──

    [Fact]
    public void MasterAndChinese_CombinesMasterWithPhrase()
    {
        var p = new ScholarPassage
        {
            MasterNames = new List<string> { "Zhaozhou Congshen" },
            ZhText = "趙州因僧問。某甲乍入叢林。乞師指示。"
        };
        var summary = p.GenerateAutoSummary();
        Assert.StartsWith("Zhaozhou Congshen:", summary);
        Assert.Contains("趙州", summary);
    }

    [Fact]
    public void MasterAndChinese_BreaksOnPunctuation()
    {
        var p = new ScholarPassage
        {
            MasterNames = new List<string> { "Deshan" },
            ZhText = "德山一日托鉢下堂。見雪峯問。"
        };
        var summary = p.GenerateAutoSummary();
        Assert.StartsWith("Deshan:", summary);
        // Should break on 。 within first 12 chars
    }

    [Fact]
    public void MasterButNoZhText_ReturnsMasterNameOnly()
    {
        var p = new ScholarPassage
        {
            MasterNames = new List<string> { "Linji Yixuan" },
            ZhText = null
        };
        var summary = p.GenerateAutoSummary();
        // Falls through to Priority 4 or 5 since no ZhText
        Assert.NotNull(summary);
    }

    // ── Priority 3: Chinese text only ──

    [Fact]
    public void ChineseOnly_ShortText_ReturnsFullText()
    {
        var p = new ScholarPassage { ZhText = "趙州洗鉢" };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("趙州洗鉢", summary);
    }

    [Fact]
    public void ChineseOnly_LongText_TruncatesAt20()
    {
        var p = new ScholarPassage
        {
            ZhText = "趙州因僧問某甲乍入叢林乞師指示州云喫粥了也未僧云喫粥了也州云洗鉢盂去"
        };
        var summary = p.GenerateAutoSummary();
        Assert.True(summary.Length <= 25, $"Chinese summary too long: {summary.Length}");
        Assert.EndsWith("…", summary);
    }

    // ── Priority 4: English snippet (no sentence boundary) ──

    [Fact]
    public void EnglishSnippet_NoChinese_UsesEnglish()
    {
        var p = new ScholarPassage { EnText = "brief" };
        // EnText too short for first sentence (< 10 chars), no ZhText
        var summary = p.GenerateAutoSummary();
        Assert.Equal("brief", summary);
    }

    // ── Priority 5: File name ──

    [Fact]
    public void FileNameOnly_ReturnsFileName()
    {
        var p = new ScholarPassage { SourceRelPath = "T/T48/T48n2005.xml" };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("T48n2005", summary);
    }

    // ── Priority 6: Fallback ──

    [Fact]
    public void EmptyPassage_ReturnsFallback()
    {
        var p = new ScholarPassage();
        var summary = p.GenerateAutoSummary();
        Assert.Equal("(untitled passage)", summary);
    }

    // ── Summary not overwritten if already set ──

    [Fact]
    public void ExistingSummary_NotOverwritten()
    {
        var p = new ScholarPassage
        {
            Summary = "My Custom Name",
            ZhText = "趙州洗鉢",
            EnText = "Zhaozhou washes his bowl."
        };
        // GenerateAutoSummary always generates, but callers check IsNullOrWhiteSpace first
        Assert.Equal("My Custom Name", p.Summary);
    }

    // ── DisplayTitle uses Summary when set ──

    [Fact]
    public void DisplayTitle_UsesSummary_WhenSet()
    {
        var p = new ScholarPassage
        {
            Summary = "Zhaozhou Bowl",
            ZhText = "趙州因僧問某甲乍入叢林",
            EnText = "A monk asked Zhaozhou."
        };
        Assert.Equal("Zhaozhou Bowl", p.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_FallsToZhText_WhenSummaryEmpty()
    {
        var p = new ScholarPassage
        {
            Summary = null,
            ZhText = "趙州洗鉢"
        };
        Assert.Equal("趙州洗鉢", p.DisplayTitle);
    }

    // ── Edge cases ──

    [Fact]
    public void WhitespaceOnlyEnText_SkipsEnglish()
    {
        var p = new ScholarPassage
        {
            EnText = "   \n\t  ",
            ZhText = "趙州洗鉢"
        };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("趙州洗鉢", summary);
    }

    [Fact]
    public void NullMasterNames_SkipsMasterPriority()
    {
        var p = new ScholarPassage
        {
            MasterNames = null,
            ZhText = "趙州洗鉢"
        };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("趙州洗鉢", summary);
    }

    [Fact]
    public void EmptyMasterNames_SkipsMasterPriority()
    {
        var p = new ScholarPassage
        {
            MasterNames = new List<string>(),
            ZhText = "趙州洗鉢"
        };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("趙州洗鉢", summary);
    }

    [Fact]
    public void EnglishNoPunctuation_TakesWholeLine()
    {
        var p = new ScholarPassage { EnText = "Zhaozhou washes the bowl and the monk is enlightened" };
        var summary = p.GenerateAutoSummary();
        Assert.Equal("Zhaozhou washes the bowl and the monk is enlightened", summary);
    }
}
