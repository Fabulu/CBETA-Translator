using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Phase 2 tests for TeiRenderer: apparatus annotation emission, rdg capture,
/// MarkerKind detection, empty rdg handling, and no-rdg suppression.
/// </summary>
public class TeiRendererPhase2Tests
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
    // 1. Apparatus annotation emitted
    // ---------------------------------------------------------------

    /// <summary>
    /// Rendering XML with anchor-bracketed text and an app element referencing
    /// those anchors must produce exactly one DocAnnotation with Kind="apparatus".
    /// </summary>
    [Fact]
    public void Render_AppWithAnchors_EmitsApparatusAnnotation()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg1\"/>A<anchor xml:id=\"end1\"/>" +
            "<app from=\"#beg1\" to=\"#end1\">" +
            "<lem>A</lem>" +
            "<rdg wit=\"#wit.orig\">B</rdg>" +
            "</app>");

        var apparatusAnns = doc.Annotations
            .Where(a => a.Kind == "apparatus")
            .ToList();

        Assert.Single(apparatusAnns);
    }

    // ---------------------------------------------------------------
    // 2. Annotation text contains rdg with witness info
    // ---------------------------------------------------------------

    /// <summary>
    /// The apparatus annotation text must contain the reading prefixed with
    /// "Rdg:" and the witness identifier in brackets.
    /// </summary>
    [Fact]
    public void Render_AppAnnotation_TextContainsRdgAndWitness()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg1\"/>A<anchor xml:id=\"end1\"/>" +
            "<app from=\"#beg1\" to=\"#end1\">" +
            "<lem>A</lem>" +
            "<rdg wit=\"#wit.orig\">B</rdg>" +
            "</app>");

        var ann = doc.Annotations.Single(a => a.Kind == "apparatus");

        Assert.Contains("Rdg: B", ann.Text);
        Assert.Contains("#wit.orig", ann.Text);
    }

    // ---------------------------------------------------------------
    // 3. Multi-rdg capture
    // ---------------------------------------------------------------

    /// <summary>
    /// When an app element has multiple rdg children, the annotation text
    /// must contain all readings.
    /// </summary>
    [Fact]
    public void Render_AppWithMultipleRdg_CapturesBoth()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg1\"/>A<anchor xml:id=\"end1\"/>" +
            "<app from=\"#beg1\" to=\"#end1\">" +
            "<lem>A</lem>" +
            "<rdg wit=\"#wit.orig\">X</rdg>" +
            "<rdg wit=\"#wit.cbeta\">Y</rdg>" +
            "</app>");

        var ann = doc.Annotations.Single(a => a.Kind == "apparatus");

        Assert.Contains("Rdg: X", ann.Text);
        Assert.Contains("#wit.orig", ann.Text);
        Assert.Contains("Rdg: Y", ann.Text);
        Assert.Contains("#wit.cbeta", ann.Text);
    }

    // ---------------------------------------------------------------
    // 4. MarkerKind.Apparatus detection
    // ---------------------------------------------------------------

    /// <summary>
    /// A DocAnnotation with Kind="apparatus" must cause GetMarkerKind
    /// to return MarkerKind.Apparatus.
    /// </summary>
    [Fact]
    public void GetMarkerKind_ApparatusKind_ReturnsApparatus()
    {
        var ann = new DocAnnotation(
            start: 0,
            endExclusive: 5,
            text: "Rdg: B [#wit.orig]",
            kind: "apparatus");

        var result = AnnotationMarkerInserter.GetMarkerKind(ann);

        Assert.Equal(AnnotationMarkerInserter.MarkerKind.Apparatus, result);
    }

    // ---------------------------------------------------------------
    // 5. Empty rdg (space quantity=0)
    // ---------------------------------------------------------------

    /// <summary>
    /// An app with a rdg containing only &lt;space quantity="0"/&gt; (i.e., empty
    /// reading) must still emit an apparatus annotation with "(empty)" for the reading.
    /// </summary>
    [Fact]
    public void Render_AppWithEmptyRdg_EmitsAnnotationWithEmpty()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg1\"/>A<anchor xml:id=\"end1\"/>" +
            "<app from=\"#beg1\" to=\"#end1\">" +
            "<lem>A</lem>" +
            "<rdg wit=\"#wit.orig\"><space quantity=\"0\"/></rdg>" +
            "</app>");

        var apparatusAnns = doc.Annotations
            .Where(a => a.Kind == "apparatus")
            .ToList();

        Assert.Single(apparatusAnns);
        Assert.Contains("(empty)", apparatusAnns[0].Text);
    }

    // ---------------------------------------------------------------
    // 6. No annotation when no rdg
    // ---------------------------------------------------------------

    /// <summary>
    /// An app element with only a lem and no rdg children must NOT emit
    /// an apparatus annotation, since there is no variant to report.
    /// </summary>
    [Fact]
    public void Render_AppWithLemOnly_NoApparatusAnnotation()
    {
        var doc = RenderSnippet(
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg1\"/>A<anchor xml:id=\"end1\"/>" +
            "<app from=\"#beg1\" to=\"#end1\">" +
            "<lem>A</lem>" +
            "</app>");

        var apparatusAnns = doc.Annotations
            .Where(a => a.Kind == "apparatus")
            .ToList();

        Assert.Empty(apparatusAnns);
    }
}
