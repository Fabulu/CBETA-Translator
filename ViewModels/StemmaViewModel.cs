// ViewModels/StemmaViewModel.cs
// Wraps LineageGraphViewModel to display witness transmission relationships
// (stemma) using the existing lineage web rendering infrastructure.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

/// <summary>
/// Builds a <see cref="LineageGraphViewModel"/> from stemma edges and
/// witness metadata. Reuses the existing lineage web control for rendering.
/// </summary>
public static class StemmaViewModel
{
    /// <summary>
    /// Predefined family colors for consistent stemma visualization.
    /// Witness families get distinct colors; unknown families get grey.
    /// </summary>
    private static readonly Dictionary<string, Color> FamilyColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["standalone"] = Color.FromRgb(76, 175, 80),
        ["shibuuroku"] = Color.FromRgb(30, 136, 229),
        ["nyuushuunichiyou"] = Color.FromRgb(156, 39, 176),
        ["korea-commons"] = Color.FromRgb(0, 150, 136),
        ["archetype"] = Color.FromRgb(255, 183, 77),
    };

    /// <summary>
    /// Creates a fully laid-out LineageGraphViewModel from stemma data
    /// and optional witness metadata for enriched display.
    /// </summary>
    public static LineageGraphViewModel Build(
        StemmaParserService.StemmaData stemma,
        WitnessTextRegistry? registry = null)
    {
        var vm = new LineageGraphViewModel();

        // Build lookup for witness metadata
        var witnessLookup = new Dictionary<string, WitnessTextEntry>(StringComparer.OrdinalIgnoreCase);
        if (registry?.Witnesses != null)
        {
            foreach (var w in registry.Witnesses)
            {
                if (!string.IsNullOrWhiteSpace(w.Siglum))
                    witnessLookup.TryAdd(w.Siglum, w);
                if (!string.IsNullOrWhiteSpace(w.WitnessId))
                    witnessLookup.TryAdd(w.WitnessId, w);
            }
        }

        // Create nodes
        var nodeLookup = new Dictionary<string, LineageGraphNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in stemma.NodeNames)
        {
            witnessLookup.TryGetValue(name, out var witness);

            var familyId = witness?.FamilyId ?? "";
            // Map family to a "school" string for coloring via the existing engine
            var school = MapFamilyToSchool(familyId, name);

            var node = new LineageGraphNode
            {
                CanonicalName = witness?.Siglum ?? name,
                ChineseName = witness?.Label ?? "",
                DatesSummary = witness != null
                    ? $"{witness.StatusDisplay} | {witness.Completeness ?? "?"}"
                    : "",
                School = school,
                Notes = witness?.FamilyId,
                SortDate = 0, // stemma doesn't use temporal layout
            };

            vm.Nodes.Add(node);
            nodeLookup[name] = node;

            // Also register by siglum if different
            if (witness?.Siglum != null && !string.Equals(witness.Siglum, name, StringComparison.OrdinalIgnoreCase))
                nodeLookup.TryAdd(witness.Siglum, node);
        }

        // Create edges
        foreach (var (from, to) in stemma.Edges)
        {
            if (nodeLookup.TryGetValue(from, out var fromNode) &&
                nodeLookup.TryGetValue(to, out var toNode) &&
                fromNode != toNode)
            {
                vm.Edges.Add(new LineageEdge { From = fromNode, To = toNode });
            }
        }

        // Run the existing layered layout engine
        RunStemmaLayout(vm);

        return vm;
    }

    /// <summary>
    /// Maps witness family IDs to "school" names that the existing
    /// LineageGraphViewModel.GetSchoolColor can handle, or assigns
    /// custom colors via the SchoolColors dictionary.
    /// </summary>
    private static string MapFamilyToSchool(string familyId, string nodeName)
    {
        if (string.Equals(nodeName, "archetype", StringComparison.OrdinalIgnoreCase))
            return "Hongzhou"; // orange-green for root

        if (string.IsNullOrWhiteSpace(familyId))
            return "Chan"; // grey for unknown

        // Map known families to existing school colors for visual variety
        return familyId.ToLowerInvariant() switch
        {
            "standalone" => "Fayan",        // teal
            "shibuuroku" => "Caodong",      // blue
            "nyuushuunichiyou" => "Yunmen",  // purple
            _ => "Linji",                    // red for other families
        };
    }

    /// <summary>
    /// Simple top-down tree layout for stemma graphs. Unlike the temporal
    /// lineage layout, this uses pure generation depth for Y positioning.
    /// </summary>
    private static void RunStemmaLayout(LineageGraphViewModel vm)
    {
        if (vm.Nodes.Count == 0) return;

        // Build adjacency
        var childrenOf = new Dictionary<LineageGraphNode, List<LineageGraphNode>>();
        var parentOf = new Dictionary<LineageGraphNode, LineageGraphNode>();

        foreach (var edge in vm.Edges)
        {
            if (!childrenOf.ContainsKey(edge.From))
                childrenOf[edge.From] = new();
            childrenOf[edge.From].Add(edge.To);
            parentOf.TryAdd(edge.To, edge.From);
        }

        // Find roots (nodes with no parent)
        var roots = vm.Nodes.Where(n => !parentOf.ContainsKey(n)).ToList();
        var orphans = vm.Nodes.Where(n => !parentOf.ContainsKey(n) &&
            (!childrenOf.ContainsKey(n) || childrenOf[n].Count == 0)).ToList();
        var treeRoots = roots.Except(orphans).ToList();
        if (treeRoots.Count == 0) treeRoots = roots.Take(1).ToList(); // at least one root

        // BFS to assign layers
        var visited = new HashSet<LineageGraphNode>();
        var queue = new Queue<LineageGraphNode>();
        foreach (var root in treeRoots)
        {
            root.Layer = 0;
            queue.Enqueue(root);
            visited.Add(root);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (childrenOf.TryGetValue(node, out var children))
            {
                foreach (var child in children)
                {
                    if (visited.Contains(child)) continue;
                    child.Layer = node.Layer + 1;
                    visited.Add(child);
                    queue.Enqueue(child);
                }
            }
        }

        // Position: X = layer (generation), Y = order within layer
        double xSpacing = LineageGraphViewModel.HorizontalSpacing;
        double ySpacing = LineageGraphViewModel.NodeHeight + 12;

        var layers = vm.Nodes.Where(n => visited.Contains(n))
            .GroupBy(n => n.Layer)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (layer, nodes) in layers)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].X = layer * xSpacing + 60;
                nodes[i].Y = i * ySpacing + 60;
            }
        }

        // Position orphans below
        double maxY = vm.Nodes.Where(n => visited.Contains(n))
            .Select(n => n.Y).DefaultIfEmpty(0).Max();
        double orphanY = maxY + LineageGraphViewModel.NodeHeight + 40;

        var realOrphans = vm.Nodes.Where(n => !visited.Contains(n)).ToList();
        for (int i = 0; i < realOrphans.Count; i++)
        {
            realOrphans[i].IsOrphan = true;
            realOrphans[i].X = (i % 3) * (LineageGraphViewModel.NodeWidth + 16) + 60;
            realOrphans[i].Y = orphanY + (i / 3) * (LineageGraphViewModel.NodeHeight + 8);
        }
    }
}
