using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for classical apparatus components: StemmaParserService,
/// ApparatusReading deserialization, and StemmaViewModel.Build.
/// </summary>
public class ClassicalApparatusTests
{
    // ── StemmaParserService: arrow format ──────────────────────────────

    [Fact]
    public void ParseArrowFormat_ExtractsEdgesAndNodes()
    {
        var lines = new[] { "# header", "archetype -> alpha", "alpha -> T1", "alpha -> T2" };
        // TryParseFile requires a real file; test the public API via a temp file.
        var tmp = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllLines(tmp, lines);
            var data = StemmaParserService.TryParseFile(tmp);
            Assert.NotNull(data);
            Assert.Equal(3, data!.Edges.Count);
            Assert.Contains(("archetype", "alpha"), data.Edges);
            Assert.Contains(("alpha", "T1"), data.Edges);
            Assert.Equal(4, data.NodeNames.Count);
        }
        finally { System.IO.File.Delete(tmp); }
    }

    [Fact]
    public void ParseFile_MissingFile_ReturnsNull()
    {
        var result = StemmaParserService.TryParseFile("/nonexistent/path/stemma.md");
        Assert.Null(result);
    }

    [Fact]
    public void ParseFile_EmptyFile_ReturnsNull()
    {
        var tmp = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(tmp, "");
            var result = StemmaParserService.TryParseFile(tmp);
            Assert.Null(result);
        }
        finally { System.IO.File.Delete(tmp); }
    }

    // ── StemmaParserService: GenerateFromRegistry ──────────────────────

    [Fact]
    public void GenerateFromRegistry_GroupsByFamily()
    {
        var registry = new WitnessTextRegistry
        {
            Witnesses = new List<WitnessTextEntry>
            {
                new() { WitnessId = "w1", Siglum = "T1", FamilyId = "fam-A" },
                new() { WitnessId = "w2", Siglum = "T2", FamilyId = "fam-A" },
                new() { WitnessId = "w3", Siglum = "K1", FamilyId = "fam-B" },
            }
        };

        var data = StemmaParserService.GenerateFromRegistry(registry);

        Assert.NotNull(data);
        // archetype -> fam-A, archetype -> fam-B, fam-A -> T1, fam-A -> T2, fam-B -> K1
        Assert.Equal(5, data!.Edges.Count);
        Assert.Contains(("archetype", "fam-A"), data.Edges);
        Assert.Contains(("fam-A", "T1"), data.Edges);
        Assert.Contains(("fam-B", "K1"), data.Edges);
    }

    [Fact]
    public void GenerateFromRegistry_NullOrTooFew_ReturnsNull()
    {
        Assert.Null(StemmaParserService.GenerateFromRegistry(null));
        var single = new WitnessTextRegistry
        {
            Witnesses = new List<WitnessTextEntry> { new() { Siglum = "T1" } }
        };
        Assert.Null(StemmaParserService.GenerateFromRegistry(single));
    }

    // ── ApparatusReading: Type + Editor deserialization ────────────────

    [Fact]
    public void ApparatusReading_DeserializesTypeAndEditor()
    {
        var json = """{"witness_id":"T1","reading":"無雅","type":"subst","editor":"Iriya"}""";
        var reading = JsonSerializer.Deserialize<ApparatusReading>(json);

        Assert.NotNull(reading);
        Assert.Equal("subst", reading!.Type);
        Assert.Equal("Iriya", reading.Editor);
        Assert.Equal("T1", reading.WitnessId);
    }

    [Fact]
    public void ApparatusReading_MissingOptionalFields_DefaultsNull()
    {
        var json = """{"witness_id":"K1","reading":"無難"}""";
        var reading = JsonSerializer.Deserialize<ApparatusReading>(json);

        Assert.NotNull(reading);
        Assert.Null(reading!.Type);
        Assert.Null(reading.Editor);
        Assert.Null(reading.Certainty);
    }

    // ── StemmaViewModel.Build ──────────────────────────────────────────

    [Fact]
    public void Build_CreatesCorrectNodeAndEdgeCount()
    {
        var stemma = new StemmaParserService.StemmaData();
        stemma.Edges.Add(("archetype", "alpha"));
        stemma.Edges.Add(("alpha", "T1"));
        stemma.Edges.Add(("alpha", "T2"));
        stemma.NodeNames.AddRange(new[] { "alpha", "archetype", "T1", "T2" });

        var vm = StemmaViewModel.Build(stemma);

        Assert.Equal(4, vm.Nodes.Count);
        Assert.Equal(3, vm.Edges.Count);
        // Root should be at layer 0
        var root = vm.Nodes.First(n => n.CanonicalName == "archetype");
        Assert.Equal(0, root.Layer);
    }
}
