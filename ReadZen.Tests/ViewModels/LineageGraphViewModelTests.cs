using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class LineageGraphViewModelTests
{
    [Fact]
    public void BuildGraph_FromBundledData_HasEdges()
    {
        var path = Path.Combine(System.AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
        if (!File.Exists(path))
        {
            // Skip if running outside build output
            return;
        }

        var json = File.ReadAllText(path);
        var wrapper = JsonSerializer.Deserialize<MasterDateWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(wrapper?.Masters);

        // Build a simple catalog manually (no service dependency)
        var catalog = new ZenMasterCatalog();
        var records = new List<ZenMasterRecord>();

        foreach (var entry in wrapper!.Masters!)
        {
            var names = entry.Names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
            if (names.Count == 0) continue;

            var rec = new ZenMasterRecord
            {
                CanonicalName = names[0],
                Aliases = names,
                Variants = new List<ZenMasterVariant>
                {
                    new()
                    {
                        Names = names,
                        Floruit = entry.Floruit,
                        Death = entry.Death,
                        IsBase = true,
                        School = entry.School,
                        Teacher = entry.Teacher,
                        Students = entry.Students,
                    }
                }
            };
            records.Add(rec);
        }

        var catalogObj = new ZenMasterCatalog { Records = records };

        var vm = new LineageGraphViewModel();
        vm.BuildGraph(catalogObj);

        // Must have nodes
        Assert.True(vm.Nodes.Count > 100, $"Expected 100+ nodes, got {vm.Nodes.Count}");

        // Must have edges (teacher links)
        Assert.True(vm.Edges.Count > 20, $"Expected 20+ edges, got {vm.Edges.Count}");

        // Specific check: Huike's teacher should be Bodhidharma
        var huike = vm.Nodes.FirstOrDefault(n => n.CanonicalName == "Huike");
        Assert.NotNull(huike);
        var huikeEdge = vm.Edges.FirstOrDefault(e => e.To == huike);
        Assert.NotNull(huikeEdge);
        Assert.Equal("Bodhidharma", huikeEdge!.From.CanonicalName);

        // Check that the service-loaded catalog also works
        var svc = new MasterDatesService();
        var mgr = new ZenMasterManagerService(svc);
        var serviceCatalog = mgr.LoadAsync(null, path).GetAwaiter().GetResult();

        // Verify teacher field survived service loading
        var huikeFromService = serviceCatalog.Records.FirstOrDefault(r => r.CanonicalName == "Huike");
        Assert.NotNull(huikeFromService);
        // Debug: what's the actual teacher value?
        var actualTeacher = huikeFromService!.Teacher;
        Assert.True(!string.IsNullOrWhiteSpace(actualTeacher),
            $"Huike teacher is null/empty. Variants: {huikeFromService.Variants.Count}, " +
            $"variant teachers: [{string.Join(", ", huikeFromService.Variants.Select(v => v.Teacher ?? "NULL"))}]");
        Assert.Equal("Bodhidharma", actualTeacher);

        var vm2 = new LineageGraphViewModel();
        vm2.BuildGraph(serviceCatalog);
        Assert.True(vm2.Edges.Count > 20, $"Service-loaded graph: expected 20+ edges, got {vm2.Edges.Count}");
    }

    private sealed class MasterDateWrapper
    {
        public List<MasterDateEntry>? Masters { get; set; }
    }
}
