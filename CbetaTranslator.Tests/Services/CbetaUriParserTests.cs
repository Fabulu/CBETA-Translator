using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

/// <summary>
/// Tests for <see cref="CbetaUriParser"/>: TryParse, BuildUri, CJK encoding, round-trips,
/// and malformed-input handling.
/// </summary>
public class CbetaUriParserTests
{
    // ---- 1. TryParse — valid URI with all params returns correct NavigationRequest ----
    // NOTE: System.Uri lowercases the Host component (first path segment before '/').
    // This is a known bug — see review comments. Tests use lowercase first segment
    // to match current behavior.

    [Fact]
    public void TryParse_ValidUri_AllParams_ReturnsCorrectRequest()
    {
        // Use lowercase first segment because Uri.Host lowercases it
        var uri = "cbeta://t/T48/T48n2005.xml?highlight=hello&side=Translated&lctx=left&rctx=right&block=42";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal("t/T48/T48n2005.xml", result.RelPath);
        Assert.Equal("hello", result.MatchText);
        Assert.Equal(SearchSide.Translated, result.Side);
        Assert.Equal("left", result.LeftContext);
        Assert.Equal("right", result.RightContext);
        Assert.Equal(42, result.AnchorStartHint);
    }

    [Fact]
    public void TryParse_HostIsLowercased_BugDocumentation()
    {
        // This test documents the bug: Uri.Host lowercases the first segment.
        // "T" in "cbeta://T/T48/..." becomes "t" after parsing.
        // This can break case-sensitive file lookups on Linux.
        var uri = "cbeta://T/T48/T48n2005.xml";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        // The host "T" is lowercased to "t" by System.Uri — this is the bug
        Assert.Equal("t/T48/T48n2005.xml", result.RelPath);
        Assert.NotEqual("T/T48/T48n2005.xml", result.RelPath); // Proves the bug
    }

    // ---- 2. TryParse — file-only URI (no query params) works ----

    [Fact]
    public void TryParse_FileOnlyUri_NoQueryParams_Works()
    {
        var uri = "cbeta://T/T48/T48n2005.xml";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        // Host lowercased by System.Uri (see bug documentation test)
        Assert.Equal("t/T48/T48n2005.xml", result.RelPath);
        Assert.Null(result.MatchText);
        Assert.Equal(SearchSide.Original, result.Side);
        Assert.Null(result.LeftContext);
        Assert.Null(result.RightContext);
        Assert.Null(result.AnchorStartHint);
    }

    // ---- 3. TryParse — CJK text in highlight is decoded correctly ----

    [Fact]
    public void TryParse_CjkHighlight_DecodedCorrectly()
    {
        // Build the URI using BuildUri to ensure correct encoding, then parse it back
        var cjkText = "\u4f5b\u8aaa"; // 佛說
        var encoded = Uri.EscapeDataString(cjkText);
        var uri = $"cbeta://T/T01/T01n0001.xml?highlight={encoded}";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(cjkText, result.MatchText);
    }

    // ---- 4. TryParse — malformed URI returns null ----

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

    [Fact]
    public void TryParse_EmptyCbetaUri_ReturnsNullOrEmptyPath()
    {
        // "cbeta://" parses as valid URI with empty host — relPath ends up "/"
        // which is not a usable file path, but the parser does not reject it.
        // This documents current behavior; ideally it should return null.
        var result = CbetaUriParser.TryParse("cbeta://");

        // Current behavior: returns a NavigationRequest with RelPath="/"
        // This is a minor gap — caller must validate RelPath before use.
        if (result != null)
            Assert.Equal("/", result.RelPath);
    }

    // ---- 5. TryParse — non-cbeta scheme returns null ----

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

    // ---- 6. BuildUri — file only produces correct URI ----

    [Fact]
    public void BuildUri_FileOnly_ProducesCorrectUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml");

        Assert.Equal("cbeta://T/T48/T48n2005.xml", uri);
    }

    // ---- 7. BuildUri — with highlight produces encoded URI ----

    [Fact]
    public void BuildUri_WithHighlight_ProducesEncodedUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", highlightText: "hello world");

        Assert.StartsWith("cbeta://T/T48/T48n2005.xml?highlight=", uri);
        Assert.Contains("hello%20world", uri);
    }

    // ---- 8. BuildUri — with CJK highlight encodes correctly ----

    [Fact]
    public void BuildUri_CjkHighlight_EncodesCorrectly()
    {
        var cjkText = "\u4f5b\u8aaa\u963f\u5f4c\u9640\u7d93"; // 佛說阿彌陀經
        var uri = CbetaUriParser.BuildUri("T/T12/T12n0366.xml", highlightText: cjkText);

        // URI should not contain raw CJK characters
        Assert.DoesNotContain(cjkText, uri);
        // But it should start with the expected base
        Assert.StartsWith("cbeta://T/T12/T12n0366.xml?highlight=", uri);
    }

    // ---- 9. BuildUri — with all params produces correct URI ----

    [Fact]
    public void BuildUri_AllParams_ProducesCorrectUri()
    {
        var uri = CbetaUriParser.BuildUri(
            "T/T48/T48n2005.xml",
            highlightText: "test",
            side: SearchSide.Translated,
            leftContext: "before",
            rightContext: "after",
            blockNumber: 7);

        Assert.Contains("highlight=test", uri);
        Assert.Contains("side=Translated", uri);
        Assert.Contains("lctx=before", uri);
        Assert.Contains("rctx=after", uri);
        Assert.Contains("block=7", uri);
    }

    // ---- 10. Round-trip: BuildUri -> TryParse preserves all fields ----

    [Fact]
    public void RoundTrip_BuildUri_ThenTryParse_PreservesAllFields()
    {
        // Use lowercase first segment to avoid the Host-lowercasing issue
        var relPath = "t/T48/T48n2005.xml";
        var highlight = "some search text";
        var side = SearchSide.Translated;
        var lctx = "left context";
        var rctx = "right context";
        var block = 99;

        var uri = CbetaUriParser.BuildUri(relPath, highlightText: highlight, side: side, leftContext: lctx, rightContext: rctx, blockNumber: block);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal(highlight, result.MatchText);
        Assert.Equal(side, result.Side);
        Assert.Equal(lctx, result.LeftContext);
        Assert.Equal(rctx, result.RightContext);
        Assert.Equal(block, result.AnchorStartHint);
    }

    [Fact]
    public void RoundTrip_UppercaseFirstSegment_LosesCase()
    {
        // Documents that round-trip breaks when first path segment is uppercase,
        // because System.Uri lowercases the Host component.
        var relPath = "T/T48/T48n2005.xml";

        var uri = CbetaUriParser.BuildUri(relPath);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        // Round-trip DOES NOT preserve case of first segment — this is the bug
        Assert.NotEqual(relPath, result.RelPath);
        Assert.Equal("t/T48/T48n2005.xml", result.RelPath);
    }

    [Fact]
    public void RoundTrip_CjkHighlight_PreservesText()
    {
        var cjkText = "\u5982\u662f\u6211\u805e"; // 如是我聞
        // Use lowercase first segment for clean round-trip
        var relPath = "t/T01/T01n0001.xml";

        var uri = CbetaUriParser.BuildUri(relPath, highlightText: cjkText);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(relPath, result.RelPath);
        Assert.Equal(cjkText, result.MatchText);
    }

    // ---- Additional edge cases ----

    [Fact]
    public void BuildUri_BackslashPath_NormalizedToForwardSlash()
    {
        var uri = CbetaUriParser.BuildUri(@"T\T48\T48n2005.xml");

        Assert.Equal("cbeta://T/T48/T48n2005.xml", uri);
        Assert.DoesNotContain("\\", uri);
    }

    [Fact]
    public void TryParse_CaseInsensitiveScheme_Works()
    {
        var uri = "CBETA://T/T48/T48n2005.xml";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        // Host lowercased by System.Uri
        Assert.Equal("t/T48/T48n2005.xml", result.RelPath);
    }

    [Fact]
    public void TryParse_SideParam_CaseInsensitive()
    {
        var uri = "cbeta://T/T48/T48n2005.xml?side=translated";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Translated, result.Side);
    }

    [Fact]
    public void TryParse_InvalidSide_DefaultsToOriginal()
    {
        var uri = "cbeta://T/T48/T48n2005.xml?side=invalid";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(SearchSide.Original, result.Side);
    }

    [Fact]
    public void TryParse_InvalidBlock_IgnoredGracefully()
    {
        var uri = "cbeta://T/T48/T48n2005.xml?block=notanumber";

        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Null(result.AnchorStartHint);
    }

    [Fact]
    public void BuildUri_DefaultSide_NotIncludedInUri()
    {
        var uri = CbetaUriParser.BuildUri("T/T48/T48n2005.xml", side: SearchSide.Original);

        Assert.DoesNotContain("side=", uri);
    }

    [Fact]
    public void RoundTrip_SpecialCharactersInContexts_Preserved()
    {
        var lctx = "text with spaces & symbols=yes";
        var rctx = "more?special/chars";

        var uri = CbetaUriParser.BuildUri("t/T01/T01n0001.xml",
            highlightText: "match", leftContext: lctx, rightContext: rctx);
        var result = CbetaUriParser.TryParse(uri);

        Assert.NotNull(result);
        Assert.Equal(lctx, result.LeftContext);
        Assert.Equal(rctx, result.RightContext);
    }
}
