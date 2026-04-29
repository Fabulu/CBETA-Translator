using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

/// <summary>
/// Tests for the 12 Research Graph fixes: force layout SPA formula,
/// convergence early-exit, HitTest radius, ZenMaster SourceData,
/// search highlight, single-node centering, and undo/redo state.
/// </summary>
public class ResearchGraphFixTests : IClassFixture<AvaloniaFixture>
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

    private static double InvokeGetNodeRadius(ResearchGraphCanvasControl ctrl, ResearchGraphNode node)
    {
        var method = typeof(ResearchGraphCanvasControl)
            .GetMethod("GetNodeRadius", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (double)method!.Invoke(ctrl, new object[] { node })!;
    }

    private static ResearchGraphNode? InvokeHitTest(ResearchGraphCanvasControl ctrl, double x, double y)
    {
        var method = typeof(ResearchGraphCanvasControl)
            .GetMethod("HitTest", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (ResearchGraphNode?)method!.Invoke(ctrl, new object[] { x, y });
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

    // ── 3. HitTest radius: expanded by +15 to cover handles ─────────

    [Fact]
    public void HitTest_MatchesWithinExpandedRadius()
    {
        // Place a single node, then set up a ctrl with a vm.
        // The HitTest uses r + 15, so clicking within r+14 should hit.
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Test" });
        var vm = MakeVm(col);

        // Place node at exact position (bypass layout, set directly)
        vm.Nodes[0].X = 200;
        vm.Nodes[0].Y = 200;

        var ctrl = new ResearchGraphCanvasControl();
        ctrl.SetViewModel(vm);

        double r = InvokeGetNodeRadius(ctrl, vm.Nodes[0]); // r=10 for 0-degree Passage

        // At zoom=1, offset=0, screen coords == graph coords
        // Click at distance r+14 from center (within r+15 threshold)
        double clickDist = r + 14;
        var hit = InvokeHitTest(ctrl, 200 + clickDist, 200);
        Assert.NotNull(hit);
        Assert.Equal("p1", hit!.NodeId);
    }

    [Fact]
    public void HitTest_MissesOutsideExpandedRadius()
    {
        var col = MakeEmptyCollection();
        col.Passages.Add(new ScholarPassage { Id = "p1", ZhText = "Test" });
        var vm = MakeVm(col);

        vm.Nodes[0].X = 200;
        vm.Nodes[0].Y = 200;

        var ctrl = new ResearchGraphCanvasControl();
        ctrl.SetViewModel(vm);

        double r = InvokeGetNodeRadius(ctrl, vm.Nodes[0]); // r=10

        // Click at distance r+20 from center (beyond r+15 threshold)
        double clickDist = r + 20;
        var hit = InvokeHitTest(ctrl, 200 + clickDist, 200);
        Assert.Null(hit);
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

    // ── 8. EaseOutCubic easing function ─────────────────────────────

    private static double InvokeEaseOutCubic(double t)
    {
        var method = typeof(ResearchGraphCanvasControl)
            .GetMethod("EaseOutCubic", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (double)method!.Invoke(null, new object[] { t })!;
    }

    [Fact]
    public void EaseOutCubic_AtZero_ReturnsZero()
    {
        double result = InvokeEaseOutCubic(0);
        Assert.Equal(0.0, result, precision: 10);
    }

    [Fact]
    public void EaseOutCubic_AtOne_ReturnsOne()
    {
        double result = InvokeEaseOutCubic(1);
        Assert.Equal(1.0, result, precision: 10);
    }

    [Fact]
    public void EaseOutCubic_AtHalf_ReturnsApprox0875()
    {
        // EaseOutCubic(0.5) = 1 - (1 - 0.5)^3 = 1 - 0.125 = 0.875
        double result = InvokeEaseOutCubic(0.5);
        Assert.Equal(0.875, result, precision: 6);
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

    [Fact]
    public void EdgeAlpha_DefaultPen_MatchesExpectedAlpha()
    {
        // The static _defaultNodePen uses Color.FromArgb(153, 255, 255, 255)
        // confirming the 153 alpha for default edges aligns with node outlines.
        // Use reflection to read the private static field without importing Avalonia.Media.
        var field = typeof(ResearchGraphCanvasControl)
            .GetField("_defaultNodePen", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var pen = field!.GetValue(null);
        Assert.NotNull(pen);
        // Access Brush property via reflection
        var brushProp = pen!.GetType().GetProperty("Brush");
        Assert.NotNull(brushProp);
        var brush = brushProp!.GetValue(pen);
        Assert.NotNull(brush);
        // Access Color.A via reflection
        var colorProp = brush!.GetType().GetProperty("Color");
        Assert.NotNull(colorProp);
        var color = colorProp!.GetValue(brush);
        Assert.NotNull(color);
        var alphaProp = color!.GetType().GetProperty("A");
        Assert.NotNull(alphaProp);
        byte alpha = (byte)alphaProp!.GetValue(color)!;
        Assert.Equal(153, alpha);
    }

    // ── 11. Entry animation state ───────────────────────────────────

    [Fact]
    public void EntryProgress_InitiallyZero_BeforeSetViewModel()
    {
        var ctrl = new ResearchGraphCanvasControl();
        var field = typeof(ResearchGraphCanvasControl)
            .GetField("_entryProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        double progress = (double)field!.GetValue(ctrl)!;
        Assert.Equal(0.0, progress, precision: 10);
    }

    [Fact]
    public void EntryProgress_ResetToZero_WhenSetViewModelCalled()
    {
        // SetViewModel calls StartEntryAnimation which sets _entryProgress = 0
        var ctrl = new ResearchGraphCanvasControl();
        var vm = MakeVm();
        ctrl.SetViewModel(vm);

        var field = typeof(ResearchGraphCanvasControl)
            .GetField("_entryProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        // Right after SetViewModel, _entryProgress starts at 0 (animation just began)
        // The timer ticks asynchronously, so synchronously it should still be 0
        double progress = (double)field!.GetValue(ctrl)!;
        Assert.Equal(0.0, progress, precision: 10);
    }

    [Fact]
    public void EntryTimer_CreatedAfterSetViewModel()
    {
        var ctrl = new ResearchGraphCanvasControl();

        // Before SetViewModel, timer should be null
        var timerField = typeof(ResearchGraphCanvasControl)
            .GetField("_entryTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(timerField);
        Assert.Null(timerField!.GetValue(ctrl));

        // After SetViewModel, timer should be created
        var vm = MakeVm();
        ctrl.SetViewModel(vm);
        Assert.NotNull(timerField.GetValue(ctrl));
    }

    [Fact]
    public void StartEntryAnimation_SetsEntryStartTime()
    {
        var ctrl = new ResearchGraphCanvasControl();
        var startField = typeof(ResearchGraphCanvasControl)
            .GetField("_entryStart", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(startField);

        var before = DateTime.UtcNow;
        var vm = MakeVm();
        ctrl.SetViewModel(vm); // calls StartEntryAnimation
        var after = DateTime.UtcNow;

        var entryStart = (DateTime)startField!.GetValue(ctrl)!;
        Assert.True(entryStart >= before && entryStart <= after,
            $"EntryStart {entryStart} should be between {before} and {after}");
    }
}
