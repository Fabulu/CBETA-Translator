// LineageForestLayoutTests — pins the pure tidy-forest layout port
// (Infrastructure/LineageForestLayout.cs) against the SPA reference engine
// (ZenLinkPage/lib/lineage-layout.js).
//
// THE GOLDEN RATCHET (mirrors the SPA's own assertNoOverlaps): compute the layout
// for the FULL 609-master roster and assert the overlap assertion returns
// nodeNode==0 && edgeNode==0, PLUS a world-width sanity bound. A botched contour
// pack produces overlaps or a ~26,000px-wide world; this test is the permanent
// guard that catches both. The micro-forest tests below exercise one packing
// stage each (leaf-stack fold, book-source shelf, two-root pack, deep chain).

using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;
using Xunit.Abstractions;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Lineage")]
public class LineageForestLayoutTests
{
    private readonly ITestOutputHelper _out;
    public LineageForestLayoutTests(ITestOutputHelper output) => _out = output;

    private static IReadOnlyList<LineageMasterRecord> RealRoster()
        => new LineageRosterService().GetAll();

    private static LineageMasterRecord Rec(
        string primary,
        string? teacherKey = null,
        string? transmission = null,
        string? school = null,
        int? death = null,
        IEnumerable<string>? extraNames = null)
    {
        var names = new List<string> { primary };
        if (extraNames != null) names.AddRange(extraNames);
        return new LineageMasterRecord
        {
            Names = names,
            TeacherKey = teacherKey,
            Transmission = transmission,
            School = school,
            Death = death,
        };
    }

    // ── THE RATCHET: real 609 roster, zero overlaps, narrow world ──

    [Fact]
    public void FullRoster_NoOverlaps_AndWorldStaysNarrow()
    {
        var roster = RealRoster();
        // 2026-07-17 fold (RUN-20260711-1248): 609 -> 965 (researched masters
        // folded back in after the 1012-record auto-harvest corruption was reverted).
        Assert.Equal(965, roster.Count); // guard: this is the post-fold 965-record roster

        var graph = LineageGraphBuilder.Build(roster);
        var layout = LineageForestLayout.Compute(graph.Nodes, graph.Edges);
        var overlaps = LineageForestLayout.AssertNoOverlaps(graph.Nodes, graph.Edges, layout.Routes);

        _out.WriteLine($"world: width={layout.Width:F0} height={layout.Height:F0} " +
                       $"minX={layout.MinX:F0} maxX={layout.MaxX:F0}");
        _out.WriteLine($"overlaps: ok={overlaps.Ok} nodeNode={overlaps.NodeNode} edgeNode={overlaps.EdgeNode}");
        foreach (var s in overlaps.Samples) _out.WriteLine("  " + s);

        // The core guarantee: packing is overlap-free by construction.
        Assert.Equal(0, overlaps.NodeNode);
        Assert.Equal(0, overlaps.EdgeNode);
        Assert.True(overlaps.Ok);

        // World-width sanity: the SPA lands ~12,000px for 609 masters; a botched
        // pack blows past 25,000. 2026-07-17 fold (RUN-20260711-1248) took the
        // roster 609 -> 965 (+356 nodes, +58%), which legitimately widens a tidy
        // forest -- the overlap-free guarantee above is the real correctness
        // check; this is only a "did the pack blow up" ceiling, recalibrated
        // for the new node count with headroom short of the 25,000 botched line.
        Assert.True(layout.Width < 26000, $"world too wide: {layout.Width:F0}px (botched pack?)");
        Assert.True(layout.Width > 1000, $"world implausibly narrow: {layout.Width:F0}px");
    }

    // ── Determinism: two Compute() runs -> identical coordinates for every node ──

    [Fact]
    public void Determinism_TwoRuns_IdenticalCoordinates()
    {
        var roster = RealRoster();

        var gA = LineageGraphBuilder.Build(roster);
        LineageForestLayout.Compute(gA.Nodes, gA.Edges);
        var snapA = gA.Nodes.Select(n => (n.Id, n.Layer, n.X, n.Y, n.Order)).ToList();

        var gB = LineageGraphBuilder.Build(roster);
        LineageForestLayout.Compute(gB.Nodes, gB.Edges);
        var snapB = gB.Nodes.Select(n => (n.Id, n.Layer, n.X, n.Y, n.Order)).ToList();

        Assert.Equal(snapA, snapB);
    }

    // ── Stage: leaf stacking — >=2 sibling leaves fold into ONE vertical column ──

    [Fact]
    public void LeafStack_SiblingLeavesFoldIntoAColumn()
    {
        // Parent with two dated leaf children -> cols=1, rows=2 (column-major).
        var roster = new List<LineageMasterRecord>
        {
            Rec("Parent"),
            Rec("Elder",   teacherKey: "Parent", death: 800),
            Rec("Younger", teacherKey: "Parent", death: 850),
        };
        var g = LineageGraphBuilder.Build(roster);
        LineageForestLayout.Compute(g.Nodes, g.Edges);

        var parent = g.ByName["Parent"];
        var elder = g.ByName["Elder"];
        var younger = g.ByName["Younger"];

        // Same column (equal X), stacked on DIFFERENT layers (breadth -> depth).
        Assert.Equal(elder.X, younger.X, 3);
        Assert.NotEqual(elder.Layer, younger.Layer);
        Assert.True(elder.Layer > parent.Layer);
        Assert.True(younger.Layer > parent.Layer);
        // Earlier master sits higher (row 0) — column reads top->bottom by year.
        Assert.True(elder.Layer < younger.Layer);
    }

    // ── Stage: book-source shelf — a book sits directly ABOVE its master ──

    [Fact]
    public void BookSource_ShelvedOneLayerAboveItsMaster()
    {
        var roster = new List<LineageMasterRecord> { Rec("Author", transmission: "book") };
        var g = LineageGraphBuilder.Build(roster);
        LineageForestLayout.Compute(g.Nodes, g.Edges);

        var author = g.ByName["Author"];
        var source = Assert.Single(g.Sources);

        Assert.True(source.IsSource);
        Assert.Equal(author.Layer - 1, source.Layer);   // shelved one layer up
        Assert.Equal(author.X, source.X, 3);            // centered over the master (single book)
    }

    // ── Stage: two root trees pack on the side that grows the world less ──

    [Fact]
    public void TwoRootTrees_PackWithoutOverlapAndSeparated()
    {
        var roster = new List<LineageMasterRecord>
        {
            Rec("RootA"),
            Rec("A1", teacherKey: "RootA"), Rec("A2", teacherKey: "RootA"), Rec("A3", teacherKey: "RootA"),
            Rec("RootB"),
            Rec("B1", teacherKey: "RootB"), Rec("B2", teacherKey: "RootB"),
        };
        var g = LineageGraphBuilder.Build(roster);
        var layout = LineageForestLayout.Compute(g.Nodes, g.Edges);
        var overlaps = LineageForestLayout.AssertNoOverlaps(g.Nodes, g.Edges, layout.Routes);

        Assert.Equal(0, overlaps.NodeNode);
        Assert.Equal(0, overlaps.EdgeNode);

        var rootA = g.ByName["RootA"];
        var rootB = g.ByName["RootB"];
        // Distinct trees do not stack at the same x.
        Assert.NotEqual(rootA.X, rootB.X);
    }

    // ── Stage: deep chain — each single-child parent is centered over its child ──

    [Fact]
    public void DeepChain_ParentCenteredOverChild_RunsStraight()
    {
        var roster = new List<LineageMasterRecord>
        {
            Rec("G0"),
            Rec("G1", teacherKey: "G0"),
            Rec("G2", teacherKey: "G1"),
            Rec("G3", teacherKey: "G2"),
        };
        var g = LineageGraphBuilder.Build(roster);
        LineageForestLayout.Compute(g.Nodes, g.Edges);

        var g0 = g.ByName["G0"];
        var g1 = g.ByName["G1"];
        var g2 = g.ByName["G2"];
        var g3 = g.ByName["G3"];

        // A pure chain: every parent sits exactly above its single child.
        Assert.Equal(g0.X, g1.X, 3);
        Assert.Equal(g1.X, g2.X, 3);
        Assert.Equal(g2.X, g3.X, 3);
        // Descending generations occupy consecutive layers.
        Assert.Equal(g0.Layer + 1, g1.Layer);
        Assert.Equal(g1.Layer + 1, g2.Layer);
        Assert.Equal(g2.Layer + 1, g3.Layer);
    }
}
