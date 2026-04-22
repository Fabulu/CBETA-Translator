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
    public double OrphanSectionY { get; set; }

    /// <summary>
    /// When non-empty, rendering dims every node + edge that's NOT in this set.
    /// Populated by <see cref="FocusOn"/> (click-to-focus) and cleared by
    /// <see cref="ClearFocus"/> (click empty space). Contains the focused node
    /// itself plus every ancestor (teacher chain) and every descendant (student
    /// chain) reachable through lineage edges.
    /// </summary>
    public HashSet<LineageGraphNode> FocusedNodes { get; } = new();

    /// <summary>
    /// Compute and store the lineage closure around <paramref name="node"/>:
    /// all ancestors (teachers of teachers...) + all descendants (students of
    /// students...). Replaces any prior focus. Safe to call repeatedly.
    /// </summary>
    public void FocusOn(LineageGraphNode node)
    {
        FocusedNodes.Clear();
        FocusedNodes.Add(node);

        // Direct connections only: immediate teacher + immediate students
        foreach (var edge in Edges)
        {
            if (edge.To == node) FocusedNodes.Add(edge.From);   // teacher
            if (edge.From == node) FocusedNodes.Add(edge.To);   // student
        }
    }

    public void ClearFocus() => FocusedNodes.Clear();

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
        ["Niutou"] = Color.FromRgb(121, 85, 72),
        ["Korean Seon"] = Color.FromRgb(26, 122, 106),
        ["Early Chan"] = Color.FromRgb(180, 160, 130),
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
                Attestation = rec.Attestation ?? "",
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

        // Build adjacency
        var childrenOf = new Dictionary<LineageGraphNode, List<LineageGraphNode>>();
        var parentOf = new Dictionary<LineageGraphNode, LineageGraphNode>();

        foreach (var edge in Edges)
        {
            if (!childrenOf.ContainsKey(edge.From))
                childrenOf[edge.From] = new();
            childrenOf[edge.From].Add(edge.To);
            parentOf[edge.To] = edge.From;
        }

        // Separate connected from orphans
        var connected = new HashSet<LineageGraphNode>();
        foreach (var edge in Edges)
        {
            connected.Add(edge.From);
            connected.Add(edge.To);
        }
        var orphans = Nodes.Where(n => !connected.Contains(n)).OrderBy(n => n.SortDate).ToList();
        var treeRoots = connected.Where(n => !parentOf.ContainsKey(n)).OrderBy(n => n.SortDate).ToList();

        // BFS assign layers (generation depth)
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
                foreach (var child in children.OrderBy(c => c.SortDate))
                {
                    if (visited.Contains(child)) continue;
                    child.Layer = node.Layer + 1;
                    visited.Add(child);
                    queue.Enqueue(child);
                }
            }
        }

        int maxLayer = connected.Count > 0 ? connected.Max(n => n.Layer) : 0;

        // ── TEMPORAL LAYOUT ──
        // Y = death/floruit year (absolute temporal position)
        // X = generation layer (left to right)
        //
        // This makes the chart a TIMELINE: early masters at top, late masters at bottom.
        // Teacher → student lines naturally flow downward because students die later.

        int minYear = Nodes.Where(n => n.SortDate > 0).Select(n => n.SortDate).DefaultIfEmpty(300).Min();

        foreach (var node in Nodes.Where(n => connected.Contains(n)))
        {
            node.X = node.Layer * HorizontalSpacing + 60;
            node.Y = node.SortDate > 0
                ? (node.SortDate - minYear) * PixelsPerYear + 60
                : 60; // unknown date goes to top
        }

        bool IsKorean(LineageGraphNode n) =>
            n.School?.Contains("Korean", StringComparison.OrdinalIgnoreCase) == true;

        // ── Collision resolution within each layer (Chinese only first) ──
        var chineseConnected = Nodes.Where(n => connected.Contains(n) && !IsKorean(n));
        var layers = chineseConnected
            .GroupBy(n => n.Layer)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Y).ToList());

        foreach (var (_, layerNodes) in layers)
        {
            for (int i = 1; i < layerNodes.Count; i++)
            {
                double minGap = NodeHeight + 6;
                if (layerNodes[i].Y - layerNodes[i - 1].Y < minGap)
                    layerNodes[i].Y = layerNodes[i - 1].Y + minGap;
            }
        }

        // ── Korean positioning AFTER Chinese collision resolution ──
        var koreanNodes = Nodes.Where(n => connected.Contains(n) && IsKorean(n)).ToList();
        if (koreanNodes.Count > 0)
        {
            const double MinGap = 200;
            const double YProximity = NodeHeight * 15;

            var chineseNodes = Nodes
                .Where(n => connected.Contains(n) && !IsKorean(n)).ToList();

            // Push each Korean node right of nearby Chinese (by final Y position)
            foreach (var node in koreanNodes)
            {
                double maxNearbyX = 0;
                foreach (var c in chineseNodes)
                {
                    if (Math.Abs(c.Y - node.Y) < YProximity && c.X > maxNearbyX)
                        maxNearbyX = c.X;
                }

                double minX = maxNearbyX + NodeWidth + MinGap;
                if (node.X < minX)
                    node.X = minX;
            }

            // Korean-specific collision: push overlapping Korean nodes rightward
            var sortedKorean = koreanNodes.OrderBy(n => n.X).ThenBy(n => n.Y).ToList();
            for (int i = 0; i < sortedKorean.Count; i++)
            {
                for (int j = i + 1; j < sortedKorean.Count; j++)
                {
                    var a = sortedKorean[i];
                    var b = sortedKorean[j];
                    bool xOverlap = Math.Abs(a.X - b.X) < NodeWidth + 10;
                    bool yOverlap = Math.Abs(a.Y - b.Y) < NodeHeight + 6;
                    if (xOverlap && yOverlap)
                    {
                        b.X = a.X + NodeWidth + 20;
                    }
                }
            }
        }

        // ── ORPHANS below the tree ──
        double treeMaxY = Nodes.Where(n => connected.Contains(n)).Select(n => n.Y).DefaultIfEmpty(0).Max();
        double orphanStartY = treeMaxY + NodeHeight + 60;
        OrphanSectionY = orphanStartY - 20;

        int orphanCols = Math.Max(3, (int)((maxLayer + 2) * HorizontalSpacing / (NodeWidth + 16)));
        for (int i = 0; i < orphans.Count; i++)
        {
            orphans[i].IsOrphan = true;
            orphans[i].X = (i % orphanCols) * (NodeWidth + 16) + 60;
            orphans[i].Y = orphanStartY + (i / orphanCols) * (NodeHeight + 8);
            orphans[i].Layer = -1;
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
