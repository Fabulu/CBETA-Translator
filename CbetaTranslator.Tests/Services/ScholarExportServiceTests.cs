using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class ScholarExportServiceTests : IDisposable
{
    private readonly ScholarExportService _svc = new();
    private readonly string _tempDir;

    public ScholarExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "scholar-export-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private string TempFile(string name) => Path.Combine(_tempDir, name);

    private static ScholarCollection MakeSampleCollection(
        bool withLinks = false,
        int passageCount = 2)
    {
        var collection = new ScholarCollection
        {
            Id = "col1",
            Name = "Test Collection",
            Description = "A test description",
            Tags = new List<string> { "zen", "buddhism" },
            CreatedUtc = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            CreatedBy = "tester"
        };

        for (int i = 0; i < passageCount; i++)
        {
            collection.Passages.Add(new ScholarPassage
            {
                Id = $"p{i + 1}",
                SourceRelPath = $"xml-p5/T/T000{i + 1}.xml",
                ZhText = $"Chinese text {i + 1}",
                EnText = $"English text {i + 1}",
                Notes = i == 0 ? "Some notes here" : "",
                Tags = i == 0 ? new List<string> { "dharma" } : new List<string>(),
                MasterNames = i == 0 ? new List<string> { "Huineng" } : new List<string>(),
                AddedUtc = DateTimeOffset.UtcNow
            });
        }

        if (withLinks && passageCount >= 2)
        {
            collection.Links.Add(new PassageLink
            {
                Id = "link1",
                FromPassageId = "p1",
                ToPassageId = "p2",
                RelationType = "quotes",
                Note = "Direct quotation",
                CreatedUtc = DateTimeOffset.UtcNow
            });
        }

        return collection;
    }

    // ---- HTML export ----

    [Fact]
    public async Task HtmlExport_ProducesValidSelfContainedHtml()
    {
        var collection = MakeSampleCollection();
        var path = TempFile("export.html");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);

        var html = await File.ReadAllTextAsync(path);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("<meta charset=\"UTF-8\">", html);
        Assert.Contains("<style>", html);
        Assert.Contains("</style>", html);
        Assert.Contains("</html>", html);

        // Collection name is in the title and as h1
        Assert.Contains("<title>Test Collection</title>", html);
        Assert.Contains("<h1>Test Collection</h1>", html);

        // Description
        Assert.Contains("A test description", html);

        // Tags
        Assert.Contains("zen", html);
        Assert.Contains("buddhism", html);

        // Passages
        Assert.Contains("Passage 1", html);
        Assert.Contains("Passage 2", html);
        Assert.Contains("Chinese text 1", html);
        Assert.Contains("English text 1", html);

        // Source titles extracted from path
        Assert.Contains("T0001", html);
        Assert.Contains("T0002", html);

        // Passage-level tags and masters
        Assert.Contains("dharma", html);
        Assert.Contains("Huineng", html);

        // Notes
        Assert.Contains("Some notes here", html);

        // Self-contained: CSS is inline, no external references
        Assert.DoesNotContain("<link rel=\"stylesheet\"", html);
        Assert.DoesNotContain("<script src=", html);
    }

    [Fact]
    public async Task HtmlExport_EscapesHtmlSpecialChars()
    {
        var collection = new ScholarCollection
        {
            Id = "col1",
            Name = "<script>alert('xss')</script>",
            Description = "A & B < C > D",
            Passages = { new ScholarPassage
            {
                Id = "p1",
                ZhText = "text with <angle> & \"quotes\"",
                SourceRelPath = "test.xml",
                AddedUtc = DateTimeOffset.UtcNow
            }}
        };

        var path = TempFile("escape.html");
        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("A &amp; B &lt; C &gt; D", html);
        Assert.Contains("&lt;angle&gt; &amp; &quot;quotes&quot;", html);
        Assert.DoesNotContain("<script>alert", html);
    }

    // ---- Markdown export ----

    [Fact]
    public async Task MarkdownExport_HasCorrectFormat()
    {
        var collection = MakeSampleCollection();
        var path = TempFile("export.md");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Markdown);

        var md = await File.ReadAllTextAsync(path);

        // Title
        Assert.Contains("# Test Collection", md);

        // Description
        Assert.Contains("A test description", md);

        // Separators
        Assert.Contains("---", md);

        // Passage headings
        Assert.Contains("## Passage 1", md);
        Assert.Contains("## Passage 2", md);

        // Source lines
        Assert.Contains("**Source:** T0001", md);
        Assert.Contains("**Source:** T0002", md);

        // ZH text is quoted (blockquote)
        Assert.Contains("> Chinese text 1", md);

        // EN text is plain
        Assert.Contains("English text 1", md);

        // Tags and masters
        Assert.Contains("**Tags:** dharma", md);
        Assert.Contains("**Masters:** Huineng", md);

        // Notes
        Assert.Contains("**Notes:** Some notes here", md);
    }

    [Fact]
    public async Task MarkdownExport_MultilineZhQuoted()
    {
        var collection = new ScholarCollection
        {
            Id = "col1",
            Name = "Multi",
            Passages = { new ScholarPassage
            {
                Id = "p1",
                ZhText = "line one\nline two\nline three",
                SourceRelPath = "test.xml"
            }}
        };

        var path = TempFile("multiline.md");
        await _svc.ExportAsync(path, collection, ScholarExportFormat.Markdown);
        var md = await File.ReadAllTextAsync(path);

        Assert.Contains("> line one", md);
        Assert.Contains("> line two", md);
        Assert.Contains("> line three", md);
    }

    // ---- PlainText export ----

    [Fact]
    public async Task PlainTextExport_HasCorrectFormat()
    {
        var collection = MakeSampleCollection();
        var path = TempFile("export.txt");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.PlainText);

        var text = await File.ReadAllTextAsync(path);

        // Title with underline
        Assert.Contains("Test Collection", text);
        Assert.Contains("===============", text);

        // Description
        Assert.Contains("A test description", text);

        // Passages
        Assert.Contains("Passage 1", text);
        Assert.Contains("Source: T0001", text);
        Assert.Contains("Chinese text 1", text);
        Assert.Contains("English text 1", text);

        // Tags and masters
        Assert.Contains("Tags: dharma", text);
        Assert.Contains("Masters: Huineng", text);

        // Notes
        Assert.Contains("Notes: Some notes here", text);
    }

    // ---- Empty collection ----

    [Fact]
    public async Task HtmlExport_EmptyCollection_ProducesValidHtml()
    {
        var collection = new ScholarCollection
        {
            Id = "empty",
            Name = "Empty",
            Description = ""
        };

        var path = TempFile("empty.html");
        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<h1>Empty</h1>", html);
        Assert.Contains("</html>", html);
        // No passage cards
        Assert.DoesNotContain("Passage 1", html);
        // No links section
        Assert.DoesNotContain("Cross-References", html);
    }

    [Fact]
    public async Task MarkdownExport_EmptyCollection_HasHeaderOnly()
    {
        var collection = new ScholarCollection { Id = "e", Name = "Empty" };
        var path = TempFile("empty.md");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Markdown);
        var md = await File.ReadAllTextAsync(path);

        Assert.Contains("# Empty", md);
        Assert.DoesNotContain("## Passage", md);
    }

    [Fact]
    public async Task PlainTextExport_EmptyCollection_HasHeaderOnly()
    {
        var collection = new ScholarCollection { Id = "e", Name = "Empty" };
        var path = TempFile("empty.txt");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.PlainText);
        var text = await File.ReadAllTextAsync(path);

        Assert.Contains("Empty", text);
        Assert.DoesNotContain("Passage 1", text);
    }

    // ---- Collection with links ----

    [Fact]
    public async Task HtmlExport_WithLinks_IncludesCrossReferencesSection()
    {
        var collection = MakeSampleCollection(withLinks: true);
        var path = TempFile("links.html");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        // Cross-references section
        Assert.Contains("Cross-References", html);

        // SVG graph (<=20 passages triggers SVG)
        Assert.Contains("<svg", html);
        Assert.Contains("</svg>", html);

        // Link relation type
        Assert.Contains("quotes", html);

        // Link note
        Assert.Contains("Direct quotation", html);

        // Anchor links to passages
        Assert.Contains("passage-p1", html);
        Assert.Contains("passage-p2", html);
    }

    [Fact]
    public async Task HtmlExport_WithLinks_SvgHasEdgesAndNodes()
    {
        var collection = MakeSampleCollection(withLinks: true);
        var path = TempFile("svg.html");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        // SVG line elements (edges)
        Assert.Contains("<line", html);
        Assert.Contains("stroke=", html);

        // SVG circle elements (nodes)
        Assert.Contains("<circle", html);

        // SVG text elements (labels)
        Assert.Contains("<text", html);

        // Legend
        Assert.Contains("quotes", html);
    }

    [Fact]
    public async Task HtmlExport_ManyPassages_UsesTableInsteadOfSvg()
    {
        // >20 passages should trigger table instead of SVG
        var collection = MakeSampleCollection(withLinks: false, passageCount: 22);
        // Add a link
        collection.Links.Add(new PassageLink
        {
            Id = "link1",
            FromPassageId = "p1",
            ToPassageId = "p2",
            RelationType = "parallels",
            CreatedUtc = DateTimeOffset.UtcNow
        });

        var path = TempFile("table.html");
        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        Assert.Contains("<table", html);
        Assert.Contains("links-table", html);
        Assert.DoesNotContain("<svg", html);
    }

    [Fact]
    public async Task MarkdownExport_WithLinks_IncludesCrossReferences()
    {
        var collection = MakeSampleCollection(withLinks: true);
        var path = TempFile("links.md");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Markdown);
        var md = await File.ReadAllTextAsync(path);

        Assert.Contains("## Cross-References", md);
        Assert.Contains("**quotes**", md);
        Assert.Contains("Direct quotation", md);
    }

    [Fact]
    public async Task PlainTextExport_WithLinks_IncludesCrossReferences()
    {
        var collection = MakeSampleCollection(withLinks: true);
        var path = TempFile("links.txt");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.PlainText);
        var text = await File.ReadAllTextAsync(path);

        Assert.Contains("Cross-References", text);
        Assert.Contains("quotes", text);
        Assert.Contains("Direct quotation", text);
    }

    // ---- SVG edge cases ----

    [Fact]
    public async Task HtmlExport_LinksWithInvalidPassageIds_SkipsOrphanEdges()
    {
        var collection = MakeSampleCollection(withLinks: false);
        collection.Links.Add(new PassageLink
        {
            Id = "orphan",
            FromPassageId = "nonexistent1",
            ToPassageId = "nonexistent2",
            RelationType = "quotes",
            CreatedUtc = DateTimeOffset.UtcNow
        });

        var path = TempFile("orphan.html");
        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        // The SVG should be generated but the orphan link should not produce a <line>
        // because the IDs don't match any passage
        Assert.Contains("<svg", html);
        Assert.DoesNotContain("<line", html);
    }

    [Fact]
    public async Task HtmlExport_SvgLegendContainsAllRelationTypes()
    {
        var collection = MakeSampleCollection(withLinks: true);
        var path = TempFile("legend.html");

        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);
        var html = await File.ReadAllTextAsync(path);

        // All relation types should appear in the legend
        Assert.Contains("quotes", html);
        Assert.Contains("alludes-to", html);
        Assert.Contains("comments-on", html);
        Assert.Contains("contradicts", html);
        Assert.Contains("parallels", html);
        Assert.Contains("responds-to", html);
    }

    // ---- Invalid format ----

    [Fact]
    public async Task ExportAsync_InvalidFormat_Throws()
    {
        var collection = MakeSampleCollection();
        var path = TempFile("invalid.txt");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _svc.ExportAsync(path, collection, (ScholarExportFormat)99));
    }

    // ---- File output ----

    [Fact]
    public async Task ExportAsync_WritesUtf8File()
    {
        var collection = new ScholarCollection
        {
            Id = "utf8",
            Name = "UTF-8 Test",
            Passages = { new ScholarPassage
            {
                Id = "p1",
                ZhText = "\u4f5b\u6cd5\u50e7",  // Buddhist terms in CJK
                SourceRelPath = "test.xml"
            }}
        };

        var path = TempFile("utf8.html");
        await _svc.ExportAsync(path, collection, ScholarExportFormat.Html);

        var bytes = await File.ReadAllBytesAsync(path);
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("\u4f5b\u6cd5\u50e7", text);
    }
}
