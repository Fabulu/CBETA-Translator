using System;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Pins the display&lt;-&gt;XML offset arithmetic on <see cref="RenderedDocument"/>
/// (DisplayIndexToBaseIndex / DisplayIndexToXmlIndex / TryFindRenderedOffsetByXmlIndex),
/// which had ZERO direct coverage despite underpinning linked selection and translation
/// write-back placement. The class comments call out a "one line too high" fencepost
/// hazard; these tests exercise it directly via a real TeiRenderer render containing
/// &lt;lb/&gt;, &lt;pb/&gt;, and a suppressed &lt;app&gt; apparatus run.
/// </summary>
public class RenderedDocumentOffsetTests
{
    // <lb/> + suppressed <app> apparatus (LemText/RdgText render to nothing but still
    // consume XML index space) + <pb/>. Distinct ASCII tokens so IndexOf never collides
    // with the Unicode superscript annotation markers the renderer inserts.
    private const string Fixture =
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<text><body><p>" +
        "<lb n=\"1\"/>Alpha" +
        "<app from=\"#beg1\"><lem>LemText</lem><rdg wit=\"W1\">RdgText</rdg></app>" +
        "Beta" +
        "<pb n=\"2\"/>Gamma" +
        "</p></body></text></TEI>";

    [Fact]
    public void SuppressedApparatusText_IsNotRenderedButConsumesXmlIndexSpace()
    {
        var doc = TeiRenderer.Render(Fixture);

        // The apparatus lemma/reading text must never leak into the reader surface.
        Assert.DoesNotContain("LemText", doc.Text);
        Assert.DoesNotContain("RdgText", doc.Text);
        Assert.Contains("Alpha", doc.Text);
        Assert.Contains("Beta", doc.Text);
        Assert.Contains("Gamma", doc.Text);

        // Renderer contract: position map, one entry per caret slot.
        Assert.NotNull(doc.BaseToXmlIndex);
        Assert.NotNull(doc.BaseTextLength);
    }

    [Fact]
    public void DisplayIndexToXmlIndex_JustAfterSuppressedApparatus_LandsOnBetaNotInsideApp()
    {
        var doc = TeiRenderer.Render(Fixture);
        int betaDisplay = doc.Text.IndexOf("Beta", StringComparison.Ordinal);
        Assert.True(betaDisplay >= 0, "fixture must render the token 'Beta'");

        int xmlBeta = doc.DisplayIndexToXmlIndex(betaDisplay);

        // The caret at the start of "Beta" must map into the source 'Beta' node — i.e.
        // PAST the </app> close, never into the suppressed apparatus content. A drift of
        // "one line too high" would land inside the <app> run (LemText/RdgText).
        Assert.True(xmlBeta > Fixture.IndexOf("</app>", StringComparison.Ordinal),
            $"expected xmlBeta past </app>, got {xmlBeta}");
        Assert.StartsWith("Beta", Fixture.Substring(xmlBeta), StringComparison.Ordinal);
    }

    [Fact]
    public void TryFindRenderedOffsetByXmlIndex_IsInverseOf_DisplayIndexToXmlIndex_AtBeta()
    {
        var doc = TeiRenderer.Render(Fixture);
        int betaDisplay = doc.Text.IndexOf("Beta", StringComparison.Ordinal);

        int xmlBeta = doc.DisplayIndexToXmlIndex(betaDisplay);

        Assert.True(doc.TryFindRenderedOffsetByXmlIndex(xmlBeta, out int renderedOffset),
            "inverse lookup must succeed for a valid XML index");
        Assert.StartsWith("Beta", doc.Text.Substring(renderedOffset), StringComparison.Ordinal);
        Assert.Equal(betaDisplay, renderedOffset);
    }

    [Fact]
    public void DisplayIndexToXmlIndex_AfterPageBreak_LandsOnGamma()
    {
        var doc = TeiRenderer.Render(Fixture);
        int gammaDisplay = doc.Text.IndexOf("Gamma", StringComparison.Ordinal);
        Assert.True(gammaDisplay >= 0, "fixture must render the token 'Gamma'");

        int xmlGamma = doc.DisplayIndexToXmlIndex(gammaDisplay);

        Assert.True(xmlGamma > Fixture.IndexOf("<pb", StringComparison.Ordinal),
            $"expected xmlGamma past <pb/>, got {xmlGamma}");
        Assert.StartsWith("Gamma", Fixture.Substring(xmlGamma), StringComparison.Ordinal);

        // Inverse round-trips back to the same rendered caret.
        Assert.True(doc.TryFindRenderedOffsetByXmlIndex(xmlGamma, out int back));
        Assert.Equal(gammaDisplay, back);
    }

    [Fact]
    public void DisplayIndexToXmlIndex_OnSegmentBoundary_MapsToTheBoundaryTagNotOneLineTooHigh()
    {
        var doc = TeiRenderer.Render(Fixture);

        // The <pb n="2"/> starts its own segment (sync key "pb|...") at the caret right
        // after "Beta", before the inserted paragraph break. Mapping that boundary must
        // land exactly on the <pb tag in source — the canonical "one line too high"
        // fencepost: a stale-by-one map would resolve to inside "Beta" instead.
        var pbSeg = doc.Segments.First(s => s.Key.StartsWith("pb", StringComparison.Ordinal));

        int xmlAtBoundary = doc.DisplayIndexToXmlIndex(pbSeg.Start);
        Assert.StartsWith("<pb", Fixture.Substring(xmlAtBoundary), StringComparison.Ordinal);
    }

    [Fact]
    public void DisplayIndexToBaseIndex_WithNoMarkersInDocument_IsIdentity()
    {
        // A doc with no annotations => no inserted markers => display == base.
        const string plain =
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Hello World</p></body></text></TEI>";
        var doc = TeiRenderer.Render(plain);
        Assert.Empty(doc.AnnotationMarkers);

        int idx = doc.Text.IndexOf("World", StringComparison.Ordinal);
        Assert.Equal(idx, doc.DisplayIndexToBaseIndex(idx));
    }
}
