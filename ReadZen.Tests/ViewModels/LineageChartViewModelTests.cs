// LineageChartViewModelTests — pins the L4 chart view-model + the single most
// important behavior of the whole chart: the attestation FAIL-SAFE.
//
// Two guarantees:
//  1. The VM loads the real 609-master roster, builds the graph, and runs the
//     tidy-forest layout so the control has non-empty nodes + edge routes to
//     draw; selection maps a node back to its raw record.
//  2. LineageChartControl.StyleFor() — the pure fail-safe — resolves EVERY
//     unknown/missing/invalid attestation to the weakest style (D, faint dots),
//     and ONLY "A"/"B"/"C" earn a stronger one. It is structurally impossible
//     for bad data to render as a confident solid line.

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using ReadZen.App.Infrastructure;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.ViewModels;

[Trait("Domain", "Lineage")]
public class LineageChartViewModelTests
{
    private static LineageChartViewModel MakeVm()
        => new LineageChartViewModel(new LineageRosterService());

    // ── the VM loads a real, drawable chart ──

    [Fact]
    public void Loads609Masters_WithNonEmptyLayoutAndRoutes()
    {
        var vm = MakeVm();

        Assert.True(vm.IsLoaded);
        // 2026-07-17 fold (RUN-20260711-1248): 609 -> 965 (researched masters
        // folded back in after the 1012-record auto-harvest corruption was
        // reverted). See LineageGraphBuilderTests.FullRoster_ReportCountsMatchJsReference.
        Assert.Equal(965, vm.Report.Masters);         // the post-fold 965-record roster
        Assert.NotEmpty(vm.Nodes);                     // masters + synthesized book sources
        Assert.NotEmpty(vm.Edges);
        Assert.True(vm.Routes.Count > 0, "layout produced no edge routes");

        // every real edge has a routing polyline the control can stroke
        foreach (var e in vm.Edges)
            Assert.True(vm.Routes.ContainsKey(e), "an edge had no route");

        // layout actually assigned coordinates (not all stacked at origin)
        Assert.Contains(vm.Nodes, n => n.X != 0);
        Assert.Contains(vm.Nodes, n => n.Layer > 0);
    }

    [Fact]
    public void SelectingNode_ResolvesSelectedMaster()
    {
        var vm = MakeVm();

        // a real master node (not a synthesized book source) maps to a record
        var master = vm.Nodes.First(n => !n.IsSource);
        vm.SelectedNode = master;
        Assert.NotNull(vm.SelectedMaster);
        Assert.Contains(master.Id, vm.SelectedMaster!.Names);

        // a book-source pseudo-node has no backing record
        var source = vm.Nodes.FirstOrDefault(n => n.IsSource);
        if (source != null)
        {
            vm.SelectedNode = source;
            Assert.Null(vm.SelectedMaster);
        }

        // clearing selection clears the record
        vm.SelectedNode = null;
        Assert.Null(vm.SelectedMaster);
    }

    [Fact]
    public void SearchText_MatchesNodesByNameOrdinalCaseInsensitive()
    {
        var vm = MakeVm();
        vm.SearchText = "linji";
        Assert.NotEmpty(vm.SearchHitIds);

        vm.SearchText = "";
        Assert.Empty(vm.SearchHitIds);
    }

    // ── THE FAIL-SAFE: unknown/missing/invalid attestation → D (never A/B/C) ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("X")]     // misspelling
    [InlineData("E")]     // a future grade that doesn't exist yet
    [InlineData("a")]     // wrong case — only exact "A" earns A
    [InlineData("D")]     // D maps to D too
    public void StyleFor_UnknownOrMissing_FallsToWeakestD(string? att)
    {
        var st = LineageChartControl.StyleFor(att);
        var d = LineageChartControl.StyleFor("D");

        Assert.Equal("D", st.Grade);
        Assert.Equal(d.Width, st.Width);
        Assert.Equal(d.Opacity, st.Opacity);
        Assert.True(st.Faint);
        Assert.Equal(1.00, st.Width);
        Assert.Equal(0.40, st.Opacity);
        Assert.NotEmpty(st.Dash);          // D is dotted, never solid
    }

    [Theory]
    [InlineData("A", 2.25, 0.85)]
    [InlineData("B", 1.40, 0.60)]
    [InlineData("C", 1.20, 0.50)]
    public void StyleFor_KnownGrades_UpgradeAboveD(string att, double width, double opacity)
    {
        var st = LineageChartControl.StyleFor(att);
        Assert.Equal(att, st.Grade);
        Assert.Equal(width, st.Width);
        Assert.Equal(opacity, st.Opacity);
        Assert.False(st.Faint);            // only D is faint
        Assert.NotEqual("D", st.Grade);    // an earned grade is never the fail-safe
    }

    [Fact]
    public void StyleFor_AandB_AreSolid_CisDashed()
    {
        Assert.Empty(LineageChartControl.StyleFor("A").Dash);   // solid
        Assert.Empty(LineageChartControl.StyleFor("B").Dash);   // solid
        Assert.NotEmpty(LineageChartControl.StyleFor("C").Dash); // dashed [7,5]
    }

    // ── L5 CHANNEL 2 (transmission → GEOMETRY): the pure marker mapping ──
    // A separate axis from attestation-ink. Only the exact SPA tokens earn a marker.

    [Theory]
    [InlineData("遙嗣", LineageChartControl.TransmissionMarker.RemoteCircle)]
    [InlineData("代囑", LineageChartControl.TransmissionMarker.ProxyDiamond)]
    [InlineData("book", LineageChartControl.TransmissionMarker.Book)]
    [InlineData("disputed", LineageChartControl.TransmissionMarker.Disputed)]
    [InlineData("direct", LineageChartControl.TransmissionMarker.None)]
    [InlineData("none", LineageChartControl.TransmissionMarker.None)]
    [InlineData("", LineageChartControl.TransmissionMarker.None)]
    [InlineData(null, LineageChartControl.TransmissionMarker.None)]
    [InlineData("Book", LineageChartControl.TransmissionMarker.None)]   // ordinal, case-sensitive
    public void MarkerFor_MapsTransmissionToGeometry(string? transmission, LineageChartControl.TransmissionMarker expected)
        => Assert.Equal(expected, LineageChartControl.MarkerFor(transmission));

    [Fact]
    public void IsOffChartTeacher_TrueOnlyForStubNodes()
    {
        Assert.True(LineageChartControl.IsOffChartTeacher(new LineageNode { Stub = true }));   // dangling teacher → ⊣ stub
        Assert.False(LineageChartControl.IsOffChartTeacher(new LineageNode()));                 // resolved parent
        Assert.False(LineageChartControl.IsOffChartTeacher(null));
    }

    // The four channels never cross: a transmission marker carries NO ink meaning,
    // and an attestation grade carries NO geometry.
    [Fact]
    public void TransmissionAndAttestation_AreIndependentAxes()
    {
        // A "book" edge can be A-grade (solid) OR D-grade (dotted); the marker is Book either way.
        Assert.Equal(LineageChartControl.TransmissionMarker.Book, LineageChartControl.MarkerFor("book"));
        Assert.Empty(LineageChartControl.StyleFor("A").Dash);      // book + A => solid
        Assert.NotEmpty(LineageChartControl.StyleFor("D").Dash);   // book + D => dotted
        // A remote-succession edge (遙嗣) is likewise orthogonal to its ink grade.
        Assert.Equal(LineageChartControl.TransmissionMarker.RemoteCircle, LineageChartControl.MarkerFor("遙嗣"));
    }

    // ── L5 CHANNEL 3 (contested): exactly two contested edges, each with a rival ──

    [Fact]
    public void Graph_HasExactlyTwoContestedEdges_EachCarryingRivalAndRung()
    {
        var vm = MakeVm();
        var contested = vm.Edges.Where(e => e.Contested != null).ToList();

        Assert.Equal(2, contested.Count);   // Yaoshan Weiyan + Longtan Chongxin
        foreach (var e in contested)
        {
            Assert.False(string.IsNullOrEmpty(e.Contested!.Rival), "a contested edge carried no rival");
            Assert.False(string.IsNullOrEmpty(e.Contested!.RivalRung), "a contested edge carried no rival rung");
        }

        // The rivals are the two Tang-stele disputes documented in the roster.
        var rivals = contested.Select(e => e.Contested!.Rival).ToList();
        Assert.Contains("天王道悟", rivals);   // Longtan Chongxin
        Assert.Contains("馬祖道一", rivals);   // Yaoshan Weiyan
    }

    // ── L5 CHANNEL crossing check: Jinul's three book edges are attestation A ──

    [Fact]
    public void JinulBookEdges_AreSolidA_NotWeakDotted()
    {
        var vm = MakeVm();
        var jinul = vm.Nodes.First(n => !n.IsSource && n.Names.Contains("Jinul"));
        var bookEdges = vm.Edges.Where(e => e.Kind == "book" && ReferenceEquals(e.To, jinul)).ToList();

        Assert.Equal(3, bookEdges.Count);
        foreach (var e in bookEdges)
        {
            // channel 2 — geometry: a book transmission.
            Assert.Equal(LineageChartControl.TransmissionMarker.Book, LineageChartControl.MarkerFor(e.Transmission));
            // channel 1 — ink: A-grade attestation → SOLID, not weak/dotted.
            Assert.Equal("A", e.Attestation);
            var st = LineageChartControl.StyleFor(e.Attestation);
            Assert.Equal("A", st.Grade);
            Assert.Empty(st.Dash);       // solid
            Assert.False(st.Faint);      // never the weak D fail-safe
        }
    }

    // ── L5 seal interaction: clicking a seal surfaces the dispute via the VM ──

    [Fact]
    public void SelectedEdge_TracksContestedSelection()
    {
        var vm = MakeVm();
        var contested = vm.Edges.First(e => e.Contested != null);

        // Simulate the control's seal hit: select the edge + its student.
        vm.SelectedEdge = contested;
        vm.SelectedNode = contested.To;

        Assert.Same(contested, vm.SelectedEdge);
        Assert.NotNull(vm.SelectedEdge!.Contested);       // the dispute is reachable for L6
        Assert.NotNull(vm.SelectedMaster);                 // the student's record (with ContestedBy)

        vm.SelectedEdge = null;
        Assert.Null(vm.SelectedEdge);
    }

    // ── M1: FitAll must raise ZoomChanged so the zoom slider tracks the fitted zoom
    //    (before the fix it set _zoom silently, leaving the slider at a stale value
    //    that snap-jumped on first touch). ──

    [Fact]
    public void FitAll_RaisesZoomChanged_SoTheSliderTracks()
    {
        var vm = MakeVm();
        // Construct the control WITHOUT its Avalonia-touching ctor (parity with the
        // View interaction tests), inject the VM, and invoke the private FitAll.
        var chart = (LineageChartControl)RuntimeHelpers.GetUninitializedObject(typeof(LineageChartControl));
        SetField(typeof(LineageChartControl), chart, "_vm", vm);

        double? raised = null;
        chart.ZoomChanged += z => raised = z;

        var fitAll = typeof(LineageChartControl).GetMethod("FitAll", BindingFlags.Instance | BindingFlags.NonPublic)!;
        fitAll.Invoke(chart, new object[] { new Rect(0, 0, 800, 600) });

        Assert.NotNull(raised);                       // the slider got told
        Assert.Equal(chart.Zoom, raised!.Value, 6);   // …the actual fitted zoom
    }

    // ── M6: a book-SOURCE double-click must not activate (which would flip to the
    //    List tab carrying a stale selection); only a real master is activatable. ──

    [Fact]
    public void BookSourceDoubleClick_DoesNotActivate_SoListSelectionIsUnchanged()
    {
        Assert.False(LineageChartControl.ShouldActivateOnDoubleClick(new LineageNode { IsSource = true }));
        Assert.True(LineageChartControl.ShouldActivateOnDoubleClick(new LineageNode { IsSource = false }));
        Assert.False(LineageChartControl.ShouldActivateOnDoubleClick(null));
    }

    // ── M6: list→chart sync must tolerate case (FindListRecord is OrdinalIgnoreCase;
    //    the raw ByName index is ordinal). NodeByNameInsensitive bridges the gap. ──

    [Fact]
    public void NodeByNameInsensitive_MatchesRegardlessOfCase()
    {
        var vm = MakeVm();
        var exact = vm.NodeByNameInsensitive("Bodhidharma");
        Assert.NotNull(exact);
        Assert.Same(exact, vm.NodeByNameInsensitive("BODHIDHARMA"));
        Assert.Same(exact, vm.NodeByNameInsensitive("bodhidharma"));
        Assert.Null(vm.NodeByNameInsensitive(null));
        Assert.Null(vm.NodeByNameInsensitive(""));
    }

    private static void SetField(System.Type type, object target, string name, object? value)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new System.InvalidOperationException($"Missing field {name} on {type.Name}");
        field.SetValue(target, value);
    }
}
