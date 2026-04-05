using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

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

    private static int CountOccurrences(string text, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}