using System;
using System.IO;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class AnchorServicePathTests : IDisposable
{
    private readonly string _tempDir;

    public AnchorServicePathTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-anchor-path-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private static readonly string EventJsonl = """
        {"event_id":"EVT-P01","locus_id":"loc.v1","change_type":"variant_reading"}
        """;

    private static readonly string EventJsonlAlt = """
        {"event_id":"EVT-ALT","locus_id":"loc.v9","change_type":"addition"}
        """;

    /// <summary>
    /// Mimics xml-open/ce/test-edition/sample.xml with provenance at
    /// provenance/test-edition/process/anchor-event-log.jsonl
    /// (three levels up from the edition directory).
    /// </summary>
    [Fact]
    public void TryLoadEvents_FindsFileInProvenancePath()
    {
        // Build directory tree: root/xml-open/ce/test-edition/
        var editionDir = Path.Combine(_tempDir, "xml-open", "ce", "test-edition");
        Directory.CreateDirectory(editionDir);
        var xmlPath = Path.Combine(editionDir, "sample.xml");
        File.WriteAllText(xmlPath, "<TEI/>");

        // Put event log in provenance path: root/provenance/test-edition/process/
        var provenanceDir = Path.Combine(_tempDir, "provenance", "test-edition", "process");
        Directory.CreateDirectory(provenanceDir);
        File.WriteAllText(Path.Combine(provenanceDir, "anchor-event-log.jsonl"), EventJsonl);

        var svc = new AnchorService();
        var events = svc.TryLoadEvents(xmlPath);

        Assert.NotNull(events);
        Assert.Single(events!);
        Assert.Equal("EVT-P01", events[0].EventId);
    }

    [Fact]
    public void TryLoadEvents_FindsFileInSameDirectory()
    {
        var dir = Path.Combine(_tempDir, "local-test");
        Directory.CreateDirectory(dir);
        var xmlPath = Path.Combine(dir, "sample.xml");
        File.WriteAllText(xmlPath, "<TEI/>");
        File.WriteAllText(Path.Combine(dir, "anchor-event-log.jsonl"), EventJsonl);

        var svc = new AnchorService();
        var events = svc.TryLoadEvents(xmlPath);

        Assert.NotNull(events);
        Assert.Single(events!);
        Assert.Equal("EVT-P01", events[0].EventId);
    }

    [Fact]
    public void TryLoadEvents_PrefersLocalOverProvenance()
    {
        // Build directory tree: root/xml-open/ce/test-edition/
        var editionDir = Path.Combine(_tempDir, "xml-open", "ce", "test-edition");
        Directory.CreateDirectory(editionDir);
        var xmlPath = Path.Combine(editionDir, "sample.xml");
        File.WriteAllText(xmlPath, "<TEI/>");

        // Put DIFFERENT event logs at both locations
        // Local (same directory) — checked first by AnchorService
        File.WriteAllText(Path.Combine(editionDir, "anchor-event-log.jsonl"), EventJsonl);

        // Provenance path
        var provenanceDir = Path.Combine(_tempDir, "provenance", "test-edition", "process");
        Directory.CreateDirectory(provenanceDir);
        File.WriteAllText(Path.Combine(provenanceDir, "anchor-event-log.jsonl"), EventJsonlAlt);

        var svc = new AnchorService();
        var events = svc.TryLoadEvents(xmlPath);

        Assert.NotNull(events);
        Assert.Single(events!);
        // Local file is checked first, so we should get EVT-P01 (not EVT-ALT)
        Assert.Equal("EVT-P01", events[0].EventId);
    }
}
