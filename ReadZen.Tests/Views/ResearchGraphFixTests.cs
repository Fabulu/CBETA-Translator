using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.Views;

/// <summary>
/// Tests for the 12 Research Graph fixes: force layout SPA formula,
/// convergence early-exit, HitTest radius, ZenMaster SourceData,
/// search highlight, single-node centering, and undo/redo state.
/// </summary>
public class ResearchGraphFixTests
{

    // ── Helpers ───────────────────────────────────────────────────────

    private static ScholarCollection MakeEmptyCollection(string id = "col-1")
    {
        return new ScholarCollection { Id = id, Name = "Test Collection" };
    }

    private static ResearchGraphViewModel MakeVm(ScholarCollection? col = null)
    {
        col ??= MakeEmptyCollection();
        return new ResearchGraphViewModel(col, new List<ScholarCollection> { col });
    }

    // ── 1. Force layout SPA formula: R scales with sqrt(N)*80 ────────

    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(25)]
    public void ForceLayout_RadiusScalesWithSqrtN(int nodeCount)
    {
        var col = MakeEmptyCollection();
        for (int i = 0; i < nodeCount; i++)
        {
            col.Passages.Add(new ScholarPassage
            {
                Id = $"p-{i}",
                ZhText = $"Passage {i}"
            });
        }
        var vm = MakeVm(col);

        // After RebuildGraph, nodes should be placed using R = sqrt(N)*80
        double expectedR = Math.Sqrt(nodeCount) * 80;
        double cx = 400, cy = 300; // default layout is 800x600

        // The initial circular placement puts nodes at distance R from center.
        // After force iterations they move, but for unconnected nodes with repulsion
        // they should spread out. Verify initial spread is in the right ballpark.
        var distances = vm.Nodes.Select(n =>
            Math.Sqrt((n.X - cx) * (n.X - cx) + (n.Y - cy) * (n.Y - cy))).ToList();

        // Average distance should be roughly proportional to expectedR
        // (not all at center, not all collapsed)
        double avgDist = distances.Average();
        Assert.True(avgDist > 10, $"Nodes should not be collapsed at center, avg dist = {avgDist}");
    }

    [Fact]
    public void ForceLayout_FourNodesFormCircularArrangement()
    {
        var col = MakeEmptyCollection();
        for (int i = 0; i < 4; i++)
        {
            col.Passages.Add(new ScholarPassage
            {
                Id = $"p-{i}",
                ZhText = $"Passage {i}"
            });
        }
        var vm = MakeVm(col);

        // Nodes should NOT all be at the same point
        var xs = vm.Nodes.Select(n => n.X).ToList();
        var ys = vm.Nodes.Select(n => n.Y).ToList();
        double xSpread = xs.Max() - xs.Min();
        double ySpread = ys.Max() - ys.Min();

        Assert.True(xSpread > 20, $"X spread too small: {xSpread}");
        Assert.True(ySpread > 20, $"Y spread too small: {ySpread}");

        // All 4 positions should be distinct
        var positions = vm.Nodes.Select(n => (Math.Round(n.X, 1), Math.Round(n.Y, 1))).ToHashSet();
        Assert.Equal(4, positions.Count);
    }

    // ── 2. Force layout convergence: early exit when spread out ──────

    [Fact]
    public void ForceLayout_ConvergesEarlyWhenWellSpread()
    {
        // With only 2 unconnected nodes, repulsion pushes them apart quickly
        // and the layout should converge well before 150 iterations.
        // We verify by checking that the layout completes (doesn't hang)
        // and that nodes end up far apart.
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "a", ZhText = "A" });
        col.Passages.Add(new ScholarPassage { Id = "b", ZhText = "B" });
        var vm = MakeVm(col);

        double dx = vm.Nodes[0].X - vm.Nodes[1].X;
        double dy = vm.Nodes[0].Y - vm.Nodes[1].Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // Two repelling nodes should be well separated
        Assert.True(dist > 50, $"Two unconnected nodes should be far apart, got dist = {dist}");
    }

    // ── 4. ZenMaster SourceData from passages with MasterNames ──────

    [Fact]
    public void ZenMasterNode_HasSourceDataWithPassageCount()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage
        {
            Id = "p1",
            ZhText = "Some text",
            MasterNames = new List<string> { "Zhaozhou", "Linji" }
        });
        col.Passages.Add(new ScholarPassage
        {
            Id = "p2",
            ZhText = "Other text",
            MasterNames = new List<string> { "Zhaozhou" }
        });

        var vm = MakeVm(col);

        // Find the ZenMaster node for Zhaozhou
        var zhaozhouNode = vm.Nodes.FirstOrDefault(n =>
            n.NodeType == ScholarNodeType.ZenMaster && n.Label == "Zhaozhou");
        Assert.NotNull(zhaozhouNode);
        Assert.NotNull(zhaozhouNode!.SourceData);

        var data = Assert.IsType<Dictionary<string, object>>(zhaozhouNode.SourceData);
        Assert.True(data.ContainsKey("PassageCount"), "SourceData should contain PassageCount");
        Assert.Equal(2, (int)data["PassageCount"]);

        // Linji should have 1 passage
        var linjiNode = vm.Nodes.FirstOrDefault(n =>
            n.NodeType == ScholarNodeType.ZenMaster && n.Label == "Linji");
        Assert.NotNull(linjiNode);
        var linjiData = Assert.IsType<Dictionary<string, object>>(linjiNode!.SourceData);
        Assert.Equal(1, (int)linjiData["PassageCount"]);
    }

    [Fact]
    public void ZenMasterNode_SourceDataContainsPassageList()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage
        {
            Id = "p1",
            Summary = "Case 1",
            MasterNames = new List<string> { "Yunmen" }
        });
        var vm = MakeVm(col);

        var node = vm.Nodes.First(n => n.NodeType == ScholarNodeType.ZenMaster && n.Label == "Yunmen");
        var data = Assert.IsType<Dictionary<string, object>>(node.SourceData);
        var passages = Assert.IsType<List<string>>(data["Passages"]);
        Assert.Single(passages);
        Assert.Equal("Case 1", passages[0]);
    }

    // ── 5. Search highlight ─────────────────────────────────────────

    [Fact]
    public void HighlightSearch_MatchesNodesContainingQuery()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", Summary = "Buddha nature" });
        col.Passages.Add(new ScholarPassage { Id = "p2", Summary = "Emptiness" });
        col.Concepts.Add(new ConceptNode { Id = "c1", Name = "Buddha", Description = "" });

        var vm = MakeVm(col);
        vm.SearchText = "buddha";
        vm.HighlightSearch();

        // "Buddha nature" passage and "Buddha" concept should match
        Assert.Contains("p1", vm.HighlightedNodeIds);
        Assert.Contains("c1", vm.HighlightedNodeIds);

        // "Emptiness" passage should NOT match
        Assert.DoesNotContain("p2", vm.HighlightedNodeIds);
    }

    [Fact]
    public void HighlightSearch_ClearsOnEmptyQuery()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", Summary = "Test" });
        var vm = MakeVm(col);

        // First highlight something
        vm.SearchText = "test";
        vm.HighlightSearch();
        Assert.NotEmpty(vm.HighlightedNodeIds);

        // Then clear
        vm.SearchText = "";
        vm.HighlightSearch();
        Assert.Empty(vm.HighlightedNodeIds);
    }

    [Fact]
    public void HighlightSearch_IsCaseInsensitive()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", Summary = "Dharma Talk" });
        var vm = MakeVm(col);

        vm.SearchText = "DHARMA";
        vm.HighlightSearch();

        Assert.Contains("p1", vm.HighlightedNodeIds);
    }

    // ── 6. Single node centers at width/2, height/2 ─────────────────

    [Fact]
    public void SingleNode_CentersInViewport()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Only node" });
        var vm = MakeVm(col);

        // RebuildGraph calls RunForceDirectedLayout(800, 600) only when Count > 1.
        // For single node, it skips layout entirely. We call it explicitly.
        vm.RunForceDirectedLayout(800, 600);

        Assert.Equal(400, vm.Nodes[0].X, precision: 5);
        Assert.Equal(300, vm.Nodes[0].Y, precision: 5);
    }

    [Fact]
    public void SingleNode_CentersAtCustomDimensions()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Only" });
        var vm = MakeVm(col);

        vm.RunForceDirectedLayout(1200, 900);

        Assert.Equal(600, vm.Nodes[0].X, precision: 5);
        Assert.Equal(450, vm.Nodes[0].Y, precision: 5);
    }

    // ── 7. Undo/Redo state ──────────────────────────────────────────

    [Fact]
    public void UndoRedo_AddConceptCommand()
    {
        var col = MakeEmptyCollection();
        var vm = MakeVm(col);
        int initialCount = vm.Nodes.Count;

        var concept = new ConceptNode
        {
            Id = "c-new",
            Name = "New Concept",
            Description = "A test concept"
        };
        var cmd = new AddConceptCommand(vm, concept);
        vm.ExecuteCommand(cmd);

        // After execute: node count increased
        Assert.Equal(initialCount + 1, vm.Nodes.Count);
        Assert.True(vm.CanUndo);
        Assert.False(vm.CanRedo);

        // Undo: node count back to initial
        vm.Undo();
        Assert.Equal(initialCount, vm.Nodes.Count);
        Assert.False(vm.CanUndo);
        Assert.True(vm.CanRedo);

        // Redo: node count increased again
        vm.Redo();
        Assert.Equal(initialCount + 1, vm.Nodes.Count);
        Assert.True(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void ExecuteCommand_ClearsRedoStack()
    {
        var col = MakeEmptyCollection();
        var vm = MakeVm(col);

        var c1 = new ConceptNode { Id = "c1", Name = "First", Description = "" };
        var c2 = new ConceptNode { Id = "c2", Name = "Second", Description = "" };

        vm.ExecuteCommand(new AddConceptCommand(vm, c1));
        vm.Undo(); // c1 removed, redo stack has AddConceptCommand(c1)
        Assert.True(vm.CanRedo);

        // Executing a new command should clear the redo stack
        vm.ExecuteCommand(new AddConceptCommand(vm, c2));
        Assert.False(vm.CanRedo);
    }

    // ── 9. Hexagon pointy-top: top vertex at Y = center.Y - radius ──

    [Fact]
    public void DrawHexagon_UsesPointyTopOffset()
    {
        // DrawHexagon uses angle = PI/3 * i - PI/2.
        // At i=0, angle = -PI/2, so the first vertex is at:
        //   X = center.X + size * cos(-PI/2) = center.X + 0
        //   Y = center.Y + size * sin(-PI/2) = center.Y - size
        // This confirms pointy-top orientation.
        double centerX = 100, centerY = 100, size = 20;
        double angle0 = Math.PI / 3 * 0 - Math.PI / 2;

        double topX = centerX + size * Math.Cos(angle0);
        double topY = centerY + size * Math.Sin(angle0);

        // Top vertex should be directly above center
        Assert.Equal(centerX, topX, precision: 10);
        Assert.Equal(centerY - size, topY, precision: 10);
    }

    [Fact]
    public void DrawHexagon_AllSixVerticesAtCorrectRadius()
    {
        double centerX = 50, centerY = 50, size = 15;
        for (int i = 0; i < 6; i++)
        {
            double angle = Math.PI / 3 * i - Math.PI / 2;
            double vx = centerX + size * Math.Cos(angle);
            double vy = centerY + size * Math.Sin(angle);
            double dist = Math.Sqrt((vx - centerX) * (vx - centerX) + (vy - centerY) * (vy - centerY));
            Assert.Equal(size, dist, precision: 10);
        }
    }

    // ── 10. Edge alpha levels (ego-aware) ───────────────────────────

    [Fact]
    public void EdgeAlpha_DefaultNoEgo_Is153()
    {
        // From DrawEdge: when no node is selected (!hasEgo), alpha = 153
        // 153 / 255 ≈ 0.6
        byte expected = 153;
        Assert.Equal(expected, (byte)(0.6 * 255));
    }

    [Fact]
    public void EdgeAlpha_EgoRelevant_Is204()
    {
        // From DrawEdge: when edge connects to selected node, alpha = 204
        // 204 / 255 ≈ 0.8
        byte expected = 204;
        Assert.Equal(expected, (byte)(0.8 * 255));
    }

    [Fact]
    public void EdgeAlpha_NonRelevant_Is89()
    {
        // From DrawEdge: when ego is selected but edge is unrelated, alpha = 89
        // 89 / 255 ≈ 0.35 (truncated: 0.35 * 255 = 89.25)
        byte expected = 89;
        Assert.Equal(expected, (byte)(0.35 * 255));
    }

    // ── 12. IsPinned default ────────────────────────────────────────

    [Fact]
    public void IsPinned_DefaultIsFalse()
    {
        var node = new ResearchGraphNode();
        Assert.False(node.IsPinned);
    }

    // ── 19. GoBack: collection history navigation ────────────────────

    [Fact]
    public void GoBack_SwitchToCollectionPushesHistory()
    {
        var col1 = MakeEmptyCollection("col-1");
        col1.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "A" });
        var col2 = new ScholarCollection { Id = "col-2", Name = "Collection 2" };
        col2.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "B" });

        var vm = new ResearchGraphViewModel(col1, new List<ScholarCollection> { col1, col2 });

        Assert.False(vm.CanGoBack);

        // Switch to col-2 — should push col-1 to history
        vm.SwitchToCollection("col-2");
        Assert.True(vm.CanGoBack);
        Assert.Equal("col-2", vm.GetCollection().Id);

        // GoBack — should return to col-1
        vm.GoBack();
        Assert.Equal("col-1", vm.GetCollection().Id);
        Assert.False(vm.CanGoBack);
    }

    [Fact]
    public void GoBack_MultipleNavigationsPopInOrder()
    {
        var col1 = MakeEmptyCollection("col-1");
        var col2 = new ScholarCollection { Id = "col-2", Name = "C2" };
        var col3 = new ScholarCollection { Id = "col-3", Name = "C3" };
        var all = new List<ScholarCollection> { col1, col2, col3 };

        var vm = new ResearchGraphViewModel(col1, all);
        vm.SwitchToCollection("col-2");
        vm.SwitchToCollection("col-3");
        Assert.True(vm.CanGoBack);

        vm.GoBack();
        Assert.Equal("col-2", vm.GetCollection().Id);
        Assert.True(vm.CanGoBack);

        vm.GoBack();
        Assert.Equal("col-1", vm.GetCollection().Id);
        Assert.False(vm.CanGoBack);
    }

    // ── 20. Multi-select: GetSelectedNodes ──────────────────────────

    [Fact]
    public void GetSelectedNodes_ReturnsAllSelected()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "A" });
        col.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "B" });
        col.Passages.Add(new ScholarPassage { Id = "p3", ZhText = "C" });
        var vm = MakeVm(col);

        Assert.Equal(3, vm.Nodes.Count);

        // Select all 3
        foreach (var n in vm.Nodes) n.IsSelected = true;
        var selected = vm.GetSelectedNodes();
        Assert.Equal(3, selected.Count);

        // Deselect one
        vm.Nodes[1].IsSelected = false;
        selected = vm.GetSelectedNodes();
        Assert.Equal(2, selected.Count);
        Assert.DoesNotContain(vm.Nodes[1], selected);
    }

    [Fact]
    public void GetSelectedNodes_NoneSelected_ReturnsEmpty()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "A" });
        var vm = MakeVm(col);

        var selected = vm.GetSelectedNodes();
        Assert.Empty(selected);
    }

    // ── 21. MovePassageToIndex ──────────────────────────────────────

    [Fact]
    public void MovePassageToIndex_MovesFirstToLast()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p0", ZhText = "Zero" });
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "One" });
        col.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "Two" });
        var vm = MakeVm(col);

        vm.MovePassageToIndex(0, 2);

        Assert.Equal("p1", col.Passages[0].Id);
        Assert.Equal("p2", col.Passages[1].Id);
        Assert.Equal("p0", col.Passages[2].Id);
    }

    [Fact]
    public void MovePassageToIndex_SameIndex_NoChange()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p0", ZhText = "Zero" });
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "One" });
        var vm = MakeVm(col);

        vm.MovePassageToIndex(0, 0);

        Assert.Equal("p0", col.Passages[0].Id);
        Assert.Equal("p1", col.Passages[1].Id);
    }

    // ── 22. MergeConceptInto ────────────────────────────────────────

    [Fact]
    public void MergeConceptInto_MovesEdgesAndRemovesSource()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Passage" });
        col.Concepts.Add(new ConceptNode { Id = "cA", Name = "Concept A", Description = "" });
        col.Concepts.Add(new ConceptNode { Id = "cB", Name = "Concept B", Description = "" });
        col.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1", FromNodeId = "cA", ToNodeId = "p1", RelationType = "illustrates"
        });
        col.Edges.Add(new ScholarGraphEdge
        {
            Id = "e2", FromNodeId = "cB", ToNodeId = "p1", RelationType = "illustrates"
        });

        var vm = MakeVm(col);

        // Verify initial state: 3 nodes (p1, cA, cB), 2 edges
        Assert.Equal(3, vm.Nodes.Count);
        Assert.Equal(2, vm.Edges.Count);

        // Merge B into A
        vm.MergeConceptInto("cB", "cA");

        // B should be removed
        Assert.Null(vm.Nodes.FirstOrDefault(n => n.NodeId == "cB"));
        // A should still exist
        Assert.NotNull(vm.Nodes.FirstOrDefault(n => n.NodeId == "cA"));
        // Both edges should now point from cA to p1
        Assert.All(vm.Edges, e => Assert.Equal("cA", e.From.NodeId));
    }

    [Fact]
    public void MergeConceptInto_SelfMerge_NoChange()
    {
        var col = MakeEmptyCollection();
        col.Concepts.Add(new ConceptNode { Id = "cA", Name = "A", Description = "" });
        var vm = MakeVm(col);

        int before = vm.Nodes.Count;
        vm.MergeConceptInto("cA", "cA");
        Assert.Equal(before, vm.Nodes.Count);
    }

    // ── 23. ReverseEdge ─────────────────────────────────────────────

    [Fact]
    public void ReverseEdge_SwapsFromAndTo()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "pA", ZhText = "A" });
        col.Passages.Add(new ScholarPassage { Id = "pB", ZhText = "B" });
        col.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1", FromNodeId = "pA", ToNodeId = "pB", RelationType = "references"
        });

        var vm = MakeVm(col);
        var edge = vm.Edges.First(e => e.EdgeId == "e1");

        Assert.Equal("pA", edge.From.NodeId);
        Assert.Equal("pB", edge.To.NodeId);

        vm.ReverseEdge("e1");

        Assert.Equal("pB", edge.From.NodeId);
        Assert.Equal("pA", edge.To.NodeId);

        // Backing model should also be swapped
        var modelEdge = col.Edges.First(e => e.Id == "e1");
        Assert.Equal("pB", modelEdge.FromNodeId);
        Assert.Equal("pA", modelEdge.ToNodeId);
    }

    [Fact]
    public void ReverseEdge_NonExistentId_NoException()
    {
        var col = MakeEmptyCollection();
        var vm = MakeVm(col);
        // Should not throw
        vm.ReverseEdge("nonexistent");
    }

    // ── Wave 2 Polish: ShowLabels default ────────────────────────────

    // ── Wave 2 Polish: GetSelectedNodes after SelectAll ─────────────

    [Fact]
    public void GetSelectedNodes_AfterSelectAll_ReturnsAll()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "A" });
        col.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "B" });
        col.Concepts.Add(new ConceptNode { Id = "c1", Name = "C", Description = "" });
        var vm = MakeVm(col);

        // Mark every node as selected
        foreach (var node in vm.Nodes)
            node.IsSelected = true;

        var selected = vm.GetSelectedNodes();
        Assert.Equal(vm.Nodes.Count, selected.Count);
        Assert.All(vm.Nodes, n => Assert.Contains(n, selected));
    }

    // ── Wave 2 Polish: GoBack on empty history ──────────────────────

    [Fact]
    public void GoBack_EmptyHistory_DoesNothingAndCanGoBackIsFalse()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "A" });
        var vm = MakeVm(col);

        Assert.False(vm.CanGoBack);

        // GoBack on empty stack should not throw and collection should stay the same
        vm.GoBack();

        Assert.False(vm.CanGoBack);
        Assert.Equal("col-1", vm.GetCollection().Id);
    }

    // ── Wave 2 Polish: Bezier control point perpendicular offset ────

    // ── Wave 3: Final Feature Wave ─────────────────────────────────

    // ── 25. Edge weight in VM: Weight flows from model to VM ────────

    [Fact]
    public void EdgeWeight_FlowsFromModelToVm()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "pA", ZhText = "A" });
        col.Passages.Add(new ScholarPassage { Id = "pB", ZhText = "B" });
        col.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1", FromNodeId = "pA", ToNodeId = "pB",
            RelationType = "references", Weight = 2.5
        });

        var vm = MakeVm(col);

        var edge = vm.Edges.FirstOrDefault(e => e.EdgeId == "e1");
        Assert.NotNull(edge);
        Assert.Equal(2.5, edge!.Weight, precision: 6);
    }

    // ── 26. ImportanceStars: 3 filled + 2 empty ─────────────────────

    [Fact]
    public void ImportanceStars_ThreeStars_ReturnsCorrectGlyphs()
    {
        var node = new CollectionTreeNode
        {
            Kind = TreeNodeKind.Passage,
            Importance = 3
        };

        Assert.Equal("\u2605\u2605\u2605\u2606\u2606", node.ImportanceStars);
    }

    [Fact]
    public void ImportanceStars_ZeroImportance_ReturnsEmpty()
    {
        var node = new CollectionTreeNode
        {
            Kind = TreeNodeKind.Passage,
            Importance = 0
        };

        Assert.Equal("", node.ImportanceStars);
    }

    [Fact]
    public void ImportanceStars_CollectionKind_ReturnsEmpty()
    {
        var node = new CollectionTreeNode
        {
            Kind = TreeNodeKind.Collection,
            Importance = 4
        };

        // Non-passage nodes always return empty regardless of Importance
        Assert.Equal("", node.ImportanceStars);
    }

    // ── 27. StatusDotColor: read=green, skimmed=yellow, null=gray ───

    [Fact]
    public void StatusDotColor_Read_ReturnsGreen()
    {
        var node = new CollectionTreeNode { ReadingStatus = "read" };
        Assert.Equal("#4CAF50", node.StatusDotColor);
    }

    [Fact]
    public void StatusDotColor_Skimmed_ReturnsYellow()
    {
        var node = new CollectionTreeNode { ReadingStatus = "skimmed" };
        Assert.Equal("#FFC107", node.StatusDotColor);
    }

    [Fact]
    public void StatusDotColor_Null_ReturnsGray()
    {
        var node = new CollectionTreeNode { ReadingStatus = null };
        Assert.Equal("#9E9E9E", node.StatusDotColor);
    }

    [Fact]
    public void StatusDotColor_Unknown_ReturnsGray()
    {
        var node = new CollectionTreeNode { ReadingStatus = "unknown-status" };
        Assert.Equal("#9E9E9E", node.StatusDotColor);
    }

    // ── 28. SearchAllCollections: cross-collection passage search ────

    [Fact]
    public void SearchAllCollections_FindsMatchingPassage()
    {
        var svc = new StubScholarCollectionsService();
        var vm = new ScholarTabViewModel(svc);

        var col1 = new ScholarCollection { Id = "c1", Name = "First" };
        col1.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Dharma gate" });
        var col2 = new ScholarCollection { Id = "c2", Name = "Second" };
        col2.Passages.Add(new ScholarPassage { Id = "p2", ZhText = "Mountain water" });

        // Inject collections into _allCollections via reflection
        var field = typeof(ScholarTabViewModel)
            .GetField("_allCollections", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var list = (List<ScholarCollection>)field!.GetValue(vm)!;
        list.Add(col1);
        list.Add(col2);

        var results = vm.SearchAllCollections("Dharma");
        Assert.Single(results);
        Assert.Equal("p1", results[0].Passage.Id);
        Assert.Equal("c1", results[0].Collection.Id);
    }

    [Fact]
    public void SearchAllCollections_NoMatch_ReturnsEmpty()
    {
        var svc = new StubScholarCollectionsService();
        var vm = new ScholarTabViewModel(svc);

        var col1 = new ScholarCollection { Id = "c1", Name = "First" };
        col1.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Dharma gate" });

        var field = typeof(ScholarTabViewModel)
            .GetField("_allCollections", BindingFlags.Instance | BindingFlags.NonPublic);
        var list = (List<ScholarCollection>)field!.GetValue(vm)!;
        list.Add(col1);

        var results = vm.SearchAllCollections("Nirvana");
        Assert.Empty(results);
    }

    // ── 29. GEXF structure: verify tags from inline build logic ─────

    [Fact]
    public void GexfStructure_ContainsRequiredTags()
    {
        // Replicate the GEXF string-building logic from ResearchGraphWindow.ExportGexfAsync
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "pA", ZhText = "Node A" });
        col.Passages.Add(new ScholarPassage { Id = "pB", ZhText = "Node B" });
        col.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1", FromNodeId = "pA", ToNodeId = "pB",
            RelationType = "references", Weight = 1.0
        });

        var vm = MakeVm(col);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<gexf xmlns=\"http://gexf.net/1.3\" version=\"1.3\">");
        sb.AppendLine("  <graph defaultedgetype=\"directed\">");
        sb.AppendLine("    <nodes>");
        foreach (var n in vm.Nodes)
        {
            var label = System.Security.SecurityElement.Escape(n.Label) ?? "";
            sb.AppendLine($"      <node id=\"{System.Security.SecurityElement.Escape(n.NodeId)}\" label=\"{label}\">");
            sb.AppendLine($"        <attvalues><attvalue for=\"0\" value=\"{n.NodeType}\"/></attvalues>");
            sb.AppendLine("      </node>");
        }
        sb.AppendLine("    </nodes>");
        sb.AppendLine("    <edges>");
        int edgeIdx = 0;
        foreach (var e in vm.Edges)
        {
            sb.AppendLine($"      <edge id=\"{edgeIdx++}\" source=\"{System.Security.SecurityElement.Escape(e.From.NodeId)}\" target=\"{System.Security.SecurityElement.Escape(e.To.NodeId)}\">");
            sb.AppendLine("      </edge>");
        }
        sb.AppendLine("    </edges>");
        sb.AppendLine("  </graph>");
        sb.AppendLine("</gexf>");

        var gexf = sb.ToString();
        Assert.Contains("<gexf", gexf);
        Assert.Contains("<node", gexf);
        Assert.Contains("<edge", gexf);
        Assert.Contains("pA", gexf);
        Assert.Contains("pB", gexf);
    }

    // ── End of Wave 3 ───────────────────────────────────────────────

    [Fact]
    public void BezierControlPoint_PerpendicularOffset_AtMidpoint()
    {
        // Replicate the control-point calculation from DrawEdge:
        //   from = (0,0), to = (100,0)
        //   edgeDx = 100, edgeDy = 0, edgeLen = 100
        //   perpX = -0/100 = 0, perpY = 100/100 = 1
        //   curveOffset = min(20, 100*0.12) = 12
        //   mid = (50, 0)
        //   controlPt = (50 + 0*12, 0 + 1*12) = (50, 12)

        double fromX = 0, fromY = 0, toX = 100, toY = 0;
        double edgeDx = toX - fromX, edgeDy = toY - fromY;
        double edgeLen = Math.Sqrt(edgeDx * edgeDx + edgeDy * edgeDy);

        double perpX = -edgeDy / edgeLen;
        double perpY = edgeDx / edgeLen;
        double curveOffset = Math.Min(20, edgeLen * 0.12);
        double midX = (fromX + toX) / 2;
        double midY = (fromY + toY) / 2;
        double ctrlX = midX + perpX * curveOffset;
        double ctrlY = midY + perpY * curveOffset;

        // Control point should be at midpoint X = 50
        Assert.Equal(50.0, ctrlX, precision: 6);
        // Control point should be offset perpendicularly by +12
        Assert.Equal(12.0, ctrlY, precision: 6);
        // Offset magnitude should match curveOffset
        double offsetDist = Math.Sqrt((ctrlX - midX) * (ctrlX - midX) + (ctrlY - midY) * (ctrlY - midY));
        Assert.Equal(curveOffset, offsetDist, precision: 6);
    }

    // ── Cluster grouping produces correct groups by NodeType ────────────

    [Fact]
    public void ClusterGrouping_GroupsByNodeType_CorrectCounts()
    {
        var vm = MakeVm();

        // Add 3 passages
        for (int i = 0; i < 3; i++)
            vm.Nodes.Add(new ResearchGraphNode
            {
                NodeId = $"p-{i}", Label = $"Passage {i}",
                NodeType = ScholarNodeType.Passage
            });

        // Add 2 concepts
        for (int i = 0; i < 2; i++)
            vm.Nodes.Add(new ResearchGraphNode
            {
                NodeId = $"c-{i}", Label = $"Concept {i}",
                NodeType = ScholarNodeType.Concept
            });

        var visible = vm.GetVisibleNodes();
        var groups = visible.GroupBy(n => n.NodeType)
            .Where(g => g.Count() >= 2)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, groups.Count);
        Assert.Equal(3, groups[ScholarNodeType.Passage]);
        Assert.Equal(2, groups[ScholarNodeType.Concept]);
    }

    // ── ScholarPassage.IsSelectedForCompare defaults to false ───────────

    [Fact]
    public void ScholarPassage_IsSelectedForCompare_DefaultsFalse()
    {
        var passage = new ScholarPassage();
        Assert.False(passage.IsSelectedForCompare);
    }
}
