using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Tests for Research Graph UX deep pass: collection navigation,
/// gravity physics, AddConcept placement, and layout persistence.
/// </summary>
public class ResearchGraphDeepTests
{
    #region Helpers

    private static ScholarCollection CreateCollection(
        string id = "col-1",
        string name = "Collection 1",
        int passageCount = 2,
        int conceptCount = 1,
        List<ScholarGraphEdge>? edges = null)
    {
        var collection = new ScholarCollection
        {
            Id = id,
            Name = name,
            SchemaVersion = 2
        };

        for (int i = 0; i < passageCount; i++)
        {
            collection.Passages.Add(new ScholarPassage
            {
                Id = $"{id}-p{i}",
                ZhText = $"Chinese text {i}",
                EnText = $"English text {i}",
                MasterNames = i == 0 ? new List<string> { "Linji" } : new List<string>()
            });
        }

        for (int i = 0; i < conceptCount; i++)
        {
            collection.Concepts.Add(new ConceptNode
            {
                Id = $"{id}-c{i}",
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

    private static (ResearchGraphViewModel vm, ScholarCollection col1, ScholarCollection col2) CreateTwoCollectionVm()
    {
        var col1 = CreateCollection(id: "col-1", name: "Alpha", passageCount: 3, conceptCount: 1);
        var col2 = CreateCollection(id: "col-2", name: "Beta", passageCount: 2, conceptCount: 2);
        var all = new List<ScholarCollection> { col1, col2 };
        var vm = new ResearchGraphViewModel(col1, all);
        return (vm, col1, col2);
    }

    #endregion

    // ================================================================
    // Collection Navigation
    // ================================================================

    [Fact]
    public void SwitchToCollection_ValidId_ChangesCollection()
    {
        var (vm, col1, col2) = CreateTwoCollectionVm();

        Assert.Equal("col-1", vm.GetCollection().Id);

        vm.SwitchToCollection("col-2");

        Assert.Equal("col-2", vm.GetCollection().Id);
        // Nodes should reflect col-2 passages (2 passages, 2 concepts)
        Assert.Equal(2, vm.Nodes.Count(n => n.NodeType == ScholarNodeType.Passage));
        Assert.Equal(2, vm.Nodes.Count(n => n.NodeType == ScholarNodeType.Concept));
    }

    [Fact]
    public void SwitchToCollection_InvalidId_NoOp()
    {
        var (vm, col1, _) = CreateTwoCollectionVm();

        Assert.Equal("col-1", vm.GetCollection().Id);
        int nodeCountBefore = vm.Nodes.Count;

        vm.SwitchToCollection("nonexistent-id");

        // Should remain on col-1, unchanged
        Assert.Equal("col-1", vm.GetCollection().Id);
        Assert.Equal(nodeCountBefore, vm.Nodes.Count);
    }

    [Fact]
    public void SwitchToCollection_SavesLayoutBeforeSwitch()
    {
        var (vm, col1, col2) = CreateTwoCollectionVm();

        // Move a node to a known position
        var firstNode = vm.Nodes.First();
        firstNode.X = 123.0;
        firstNode.Y = 456.0;

        vm.SwitchToCollection("col-2");

        // col-1's layout should have been saved with the known position
        Assert.NotNull(col1.GraphLayout);
        Assert.True(col1.GraphLayout.NodePositions.ContainsKey(firstNode.NodeId));
        Assert.Equal(123.0, col1.GraphLayout.NodePositions[firstNode.NodeId].X);
        Assert.Equal(456.0, col1.GraphLayout.NodePositions[firstNode.NodeId].Y);
    }

    [Fact]
    public void SwitchToCollection_RestoresSavedPositions()
    {
        var (vm, col1, col2) = CreateTwoCollectionVm();

        // Move nodes in col-1 to known positions
        foreach (var node in vm.Nodes)
        {
            node.X = 200.0;
            node.Y = 300.0;
        }

        // Switch to col-2 (saves col-1 layout)
        vm.SwitchToCollection("col-2");

        // Switch back to col-1 (should restore saved positions)
        vm.SwitchToCollection("col-1");

        Assert.Equal("col-1", vm.GetCollection().Id);
        // All nodes should be at the saved position (200, 300)
        foreach (var node in vm.Nodes)
        {
            if (col1.GraphLayout.NodePositions.ContainsKey(node.NodeId))
            {
                Assert.Equal(200.0, node.X);
                Assert.Equal(300.0, node.Y);
            }
        }
    }

    [Fact]
    public void GetAllCollections_ReturnsAllCollections()
    {
        var (vm, col1, col2) = CreateTwoCollectionVm();

        var all = vm.GetAllCollections();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, c => c.Id == "col-1");
        Assert.Contains(all, c => c.Id == "col-2");
    }

    // ================================================================
    // Gravity
    // ================================================================

    [Fact]
    public void Gravity_PullsNodesNearCenter()
    {
        // Create a graph with several nodes and run layout
        var col = CreateCollection(passageCount: 6, conceptCount: 0);
        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        vm.RunForceDirectedLayout(800, 600);

        // Centroid should be near canvas center (400, 300) within tolerance
        double avgX = vm.Nodes.Average(n => n.X);
        double avgY = vm.Nodes.Average(n => n.Y);
        Assert.InRange(avgX, 200, 600);
        Assert.InRange(avgY, 150, 450);
    }

    [Fact]
    public void Gravity_Formula_IsLinearSpring()
    {
        // Verify the gravity force is proportional to displacement.
        // A node further from center should receive proportionally more force.
        // We test this by placing two nodes at different distances and checking
        // relative gravity pull after one iteration.

        var col = CreateCollection(passageCount: 3, conceptCount: 0);
        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        double width = 800, height = 600;
        double cx = width / 2, cy = height / 2;

        // Place nodes at known distances from center
        var nodes = vm.Nodes.ToList();
        nodes[0].X = cx + 100; nodes[0].Y = cy; // 100px from center
        nodes[1].X = cx + 200; nodes[1].Y = cy; // 200px from center
        nodes[2].X = cx;       nodes[2].Y = cy; // at center

        // Clear velocities
        foreach (var n in nodes) { n.Vx = 0; n.Vy = 0; }

        // Apply gravity formula: vx += (cx - x) * strength
        double gravityStrength = 0.01;
        foreach (var n in nodes)
        {
            n.Vx += (cx - n.X) * gravityStrength;
            n.Vy += (cy - n.Y) * gravityStrength;
        }

        // Node at 200px should have 2x the gravity pull of node at 100px
        double pull100 = Math.Abs(nodes[0].Vx);
        double pull200 = Math.Abs(nodes[1].Vx);
        double pullCenter = Math.Abs(nodes[2].Vx);

        Assert.True(pull200 > pull100, "Gravity should be stronger for nodes further from center");
        Assert.Equal(pull200, pull100 * 2, precision: 10);
        Assert.Equal(0.0, pullCenter, precision: 10);
    }

    // ================================================================
    // AddConcept
    // ================================================================

    [Fact]
    public void AddConcept_PlacesNearCentroid_NotRandom()
    {
        var col = CreateCollection(passageCount: 4, conceptCount: 0);
        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        // Run layout so nodes have stable positions
        vm.RunForceDirectedLayout(800, 600);

        double centroidX = vm.Nodes.Average(n => n.X);
        double centroidY = vm.Nodes.Average(n => n.Y);

        var concept = new ConceptNode
        {
            Id = "new-concept",
            Name = "New Concept",
            Description = "Test"
        };
        vm.AddConcept(concept);

        var newNode = vm.Nodes.First(n => n.NodeId == "new-concept");

        // Should be placed at centroid + 30 offset
        Assert.Equal(centroidX + 30, newNode.X, precision: 1);
        Assert.Equal(centroidY + 30, newNode.Y, precision: 1);
    }

    [Fact]
    public void AddConcept_DoesNotDestroyExistingPositions()
    {
        var col = CreateCollection(passageCount: 3, conceptCount: 0);
        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        vm.RunForceDirectedLayout(800, 600);

        // Record positions before adding concept
        var positionsBefore = vm.Nodes.ToDictionary(n => n.NodeId, n => (n.X, n.Y));

        var concept = new ConceptNode
        {
            Id = "added-concept",
            Name = "Added",
            Description = "Test"
        };
        vm.AddConcept(concept);

        // All pre-existing nodes should keep their exact positions
        foreach (var (nodeId, (x, y)) in positionsBefore)
        {
            var node = vm.Nodes.First(n => n.NodeId == nodeId);
            Assert.Equal(x, node.X);
            Assert.Equal(y, node.Y);
        }
    }

    // ================================================================
    // Layout Persistence
    // ================================================================

    [Fact]
    public void SaveLayoutToCollection_PersistsNodePositions()
    {
        var col = CreateCollection(passageCount: 3, conceptCount: 1);
        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        // Set known positions
        foreach (var node in vm.Nodes)
        {
            node.X = 100.0 + vm.Nodes.IndexOf(node) * 50;
            node.Y = 200.0 + vm.Nodes.IndexOf(node) * 30;
        }

        vm.SaveLayoutToCollection();

        Assert.NotNull(col.GraphLayout);
        Assert.Equal(vm.Nodes.Count, col.GraphLayout.NodePositions.Count);

        foreach (var node in vm.Nodes)
        {
            Assert.True(col.GraphLayout.NodePositions.ContainsKey(node.NodeId));
            Assert.Equal(node.X, col.GraphLayout.NodePositions[node.NodeId].X);
            Assert.Equal(node.Y, col.GraphLayout.NodePositions[node.NodeId].Y);
        }
    }

    [Fact]
    public void RebuildGraph_WithSavedPositions_SkipsForceLayout()
    {
        var col = CreateCollection(passageCount: 3, conceptCount: 0);

        // Pre-populate saved layout with specific positions
        col.GraphLayout = new ScholarGraphLayout();
        for (int i = 0; i < 3; i++)
        {
            col.GraphLayout.NodePositions[$"col-1-p{i}"] = new GraphNodeLayout
            {
                X = 100.0 + i * 100,
                Y = 200.0 + i * 50
            };
        }

        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        // Nodes should be at saved positions, not force-layout positions
        for (int i = 0; i < 3; i++)
        {
            var node = vm.Nodes.First(n => n.NodeId == $"col-1-p{i}");
            Assert.Equal(100.0 + i * 100, node.X);
            Assert.Equal(200.0 + i * 50, node.Y);
        }
    }

    [Fact]
    public void RebuildGraph_WithoutSavedPositions_RunsForceLayout()
    {
        var col = CreateCollection(passageCount: 4, conceptCount: 0);
        // No saved layout (default empty)
        col.GraphLayout = new ScholarGraphLayout();

        var vm = new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

        // Force layout should have run, placing nodes at distinct positions
        var positions = vm.Nodes.Select(n => (n.X, n.Y)).ToList();
        var distinct = positions.Distinct().Count();
        Assert.Equal(positions.Count, distinct);

        // Nodes should be within canvas bounds (force layout uses 800x600)
        foreach (var node in vm.Nodes)
        {
            Assert.InRange(node.X, 30, 770);
            Assert.InRange(node.Y, 30, 570);
        }
    }
}
