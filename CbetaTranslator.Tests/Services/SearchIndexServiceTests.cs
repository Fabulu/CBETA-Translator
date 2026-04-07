using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class SearchIndexServiceTests
{
    [Fact]
    public void SearchResultGroup_ApplyEnrichment_MutatesExistingChildren_WhenShapeMatches()
    {
        var existingChild = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("zh-left ", "??", " zh-right")
        };
        var group = new SearchResultGroup
        {
            RelPath = "T/T01/T01n0001.xml",
            Children = new List<SearchResultChild> { existingChild }
        };

        var enrichedChild = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("zh-left ", "??", " zh-right"),
            SecondaryHit = Hit("en-left ", "barrier", " en-right"),
            SecondaryIsContextOnly = false
        };

        group.ApplyEnrichment(new List<SearchResultChild> { enrichedChild });

        Assert.Same(existingChild, group.Children[0]);
        Assert.Equal("en-left barrier en-right", existingChild.SecondarySnippetText);
        Assert.False(existingChild.SecondaryIsContextOnly);
    }

    [Fact]
    public void SearchResultGroup_ApplyEnrichment_ReplacesChildren_WhenShapeChanges()
    {
        var existingChild = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("zh-left ", "??", " zh-right")
        };
        var group = new SearchResultGroup
        {
            RelPath = "T/T01/T01n0001.xml",
            Children = new List<SearchResultChild> { existingChild }
        };

        var enrichedChildren = new List<SearchResultChild>
        {
            new()
            {
                RelPath = "T/T01/T01n0001.xml",
                Side = SearchSide.Original,
                Hit = Hit("zh-left ", "??", " zh-right"),
                SecondaryHit = Hit("en-left ", string.Empty, string.Empty),
                SecondaryIsContextOnly = true
            },
            new()
            {
                RelPath = "T/T01/T01n0001.xml",
                Side = SearchSide.Translated,
                Hit = Hit("en-left ", "barrier", " en-right")
            }
        };

        group.ApplyEnrichment(enrichedChildren);

        Assert.Equal(2, group.Children.Count);
        Assert.NotSame(existingChild, group.Children[0]);
        Assert.Equal(SearchSide.Translated, group.Children[1].Side);
    }

    private static SearchHit Hit(string left, string match, string right)
        => new() { Left = left, Match = match, Right = right };

    [Fact]
    public void BuildResultChildren_PairsOppositeSideHitsByOrdinal()
    {
        var originalHits = new List<SearchHit>
        {
            Hit("zh-l1 ", "\u5B57\u4E00", " zh-r1"),
            Hit("zh-l2 ", "\u5B57\u4E8C", " zh-r2")
        };

        var translatedHits = new List<SearchHit>
        {
            Hit("en-l1 ", "word1", " en-r1")
        };

        var children = SearchIndexService.BuildResultChildren("T/T01/T01n0001.xml", originalHits, translatedHits);

        Assert.Equal(3, children.Count);

        Assert.Equal(SearchSide.Original, children[0].Side);
        Assert.Equal("zh-l1 \u5B57\u4E00 zh-r1", children[0].PrimarySnippetText);
        Assert.Equal("en-l1 word1 en-r1", children[0].SecondarySnippetText);
        Assert.True(children[0].HasSecondaryDisplayText);

        Assert.Equal(SearchSide.Original, children[1].Side);
        Assert.Equal("zh-l2 \u5B57\u4E8C zh-r2", children[1].PrimarySnippetText);
        Assert.False(children[1].HasSecondaryDisplayText);

        Assert.Equal(SearchSide.Translated, children[2].Side);
        Assert.Equal("en-l1 word1 en-r1", children[2].PrimarySnippetText);
        Assert.Equal("zh-l1 \u5B57\u4E00 zh-r1", children[2].SecondarySnippetText);
        Assert.True(children[2].HasSecondaryDisplayText);
    }

    [Fact]
    public void BuildResultChildren_ContextOnlyCounterpartRows_AreMarkedAsContextOnly()
    {
        var originalHits = new List<SearchHit>
        {
            Hit("zh-l1 ", "\u5B57\u4E00", " zh-r1"),
            Hit("zh-l2 ", "\u5B57\u4E8C", " zh-r2")
        };

        var translatedHits = new List<SearchHit>
        {
            Hit("en-l1 ", "word1", " en-r1"),
            new() { Left = "context only counterpart", Match = string.Empty, Right = string.Empty }
        };

        var children = SearchIndexService.BuildResultChildren("T/T01/T01n0001.xml", originalHits, translatedHits);

        Assert.False(children[0].PrimaryIsContextOnly);
        Assert.False(children[0].SecondaryIsContextOnly);
        Assert.False(children[1].PrimaryIsContextOnly);
        Assert.True(children[1].SecondaryIsContextOnly);
        Assert.True(children[3].PrimaryIsContextOnly);
        Assert.Equal("context only counterpart", children[3].PrimarySnippetText);
    }

    [Fact]
    public void ToScholarPassage_CarriesBothLanguagesWhenPaired()
    {
        var child = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("\u5DE6\u6587 ", "\u4E2D", " \u53F3\u6587"),
            SecondaryHit = Hit("left ", "match", " right")
        };

        var passage = child.ToScholarPassage();

        Assert.Equal("T/T01/T01n0001.xml", passage.SourceRelPath);
        Assert.Equal("\u5DE6\u6587 \u4E2D \u53F3\u6587", passage.ZhText);
        Assert.Equal("left match right", passage.EnText);
        Assert.NotEqual(default, passage.AddedUtc);
        Assert.False(string.IsNullOrWhiteSpace(passage.Id));
    }

    [Fact]
    public void ToScholarPassage_OmitsMissingCounterpart()
    {
        var child = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Translated,
            Hit = Hit("left ", "match", " right")
        };

        var passage = child.ToScholarPassage();

        Assert.Equal("T/T01/T01n0001.xml", passage.SourceRelPath);
        Assert.Equal("left match right", passage.EnText);
        Assert.Equal(string.Empty, passage.ZhText);
    }

    [Fact]
    public void BuildCounterpartHitsFromIndexedUnits_ContextOnlyHit_UsesPlainSnippetWithoutMatchSegment()
    {
        var doc = new IndexedTranslationDocument();
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u8D99\u5DDE\u554F\u4F5B\u6CD5", En = "Zhaozhou asked about the Dharma." });

        var hits = SearchIndexService.BuildCounterpartHitsFromIndexedUnits(
            doc,
            "\u4F5B\u6CD5",
            SearchSide.Original,
            neededCount: 1,
            contextWidth: 40);

        var hit = Assert.Single(hits);
        Assert.Equal(string.Empty, hit.Match);
        Assert.Equal("Zhaozhou asked about the Dharma.", hit.Left);
        Assert.Equal(string.Empty, hit.Right);
    }

    [Fact]
    public void BuildCounterpartHitsFromIndexedUnits_ForChineseQuery_ReturnsEnglishCounterparts()
    {
        var doc = new IndexedTranslationDocument();
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u8D99\u5DDE\u554F\u4F5B\u6CD5", En = "Zhaozhou asked about the Dharma." });
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u7121\u9580\u66F0", En = "Wumen said." });

        var hits = SearchIndexService.BuildCounterpartHitsFromIndexedUnits(
            doc,
            "\u4F5B\u6CD5",
            SearchSide.Original,
            neededCount: 1,
            contextWidth: 40);

        Assert.Single(hits);
        Assert.Equal(string.Empty, hits[0].Match);
        Assert.Equal("Zhaozhou asked about the Dharma.", hits[0].Left);
    }

    [Fact]
    public void BuildCounterpartHitsFromIndexedUnits_ForEnglishQuery_ReturnsChineseCounterparts()
    {
        var doc = new IndexedTranslationDocument();
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u8D99\u5DDE\u554F\u4F5B\u6CD5", En = "Zhaozhou asked about the Dharma." });
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u7121\u9580\u66F0", En = "Wumen said." });

        var hits = SearchIndexService.BuildCounterpartHitsFromIndexedUnits(
            doc,
            "Dharma",
            SearchSide.Translated,
            neededCount: 1,
            contextWidth: 40);

        Assert.Single(hits);
        Assert.Equal(string.Empty, hits[0].Match);
        Assert.Equal("\u8D99\u5DDE\u554F\u4F5B\u6CD5", hits[0].Left);
    }

    [Fact]
    public void EnumerateTranslatedCounterpartDirs_PrefersActiveDirThenCanonicalXmlP5t()
    {
        string root = Path.Combine(Path.GetTempPath(), "rz-search-" + Guid.NewGuid().ToString("N"));
        string originalDir = Path.Combine(root, "xml-p5");
        string personalTranslatedDir = Path.Combine(root, "community", "translations", "dota2nub");
        string canonicalTranslatedDir = Path.Combine(root, "xml-p5t");

        Directory.CreateDirectory(originalDir);
        Directory.CreateDirectory(personalTranslatedDir);
        Directory.CreateDirectory(canonicalTranslatedDir);

        try
        {
            var dirs = SearchIndexService.EnumerateTranslatedCounterpartDirs(
                originalDir,
                personalTranslatedDir,
                SearchSide.Original).ToList();

            Assert.Equal(2, dirs.Count);
            Assert.Equal(Path.GetFullPath(personalTranslatedDir), dirs[0]);
            Assert.Equal(Path.GetFullPath(canonicalTranslatedDir), dirs[1]);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BuildAlignedDisplayChildrenFromIndexedUnits_UsesUnitOrderAndCounterpartsForMixedHits()
    {
        string root = Path.Combine(Path.GetTempPath(), "rz-search-display-" + Guid.NewGuid().ToString("N"));
        string originalDir = Path.Combine(root, "xml-p5");
        string translatedDir = Path.Combine(root, "xml-p5t");
        Directory.CreateDirectory(Path.Combine(originalDir, "T", "T48"));
        Directory.CreateDirectory(Path.Combine(translatedDir, "T", "T48"));

        string relPath = "T/T48/T48n2005.xml";
        File.WriteAllText(Path.Combine(originalDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>\u7121\u9580\u95DC \u7B2C\u4E00\u5247</p><p>\u5E73\u5E38\u5FC3\u662F\u9053</p></body></text></TEI>");
        File.WriteAllText(Path.Combine(translatedDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Wumenguan Case One</p><p>The ordinary mind is the Way</p></body></text></TEI>");

        try
        {
            var children = SearchIndexService.BuildAlignedDisplayChildrenFromIndexedUnits(
                originalDir,
                translatedDir,
                relPath,
                "\u9580\u95DC",
                includeOriginal: true,
                includeTranslated: true,
                contextWidth: 40);

            var child = Assert.Single(children);
            Assert.Equal(SearchSide.Original, child.Side);
            Assert.Contains("\u7121\u9580\u95DC", child.PrimarySnippetText);
            Assert.True(child.HasSecondaryDisplayText);
            Assert.Contains("Wumenguan", child.SecondarySnippetText);
            Assert.True(child.SecondaryIsContextOnly);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BuildAlignedDisplayChildrenFromIndexedUnits_StitchesAdjacentCounterpartUnitsWithinSameElement()
    {
        string root = Path.Combine(Path.GetTempPath(), "rz-search-display-" + Guid.NewGuid().ToString("N"));
        string originalDir = Path.Combine(root, "xml-p5");
        string translatedDir = Path.Combine(root, "xml-p5t");
        Directory.CreateDirectory(Path.Combine(originalDir, "T", "T48"));
        Directory.CreateDirectory(Path.Combine(translatedDir, "T", "T48"));

        string relPath = "T/T48/T48n2005.xml";
        File.WriteAllText(Path.Combine(originalDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>無門<lb/>關第一則</p></body></text></TEI>");
        File.WriteAllText(Path.Combine(translatedDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Wumen<lb/>Barrier case one</p></body></text></TEI>");

        try
        {
            var child = Assert.Single(SearchIndexService.BuildAlignedDisplayChildrenFromIndexedUnits(
                originalDir,
                translatedDir,
                relPath,
                "門",
                includeOriginal: true,
                includeTranslated: true,
                contextWidth: 80));

            Assert.Equal(SearchSide.Original, child.Side);
            Assert.Contains("Wumen", child.SecondarySnippetText);
            Assert.Contains("Barrier case one", child.SecondarySnippetText);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BuildAlignedDisplayChildrenFromIndexedUnits_DoesNotCrossElementBoundaryWhenStitchingCounterpart()
    {
        string root = Path.Combine(Path.GetTempPath(), "rz-search-display-" + Guid.NewGuid().ToString("N"));
        string originalDir = Path.Combine(root, "xml-p5");
        string translatedDir = Path.Combine(root, "xml-p5t");
        Directory.CreateDirectory(Path.Combine(originalDir, "T", "T48"));
        Directory.CreateDirectory(Path.Combine(translatedDir, "T", "T48"));

        string relPath = "T/T48/T48n2005.xml";
        File.WriteAllText(Path.Combine(originalDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>無門<lb/>關第一則</p><p>平常心是道</p></body></text></TEI>");
        File.WriteAllText(Path.Combine(translatedDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Wumen<lb/>Barrier case one</p><p>The ordinary mind is the Way</p></body></text></TEI>");

        try
        {
            var child = Assert.Single(SearchIndexService.BuildAlignedDisplayChildrenFromIndexedUnits(
                originalDir,
                translatedDir,
                relPath,
                "門",
                includeOriginal: true,
                includeTranslated: true,
                contextWidth: 160));

            Assert.Contains("Wumen", child.SecondarySnippetText);
            Assert.Contains("Barrier case one", child.SecondarySnippetText);
            Assert.DoesNotContain("ordinary mind", child.SecondarySnippetText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void BuildAlignedDisplayChildrenFromIndexedUnits_FallsBackToCanonicalXmlP5tForDisplay()
    {
        string root = Path.Combine(Path.GetTempPath(), "rz-search-display-" + Guid.NewGuid().ToString("N"));
        string originalDir = Path.Combine(root, "xml-p5");
        string personalTranslatedDir = Path.Combine(root, "community", "translations", "dota2nub");
        string canonicalTranslatedDir = Path.Combine(root, "xml-p5t");
        Directory.CreateDirectory(Path.Combine(originalDir, "T", "T48"));
        Directory.CreateDirectory(Path.Combine(personalTranslatedDir, "T", "T48"));
        Directory.CreateDirectory(Path.Combine(canonicalTranslatedDir, "T", "T48"));

        string relPath = "T/T48/T48n2004.xml";
        File.WriteAllText(Path.Combine(originalDir, "T", "T48", "T48n2004.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>\u96F2\u9580\u95DC</p></body></text></TEI>");
        File.WriteAllText(Path.Combine(canonicalTranslatedDir, "T", "T48", "T48n2004.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Yunmen''s Barrier</p></body></text></TEI>");

        try
        {
            var children = SearchIndexService.BuildAlignedDisplayChildrenFromIndexedUnits(
                originalDir,
                personalTranslatedDir,
                relPath,
                "\u95DC",
                includeOriginal: true,
                includeTranslated: true,
                contextWidth: 40);

            var child = Assert.Single(children);
            Assert.Equal(SearchSide.Original, child.Side);
            Assert.Contains("\u96F2\u9580\u95DC", child.PrimarySnippetText);
            Assert.Contains("Yunmen", child.SecondarySnippetText);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ComputeCooccurrences_FiltersQueryCharsAndPunctuationNoise()
    {
        var groups = new List<SearchResultGroup>
        {
            new()
            {
                RelPath = "T/T48/T48n2005.xml",
                Children = new List<SearchResultChild>
                {
                    new() { Hit = Hit("!\u96F2\u9580", "\u9580\u95DC", "\u3002\u8D99\u5DDE"), Side = SearchSide.Original }
                }
            }
        };

        var result = SearchIndexService.ComputeCooccurrences(groups, "\u9580\u95DC", 80, CoocMetric.TopCooccurrences, topK: 10);

        Assert.DoesNotContain(result.Left, row => row.Key == "\u9580" || row.Key == "\u95DC" || row.Key == "!" || row.Key == "\u3002");
        Assert.Contains(result.Left, row => row.Key == "\u96F2");
        Assert.Contains("result-scoped", result.Summary);
    }

    [Fact]
    public void ComputeCooccurrences_FiltersCommonParticleNgrams()
    {
        var groups = new List<SearchResultGroup>
        {
            new()
            {
                RelPath = "T/T48/T48n2005.xml",
                Children = new List<SearchResultChild>
                {
                    new() { Hit = Hit("\u4E4B\u4E4E", "\u4F5B\u6CD5", "\u8005\u4E5F\u96F2\u9580\u95DC"), Side = SearchSide.Original }
                }
            }
        };

        var result = SearchIndexService.ComputeCooccurrences(groups, "\u4F5B\u6CD5", 80, CoocMetric.TopCooccurrences, topK: 20);

        Assert.DoesNotContain(result.Right, row => row.Key == "\u4E4B\u4E4E" || row.Key == "\u4E4E\u8005" || row.Key == "\u8005\u4E5F");
        Assert.Contains(result.Right, row => row.Key.Contains("\u96F2\u9580") || row.Key.Contains("\u9580\u95DC"));
    }

    [Fact]
    public void ComputeCooccurrences_FiltersPunctuationAsciiCharNoiseAndQuerySelf()
    {
        var groups = new List<SearchResultGroup>
        {
            new()
            {
                RelPath = "T/T48/T48n2005.xml",
                Children = new List<SearchResultChild>
                {
                    new()
                    {
                        RelPath = "T/T48/T48n2005.xml",
                        Side = SearchSide.Original,
                        Hit = Hit("\u96F2\u9580", "\u95DC", " is the gate.")
                    }
                }
            }
        };

        var result = SearchIndexService.ComputeCooccurrences(groups, "\u96F2\u9580\u95DC", 80, CoocMetric.Frequency, topK: 20);

        Assert.DoesNotContain(result.Left, row => row.Key == "\u96F2" || row.Key == "\u9580" || row.Key == "\u95DC" || row.Key == "i");
        Assert.DoesNotContain(result.Right, row => row.Key.Contains("\u96F2\u9580\u95DC", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Right, row => row.Key.Any(char.IsPunctuation));
    }

    [Fact]
    public void ComputeCooccurrences_SummaryAndTitlesStateResultScopedSemantics()
    {
        var groups = new List<SearchResultGroup>
        {
            new()
            {
                RelPath = "T/T48/T48n2005.xml",
                Children = new List<SearchResultChild>
                {
                    new()
                    {
                        RelPath = "T/T48/T48n2005.xml",
                        Side = SearchSide.Original,
                        Hit = Hit("\u96F2\u9580", "\u95DC", "\u8D99\u5DDE")
                    }
                }
            }
        };

        var result = SearchIndexService.ComputeCooccurrences(groups, "\u95DC", 80, CoocMetric.DispersionScore, topK: 10);

        Assert.Contains("result-scoped", result.Summary);
        Assert.Contains("current results", result.LeftTitle);
        Assert.Contains("current results", result.RightTitle);
        Assert.Contains("not corpus-wide", result.ExtraLine);
    }
    [Fact]
    public void ComputeCorpusCooccurrences_SummaryTitlesAndProgressStateCorpusScanSemantics()
    {
        string root = Path.Combine(Path.GetTempPath(), "rz-search-corpus-" + Guid.NewGuid().ToString("N"));
        string originalDir = Path.Combine(root, "xml-p5");
        string translatedDir = Path.Combine(root, "xml-p5t");
        Directory.CreateDirectory(Path.Combine(originalDir, "T", "T48"));
        Directory.CreateDirectory(Path.Combine(translatedDir, "T", "T48"));

        var files = new List<FileNavItem>
        {
            new()
            {
                RelPath = "T/T48/T48n2005.xml",
                FileName = "T48n2005",
                DisplayShort = "Wumenguan",
                Tooltip = "T/T48/T48n2005.xml"
            }
        };

        File.WriteAllText(Path.Combine(originalDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>???</p><p>??</p></body></text></TEI>");
        File.WriteAllText(Path.Combine(translatedDir, "T", "T48", "T48n2005.xml"),
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Wumenguan</p><p>The barrier gate</p></body></text></TEI>");

        var progress = new List<(int done, int total)>();

        try
        {
            var result = SearchIndexService.ComputeCorpusCooccurrences(
                originalDir,
                translatedDir,
                files,
                "?",
                includeOriginal: true,
                includeTranslated: true,
                contextWidth: 40,
                metric: CoocMetric.TopCooccurrences,
                progress: new Progress<(int done, int total)>(p => progress.Add(p)));

            Assert.Contains("corpus-scan", result.Summary);
            Assert.Contains("across filtered corpus", result.LeftTitle);
            Assert.Contains("across filtered corpus", result.RightTitle);
            Assert.Contains("Filtered files scanned: 1", result.ExtraLine);
            Assert.Contains("Corpus scan is slower", result.ExtraLine);
            Assert.Contains(progress, p => p.done == 1 && p.total == 1);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SearchResultGroup_ApplyEnrichment_PreservesVerifiedPrimaryHitWhenOnlyCounterpartIsEnriched()
    {
        var existingChild = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("wide-left ", "門關", " wide-right")
        };
        var group = new SearchResultGroup
        {
            RelPath = "T/T01/T01n0001.xml",
            Children = new List<SearchResultChild> { existingChild }
        };

        var enrichedChild = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("narrow-left ", "門關", " narrow-right"),
            SecondaryHit = new SearchHit { Left = "Wumen Barrier case one", Match = string.Empty, Right = string.Empty },
            SecondaryIsContextOnly = true
        };

        group.ApplyEnrichment(new List<SearchResultChild> { enrichedChild });

        Assert.Equal("wide-left 門關 wide-right", existingChild.PrimarySnippetText);
        Assert.Equal("Wumen Barrier case one", existingChild.SecondarySnippetText);
        Assert.True(existingChild.SecondaryIsContextOnly);
    }
}





