using System;
using System.Collections.Generic;
using System.IO;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class SearchIndexServiceTests
{
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
    public void BuildCounterpartHitsFromIndexedUnits_ForChineseQuery_ReturnsEnglishCounterparts()
    {
        var doc = new CbetaTranslator.App.Services.IndexedTranslationDocument();
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u8D99\u5DDE\u554F\u4F5B\u6CD5", En = "Zhaozhou asked about the Dharma." });
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u7121\u9580\u66F0", En = "Wumen said." });

        var hits = SearchIndexService.BuildCounterpartHitsFromIndexedUnits(
            doc,
            "\u4F5B\u6CD5",
            SearchSide.Original,
            neededCount: 1,
            contextWidth: 40);

        Assert.Single(hits);
        Assert.Equal("Zhaozhou asked about the Dharma.", hits[0].Match);
    }

    [Fact]
    public void BuildCounterpartHitsFromIndexedUnits_ForEnglishQuery_ReturnsChineseCounterparts()
    {
        var doc = new CbetaTranslator.App.Services.IndexedTranslationDocument();
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u8D99\u5DDE\u554F\u4F5B\u6CD5", En = "Zhaozhou asked about the Dharma." });
        doc.Units.Add(new CbetaTranslator.App.Services.TranslationUnit { Zh = "\u7121\u9580\u66F0", En = "Wumen said." });

        var hits = SearchIndexService.BuildCounterpartHitsFromIndexedUnits(
            doc,
            "Dharma",
            SearchSide.Translated,
            neededCount: 1,
            contextWidth: 40);

        Assert.Single(hits);
        Assert.Equal("\u8D99\u5DDE\u554F\u4F5B\u6CD5", hits[0].Match);
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
}


