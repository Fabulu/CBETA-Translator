using System.Collections.Generic;
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
            Hit("zh-l1 ", "字一", " zh-r1"),
            Hit("zh-l2 ", "字二", " zh-r2")
        };

        var translatedHits = new List<SearchHit>
        {
            Hit("en-l1 ", "word1", " en-r1")
        };

        var children = SearchIndexService.BuildResultChildren("T/T01/T01n0001.xml", originalHits, translatedHits);

        Assert.Equal(3, children.Count);

        Assert.Equal(SearchSide.Original, children[0].Side);
        Assert.Equal("zh-l1 字一 zh-r1", children[0].PrimarySnippetText);
        Assert.Equal("en-l1 word1 en-r1", children[0].SecondarySnippetText);
        Assert.True(children[0].HasSecondaryDisplayText);

        Assert.Equal(SearchSide.Original, children[1].Side);
        Assert.Equal("zh-l2 字二 zh-r2", children[1].PrimarySnippetText);
        Assert.False(children[1].HasSecondaryDisplayText);

        Assert.Equal(SearchSide.Translated, children[2].Side);
        Assert.Equal("en-l1 word1 en-r1", children[2].PrimarySnippetText);
        Assert.Equal("zh-l1 字一 zh-r1", children[2].SecondarySnippetText);
        Assert.True(children[2].HasSecondaryDisplayText);
    }

    [Fact]
    public void ToScholarPassage_CarriesBothLanguagesWhenPaired()
    {
        var child = new SearchResultChild
        {
            RelPath = "T/T01/T01n0001.xml",
            Side = SearchSide.Original,
            Hit = Hit("左文 ", "中", " 右文"),
            SecondaryHit = Hit("left ", "match", " right")
        };

        var passage = child.ToScholarPassage();

        Assert.Equal("T/T01/T01n0001.xml", passage.SourceRelPath);
        Assert.Equal("左文 中 右文", passage.ZhText);
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
}
