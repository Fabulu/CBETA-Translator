using System;
using System.Linq;
using System.Xml.Linq;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class IndexedTranslationServiceTests
{
    [Fact]
    public void RenderProjection_Body_SuppressesCbMuluDuplicatesButKeepsHead()
    {
        var svc = new IndexedTranslationService();
        const string orig = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"" xmlns:cb=""http://www.cbeta.org/ns/1.0""><text><body><cb:div type=""other""><cb:mulu level=""1"" type=""其他"">Gate Title</cb:mulu><head>Gate Title</head><p>Body text</p></cb:div></body></text></TEI>";
        const string tran = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"" xmlns:cb=""http://www.cbeta.org/ns/1.0""><text><body><cb:div type=""other""><cb:mulu level=""1"" type=""其他"">Gate Title</cb:mulu><head>Gate Title EN</head><p>Body text EN</p></cb:div></body></text></TEI>";

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

    [Fact]
    public void BuildTranslatedXml_MultiBlockBodyEdits_RemainsWellFormedAndKeepsStructure()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(MultiBlockOrigXml(), MultiBlockTranXml());
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body)
            .Replace("EN: Body EN", "EN: Updated first body line", StringComparison.Ordinal)
            .Replace("EN: Tail EN", "EN: Updated second body line", StringComparison.Ordinal)
            .Replace("EN: Closing EN", "EN: Updated closing line", StringComparison.Ordinal);

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, projection);
        var rebuilt = svc.BuildTranslatedXml(doc, out var updatedCount);

        var parsed = XDocument.Parse(rebuilt, LoadOptions.PreserveWhitespace);
        XNamespace ns = "http://www.tei-c.org/ns/1.0";

        Assert.True(updatedCount > 0);
        Assert.NotNull(parsed.Root);
        Assert.Contains("Updated first body line", rebuilt);
        Assert.Contains("Updated second body line", rebuilt);
        Assert.Contains("Updated closing line", rebuilt);
        Assert.Contains("<lb", rebuilt);
        Assert.Equal("Translated Title", parsed.Descendants(ns + "head").Single().Value);
    }

    [Fact]
    public void BuildTranslatedXml_BlankEnglishPreservesOriginalChineseForThatLine()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(SimpleOrigXml(), SimpleTranXml());
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body)
            .Replace("EN: Body text EN", "EN: ", StringComparison.Ordinal);

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, projection);
        var rebuilt = svc.BuildTranslatedXml(doc, out _);

        var parsed = XDocument.Parse(rebuilt, LoadOptions.PreserveWhitespace);
        XNamespace ns = "http://www.tei-c.org/ns/1.0";
        Assert.Equal("Body text", parsed.Descendants(ns + "p").Single().Value);
    }

    [Fact]
    public void BuildTranslatedXml_HeaderEdit_DoesNotTouchBodyContent()
    {
        var svc = new IndexedTranslationService();
        var doc = svc.BuildIndex(HeaderOrigXml(), HeaderTranXml());
        var headProjection = System.Text.RegularExpressions.Regex.Replace(
            svc.RenderProjection(doc, TranslationEditMode.Head),
            @"EN:\s?[^\r\n]*",
            "EN: Updated Header Title",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Head, headProjection);
        var rebuilt = svc.BuildTranslatedXml(doc, out _);

        var parsed = XDocument.Parse(rebuilt, LoadOptions.PreserveWhitespace);
        XNamespace ns = "http://www.tei-c.org/ns/1.0";
        Assert.Equal("Updated Header Title", parsed.Descendants(ns + "title").First().Value);
        Assert.Contains("Body EN", parsed.Descendants(ns + "p").First().Value);
        Assert.Contains("Closing EN", parsed.Descendants(ns + "p").Skip(1).First().Value);
    }
    private static string SimpleOrigXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Body text</p></body></text></TEI>";

    private static string SimpleTranXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>Body text EN</p></body></text></TEI>";

    private static string MultiBlockOrigXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><div><head>Original Title</head><p>Body<lb/>Tail</p><p>Closing</p></div></body></text></TEI>";

    private static string MultiBlockTranXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><div><head>Translated Title</head><p>Body EN<lb/>Tail EN</p><p>Closing EN</p></div></body></text></TEI>";

    private static string HeaderOrigXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><teiHeader><fileDesc><titleStmt><title>Original Header Title</title></titleStmt></fileDesc></teiHeader><text><body><div><head>Original Title</head><p>Body<lb/>Tail</p><p>Closing</p></div></body></text></TEI>";

    private static string HeaderTranXml()
        => "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><teiHeader><fileDesc><titleStmt><title>Translated Header Title</title></titleStmt></fileDesc></teiHeader><text><body><div><head>Translated Title</head><p>Body EN<lb/>Tail EN</p><p>Closing EN</p></div></body></text></TEI>";
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



