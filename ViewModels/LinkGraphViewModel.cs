using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.ViewModels;

public class LinkGraphViewModel
{
    public List<GraphNode> Nodes { get; } = new();
    public List<GraphEdge> Edges { get; } = new();
    public GraphNode? SelectedNode { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Zoom { get; set; } = 1.0;

    // Relation type -> color hex
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

    public void BuildGraph(IReadOnlyList<ScholarPassage> passages, IReadOnlyList<PassageLink> links)
    {
        Nodes.Clear();
        Edges.Clear();
        SelectedNode = null;
        OffsetX = 0; OffsetY = 0; Zoom = 1.0;

        var nodeMap = new Dictionary<string, GraphNode>();
        var rng = new Random(42); // deterministic for consistent layout

        foreach (var p in passages)
        {
            var label = p.ZhText?.Length > 8 ? p.ZhText[..8] + "\u2026" : p.ZhText ?? "?";
            var node = new GraphNode
            {
                PassageId = p.Id ?? "",
                Label = label,
                X = rng.NextDouble() * 400,
                Y = rng.NextDouble() * 400
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
            }
        }
    }

    public void RunLayout(int iterations = 80, double width = 500, double height = 400)
    {
        if (Nodes.Count <= 1) return;

        double area = width * height;
        double k = Math.Sqrt(area / Nodes.Count);
        double temperature = width / 10.0;
        const double cooling = 0.95;

        for (int iter = 0; iter < iterations; iter++)
        {
            // Reset velocities
            foreach (var n in Nodes) { n.Vx = 0; n.Vy = 0; }

            // Repulsive forces between all pairs
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int j = i + 1; j < Nodes.Count; j++)
                {
                    double dx = Nodes[i].X - Nodes[j].X;
                    double dy = Nodes[i].Y - Nodes[j].Y;
                    double dist = Math.Max(Math.Sqrt(dx * dx + dy * dy), 0.01);
                    double force = (k * k) / dist;
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
