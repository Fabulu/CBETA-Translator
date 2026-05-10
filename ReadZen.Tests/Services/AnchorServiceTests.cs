using System;
using System.IO;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class AnchorServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _xmlPath;

    public AnchorServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-anchor-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _xmlPath = Path.Combine(_tempDir, "sample.xml");
        File.WriteAllText(_xmlPath, "<TEI/>");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void TryLoadEvents_ReturnsNull_WhenFileDoesNotExist()
    {
        var svc = new AnchorService();
        var result = svc.TryLoadEvents(_xmlPath);
        Assert.Null(result);
    }

    [Fact]
    public void TryLoadEvents_ParsesValidJsonl_ReturnsEvents()
    {
        var jsonl = """
        {"event_id":"EVT-001","event_date":"2025-12-01","locus_id":"loc.v3","witness_id":"W-DZ","before_text":"心","after_text":"信","change_type":"variant_reading","status":"accepted","confidence":"high"}
        {"event_id":"EVT-002","event_date":"2025-12-02","locus_id":"loc.v5","witness_id":"W-TK","before_text":"","after_text":"道","change_type":"addition","status":"pending","confidence":"medium"}
        {"event_id":"EVT-003","event_date":"2025-12-03","locus_id":"loc.v3","witness_id":"W-JL","before_text":"心","after_text":"信心","change_type":"expansion","status":"accepted","confidence":"high"}
        """;
        File.WriteAllText(Path.Combine(_tempDir, "anchor-event-log.jsonl"), jsonl);

        var svc = new AnchorService();
        var events = svc.TryLoadEvents(_xmlPath);

        Assert.NotNull(events);
        Assert.Equal(3, events!.Count);

        var evt1 = events.First(e => e.EventId == "EVT-001");
        Assert.Equal("2025-12-01", evt1.EventDate);
        Assert.Equal("loc.v3", evt1.LocusId);
        Assert.Equal("W-DZ", evt1.WitnessId);
        Assert.Equal("心", evt1.BeforeText);
        Assert.Equal("信", evt1.AfterText);
        Assert.Equal("variant_reading", evt1.ChangeType);
        Assert.Equal("variant reading", evt1.ChangeTypeDisplay);
        Assert.Equal("accepted", evt1.Status);
        Assert.Equal("high", evt1.Confidence);
    }

    [Fact]
    public void TryLoadEvents_SkipsEmptyLines()
    {
        var jsonl = """
        {"event_id":"EVT-010","locus_id":"loc.v1","change_type":"variant_reading"}

        {"event_id":"EVT-011","locus_id":"loc.v2","change_type":"addition"}

        """;
        File.WriteAllText(Path.Combine(_tempDir, "anchor-event-log.jsonl"), jsonl);

        var svc = new AnchorService();
        var events = svc.TryLoadEvents(_xmlPath);

        Assert.NotNull(events);
        Assert.Equal(2, events!.Count);
    }

    [Fact]
    public void TryLoadEvents_CachesByMtime()
    {
        var jsonl = """
        {"event_id":"EVT-020","locus_id":"loc.v1","change_type":"variant_reading"}
        """;
        File.WriteAllText(Path.Combine(_tempDir, "anchor-event-log.jsonl"), jsonl);

        var svc = new AnchorService();
        var r1 = svc.TryLoadEvents(_xmlPath);
        var r2 = svc.TryLoadEvents(_xmlPath);

        Assert.NotNull(r1);
        Assert.Same(r1, r2); // cached instance returned
    }

    [Fact]
    public void TryLoadBases_ParsesValidJsonl_ReturnsBases()
    {
        var jsonl = """
        {"anchor_id":"ANC-001","witness_id":"W-DZ","page_id":"p001","locus_id":"loc.v3","source_kind":"woodblock","page_bbox":[0,0,100,200],"locus_bbox":[10,20,50,60]}
        {"anchor_id":"ANC-002","witness_id":"W-TK","page_id":"p002","locus_id":"loc.v5","source_kind":"manuscript","page_bbox":[0,0,120,250],"locus_bbox":[15,25,55,65]}
        """;
        File.WriteAllText(Path.Combine(_tempDir, "anchor-base-register.jsonl"), jsonl);

        var svc = new AnchorService();
        var bases = svc.TryLoadBases(_xmlPath);

        Assert.NotNull(bases);
        Assert.Equal(2, bases!.Count);

        var b1 = bases[0];
        Assert.Equal("ANC-001", b1.AnchorId);
        Assert.Equal("W-DZ", b1.WitnessId);
        Assert.Equal("p001", b1.PageId);
        Assert.Equal("loc.v3", b1.LocusId);
        Assert.Equal("woodblock", b1.SourceKind);
        Assert.NotNull(b1.PageBbox);
        Assert.Equal(4, b1.PageBbox!.Length);
        Assert.NotNull(b1.LocusBbox);
        Assert.Equal(4, b1.LocusBbox!.Length);
    }

    [Fact]
    public void GetAnchorById_FindsMatchingAnchor()
    {
        var bases = new System.Collections.Generic.List<AnchorBase>
        {
            new() { AnchorId = "ANC-001", WitnessId = "W-DZ", LocusId = "loc.v3" },
            new() { AnchorId = "ANC-002", WitnessId = "W-TK", LocusId = "loc.v5" },
            new() { AnchorId = "ANC-003", WitnessId = "W-JL", LocusId = "loc.v3" },
        };

        var svc = new AnchorService();
        var found = svc.GetAnchorById(bases, "ANC-002");

        Assert.NotNull(found);
        Assert.Equal("ANC-002", found!.AnchorId);
        Assert.Equal("W-TK", found.WitnessId);
    }

    [Fact]
    public void GetAnchorById_ReturnsNull_WhenNotFound()
    {
        var bases = new System.Collections.Generic.List<AnchorBase>
        {
            new() { AnchorId = "ANC-001", WitnessId = "W-DZ" },
        };

        var svc = new AnchorService();
        var found = svc.GetAnchorById(bases, "ANC-999");

        Assert.Null(found);
    }

    [Fact]
    public void GetEventsForLocus_FiltersCorrectly()
    {
        var events = new System.Collections.Generic.List<AnchorEvent>
        {
            new() { EventId = "EVT-001", LocusId = "loc.v3", ChangeType = "variant_reading" },
            new() { EventId = "EVT-002", LocusId = "loc.v5", ChangeType = "addition" },
            new() { EventId = "EVT-003", LocusId = "loc.v3", ChangeType = "expansion" },
            new() { EventId = "EVT-004", LocusId = "loc.v7", ChangeType = "deletion" },
        };

        var svc = new AnchorService();
        var filtered = svc.GetEventsForLocus(events, "loc.v3");

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, e => Assert.Equal("loc.v3", e.LocusId));
        Assert.Contains(filtered, e => e.EventId == "EVT-001");
        Assert.Contains(filtered, e => e.EventId == "EVT-003");
    }
}
