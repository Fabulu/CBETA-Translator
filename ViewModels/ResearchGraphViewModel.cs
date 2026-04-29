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
    public object? SourceData { get; set; }
    public bool IsPinned { get; set; }
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

    // Collection navigation history
    private readonly Stack<string> _collectionHistory = new();
    public bool CanGoBack => _collectionHistory.Count > 0;

    public ScholarCollection GetCollection() => _collection;
    public List<ScholarCollection> GetAllCollections() => _allCollections;

    public void SwitchToCollection(string collectionId, bool pushHistory = true)
    {
        var target = _allCollections.FirstOrDefault(c => c.Id == collectionId);
        if (target == null) return;

        if (pushHistory)
            _collectionHistory.Push(_collection.Id);

        // Save current layout
        SaveLayoutToCollection();

        // Switch
        _collection = target;
        RebuildGraph();
    }

    public void GoBack()
    {
        if (_collectionHistory.Count == 0) return;
        var previousId = _collectionHistory.Pop();
        SwitchToCollection(previousId, pushHistory: false);
    }

    /// <summary>Zoom and pan state from the canvas, set by the window before saving.</summary>
    public double SavedZoom { get; set; } = 1.0;
    public double SavedOffsetX { get; set; }
    public double SavedOffsetY { get; set; }

    public void SaveLayoutToCollection()
    {
        var layout = _collection.GraphLayout ?? new ScholarGraphLayout();
        layout.NodePositions.Clear();
        foreach (var node in Nodes)
        {
            layout.NodePositions[node.NodeId] = new GraphNodeLayout { X = node.X, Y = node.Y };
        }
        layout.Zoom = SavedZoom;
        layout.OffsetX = SavedOffsetX;
        layout.OffsetY = SavedOffsetY;
        _collection.GraphLayout = layout;
    }

    /// <summary>Returns the saved zoom/pan if a layout exists, otherwise null.</summary>
    public (double zoom, double offsetX, double offsetY)? GetSavedViewport()
    {
        var layout = _collection.GraphLayout;
        if (layout?.NodePositions != null && layout.NodePositions.Count > 0 && layout.Zoom > 0.01)
            return (layout.Zoom, layout.OffsetX, layout.OffsetY);
        return null;
    }

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
                ColorHex = "#6EAFF8",
                SourceData = p
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
                ColorHex = c.ColorHex ?? "#FF8A65",
                SourceData = c
            };
            Nodes.Add(node);
            _nodeMap[c.Id] = node;
        }

        // Add master nodes (from passage MasterNames)
        var masterNames = _collection.Passages
            .SelectMany(p => p.MasterNames ?? new List<string>())
            .Distinct()
            .ToList();
        foreach (var name in masterNames)
        {
            var nodeId = $"master:{name}";
            if (_nodeMap.ContainsKey(nodeId)) continue;
            var relatedPassages = _collection.Passages
                .Where(p => p.MasterNames != null && p.MasterNames.Contains(name))
                .Select(p => p.DisplayTitle)
                .ToList();
            var node = new ResearchGraphNode
            {
                NodeId = nodeId,
                NodeType = ScholarNodeType.ZenMaster,
                Label = name,
                ColorHex = "#64B5F6",
                SourceData = new Dictionary<string, object>
                {
                    ["PassageCount"] = relatedPassages.Count,
                    ["Passages"] = relatedPassages
                }
            };
            Nodes.Add(node);
            _nodeMap[nodeId] = node;
        }

        // Add collection reference nodes
        if (_collection.CollectionRefs != null)
        {
            foreach (var collRef in _collection.CollectionRefs)
            {
                var nodeId = $"collection:{collRef.CollectionId}";
                if (_nodeMap.ContainsKey(nodeId)) continue;
                var node = new ResearchGraphNode
                {
                    NodeId = nodeId,
                    NodeType = ScholarNodeType.Collection,
                    Label = collRef.CollectionName ?? collRef.CollectionId,
                    SecondaryLabel = collRef.IsShared ? $"by {collRef.OwnerUsername}" : "local",
                    ColorHex = "#AB47BC"
                };
                Nodes.Add(node);
                _nodeMap[nodeId] = node;
            }
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

        // Apply saved positions if available
        var savedLayout = _collection.GraphLayout;
        if (savedLayout?.NodePositions != null && savedLayout.NodePositions.Count > 0)
        {
            bool hasPositions = false;
            foreach (var node in Nodes)
            {
                if (savedLayout.NodePositions.TryGetValue(node.NodeId, out var pos))
                {
                    node.X = pos.X;
                    node.Y = pos.Y;
                    hasPositions = true;
                }
            }
            if (hasPositions)
            {
                ComputeStats();
                return; // Skip force layout — use saved positions
            }
        }

        // No saved positions — run force-directed layout
        if (Nodes.Count > 1) RunForceDirectedLayout(800, 600);
        ComputeStats();
    }

    public void RunForceDirectedLayout(double width, double height)
    {
        if (Nodes.Count <= 1)
        {
            if (Nodes.Count == 1) { Nodes[0].X = width / 2; Nodes[0].Y = height / 2; }
            return;
        }

        var nodeList = Nodes.OrderBy(n => n.NodeId).ToList();
        int N = nodeList.Count;

        // SPA-aligned parameters
        double R = Math.Sqrt(N) * 80;
        double k = Math.Sqrt((R * R * 4) / N);
        double temp = R / 5;
        double cx = width / 2, cy = height / 2;

        // Circular initial positions centered on viewport
        for (int i = 0; i < N; i++)
        {
            double angle = (2.0 * Math.PI * i) / N;
            nodeList[i].X = cx + R * Math.Cos(angle);
            nodeList[i].Y = cy + R * Math.Sin(angle);
            nodeList[i].Vx = 0;
            nodeList[i].Vy = 0;
        }

        // Force-directed iterations
        for (int iter = 0; iter < 150; iter++)
        {
            double maxDisp = 0;

            // Repulsion (all pairs): force = k^2 / dist
            for (int i = 0; i < N; i++)
            {
                for (int j = i + 1; j < N; j++)
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

            // Attraction (edges): force = dist^2 / k
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

            // Gravity: pull toward viewport center
            foreach (var n in nodeList)
            {
                n.Vx -= (n.X - cx) * 0.01;
                n.Vy -= (n.Y - cy) * 0.01;
            }

            // Apply with temperature clamping
            foreach (var n in nodeList)
            {
                double disp = Math.Sqrt(n.Vx * n.Vx + n.Vy * n.Vy) + 0.01;
                double scale = Math.Min(disp, temp) / disp;
                double moveX = n.Vx * scale;
                double moveY = n.Vy * scale;
                n.X += moveX;
                n.Y += moveY;
                n.X = Math.Max(30, Math.Min(width - 30, n.X));
                n.Y = Math.Max(30, Math.Min(height - 30, n.Y));
                n.Vx = 0;
                n.Vy = 0;
                maxDisp = Math.Max(maxDisp, Math.Abs(moveX) + Math.Abs(moveY));
            }

            // Cooling + convergence
            temp *= 0.95;
            if (maxDisp < 0.5) break;  // Early exit on convergence
        }
    }

    public void AddConcept(ConceptNode concept)
    {
        _collection.Concepts.Add(concept);
        // Place near centroid of existing nodes (or center if graph is empty)
        double cx = Nodes.Count > 0 ? Nodes.Average(n => n.X) : 400;
        double cy = Nodes.Count > 0 ? Nodes.Average(n => n.Y) : 300;
        var node = new ResearchGraphNode
        {
            NodeId = concept.Id,
            NodeType = ScholarNodeType.Concept,
            Label = concept.DisplayTitle,
            ColorHex = concept.ColorHex ?? "#FF8A65",
            X = cx + 30,  // Slight offset from centroid to avoid overlap
            Y = cy + 30,
            SourceData = concept
        };
        Nodes.Add(node);
        _nodeMap[concept.Id] = node;
        // Don't re-layout entire graph — preserve user's arrangement
        ComputeStats();
    }

    public void AddEdge(ScholarGraphEdge edge)
    {
        if (edge.FromNodeId == edge.ToNodeId) return;
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

    public void RestoreNodeToMap(ResearchGraphNode node)
    {
        _nodeMap[node.NodeId] = node;
    }

    /// <summary>
    /// Removes a single edge by its edge ID, updating degrees and the backing collection.
    /// </summary>
    public void RemoveEdge(string edgeId)
    {
        var edgeVm = Edges.FirstOrDefault(e => e.EdgeId == edgeId);
        if (edgeVm == null) return;
        edgeVm.From.Degree--;
        edgeVm.To.Degree--;
        Edges.Remove(edgeVm);
        _collection.Edges.RemoveAll(e => e.Id == edgeId);
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
            if (e.From.NodeId != nodeId) e.From.Degree--;
            if (e.To.NodeId != nodeId) e.To.Degree--;
            Edges.Remove(e);
            _collection.Edges.RemoveAll(se => se.Id == e.EdgeId);
        }

        Nodes.Remove(node);
        _nodeMap.Remove(nodeId);

        // Remove from collection
        _collection.Concepts.RemoveAll(c => c.Id == nodeId);
        _collection.Passages.RemoveAll(p => p.Id == nodeId);
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

    public void ExecuteCommand(IGraphCommand cmd)
    {
        cmd.Execute();
        _undoStack.Push(cmd);
        _redoStack.Clear();
    }

    public void SetEgoMode(string? nodeId)
    {
        if (nodeId == null)
        {
            foreach (var n in Nodes) n.IsDimmed = false;
            return;
        }
        if (!_nodeMap.ContainsKey(nodeId)) return;
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

    /// <summary>Returns all nodes that currently have IsSelected = true.</summary>
    public IReadOnlyList<ResearchGraphNode> GetSelectedNodes()
    {
        return Nodes.Where(n => n.IsSelected).ToList();
    }

    /// <summary>
    /// Moves a passage from its current index to a target index within the collection.
    /// This reorders the backing list and rebuilds the graph.
    /// </summary>
    public void MovePassageToIndex(int sourceIndex, int targetIndex)
    {
        var passages = _collection.Passages;
        if (sourceIndex < 0 || sourceIndex >= passages.Count) return;
        if (targetIndex < 0 || targetIndex >= passages.Count) return;
        if (sourceIndex == targetIndex) return;

        var item = passages[sourceIndex];
        passages.RemoveAt(sourceIndex);
        passages.Insert(targetIndex, item);
    }

    /// <summary>
    /// Merges concept B into concept A: all edges referencing B are re-pointed to A,
    /// then B is removed from the graph and collection.
    /// </summary>
    public void MergeConceptInto(string sourceConceptId, string targetConceptId)
    {
        if (sourceConceptId == targetConceptId) return;
        if (!_nodeMap.ContainsKey(sourceConceptId) || !_nodeMap.ContainsKey(targetConceptId)) return;

        var targetNode = _nodeMap[targetConceptId];

        // Re-point all edges from source to target
        foreach (var edge in Edges.ToList())
        {
            bool changed = false;
            if (edge.From.NodeId == sourceConceptId)
            {
                edge.From.Degree--;
                edge.From = targetNode;
                targetNode.Degree++;
                changed = true;
            }
            if (edge.To.NodeId == sourceConceptId)
            {
                edge.To.Degree--;
                edge.To = targetNode;
                targetNode.Degree++;
                changed = true;
            }
            if (changed)
            {
                // Update the backing model edge
                var modelEdge = _collection.Edges.FirstOrDefault(e => e.Id == edge.EdgeId);
                if (modelEdge != null)
                {
                    if (modelEdge.FromNodeId == sourceConceptId) modelEdge.FromNodeId = targetConceptId;
                    if (modelEdge.ToNodeId == sourceConceptId) modelEdge.ToNodeId = targetConceptId;
                }
            }
        }

        // Remove self-loops that may have resulted from merging
        var selfLoops = Edges.Where(e => e.From.NodeId == e.To.NodeId).ToList();
        foreach (var loop in selfLoops)
        {
            loop.From.Degree--;
            loop.To.Degree--;
            Edges.Remove(loop);
            _collection.Edges.RemoveAll(e => e.Id == loop.EdgeId);
        }

        // Remove source node
        RemoveNode(sourceConceptId);
    }

    /// <summary>
    /// Reverses the direction of an edge by swapping its From and To endpoints.
    /// </summary>
    public void ReverseEdge(string edgeId)
    {
        var edgeVm = Edges.FirstOrDefault(e => e.EdgeId == edgeId);
        if (edgeVm == null) return;

        // Swap the VM endpoints
        (edgeVm.From, edgeVm.To) = (edgeVm.To, edgeVm.From);

        // Swap the backing model
        var modelEdge = _collection.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (modelEdge != null)
        {
            (modelEdge.FromNodeId, modelEdge.ToNodeId) = (modelEdge.ToNodeId, modelEdge.FromNodeId);
            (modelEdge.FromNodeType, modelEdge.ToNodeType) = (modelEdge.ToNodeType, modelEdge.FromNodeType);
        }
    }
}
