using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for <see cref="ZenUriParser"/>: TryParse (clean + legacy formats),
/// BuildUri (clean format), FileIdToRelPath, RelPathToFileId, round-trips,
/// and malformed-input handling.
/// </summary>
public class ZenUriParserTests
{
    // ==== FileIdToRelPath / RelPathToFileId ====

    [Theory]
    [InlineData("T48n2005", "T/T48/T48n2005.xml")]
    [InlineData("T01n0001", "T/T01/T01n0001.xml")]
    [InlineData("T12n0366", "T/T12/T12n0366.xml")]
    [InlineData("X73n1452", "X/X73/X73n1452.xml")]
    public void FileIdToRelPath_ValidIds_ReturnsExpectedPath(string fileId, string expected)
    {
        Assert.Equal(expected, ZenUriParser.FileIdToRelPath(fileId));
    }

    [Theory]
    [InlineData("nope")]    // n at position 0
    [InlineData("")]
    [InlineData("abc")]     // no 'n' at all
    public void FileIdToRelPath_InvalidId_ReturnsNull(string fileId)
    {
        Assert.Null(ZenUriParser.FileIdToRelPath(fileId));
    }

    [Theory]
    [InlineData("T/T48/T48n2005.xml", "T48n2005")]
    [InlineData("X/X73/X73n1452.xml", "X73n1452")]
    [InlineData("T\\T01\\T01n0001.xml", "T01n0001")]
    public void RelPathToFileId_ValidPaths_ReturnsExpectedId(string relPath, string expected)
    {
        Assert.Equal(expected, ZenUriParser.RelPathToFileId(relPath));
    }

    // ==== OpenZenTexts file ID format (publisher.slug) ====

    [Theory]
    [InlineData("ws.gateless-barrier", "ws/gateless-barrier/gateless-barrier.xml")]
    [InlineData("pd.linji-record", "pd/linji-record/linji-record.xml")]
    [InlineData("ce.blue-cliff-record", "ce/blue-cliff-record/blue-cliff-record.xml")]
    [InlineData("mit.platform-sutra", "mit/platform-sutra/platform-sutra.xml")]
    public void FileIdToRelPath_OpenZenIds_ReturnsExpectedPath(string fileId, string expected)
    {
        Assert.Equal(expected, ZenUriParser.FileIdToRelPath(fileId));
    }

    [Theory]
    [InlineData("ws/gateless-barrier/gateless-barrier.xml", "ws.gateless-barrier")]
    [InlineData("pd/linji-record/linji-record.xml", "pd.linji-record")]
    [InlineData("ce\\blue-cliff-record\\blue-cliff-record.xml", "ce.blue-cliff-record")]
    public void RelPathToFileId_OpenZenPaths_ReturnsExpectedId(string relPath, string expected)
    {
        Assert.Equal(expected, ZenUriParser.RelPathToFileId(relPath));
    }

    [Theory]
    [InlineData("ws.gateless-barrier")]
    [InlineData("pd.linji-record")]
    [InlineData("ce.blue-cliff-record")]
    [InlineData("mit.platform-sutra")]
    public void IsOpenZenFileId_ValidIds_ReturnsTrue(string fileId)
    {
        Assert.True(ZenUriParser.IsOpenZenFileId(fileId));
    }

    [Theory]
    [InlineData("T48n2005")]      // CBETA format
    [InlineData("X73n1452")]      // CBETA format
    [InlineData("xx.something")]  // unknown publisher prefix
    [InlineData("ws.")]           // empty slug
    [InlineData(".gateless")]     // empty publisher
    [InlineData("")]
    public void IsOpenZenFileId_InvalidIds_ReturnsFalse(string fileId)
    {
        Assert.False(ZenUriParser.IsOpenZenFileId(fileId));
    }

    [Theory]
    [InlineData("ws.gateless-barrier")]
    [InlineData("T48n2005")]
    public void FileId_RoundTripsThroughBothDirections(string fileId)
    {
        var path = ZenUriParser.FileIdToRelPath(fileId);
        Assert.NotNull(path);
        var roundTrip = ZenUriParser.RelPathToFileId(path!);
        Assert.Equal(fileId, roundTrip);
    }

    // ==== BuildUri ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â clean format ====

    [Fact]
    public void BuildUri_FileOnly_ProducesCleanUri()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml");

        Assert.Equal("zen://T48n2005", uri);
    }

    [Fact]
    public void BuildUri_WithFromLb_ProducesCleanUri()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml", fromLb: "0292a26");

        Assert.Equal("zen://T48n2005/0292a26", uri);
    }

    [Fact]
    public void BuildUri_WithLbRange_ProducesCleanUri()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml", fromLb: "0292a26", toLb: "0292a29");

        Assert.Equal("zen://T48n2005/0292a26-0292a29", uri);
    }

    [Fact]
    public void BuildUri_WithSide_AppendsEn()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml", side: SearchSide.Translated);

        Assert.Equal("zen://T48n2005/en", uri);
    }

    [Fact]
    public void BuildUri_WithRangeAndSide_ProducesCleanUri()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292a26", toLb: "0292a29", side: SearchSide.Translated);

        Assert.Equal("zen://T48n2005/0292a26-0292a29/en", uri);
    }

    [Fact]
    public void BuildUri_SameLbFromAndTo_NoRange()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292a26", toLb: "0292a26");

        Assert.Equal("zen://T48n2005/0292a26", uri);
    }

    [Fact]
    public void BuildUri_WithHighlight_ProducesQueryParam()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml", highlightText: "hello world");

        Assert.Equal("zen://T48n2005?highlight=hello%20world", uri);
    }

    [Fact]
    public void BuildUri_CjkHighlight_EncodesCorrectly()
    {
        var cjkText = "\u4f5b\u8aaa\u963f\u5f4c\u9640\u7d93"; // ÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¨Ã‚ÂªÃ‚ÂªÃƒÂ©Ã‹Å“Ã‚Â¿ÃƒÂ¥Ã‚Â½Ã…â€™ÃƒÂ©Ã¢â€žÂ¢Ã¢â€šÂ¬ÃƒÂ§Ã‚Â¶Ã¢â‚¬Å“
        var uri = ZenUriParser.BuildUri("T/T12/T12n0366.xml", highlightText: cjkText);

        // URI should not contain raw CJK characters
        Assert.DoesNotContain(cjkText, uri);
        Assert.StartsWith("zen://T12n0366?highlight=", uri);
    }

    [Fact]
    public void BuildUri_AllParams_ProducesCorrectUri()
    {
        var uri = ZenUriParser.BuildUri(
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
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml", side: SearchSide.Original);

        Assert.DoesNotContain("/en", uri);
        Assert.DoesNotContain("side=", uri);
    }

    [Fact]
    public void BuildUri_BackslashPath_NormalizedCorrectly()
    {
        var uri = ZenUriParser.BuildUri(@"T\T48\T48n2005.xml");

        Assert.Equal("zen://T48n2005", uri);
        Assert.DoesNotContain("\\", uri);
    }

    // ==== TryParse ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â clean format ====

    [Fact]
    public void TryParse_CleanFormat_FileOnly()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005");

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
        var result = ZenUriParser.TryParse("zen://T48n2005/0292a26");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Null(result.ToLb);
    }

    [Fact]
    public void TryParse_CleanFormat_LbRange()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005/0292a26-0292a29");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Equal("0292a29", result.ToLb);
    }

    [Fact]
    public void TryParse_CleanFormat_WithSide()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005/en");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_CleanFormat_RangeAndSide()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005/0292a26-0292a29/en");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292a26", result.FromLb);
        Assert.Equal("0292a29", result.ToLb);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_CleanFormat_TranAlias()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005/0292a26/tran");

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("0292a26", result.FromLb);
    }

    [Fact]
    public void TryParse_CleanFormat_AllQueryParams()
    {
        var uri = "zen://T48n2005/0292a26-0292a29/en?highlight=test&lctx=before&rctx=after&block=42";

        var result = ZenUriParser.TryParse(uri);

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
        var cjkText = "\u4f5b\u8aaa"; // ÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¨Ã‚ÂªÃ‚Âª
        var encoded = Uri.EscapeDataString(cjkText);
        var uri = $"zen://T01n0001?highlight={encoded}";

        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("T/T01/T01n0001.xml", result.RelPath);
        Assert.Equal(cjkText, result.MatchText);
    }

    // ==== TryParse ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â legacy format (backward compatibility) ====

    [Fact]
    public void TryParse_LegacyFormat_AllParams()
    {
        var uri = "zen://T/T48/T48n2005.xml?from=0292a26&to=0292a29&side=Translated&highlight=hello&lctx=left&rctx=right&block=42";

        var result = ZenUriParser.TryParse(uri);

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

        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Null(result.FromLb);
        Assert.Equal(SearchSide.Original, result.Side);
    }

    [Fact]
    public void TryParse_LegacyFormat_CaseInsensitiveSide()
    {
        var uri = "zen://T/T48/T48n2005.xml?side=translated";

        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_LegacyFormat_InvalidSide_DefaultsToOriginal()
    {
        var uri = "zen://T/T48/T48n2005.xml?side=invalid";

        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Original, result.Side);
    }

    [Fact]
    public void TryParse_LegacyFormat_InvalidBlock_IgnoredGracefully()
    {
        var uri = "zen://T/T48/T48n2005.xml?block=notanumber";

        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Null(result.AnchorStartHint);
    }

    [Fact]
    public void TryParse_LegacyFormat_CjkHighlight()
    {
        var cjkText = "\u4f5b\u8aaa"; // ÃƒÂ¤Ã‚Â½Ã¢â‚¬ÂºÃƒÂ¨Ã‚ÂªÃ‚Âª
        var encoded = Uri.EscapeDataString(cjkText);
        var uri = $"zen://T/T01/T01n0001.xml?highlight={encoded}";

        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(cjkText, result.MatchText);
    }

    // ==== TryParse ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â malformed input ====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-uri")]
    [InlineData("://missing-scheme")]
    public void TryParse_MalformedUri_ReturnsNull(string? uri)
    {
        var result = ZenUriParser.TryParse(uri!);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("http://T/T48/T48n2005.xml")]
    [InlineData("https://T/T48/T48n2005.xml")]
    [InlineData("ftp://T/T48/T48n2005.xml")]
    [InlineData("file://T/T48/T48n2005.xml")]
    public void TryParse_NonCbetaScheme_ReturnsNull(string uri)
    {
        var result = ZenUriParser.TryParse(uri);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_CaseInsensitiveScheme_Works()
    {
        var result = ZenUriParser.TryParse("ZEN://T48n2005");

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

        var uri = ZenUriParser.BuildUri(relPath,
            fromLb: "0001a01", toLb: "0001a03",
            highlightText: highlight, side: side,
            leftContext: lctx, rightContext: rctx, blockNumber: block);
        var result = ZenUriParser.TryParse(uri);

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

        var uri = ZenUriParser.BuildUri(relPath);
        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
    }

    [Fact]
    public void RoundTrip_CjkHighlight_PreservesText()
    {
        var cjkText = "\u5982\u662f\u6211\u805e"; // ÃƒÂ¥Ã‚Â¦Ã¢â‚¬Å¡ÃƒÂ¦Ã‹Å“Ã‚Â¯ÃƒÂ¦Ã‹â€ Ã¢â‚¬ËœÃƒÂ¨Ã‚ÂÃ…Â¾
        var relPath = "T/T01/T01n0001.xml";

        var uri = ZenUriParser.BuildUri(relPath, highlightText: cjkText);
        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal(cjkText, result.MatchText);
    }

    [Fact]
    public void RoundTrip_SpecialCharactersInContexts_Preserved()
    {
        var lctx = "text with spaces & symbols=yes";
        var rctx = "more?special/chars";

        var uri = ZenUriParser.BuildUri("T/T01/T01n0001.xml",
            highlightText: "match", leftContext: lctx, rightContext: rctx);
        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(lctx, result.LeftContext);
        Assert.Equal(rctx, result.RightContext);
    }

    [Fact]
    public void RoundTrip_DifferentCanons_AllWork()
    {
        // Test with X canon
        var relPath = "X/X73/X73n1452.xml";
        var uri = ZenUriParser.BuildUri(relPath, fromLb: "0001a01");
        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal("0001a01", result.FromLb);
    }

    // ==== BuildShareableUrl (unchanged behavior) ====

    [Fact]
    public void BuildShareableUrl_FileOnly_ReturnsCleanUrl()
    {
        var url = ZenUriParser.BuildShareableUrl("T/T48/T48n2005.xml");
        Assert.Equal("https://readzen.pages.dev/T48n2005", url);
    }

    [Fact]
    public void BuildShareableUrl_WithLbRange_AppendsRange()
    {
        var url = ZenUriParser.BuildShareableUrl("T/T48/T48n2005.xml", "0001a01", "0001a03");
        Assert.Equal("https://readzen.pages.dev/T48n2005/0001a01-0001a03", url);
    }

    [Fact]
    public void BuildShareableUrl_SingleLb_NoRange()
    {
        var url = ZenUriParser.BuildShareableUrl("T/T48/T48n2005.xml", "0001a01");
        Assert.Equal("https://readzen.pages.dev/T48n2005/0001a01", url);
    }

    [Fact]
    public void BuildShareableUrl_WithSide_AppendsEnPath()
    {
        var url = ZenUriParser.BuildShareableUrl("T/T48/T48n2005.xml", side: SearchSide.Translated);
        Assert.EndsWith("/en", url);
        Assert.DoesNotContain("?side=", url);
    }

    // ==== TryParseDeepLink ====

    [Fact]
    public void TryParseDeepLink_Null_ReturnsNull()
    {
        Assert.Null(ZenUriParser.TryParseDeepLink(null));
    }

    [Fact]
    public void TryParseDeepLink_Empty_ReturnsNull()
    {
        Assert.Null(ZenUriParser.TryParseDeepLink(""));
    }

    [Fact]
    public void TryParseDeepLink_PassageLink_ReturnsPassageKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://T48n2005/0292b28");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Passage, result.Kind);
        Assert.NotNull(result.Passage);
        Assert.Equal("T/T48/T48n2005.xml", result.Passage.RelPath);
        Assert.Equal("0292b28", result.Passage.FromLb);
    }

    [Fact]
    public void TryParseDeepLink_HttpsPassageLink_ReturnsPassageKind()
    {
        var result = ZenUriParser.TryParseDeepLink("https://readzen.pages.dev/#/T48n2005/0292b28");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Passage, result.Kind);
        Assert.NotNull(result.Passage);
        Assert.Equal("T/T48/T48n2005.xml", result.Passage.RelPath);
        Assert.Equal("0292b28", result.Passage.FromLb);
    }

    [Fact]
    public void TryParseDeepLink_DictLink_ReturnsDictionaryKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://dict/\u4f5b");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    [Fact]
    public void TryParseDeepLink_DictLink_DecodesUri()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://dict/%E4%BD%9B");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    [Fact]
    public void TryParseDeepLink_ScholarLink_ReturnsScholarKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://scholar/col1/pass1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("col1", result.ScholarCollectionId);
        Assert.Equal("pass1", result.ScholarPassageId);
    }

    [Fact]
    public void TryParseDeepLink_ScholarLink_CollectionOnly()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://scholar/col1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("col1", result.ScholarCollectionId);
        Assert.Null(result.ScholarPassageId);
    }

    [Fact]
    public void TryParseDeepLink_SearchLink_ReturnsSearchKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://search?q=\u7121\u9580\u95dc&corpus=T");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("\u7121\u9580\u95dc", result.SearchQuery);
        Assert.Equal("T", result.SearchCorpus);
    }

    [Fact]
    public void TryParseDeepLink_SearchLink_QueryOnly()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://search?q=test");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Null(result.SearchCorpus);
    }


    [Fact]
    public void TryParseDeepLink_SearchLink_QueryOnly_HasNullExtendedState()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://search?q=test");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Null(result.SearchCorpus);
        Assert.Null(result.SearchOriginal);
        Assert.Null(result.SearchTranslated);
        Assert.Null(result.SearchZenOnly);
        Assert.Null(result.SearchStatusIndex);
        Assert.Null(result.SearchTagId);
        Assert.Null(result.SearchContextIndex);
        Assert.Null(result.SearchTranslationSource);
    }

    [Fact]
    public void TryParseDeepLink_SearchLink_RichState_ParsesCanonicalFields()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://search?q=test&corpus=T&src=alice&orig=1&tran=0&zen=true&status=2&tag=topic-1&ctx=5");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Equal("T", result.SearchCorpus);
        Assert.Equal("alice", result.SearchTranslationSource);
        Assert.True(result.SearchOriginal);
        Assert.False(result.SearchTranslated);
        Assert.True(result.SearchZenOnly);
        Assert.Equal(2, result.SearchStatusIndex);
        Assert.Equal("topic-1", result.SearchTagId);
        Assert.Equal(6, result.SearchContextIndex);
    }

    [Fact]
    public void BuildSearchUri_RoundTrip_RichState()
    {
        var uri = ZenUriParser.BuildSearchUri(
            "test",
            corpus: "T",
            searchOriginal: true,
            searchTranslated: false,
            zenOnly: true,
            statusIndex: 2,
            tagId: "topic-1",
            contextIndex: 5,
            translationSource: "alice");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Equal("T", result.SearchCorpus);
        Assert.True(result.SearchOriginal);
        Assert.False(result.SearchTranslated);
        Assert.True(result.SearchZenOnly);
        Assert.Equal(2, result.SearchStatusIndex);
        Assert.Equal("topic-1", result.SearchTagId);
        Assert.Equal(5, result.SearchContextIndex);
        Assert.Equal("alice", result.SearchTranslationSource);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_ReturnsTagsKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005?user=alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
        Assert.Null(result.TagsTagId);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_NoUser()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Null(result.TagsUser);
        Assert.Null(result.TagsTagId);
    }

    [Fact]
    public void TryParseDeepLink_TermLink_ReturnsTermbaseKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://term/ÃƒÂ¨Ã‹â€ Ã‚Â¬ÃƒÂ¨Ã¢â‚¬Â¹Ã‚Â¥");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Termbase, result.Kind);
        Assert.Equal("ÃƒÂ¨Ã‹â€ Ã‚Â¬ÃƒÂ¨Ã¢â‚¬Â¹Ã‚Â¥", result.TermbaseEntry);
        Assert.Null(result.TermbaseUser);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_WithUserAndTagId()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005/alice/topic-1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
        Assert.Equal("topic-1", result.TagsTagId);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_TagIdOnly_UsesEmptyUserSlot()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005//topic-1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Null(result.TagsUser);
        Assert.Equal("topic-1", result.TagsTagId);
    }

    [Fact]
    public void TryParseDeepLink_TagsLink_QueryTagIdFallback()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005?user=alice&tagId=topic-1");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("alice", result.TagsUser);
        Assert.Equal("topic-1", result.TagsTagId);
    }

    [Fact]
    public void TryParseDeepLink_TermLink_WithUser_ReturnsTermbaseKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://term/ÃƒÂ¨Ã‹â€ Ã‚Â¬ÃƒÂ¨Ã¢â‚¬Â¹Ã‚Â¥/alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Termbase, result.Kind);
        Assert.Equal("ÃƒÂ¨Ã‹â€ Ã‚Â¬ÃƒÂ¨Ã¢â‚¬Â¹Ã‚Â¥", result.TermbaseEntry);
        Assert.Equal("alice", result.TermbaseUser);
    }


    [Fact]
    public void TryParseDeepLink_MasterLink_ReturnsMasterKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://master/Linji%20Yixuan");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Master, result.Kind);
        Assert.Equal("Linji Yixuan", result.MasterName);
        Assert.Null(result.MasterUser);
    }

    [Fact]
    public void TryParseDeepLink_MasterLink_WithUser_ReturnsMasterKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://master/Linji%20Yixuan/alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Master, result.Kind);
        Assert.Equal("Linji Yixuan", result.MasterName);
        Assert.Equal("alice", result.MasterUser);
    }
    [Fact]
    public void TryParseDeepLink_HttpsDict_Works()
    {
        var result = ZenUriParser.TryParseDeepLink("https://readzen.pages.dev/#/dict/\u4f5b");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    // ==== Deep-link builder round-trips ====


    [Fact]
    public void BuildMasterUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildMasterUri("Linji Yixuan", "alice");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Master, result.Kind);
        Assert.Equal("Linji Yixuan", result.MasterName);
        Assert.Equal("alice", result.MasterUser);
    }
    [Fact]
    public void BuildDictUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildDictUri("\u4f5b");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Dictionary, result.Kind);
        Assert.Equal("\u4f5b", result.DictTerm);
    }

    [Fact]
    public void BuildScholarUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildScholarUri("c1", "p1");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("c1", result.ScholarCollectionId);
        Assert.Equal("p1", result.ScholarPassageId);
    }

    [Fact]
    public void BuildSearchUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildSearchUri("test", "T");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Search, result.Kind);
        Assert.Equal("test", result.SearchQuery);
        Assert.Equal("T", result.SearchCorpus);
    }

    [Fact]
    public void BuildTagsUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildTagsUri("T48n2005", "alice", "topic-1");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
        Assert.Equal("topic-1", result.TagsTagId);
    }

    [Fact]
    public void BuildTermUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildTermUri("ÃƒÂ¨Ã‹â€ Ã‚Â¬ÃƒÂ¨Ã¢â‚¬Â¹Ã‚Â¥", "alice");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Termbase, result.Kind);
        Assert.Equal("ÃƒÂ¨Ã‹â€ Ã‚Â¬ÃƒÂ¨Ã¢â‚¬Â¹Ã‚Â¥", result.TermbaseEntry);
        Assert.Equal("alice", result.TermbaseUser);
    }

    // ==== Per-user deep links ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Passage ====

    [Fact]
    public void TryParse_PassageWithUser_ExtractsUser()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005/0292b28/en/bob");

        Assert.NotNull(result);
        Assert.Equal("T/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("0292b28", result.FromLb);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("bob", result.User);
    }

    [Fact]
    public void TryParse_PassageWithoutUser_UserIsNull()
    {
        var result = ZenUriParser.TryParse("zen://T48n2005/0292b28/en");

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Null(result.User);
    }

    [Fact]
    public void TryParse_PassageWithUserNoSide_ExtractsUser()
    {
        // zen://T48n2005/en/bob (no lb, side + user)
        var result = ZenUriParser.TryParse("zen://T48n2005/en/bob");

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("bob", result.User);
        Assert.Null(result.FromLb);
    }

    [Fact]
    public void BuildUri_WithUser_AppendsUserSegment()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292b28", side: SearchSide.Translated, user: "bob");

        Assert.Contains("/bob", uri);
        Assert.EndsWith("/bob", uri.Split('?')[0]);
    }

    [Fact]
    public void BuildUri_WithoutUser_NoUserSegment()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292b28", side: SearchSide.Translated);

        Assert.Equal("zen://T48n2005/0292b28/en", uri);
    }

    [Fact]
    public void RoundTrip_PassageWithUser()
    {
        var uri = ZenUriParser.BuildUri("T/T48/T48n2005.xml",
            fromLb: "0292b28", side: SearchSide.Translated, user: "bob");
        var result = ZenUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("bob", result.User);
        Assert.Equal("0292b28", result.FromLb);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    // ==== Per-user deep links ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Scholar ====

    [Fact]
    public void TryParseDeepLink_ScholarWithUser()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://scholar/col/pass/bob");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("col", result.ScholarCollectionId);
        Assert.Equal("pass", result.ScholarPassageId);
        Assert.Equal("bob", result.ScholarUser);
    }

    [Fact]
    public void TryParseDeepLink_ScholarWithoutUser_UserIsNull()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://scholar/col/pass");

        Assert.NotNull(result);
        Assert.Null(result.ScholarUser);
    }

    [Fact]
    public void BuildScholarUri_WithUser_RoundTrip()
    {
        var uri = ZenUriParser.BuildScholarUri("c1", "p1", user: "bob");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Scholar, result.Kind);
        Assert.Equal("c1", result.ScholarCollectionId);
        Assert.Equal("p1", result.ScholarPassageId);
        Assert.Equal("bob", result.ScholarUser);
    }

    // ==== Per-user deep links ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â Tags ====

    [Fact]
    public void TryParseDeepLink_TagsPathUser()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005/alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
        Assert.Null(result.TagsTagId);
    }

    [Fact]
    public void TryParseDeepLink_TagsQueryUserFallback()
    {
        // Legacy ?user= query param still works
        var result = ZenUriParser.TryParseDeepLink("zen://tags/T48n2005?user=alice");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("alice", result.TagsUser);
    }

    [Fact]
    public void BuildTagsUri_WithUser_RoundTrip()
    {
        var uri = ZenUriParser.BuildTagsUri("T48n2005", "alice", "tag-42");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Tags, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.TagsRelPath);
        Assert.Equal("alice", result.TagsUser);
        Assert.Equal("tag-42", result.TagsTagId);
    }

    // ==== Shareable URL with user ====

    [Fact]
    public void BuildShareableUrl_WithUser_AppendsUserSegment()
    {
        var url = ZenUriParser.BuildShareableUrl("T/T48/T48n2005.xml",
            fromLb: "0001a01", side: SearchSide.Translated, user: "bob");

        Assert.Contains("/bob", url);
        Assert.DoesNotContain("?user=", url);
    }


    [Fact]
    public void TryParseDeepLink_CompareLink_ReturnsCompareKind()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://compare/T48n2005/orig/me/community?from=0292a26&to=0292a29");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Compare, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.CompareRelPath);
        Assert.Equal(ComparePaneTarget.Original, result.ComparePane);
        Assert.Equal("me", result.CompareSourceA);
        Assert.Equal("community", result.CompareSourceB);
        Assert.NotNull(result.CompareNavigation);
        Assert.Equal("0292a26", result.CompareNavigation!.FromLb);
        Assert.Equal("0292a29", result.CompareNavigation.ToLb);
        Assert.Equal(SearchSide.Original, result.CompareNavigation.Side);
    }

    [Fact]
    public void TryParseDeepLink_CompareLink_TranslatedPane_UsesTranslatedSide()
    {
        var result = ZenUriParser.TryParseDeepLink("zen://compare/T48n2005/b/alice/bob?highlight=linji");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Compare, result.Kind);
        Assert.Equal(ComparePaneTarget.TranslationB, result.ComparePane);
        Assert.Equal("alice", result.CompareSourceA);
        Assert.Equal("bob", result.CompareSourceB);
        Assert.NotNull(result.CompareNavigation);
        Assert.Equal(SearchSide.Translated, result.CompareNavigation!.Side);
        Assert.Equal("linji", result.CompareNavigation.MatchText);
    }

    [Fact]
    public void BuildCompareUri_RoundTrip()
    {
        var uri = ZenUriParser.BuildCompareUri(
            "T/T48/T48n2005.xml",
            ComparePaneTarget.TranslationA,
            "me",
            "alice",
            fromLb: "0292a26",
            toLb: "0292a29");
        var result = ZenUriParser.TryParseDeepLink(uri);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Compare, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.CompareRelPath);
        Assert.Equal(ComparePaneTarget.TranslationA, result.ComparePane);
        Assert.Equal("me", result.CompareSourceA);
        Assert.Equal("alice", result.CompareSourceB);
        Assert.NotNull(result.CompareNavigation);
        Assert.Equal("0292a26", result.CompareNavigation!.FromLb);
        Assert.Equal("0292a29", result.CompareNavigation.ToLb);
        Assert.Equal(SearchSide.Translated, result.CompareNavigation.Side);
    }

    [Fact]
    public void BuildShareableCompareUrl_RoundTrip()
    {
        var url = ZenUriParser.BuildShareableCompareUrl(
            "T/T48/T48n2005.xml",
            ComparePaneTarget.Original,
            "community",
            "alice",
            highlightText: "some highlighted text");
        var result = ZenUriParser.TryParseDeepLink(url);

        Assert.NotNull(result);
        Assert.Equal(DeepLinkKind.Compare, result.Kind);
        Assert.Equal("T/T48/T48n2005.xml", result.CompareRelPath);
        Assert.Equal(ComparePaneTarget.Original, result.ComparePane);
        Assert.Equal("community", result.CompareSourceA);
        Assert.Equal("alice", result.CompareSourceB);
        Assert.NotNull(result.CompareNavigation);
        Assert.Equal("some highlighted text", result.CompareNavigation!.MatchText);
        Assert.Equal(SearchSide.Original, result.CompareNavigation.Side);
    }

    [Fact]
    public void BuildShareableUrl_WithoutUser_NoUserSegment()
    {
        var url = ZenUriParser.BuildShareableUrl("T/T48/T48n2005.xml",
            side: SearchSide.Translated);

        Assert.EndsWith("/en", url);
    }
}



