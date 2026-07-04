using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.2 (RUN-20260702-2259 R2-H2/M4): the hand-rolled
/// tag scanner corrupted its capture state on self-closing container tags and nested
/// notes, and leaked text from XML comments containing '&gt;'. All of these silently
/// shifted the position map (the invariant check cannot fire — the map stays
/// consistent, just wrong), poisoning segments, selection sync, and write-back offsets.
/// </summary>
public class TeiRendererScannerHardeningTests
{
    private const string Header =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body><div1>";

    private const string Footer = "</div1></body></text></TEI>";

    private static RenderedDocument RenderSnippet(string bodyXml)
        => TeiRenderer.Render(Header + bodyXml + Footer);

    // ---------------------------------------------------------------
    // Self-closing <note/> (R2-H2): must not start a capture that never ends
    // ---------------------------------------------------------------

    [Fact]
    public void SelfClosingInlineNote_DoesNotSwallowFollowingText()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>before<note place=\"inline\"/>after");

        Assert.Contains("before", doc.Text);
        Assert.Contains("after", doc.Text);
    }

    [Fact]
    public void SelfClosingCommunityNote_DoesNotSwallowFollowingText()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>before<note type=\"community\" resp=\"someone\"/>after");

        Assert.Contains("after", doc.Text);
    }

    // ---------------------------------------------------------------
    // Nested <note> (R2-H2): capture must end at the OUTER close tag
    // ---------------------------------------------------------------

    [Fact]
    public void NestedNote_TailStaysInAnnotation_NotInRenderedText()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>main" +
            "<note place=\"inline\">outer<note>inner</note>NOTETAIL</note>visible");

        // Before the fix the capture ended at the FIRST </note>, so NOTETAIL leaked
        // into the rendered text and shifted every later position-map offset.
        Assert.DoesNotContain("NOTETAIL", doc.Text);
        Assert.Contains("visible", doc.Text);

        var note = Assert.Single(doc.Annotations.Where(a => a.Kind == "inline"));
        Assert.Contains("inner", note.Text);
        Assert.Contains("NOTETAIL", note.Text);
    }

    // ---------------------------------------------------------------
    // Comments containing '>' (R2-M4): must be skipped wholesale
    // ---------------------------------------------------------------

    [Fact]
    public void CommentContainingGt_DoesNotLeakTextIntoDocument()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>before<!-- a > b -->after");

        // Before the fix the scanner ended the "tag" at the first '>', then emitted
        // " b -->" as visible text.
        Assert.DoesNotContain("b -->", doc.Text);
        Assert.Contains("before", doc.Text);
        Assert.Contains("after", doc.Text);
    }

    [Fact]
    public void PlainComment_IsStillIgnored()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>before<!-- plain comment -->after");

        Assert.DoesNotContain("plain comment", doc.Text);
        Assert.Contains("before", doc.Text);
        Assert.Contains("after", doc.Text);
    }

    [Fact]
    public void UnterminatedComment_DoesNotThrowOrLeak()
    {
        var doc = RenderSnippet("<lb n=\"0001a01\" ed=\"T\"/>before<!-- never closed ");

        Assert.Contains("before", doc.Text);
        Assert.DoesNotContain("never closed", doc.Text);
    }

    [Fact]
    public void CdataContainingGt_IsSkipped()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>before<![CDATA[x > y]]>after");

        Assert.DoesNotContain("y]]", doc.Text);
        Assert.Contains("before", doc.Text);
        Assert.Contains("after", doc.Text);
    }

    // ---------------------------------------------------------------
    // Self-closing <app/> — same family as the search-v4 <app/> extractor bug
    // ---------------------------------------------------------------

    [Fact]
    public void SelfClosingApp_DoesNotSuppressFollowingText()
    {
        var doc = RenderSnippet("<lb n=\"0001a01\" ed=\"T\"/>before<app/>after");

        // Before the fix appDepth was incremented and never decremented, so ALL
        // following document text was suppressed as apparatus content.
        Assert.Contains("after", doc.Text);
    }

    // ---------------------------------------------------------------
    // Self-closing <head/> (R2-H2 "same treatment for inHeadCapture")
    // ---------------------------------------------------------------

    [Fact]
    public void SelfClosingHead_DoesNotTurnFollowingTextIntoAHeading()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/><head/>NotAHeading</head>rest");

        // Before the fix <head/> opened a capture and the stray </head> flushed it,
        // fabricating a heading out of body text.
        Assert.DoesNotContain(doc.Headings, h => h.Text.Contains("NotAHeading"));
        Assert.Contains("NotAHeading", doc.Text);
    }

    // ---------------------------------------------------------------
    // Self-closing <rdg/> inside apparatus: omission reading, no capture leak
    // ---------------------------------------------------------------

    [Fact]
    public void SelfClosingRdg_RecordsOmissionWithoutCapturingStrayText()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg0001\"/>A<anchor xml:id=\"end0001\"/>" +
            "<app from=\"#beg0001\"><lem>A</lem><rdg wit=\"W\"/>STRAY</app>rest");

        var app = Assert.Single(doc.Annotations.Where(a => a.Kind == "apparatus"));
        // Before the fix <rdg wit="W"/> left the capture open and STRAY (loose text
        // inside <app>, outside any reading) was recorded as the reading's content.
        Assert.DoesNotContain("STRAY", app.Text);
        Assert.Contains("(empty)", app.Text);
        Assert.Contains("[W]", app.Text);
        Assert.DoesNotContain("STRAY", doc.Text);
        Assert.Contains("rest", doc.Text);
    }
}
