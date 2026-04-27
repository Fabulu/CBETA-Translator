using System.Collections.Generic;
using System.Linq;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Models;

/// <summary>
/// Command to add a concept node to the research graph (supports undo).
/// </summary>
public sealed class AddConceptCommand : IGraphCommand
{
    private readonly ResearchGraphViewModel _vm;
    private readonly ConceptNode _concept;

    public string Description => $"Add concept: {_concept.Name}";

    public AddConceptCommand(ResearchGraphViewModel vm, ConceptNode concept)
    {
        _vm = vm;
        _concept = concept;
    }

    public void Execute() => _vm.AddConcept(_concept);
    public void Undo() => _vm.RemoveNode(_concept.Id);
}

/// <summary>
/// Command to add an edge between two nodes in the research graph (supports undo).
/// </summary>
public sealed class AddEdgeCommand : IGraphCommand
{
    private readonly ResearchGraphViewModel _vm;
    private readonly ScholarGraphEdge _edge;

    public string Description => $"Add edge: {_edge.RelationType}";

    public AddEdgeCommand(ResearchGraphViewModel vm, ScholarGraphEdge edge)
    {
        _vm = vm;
        _edge = edge;
    }

    public void Execute() => _vm.AddEdge(_edge);
    public void Undo()
    {
        var edgeVm = _vm.Edges.FirstOrDefault(e => e.EdgeId == _edge.Id);
        if (edgeVm != null) _vm.Edges.Remove(edgeVm);
        _vm.GetCollection().Edges.RemoveAll(e => e.Id == _edge.Id);
    }
}

/// <summary>
/// Command to remove a node and its connected edges from the research graph (supports undo).
/// </summary>
public sealed class RemoveNodeCommand : IGraphCommand
{
    private readonly ResearchGraphViewModel _vm;
    private readonly string _nodeId;
    private readonly ResearchGraphNode _savedNode;
    private readonly List<ScholarGraphEdge> _savedEdges;

    public string Description => $"Remove node: {_savedNode.Label}";

    public RemoveNodeCommand(ResearchGraphViewModel vm, string nodeId)
    {
        _vm = vm;
        _nodeId = nodeId;
        _savedNode = vm.Nodes.First(n => n.NodeId == nodeId);
        _savedEdges = vm.GetCollection().Edges
            .Where(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId)
            .ToList();
    }

    public void Execute() => _vm.RemoveNode(_nodeId);
    public void Undo()
    {
        // Restore node
        _vm.Nodes.Add(_savedNode);
        // Restore edges
        foreach (var edge in _savedEdges)
            _vm.AddEdge(edge);
    }
}

/// <summary>
/// Command to rename a concept node in the research graph (supports undo).
/// </summary>
public sealed class RenameConceptCommand : IGraphCommand
{
    private readonly ResearchGraphViewModel _vm;
    private readonly string _conceptId;
    private readonly string _oldName;
    private readonly string _newName;

    public string Description => $"Rename: {_oldName} \u2192 {_newName}";

    public RenameConceptCommand(ResearchGraphViewModel vm, string conceptId, string oldName, string newName)
    {
        _vm = vm;
        _conceptId = conceptId;
        _oldName = oldName;
        _newName = newName;
    }

    public void Execute()
    {
        var concept = _vm.GetCollection().Concepts.FirstOrDefault(c => c.Id == _conceptId);
        if (concept != null) concept.Name = _newName;
        var node = _vm.Nodes.FirstOrDefault(n => n.NodeId == _conceptId);
        if (node != null) node.Label = _newName;
    }

    public void Undo()
    {
        var concept = _vm.GetCollection().Concepts.FirstOrDefault(c => c.Id == _conceptId);
        if (concept != null) concept.Name = _oldName;
        var node = _vm.Nodes.FirstOrDefault(n => n.NodeId == _conceptId);
        if (node != null) node.Label = _oldName;
    }
}
