using System.Linq;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Text;

public class TeiCompanionTranslationTests
{
    private const string TeiHeader = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TEI xmlns="http://www.tei-c.org/ns/1.0">
        <teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p/></publicationStmt><sourceDesc><p/></sourceDesc></fileDesc></teiHeader>
        <text><body>
        """;

    private const string TeiFooter = """
        </body></text>
        </TEI>
        """;

    private static string Wrap(string body) => TeiHeader + body + TeiFooter;

    [Fact]
    public void Render_EnglishTeiWithLElements_ProducesCorrectText()
    {
        var tei = Wrap("""<lg><l n="1">English line one</l><l n="2">English line two</l></lg>""");

        var doc = TeiRenderer.Render(tei);

        Assert.False(doc.IsEmpty);
        Assert.Contains("English line one", doc.Text);
        Assert.Contains("English line two", doc.Text);

        // Verify lines are separated by newlines (not on the same line)
        var lines = doc.Text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var idx1 = lines.FindIndex(l => l.Contains("English line one"));
        var idx2 = lines.FindIndex(l => l.Contains("English line two"));
        Assert.True(idx1 >= 0, "English line one should appear in output");
        Assert.True(idx2 > idx1, "English line two should be on a later line than English line one");
    }

    [Fact]
    public void Render_EnglishAndChineseTei_SegmentKeysMatch()
    {
        var chineseTei = Wrap("""
            <lg><l n="1">信心銘</l><l n="2">至道無難</l><l n="3">唯嫌揀擇</l></lg>
            """);

        var englishTei = Wrap("""
            <lg><l n="1">Faith in Mind</l><l n="2">The Great Way is not difficult</l><l n="3">just avoid picking and choosing</l></lg>
            """);

        var chineseDoc = TeiRenderer.Render(chineseTei);
        var englishDoc = TeiRenderer.Render(englishTei);

        var chineseKeys = chineseDoc.Segments.Select(s => s.Key).OrderBy(k => k).ToList();
        var englishKeys = englishDoc.Segments.Select(s => s.Key).OrderBy(k => k).ToList();

        // Both documents should produce the same segment keys for selection sync
        Assert.Equal(chineseKeys, englishKeys);
        Assert.Contains("l|1", chineseKeys);
        Assert.Contains("l|2", chineseKeys);
        Assert.Contains("l|3", chineseKeys);
    }

    [Fact]
    public void Render_OmissionJudgmentLine_RendersNormally()
    {
        var tei = Wrap("""<lg><l n="1" type="omission_judgment">This line has a special type attribute</l></lg>""");

        var doc = TeiRenderer.Render(tei);

        Assert.False(doc.IsEmpty);
        Assert.Contains("This line has a special type attribute", doc.Text);

        // Should still produce a segment for the line
        var segmentKeys = doc.Segments.Select(s => s.Key).ToList();
        Assert.Contains("l|1", segmentKeys);
    }
}
