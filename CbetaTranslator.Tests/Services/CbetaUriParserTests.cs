using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

/// <summary>
/// Tests for <see cref="CbetaUriParser"/>: TryParse (clean + legacy formats),
/// BuildUri (clean format), FileIdToRelPath, RelPathToFileId, round-trips,
/// and malformed-input handling.
/// </summary>
public class CbetaUriParserTests
{
    // ==== FileIdToRelPath / RelPathToFileId ====

    [Theory]
    [InlineData("T48n2005", "T/T48/T48n2005.xml")]
    [InlineData("T01n0001", "T/T01/T01n0001.xml")]
    [InlineData("T12n0366", "T/T12/T12n0366.xml")]
    [InlineData("X73n1452", "X/X73/X73n1452.xml")]
    public void FileIdToRelPath_ValidIds_ReturnsExpectedPath(string fileId, string expected)
    {
        Assert.Equal(expected, CbetaUriParser.FileIdToRelPath(fileId));
    }

    [Theory]
    [InlineData("nope")]    // n at position 0
    [InlineData("")]
    [InlineData("abc")]     // no 'n' at all
    public void FileIdToRelPath_InvalidId_ReturnsNull(string fileId)
    {
        Assert.Null(CbetaUriParser.FileIdToRelPath(fileId));
    }

    [Theory]
    [InlineData("T/T48/T48n2005.xml", "T48n2005")]
    [InlineData("X/X73/X73n1452.xml", "X73n1452")]
    [InlineData("T\\T01\\T01n0001.xml", "T01n0001")]
    public void RelPathToFileId_ValidPaths_ReturnsExpectedId(string relPath, string expected)
    {
        Assert.Equal(expected, CbetaUriParser.RelPathToFileId(relPath));
    }

    // ==== BuildUri — clean format ====

    [Fact]
    public void BuildUri_FileOnly_ProducesCleanUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml");

        Assert.Equal("zen://T48n2005", uri);
    }

    [Fact]
    public void BuildUri_WithFromLb_ProducesCleanUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", fromLb: "0292a26");

        Assert.Equal("zen://T48n2005/0292a26", uri);
    }

    [Fact]
    public void BuildUri_WithLbRange_ProducesCleanUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", fromLb: "0292a26", toLb: "0292a29");

        Assert.Equal("zen://T48n2005/0292a26-0292a29", uri);
    }

    [Fact]
    public void BuildUri_WithSide_AppendsEn()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", side: SearchSide.Translated);

        Assert.Equal("zen://T48n2005/en", uri);
    }

    [Fact]
    public void BuildUri_WithRangeAndSide_ProducesCleanUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292a26", toLb: "0292a29", side: SearchSide.Translated);

        Assert.Equal("zen://T48n2005/0292a26-0292a29/en", uri);
    }

    [Fact]
    public void BuildUri_SameLbFromAndTo_NoRange()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292a26", toLb: "0292a26");

        Assert.Equal("zen://T48n2005/0292a26", uri);
    }

    [Fact]
    public void BuildUri_WithHighlight_ProducesQueryParam()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", highlightText: "hello world");

        Assert.Equal("zen://T48n2005?highlight=hello%20world", uri);
    }

    [Fact]
    public void BuildUri_CjkHighlight_EncodesCorrectly()
    {
        var cjkText = "\u4f5b\u8aaa\u963f\u5f4c\u9640\u7d93"; // 佛說阿彌陀經
        var uri = CbetaUriParser.BuildUri("T/T12/T12n0366.xml", highlightText: cjkText);

        // URI should not contain raw CJK characters
        Assert.DoesNotContain(cjkText, uri);
        Assert.StartsWith("zen://T12n0366?highlight=", uri);
    }

    [Fact]
    public void BuildUri_AllParams_ProducesCorrectUri()
    {
        var uri = CbetaUriParser.BuildUri(
            "T/T48/T48n2005.xml",
            fromLb: "0001a01",
            toLb: "0001a03",
            highlightText: "test",
            side: SearchSide.Translated,
            leftContext: "before",
            rightContext: "after",
            blockNumber: 7);

        Assert.StartsWith("zen://T48n2005/0001a01-0001a03/en?", uri);
        Assert.Contains("highlight=test", uri);
        Assert.Contains("lctx=before", uri);
        Assert.Contains("rctx=after", uri);
        Assert.Contains("block=7", uri);
        // No "side=" or "from=" query params in clean format
        Assert.DoesNotContain("side=", uri);
        Assert.DoesNotContain("from=", uri);
    }

    [Fact]
    public void BuildUri_DefaultSide_NotIncludedInUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", side: SearchSide.Original);

        Assert.DoesNotContain("/en", uri);
        Assert.DoesNotContain("side=", uri);
    }

    [Fact]
    public void BuildUri_BackslashPath_NormalizedCorrectly()
    {
        var uri = CbetaUriParser.BuildUri(@"T\T48\T48n2005.xml");

        Assert.Equal("zen://T48n2005", uri);
        Assert.DoesNotContain("\\", uri);
    }

    // ==== TryParse — clean format ====

    [Fact]
    public void TryParse_CleanFormat_FileOnly()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal(SearchSide.Original, result.Side);
        Assert.Null(result.FromLb);
        Assert.Null(result.ToLb);
        Assert.Null(result.MatchText);
    }

    [Fact]
    public void TryParse_CleanFormat_SingleLb()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/0292a26");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Null(result.ToLb);
    }

    [Fact]
    public void TryParse_CleanFormat_LbRange()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/0292a26-0292a29");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Equal("0292a29", result.ToLb);
    }

    [Fact]
    public void TryParse_CleanFormat_WithSide()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/en");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_CleanFormat_RangeAndSide()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/0292a26-0292a29/en");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Equal("0292a29", result.ToLb);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_CleanFormat_TranAlias()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/0292a26/tran");

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("0292a26", result.FromLb);
    }

    [Fact]
    public void TryParse_CleanFormat_AllQueryParams()
    {
        var uri = "zen://T48n2005/0292a26-0292a29/en?highlight=test&lctx=before&rctx=after&block=42";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Equal("0292a29", result.ToLb);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("test", result.MatchText);
        Assert.Equal("before", result.LeftContext);
        Assert.Equal("after", result.RightContext);
        Assert.Equal(42, result.AnchorStartHint);
    }

    [Fact]
    public void TryParse_CleanFormat_CjkHighlight()
    {
        var cjkText = "\u4f5b\u8aaa"; // 佛說
        var encoded = Uri.EscapeDataString(cjkText);
        var uri = $"zen://T01n0001?highlight={encoded}";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("T/T01/T01n0001.xml", result.RelPath);
        Assert.Equal(cjkText, result.MatchText);
    }

    // ==== TryParse — legacy format (backward compatibility) ====

    [Fact]
    public void TryParse_LegacyFormat_AllParams()
    {
        var uri = "zen://T/T48/T48n2005.xml?from=0292a26&to=0292a29&side=Translated&highlight=hello&lctx=left&rctx=right&block=42";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Equal("0292a29", result.ToLb);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("hello", result.MatchText);
        Assert.Equal("left", result.LeftContext);
        Assert.Equal("right", result.RightContext);
        Assert.Equal(42, result.AnchorStartHint);
    }

    [Fact]
    public void TryParse_LegacyFormat_FileOnly()
    {
        var uri = "zen://T/T48/T48n2005.xml";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Null(result.FromLb);
        Assert.Equal(SearchSide.Original, result.Side);
    }

    [Fact]
    public void TryParse_LegacyFormat_CaseInsensitiveSide()
    {
        var uri = "zen://T/T48/T48n2005.xml?side=translated";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_LegacyFormat_InvalidSide_DefaultsToOriginal()
    {
        var uri = "zen://T/T48/T48n2005.xml?side=invalid";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Original, result.Side);
    }

    [Fact]
    public void TryParse_LegacyFormat_InvalidBlock_IgnoredGracefully()
    {
        var uri = "zen://T/T48/T48n2005.xml?block=notanumber";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Null(result.AnchorStartHint);
    }

    [Fact]
    public void TryParse_LegacyFormat_CjkHighlight()
    {
        var cjkText = "\u4f5b\u8aaa"; // 佛說
        var encoded = Uri.EscapeDataString(cjkText);
        var uri = $"zen://T/T01/T01n0001.xml?highlight={encoded}";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(cjkText, result.MatchText);
    }

    // ==== TryParse — malformed input ====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-uri")]
    [InlineData("://missing-scheme")]
    public void TryParse_MalformedUri_ReturnsNull(string? uri)
    {
        var result = CbetaUriParser.TryParse(uri!);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("http://T/T48/T48n2005.xml")]
    [InlineData("https://T/T48/T48n2005.xml")]
    [InlineData("ftp://T/T48/T48n2005.xml")]
    [InlineData("file://T/T48/T48n2005.xml")]
    public void TryParse_NonCbetaScheme_ReturnsNull(string uri)
    {
        var result = CbetaUriParser.TryParse(uri);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_CaseInsensitiveScheme_Works()
    {
        var result = CbetaUriParser.TryParse("ZEN://T48n2005");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
    }

    // ==== Round-trips ====

    [Fact]
    public void RoundTrip_BuildUri_ThenTryParse_PreservesAllFields()
    {
        var relPath = "T/T48/T48n2005.xml";
        var highlight = "some search text";
        var side = SearchSide.Translated;
        var lctx = "left context";
        var rctx = "right context";
        var block = 99;

        var uri = CbetaUriParser.BuildUri(relPath,
            fromLb: "0001a01", toLb: "0001a03",
            highlightText: highlight, side: side,
            leftContext: lctx, rightContext: rctx, blockNumber: block);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal("0001a01", result.FromLb);
        Assert.Equal("0001a03", result.ToLb);
        Assert.Equal(highlight, result.MatchText);
        Assert.Equal(side, result.Side);
        Assert.Equal(lctx, result.LeftContext);
        Assert.Equal(rctx, result.RightContext);
        Assert.Equal(block, result.AnchorStartHint);
    }

    [Fact]
    public void RoundTrip_FileOnly_PreservesPath()
    {
        var relPath = "T/T48/T48n2005.xml";

        var uri = CbetaUriParser.BuildUri(relPath);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
    }

    [Fact]
    public void RoundTrip_CjkHighlight_PreservesText()
    {
        var cjkText = "\u5982\u662f\u6211\u805e"; // 如是我聞
        var relPath = "T/T01/T01n0001.xml";

        var uri = CbetaUriParser.BuildUri(relPath, highlightText: cjkText);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal(cjkText, result.MatchText);
    }

    [Fact]
    public void RoundTrip_SpecialCharactersInContexts_Preserved()
    {
        var lctx = "text with spaces & symbols=yes";
        var rctx = "more?special/chars";

        var uri = CbetaUriParser.BuildUri("T/T01/T01n0001.xml",
            highlightText: "match", leftContext: lctx, rightContext: rctx);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(lctx, result.LeftContext);
        Assert.Equal(rctx, result.RightContext);
    }

    [Fact]
    public void RoundTrip_DifferentCanons_AllWork()
    {
        // Test with X canon
        var relPath = "X/X73/X73n1452.xml";
        var uri = CbetaUriParser.BuildUri(relPath, fromLb: "0001a01");
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal("0001a01", result.FromLb);
    }

    // ==== BuildShareableUrl (unchanged behavior) ====

    [Fact]
    public void BuildShareableUrl_FileOnly_ReturnsCleanUrl()
    {
        var url = CbetaUriParser.BuildShareableUrl("T/T48/T48n2005.xml");
        Assert.Equal("https://readzen.pages.dev/T48n2005", url);
    }

    [Fact]
    public void BuildShareableUrl_WithLbRange_AppendsRange()
    {
        var url = CbetaUriParser.BuildShareableUrl("T/T48/T48n2005.xml", "0001a01", "0001a03");
        Assert.Equal("https://readzen.pages.dev/T48n2005/0001a01-0001a03", url);
    }

    [Fact]
    public void BuildShareableUrl_SingleLb_NoRange()
    {
        var url = CbetaUriParser.BuildShareableUrl("T/T48/T48n2005.xml", "0001a01");
        Assert.Equal("https://readzen.pages.dev/T48n2005/0001a01", url);
    }

    [Fact]
    public void BuildShareableUrl_WithSide_AppendsEnPath()
    {
        var url = CbetaUriParser.BuildShareableUrl("T/T48/T48n2005.xml", side: SearchSide.Translated);
        Assert.EndsWith("/en", url);
        Assert.DoesNotContain("?side=", url);
    }

    // ==== TryParseDeepLink ====

    [Fact]
    public void TryParseDeepLink_Null_ReturnsNull()
    {
        Assert.Null(CbetaUriParser.TryParseDeepLink(null));
    }

    [Fact]
    public void TryParseDeepLink_Empty_ReturnsNull()
    {
        Assert.Null(CbetaUriParser.TryParseDeepLink(""));
    }

    [Fact]
    public void TryParseDeepLink_PassageLink_ReturnsPassageKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://T48n2005/0292b28");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Passage, result.Kind);
        Assert.NotNull(result.Passage);
        Assert.Equal("T/T48/T48n2005.xml", result.Passage.RelPath);
        Assert.Equal("0292b28", result.Passage.FromLb);
    }

    [Fact]
    public void TryParseDeepLink_HttpsPassageLink_ReturnsPassageKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("https://readzen.pages.dev/#/T48n2005/0292b28");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Passage, result.Kind);
        Assert.NotNull(result.Passage);
        Assert.Equal("T/T48/T48n2005.xml", result.Passage.RelPath);
        Assert.Equal("0292b28", result.Passage.FromLb);
    }

    [Fact]
    public void TryParseDeepLink_DictLink_ReturnsDictionaryKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://dict/\u4f5b");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    [Fact]
    public void TryParseDeepLink_DictLink_DecodesUri()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://dict/%E4%BD%9B");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    [Fact]
    public void TryParseDeepLink_ScholarLink_ReturnsScholarKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://scholar/col1/pass1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("col1", result.ScholarCollectionId);
        Assert.Equal("pass1", result.ScholarPassageId);
    }

    [Fact]
    public void TryParseDeepLink_ScholarLink_CollectionOnly()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://scholar/col1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("col1", result.ScholarCollectionId);
        Assert.Null(result.ScholarPassageId);
    }

    [Fact]
    public void TryParseDeepLink_SearchLink_ReturnsSearchKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://search?q=\u7121\u9580\u95dc&corpus=T");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("\u7121\u9580\u95dc", result.SearchQuery);
        Assert.Equal("T", result.SearchCorpus);
    }

    [Fact]
    public void TryParseDeepLink_SearchLink_QueryOnly()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://search?q=test");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Null(result.SearchCorpus);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_ReturnsTagsKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://tags/T48n2005?user=alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_NoUser()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://tags/T48n2005");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Null(result.TagsUser);
    }

    [Fact]
    public void TryParseDeepLink_TermLink_ReturnsDictionaryKind()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://term/\u822c\u82e5");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u822c\u82e5", result.DictTerm);
    }

    [Fact]
    public void TryParseDeepLink_HttpsDict_Works()
    {
        var result = CbetaUriParser.TryParseDeepLink("https://readzen.pages.dev/#/dict/\u4f5b");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    // ==== Deep-link builder round-trips ====

    [Fact]
    public void BuildDictUri_RoundTrip()
    {
        var uri = CbetaUriParser.BuildDictUri("\u4f5b");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    [Fact]
    public void BuildScholarUri_RoundTrip()
    {
        var uri = CbetaUriParser.BuildScholarUri("c1", "p1");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("c1", result.ScholarCollectionId);
        Assert.Equal("p1", result.ScholarPassageId);
    }

    [Fact]
    public void BuildSearchUri_RoundTrip()
    {
        var uri = CbetaUriParser.BuildSearchUri("test", "T");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Equal("T", result.SearchCorpus);
    }

    [Fact]
    public void BuildTagsUri_RoundTrip()
    {
        var uri = CbetaUriParser.BuildTagsUri("T48n2005", "alice");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
    }

    [Fact]
    public void BuildTermUri_RoundTrip()
    {
        var uri = CbetaUriParser.BuildTermUri("\u822c\u82e5");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u822c\u82e5", result.DictTerm);
    }

    // ==== Per-user deep links — Passage ====

    [Fact]
    public void TryParse_PassageWithUser_ExtractsUser()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/0292b28/en/bob");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292b28", result.FromLb);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("bob", result.User);
    }

    [Fact]
    public void TryParse_PassageWithoutUser_UserIsNull()
    {
        var result = CbetaUriParser.TryParse("zen://T48n2005/0292b28/en");

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Null(result.User);
    }

    [Fact]
    public void TryParse_PassageWithUserNoSide_ExtractsUser()
    {
        // zen://T48n2005/en/bob (no lb, side + user)
        var result = CbetaUriParser.TryParse("zen://T48n2005/en/bob");

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("bob", result.User);
        Assert.Null(result.FromLb);
    }

    [Fact]
    public void BuildUri_WithUser_AppendsUserSegment()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292b28", side: SearchSide.Translated, user: "bob");

        Assert.Contains("/bob", uri);
        Assert.EndsWith("/bob", uri.Split('?')[0]);
    }

    [Fact]
    public void BuildUri_WithoutUser_NoUserSegment()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292b28", side: SearchSide.Translated);

        Assert.Equal("zen://T48n2005/0292b28/en", uri);
    }

    [Fact]
    public void RoundTrip_PassageWithUser()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292b28", side: SearchSide.Translated, user: "bob");
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("bob", result.User);
        Assert.Equal("0292b28", result.FromLb);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    // ==== Per-user deep links — Scholar ====

    [Fact]
    public void TryParseDeepLink_ScholarWithUser()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://scholar/col/pass/bob");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("col", result.ScholarCollectionId);
        Assert.Equal("pass", result.ScholarPassageId);
        Assert.Equal("bob", result.ScholarUser);
    }

    [Fact]
    public void TryParseDeepLink_ScholarWithoutUser_UserIsNull()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://scholar/col/pass");

        Assert.NotNull(result);
        Assert.Null(result.ScholarUser);
    }

    [Fact]
    public void BuildScholarUri_WithUser_RoundTrip()
    {
        var uri = CbetaUriParser.BuildScholarUri("c1", "p1", user: "bob");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("c1", result.ScholarCollectionId);
        Assert.Equal("p1", result.ScholarPassageId);
        Assert.Equal("bob", result.ScholarUser);
    }

    // ==== Per-user deep links — Tags ====

    [Fact]
    public void TryParseDeepLink_TagsPathUser()
    {
        var result = CbetaUriParser.TryParseDeepLink("zen://tags/T48n2005/alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
    }

    [Fact]
    public void TryParseDeepLink_TagsQueryUserFallback()
    {
        // Legacy ?user= query param still works
        var result = CbetaUriParser.TryParseDeepLink("zen://tags/T48n2005?user=alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("alice", result.TagsUser);
    }

    [Fact]
    public void BuildTagsUri_WithUser_RoundTrip()
    {
        var uri = CbetaUriParser.BuildTagsUri("T48n2005", "alice");
        var result = CbetaUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
    }

    // ==== Shareable URL with user ====

    [Fact]
    public void BuildShareableUrl_WithUser_AppendsUserSegment()
    {
        var url = CbetaUriParser.BuildShareableUrl("T/T48/T48n2005.xml",
            fromLb: "0001a01", side: SearchSide.Translated, user: "bob");

        Assert.Contains("/bob", url);
        Assert.DoesNotContain("?user=", url);
    }

    [Fact]
    public void BuildShareableUrl_WithoutUser_NoUserSegment()
    {
        var url = CbetaUriParser.BuildShareableUrl("T/T48/T48n2005.xml",
            side: SearchSide.Translated);

        Assert.EndsWith("/en", url);
    }
}
