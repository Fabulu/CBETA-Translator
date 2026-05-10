using System.Linq;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Text;

public class TeiRendererLTests
{
    private const string MiniTei = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TEI xmlns="http://www.tei-c.org/ns/1.0">
        <teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p/></publicationStmt><sourceDesc><p/></sourceDesc></fileDesc></teiHeader>
        <text><body>
        <lg><l n="1">first line</l><l n="2">second line</l><l n="3">third line</l></lg>
        </body></text>
        </TEI>
        """;

    [Fact]
    public void Render_LElements_ProducesLineBreaks()
    {
        var doc = TeiRenderer.Render(MiniTei);

        Assert.False(doc.IsEmpty);
        // Each <l> should produce a newline before its content (except possibly the first)
        // so "first line", "second line", "third line" should be on separate lines
        Assert.Contains("first line", doc.Text);
        Assert.Contains("second line", doc.Text);
        Assert.Contains("third line", doc.Text);

        // Verify the lines are separated by newlines
        var lines = doc.Text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        Assert.Contains(lines, l => l.Contains("first line"));
        Assert.Contains(lines, l => l.Contains("second line"));
        Assert.Contains(lines, l => l.Contains("third line"));

        // "second line" and "third line" must not be on the same line as "first line"
        var firstIdx = lines.FindIndex(l => l.Contains("first line"));
        var secondIdx = lines.FindIndex(l => l.Contains("second line"));
        var thirdIdx = lines.FindIndex(l => l.Contains("third line"));
        Assert.True(secondIdx > firstIdx, "second line should be on a later line than first line");
        Assert.True(thirdIdx > secondIdx, "third line should be on a later line than second line");
    }

    [Fact]
    public void Render_LElements_CreatesSegments()
    {
        var doc = TeiRenderer.Render(MiniTei);

        var segmentKeys = doc.Segments.Select(s => s.Key).ToList();
        Assert.Contains("l|1", segmentKeys);
        Assert.Contains("l|2", segmentKeys);
        Assert.Contains("l|3", segmentKeys);
    }

    [Fact]
    public void Render_LWithoutN_NoSegment()
    {
        var tei = """
            <?xml version="1.0" encoding="UTF-8"?>
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
            <teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p/></publicationStmt><sourceDesc><p/></sourceDesc></fileDesc></teiHeader>
            <text><body>
            <lg><l>no attribute line</l></lg>
            </body></text>
            </TEI>
            """;

        var doc = TeiRenderer.Render(tei);

        // Text should still render
        Assert.Contains("no attribute line", doc.Text);
        // But no segment with key "l|..." should exist since there's no n attribute
        Assert.DoesNotContain(doc.Segments, s => s.Key.StartsWith("l|"));
    }

    [Fact]
    public void Render_LgWithL_ParagraphBreaksAndLineBreaks()
    {
        var tei = """
            <?xml version="1.0" encoding="UTF-8"?>
            <TEI xmlns="http://www.tei-c.org/ns/1.0">
            <teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p/></publicationStmt><sourceDesc><p/></sourceDesc></fileDesc></teiHeader>
            <text><body>
            <p xml:id="p1">Prose paragraph.</p>
            <lg><l n="1">verse one</l><l n="2">verse two</l></lg>
            <p xml:id="p2">Another paragraph.</p>
            </body></text>
            </TEI>
            """;

        var doc = TeiRenderer.Render(tei);

        Assert.Contains("Prose paragraph.", doc.Text);
        Assert.Contains("verse one", doc.Text);
        Assert.Contains("verse two", doc.Text);
        Assert.Contains("Another paragraph.", doc.Text);

        // The lg block should be separated from surrounding paragraphs by paragraph breaks
        // (double newlines), while l elements within lg use single newlines
        var proseEnd = doc.Text.IndexOf("Prose paragraph.") + "Prose paragraph.".Length;
        var verseStart = doc.Text.IndexOf("verse one");
        var betweenProseAndVerse = doc.Text[proseEnd..verseStart];

        // Should have at least two newlines (paragraph break) between prose and verse
        var newlineCount = betweenProseAndVerse.Count(c => c == '\n');
        Assert.True(newlineCount >= 2, $"Expected paragraph break (>=2 newlines) between <p> and <lg>, got {newlineCount}");
    }
}
