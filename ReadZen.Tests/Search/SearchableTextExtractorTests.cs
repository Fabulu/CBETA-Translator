using System;
using System.Text;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// Direct unit tests for <see cref="SearchIndexService.MakeSearchableTextFromXml_Fast"/>.
/// Ports the relevant SPA-side <c>extract-text.test.js</c> coverage (<c>C:\Programmieren\ZenLinkPage\test\extract-text.test.js</c>)
/// to xUnit. Locks in the &lt;app&gt; suppression semantics plus the regression
/// test for the self-closing &lt;app/&gt; bug fixed in this PR.
///
/// lb / pb anchor-map tests are intentionally skipped — the desktop method
/// returns plain <c>string</c> (no offset map) and would require API extension.
/// </summary>
public class SearchableTextExtractorTests
{
    private static string Wrap(string body) => $"<TEI><text><body>{body}</body></text></TEI>";

    private static string Extract(string xml, bool decodeEntities = true)
        => SearchIndexService.MakeSearchableTextFromXml_Fast(xml, decodeEntities);

    // ---- Body parsing & general hygiene ---------------------------------

    [Fact]
    public void Extract_DropsBodyPrefix_KeepsOnlyBodyContent()
    {
        // teiHeader text outside <body> must not appear in the searchable string.
        var xml = "<TEI><teiHeader>HDR</teiHeader><text><body>inside</body></text></TEI>";
        var text = Extract(xml);
        Assert.Contains("inside", text);
        Assert.DoesNotContain("HDR", text);
    }

    [Fact]
    public void Extract_ReturnsEmptyString_WhenNoBodyTag()
    {
        Assert.Equal("", Extract("<TEI><teiHeader/></TEI>"));
        Assert.Equal("", Extract("<root>nope</root>"));
    }

    [Fact]
    public void Extract_ReturnsEmptyString_WhenBodyUnclosed()
    {
        Assert.Equal("", Extract("<TEI><text><body>hello world"));
    }

    [Fact]
    public void Extract_StripsAllTags_BetweenBodyTags()
    {
        var xml = Wrap("plain<i>italic</i><b>bold</b>end");
        var text = Extract(xml);
        // Tag markup is stripped; only character data survives.
        Assert.DoesNotContain("<", text);
        Assert.DoesNotContain(">", text);
        Assert.Contains("plain", text);
        Assert.Contains("italic", text);
        Assert.Contains("bold", text);
        Assert.Contains("end", text);
    }

    [Fact]
    public void Extract_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", Extract(""));
        Assert.Equal("", Extract(null!));
    }

    [Fact]
    public void Extract_CollapsesAdjacentWhitespace_ToSingleSpace()
    {
        var xml = Wrap("  hello   world  ");
        Assert.Equal("hello world", Extract(xml));
    }

    [Fact]
    public void Extract_DropsCarriageReturn_TreatsOtherWhitespaceAsSpace()
    {
        // \r is dropped; \n, \t, \f, \v collapse to a single space between runs.
        var xml = Wrap("a\r\nb\tc\fd\ve");
        Assert.Equal("a b c d e", Extract(xml));
    }

    [Fact]
    public void Extract_PreservesCjkCharacters_VerbatimNoNormalize()
    {
        // CJK characters must pass through verbatim — no normalization, no replacement.
        var xml = Wrap("無門關 first; 雲門 second");
        var text = Extract(xml);
        Assert.Contains("無門關", text);
        Assert.Contains("雲門", text);
    }

    // ---- Entity decoding ------------------------------------------------

    [Fact]
    public void Extract_DecodesNamedEntities_AmpLtGt()
    {
        var xml = Wrap("p&amp;q");
        var text = Extract(xml);
        Assert.Contains("&", text);
        // Single-pass decode mirrors WebUtility.HtmlDecode — no residue.
        Assert.DoesNotContain("amp;", text);
    }

    [Fact]
    public void Extract_DecodesNumericEntities_DecimalAndHex()
    {
        Assert.Contains("一", Extract(Wrap("a&#x4E00;b")));   // hex
        Assert.Contains("&",  Extract(Wrap("x&#38;y")));       // decimal &
    }

    [Fact]
    public void Extract_DecodesDoublyEscapedEntity_SinglePassOnly()
    {
        // WebUtility.HtmlDecode is single-pass: "&amp;amp;" -> "&amp;" (not "&").
        var text = Extract(Wrap("one&amp;amp;two"));
        Assert.Contains("&amp;", text);
        Assert.DoesNotContain("&amp;amp;", text);
    }

    [Fact]
    public void Extract_DoublyEscapedHexEntity_DecodesOnlyOuterAmp()
    {
        // "&amp;#x4E00;" -> "&#x4E00;" after one decode pass — NOT the resolved 一.
        var text = Extract(Wrap("a&amp;#x4E00;b"));
        Assert.Contains("&#x4E00;", text);
        Assert.DoesNotContain("一", text);
    }

    [Fact]
    public void Extract_HtmlDecodeFlagFalse_LeavesEntitiesIntact()
    {
        // With the decode pass suppressed, raw entities survive verbatim.
        var text = Extract(Wrap("p&amp;q"), decodeEntities: false);
        Assert.Contains("&amp;", text);
        Assert.DoesNotContain("&q", text.Replace("&amp;q", ""));
    }

    [Fact]
    public void Extract_NoAmpersand_SkipsDecodePassEntirely()
    {
        // Sanity: text without any '&' should round-trip unchanged.
        Assert.Equal("plain text only", Extract(Wrap("plain text only")));
    }

    // ---- <app> suppression: paired form ---------------------------------

    [Fact]
    public void Extract_PairedAppTag_SuppressesEnclosedContent()
    {
        // <app><rdg>v</rdg></app> between two lb's: 'v' must vanish, surrounding text survives.
        var xml = Wrap(@"<lb n=""1""/>before<app><rdg>v</rdg></app><lb n=""2""/>after");
        var text = Extract(xml);
        Assert.Contains("before", text);
        Assert.Contains("after", text);
        Assert.DoesNotContain("v", text);
    }

    [Fact]
    public void Extract_PairedAppTag_ExcludesBothLemAndRdg()
    {
        var xml = Wrap("A<app><lem>foo</lem><rdg>bar</rdg></app>B");
        var text = Extract(xml);
        Assert.DoesNotContain("foo", text);
        Assert.DoesNotContain("bar", text);
        Assert.Contains("A", text);
        Assert.Contains("B", text);
    }

    [Fact]
    public void Extract_AppTagWithAttributes_StillSkipped()
    {
        // <app type="apparatus"> matches the "app" prefix + space terminator, so its content is suppressed.
        var xml = Wrap(@"keep1<app type=""apparatus"">drop</app>keep2");
        var text = Extract(xml);
        Assert.Contains("keep1", text);
        Assert.Contains("keep2", text);
        Assert.DoesNotContain("drop", text);
    }

    [Fact]
    public void Extract_LemRdgInsideApp_DoNotEscapeSkip()
    {
        // The <lem>/<rdg> open/close tags themselves don't decrement appSkipDepth;
        // only </app> can close the skip.
        var xml = Wrap("L<app><lem>x</lem>middle<rdg>y</rdg></app>R");
        var text = Extract(xml);
        Assert.DoesNotContain("x", text);
        Assert.DoesNotContain("y", text);
        Assert.DoesNotContain("middle", text);
        Assert.Contains("L", text);
        Assert.Contains("R", text);
    }

    // ---- <app> suppression: self-closing (THE BUG) ----------------------

    [Fact]
    public void Extract_SelfClosingAppTag_DoesNotSuppressFollowingText()
    {
        // Regression for the bug: <app/> must be a no-op for appSkipDepth.
        // Before the fix, depth++ fired unbalanced and all subsequent body text was dropped.
        var xml = @"<TEI><body><lb n=""0001a01""/>before<app/><lb n=""0001a02""/>after</body></TEI>";
        var text = Extract(xml);
        Assert.Contains("before", text);
        Assert.Contains("after", text);
    }

    [Fact]
    public void Extract_SelfClosingAppTagWithAttributes_DoesNotSuppressFollowingText()
    {
        // <app n="x"/> form — same regression, attribute-bearing self-close.
        var xml = Wrap(@"alpha<app n=""x""/>beta");
        var text = Extract(xml);
        Assert.Contains("alpha", text);
        Assert.Contains("beta", text);
    }

    [Fact]
    public void Extract_MinimalSelfCloseFromPlan_KeepsBothChineseRuns()
    {
        // Exact plan acceptance literal: "<TEI><body>前<app/>後</body></TEI>".
        var text = Extract("<TEI><body>前<app/>後</body></TEI>");
        Assert.Contains("前", text);
        Assert.Contains("後", text);
    }

    [Fact]
    public void Extract_AppTagWithWhitespaceBeforeSlash_StillSelfCloses()
    {
        // Reviewer Wave 5 H3: whitespace between the tag name (or attributes) and
        // the closing "/>" is legal XML syntax. The self-close guard checks
        // xml[i-1] == '/' — that char must be the slash immediately preceding '>'.
        // Both forms below have a '/' as xml[i-1] and must therefore self-close.
        var xml1 = @"<TEI><body>L1<app   />R1</body></TEI>";       // multiple spaces inside tag
        var text1 = Extract(xml1);
        Assert.Contains("L1", text1);
        Assert.Contains("R1", text1);

        var xml2 = @"<TEI><body>L2<app n=""x"" />R2</body></TEI>"; // attr + space before />
        var text2 = Extract(xml2);
        Assert.Contains("L2", text2);
        Assert.Contains("R2", text2);
    }

    // ---- Depth tracking under nesting -----------------------------------

    [Fact]
    public void Extract_NestedAppTag_DepthBalances()
    {
        // X<app>1<app>2</app>3</app>Y — all inner digits suppressed, outer X / Y survive.
        var xml = Wrap("X<app>1<app>2</app>3</app>Y");
        var text = Extract(xml);
        Assert.DoesNotContain("1", text);
        Assert.DoesNotContain("2", text);
        Assert.DoesNotContain("3", text);
        Assert.Contains("X", text);
        Assert.Contains("Y", text);
    }

    [Fact]
    public void Extract_DeeplyNestedAppTags_AllSuppressed()
    {
        var xml = Wrap("A<app>1<app>2<app>3<app>4</app>5</app>6</app>7</app>B");
        var text = Extract(xml);
        foreach (var d in "1234567")
            Assert.DoesNotContain(d.ToString(), text);
        Assert.Contains("A", text);
        Assert.Contains("B", text);
    }

    [Fact]
    public void Extract_NestedAppWithSelfClose_HandlesMixedDepth()
    {
        // Mix paired + self-close: A<app>1<app/>2</app>B
        // - <app>     opens, depth=1
        // - <app/>    self-close, depth unchanged (with fix)
        // - </app>    closes, depth=0
        // Inner 1 and 2 must vanish (both are inside outer <app>); A and B survive.
        var xml = Wrap("A<app>1<app/>2</app>B");
        var text = Extract(xml);
        Assert.DoesNotContain("1", text);
        Assert.DoesNotContain("2", text);
        Assert.Contains("A", text);
        Assert.Contains("B", text);
    }

    [Fact]
    public void Extract_SelfCloseAfterPairedClose_DoesNotUnderflow()
    {
        // <app>x</app><app/>tail — after </app> depth=0; <app/> must not push it negative.
        var xml = Wrap("head<app>x</app><app/>tail");
        var text = Extract(xml);
        Assert.Contains("head", text);
        Assert.Contains("tail", text);
        Assert.DoesNotContain("x", text);
    }

    // ---- Tag-name disambiguation ----------------------------------------

    [Fact]
    public void Extract_UnknownTagWithAppPrefix_NotConfusedForApp()
    {
        // <appendix>kept</appendix> — name starts with 'a','p','p' but next char 'e'
        // is NOT in the terminator set ( / > space tab newline ), so the <app>
        // branch must NOT be entered and 'kept' must survive.
        var xml = Wrap("lead<appendix>kept</appendix>tail");
        var text = Extract(xml);
        Assert.Contains("kept", text);
        Assert.Contains("lead", text);
        Assert.Contains("tail", text);
    }

    [Fact]
    public void Extract_RdgGroupTag_NotConfusedForRdg()
    {
        // The desktop suppressor is scoped to <app> only; <rdgGroup> is a
        // distinct tag and its contents survive.
        var xml = Wrap("lead<rdgGroup>kept</rdgGroup>tail");
        var text = Extract(xml);
        Assert.Contains("kept", text);
        Assert.Contains("lead", text);
        Assert.Contains("tail", text);
    }

    [Fact]
    public void Extract_BareRdgOutsideApp_NotSuppressed()
    {
        // Desktop skip is <app>-scoped; a loose <rdg> outside <app> is not suppressed.
        var xml = Wrap("p<rdg>kept</rdg>q");
        var text = Extract(xml);
        Assert.Contains("kept", text);
    }

    // ---- Combined pipeline ----------------------------------------------

    [Fact]
    public void Extract_CombinedAppAndEntities_FullPipeline()
    {
        // App suppression + entity decode + whitespace collapse, all in one round-trip.
        var xml = Wrap("  start &amp; <app><rdg>drop</rdg></app>  end &#x4E00;  ");
        var text = Extract(xml);
        Assert.Contains("start", text);
        Assert.Contains("&", text);     // &amp; decoded
        Assert.Contains("end", text);
        Assert.Contains("一", text);    // &#x4E00; decoded
        Assert.DoesNotContain("drop", text);
    }

    [Fact]
    public void Extract_LineBreakTagBoundary_InsertsSpaceBetweenChineseRuns()
    {
        // A tag boundary between two non-whitespace runs inserts a single space.
        var xml = Wrap(@"前<lb n=""0001a02""/>後");
        var text = Extract(xml);
        Assert.Contains("前", text);
        Assert.Contains("後", text);
        // Both must appear and be separated by a space (single tag-boundary space).
        var iFirst = text.IndexOf("前", StringComparison.Ordinal);
        var iSecond = text.IndexOf("後", StringComparison.Ordinal);
        Assert.True(iFirst >= 0 && iSecond > iFirst, $"expected ordered runs, got '{text}'");
    }

    // ---- Smoke / robustness ---------------------------------------------

    // ---- Gap-fill: tag-name disambiguation + attribute permutations ----

    [Fact]
    public void Extract_SelfCloseAppWithXmlnsAttribute_StillRecognizedAsApp()
    {
        // <app xmlns="..." /> — attribute-bearing self-close with internal whitespace.
        // The space after "app" is in the terminator set so the tag matches the <app> branch,
        // and `i-1 == '/'` makes it a self-close (depth no-op). Surrounding runs survive.
        var xml = Wrap("alpha<app xmlns=\"http://example.org/ns\" />beta");
        var text = Extract(xml);
        Assert.Contains("alpha", text);
        Assert.Contains("beta", text);
    }

    [Fact]
    public void Extract_ApplicationTag_NotConfusedForApp()
    {
        // <application>kept</application> — name starts with 'a','p','p' but the 4th char 'l'
        // is NOT in the terminator set (space/>/slash/tab/newline), so the <app> branch must
        // NOT engage. The text "kept" must survive — regression for the near-name false positive.
        var xml = Wrap("lead<application>kept</application>tail");
        var text = Extract(xml);
        Assert.Contains("kept", text);
        Assert.Contains("lead", text);
        Assert.Contains("tail", text);
    }

    [Fact]
    public void Extract_UppercaseAppTag_NotSuppressed_TeiIsCaseSensitive()
    {
        // TEI is case-sensitive: <APP/> is NOT the same element as <app/>. The fast extractor
        // does a literal lower-case 'a','p','p' compare, so uppercase APP must NOT engage the
        // skip branch. Anything between <APP> and </APP> survives.
        // This locks in the documented behaviour (consistent with corpus invariant: all CBETA
        // <app> tags are lowercase).
        var xml = Wrap("lead<APP>kept</APP>tail");
        var text = Extract(xml);
        Assert.Contains("kept", text);
        Assert.Contains("lead", text);
        Assert.Contains("tail", text);
    }

    [Fact]
    public void Extract_LargeDocumentRoundtrip_NoCrash()
    {
        // ~100 KB body with mixed apparatus + plain runs. Smoke test: no crash, output non-empty.
        var sb = new StringBuilder(100_000);
        for (int i = 0; i < 2000; i++)
        {
            sb.Append("plain").Append(i).Append(' ');
            if ((i & 7) == 0) sb.Append("<app><rdg>variant").Append(i).Append("</rdg></app>");
            if ((i & 15) == 0) sb.Append("<app/>");
        }
        var xml = Wrap(sb.ToString());
        var text = Extract(xml);
        Assert.False(string.IsNullOrEmpty(text));
        Assert.Contains("plain0", text);
        Assert.Contains("plain1999", text);
        // The fix means text after every <app/> still flows through.
        Assert.DoesNotContain("variant0", text);
    }
}
