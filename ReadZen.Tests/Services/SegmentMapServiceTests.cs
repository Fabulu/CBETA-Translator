// SegmentMapServiceTests — exercises the segment-map JSONL loading pipeline:
// path resolution, JSONL parsing, mtime caching, graceful malformed handling,
// lb-ID to SegmentInfo mapping, and multi-lb-range entries.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

[Trait("Domain", "Segmentation")]
public class SegmentMapServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _jsonlPath;

    public SegmentMapServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-segmap-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _jsonlPath = Path.Combine(_tempDir, "test.segments.jsonl");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private void WriteJsonl(params string[] lines)
    {
        File.WriteAllText(_jsonlPath, string.Join("\n", lines));
    }

    private static string MakeLine(string unitId, string[] lbRange, string type, double confidence = 0.94,
        string? subType = null, string? speaker = null)
    {
        var obj = new Dictionary<string, object?>
        {
            ["unit_id"] = unitId,
            ["lb_range"] = lbRange,
            ["text_zh"] = "test",
            ["type"] = type,
            ["confidence"] = confidence
        };
        if (subType != null) obj["sub_type"] = subType;
        if (speaker != null) obj["speaker"] = speaker;
        return JsonSerializer.Serialize(obj);
    }

    [Fact]
    public void TryLoad_ReturnsNull_WhenNoJsonlExists()
    {
        var service = new SegmentMapService();
        // Pass a path to a nonexistent XML file — no JSONL can possibly exist
        var result = service.TryLoad(Path.Combine(_tempDir, "nonexistent.xml"));
        Assert.Null(result);
    }

    [Fact]
    public void TryLoad_LoadsSegments_FromValidJsonl()
    {
        WriteJsonl(
            MakeLine("seg-0001", new[] { "0526b24", "0526b25" }, "prose"),
            MakeLine("seg-0002", new[] { "0526c01" }, "verse", 0.95),
            MakeLine("seg-0003", new[] { "0526c02", "0526c03" }, "dialogue", speaker: "洞山")
        );

        var map = SegmentMapService.ParseJsonl(_jsonlPath);

        Assert.NotNull(map);
        Assert.Equal(3, map!.Segments.Count);
        Assert.Equal("seg-0001", map.Segments[0].UnitId);
        Assert.Equal("prose", map.Segments[0].Type);
        Assert.Equal("verse", map.Segments[1].Type);
        Assert.Equal("dialogue", map.Segments[2].Type);
        Assert.Equal("洞山", map.Segments[2].Speaker);
    }

    [Fact]
    public void TryLoad_ReturnsCachedInstance_UntilJsonlMtimeChanges()
    {
        // Build a repo layout that AppPaths discovery + ResolveJsonlPath recognize:
        //   <parent>/orig/xml-p5/T/T47/T47n1987A.xml        (source XML)
        //   <parent>/trans/xml-p5t/                          (marks the translations repo)
        //   <parent>/trans/segments/T/T47/T47n1987A.segments.jsonl
        var xmlDir = Path.Combine(_tempDir, "orig", "xml-p5", "T", "T47");
        Directory.CreateDirectory(xmlDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "trans", "xml-p5t"));
        var segDir = Path.Combine(_tempDir, "trans", "segments", "T", "T47");
        Directory.CreateDirectory(segDir);

        var xmlPath = Path.Combine(xmlDir, "T47n1987A.xml");
        File.WriteAllText(xmlPath, "<TEI><body><p>hi</p></body></TEI>");

        var segPath = Path.Combine(segDir, "T47n1987A.segments.jsonl");
        File.WriteAllText(segPath, MakeLine("seg-0001", new[] { "0001a01" }, "prose"));
        AppPaths.InvalidateDiscoveryCache(_tempDir);

        var service = new SegmentMapService();

        var first = service.TryLoad(xmlPath);
        Assert.NotNull(first);
        Assert.Single(first!.Segments);

        // Second call with no filesystem change must return the SAME cached
        // instance — this is the mtime cache actually short-circuiting a re-parse.
        var second = service.TryLoad(xmlPath);
        Assert.Same(first, second);

        // Rewrite the jsonl with different content and bump its mtime. The cache
        // key includes the jsonl mtime, so TryLoad must re-parse and reflect the
        // new content rather than serving the stale cached instance.
        File.WriteAllText(segPath, string.Join("\n",
            MakeLine("seg-0001", new[] { "0001a01" }, "prose"),
            MakeLine("seg-0002", new[] { "0001a02" }, "verse")));
        File.SetLastWriteTimeUtc(segPath, DateTime.UtcNow.AddMinutes(1));

        var third = service.TryLoad(xmlPath);
        Assert.NotNull(third);
        Assert.NotSame(first, third);           // cache was invalidated by the mtime bump
        Assert.Equal(2, third!.Segments.Count);  // and the fresh content is reflected
    }

    [Fact]
    public void TryLoad_GracefulOnMalformedJsonl()
    {
        WriteJsonl(
            MakeLine("seg-0001", new[] { "0001a01" }, "verse"),
            "this is not valid json {{{",
            "",
            MakeLine("seg-0003", new[] { "0001a03" }, "commentary")
        );

        var map = SegmentMapService.ParseJsonl(_jsonlPath);

        Assert.NotNull(map);
        // Malformed line is skipped; we get 2 valid segments
        Assert.Equal(2, map!.Segments.Count);
        Assert.Equal("verse", map.Segments[0].Type);
        Assert.Equal("commentary", map.Segments[1].Type);
    }

    [Fact]
    public void TryLoad_MapsLbIdToSegmentInfo()
    {
        WriteJsonl(
            MakeLine("seg-0001", new[] { "0526b24" }, "prose"),
            MakeLine("seg-0002", new[] { "0526c01" }, "verse")
        );

        var map = SegmentMapService.ParseJsonl(_jsonlPath);

        Assert.NotNull(map);
        Assert.True(map!.ByLbId.ContainsKey("0526b24"));
        Assert.True(map.ByLbId.ContainsKey("0526c01"));
        Assert.Equal("prose", map.ByLbId["0526b24"].Type);
        Assert.Equal("verse", map.ByLbId["0526c01"].Type);
    }

    // ---------------------------------------------------------------
    // P3.1b — source-hash staleness contract
    // ---------------------------------------------------------------

    [Fact]
    public void ParseJsonl_SkipsMetaHeader_AndCapturesSourceHash()
    {
        WriteJsonl(
            "{\"source_sha256\":\"abc123\",\"schema\":\"seg-v1\"}",
            MakeLine("seg-0001", new[] { "0001a01" }, "prose"),
            MakeLine("seg-0002", new[] { "0001a02" }, "verse")
        );

        var map = SegmentMapService.ParseJsonl(_jsonlPath);

        Assert.NotNull(map);
        // The header is NOT a segment.
        Assert.Equal(2, map!.Segments.Count);
        Assert.Equal("abc123", map.SourceSha256);
    }

    [Fact]
    public void ParseJsonl_WithoutHeader_LeavesSourceHashNull()
    {
        WriteJsonl(
            MakeLine("seg-0001", new[] { "0001a01" }, "prose")
        );

        var map = SegmentMapService.ParseJsonl(_jsonlPath);

        Assert.NotNull(map);
        Assert.Single(map!.Segments);
        Assert.Null(map.SourceSha256);
    }

    [Fact]
    public void TryComputeSourceHash_MatchesGeneratorParity_AndIsLineEndingIndependent()
    {
        // Anchors pinned identically in eng/tools/test-structural-segments.js.
        var xmlLf = Path.Combine(_tempDir, "src-lf.xml");
        var xmlCrLf = Path.Combine(_tempDir, "src-crlf.xml");
        File.WriteAllText(xmlLf, "<TEI><body><p>hi</p></body></TEI>");
        Assert.Equal(
            "730c6fa790830ef1efbd219963d42c66be2aa49f8ba93a8f45430784b754068e",
            SegmentMapService.TryComputeSourceHash(xmlLf));

        File.WriteAllText(xmlLf, "<TEI>\n<body><p>hi</p></body>\n</TEI>");
        File.WriteAllText(xmlCrLf, "<TEI>\r\n<body><p>hi</p></body>\r\n</TEI>");
        var hLf = SegmentMapService.TryComputeSourceHash(xmlLf);
        var hCrLf = SegmentMapService.TryComputeSourceHash(xmlCrLf);
        Assert.Equal(hLf, hCrLf);
        Assert.Equal("eb0b16976a228903ae3643e755320fa35c6b65f9b6c35ae7c2c5e582c37ec097", hLf);
    }

    [Fact]
    public void IsMapStale_TrueWhenSourceHashMismatches()
    {
        var xmlPath = Path.Combine(_tempDir, "source.xml");
        File.WriteAllText(xmlPath, "<TEI><body><p>current</p></body></TEI>");

        // Map claims to have been built from DIFFERENT source content.
        WriteJsonl(
            "{\"source_sha256\":\"0000000000000000000000000000000000000000000000000000000000000000\"}",
            MakeLine("seg-0001", new[] { "0001a01" }, "prose")
        );
        var map = SegmentMapService.ParseJsonl(_jsonlPath)!;

        Assert.True(SegmentMapService.IsMapStale(map, xmlPath), "mismatched source hash must be stale");
    }

    [Fact]
    public void IsMapStale_FalseWhenSourceHashMatches()
    {
        var xmlPath = Path.Combine(_tempDir, "source.xml");
        var content = "<TEI><body><p>hi</p></body></TEI>";
        File.WriteAllText(xmlPath, content);

        WriteJsonl(
            "{\"source_sha256\":\"730c6fa790830ef1efbd219963d42c66be2aa49f8ba93a8f45430784b754068e\"}",
            MakeLine("seg-0001", new[] { "0001a01" }, "prose")
        );
        var map = SegmentMapService.ParseJsonl(_jsonlPath)!;

        Assert.False(SegmentMapService.IsMapStale(map, xmlPath), "matching hash must not be stale");
    }

    [Fact]
    public void IsMapStale_FalseWhenNoEmbeddedHash()
    {
        var xmlPath = Path.Combine(_tempDir, "source.xml");
        File.WriteAllText(xmlPath, "<TEI><body><p>anything</p></body></TEI>");

        WriteJsonl(
            MakeLine("seg-0001", new[] { "0001a01" }, "prose")  // no header line
        );
        var map = SegmentMapService.ParseJsonl(_jsonlPath)!;

        // Backward compat: maps without an embedded hash are never stale.
        Assert.False(SegmentMapService.IsMapStale(map, xmlPath));
    }

    [Fact]
    public void TryLoad_HandlesMultipleLbRangeEntries()
    {
        // A single segment spanning 4 lb lines
        WriteJsonl(
            MakeLine("seg-0001", new[] { "0526c04", "0526c05", "0526c06", "0526c07" }, "dialogue",
                subType: "dharma_dialogue", speaker: "師曰")
        );

        var map = SegmentMapService.ParseJsonl(_jsonlPath);

        Assert.NotNull(map);
        Assert.Single(map!.Segments);

        // All 4 lb-IDs should map to the same segment
        var seg = map.Segments[0];
        Assert.Equal("dialogue", seg.Type);
        Assert.Equal("dharma_dialogue", seg.SubType);
        Assert.Equal("師曰", seg.Speaker);

        foreach (var lbId in new[] { "0526c04", "0526c05", "0526c06", "0526c07" })
        {
            Assert.True(map.ByLbId.ContainsKey(lbId), $"Expected lb-ID '{lbId}' in map");
            Assert.Same(seg, map.ByLbId[lbId]);
        }
    }
}
