// Infrastructure/LineageForestLayout.cs
//
// Pure, DOM-free port of the SPA's tidy-forest layout engine
// (ZenLinkPage/lib/lineage-layout.js). Given the { nodes, edges } graph produced
// by LineageGraphBuilder (PR-L2), it assigns layer/x/y/order to every node and
// produces each edge's routing polyline, so the forthcoming lineage control
// (PR-L4) can draw the hanging-scroll chart without any layout logic of its own.
//
// WHY A CONTOUR PACKER, NOT SUGIYAMA (from the JS header, verbatim in spirit):
// the lineage graph is a strict FOREST (every master has at most one parent
// edge). The first SPA attempt used a general Sugiyama pipeline; its barycenter
// passes let subtrees drift thousands of pixels sideways, producing a ~26,000px
// world with teacher-and-heir joined by long horizontal wires. This is a
// Reingold-Tilford style contour packer instead:
//   * every parent is centered ABOVE its children block (edges read as vertical
//     descent by construction, no crossing tree edges);
//   * sibling subtrees pack against each other's per-layer contours, so the
//     world is as narrow as the tree allows;
//   * runs of >= STACK_MIN sibling LEAF masters fold into vertical columns
//     (breadth becomes depth) with a reserved drop-line gutter;
//   * root trees pack LEFT or RIGHT of the great tree by whichever grows the
//     world less.
// Overlap safety is guaranteed by construction and RATCHETED by AssertNoOverlaps
// (ported first, driven red->green against the real 609-master roster).
//
// PORTING NOTES (JS -> C#), kept faithful on purpose:
//  * JS `Map` with OBJECT keys -> Dictionary<LineageNode,...> with reference
//    equality (LineageNode is a class; default equality is reference — the same
//    pattern the L2 port established).
//  * `n.year || 9999`: JS treats 0 AND null as "missing" -> YearKey() maps both
//    to 9999.
//  * JS Array.prototype.sort is STABLE; List<T>.Sort is NOT — every JS sort is
//    ported as OrderBy/ThenBy (stable) so layouts are deterministic.
//  * `localeCompare` -> string.CompareOrdinal (InvariantGlobalization=true).
//  * Guard-counter loop bounds are runaway guards — preserved exactly.
//  * `n.capsule` / `n.dummy` (L5 evidence layer) do not exist in the L2 graph;
//    they are treated as false, which is exactly the current data.
//  * edge.fromPort/toPort/points have no home on LineageEdge (owned by L2), so
//    routing is returned OUT-OF-BAND in LayoutResult.Routes (a
//    Dictionary<LineageEdge, EdgeRoute>) rather than mutated onto the edge.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Infrastructure;

/// <summary>A point on an edge routing polyline (world coordinates).</summary>
public readonly struct LayoutPoint
{
    public double X { get; }
    public double Y { get; }
    public LayoutPoint(double x, double y) { X = x; Y = y; }
}

/// <summary>The routing of one edge: port offsets plus the polyline the renderer draws.</summary>
public sealed class EdgeRoute
{
    public double FromPort { get; internal set; }
    public double ToPort { get; internal set; }
    public IReadOnlyList<LayoutPoint> Points { get; internal set; } = Array.Empty<LayoutPoint>();
}

/// <summary>Result of <see cref="LineageForestLayout.Compute"/>: world bounds + per-edge routing.</summary>
public sealed class LayoutResult
{
    public double Width { get; init; }
    public double Height { get; init; }
    public double MinX { get; init; }
    public double MaxX { get; init; }
    /// <summary>edge -&gt; its routing (port offsets + polyline). Reference-keyed.</summary>
    public IReadOnlyDictionary<LineageEdge, EdgeRoute> Routes { get; init; }
        = new Dictionary<LineageEdge, EdgeRoute>();
}

/// <summary>Result of the headless overlap ratchet (parity with the JS assertNoOverlaps).</summary>
public sealed class OverlapResult
{
    public bool Ok { get; init; }
    public int NodeNode { get; init; }
    public int EdgeNode { get; init; }
    public IReadOnlyList<string> Samples { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Pure tidy-forest contour packer. Instance-per-run (the fields mirror the JS
/// closures over its Maps); call the static <see cref="Compute"/>. No Avalonia,
/// no I/O, no DOM — headless-testable like RowGridBuilder and LineageGraphBuilder.
/// </summary>
public sealed class LineageForestLayout
{
    // ── Constants (copied VERBATIM from lineage-layout.js:30-40). ──
    public const double NODE_W = 132;
    public const double NODE_H = 40;
    public const double SOURCE_W = 44;
    public const double LAYER_PITCH = 150;   // 40 node + 110 edge channel

    private const double GAP_SIBLING = 18;   // gutter between sibling subtree contours
    private const double GAP_TREE = 30;      // gutter between separate root trees
    private const int STACK_MIN = 2;         // >= this many sibling leaves fold vertical
    private const int STACK_COL_ROWS = 6;    // target rows per stack column
    private const int STACK_COLS_MAX = 2;
    private const double STACK_GUT = 16;     // drop-line gutter left of every stack column

    // Contiguous school bands: Linji left-of-center, Caodong right, houses fanning.
    private static readonly IReadOnlyDictionary<string, int> SchoolOrder = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["niutou"] = -6,
        ["early-chan"] = -5,
        ["heze"] = -4,
        ["hongzhou"] = -3,
        ["linji"] = -2,
        ["guiyang"] = -1,
        ["source"] = 0,
        ["pre-chan"] = 0,
        ["other"] = 0,
        ["shitou"] = 2,
        ["caodong"] = 3,
        ["fayan"] = 4,
        ["yunmen"] = 5,
        ["korean-seon"] = 8,
    };

    // ── Per-run state (the JS closure variables, one instance per Compute). ──
    private readonly Dictionary<LineageNode, List<LineageEdge>> _outR = new();
    private readonly Dictionary<LineageNode, List<LineageEdge>> _inR = new();
    private readonly Dictionary<LineageNode, List<(LineageNode Src, LineageEdge Edge)>> _sourcesOf = new();
    private readonly Dictionary<LineageNode, List<LineageNode>> _childrenOf = new();
    private readonly Dictionary<LineageNode, StackCell> _stack = new();
    private readonly Dictionary<LineageNode, int> _sizeOf = new();
    private readonly Dictionary<LineageEdge, EdgeRoute> _routes = new();

    private sealed class StackCell
    {
        public LineageNode Parent = null!;
        public int Col, Row, Cols, Rows;
    }

    // A mutable per-layer contour span {min,max}. Dictionary<int, MinMax>.
    private sealed class MinMax
    {
        public double Min, Max;
        public MinMax(double min, double max) { Min = min; Max = max; }
    }

    private sealed class SubtreeResult
    {
        public List<LineageNode> List = new();
        public Dictionary<int, MinMax> Contour = new();
        public double Anchor;
    }

    private LineageForestLayout() { }

    private static double WidthOf(LineageNode n) => n.IsSource ? SOURCE_W : NODE_W; // (no capsules in the L2 graph)
    private static double HalfW(LineageNode n) => WidthOf(n) / 2;

    // JS `n.year || 9999`: 0 AND null are "missing".
    private static int YearKey(LineageNode n) => (n.Year ?? 0) == 0 ? 9999 : n.Year!.Value;

    private static int SchoolRank(LineageNode n)
    {
        if (n.Korean) return 8;
        return SchoolOrder.TryGetValue(n.SchoolKey ?? "", out var v) ? v : 0;
    }

    private EdgeRoute Route(LineageEdge e)
    {
        if (!_routes.TryGetValue(e, out var r)) { r = new EdgeRoute(); _routes[e] = r; }
        return r;
    }

    /// <summary>
    /// Compute a full layout in place: assigns Layer/X/Y/Order to every node in
    /// <paramref name="nodes"/> and returns world bounds plus per-edge routing.
    /// </summary>
    public static LayoutResult Compute(IReadOnlyList<LineageNode> nodes, IReadOnlyList<LineageEdge> edges)
        => new LineageForestLayout().Run(nodes ?? Array.Empty<LineageNode>(), edges ?? Array.Empty<LineageEdge>());

    private LayoutResult Run(IReadOnlyList<LineageNode> nodes, IReadOnlyList<LineageEdge> edges)
    {
        foreach (var n in nodes) { n.Layer = -1; _stack.Remove(n); n.X = 0; }

        // adjacency — book-source edges are ANNOTATIONS, not tree edges (a master
        // can hold several books, which would break the forest invariant). They
        // are collected per master and shelved above him later.
        foreach (var n in nodes) { _outR[n] = new List<LineageEdge>(); _inR[n] = new List<LineageEdge>(); }
        foreach (var e in edges)
        {
            if (!_outR.ContainsKey(e.From) || !_inR.ContainsKey(e.To)) continue;
            if (e.From.IsSource)
            {
                if (!_sourcesOf.TryGetValue(e.To, out var lst)) { lst = new(); _sourcesOf[e.To] = lst; }
                lst.Add((e.From, e));
                continue;
            }
            _outR[e.From].Add(e); _inR[e.To].Add(e);
        }

        // ── Stage 1: layering (generation = depth; dates nudge parentless nodes) ──
        var roots = nodes.Where(n => !n.IsSource && _inR[n].Count == 0).ToList();
        AssignLongestPath(nodes, roots, keepMin: false);
        DateNudge(nodes);
        AssignLongestPath(nodes, roots, keepMin: true);

        // normalize min layer to 0
        int minLayer = int.MaxValue;
        foreach (var n in nodes) if (n.Layer < minLayer) minLayer = n.Layer;
        if (minLayer != 0 && minLayer != int.MaxValue) foreach (var n in nodes) n.Layer -= minLayer;

        // children lists (tree edges; a node is positioned under its FIRST parent)
        foreach (var n in nodes) _childrenOf[n] = new List<LineageNode>();
        var seenChild = new HashSet<LineageNode>();
        foreach (var e in edges)
        {
            if (!_outR.ContainsKey(e.From) || !_inR.ContainsKey(e.To) || e.From.IsSource) continue;
            if (seenChild.Contains(e.To)) continue;      // defensive: forest by contract
            seenChild.Add(e.To);
            _childrenOf[e.From].Add(e.To);
        }

        // ── Stage 2: leaf stacking — sibling leaves become vertical columns ──
        foreach (var p in nodes)
        {
            var kids = _childrenOf[p];
            if (kids.Count == 0) continue;
            var leaves = kids.Where(k =>
                _childrenOf[k].Count == 0 && !k.IsSource &&
                !_sourcesOf.ContainsKey(k)).ToList();   // a master with a book shelf never stacks
            if (leaves.Count < STACK_MIN) continue;
            int cols = Math.Max(1, Math.Min(STACK_COLS_MAX,
                (int)Math.Ceiling(leaves.Count / (double)STACK_COL_ROWS)));
            int rows = (int)Math.Ceiling(leaves.Count / (double)cols);
            // stable: (year||9999) asc, then id ordinal
            leaves = leaves
                .OrderBy(YearKey)
                .ThenBy(k => k.Id ?? "", StringComparer.Ordinal)
                .ToList();
            for (int i = 0; i < leaves.Count; i++)
            {
                var k = leaves[i];
                // column-major: each column reads top -> bottom
                int col = i / rows, row = i % rows;
                _stack[k] = new StackCell { Parent = p, Col = col, Row = row, Cols = cols, Rows = rows };
                k.Layer = p.Layer + 1 + row;
            }
        }

        // ── Stage 3: tidy contour packing (Reingold-Tilford over the forest) ──
        // subtree sizes so the great tree packs first and small trees tuck after
        foreach (var r in roots) Measure(r);
        var orderedRoots = roots
            .OrderByDescending(r => _sizeOf.TryGetValue(r, out var s) ? s : 0)
            .ThenBy(YearKey)
            .ThenBy(r => r.Id ?? "", StringComparer.Ordinal)
            .ToList();

        // Pack root trees around the great tree, LEFT or RIGHT, whichever grows
        // the world less — unresolved-teacher subtrees become wings, not a tail.
        var global = new Dictionary<int, MinMax>();
        double gMin = double.PositiveInfinity, gMax = double.NegativeInfinity;
        foreach (var r in orderedRoots)
        {
            var res = LayoutSubtree(r);
            double cMin = double.PositiveInfinity, cMax = double.NegativeInfinity;
            foreach (var e in res.Contour.Values) { cMin = Math.Min(cMin, e.Min); cMax = Math.Max(cMax, e.Max); }
            double delta = 0;
            if (global.Count > 0)
            {
                double dR = ShiftFor(global, res.Contour, GAP_TREE);
                double dL = ShiftForLeft(global, res.Contour, GAP_TREE);
                double wR = Math.Max(gMax, cMax + dR) - Math.Min(gMin, cMin + dR);
                double wL = Math.Max(gMax, cMax + dL) - Math.Min(gMin, cMin + dL);
                delta = wL < wR ? dL : dR;
            }
            ApplyShift(res.List, delta);
            MergeInto(global, res.Contour, delta);
            gMin = Math.Min(gMin, cMin + delta);
            gMax = Math.Max(gMax, cMax + delta);
        }

        // ── y + edge routing ──
        foreach (var n in nodes) n.Y = n.Layer * LAYER_PITCH;
        AssignPorts(nodes);
        foreach (var e in edges) BuildEdgePoints(e);

        // per-layer x-rank -> Order (deterministic; not in the JS but the scaffold field)
        AssignOrder(nodes);

        // bounds
        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = 0;
        foreach (var n in nodes)
        {
            minX = Math.Min(minX, n.X - HalfW(n));
            maxX = Math.Max(maxX, n.X + HalfW(n));
            maxY = Math.Max(maxY, n.Y + NODE_H / 2);
        }
        if (double.IsInfinity(minX)) { minX = 0; maxX = 0; }

        return new LayoutResult
        {
            Width = maxX - minX,
            Height = maxY,
            MinX = minX,
            MaxX = maxX,
            Routes = _routes,
        };
    }

    // ── contour helpers ──

    private static void ContourAdd(Dictionary<int, MinMax> contour, int layer, double min, double max)
    {
        if (!contour.TryGetValue(layer, out var e)) contour[layer] = new MinMax(min, max);
        else { e.Min = Math.Min(e.Min, min); e.Max = Math.Max(e.Max, max); }
    }

    // how far RIGHT to shift `contour` so it clears `merged` by `gap`.
    private static double ShiftFor(Dictionary<int, MinMax> merged, Dictionary<int, MinMax> contour, double gap)
    {
        double delta = double.NegativeInfinity;
        foreach (var kv in contour)
            if (merged.TryGetValue(kv.Key, out var m))
                delta = Math.Max(delta, m.Max + gap - kv.Value.Min);
        return double.IsInfinity(delta) ? 0 : delta;
    }

    // how far LEFT (negative) to shift `contour` so it clears `merged` by `gap`.
    private static double ShiftForLeft(Dictionary<int, MinMax> merged, Dictionary<int, MinMax> contour, double gap)
    {
        double delta = double.PositiveInfinity;
        foreach (var kv in contour)
            if (merged.TryGetValue(kv.Key, out var m))
                delta = Math.Min(delta, m.Min - gap - kv.Value.Max);
        return double.IsInfinity(delta) ? 0 : delta;
    }

    private static void ApplyShift(List<LineageNode> list, double dx)
    {
        if (dx != 0) foreach (var n in list) n.X += dx;
    }

    private static Dictionary<int, MinMax> MergeInto(Dictionary<int, MinMax> merged, Dictionary<int, MinMax> contour, double dx)
    {
        foreach (var kv in contour) ContourAdd(merged, kv.Key, kv.Value.Min + dx, kv.Value.Max + dx);
        return merged;
    }

    // ── subtree layout ──

    private SubtreeResult LayoutStack(List<LineageNode> leaves)
    {
        // vertical columns; every column reserves a drop-line gutter on its left
        double colPitch = NODE_W + STACK_GUT;
        var contour = new Dictionary<int, MinMax>();
        foreach (var k in leaves)
        {
            k.X = _stack[k].Col * colPitch;
            ContourAdd(contour, k.Layer, k.X - NODE_W / 2 - STACK_GUT, k.X + NODE_W / 2);
        }
        double mn = double.PositiveInfinity, mx = double.NegativeInfinity;
        foreach (var e in contour.Values) { mn = Math.Min(mn, e.Min); mx = Math.Max(mx, e.Max); }
        return new SubtreeResult { List = leaves.ToList(), Contour = contour, Anchor = (mn + mx) / 2 };
    }

    // Book sources: shelved in a row directly ABOVE their master, inside his
    // subtree's contour so nothing else can pack into that shelf.
    private void ShelveSources(LineageNode n, List<LineageNode> list, Dictionary<int, MinMax> contour)
    {
        if (!_sourcesOf.TryGetValue(n, out var srcs) || srcs.Count == 0) return;
        double pitch = SOURCE_W + 12;
        double total = srcs.Count * pitch - 12;
        for (int i = 0; i < srcs.Count; i++)
        {
            var (src, edge) = srcs[i];
            src.Layer = n.Layer - 1;
            src.X = n.X - total / 2 + SOURCE_W / 2 + i * pitch;
            var route = Route(edge);
            route.FromPort = 0;
            route.ToPort = srcs.Count > 1
                ? (i - (srcs.Count - 1) / 2.0) * Math.Min(NODE_W / (srcs.Count + 1), 22)
                : 0;
            list.Add(src);
        }
        ContourAdd(contour, n.Layer - 1, n.X - total / 2, n.X + total / 2);
    }

    private SubtreeResult LayoutSubtree(LineageNode n)
    {
        var all = _childrenOf[n];
        var stacked = all.Where(k => _stack.TryGetValue(k, out var s) && ReferenceEquals(s.Parent, n)).ToList();
        var items = all.Where(k => !(_stack.TryGetValue(k, out var s) && ReferenceEquals(s.Parent, n)))
            .OrderBy(SchoolRank)
            .ThenBy(YearKey)
            .ThenBy(k => k.Id ?? "", StringComparer.Ordinal)
            .ToList();

        if (items.Count == 0 && stacked.Count == 0)
        {
            n.X = 0;
            var leafContour = new Dictionary<int, MinMax>();
            ContourAdd(leafContour, n.Layer, -HalfW(n), HalfW(n));
            var leafList = new List<LineageNode> { n };
            ShelveSources(n, leafList, leafContour);
            return new SubtreeResult { List = leafList, Contour = leafContour, Anchor = 0 };
        }

        Dictionary<int, MinMax>? merged = null;
        var list = new List<LineageNode> { n };
        var anchors = new List<double>();
        var weights = new List<double>();

        void Place(SubtreeResult r, double w)
        {
            double delta = merged != null ? ShiftFor(merged, r.Contour, GAP_SIBLING) : 0;
            ApplyShift(r.List, delta);
            merged = MergeInto(merged ?? new Dictionary<int, MinMax>(), r.Contour, delta);
            foreach (var m in r.List) list.Add(m);
            anchors.Add(r.Anchor + delta);
            weights.Add(w);
        }

        foreach (var c in items) Place(LayoutSubtree(c), Math.Sqrt(_sizeOf.TryGetValue(c, out var sz) ? sz : 1));
        if (stacked.Count > 0) Place(LayoutStack(stacked), Math.Sqrt(stacked.Count));

        // Size-weighted centering (sqrt-damped): the trunk leans over its heavier
        // branches — the main descent line runs near-straight — without fully
        // orphaning the light twigs.
        double wSum = 0, wx = 0;
        for (int i = 0; i < anchors.Count; i++) { wSum += weights[i]; wx += anchors[i] * weights[i]; }
        n.X = wSum > 0 ? wx / wSum : (anchors[0] + anchors[anchors.Count - 1]) / 2;

        merged ??= new Dictionary<int, MinMax>();
        ContourAdd(merged, n.Layer, n.X - HalfW(n), n.X + HalfW(n));
        ShelveSources(n, list, merged);
        return new SubtreeResult { List = list, Contour = merged, Anchor = n.X };
    }

    private int Measure(LineageNode n)
    {
        if (_sizeOf.TryGetValue(n, out var cached)) return cached;
        _sizeOf[n] = 1; // pre-seed acts as the cycle guard (parity with JS)
        int s = 1;
        foreach (var c in _childrenOf[n]) s += Measure(c);
        _sizeOf[n] = s;
        return s;
    }

    // ── layering ──

    private void AssignLongestPath(IReadOnlyList<LineageNode> nodes, List<LineageNode> roots, bool keepMin)
    {
        if (!keepMin) foreach (var n in nodes) n.Layer = -1;
        // Kahn topological order
        var indeg = new Dictionary<LineageNode, int>();
        foreach (var n in nodes) indeg[n] = _inR[n].Count;
        var q = new List<LineageNode>(roots);
        foreach (var r in q) if (r.Layer < 0) r.Layer = 0;
        var seen = new HashSet<LineageNode>();
        int head = 0;
        while (head < q.Count)
        {
            var n = q[head++];
            if (seen.Contains(n)) continue;
            seen.Add(n);
            if (n.Layer < 0) n.Layer = 0;
            foreach (var e in _outR[n])
            {
                var c = e.To;
                if (c.Layer < n.Layer + 1) c.Layer = n.Layer + 1;
                indeg[c] = indeg[c] - 1;
                if (indeg[c] <= 0) q.Add(c);
            }
        }
        // any node left unlayered (cycle remnant): BFS relax
        foreach (var n in nodes) if (n.Layer < 0) n.Layer = 0;
        bool changed = true; int guard = 0;
        while (changed && guard++ < 100)
        {
            changed = false;
            foreach (var n in nodes)
                foreach (var e in _outR[n])
                    if (e.To.Layer < n.Layer + 1) { e.To.Layer = n.Layer + 1; changed = true; }
        }
    }

    private void DateNudge(IReadOnlyList<LineageNode> nodes)
    {
        // Median year per layer — computed from the LARGEST root tree only (the
        // Bodhidharma line, ~70% of the chart). Unresolved-teacher subtrees start
        // piled at layer 0; using them in the curve would drag the early layers to
        // the Ming dynasty and wreck the year->layer inversion about to place them.
        var roots = nodes.Where(n => _inR[n].Count == 0).ToList();

        int SizeOfRoot(LineageNode r)
        {
            int s = 0; var stack = new Stack<LineageNode>(); stack.Push(r); int guard = 0;
            while (stack.Count > 0 && guard++ < 5000)
            {
                var n = stack.Pop(); s++;
                foreach (var e in _outR[n]) stack.Push(e.To);
            }
            return s;
        }

        LineageNode? mainRoot = null; int mainSize = -1;
        foreach (var r in roots) { int s = SizeOfRoot(r); if (s > mainSize) { mainSize = s; mainRoot = r; } }

        var inMain = new HashSet<LineageNode>();
        if (mainRoot != null)
        {
            var stack = new Stack<LineageNode>(); stack.Push(mainRoot); int guard = 0;
            while (stack.Count > 0 && guard++ < 5000)
            {
                var n = stack.Pop(); inMain.Add(n);
                foreach (var e in _outR[n]) stack.Push(e.To);
            }
        }

        var byLayer = new Dictionary<int, List<int>>();
        foreach (var n in nodes)
        {
            if ((n.Year ?? 0) == 0 || !inMain.Contains(n)) continue;   // JS `!n.year` (0/null => skip)
            if (!byLayer.TryGetValue(n.Layer, out var lst)) { lst = new(); byLayer[n.Layer] = lst; }
            lst.Add(n.Year!.Value);
        }
        var med = new Dictionary<int, int>();
        foreach (var kv in byLayer)
        {
            var ys = kv.Value; ys.Sort();
            med[kv.Key] = ys[ys.Count / 2];
        }
        var layersSorted = med.Keys.OrderBy(x => x).ToList();
        if (layersSorted.Count < 2) return;

        double YearToLayer(int yr)
        {
            // piecewise-linear inverse of layer->medianYear
            int lo = layersSorted[0], hi = layersSorted[layersSorted.Count - 1];
            if (yr <= med[lo]) return lo;
            if (yr >= med[hi]) return hi;
            for (int i = 0; i < layersSorted.Count - 1; i++)
            {
                int a = layersSorted[i], b = layersSorted[i + 1];
                int ya = med[a], yb = med[b];
                if (yr >= ya && yr <= yb && yb != ya)
                    return a + (double)(b - a) * (yr - ya) / (yb - ya);
            }
            return lo;
        }

        // A root without its own dates borrows an estimate from its dated
        // descendants (year minus ~28y per generation).
        int? EstimateYear(LineageNode root)
        {
            if ((root.Year ?? 0) != 0) return root.Year;
            var ests = new List<int>();
            var stack = new Stack<(LineageNode Node, int Depth)>(); stack.Push((root, 0)); int guard = 0;
            while (stack.Count > 0 && guard++ < 2000)
            {
                var (n, d) = stack.Pop();
                if (d > 0 && (n.Year ?? 0) != 0) ests.Add(n.Year!.Value - 28 * d);
                foreach (var e in _outR[n]) stack.Push((e.To, d + 1));
            }
            if (ests.Count == 0) return null;
            ests.Sort();
            return ests[ests.Count / 2];
        }

        // Only parentless nodes get nudged downward (keeps chains straight).
        foreach (var n in nodes)
        {
            if (_inR[n].Count > 0) continue;
            var yr = EstimateYear(n);
            if (!yr.HasValue || yr.Value == 0) continue;
            int est = (int)Math.Round(YearToLayer(yr.Value), MidpointRounding.AwayFromZero);
            if (est > n.Layer) n.Layer = est;
        }
    }

    // ── ports + routing ──

    private double RouteX(LineageEdge e) =>
        _stack.ContainsKey(e.To)
            ? e.To.X - HalfW(e.To) - STACK_GUT / 2    // stack edges aim at the gutter
            : e.To.X;

    private void AssignPorts(IReadOnlyList<LineageNode> nodes)
    {
        foreach (var n in nodes)
        {
            var outs = _outR[n].OrderBy(RouteX).ToList();   // stable
            int k = outs.Count;
            for (int i = 0; i < outs.Count; i++)
            {
                double spread = Math.Min(NODE_W / (k + 1), 22);
                Route(outs[i]).FromPort = k > 1 ? (i - (k - 1) / 2.0) * spread : 0;
            }
            var ins = _inR[n].OrderBy(e => e.From.X).ToList();   // stable
            int m = ins.Count;
            for (int i = 0; i < ins.Count; i++)
            {
                double spread = Math.Min(NODE_W / (m + 1), 22);
                Route(ins[i]).ToPort = m > 1 ? (i - (m - 1) / 2.0) * spread : 0;
            }
        }
    }

    private void BuildEdgePoints(LineageEdge e)
    {
        var route = Route(e);
        if (_stack.ContainsKey(e.To))
        {
            // comb routing: diagonal into the column's reserved gutter inside the
            // teacher's edge channel, straight drop beside the column, short stub
            // into the leaf's left edge. The gutter is inside the stack block's
            // contour, so the drop can never cross a foreign box.
            double hw = HalfW(e.To);
            double gx = e.To.X - hw - STACK_GUT / 2;
            route.Points = new[]
            {
                new LayoutPoint(e.From.X + route.FromPort, e.From.Y + NODE_H / 2),
                new LayoutPoint(gx, e.From.Y + LAYER_PITCH - NODE_H / 2 - 8),
                new LayoutPoint(gx, e.To.Y),
                new LayoutPoint(e.To.X - hw, e.To.Y),
            };
            return;
        }
        route.Points = new[]
        {
            new LayoutPoint(e.From.X + route.FromPort, e.From.Y + NODE_H / 2),
            new LayoutPoint(e.To.X + route.ToPort, e.To.Y - NODE_H / 2),
        };
    }

    private static void AssignOrder(IReadOnlyList<LineageNode> nodes)
    {
        var byLayer = new Dictionary<int, List<LineageNode>>();
        foreach (var n in nodes)
        {
            if (!byLayer.TryGetValue(n.Layer, out var lst)) { lst = new(); byLayer[n.Layer] = lst; }
            lst.Add(n);
        }
        foreach (var lst in byLayer.Values)
        {
            var ordered = lst.OrderBy(n => n.X).ThenBy(n => n.Id ?? "", StringComparer.Ordinal).ToList();
            for (int i = 0; i < ordered.Count; i++) ordered[i].Order = i;
        }
    }

    /// <summary>
    /// Headless overlap ratchet (parity with lineage-layout.js assertNoOverlaps).
    /// Returns counts + first offenders rather than throwing, so a test can assert
    /// on the numbers. Feed it the same nodes/edges Compute saw and its
    /// <see cref="LayoutResult.Routes"/>.
    /// </summary>
    public static OverlapResult AssertNoOverlaps(
        IEnumerable<LineageNode> nodes,
        IEnumerable<LineageEdge> edges,
        IReadOnlyDictionary<LineageEdge, EdgeRoute> routes)
    {
        var real = nodes.ToList();   // no dummy nodes in the forest port
        bool ok = true;
        int nodeNode = 0, edgeNode = 0;
        var samples = new List<string>();

        // node-node: only same-layer pairs can collide (layers differ in y >= pitch)
        var byLayer = new Dictionary<int, List<LineageNode>>();
        foreach (var n in real)
        {
            if (!byLayer.TryGetValue(n.Layer, out var lst)) { lst = new(); byLayer[n.Layer] = lst; }
            lst.Add(n);
        }
        foreach (var kv in byLayer)
        {
            var arr = kv.Value.OrderBy(n => n.X).ToList();
            for (int i = 1; i < arr.Count; i++)
            {
                var a = arr[i - 1]; var b = arr[i];
                double gap = (b.X - HalfW(b)) - (a.X + HalfW(a));
                if (gap < -0.01)
                {
                    ok = false; nodeNode++;
                    if (samples.Count < 8)
                        samples.Add($"NODE<->NODE L{a.Layer}: {a.Id} / {b.Id} overlap {gap:F1}px");
                }
            }
        }

        // edge-node: sample each edge polyline, test against non-endpoint node rects
        foreach (var e in edges)
        {
            if (routes == null || !routes.TryGetValue(e, out var route) || route.Points.Count == 0) continue;
            var pts = route.Points;
            var candidates = new List<LineageNode>();
            for (int L = Math.Min(e.From.Layer, e.To.Layer); L <= Math.Max(e.From.Layer, e.To.Layer); L++)
                if (byLayer.TryGetValue(L, out var lst))
                    foreach (var n in lst) { if (ReferenceEquals(n, e.From) || ReferenceEquals(n, e.To)) continue; candidates.Add(n); }
            if (candidates.Count == 0) continue;

            bool hit = false;
            for (int s = 0; s < pts.Count - 1 && !hit; s++)
            {
                var p0 = pts[s]; var p1 = pts[s + 1];
                for (int t = 0; t <= 8 && !hit; t++)
                {
                    double x = p0.X + (p1.X - p0.X) * t / 8.0;
                    double y = p0.Y + (p1.Y - p0.Y) * t / 8.0;
                    foreach (var n in candidates)
                    {
                        if (x > n.X - HalfW(n) && x < n.X + HalfW(n) &&
                            y > n.Y - NODE_H / 2 && y < n.Y + NODE_H / 2)
                        {
                            ok = false; edgeNode++;
                            if (samples.Count < 8)
                                samples.Add($"EDGE<->NODE {e.From.Id}->{e.To.Id} hits {n.Id} @({x:F0},{y:F0})");
                            hit = true; break;
                        }
                    }
                }
            }
        }

        return new OverlapResult { Ok = ok, NodeNode = nodeNode, EdgeNode = edgeNode, Samples = samples };
    }
}
