using System;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class IndexedTranslationServiceTests
{
    [Fact]
    public void RenderProjection_Body_SuppressesCbMuluDuplicatesButKeepsHead()
    {
        var svc = new IndexedTranslationService();
        const string orig = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"" xmlns:cb=""http://www.cbeta.org/ns/1.0""><text><body><cb:div type=""other""><cb:mulu level=""1"" type=""å…¶ä»–"">Gate Title</cb:mulu><head>Gate Title</head><p>Body text</p></cb:div></body></text></TEI>";
        const string tran = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"" xmlns:cb=""http://www.cbeta.org/ns/1.0""><text><body><cb:div type=""other""><cb:mulu level=""1"" type=""å…¶ä»–"">Gate Title</cb:mulu><head>Gate Title EN</head><p>Body text EN</p></cb:div></body></text></TEI>";

        var doc = svc.BuildIndex(orig, tran);
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body);

        Assert.Equal(1, CountOccurrences(projection, "Gate Title EN"));
        Assert.DoesNotContain("Gate Title\nGate Title EN", projection);
        Assert.Contains("Body text EN", projection);
    }

    [Fact]
    public void ApplyProjectionEdits_PreservesIntentionalLeadingSpacesAfterEnPrefix()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(SimpleOrigXml(), SimpleTranXml());
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body)
            .Replace("EN: Body text EN", "EN:   Indented translation", StringComparison.Ordinal);

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, projection);

        var bodyUnit = Assert.Single(doc.Units, u => string.Equals(u.Kind.ToString(), "Body", StringComparison.Ordinal));
        Assert.Equal("  Indented translation", bodyUnit.En);
    }

    [Fact]
    public void ApplyProjectionEdits_MultilineEnglishInSingleBlock_ThrowsClearError()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(SimpleOrigXml(), SimpleTranXml());
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body)
            .Replace("EN: Body text EN", "EN: First line\ncontinued line", StringComparison.Ordinal);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, projection));

        Assert.Contains("Multiline EN is not supported", ex.Message);
    }

    [Fact]
    public void BuildTranslatedXml_PreservesOriginalCrlfLineEndings()
    {
        var svc = new IndexedTranslationService();
        const string orig = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Body<lb/>Tail</p></body></text></TEI>";
        const string tran = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Body EN<lb/>Tail EN</p></body></text></TEI>";
        var doc = svc.BuildIndex(orig, tran.Replace("<lb/>", "\r\n<lb/>", StringComparison.Ordinal));
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body)
            .Replace("EN: Body EN", "EN: Updated EN", StringComparison.Ordinal);

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, projection);
        var rebuilt = svc.BuildTranslatedXml(doc, out _);

        Assert.Contains("\r\n<lb", rebuilt);
        Assert.DoesNotContain("\n<lb", rebuilt.Replace("\r\n<lb", string.Empty, StringComparison.Ordinal));
    }

    private static string SimpleOrigXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Body text</p></body></text></TEI>";

    private static string SimpleTranXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Body text EN</p></body></text></TEI>";

    private static int CountOccurrences(string text, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
