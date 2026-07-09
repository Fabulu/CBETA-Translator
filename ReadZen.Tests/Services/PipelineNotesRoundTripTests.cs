using System;
using System.Linq;
using System.Xml.Linq;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Round-trips the translation pipeline in NOTES mode
/// (BuildIndex -&gt; RenderProjection(Notes) -&gt; edit EN -&gt; ApplyProjectionEdits(Notes)
/// -&gt; BuildTranslatedXml). The existing IndexedTranslationServiceTests only round-trip
/// Body/Head; TranslationEditMode.Notes was never exercised end-to-end at the service
/// level. Confirms an edited note is written back AND sibling body/head content is left
/// untouched.
/// </summary>
public class PipelineNotesRoundTripTests
{
    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";

    // <back> notes are Notes-mode units; the <head>/<p> in <body> are the siblings that
    // must NOT move when only a note is edited.
    private const string Orig =
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<text>" +
        "<body><div><head>Heading</head><p>Body text</p></div></body>" +
        "<back><note>Original note</note></back>" +
        "</text></TEI>";

    private const string Tran =
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<text>" +
        "<body><div><head>Heading EN</head><p>Body text EN</p></div></body>" +
        "<back><note>Note EN</note></back>" +
        "</text></TEI>";

    [Fact]
    public void NotesProjection_SurfacesOnlyNoteUnits()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(Orig, Tran);

        var projection = svc.RenderProjection(doc, TranslationEditMode.Notes);

        Assert.Contains("# MODE: Notes", projection);
        Assert.Contains("ZH: Original note", projection);
        Assert.Contains("EN: Note EN", projection);
        // Body/head text is not part of the Notes projection surface.
        Assert.DoesNotContain("Body text", projection);
        Assert.DoesNotContain("Heading", projection);
    }

    [Fact]
    public void NotesRoundTrip_EditedNote_IsWrittenBack_AndBodyHeadUntouched()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(Orig, Tran);

        var projection = svc.RenderProjection(doc, TranslationEditMode.Notes)
            .Replace("EN: Note EN", "EN: Edited note EN", StringComparison.Ordinal);

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Notes, projection);
        var rebuilt = svc.BuildTranslatedXml(doc, out int updatedCount);

        Assert.True(updatedCount > 0, "the edited note group should have been patched");

        var parsed = XDocument.Parse(rebuilt, LoadOptions.PreserveWhitespace);

        // The note was updated.
        var note = parsed.Descendants(Tei + "note").Single();
        Assert.Equal("Edited note EN", note.Value);

        // Sibling body/head content is byte-identical to the pre-edit translated source.
        Assert.Equal("Heading EN", parsed.Descendants(Tei + "head").Single().Value);
        Assert.Equal("Body text EN", parsed.Descendants(Tei + "p").Single().Value);
    }

    [Fact]
    public void NotesRoundTrip_UneditedNote_LeavesTranslatedXmlEquivalent()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(Orig, Tran);

        // Render + apply with NO edit: nothing is dirty, so no group is patched.
        var projection = svc.RenderProjection(doc, TranslationEditMode.Notes);
        svc.ApplyProjectionEdits(doc, TranslationEditMode.Notes, projection);
        var rebuilt = svc.BuildTranslatedXml(doc, out int updatedCount);

        Assert.Equal(0, updatedCount);

        var parsed = XDocument.Parse(rebuilt, LoadOptions.PreserveWhitespace);
        Assert.Equal("Note EN", parsed.Descendants(Tei + "note").Single().Value);
        Assert.Equal("Heading EN", parsed.Descendants(Tei + "head").Single().Value);
        Assert.Equal("Body text EN", parsed.Descendants(Tei + "p").Single().Value);
    }
}
