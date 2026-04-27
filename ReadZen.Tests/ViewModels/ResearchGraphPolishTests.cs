using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Tests for Research Graph polish changes: force layout edge cases, undo/redo
/// edge duplication fix, edge type registry coverage, ego mode, and stats.
/// </summary>
public class ResearchGraphPolishTests
{
    #region Helpers

    private static ScholarCollection CreateCollection(
        int passageCount = 2,
        int conceptCount = 1,
        List<ScholarGraphEdge>? edges = null)
    {
        var collection = new ScholarCollection
        {
            Id = "test-col",
            Name = "Test Collection",
            SchemaVersion = 2
        };

        for (int i = 0; i < passageCount; i++)
        {
            collection.Passages.Add(new ScholarPassage
            {
                Id = $"p{i}",
                ZhText = $"Chinese text {i}",
                EnText = $"English text {i}",
                MasterNames = i == 0 ? new List<string> { "Linji" } : new List<string>()
            });
        }

        for (int i = 0; i < conceptCount; i++)
        {
            collection.Concepts.Add(new ConceptNode
            {
                Id = $"c{i}",
                Name = $"Concept {i}",
                Description = "A test concept"
            });
        }

        if (edges != null)
        {
            foreach (var e in edges)
                collection.Edges.Add(e);
        }

        return collection;
    }

    private static ResearchGraphViewModel CreateVm(ScholarCollection? collection = null)
    {
        collection ??= CreateCollection();
        return new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
    }

    #endregion

    // ================================================================
    // Force Layout
    // ================================================================

    [Fact]
    public void RunForceDirectedLayout_SingleNode_NoException()
    {
        var collection = CreateCollection(passageCount: 1, conceptCount: 0);
        // Constructor calls RunForceDirectedLayout internally -- should not throw
        var vm = CreateVm(collection);

        // Also call it explicitly
        vm.RunForceDirectedLayout(800, 600);

        Assert.Single(vm.Nodes.Where(n => n.NodeType == ScholarNodeType.Passage));
    }

    [Fact]
    public void RunForceDirectedLayout_ZeroNodes_NoException()
    {
        var collection = CreateCollection(passageCount: 0, conceptCount: 0);
        var vm = CreateVm(collection);

        vm.RunForceDirectedLayout(800, 600);

        Assert.Empty(vm.Nodes);
    }

    [Fact]
    public void RunForceDirectedLayout_NodesNotAtSamePosition_AfterLayout()
    {
        var collection = CreateCollection(passageCount: 4, conceptCount: 0);
        var vm = CreateVm(collection);

        vm.RunForceDirectedLayout(800, 600);

        var positions = vm.Nodes.Select(n => (n.X, n.Y)).ToList();
        // All positions should be distinct (no two nodes exactly overlapping)
        var distinct = positions.Distinct().Count();
        Assert.Equal(positions.Count, distinct);
    }

    [Fact]
    public void RunForceDirectedLayout_GravityKeepsNodesCentered()
    {
        var collection = CreateCollection(passageCount: 5, conceptCount: 0);
        var vm = CreateVm(collection);

        vm.RunForceDirectedLayout(800, 600);

        // All nodes should be within bounds (30px margin)
        foreach (var node in vm.Nodes)
        {
            Assert.InRange(node.X, 30, 770);
            Assert.InRange(node.Y, 30, 570);
        }

        // Centroid should be roughly in the middle half of the canvas
        double avgX = vm.Nodes.Average(n => n.X);
        double avgY = vm.Nodes.Average(n => n.Y);
        Assert.InRange(avgX, 200, 600);
        Assert.InRange(avgY, 150, 450);
    }

    // ================================================================
    // RemoveNodeCommand Undo -- Edge Duplication Bug
    // ================================================================

    [Fact]
    public void RemoveNodeCommand_Undo_DoesNotDuplicateEdgesInCollection()
    {
        var vm = CreateVm(CreateCollection(passageCount: 2, conceptCount: 1));
        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });

        var cmd = new RemoveNodeCommand(vm, "c0");
        cmd.Execute();
        cmd.Undo();

        // The edge should exist exactly once in the backing collection
        int edgeCount = vm.GetCollection().Edges.Count(e => e.Id == "e1");
        Assert.Equal(1, edgeCount);
        Assert.Single(vm.Edges);
    }

    [Fact]
    public void RemoveNodeCommand_UndoRedo_PreservesEdgeCount()
    {
        var vm = CreateVm(CreateCollection(passageCount: 2, conceptCount: 1));
        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });

        var cmd = new RemoveNodeCommand(vm, "c0");

        // Execute -> Undo -> Execute -> Undo cycle
        cmd.Execute();
        Assert.Empty(vm.Edges);

        cmd.Undo();
        Assert.Single(vm.Edges);
        Assert.Single(vm.GetCollection().Edges);

        cmd.Execute();
        Assert.Empty(vm.Edges);

        cmd.Undo();
        Assert.Single(vm.Edges);
        Assert.Single(vm.GetCollection().Edges);
    }

    [Fact]
    public void RemoveNodeCommand_Undo_RestoresNodeToMap()
    {
        var vm = CreateVm(CreateCollection(passageCount: 2, conceptCount: 1));

        var cmd = new RemoveNodeCommand(vm, "c0");
        cmd.Execute();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "c0");

        cmd.Undo();

        // Node should be findable again
        Assert.Contains(vm.Nodes, n => n.NodeId == "c0");

        // Should be able to add an edge to the restored node
        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e-after-restore",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });
        Assert.Single(vm.Edges);
    }

    // ================================================================
    // Edge Type Registry Coverage
    // ================================================================

    [Fact]
    public void GetValidTypes_PassageToConcept_ReturnsEvidencesAndRefutes()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Passage, ScholarNodeType.Concept);
        var ids = types.Select(t => t.Id).ToHashSet();

        Assert.Contains("evidences", ids);
        Assert.Contains("refutes", ids);
        Assert.Equal(2, types.Count);
    }

    [Fact]
    public void GetValidTypes_ConceptToConcept_ReturnsSubsumesOpposesRelated()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Concept, ScholarNodeType.Concept);
        var ids = types.Select(t => t.Id).ToHashSet();

        Assert.Contains("subsumes", ids);
        Assert.Contains("opposes", ids);
        Assert.Contains("related-to", ids);
        Assert.Equal(3, types.Count);
    }

    [Fact]
    public void GetValidTypes_PassageToMaster_ReturnsAttributedTo()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Passage, ScholarNodeType.ZenMaster);
        var ids = types.Select(t => t.Id).ToHashSet();

        Assert.Contains("attributed-to", ids);
        Assert.Single(types);
    }

    [Fact]
    public void GetValidTypes_MasterToMaster_ReturnsTeacherOfAndSameSchool()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.ZenMaster, ScholarNodeType.ZenMaster);
        var ids = types.Select(t => t.Id).ToHashSet();

        Assert.Contains("teacher-of", ids);
        Assert.Contains("same-school", ids);
        Assert.Equal(2, types.Count);
    }

    [Fact]
    public void GetValidTypes_TermToTerm_ReturnsEmpty()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.TermbaseEntry, ScholarNodeType.TermbaseEntry);
        Assert.Empty(types);
    }

    // ================================================================
    // Concept Status + Node Lifecycle
    // ================================================================

    [Fact]
    public void AddConcept_WithDeprecatedStatus_StillAdded()
    {
        var vm = CreateVm(CreateCollection(passageCount: 1, conceptCount: 0));
        var concept = new ConceptNode
        {
            Id = "c-dep",
            Name = "Old Concept",
            Description = "Deprecated",
            Status = ConceptStatus.Deprecated
        };

        vm.AddConcept(concept);

        Assert.Contains(vm.Nodes, n => n.NodeId == "c-dep");
        Assert.Contains(vm.GetCollection().Concepts, c => c.Id == "c-dep" && c.Status == ConceptStatus.Deprecated);
    }

    [Fact]
    public void ExecuteCommand_ThenUndo_ThenNewAction_ClearsRedoStack()
    {
        var vm = CreateVm();

        // Execute first command
        var c1 = new ConceptNode { Id = "c-first", Name = "First", Description = "" };
        vm.ExecuteCommand(new AddConceptCommand(vm, c1));
        Assert.True(vm.CanUndo);

        // Undo it -- should enable redo
        vm.Undo();
        Assert.True(vm.CanRedo);

        // Execute a new (different) command -- should clear redo stack
        var c2 = new ConceptNode { Id = "c-second", Name = "Second", Description = "" };
        vm.ExecuteCommand(new AddConceptCommand(vm, c2));

        Assert.False(vm.CanRedo);
        Assert.True(vm.CanUndo);
    }

    [Fact]
    public void RestoreNodeToMap_AfterRemoval_NodeFindable()
    {
        var vm = CreateVm(CreateCollection(passageCount: 2, conceptCount: 1));

        // Remove c0
        vm.RemoveNode("c0");
        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "c0");

        // Manually restore
        var restoredNode = new ResearchGraphNode
        {
            NodeId = "c0",
            NodeType = ScholarNodeType.Concept,
            Label = "Concept 0"
        };
        vm.Nodes.Add(restoredNode);
        vm.RestoreNodeToMap(restoredNode);

        // Should now be able to add edges to c0
        vm.GetCollection().Concepts.Add(new ConceptNode { Id = "c0", Name = "Concept 0", Description = "" });
        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e-restored",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });
        Assert.Single(vm.Edges);
    }

    // ================================================================
    // Ego Mode
    // ================================================================

    [Fact]
    public void SetEgoMode_ValidNode_ConnectedNodesNotDimmed()
    {
        var edges = new List<ScholarGraphEdge>
        {
            new() { Id = "e1", FromNodeId = "p0", ToNodeId = "p1", RelationType = "quotes" }
        };
        var vm = CreateVm(CreateCollection(passageCount: 3, conceptCount: 0, edges: edges));

        vm.SetEgoMode("p0");

        var p0 = vm.Nodes.First(n => n.NodeId == "p0");
        var p1 = vm.Nodes.First(n => n.NodeId == "p1");

        Assert.False(p0.IsDimmed);
        Assert.False(p1.IsDimmed);
    }

    [Fact]
    public void SetEgoMode_ValidNode_UnconnectedNodesDimmed()
    {
        var edges = new List<ScholarGraphEdge>
        {
            new() { Id = "e1", FromNodeId = "p0", ToNodeId = "p1", RelationType = "quotes" }
        };
        var vm = CreateVm(CreateCollection(passageCount: 3, conceptCount: 0, edges: edges));

        vm.SetEgoMode("p0");

        var p2 = vm.Nodes.First(n => n.NodeId == "p2");
        Assert.True(p2.IsDimmed);
    }

    [Fact]
    public void SetEgoMode_Null_AllNodesUndimmed()
    {
        var edges = new List<ScholarGraphEdge>
        {
            new() { Id = "e1", FromNodeId = "p0", ToNodeId = "p1", RelationType = "quotes" }
        };
        var vm = CreateVm(CreateCollection(passageCount: 3, conceptCount: 0, edges: edges));

        vm.SetEgoMode("p0");
        // p2 should be dimmed
        Assert.True(vm.Nodes.First(n => n.NodeId == "p2").IsDimmed);

        vm.SetEgoMode(null);
        Assert.All(vm.Nodes, n => Assert.False(n.IsDimmed));
    }

    [Fact]
    public void SetEgoMode_NonExistentId_NoChange()
    {
        var vm = CreateVm(CreateCollection(passageCount: 2, conceptCount: 0));

        // Should be a no-op, no exceptions
        vm.SetEgoMode("does-not-exist");

        Assert.All(vm.Nodes, n => Assert.False(n.IsDimmed));
    }

    // ================================================================
    // Stats
    // ================================================================

    [Fact]
    public void ComputeStats_OrphanPassage_CountedCorrectly()
    {
        // 3 passages, 1 concept; only p0 connected to c0
        var edges = new List<ScholarGraphEdge>
        {
            new() { Id = "e1", FromNodeId = "p0", ToNodeId = "c0", RelationType = "evidences" }
        };
        var vm = CreateVm(CreateCollection(passageCount: 3, conceptCount: 1, edges: edges));

        // p1 and p2 are orphans
        Assert.Equal(2, vm.OrphanPassageCount);
    }

    [Fact]
    public void ComputeStats_OverloadedConcept_AboveThreshold_Counted()
    {
        // Create a concept with > 8 edges (threshold is > 8)
        var collection = CreateCollection(passageCount: 10, conceptCount: 1);
        for (int i = 0; i < 10; i++)
        {
            collection.Edges.Add(new ScholarGraphEdge
            {
                Id = $"e{i}",
                FromNodeId = $"p{i}",
                ToNodeId = "c0",
                RelationType = "evidences"
            });
        }

        var vm = CreateVm(collection);

        // c0 has degree 10 > 8, so it's overloaded
        Assert.Equal(1, vm.OverloadedConceptCount);
    }

    [Fact]
    public void ComputeStats_QualityScore_DecreasesWithOrphans()
    {
        // All nodes disconnected = many orphans = lower quality
        var vmDisconnected = CreateVm(CreateCollection(passageCount: 5, conceptCount: 3));

        // All connected
        var collection = CreateCollection(passageCount: 2, conceptCount: 1);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1", FromNodeId = "p0", ToNodeId = "c0", RelationType = "evidences"
        });
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e2", FromNodeId = "p1", ToNodeId = "c0", RelationType = "evidences"
        });
        var vmConnected = CreateVm(collection);

        Assert.True(vmConnected.QualityScore > vmDisconnected.QualityScore,
            $"Connected score ({vmConnected.QualityScore}) should be higher than disconnected ({vmDisconnected.QualityScore})");
    }
}
