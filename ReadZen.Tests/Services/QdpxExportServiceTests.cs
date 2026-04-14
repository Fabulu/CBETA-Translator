using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for QdpxExportService: ZIP contains project.qde + Sources, XML is valid, codebook hierarchy.
/// </summary>
public class QdpxExportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public QdpxExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-qdpx-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private static TagVocabulary MakeVocab(params (string id, string name)[] defs)
    {
        var v = new TagVocabulary();
        foreach (var (id, name) in defs)
            v.Tags.Add(new TagDefinition { Id = id, Name = name, Color = "#3498DB", CreatedUtc = DateTimeOffset.UtcNow });
        return v;
    }

    private static Task<string?> SimpleDocLoader(string relPath, CancellationToken ct)
    {
        return Task.FromResult<string?>($"Sample text for {relPath}.\nLine 2.\nLine 3.");
    }

    [Fact]
    public async Task Export_CreatesValidZip()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow },
            new() { Id = "2", RelPath = "file2.xml", TagId = "t2", FromLb = "c", ToLb = "d", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        var outputPath = Path.Combine(_tempDir, "test.qdpx");
        await QdpxExportService.ExportAsync(outputPath, tags, vocab, SimpleDocLoader);

        Assert.True(File.Exists(outputPath));

        // Verify it's a valid ZIP
        using var zip = ZipFile.OpenRead(outputPath);
        Assert.True(zip.Entries.Count > 0);
    }

    [Fact]
    public async Task Export_ContainsProjectQde()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var vocab = MakeVocab(("t1", "Theme"));

        var outputPath = Path.Combine(_tempDir, "test.qdpx");
        await QdpxExportService.ExportAsync(outputPath, tags, vocab, SimpleDocLoader);

        using var zip = ZipFile.OpenRead(outputPath);
        var qdeEntry = zip.GetEntry("project.qde");
        Assert.NotNull(qdeEntry);
    }

    [Fact]
    public async Task Export_ContainsSources()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow },
            new() { Id = "2", RelPath = "file2.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var vocab = MakeVocab(("t1", "Theme"));

        var outputPath = Path.Combine(_tempDir, "test.qdpx");
        await QdpxExportService.ExportAsync(outputPath, tags, vocab, SimpleDocLoader);

        using var zip = ZipFile.OpenRead(outputPath);
        var sourceEntries = zip.Entries.Where(e => e.FullName.StartsWith("Sources/")).ToList();
        Assert.Equal(2, sourceEntries.Count);

        // Each source should be a .txt file
        Assert.All(sourceEntries, e => Assert.EndsWith(".txt", e.FullName));
    }

    [Fact]
    public async Task Export_XmlIsValid()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var vocab = MakeVocab(("t1", "Theme"));

        var outputPath = Path.Combine(_tempDir, "test.qdpx");
        await QdpxExportService.ExportAsync(outputPath, tags, vocab, SimpleDocLoader);

        using var zip = ZipFile.OpenRead(outputPath);
        var qdeEntry = zip.GetEntry("project.qde");
        Assert.NotNull(qdeEntry);

        using var stream = qdeEntry!.Open();
        using var reader = new StreamReader(stream);
        var xmlContent = await reader.ReadToEndAsync();

        // Should be valid XML
        var xdoc = XDocument.Parse(xmlContent);
        Assert.NotNull(xdoc.Root);
    }

    [Fact]
    public async Task Export_CodeBookContainsAllCodes()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow },
            new() { Id = "2", RelPath = "file1.xml", TagId = "t2", FromLb = "c", ToLb = "d", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        var outputPath = Path.Combine(_tempDir, "test.qdpx");
        await QdpxExportService.ExportAsync(outputPath, tags, vocab, SimpleDocLoader);

        using var zip = ZipFile.OpenRead(outputPath);
        var qdeEntry = zip.GetEntry("project.qde");
        using var stream = qdeEntry!.Open();
        using var reader = new StreamReader(stream);
        var xmlContent = await reader.ReadToEndAsync();

        Assert.Contains("Theme", xmlContent);
        Assert.Contains("Metaphor", xmlContent);
    }

    [Fact]
    public async Task Export_EmptyTags_CreatesMinimalArchive()
    {
        var outputPath = Path.Combine(_tempDir, "empty.qdpx");
        await QdpxExportService.ExportAsync(
            outputPath, new List<DocumentTag>(), new TagVocabulary(), SimpleDocLoader);

        Assert.True(File.Exists(outputPath));

        using var zip = ZipFile.OpenRead(outputPath);
        var qdeEntry = zip.GetEntry("project.qde");
        Assert.NotNull(qdeEntry);

        // No sources (no tags -> no RelPaths)
        var sourceEntries = zip.Entries.Where(e => e.FullName.StartsWith("Sources/")).ToList();
        Assert.Empty(sourceEntries);
    }

    [Fact]
    public async Task Export_SourceTextMatchesLoader()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "myfile.xml", TagId = "t1", FromLb = "a", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var vocab = MakeVocab(("t1", "Theme"));

        var outputPath = Path.Combine(_tempDir, "test.qdpx");
        await QdpxExportService.ExportAsync(outputPath, tags, vocab, SimpleDocLoader);

        using var zip = ZipFile.OpenRead(outputPath);
        var sourceEntry = zip.Entries.First(e => e.FullName.StartsWith("Sources/"));
        using var stream = sourceEntry.Open();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        Assert.Contains("Sample text for myfile.xml", text);
    }

    [Fact]
    public void FindLbRange_FindsMarkers()
    {
        var text = "Some text [p0001a01] here and [p0005a01] there.";
        var (start, end) = QdpxExportService.FindLbRange(text, "p0001a01", "p0005a01");

        Assert.True(start >= 0);
        Assert.True(end > start);
    }

    [Fact]
    public void FindLbRange_NotFound_ReturnsNegative()
    {
        var text = "No markers here.";
        var (start, end) = QdpxExportService.FindLbRange(text, "p0099a01", "p0100a01");

        Assert.Equal(-1, start);
    }
}
