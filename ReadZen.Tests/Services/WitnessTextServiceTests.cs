using System;
using System.IO;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class WitnessTextServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _xmlPath;

    public WitnessTextServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-witness-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _xmlPath = Path.Combine(_tempDir, "sample.xml");
        File.WriteAllText(_xmlPath, "<TEI/>");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void TryLoad_ReturnsNull_WhenFileDoesNotExist()
    {
        var svc = new WitnessTextService();
        var result = svc.TryLoad(_xmlPath);
        Assert.Null(result);
    }

    [Fact]
    public void TryLoad_LoadsWitnessesJson_WhenPresent()
    {
        var json = """
{
  "text_id": "test.edition",
  "witnesses": [
    {
      "witness_id": "T1",
      "siglum": "T1",
      "label": "Test witness 1",
      "role": "primary",
      "text_status": "corrected_working",
      "completeness": "complete",
      "confidence": "high",
      "alignment_mode": "direct_locus",
      "alignment_statuses_supported": ["present", "omitted", "lacuna"],
      "has_locus_map": false
    }
  ]
}
""";
        File.WriteAllText(Path.Combine(_tempDir, "witnesses.json"), json);

        var svc = new WitnessTextService();
        var result = svc.TryLoad(_xmlPath);

        Assert.NotNull(result);
        Assert.Equal("test.edition", result!.TextId);
        Assert.NotNull(result.Witnesses);
        Assert.Single(result.Witnesses!);
        var w = result.Witnesses![0];
        Assert.Equal("T1", w.WitnessId);
        Assert.Equal("T1", w.Siglum);
        Assert.Equal("corrected_working", w.TextStatus);
        Assert.Equal("Working (corrected)", w.StatusDisplay);
        Assert.NotNull(w.AlignmentStatusesSupported);
        Assert.Contains("present", w.AlignmentStatusesSupported!);
        Assert.Contains("omitted", w.AlignmentStatusesSupported!);
        Assert.Contains("lacuna", w.AlignmentStatusesSupported!);
    }

    [Fact]
    public void TryLoad_FallsBackToWitnessTextsJson_WhenNewNameAbsent()
    {
        // Legacy: only the old "witness-texts.json" filename exists
        var json = """
{
  "text_id": "legacy.edition",
  "witnesses": [{"witness_id":"L1","siglum":"L"}]
}
""";
        File.WriteAllText(Path.Combine(_tempDir, "witness-texts.json"), json);

        var svc = new WitnessTextService();
        var result = svc.TryLoad(_xmlPath);

        Assert.NotNull(result);
        Assert.Equal("legacy.edition", result!.TextId);
        Assert.Single(result.Witnesses!);
    }

    [Fact]
    public void TryLoad_Caches_AcrossCalls()
    {
        var json = """{"text_id":"cached","witnesses":[]}""";
        var jsonPath = Path.Combine(_tempDir, "witnesses.json");
        File.WriteAllText(jsonPath, json);

        var svc = new WitnessTextService();
        var r1 = svc.TryLoad(_xmlPath);
        var r2 = svc.TryLoad(_xmlPath);

        Assert.NotNull(r1);
        Assert.Same(r1, r2); // cached instance returned
    }

    [Fact]
    public void GetComparisonAtLocus_SortsDifferingReadingsFirst()
    {
        var registry = new WitnessTextRegistry
        {
            TextId = "test",
            Witnesses = new()
            {
                new WitnessTextEntry
                {
                    WitnessId = "T1",
                    Siglum = "T1",
                    Readings = new() { ["loc1"] = "ALICE_READING" },
                },
                new WitnessTextEntry
                {
                    WitnessId = "T2",
                    Siglum = "T2",
                    Readings = new() { ["loc1"] = "BOB_READING" },
                },
                new WitnessTextEntry
                {
                    WitnessId = "T3",
                    Siglum = "T3",
                    Readings = new() { ["loc1"] = "ALICE_READING" }, // agrees with T1 = lemma
                },
            },
        };

        var groups = WitnessTextService.GetComparisonAtLocus(
            registry, apparatus: null, locusId: "loc1", lemma: "ALICE_READING");

        Assert.NotNull(groups);
        Assert.Equal(2, groups!.Count);
        // Differing reading should be first (not lemma)
        Assert.False(groups[0].IsLemma);
        Assert.Equal("BOB_READING", groups[0].Reading);
        // Lemma/adopted reading second
        Assert.True(groups[1].IsLemma);
        Assert.Equal("ALICE_READING", groups[1].Reading);
    }
}
