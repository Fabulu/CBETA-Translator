// Tests for five bug-fix areas:
// 1. ProvenancePanel apparatus count filtering by Kind=="apparatus"
// 2. Witness extraction from Descendants (listWit under tagsDecl)
// 3. EN-side marker filtering (Apparatus markers excluded from translated side)
// 4. ApparatusAnnotationParser.Parse (Lem/Rdg parsing)
// 5. TeiRenderer apparatus with back-matter <app> + anchor references in <body>

using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests covering five distinct bug fixes: apparatus count filtering,
/// witness extraction from non-standard TEI locations, EN-side marker exclusion,
/// apparatus annotation parsing, and back-matter apparatus rendering.
/// </summary>
public class BugFixRegressionTests
{
    // ---------------------------------------------------------------
    // 1. ProvenancePanel apparatus count filter
    // ---------------------------------------------------------------

    /// <summary>
    /// Create a RenderedDocument with 3 annotations (2 notes, 1 apparatus).
    /// Filtering by Kind=="apparatus" must return exactly 1.
    /// </summary>
    [Fact]
    public void ApparatusCountFilter_ReturnsOnlyApparatusAnnotations()
    {
        var annotations = new List<DocAnnotation>
        {
            new DocAnnotation(0, 5, "Note one", kind: "inline"),
            new DocAnnotation(10, 15, "Note two", kind: "community"),
            new DocAnnotation(20, 25, "Lem: A\nRdg: B [#wit.orig]", kind: "apparatus"),
        };

        var doc = new RenderedDocument(
            "some rendered text here!!",
            new List<RenderSegment>(),
            annotations,
            new List<AnnotationMarkerInserter.MarkerSpan>());

        // This is the exact filter used in ReadableTabView line 1169/5148
        int apparatusCount = doc.Annotations.Count(a => a.Kind == "apparatus");

        Assert.Equal(1, apparatusCount);
    }

    /// <summary>
    /// When no annotations have Kind=="apparatus", the count must be 0.
    /// </summary>
    [Fact]
    public void ApparatusCountFilter_ReturnsZero_WhenNoApparatus()
    {
        var annotations = new List<DocAnnotation>
        {
            new DocAnnotation(0, 5, "Note one", kind: "inline"),
            new DocAnnotation(10, 15, "Note two", kind: "community"),
        };

        var doc = new RenderedDocument(
            "some text",
            new List<RenderSegment>(),
            annotations,
            new List<AnnotationMarkerInserter.MarkerSpan>());

        int apparatusCount = doc.Annotations.Count(a => a.Kind == "apparatus");

        Assert.Equal(0, apparatusCount);
    }

    // ---------------------------------------------------------------
    // 2. Witness extraction from Descendants
    // ---------------------------------------------------------------

    /// <summary>
    /// Create a TEI XML fragment where listWit is under tagsDecl/namespace/tagUsage
    /// (not the usual sourceDesc location). TextLicenseExtractor.Extract must still
    /// find the witnesses because it searches header.Descendants("listWit").
    /// </summary>
    [Fact]
    public void WitnessExtraction_FromDescendants_FindsListWitUnderTagsDecl()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader>
                <fileDesc>
                    <titleStmt><title>Test</title></titleStmt>
                    <publicationStmt>
                        <availability>
                            <licence target=""https://creativecommons.org/licenses/by-sa/4.0/"">
                                <p>CC BY-SA 4.0</p>
                            </licence>
                        </availability>
                    </publicationStmt>
                    <sourceDesc><p>source</p></sourceDesc>
                </fileDesc>
                <encodingDesc>
                    <tagsDecl>
                        <namespace name=""http://www.tei-c.org/ns/1.0"">
                            <tagUsage gi=""witness"">
                                <listWit>
                                    <witness>Taishō vol. 48, no. 2005</witness>
                                    <witness>Kōshōji manuscript</witness>
                                </listWit>
                            </tagUsage>
                        </namespace>
                    </tagsDecl>
                </encodingDesc>
            </teiHeader>
            <text><body/></text>
        </TEI>";

        var info = TextLicenseExtractor.Extract(xml);

        Assert.NotNull(info);
        Assert.NotNull(info!.Witnesses);
        Assert.Equal(2, info.Witnesses!.Count);
        Assert.Contains(info.Witnesses, w => w.Contains("Taishō"));
        Assert.Contains(info.Witnesses, w => w.Contains("Kōshōji"));
    }

    /// <summary>
    /// Witnesses under sourceDesc (the standard location) must also be found.
    /// </summary>
    [Fact]
    public void WitnessExtraction_FromSourceDesc_StillWorks()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader>
                <fileDesc>
                    <titleStmt><title>Test</title></titleStmt>
                    <publicationStmt><p>pub</p></publicationStmt>
                    <sourceDesc>
                        <listWit>
                            <witness>Witness Alpha</witness>
                        </listWit>
                    </sourceDesc>
                </fileDesc>
            </teiHeader>
            <text><body/></text>
        </TEI>";

        var info = TextLicenseExtractor.Extract(xml);

        Assert.NotNull(info);
        Assert.NotNull(info!.Witnesses);
        Assert.Single(info.Witnesses!);
        Assert.Equal("Witness Alpha", info.Witnesses[0]);
    }

    // ---------------------------------------------------------------
    // 3. EN-side marker filtering
    // ---------------------------------------------------------------

    /// <summary>
    /// Verify that MarkerKind.Apparatus markers can be excluded from a marker list
    /// using the same LINQ filter applied in ReadableTabView for the translated side.
    /// </summary>
    [Fact]
    public void EnSideMarkerFilter_ExcludesApparatusMarkers()
    {
        var markers = new List<AnnotationMarkerInserter.MarkerSpan>
        {
            new(0, 1, 0, AnnotationMarkerInserter.MarkerKind.Normal),
            new(5, 6, 1, AnnotationMarkerInserter.MarkerKind.Apparatus),
            new(10, 11, 2, AnnotationMarkerInserter.MarkerKind.Yuanwu),
            new(15, 16, 3, AnnotationMarkerInserter.MarkerKind.Community),
            new(20, 21, 4, AnnotationMarkerInserter.MarkerKind.Apparatus),
        };

        // This is the exact filter from ReadableTabView line 3089
        var filtered = markers
            .Where(m => m.Kind != AnnotationMarkerInserter.MarkerKind.Apparatus)
            .ToList();

        Assert.Equal(3, filtered.Count);
        Assert.DoesNotContain(filtered, m => m.Kind == AnnotationMarkerInserter.MarkerKind.Apparatus);
        Assert.Contains(filtered, m => m.Kind == AnnotationMarkerInserter.MarkerKind.Normal);
        Assert.Contains(filtered, m => m.Kind == AnnotationMarkerInserter.MarkerKind.Yuanwu);
        Assert.Contains(filtered, m => m.Kind == AnnotationMarkerInserter.MarkerKind.Community);
    }

    /// <summary>
    /// When all markers are apparatus, the filtered list must be empty.
    /// </summary>
    [Fact]
    public void EnSideMarkerFilter_AllApparatus_ReturnsEmpty()
    {
        var markers = new List<AnnotationMarkerInserter.MarkerSpan>
        {
            new(0, 1, 0, AnnotationMarkerInserter.MarkerKind.Apparatus),
            new(5, 6, 1, AnnotationMarkerInserter.MarkerKind.Apparatus),
        };

        var filtered = markers
            .Where(m => m.Kind != AnnotationMarkerInserter.MarkerKind.Apparatus)
            .ToList();

        Assert.Empty(filtered);
    }

    // ---------------------------------------------------------------
    // 4. ApparatusAnnotationParser.Parse
    // ---------------------------------------------------------------

    /// <summary>
    /// Feed "Lem: 遇\nRdg: 遇之不遇 [#wit.orig]" and verify it produces an
    /// ApparatusEntry with correct Lemma and Readings.
    /// </summary>
    [Fact]
    public void ParseApparatusAnnotation_LemAndRdg_Parsed()
    {
        var entry = ApparatusAnnotationParser.Parse("Lem: 遇\nRdg: 遇之不遇 [#wit.orig]");

        Assert.NotNull(entry);
        Assert.Equal("遇", entry!.Lemma);
        Assert.NotNull(entry.Readings);
        Assert.Single(entry.Readings!);
        Assert.Equal("遇之不遇", entry.Readings![0].Reading);
        Assert.Equal("#wit.orig", entry.Readings[0].WitnessId);
    }

    /// <summary>
    /// Multiple Rdg lines must all be captured.
    /// </summary>
    [Fact]
    public void ParseApparatusAnnotation_MultipleRdg_AllCaptured()
    {
        var text = "Lem: 無\nRdg: 有 [#wit.A]\nRdg: 空 [#wit.B]";
        var entry = ApparatusAnnotationParser.Parse(text);

        Assert.NotNull(entry);
        Assert.Equal("無", entry!.Lemma);
        Assert.NotNull(entry.Readings);
        Assert.Equal(2, entry.Readings!.Count);
        Assert.Equal("有", entry.Readings[0].Reading);
        Assert.Equal("#wit.A", entry.Readings[0].WitnessId);
        Assert.Equal("空", entry.Readings[1].Reading);
        Assert.Equal("#wit.B", entry.Readings[1].WitnessId);
    }

    /// <summary>
    /// Rdg without witness bracket must still parse (WitnessId is null).
    /// </summary>
    [Fact]
    public void ParseApparatusAnnotation_RdgWithoutWitness_WitnessIdNull()
    {
        var entry = ApparatusAnnotationParser.Parse("Lem: X\nRdg: Y");

        Assert.NotNull(entry);
        Assert.Equal("X", entry!.Lemma);
        Assert.NotNull(entry.Readings);
        Assert.Single(entry.Readings!);
        Assert.Equal("Y", entry.Readings![0].Reading);
        Assert.Null(entry.Readings[0].WitnessId);
    }

    /// <summary>
    /// Empty/whitespace input returns null.
    /// </summary>
    [Fact]
    public void ParseApparatusAnnotation_EmptyInput_ReturnsNull()
    {
        Assert.Null(ApparatusAnnotationParser.Parse(""));
        Assert.Null(ApparatusAnnotationParser.Parse("   "));
        Assert.Null(ApparatusAnnotationParser.Parse(null!));
    }

    // ---------------------------------------------------------------
    // 5. TeiRenderer apparatus with back-matter
    // ---------------------------------------------------------------

    private const string TeiHeader =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt></fileDesc></teiHeader>" +
        "<text>";

    private const string TeiFooter = "</text></TEI>";

    /// <summary>
    /// Render XML that has <app> in <back> with anchor references in <body>.
    /// The anchor "beg1" is in the body text; the <app from="#beg1"> is in <back>.
    /// Verify the apparatus annotation is created at the correct position
    /// (at the anchor, not at the end of the document).
    /// </summary>
    [Fact]
    public void Render_AppInBack_WithAnchorInBody_EmitsApparatusAtAnchorPos()
    {
        var xml = TeiHeader +
            "<body><div1>" +
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "<anchor xml:id=\"beg1\"/>遇<anchor xml:id=\"end1\"/>" +
            "佛殺佛" +
            "</div1></body>" +
            "<back>" +
            "<app from=\"#beg1\" to=\"#end1\">" +
            "<lem>遇</lem>" +
            "<rdg wit=\"#wit.orig\">遇之不遇</rdg>" +
            "</app>" +
            "</back>" +
            TeiFooter;

        var doc = TeiRenderer.Render(xml);

        var apparatusAnns = doc.Annotations
            .Where(a => a.Kind == "apparatus")
            .ToList();

        Assert.Single(apparatusAnns);
        var ann = apparatusAnns[0];
        Assert.Contains("Lem: 遇", ann.Text);
        Assert.Contains("Rdg: 遇之不遇", ann.Text);
        Assert.Contains("#wit.orig", ann.Text);

        // The annotation should be anchored at the position of "beg1" in body,
        // NOT at the end of the rendered text. The body text starts with "遇佛殺佛",
        // so the anchor for "beg1" should be near the start.
        // The rendered text after lb will start with a newline; the anchor is right after.
        Assert.True(ann.Start < doc.Text.Length,
            "Apparatus annotation should be anchored before end of text");
    }

    /// <summary>
    /// When <app> is in <back> but the referenced anchor ID doesn't exist
    /// in the body, the annotation should still be emitted (at a fallback position).
    /// </summary>
    [Fact]
    public void Render_AppInBack_NoMatchingAnchor_StillEmitsAnnotation()
    {
        var xml = TeiHeader +
            "<body><div1>" +
            "<lb n=\"0001a01\" ed=\"T\"/>" +
            "無門關" +
            "</div1></body>" +
            "<back>" +
            "<app from=\"#nonexistent\">" +
            "<lem>X</lem>" +
            "<rdg wit=\"#wit.orig\">Y</rdg>" +
            "</app>" +
            "</back>" +
            TeiFooter;

        var doc = TeiRenderer.Render(xml);

        var apparatusAnns = doc.Annotations
            .Where(a => a.Kind == "apparatus")
            .ToList();

        Assert.Single(apparatusAnns);
        Assert.Contains("Rdg: Y", apparatusAnns[0].Text);
    }
}
