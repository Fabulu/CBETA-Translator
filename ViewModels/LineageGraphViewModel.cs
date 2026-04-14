// ViewModels/LineageGraphViewModel.cs
// Builds and lays out the Zen lineage graph from the master catalog.
// Layered layout (Sugiyama-lite) with school coloring and era bands.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

public sealed class LineageGraphViewModel
{
    public List<LineageGraphNode> Nodes { get; } = new();
    public List<LineageEdge> Edges { get; } = new();
    public LineageGraphNode? SelectedNode { get; set; }

    public const double NodeWidth = 130;
    public const double NodeHeight = 38;
    public const double HorizontalSpacing = 160;
    public const double VerticalSpacing = 60;
    public const double PixelsPerYear = 3.0;

    public static readonly Dictionary<string, Color> SchoolColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Linji"] = Color.FromRgb(211, 47, 47),
        ["Caodong"] = Color.FromRgb(30, 136, 229),
        ["Yunmen"] = Color.FromRgb(156, 39, 176),
        ["Fayan"] = Color.FromRgb(0, 150, 136),
        ["Guiyang"] = Color.FromRgb(255, 143, 0),
        ["Hongzhou"] = Color.FromRgb(76, 175, 80),
        ["Northern Chan"] = Color.FromRgb(120, 144, 156),
        ["Southern Chan"] = Color.FromRgb(255, 183, 77),
        ["Niutou"] = Color.FromRgb(121, 85, 72),
        ["Chan"] = Color.FromRgb(158, 158, 158),
    };

    public static Color GetSchoolColor(string? school)
    {
        if (string.IsNullOrEmpty(school)) return Color.FromRgb(158, 158, 158);
        foreach (var (key, color) in SchoolColors)
        {
            if (school.Contains(key, StringComparison.OrdinalIgnoreCase))
                return color;
        }
        return Color.FromRgb(158, 158, 158);
    }

    public void BuildGraph(ZenMasterCatalog catalog)
    {
        Nodes.Clear();
        Edges.Clear();

        // Build nodes
        var lookup = new Dictionary<string, LineageGraphNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var rec in catalog.Records)
        {
            var variant = rec.PrimaryVariant;
            if (variant == null) continue;

            var chineseName = rec.Aliases.FirstOrDefault(a =>
                a.Length >= 2 && Services.MasterDatesService.ContainsCjk(a)) ?? "";

            var node = new LineageGraphNode
            {
                CanonicalName = rec.CanonicalName,
                ChineseName = chineseName,
                DatesSummary = rec.DatesSummary,
                School = rec.School,
                Notes = rec.Notes,
                SortDate = variant.Death > 0 ? variant.Death : (variant.Floruit > 0 ? variant.Floruit + 50 : 0),
                Record = rec,
            };
            Nodes.Add(node);

            // Register all aliases for lookup
            foreach (var alias in rec.Aliases)
                lookup.TryAdd(alias, node);
            lookup.TryAdd(rec.CanonicalName, node);
        }

        // Build edges from teacher/student relationships
        foreach (var node in Nodes)
        {
            var teacher = node.Record?.Teacher;
            if (!string.IsNullOrWhiteSpace(teacher) && lookup.TryGetValue(teacher, out var teacherNode))
            {
                if (teacherNode != node) // no self-loops
                    Edges.Add(new LineageEdge { From = teacherNode, To = node });
            }
        }
    }

    public void RunLayeredLayout()
    {
        if (Nodes.Count == 0) return;

        // Assign layers via BFS from roots
        var childrenOf = new Dictionary<LineageGraphNode, List<LineageGraphNode>>();
        var hasParent = new HashSet<LineageGraphNode>();

        foreach (var edge in Edges)
        {
            if (!childrenOf.ContainsKey(edge.From))
                childrenOf[edge.From] = new();
            childrenOf[edge.From].Add(edge.To);
            hasParent.Add(edge.To);
        }

        var roots = Nodes.Where(n => !hasParent.Contains(n)).OrderBy(n => n.SortDate).ToList();

        // BFS
        var visited = new HashSet<LineageGraphNode>();
        var queue = new Queue<LineageGraphNode>();
        foreach (var root in roots)
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

        // Assign unvisited nodes (orphans) to layer 0
        foreach (var node in Nodes.Where(n => !visited.Contains(n)))
            node.Layer = 0;

        // Group by layer, sort by date within layer
        var layers = Nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key).ToList();

        // Assign positions: Y by date, X by layer position
        foreach (var layer in layers)
        {
            var sorted = layer.OrderBy(n => n.SortDate).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].X = sorted[i].Layer * HorizontalSpacing + 50;
                sorted[i].Y = sorted[i].SortDate > 0
                    ? (sorted[i].SortDate - 300) * PixelsPerYear
                    : i * VerticalSpacing + 50;
            }
        }

        // Simple X-spreading: within each layer, spread nodes that overlap
        foreach (var layer in layers)
        {
            var sorted = layer.OrderBy(n => n.Y).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Y - sorted[i - 1].Y < NodeHeight + 8)
                    sorted[i].Y = sorted[i - 1].Y + NodeHeight + 8;
            }
        }
    }

    public void HighlightSearch(string? query)
    {
        foreach (var node in Nodes)
        {
            node.IsHighlighted = !string.IsNullOrWhiteSpace(query) &&
                (node.CanonicalName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 node.ChineseName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
    }

    public LineageGraphNode? HitTest(double x, double y)
    {
        foreach (var node in Nodes)
        {
            if (x >= node.X && x <= node.X + NodeWidth &&
                y >= node.Y && y <= node.Y + NodeHeight)
                return node;
        }
        return null;
    }

    public List<(int Century, double Y)> GetEraBands()
    {
        var bands = new List<(int, double)>();
        for (int c = 300; c <= 1400; c += 100)
            bands.Add((c, (c - 300) * PixelsPerYear));
        return bands;
    }
}
