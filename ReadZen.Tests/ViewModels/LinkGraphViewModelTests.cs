using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class LinkGraphViewModelTests
{
    private static ScholarPassage MakePassage(string id, string zhText = "test")
    {
        return new ScholarPassage { Id = id, ZhText = zhText };
    }

    private static PassageLink MakeLink(string from, string to, string relation = "quotes")
    {
        return new PassageLink { FromPassageId = from, ToPassageId = to, RelationType = relation };
    }

    [Fact]
    public void BuildGraph_EmptyCollections_ProducesEmptyGraph()
    {
        var vm = new LinkGraphViewModel();

        vm.BuildGraph(Array.Empty<ScholarPassage>(), Array.Empty<PassageLink>());

        Assert.Empty(vm.Nodes);
        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void BuildGraph_WithPassagesAndLinks_CreatesNodesAndEdges()
    {
        var vm = new LinkGraphViewModel();
        var passages = new[] { MakePassage("p1"), MakePassage("p2"), MakePassage("p3") };
        var links = new[] { MakeLink("p1", "p2"), MakeLink("p2", "p3", "parallels") };

        vm.BuildGraph(passages, links);

        Assert.Equal(3, vm.Nodes.Count);
        Assert.Equal(2, vm.Edges.Count);
        Assert.Contains(vm.Nodes, n => n.PassageId == "p1");
        Assert.Contains(vm.Nodes, n => n.PassageId == "p2");
        Assert.Contains(vm.Nodes, n => n.PassageId == "p3");
        Assert.Equal("quotes", vm.Edges[0].RelationType);
        Assert.Equal("parallels", vm.Edges[1].RelationType);
    }

    [Fact]
    public void BuildGraph_IgnoresLinksWithMissingPassages()
    {
        var vm = new LinkGraphViewModel();
        var passages = new[] { MakePassage("p1") };
        var links = new[] { MakeLink("p1", "missing"), MakeLink("ghost", "p1") };

        vm.BuildGraph(passages, links);

        Assert.Single(vm.Nodes);
        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void BuildGraph_TruncatesLongLabels()
    {
        var vm = new LinkGraphViewModel();
        var longText = "ABCDEFGHIJKLMNOP"; // 16 chars, > 8
        var passages = new[] { MakePassage("p1", longText) };

        vm.BuildGraph(passages, Array.Empty<PassageLink>());

        var node = vm.Nodes.Single();
        Assert.Equal("ABCDEFGH\u2026", node.Label); // 8 chars + ellipsis
    }

    [Fact]
    public void RunLayout_SingleNode_DoesNotThrow()
    {
        var vm = new LinkGraphViewModel();
        vm.BuildGraph(new[] { MakePassage("p1") }, Array.Empty<PassageLink>());

        var ex = Record.Exception(() => vm.RunLayout());

        Assert.Null(ex);
    }

    [Fact]
    public void RunLayout_TwoConnectedNodes_ConvergesToFiniteDistance()
    {
        var vm = new LinkGraphViewModel();
        var passages = new[] { MakePassage("p1"), MakePassage("p2") };
        var links = new[] { MakeLink("p1", "p2") };

        vm.BuildGraph(passages, links);

        // Force nodes far apart so attractive force dominates
        vm.Nodes[0].X = 30;  vm.Nodes[0].Y = 200;
        vm.Nodes[1].X = 470; vm.Nodes[1].Y = 200;

        double dx0 = vm.Nodes[0].X - vm.Nodes[1].X;
        double dy0 = vm.Nodes[0].Y - vm.Nodes[1].Y;
        double distBefore = Math.Sqrt(dx0 * dx0 + dy0 * dy0);

        vm.RunLayout(iterations: 200);

        double dx1 = vm.Nodes[0].X - vm.Nodes[1].X;
        double dy1 = vm.Nodes[0].Y - vm.Nodes[1].Y;
        double distAfter = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

        Assert.True(distAfter < distBefore,
            $"Expected connected nodes to be closer after layout. Before: {distBefore:F2}, After: {distAfter:F2}");
    }

    [Fact]
    public void RunLayout_RespectsWidthHeightBounds()
    {
        var vm = new LinkGraphViewModel();
        var passages = Enumerable.Range(0, 10)
            .Select(i => MakePassage($"p{i}"))
            .ToArray();
        var links = Enumerable.Range(0, 9)
            .Select(i => MakeLink($"p{i}", $"p{i + 1}"))
            .ToArray();

        vm.BuildGraph(passages, links);

        double width = 500, height = 400;
        vm.RunLayout(iterations: 100, width: width, height: height);

        foreach (var node in vm.Nodes)
        {
            Assert.True(node.X >= 30 && node.X <= width - 30,
                $"Node {node.PassageId} X={node.X:F2} out of bounds [30, {width - 30}]");
            Assert.True(node.Y >= 30 && node.Y <= height - 30,
                $"Node {node.PassageId} Y={node.Y:F2} out of bounds [30, {height - 30}]");
        }
    }

    [Fact]
    public void HitTest_OnNode_ReturnsNode()
    {
        var vm = new LinkGraphViewModel();
        vm.BuildGraph(new[] { MakePassage("p1") }, Array.Empty<PassageLink>());

        var node = vm.Nodes[0];
        // Hit exactly on the node center (no offset, zoom=1)
        var result = vm.HitTest(node.X, node.Y);

        Assert.NotNull(result);
        Assert.Equal("p1", result.PassageId);
    }

    [Fact]
    public void HitTest_MissesNode_ReturnsNull()
    {
        var vm = new LinkGraphViewModel();
        vm.BuildGraph(new[] { MakePassage("p1") }, Array.Empty<PassageLink>());

        // Click far away from any node
        var result = vm.HitTest(-9999, -9999);

        Assert.Null(result);
    }

    [Fact]
    public void HitTest_AccountsForZoom()
    {
        var vm = new LinkGraphViewModel();
        vm.BuildGraph(new[] { MakePassage("p1") }, Array.Empty<PassageLink>());

        var node = vm.Nodes[0];
        vm.Zoom = 2.0;

        // Canvas coords = graph coords * zoom + offset. With zoom=2, offset=0,
        // the canvas position of the node is (node.X * 2, node.Y * 2).
        var result = vm.HitTest(node.X * 2.0, node.Y * 2.0);

        Assert.NotNull(result);
        Assert.Equal("p1", result.PassageId);

        // The original graph coords should miss when zoom is applied
        // (unless the node happens to be near origin)
        // Test that a click at the raw graph coords misses when zoomed
        // Only valid if node is far enough from origin
        if (node.X > 30 && node.Y > 30)
        {
            var miss = vm.HitTest(node.X, node.Y);
            Assert.Null(miss);
        }
    }

    [Fact]
    public void RelationColors_ContainsAllNineTypes()
    {
        Assert.Equal(9, LinkGraphViewModel.RelationColors.Count);

        foreach (var type in PassageLink.RelationTypes)
        {
            Assert.True(LinkGraphViewModel.RelationColors.ContainsKey(type),
                $"RelationColors missing key '{type}'");
        }
    }

    [Fact]
    public void SelectedNode_DefaultsToNull()
    {
        var vm = new LinkGraphViewModel();

        Assert.Null(vm.SelectedNode);
    }
}
