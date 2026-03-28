using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class VocabularyAnalysisServiceTests
{
    // ---- Test 1: Returns top n-grams sorted by frequency ----

    [Fact]
    public void Analyze_ReturnsNgramsSortedByFrequencyDescending()
    {
        // Two passages sharing the bigram "佛性" multiple times
        var passages = new List<ScholarPassage>
        {
            new() { Id = "p1", ZhText = "佛性即是心性佛性本來", SourceRelPath = "x.xml" },
            new() { Id = "p2", ZhText = "佛性不二佛性圓融", SourceRelPath = "y.xml" }
        };

        var result = VocabularyAnalysisService.Analyze(passages);

        Assert.NotEmpty(result);
        // Results should be in descending order by Count
        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i - 1].Count >= result[i].Count,
                $"Item {i - 1} (count={result[i - 1].Count}) should be >= item {i} (count={result[i].Count})");
        }

        // "佛性" should appear near the top given it repeats across both passages
        var foXing = result.FirstOrDefault(v => v.Phrase == "\u4f5b\u6027");
        Assert.NotNull(foXing);
        Assert.True(foXing!.Count >= 4, "佛性 appears at least 4 times across the two passages");
    }

    // ---- Test 2: Filters stopword-only n-grams ----

    [Fact]
    public void Analyze_FiltersStopwordOnlyNgrams()
    {
        // Text composed almost entirely of stop particles (之乎者也 etc.)
        var passages = new List<ScholarPassage>
        {
            new() { Id = "p1", ZhText = "之乎者也之乎者也矣焉而以", SourceRelPath = "x.xml" },
            new() { Id = "p2", ZhText = "之乎者也之乎者也矣焉而以", SourceRelPath = "y.xml" }
        };

        var result = VocabularyAnalysisService.Analyze(passages);

        // All-stopword n-grams like "之乎", "者也" should be filtered out
        var allStopNgrams = result.Where(v =>
            v.Phrase.All(c => "\u4e4b\u4e4e\u8005\u4e5f\u77e3\u7109\u800c\u4ee5\u70ba\u65bc\u5176\u6240\u5247\u4e43\u82e5\u5982\u96d6\u65e2\u4e14\u7336\u6cc1\u8c48\u84cb\u592b\u60df\u552f\u5373\u9042\u7adf\u4f46\u7136\u54c9\u4e0d\u662f\u6709\u7121\u6b64\u5f7c\u4f55\u4e91".Contains(c)));

        Assert.Empty(allStopNgrams);
    }

    // ---- Test 3: Handles empty passages ----

    [Fact]
    public void Analyze_EmptyPassageList_ReturnsEmpty()
    {
        var result = VocabularyAnalysisService.Analyze(new List<ScholarPassage>());

        Assert.Empty(result);
    }

    // ---- Test 4: Handles passages with no Chinese text ----

    [Fact]
    public void Analyze_PassagesWithNoChinese_ReturnsEmpty()
    {
        var passages = new List<ScholarPassage>
        {
            new() { Id = "p1", ZhText = "", SourceRelPath = "x.xml" },
            new() { Id = "p2", ZhText = "   ", SourceRelPath = "y.xml" },
            new() { Id = "p3", ZhText = "hello world", SourceRelPath = "z.xml" }
        };

        var result = VocabularyAnalysisService.Analyze(passages);

        // "hello world" has Latin chars that survive normalization.
        // Bigrams like "he", "el" may appear but only once each (filtered by >1 requirement).
        // With only one passage, each n-gram appears once, so all are filtered.
        Assert.Empty(result);
    }

    // ---- Test 5: Counts passages correctly ----

    [Fact]
    public void Analyze_PassageCountReflectsDistinctPassages()
    {
        // "法門" (dharma gate) appears in all three passages
        var passages = new List<ScholarPassage>
        {
            new() { Id = "p1", ZhText = "不立法門何以傳心", SourceRelPath = "a.xml" },
            new() { Id = "p2", ZhText = "法門無量誓願學", SourceRelPath = "b.xml" },
            new() { Id = "p3", ZhText = "入此法門即見自性", SourceRelPath = "c.xml" }
        };

        var result = VocabularyAnalysisService.Analyze(passages);

        var faMen = result.FirstOrDefault(v => v.Phrase == "\u6cd5\u9580");
        Assert.NotNull(faMen);
        Assert.Equal(3, faMen!.PassageCount); // appears in all 3 passages
        Assert.True(faMen.Count >= 3); // at least 3 total occurrences (one per passage)
    }

    // ---- Additional: n-gram length range ----

    [Fact]
    public void Analyze_ProducesBigramsTrigramsAndQuadgrams()
    {
        var passages = new List<ScholarPassage>
        {
            new() { Id = "p1", ZhText = "明心見性直指人心", SourceRelPath = "a.xml" },
            new() { Id = "p2", ZhText = "明心見性直指人心", SourceRelPath = "b.xml" } // duplicate to pass >1 filter
        };

        var result = VocabularyAnalysisService.Analyze(passages);

        // Should contain bigrams (len=2), trigrams (len=3), and quadgrams (len=4)
        Assert.Contains(result, v => v.Phrase.Length == 2);
        Assert.Contains(result, v => v.Phrase.Length == 3);
        Assert.Contains(result, v => v.Phrase.Length == 4);
    }

    // ---- N-gram only counted once per passage for PassageCount ----

    [Fact]
    public void Analyze_NgramRepeatedInSamePassage_PassageCountIsOne()
    {
        var passages = new List<ScholarPassage>
        {
            // "佛心" appears twice in same passage
            new() { Id = "p1", ZhText = "佛心佛心佛心", SourceRelPath = "a.xml" },
            new() { Id = "p2", ZhText = "佛心", SourceRelPath = "b.xml" }
        };

        var result = VocabularyAnalysisService.Analyze(passages);

        var foXin = result.FirstOrDefault(v => v.Phrase == "\u4f5b\u5fc3");
        Assert.NotNull(foXin);
        Assert.Equal(2, foXin!.PassageCount); // 2 passages, not 3+
        Assert.True(foXin.Count >= 3); // but total count is higher (multiple occurrences in p1)
    }
}
