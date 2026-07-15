// ViewModels/LineageChartViewModel.cs
//
// MVVM-proper view-model for the NEW tidy-forest lineage chart (plan PR-L4).
// It is deliberately SEPARATE from the shared LineageGraphViewModel that backs
// the witness-stemma view (decision D3) — this file must never be conflated
// with, nor edited into, that one.
//
// Responsibility: load the rich 609-record roster (ILineageRosterService),
// run the two pure porting modules — LineageGraphBuilder.Build (L2) then
// LineageForestLayout.Compute (L3) — and HOLD the resulting graph, layout and
// per-edge routes so the immediate-mode LineageChartControl (L4) can render
// them with zero layout logic of its own. It also owns the bindable interaction
// state (selection + search) that a future detail panel (L6) will bind to.
//
// The heavy work (build + layout over 609 nodes) is pure and fast (single-digit
// ms), so it runs once in the constructor; the result is immutable thereafter.
// Nothing here touches Avalonia — it is headless-testable.

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
// LineageEdge exists in both Infrastructure (the graph type we render) and Models
// (an unrelated record). Pin the name to the Infrastructure graph edge.
using LineageEdge = ReadZen.App.Infrastructure.LineageEdge;

namespace ReadZen.App.ViewModels;

public partial class LineageChartViewModel : ViewModelBase
{
    private readonly ILineageRosterService _roster;

    // node.Id (== master primary name) -> the raw record it was built from.
    // Source pseudo-nodes have no record, so SelectedMaster is null for them.
    private readonly Dictionary<string, LineageMasterRecord> _recordById =
        new(StringComparer.Ordinal);

    /// <summary>The normalized graph (nodes, edges, honest report). Never null.</summary>
    public LineageGraph Graph { get; private set; } = new();

    /// <summary>World bounds + per-edge routing produced by the tidy-forest layout.</summary>
    public LayoutResult Layout { get; private set; } = new();

    /// <summary>All nodes (masters plus synthesized book sources), with X/Y/Layer/Order filled.</summary>
    public IReadOnlyList<LineageNode> Nodes => Graph.Nodes;

    /// <summary>All teacher/book edges.</summary>
    public IReadOnlyList<LineageEdge> Edges => Graph.Edges;

    /// <summary>edge -&gt; routing polyline the control draws. Reference-keyed.</summary>
    public IReadOnlyDictionary<LineageEdge, EdgeRoute> Routes => Layout.Routes;

    /// <summary>Honest build tallies (masters, roots, dangling, bad attestation…).</summary>
    public LineageReport Report => Graph.Report;

    /// <summary>True once a non-empty graph has loaded.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>The node the user last clicked (set by the control's hit-test), or null.</summary>
    [ObservableProperty]
    private LineageNode? _selectedNode;

    /// <summary>The contested edge whose vermilion seal the user last clicked, or null.
    /// L5 sets this when a seal is hit (alongside <see cref="SelectedNode"/> = the
    /// student), so L6's two-sided dispute card can bind to the exact edge and its
    /// <see cref="LineageEdge.Contested"/> rival hypothesis.</summary>
    [ObservableProperty]
    private LineageEdge? _selectedEdge;

    /// <summary>The raw record behind <see cref="SelectedNode"/> (null for book sources / stubs).
    /// Kept in sync automatically; the L6 detail panel will bind to it.</summary>
    [ObservableProperty]
    private LineageMasterRecord? _selectedMaster;

    /// <summary>Free-text master search (L6 seam). Setting it recomputes <see cref="SearchHitIds"/>.</summary>
    [ObservableProperty]
    private string _searchText = "";

    /// <summary>node.Id set of the current search matches — the control highlights these.</summary>
    public IReadOnlyCollection<string> SearchHitIds => _searchHitIds;
    private readonly HashSet<string> _searchHitIds = new(StringComparer.Ordinal);

    /// <summary>The projected detail-panel view for the current selection (L6), or null when
    /// nothing is selected. Recomputed whenever <see cref="SelectedNode"/> /
    /// <see cref="SelectedEdge"/> change; the side panel binds to it.</summary>
    [ObservableProperty]
    private LineageDetailViewModel? _detail;

    /// <summary>True while a node is selected (drives the panel-vs-placeholder switch).</summary>
    public bool HasDetail => Detail != null;

    // ── L6 interaction seams (set by the host window; all optional). Kept as plain
    //    delegates so the VM stays headless — no Avalonia / OS types leak in here. ──

    /// <summary>Open an external URL in the system browser.</summary>
    public Action<string>? OpenUrlHandler { get; set; }
    /// <summary>Navigate the corpus reader to a TEI path (stele / in-corpus book).</summary>
    public Action<string>? NavigateCorpusHandler { get; set; }
    /// <summary>Open the full master profile (List tab) for a node.</summary>
    public Action<LineageNode>? OpenProfileHandler { get; set; }
    /// <summary>Open the corpus-appearances view (Corpus tab) for a node.</summary>
    public Action<LineageNode>? OpenCorpusSearchHandler { get; set; }
    /// <summary>Raised when a node is focused from the panel (teacher / heir / book link)
    /// so the host can centre the chart on it and mirror the List-tab selection.</summary>
    public event Action<LineageNode>? NodeFocusRequested;

    public LineageChartViewModel(ILineageRosterService roster)
    {
        _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        Load();
    }

    private void Load()
    {
        try
        {
            var masters = _roster.GetAll();

            // id -> record map (build's node id is the record's first name; last wins on a
            // duplicate, mirroring LineageGraphBuilder's byId semantics).
            _recordById.Clear();
            foreach (var m in masters)
                if (m.Names is { Count: > 0 } && !string.IsNullOrEmpty(m.Names[0]))
                    _recordById[m.Names[0]] = m;

            Graph = LineageGraphBuilder.Build(masters);
            Layout = LineageForestLayout.Compute(Graph.Nodes, Graph.Edges);
            IsLoaded = Graph.Nodes.Count > 0;
        }
        catch
        {
            // Fail-soft, matching the roster loader: an unexpected fault yields an
            // empty-but-valid chart rather than taking down the host window.
            Graph = new LineageGraph();
            Layout = new LayoutResult();
            IsLoaded = false;
        }
    }

    /// <summary>Look up the raw record behind a node (null for sources / unknown ids).</summary>
    public LineageMasterRecord? RecordFor(LineageNode? node)
        => node != null && _recordById.TryGetValue(node.Id, out var rec) ? rec : null;

    partial void OnSelectedNodeChanged(LineageNode? value)
    {
        SelectedMaster = RecordFor(value);
        Detail = value != null ? BuildDetail(value) : null;
    }

    partial void OnSelectedEdgeChanged(LineageEdge? value)
    {
        // A contested-seal click sets SelectedEdge then SelectedNode; rebuild so the
        // dispute card binds to the exact edge's rival hypothesis.
        if (SelectedNode != null) Detail = BuildDetail(SelectedNode);
    }

    partial void OnDetailChanged(LineageDetailViewModel? value)
        => OnPropertyChanged(nameof(HasDetail));

    /// <summary>Project a node into the bound detail-panel view (L6). Pure; headless.</summary>
    private LineageDetailViewModel BuildDetail(LineageNode node)
    {
        var ctx = new LineageDetailContext
        {
            Focus = FocusNode,
            OpenUrl = url => OpenUrlHandler?.Invoke(url),
            NavigateCorpus = path => NavigateCorpusHandler?.Invoke(path),
            OpenProfile = n => OpenProfileHandler?.Invoke(n),
            OpenCorpusSearch = n => OpenCorpusSearchHandler?.Invoke(n),
        };
        return new LineageDetailViewModel(node, SelectedEdge, ctx);
    }

    /// <summary>Focus (select + request centring on) a node — the panel's teacher/heir/book links.</summary>
    public void FocusNode(LineageNode? node)
    {
        if (node == null) return;
        SelectedEdge = null;
        SelectedNode = node;
        NodeFocusRequested?.Invoke(node);
    }

    /// <summary>Focus a node by its id or any name/alias (used by list-selection sync).</summary>
    public LineageNode? FocusNodeByName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (!Graph.ById.TryGetValue(name!, out var node))
            Graph.ByName.TryGetValue(name!, out node);
        if (node != null) FocusNode(node);
        return node;
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchHitIds.Clear();
        var q = (value ?? "").Trim();
        if (q.Length > 0)
        {
            foreach (var n in Graph.Nodes)
            {
                foreach (var name in n.Names)
                {
                    // Ordinal, case-insensitive — InvariantGlobalization forbids locale ops.
                    if (name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _searchHitIds.Add(n.Id);
                        break;
                    }
                }
            }
        }
        OnPropertyChanged(nameof(SearchHitIds));
    }
}
