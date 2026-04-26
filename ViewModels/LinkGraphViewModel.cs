using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.ViewModels;

public enum GraphColorMode { ReadingStatus, Importance, School, DoctrinalTopic }

public class LinkGraphViewModel
{
    public List<GraphNode> Nodes { get; } = new();
    public List<GraphEdge> Edges { get; } = new();
    public GraphNode? SelectedNode { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Zoom { get; set; } = 1.0;
    public GraphColorMode CurrentColorMode { get; set; } = GraphColorMode.ReadingStatus;

    // Relation type -> color hex (legacy, kept for backward compat)
    public static readonly Dictionary<string, string> RelationColors = new()
    {
        ["quotes"] = "#4285F4",
        ["alludes-to"] = "#82B1FF",
        ["comments-on"] = "#4CAF50",
        ["contradicts"] = "#F44336",
        ["parallels"] = "#9C27B0",
        ["responds-to"] = "#FF9800",
        ["is-variant-of"] = "#009688",
        ["translates"] = "#FFC107",
        ["summarizes"] = "#9E9E9E"
    };

    // Semantic edge color groups (improved contrast on dark backgrounds)
    public static readonly Dictionary<string, string> EdgeColorGroups = new()
    {
        ["quotes"] = "#59B3FF",        // Direct Reference (blue)
        ["translates"] = "#59B3FF",
        ["is-variant-of"] = "#59B3FF",
        ["comments-on"] = "#51D996",   // Commentary (green)
        ["responds-to"] = "#51D996",
        ["summarizes"] = "#51D996",
        ["alludes-to"] = "#C854D9",    // Allusion (purple)
        ["parallels"] = "#C854D9",
        ["contradicts"] = "#FF6B6B",   // Opposition (red)
    };

    public void BuildGraph(IReadOnlyList<ScholarPassage> passages, IReadOnlyList<PassageLink> links)
    {
        Nodes.Clear();
        Edges.Clear();
        SelectedNode = null;
        OffsetX = 0; OffsetY = 0; Zoom = 1.0;

        var nodeMap = new Dictionary<string, GraphNode>();

        double width = 500;
        double height = 400;
        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double initRadius = Math.Min(width, height) * 0.35;

        for (int i = 0; i < passages.Count; i++)
        {
            var p = passages[i];
            var label = p.ZhText?.Length > 8 ? p.ZhText[..8] + "\u2026" : p.ZhText ?? "?";
            double angle = (2.0 * Math.PI * i) / passages.Count;
            var node = new GraphNode
            {
                PassageId = p.Id ?? "",
                Label = label,
                X = centerX + initRadius * Math.Cos(angle),
                Y = centerY + initRadius * Math.Sin(angle)
            };
            Nodes.Add(node);
            nodeMap[p.Id ?? ""] = node;
        }

        foreach (var link in links)
        {
            if (nodeMap.TryGetValue(link.FromPassageId ?? "", out var from) &&
                nodeMap.TryGetValue(link.ToPassageId ?? "", out var to))
            {
                Edges.Add(new GraphEdge { From = from, To = to, RelationType = link.RelationType ?? "parallels" });
                from.Degree++;
                to.Degree++;
            }
        }
    }

    public void RunLayout(int iterations = 150, double width = 500, double height = 400)
    {
        if (Nodes.Count <= 1) return;

        double area = width * height;
        double k = Math.Sqrt(area / Nodes.Count);
        double temperature = width / 10.0;
        const double cooling = 0.95;
        double centerX = width / 2.0;
        double centerY = height / 2.0;

        for (int iter = 0; iter < iterations; iter++)
        {
            // Reset velocities
            foreach (var n in Nodes) { n.Vx = 0; n.Vy = 0; }

            // Repulsive forces between all pairs (label-aware)
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int j = i + 1; j < Nodes.Count; j++)
                {
                    double dx = Nodes[i].X - Nodes[j].X;
                    double dy = Nodes[i].Y - Nodes[j].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;

                    // Label-aware minimum separation
                    double labelBuffer = 45;
                    double r_i = 8 + Math.Min(Nodes[i].Degree * 2, 14) + labelBuffer;
                    double r_j = 8 + Math.Min(Nodes[j].Degree * 2, 14) + labelBuffer;
                    double minSeparation = r_i + r_j;
                    double effectiveDist = Math.Max(dist, minSeparation * 0.5);

                    double force = (k * k) / effectiveDist;
                    double fx = (dx / dist) * force;
                    double fy = (dy / dist) * force;
                    Nodes[i].Vx += fx;
                    Nodes[i].Vy += fy;
                    Nodes[j].Vx -= fx;
                    Nodes[j].Vy -= fy;
                }
            }

            // Attractive forces along edges
            foreach (var edge in Edges)
            {
                double dx = edge.From.X - edge.To.X;
                double dy = edge.From.Y - edge.To.Y;
                double dist = Math.Max(Math.Sqrt(dx * dx + dy * dy), 0.01);
                double force = (dist * dist) / k;
                double fx = (dx / dist) * force;
                double fy = (dy / dist) * force;
                edge.From.Vx -= fx;
                edge.From.Vy -= fy;
                edge.To.Vx += fx;
                edge.To.Vy += fy;
            }

            // Gravity toward center
            double gravityStrength = 0.008 * k;
            foreach (var node in Nodes)
            {
                double dx = centerX - node.X;
                double dy = centerY - node.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                node.Vx += (dx / dist) * gravityStrength * dist * 0.01;
                node.Vy += (dy / dist) * gravityStrength * dist * 0.01;
            }

            // Apply with temperature clamping
            foreach (var n in Nodes)
            {
                double disp = Math.Sqrt(n.Vx * n.Vx + n.Vy * n.Vy);
                if (disp > 0.01)
                {
                    double scale = Math.Min(disp, temperature) / disp;
                    n.X += n.Vx * scale;
                    n.Y += n.Vy * scale;
                }
                // Keep in bounds with margin
                n.X = Math.Max(30, Math.Min(width - 30, n.X));
                n.Y = Math.Max(30, Math.Min(height - 30, n.Y));
            }

            temperature *= cooling;
        }
    }

    public void ApplyLayout(ScholarGraphLayout? layout)
    {
        if (layout == null)
            return;

        OffsetX = layout.OffsetX;
        OffsetY = layout.OffsetY;
        Zoom = layout.Zoom > 0 ? layout.Zoom : 1.0;

        foreach (var node in Nodes)
        {
            if (layout.NodePositions.TryGetValue(node.PassageId, out var pos))
            {
                node.X = pos.X;
                node.Y = pos.Y;
            }
        }
    }

    public ScholarGraphLayout CaptureLayout()
    {
        var layout = new ScholarGraphLayout
        {
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            Zoom = Zoom
        };

        foreach (var node in Nodes)
        {
            layout.NodePositions[node.PassageId] = new GraphNodeLayout
            {
                X = node.X,
                Y = node.Y
            };
        }

        return layout;
    }
    public GraphNode? HitTest(double canvasX, double canvasY, double nodeRadius = 15)
    {
        // Convert canvas coords to graph coords
        double gx = (canvasX - OffsetX) / Zoom;
        double gy = (canvasY - OffsetY) / Zoom;

        foreach (var node in Nodes)
        {
            double dx = node.X - gx;
            double dy = node.Y - gy;
            if (dx * dx + dy * dy <= nodeRadius * nodeRadius)
                return node;
        }
        return null;
    }
}
