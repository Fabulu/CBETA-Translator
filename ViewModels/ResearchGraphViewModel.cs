using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

public class ResearchGraphNode
{
    public string NodeId { get; set; } = "";
    public ScholarNodeType NodeType { get; set; }
    public string Label { get; set; } = "";
    public string? SecondaryLabel { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }
    public bool IsSelected { get; set; }
    public bool IsDimmed { get; set; }
    public int Degree { get; set; }
    public string ColorHex { get; set; } = "#6EAFF8";
}

public class ResearchGraphEdgeVm
{
    public string EdgeId { get; set; } = "";
    public ResearchGraphNode From { get; set; } = null!;
    public ResearchGraphNode To { get; set; } = null!;
    public string RelationType { get; set; } = "";
    public string? Label { get; set; }
    public bool IsDirectional { get; set; } = true;
    public string ColorHex { get; set; } = "#9E9E9E";
}

public interface IGraphCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

public class ResearchGraphViewModel
{
    private ScholarCollection _collection;
    private List<ScholarCollection> _allCollections;

    // Graph data
    public ObservableCollection<ResearchGraphNode> Nodes { get; } = new();
    public ObservableCollection<ResearchGraphEdgeVm> Edges { get; } = new();
    private Dictionary<string, ResearchGraphNode> _nodeMap = new();

    // Selection
    public ResearchGraphNode? SelectedNode { get; set; }
    public ResearchGraphEdgeVm? SelectedEdge { get; set; }

    // Filter state
    public bool ShowPassages { get; set; } = true;
    public bool ShowConcepts { get; set; } = true;
    public bool ShowMasters { get; set; } = true;
    public bool ShowTerms { get; set; } = true;
    public bool ShowCollections { get; set; } = true;
    public HashSet<string> HiddenEdgeTypes { get; } = new();

    // Search
    public string SearchText { get; set; } = "";
    public HashSet<string> HighlightedNodeIds { get; } = new();

    // Stats
    public int NodeCount => Nodes.Count;
    public int EdgeCount => Edges.Count;
    public int OrphanPassageCount { get; private set; }
    public int OrphanConceptCount { get; private set; }
    public int OverloadedConceptCount { get; private set; }
    public int WeakConceptCount { get; private set; }
    public double QualityScore { get; private set; } = 100;

    // Undo/Redo
    private readonly Stack<IGraphCommand> _undoStack = new();
    private readonly Stack<IGraphCommand> _redoStack = new();
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public ScholarCollection GetCollection() => _collection;

    public ResearchGraphViewModel(ScholarCollection collection, List<ScholarCollection> allCollections)
    {
        _collection = collection;
        _allCollections = allCollections;
        RebuildGraph();
    }

    public void RebuildGraph()
    {
        Nodes.Clear();
        Edges.Clear();
        _nodeMap.Clear();

        // Add passage nodes
        foreach (var p in _collection.Passages)
        {
            var snippet = p.ZhText?.Length > 20 ? p.ZhText[..20] + "\u2026" : p.ZhText;
            var node = new ResearchGraphNode
            {
                NodeId = p.Id,
                NodeType = ScholarNodeType.Passage,
                Label = p.DisplayTitle,
                SecondaryLabel = snippet,
                ColorHex = "#6EAFF8"
            };
            Nodes.Add(node);
            _nodeMap[p.Id] = node;
        }

        // Add concept nodes
        foreach (var c in _collection.Concepts)
        {
            var node = new ResearchGraphNode
            {
                NodeId = c.Id,
                NodeType = ScholarNodeType.Concept,
                Label = c.DisplayTitle,
                SecondaryLabel = c.Description.Length > 40 ? c.Description[..40] + "\u2026" : c.Description,
                ColorHex = c.ColorHex ?? "#FF8A65"
            };
            Nodes.Add(node);
            _nodeMap[c.Id] = node;
        }

        // Add master nodes (from passage MasterNames)
        var masterNames = _collection.Passages
            .SelectMany(p => p.MasterNames)
            .Distinct()
            .ToList();
        foreach (var name in masterNames)
        {
            var nodeId = $"master:{name}";
            if (_nodeMap.ContainsKey(nodeId)) continue;
            var node = new ResearchGraphNode
            {
                NodeId = nodeId,
                NodeType = ScholarNodeType.ZenMaster,
                Label = name,
                ColorHex = "#64B5F6"
            };
            Nodes.Add(node);
            _nodeMap[nodeId] = node;
        }

        // Build edges from ScholarGraphEdge
        foreach (var edge in _collection.Edges)
        {
            if (!_nodeMap.TryGetValue(edge.FromNodeId, out var fromNode)) continue;
            if (!_nodeMap.TryGetValue(edge.ToNodeId, out var toNode)) continue;

            var edgeDef = EdgeTypeRegistry.GetById(edge.RelationType);
            var vm = new ResearchGraphEdgeVm
            {
                EdgeId = edge.Id,
                From = fromNode,
                To = toNode,
                RelationType = edge.RelationType,
                Label = edgeDef?.DisplayName ?? edge.RelationType,
                IsDirectional = edgeDef?.IsDirectional ?? true,
                ColorHex = edgeDef?.ColorHex ?? "#9E9E9E"
            };
            Edges.Add(vm);
            fromNode.Degree++;
            toNode.Degree++;
        }

        // Initial layout
        RunForceDirectedLayout(800, 600);
        ComputeStats();
    }

    public void RunForceDirectedLayout(double width, double height)
    {
        if (Nodes.Count == 0) return;

        var k = Math.Sqrt((width * height) / Nodes.Count);
        double temp = width / 10.0;
        var nodeList = Nodes.ToList();

        // Circular initial positions
        for (int i = 0; i < nodeList.Count; i++)
        {
            var angle = (2.0 * Math.PI * i) / nodeList.Count;
            var radius = Math.Min(width, height) * 0.35;
            nodeList[i].X = width / 2 + radius * Math.Cos(angle);
            nodeList[i].Y = height / 2 + radius * Math.Sin(angle);
            nodeList[i].Vx = 0;
            nodeList[i].Vy = 0;
        }

        // Force-directed iterations
        for (int iter = 0; iter < 150; iter++)
        {
            // Repulsion (all pairs)
            for (int i = 0; i < nodeList.Count; i++)
            {
                for (int j = i + 1; j < nodeList.Count; j++)
                {
                    double dx = nodeList[i].X - nodeList[j].X;
                    double dy = nodeList[i].Y - nodeList[j].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                    double force = (k * k) / dist;
                    double fx = (dx / dist) * force;
                    double fy = (dy / dist) * force;
                    nodeList[i].Vx += fx;
                    nodeList[i].Vy += fy;
                    nodeList[j].Vx -= fx;
                    nodeList[j].Vy -= fy;
                }
            }

            // Attraction (edges)
            foreach (var e in Edges)
            {
                double dx = e.From.X - e.To.X;
                double dy = e.From.Y - e.To.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                double force = (dist * dist) / k;
                double fx = (dx / dist) * force;
                double fy = (dy / dist) * force;
                e.From.Vx -= fx;
                e.From.Vy -= fy;
                e.To.Vx += fx;
                e.To.Vy += fy;
            }

            // Gravity
            double gravity = 0.008 * k;
            double cx = width / 2, cy = height / 2;
            foreach (var n in nodeList)
            {
                double dx = cx - n.X;
                double dy = cy - n.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                n.Vx += (dx / dist) * gravity * dist * 0.01;
                n.Vy += (dy / dist) * gravity * dist * 0.01;
            }

            // Apply with temperature
            foreach (var n in nodeList)
            {
                double disp = Math.Sqrt(n.Vx * n.Vx + n.Vy * n.Vy) + 0.01;
                double scale = Math.Min(disp, temp) / disp;
                n.X += n.Vx * scale;
                n.Y += n.Vy * scale;
                n.X = Math.Max(30, Math.Min(width - 30, n.X));
                n.Y = Math.Max(30, Math.Min(height - 30, n.Y));
                n.Vx = 0;
                n.Vy = 0;
            }

            temp *= 0.95;
        }
    }

    public void AddConcept(ConceptNode concept)
    {
        _collection.Concepts.Add(concept);
        var node = new ResearchGraphNode
        {
            NodeId = concept.Id,
            NodeType = ScholarNodeType.Concept,
            Label = concept.DisplayTitle,
            ColorHex = concept.ColorHex ?? "#FF8A65",
            X = 400 + Random.Shared.NextDouble() * 100,
            Y = 300 + Random.Shared.NextDouble() * 100
        };
        Nodes.Add(node);
        _nodeMap[concept.Id] = node;
        ComputeStats();
    }

    public void AddEdge(ScholarGraphEdge edge)
    {
        _collection.Edges.Add(edge);
        if (!_nodeMap.TryGetValue(edge.FromNodeId, out var from)) return;
        if (!_nodeMap.TryGetValue(edge.ToNodeId, out var to)) return;

        var def = EdgeTypeRegistry.GetById(edge.RelationType);
        var vm = new ResearchGraphEdgeVm
        {
            EdgeId = edge.Id,
            From = from,
            To = to,
            RelationType = edge.RelationType,
            Label = def?.DisplayName ?? edge.RelationType,
            IsDirectional = def?.IsDirectional ?? true,
            ColorHex = def?.ColorHex ?? "#9E9E9E"
        };
        Edges.Add(vm);
        from.Degree++;
        to.Degree++;
        ComputeStats();
    }

    public void RemoveNode(string nodeId)
    {
        var node = Nodes.FirstOrDefault(n => n.NodeId == nodeId);
        if (node == null) return;

        // Remove connected edges
        var connected = Edges.Where(e => e.From.NodeId == nodeId || e.To.NodeId == nodeId).ToList();
        foreach (var e in connected)
        {
            Edges.Remove(e);
            _collection.Edges.RemoveAll(se => se.Id == e.EdgeId);
        }

        Nodes.Remove(node);
        _nodeMap.Remove(nodeId);

        // Remove from collection
        _collection.Concepts.RemoveAll(c => c.Id == nodeId);
        ComputeStats();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.Push(cmd);
    }

    public void SetEgoMode(string? nodeId)
    {
        if (nodeId == null)
        {
            foreach (var n in Nodes) n.IsDimmed = false;
            return;
        }
        var connected = new HashSet<string> { nodeId };
        foreach (var e in Edges)
        {
            if (e.From.NodeId == nodeId) connected.Add(e.To.NodeId);
            if (e.To.NodeId == nodeId) connected.Add(e.From.NodeId);
        }
        foreach (var n in Nodes)
            n.IsDimmed = !connected.Contains(n.NodeId);
    }

    public void HighlightSearch()
    {
        HighlightedNodeIds.Clear();
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var query = SearchText.ToLowerInvariant();
        foreach (var n in Nodes)
        {
            if (n.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
                HighlightedNodeIds.Add(n.NodeId);
        }
    }

    private void ComputeStats()
    {
        var passageIds = new HashSet<string>(Nodes.Where(n => n.NodeType == ScholarNodeType.Passage).Select(n => n.NodeId));
        var conceptIds = new HashSet<string>(Nodes.Where(n => n.NodeType == ScholarNodeType.Concept).Select(n => n.NodeId));

        var connectedIds = new HashSet<string>();
        foreach (var e in Edges)
        {
            connectedIds.Add(e.From.NodeId);
            connectedIds.Add(e.To.NodeId);
        }

        OrphanPassageCount = passageIds.Count(id => !connectedIds.Contains(id));
        OrphanConceptCount = conceptIds.Count(id => !connectedIds.Contains(id));
        OverloadedConceptCount = Nodes.Count(n => n.NodeType == ScholarNodeType.Concept && n.Degree > 8);
        WeakConceptCount = Nodes.Count(n => n.NodeType == ScholarNodeType.Concept && n.Degree == 1);

        int totalIssues = OrphanPassageCount + OrphanConceptCount + OverloadedConceptCount + WeakConceptCount;
        QualityScore = Math.Max(0, 100 - totalIssues * 3);
    }

    public IReadOnlyList<ResearchGraphNode> GetVisibleNodes()
    {
        return Nodes.Where(n =>
            (n.NodeType == ScholarNodeType.Passage && ShowPassages) ||
            (n.NodeType == ScholarNodeType.Concept && ShowConcepts) ||
            (n.NodeType == ScholarNodeType.ZenMaster && ShowMasters) ||
            (n.NodeType == ScholarNodeType.TermbaseEntry && ShowTerms) ||
            (n.NodeType == ScholarNodeType.Collection && ShowCollections)
        ).ToList();
    }

    public IReadOnlyList<ResearchGraphEdgeVm> GetVisibleEdges()
    {
        var visibleNodeIds = new HashSet<string>(GetVisibleNodes().Select(n => n.NodeId));
        return Edges.Where(e =>
            visibleNodeIds.Contains(e.From.NodeId) &&
            visibleNodeIds.Contains(e.To.NodeId) &&
            !HiddenEdgeTypes.Contains(e.RelationType)
        ).ToList();
    }
}
