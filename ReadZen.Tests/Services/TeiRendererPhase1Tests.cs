using ReadZen.App.Models;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Phase 1 tests for TeiRenderer: app suppression, caesura, lg paragraph breaks,
/// cross-lb app elements, and nested note suppression inside app.
/// </summary>
public class TeiRendererPhase1Tests
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
    // 1. Basic app suppression
    // ---------------------------------------------------------------

    /// <summary>
    /// Content inside app/lem and app/rdg must be suppressed entirely.
    /// Only text outside the app element should appear in the rendered output.
    /// </summary>
    [Fact]
    public void Render_AppLemRdg_ContentIsSuppressed()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<app><lem>LemmaText</lem><rdg>ReadingText</rdg></app>");

        Assert.DoesNotContain("LemmaText", doc.Text);
        Assert.DoesNotContain("ReadingText", doc.Text);
    }

    // ---------------------------------------------------------------
    // 2. App suppression with surrounding text (no duplication)
    // ---------------------------------------------------------------

    /// <summary>
    /// When text between anchors is repeated inside app/lem, the rendered output
    /// must contain the anchor-bracketed text exactly once and must not include
    /// the rdg variant. Result: "Text\nAmore text" (newline from the lb).
    /// </summary>
    [Fact]
    public void Render_AppWithSurroundingText_NoDuplicateNoBVariant()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>Text" +
            "<anchor xml:id=\"beg0001\"/>A<anchor xml:id=\"end0001\"/>" +
            "<app from=\"#beg0001\" to=\"#end0001\"><lem>A</lem><rdg>B</rdg></app>" +
            "more text");

        // The 'A' between the anchors is rendered (outside app).
        // The 'A' inside <lem> and 'B' inside <rdg> are suppressed.
        // So final text should contain exactly one 'A'.
        Assert.Contains("A", doc.Text);
        Assert.DoesNotContain("B", doc.Text);

        // Count occurrences of 'A' — should be exactly 1
        int countA = 0;
        foreach (char c in doc.Text)
            if (c == 'A') countA++;
        Assert.Equal(1, countA);

        // Verify surrounding text is present
        Assert.Contains("Text", doc.Text);
        Assert.Contains("more text", doc.Text);
    }

    // ---------------------------------------------------------------
    // 3. Caesura renders ideographic space (U+3000)
    // ---------------------------------------------------------------

    /// <summary>
    /// A caesura element inside a verse line must produce the ideographic space
    /// character U+3000 between the two halves of the line.
    /// </summary>
    [Fact]
    public void Render_Caesura_InsertsIdeographicSpace()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/><l>First<caesura/>Second</l>");

        // The ideographic space (U+3000) must appear between "First" and "Second".
        // Note: XML whitespace before <caesura/> may also be preserved as a regular space.
        Assert.Contains("\u3000", doc.Text);
        Assert.Contains("First", doc.Text);
        Assert.Contains("Second", doc.Text);

        // Verify ordering: First comes before Second
        int firstIdx = doc.Text.IndexOf("First");
        int secondIdx = doc.Text.IndexOf("Second");
        Assert.True(firstIdx < secondIdx);

        // The ideographic space must be between them
        string between = doc.Text.Substring(firstIdx + "First".Length,
            secondIdx - firstIdx - "First".Length);
        Assert.Contains("\u3000", between);
    }

    // ---------------------------------------------------------------
    // 4. lg produces paragraph breaks around verse groups
    // ---------------------------------------------------------------

    /// <summary>
    /// An lg (line group) element should produce paragraph breaks that separate
    /// it from surrounding prose content.
    /// </summary>
    [Fact]
    public void Render_LgProduceParagraphBreaks()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/><p>prose</p>" +
            "<lg><l>verse</l></lg>" +
            "<p>more</p>");

        // All three text chunks must be present
        Assert.Contains("prose", doc.Text);
        Assert.Contains("verse", doc.Text);
        Assert.Contains("more", doc.Text);

        // The verse must be separated from surrounding prose by newlines.
        // EnsureParagraphBreak inserts "\n\n" (or at least one newline).
        int verseIdx = doc.Text.IndexOf("verse");
        int proseIdx = doc.Text.IndexOf("prose");
        int moreIdx = doc.Text.IndexOf("more");

        // There must be at least one newline between prose and verse
        string between1 = doc.Text.Substring(proseIdx + "prose".Length,
            verseIdx - proseIdx - "prose".Length);
        Assert.Contains("\n", between1);

        // There must be at least one newline between verse and more
        string between2 = doc.Text.Substring(verseIdx + "verse".Length,
            moreIdx - verseIdx - "verse".Length);
        Assert.Contains("\n", between2);
    }

    // ---------------------------------------------------------------
    // 5. Cross-lb app: no spurious segment inside app
    // ---------------------------------------------------------------

    /// <summary>
    /// An lb element inside an app/lem must not create a spurious rendered segment,
    /// because the entire app content is suppressed from the rendered output.
    /// The lb inside app should not produce visible text or segment boundaries
    /// in the final output.
    /// </summary>
    [Fact]
    public void Render_CrossLbApp_NoSpuriousSegment()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>Before" +
            "<app><lem>X<lb n=\"0002a01\" ed=\"T\"/>Y</lem><rdg>Z</rdg></app>" +
            "After");

        // The app content (X, Y, Z) must be suppressed
        Assert.DoesNotContain("X", doc.Text);
        Assert.DoesNotContain("Y", doc.Text);
        Assert.DoesNotContain("Z", doc.Text);

        // Surrounding text must be present
        Assert.Contains("Before", doc.Text);
        Assert.Contains("After", doc.Text);

        // The lb n="0002a01" inside app must NOT have created a rendered segment
        // (since the text around it is suppressed, any segment would be spurious)
        var spuriousSegment = doc.Segments
            .FirstOrDefault(s => (s.Key ?? "").Contains("0002a01"));
        // If a segment exists for the inner lb, it should be zero-length at most
        // (the key concern is no rendered text leaking from inside app)
        if (spuriousSegment.Key != null)
        {
            Assert.Equal(spuriousSegment.Start, spuriousSegment.EndExclusive);
        }
    }

    // ---------------------------------------------------------------
    // 6. Nested note inside app is suppressed
    // ---------------------------------------------------------------

    /// <summary>
    /// A note element nested inside an app/lem must be suppressed along with
    /// the rest of the app content. The note text must not appear in output.
    /// </summary>
    [Fact]
    public void Render_NestedNoteInsideApp_IsSuppressed()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>Before" +
            "<app><lem>X<note type=\"cf1\">ref</note></lem><rdg>Y</rdg></app>" +
            "After");

        // All app content including the note must be suppressed
        Assert.DoesNotContain("X", doc.Text);
        Assert.DoesNotContain("ref", doc.Text);
        Assert.DoesNotContain("Y", doc.Text);

        // Surrounding text must be present
        Assert.Contains("Before", doc.Text);
        Assert.Contains("After", doc.Text);
    }

    /// <summary>
    /// Variant: note with place="inline" inside app must also be suppressed.
    /// </summary>
    [Fact]
    public void Render_InlineNoteInsideApp_IsSuppressed()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>Before" +
            "<app><lem>X<note place=\"inline\">inline ref</note></lem><rdg>Y</rdg></app>" +
            "After");

        Assert.DoesNotContain("inline ref", doc.Text);
        Assert.DoesNotContain("X", doc.Text);
        Assert.DoesNotContain("Y", doc.Text);
        Assert.Contains("Before", doc.Text);
        Assert.Contains("After", doc.Text);
    }
}
