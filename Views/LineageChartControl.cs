// Views/LineageChartControl.cs
//
// Immediate-mode Avalonia control that renders the NEW tidy-forest lineage
// chart (plan PR-L4) — the desktop parity port of ZenLinkPage/views/lineage-
// graph.js. It is a BRAND-NEW control: it must never be conflated with, nor
// edited into, the shared LineageWebControl that backs the witness stemma
// (decision D3). It reuses the proven pan/zoom/cull/hit-test patterns from that
// control but owns none of its state.
//
// THE GOVERNING RULE OF THE WHOLE CHART — "ink must be earned":
//   attestation -> edge INK (ATT_STYLES);  a missing/unknown grade renders as
//   the WEAKEST style (D, faint dots), never a confident solid line. That
//   fail-safe lives in StyleFor() and is the single most important behavior
//   here — it is a pure, static, unit-tested method for exactly that reason.
//
// HOT-PATH DISCIPLINE (the old control's habit we do NOT copy): every Pen and
// every FormattedText is built ONCE — pens per attestation style, a NodeVisual
// (fill/stroke/label brush + cached hanja/romanized/dates FormattedText) per
// node — and rebuilt only when the theme variant changes. Render() allocates
// nothing per frame beyond transient geometry points.
//
// L5 SEAMS (left intentionally, clearly marked below): transmission geometry
// (遙嗣 circle / 代囑 diamond / book 冊 glyph / off-chart ⊣ stub), the contested
// vermilion seal + rival arc, book-source click/interactivity, focus-dim, the
// zoom floor tuning, and the legend. L4 renders nodes + edges + bilingual
// labels + book-source capsules (label only), and wires selection.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using ReadZen.App.Infrastructure;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

/// <summary>
/// One attestation edge style. Plain value type (no Avalonia types) so
/// <see cref="LineageChartControl.StyleFor"/> stays pure and headless-testable.
/// </summary>
public readonly struct AttStyle
{
    /// <summary>Canonical grade this style represents ("A"/"B"/"C"/"D").</summary>
    public string Grade { get; }
    public double Width { get; }
    /// <summary>Dash pattern (empty = solid), in the SPA's ATT_STYLES units.</summary>
    public IReadOnlyList<double> Dash { get; }
    public double Opacity { get; }
    /// <summary>D only: drawn in the muted (not text) colour.</summary>
    public bool Faint { get; }

    public AttStyle(string grade, double width, double[] dash, double opacity, bool faint)
    {
        Grade = grade; Width = width; Dash = dash; Opacity = opacity; Faint = faint;
    }
}

public sealed class LineageChartControl : Control
{
    // ── Attestation → edge ink. Verbatim from lineage-graph.js:19-24. ──
    // A: 2.25 solid  0.85 | B: 1.40 solid 0.60 | C: 1.20 dash[7,5] 0.50 |
    // D: 1.00 dash[1.5,4.5] 0.40 faint.  No Avalonia types here → safe at
    // static-init time with no running Application (the fail-safe test relies
    // on this).
    private static readonly AttStyle StyleA = new("A", 2.25, Array.Empty<double>(), 0.85, false);
    private static readonly AttStyle StyleB = new("B", 1.40, Array.Empty<double>(), 0.60, false);
    private static readonly AttStyle StyleC = new("C", 1.20, new double[] { 7, 5 }, 0.50, false);
    private static readonly AttStyle StyleD = new("D", 1.00, new double[] { 1.5, 4.5 }, 0.40, true);

    /// <summary>
    /// THE FAIL-SAFE. Parity with <c>styleFor(att) = ATT_STYLES[att] ?? ATT_STYLES.D</c>.
    /// Only the exact strings "A"/"B"/"C" earn a stronger style; EVERYTHING else —
    /// null, "", "D", a misspelling ("X"), a future grade ("E"), whitespace — falls
    /// to D (faint dots). It is structurally impossible for an unknown value to pick
    /// A/B/C. This inverts the original sin where a missing grade read as certain.
    /// </summary>
    public static AttStyle StyleFor(string? att) => att switch
    {
        "A" => StyleA,
        "B" => StyleB,
        "C" => StyleC,
        _ => StyleD,
    };

    // ── Transmission → edge GEOMETRY. A SEPARATE channel from attestation-ink:
    //    this maps the transmission MODE to the marker shape, and nothing here
    //    ever touches ink weight/opacity. Parity with lineage-graph.js
    //    drawMidGlyph / drawDisputedEdge (lines ~389-435). ──
    public enum TransmissionMarker
    {
        /// <summary>"direct" (or anything unrecognized): no extra geometry.</summary>
        None,
        /// <summary>遙嗣 remote/posthumous succession: an empty circle at the edge midpoint.</summary>
        RemoteCircle,
        /// <summary>代囑 succession by proxy: an empty diamond at the edge midpoint.</summary>
        ProxyDiamond,
        /// <summary>book transmission: the 冊 sutra glyph on the edge.</summary>
        Book,
        /// <summary>disputed: twin parallel strands — a fork that never resolves.</summary>
        Disputed,
    }

    /// <summary>
    /// Pure transmission-mode → marker mapping (headless-testable, Avalonia-free
    /// logic). Ordinal string match (InvariantGlobalization). Only the exact SPA
    /// transmission tokens earn a marker; everything else is <see cref="TransmissionMarker.None"/>.
    /// This is the transmission CHANNEL and must never be conflated with the
    /// attestation channel (<see cref="StyleFor"/>).
    /// </summary>
    public static TransmissionMarker MarkerFor(string? transmission) => transmission switch
    {
        "遙嗣" => TransmissionMarker.RemoteCircle,
        "代囑" => TransmissionMarker.ProxyDiamond,
        "book" => TransmissionMarker.Book,
        "disputed" => TransmissionMarker.Disputed,
        _ => TransmissionMarker.None,
    };

    /// <summary>
    /// True when a node's teacher is off-chart (an unresolved teacher_key or a
    /// TeacherDangling record) — it earns the ⊣… stub above its top edge. This is
    /// a NODE-level marker, distinct from the edge transmission markers above.
    /// </summary>
    public static bool IsOffChartTeacher(LineageNode? n) => n != null && n.Stub;

    /// <summary>
    /// Rival-arc weight selector (parity with the SPA /stele|contemporary|first/
    /// test): a rung backed by a stele / a contemporary / a first-generation
    /// witness draws a heavier arc. Case-sensitive ordinal, matching the SPA.
    /// </summary>
    private static bool IsStrongRung(string? rung)
        => !string.IsNullOrEmpty(rung) &&
           (rung.IndexOf("stele", StringComparison.Ordinal) >= 0 ||
            rung.IndexOf("contemporary", StringComparison.Ordinal) >= 0 ||
            rung.IndexOf("first", StringComparison.Ordinal) >= 0);

    private const double ZOOM_MIN = 0.05;   // low floor: the full 609 must fit on a laptop
    private const double ZOOM_MAX = 2.5;
    private const double DIM = 0.12;        // focus-dim alpha multiplier (parity: lineage-graph.js DIM)
    private const double MID_GLYPH_ZOOM = 0.5;  // transmission glyphs appear at/above this zoom
    private const double STUB_LABEL_ZOOM = 0.4; // the ⊣ cap appears at/above this zoom

    private LineageChartViewModel? _vm;

    // ── view transform (screen = world * zoom + offset) ──
    private double _offsetX, _offsetY;
    private double _zoom = 0.42;
    private Point _lastPan;
    private bool _isPanning;
    private bool _needsFit;

    // ── cached render resources (rebuilt only on theme change) ──
    private ThemeVariant? _builtVariant;
    private readonly Dictionary<string, IPen> _edgePens = new(StringComparer.Ordinal); // "A".."D"
    private readonly Dictionary<LineageNode, NodeVisual> _nodeVisuals = new();
    private IPen? _selectionPen;
    private IBrush _background = Brushes.Transparent;

    // L5 evidence-grammar resources (theme-scoped; NO per-frame allocation).
    // Each channel owns its own resources so the four channels never share ink.
    private IPen _glyphPenText = null!;   // 遙嗣/代囑 outline on a non-faint edge (text hue)
    private IPen _glyphPenMuted = null!;  // …on a faint (D) edge (muted hue)
    private FormattedText? _bookGlyph;    // 冊, the book-transmission mark
    private FormattedText? _stubGlyph;    // ⊣, the off-chart-teacher cap
    private IPen _stubPen = null!;        // dashed drop for the off-chart stub
    private IBrush _sealFill = Brushes.Transparent; // contested: vermilion @0.18
    private IPen _sealStroke = null!;               // contested: vermilion @1.0, w1.5
    private IPen _rivalPenStrong = null!;           // contested rival arc (stele/first rung)
    private IPen _rivalPenWeak = null!;             // contested rival arc (weaker rung)
    private IBrush _sourceFill = Brushes.Transparent; // folded-sutra capsule fill
    private IPen _sourceStroke = null!;               // …capsule + spine stroke (muted)
    private IPen _sourceLinePen = null!;              // …the three faint text lines
    private LegendCache? _legend;

    // ── focus-dim state (parity: recomputeRel + relevance). The selected node is
    //    the focus anchor; its ancestor path + direct heirs stay lit, all else
    //    dims to DIM. Recomputed only when the selection changes. ──
    private readonly HashSet<LineageNode> _relSet = new();
    private LineageNode? _relAnchor;
    private bool _hasFocus;

    // ── contested seal hit targets (world centre + edge), rebuilt every draw so a
    //    zoom/pan leaves the seals clickable at a fixed screen radius. ──
    private readonly List<(double X, double Y, LineageEdge Edge)> _seals = new();

    // world bounds (for fit); computed alongside the node-visual cache
    private double _minX, _maxX, _minY, _maxY;

    private sealed class NodeVisual
    {
        public IBrush Fill = Brushes.Transparent;
        public IPen Stroke = null!;
        public FormattedText? Hanja;   // top line (13px)
        public FormattedText? Roman;   // bottom line (11px) — always present, never hanja-only
        public FormattedText? Dates;   // (9px)
    }

    public LineageChartControl()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        // Double-click activates a node (L6 host mirrors it to the List tab).
        DoubleTapped += (_, e) =>
        {
            var pos = e.GetPosition(this);
            var hit = HitTest(pos.X, pos.Y);
            if (hit != null) NodeActivated?.Invoke(hit);
        };
    }

    /// <summary>Current zoom (read by an external zoom control / L6).</summary>
    public double Zoom => _zoom;

    /// <summary>Raised when the zoom changes via wheel or <see cref="SetZoom"/> (L6 syncs its slider).</summary>
    public event Action<double>? ZoomChanged;

    /// <summary>Raised on a node double-click (L6 opens it in the List tab).</summary>
    public event Action<LineageNode>? NodeActivated;

    /// <summary>Set the zoom about the viewport centre (the L6 zoom slider binds here).</summary>
    public void SetZoom(double zoom)
    {
        double z = Math.Clamp(zoom, ZOOM_MIN, ZOOM_MAX);
        if (Math.Abs(z - _zoom) < 1e-9) return;
        double cx = Bounds.Width / 2, cy = Bounds.Height / 2;
        _offsetX = cx - (cx - _offsetX) * (z / _zoom);
        _offsetY = cy - (cy - _offsetY) * (z / _zoom);
        _zoom = z;
        _needsFit = false;
        ZoomChanged?.Invoke(_zoom);
        InvalidateVisual();
    }

    /// <summary>Centre the view on a node at the current zoom (L6 "Go to" / list-sync).</summary>
    public void CenterOn(LineageNode node)
    {
        if (node == null) return;
        _needsFit = false;
        _offsetX = Bounds.Width / 2 - node.X * _zoom;
        _offsetY = Bounds.Height / 2 - node.Y * _zoom;
        InvalidateVisual();
    }

    public void SetViewModel(LineageChartViewModel vm)
    {
        _vm = vm;
        _builtVariant = null;      // force a cache rebuild on next render
        _needsFit = true;          // fit the whole chart once we know our size
        InvalidateVisual();
    }

    /// <summary>Re-fit the whole chart into view (the ⛶ action; L6 will bind a button to it).</summary>
    public void FitToView()
    {
        _needsFit = true;
        InvalidateVisual();
    }

    // ── render ──

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        var bounds = Bounds;
        if (_vm == null || bounds.Width <= 0 || bounds.Height <= 0) return;

        EnsureCaches();
        ctx.FillRectangle(_background, new Rect(bounds.Size));

        if (_needsFit) { FitAll(bounds); _needsFit = false; }

        UpdateFocus();
        _seals.Clear();

        // World-space pass (scoped so the legend below draws in screen space).
        using (ctx.PushTransform(
            Matrix.CreateScale(_zoom, _zoom) *
            Matrix.CreateTranslation(_offsetX, _offsetY)))
        {
            // visible world viewport (with a generous margin for node/edge extents)
            double m = LineageForestLayout.NODE_W;
            double vLeft = -_offsetX / _zoom - m;
            double vTop = -_offsetY / _zoom - m;
            double vRight = (bounds.Width - _offsetX) / _zoom + m;
            double vBottom = (bounds.Height - _offsetY) / _zoom + m;

            DrawEdges(ctx, vLeft, vTop, vRight, vBottom);
            DrawContested(ctx);                       // rival arcs + vermilion seals
            DrawNodes(ctx, vLeft, vTop, vRight, vBottom);
        }

        // Screen-space overlay: the legend (the chart's thesis).
        DrawLegend(ctx, bounds);
    }

    /// <summary>Recompute the lit set (focus + ancestor path + direct heirs) when
    /// the selection anchor changes. Everything outside it dims to <see cref="DIM"/>.</summary>
    private void UpdateFocus()
    {
        var sel = _vm!.SelectedNode;
        if (ReferenceEquals(sel, _relAnchor)) return;
        _relAnchor = sel;
        _relSet.Clear();
        _hasFocus = sel != null;
        if (sel == null) return;
        _relSet.Add(sel);
        var cur = sel.ParentEdge?.From;
        int guard = 0;
        while (cur != null && guard++ < 200) { _relSet.Add(cur); cur = cur.ParentEdge?.From; }
        foreach (var e in sel.ChildEdges) _relSet.Add(e.To);
    }

    /// <summary>Relevance (opacity multiplier) of a node under the current focus.</summary>
    private double Rel(LineageNode n) => !_hasFocus || _relSet.Contains(n) ? 1.0 : DIM;

    /// <summary>Relevance of an edge — lit if either endpoint is lit.</summary>
    private double RelEdge(LineageEdge e) => !_hasFocus ? 1.0 : Math.Max(Rel(e.From), Rel(e.To));

    private void DrawEdges(DrawingContext ctx, double vLeft, double vTop, double vRight, double vBottom)
    {
        var routes = _vm!.Routes;
        foreach (var e in _vm.Edges)
        {
            if (!routes.TryGetValue(e, out var route) || route.Points.Count < 2) continue;

            // bbox cull
            double minx = Math.Min(e.From.X, e.To.X) - LineageForestLayout.NODE_W;
            double maxx = Math.Max(e.From.X, e.To.X) + LineageForestLayout.NODE_W;
            double miny = e.From.Y - LineageForestLayout.NODE_W;
            double maxy = e.To.Y + LineageForestLayout.NODE_W;
            if (maxx < vLeft || minx > vRight || maxy < vTop || miny > vBottom) continue;

            double rel = RelEdge(e);
            DrawingContext.PushedState? dim = rel < 1 ? ctx.PushOpacity(rel) : (DrawingContext.PushedState?)null;
            try { DrawOneEdge(ctx, e, route.Points); }
            finally { dim?.Dispose(); }
        }
    }

    // One edge = attestation INK (channel 1) + transmission GEOMETRY (channel 2),
    // kept strictly independent: the ink is chosen only by StyleFor(attestation),
    // the marker only by MarkerFor(transmission); neither reads the other.
    private void DrawOneEdge(DrawingContext ctx, LineageEdge e, IReadOnlyList<LayoutPoint> pts)
    {
        // channel 1 — ink: the edge carries the STUDENT's attestation; fail-safe applies.
        var pen = _edgePens[Canon(e.Attestation)];
        // channel 2 — geometry: the transmission mode.
        var marker = MarkerFor(e.Transmission);

        if (marker == TransmissionMarker.Disputed)
        {
            // a fork that never resolves: twin parallel strands, no midpoint glyph.
            DrawStrand(ctx, pts, pen, -1.5);
            DrawStrand(ctx, pts, pen, 1.5);
            return;
        }

        DrawStrand(ctx, pts, pen, 0);

        if (_zoom >= MID_GLYPH_ZOOM && marker != TransmissionMarker.None)
            DrawMidGlyph(ctx, pts, marker, e.Attestation);
    }

    private static void DrawStrand(DrawingContext ctx, IReadOnlyList<LayoutPoint> pts, IPen pen, double dx)
    {
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(pts[0].X + dx, pts[0].Y), isFilled: false);
            for (int i = 1; i < pts.Count; i++)
                gc.LineTo(new Point(pts[i].X + dx, pts[i].Y));
            gc.EndFigure(false);
        }
        ctx.DrawGeometry(null, pen, geo);
    }

    // The 遙嗣 circle / 代囑 diamond / book 冊 sit ON the edge at its midpoint. Their
    // outline hue follows the edge's ink base (text vs muted for a faint D edge),
    // but they carry NO attestation meaning — they are pure transmission geometry.
    private void DrawMidGlyph(DrawingContext ctx, IReadOnlyList<LayoutPoint> pts, TransmissionMarker marker, string? att)
    {
        var mp = Midpoint(pts);
        var outline = StyleFor(att).Faint ? _glyphPenMuted : _glyphPenText;
        switch (marker)
        {
            case TransmissionMarker.RemoteCircle:
                ctx.DrawEllipse(_background, outline, new Point(mp.X, mp.Y), 4.5, 4.5);
                break;
            case TransmissionMarker.ProxyDiamond:
                var geo = new StreamGeometry();
                using (var gc = geo.Open())
                {
                    gc.BeginFigure(new Point(mp.X, mp.Y - 5), isFilled: true);
                    gc.LineTo(new Point(mp.X + 5, mp.Y));
                    gc.LineTo(new Point(mp.X, mp.Y + 5));
                    gc.LineTo(new Point(mp.X - 5, mp.Y));
                    gc.EndFigure(true);
                }
                ctx.DrawGeometry(_background, outline, geo);
                break;
            case TransmissionMarker.Book:
                if (_bookGlyph != null)
                    ctx.DrawText(_bookGlyph, new Point(mp.X - _bookGlyph.Width / 2, mp.Y - _bookGlyph.Height / 2));
                break;
        }
    }

    private static LayoutPoint Midpoint(IReadOnlyList<LayoutPoint> pts)
    {
        int i = (pts.Count - 1) / 2;
        var a = pts[i];
        var b = pts[Math.Min(i + 1, pts.Count - 1)];
        return new LayoutPoint((a.X + b.X) / 2, (a.Y + b.Y) / 2);
    }

    // ── channel 3 — CONTESTED: the received edge is drawn honestly above; here we
    //    add the rival hypothesis as a bowed accent arc (weighted by the rival's
    //    rung) and stamp the tilted vermilion seal over the kept edge's midpoint.
    //    Drawn at full opacity on every LOD (an annotation layer, never dimmed). ──
    private void DrawContested(DrawingContext ctx)
    {
        var routes = _vm!.Routes;
        var byId = _vm.Graph.ById;
        var byName = _vm.Graph.ByName;
        foreach (var e in _vm.Edges)
        {
            var cb = e.Contested;
            if (cb == null) continue;
            if (!routes.TryGetValue(e, out var route) || route.Points.Count < 2) continue;

            var mp = Midpoint(route.Points);

            // rival arc — bowed out of the lattice, in the accent hue.
            LineageNode? rival = null;
            if (!string.IsNullOrEmpty(cb.Rival) &&
                !byId.TryGetValue(cb.Rival!, out rival))
                byName.TryGetValue(cb.Rival!, out rival);
            if (rival != null)
            {
                double h = LineageForestLayout.NODE_H / 2;
                var from = new Point(rival.X, rival.Y + h);
                var to = new Point(e.To.X, e.To.Y - h);
                double bow = 60 * (from.X <= to.X ? -1 : 1);
                var control = new Point((from.X + to.X) / 2 + bow, (from.Y + to.Y) / 2);
                var arcPen = IsStrongRung(cb.RivalRung) ? _rivalPenStrong : _rivalPenWeak;
                var arc = new StreamGeometry();
                using (var gc = arc.Open())
                {
                    gc.BeginFigure(from, isFilled: false);
                    gc.QuadraticBezierTo(control, to);
                    gc.EndFigure(false);
                }
                ctx.DrawGeometry(null, arcPen, arc);
            }

            // the seal — a tilted cinnabar square over the kept edge midpoint.
            _seals.Add((mp.X, mp.Y, e));
            using (ctx.PushTransform(
                Matrix.CreateRotation(-6 * Math.PI / 180) *
                Matrix.CreateTranslation(mp.X, mp.Y)))
            {
                ctx.DrawRectangle(_sealFill, _sealStroke, new Rect(-6, -6, 12, 12), 1, 1);
            }
        }
    }

    private void DrawNodes(DrawingContext ctx, double vLeft, double vTop, double vRight, double vBottom)
    {
        double hNode = LineageForestLayout.NODE_H;
        var selected = _vm!.SelectedNode;
        var hits = _vm.SearchHitIds;

        foreach (var n in _vm.Nodes)
        {
            double hw = HalfW(n);
            if (n.X + hw < vLeft || n.X - hw > vRight ||
                n.Y + hNode < vTop || n.Y - hNode > vBottom) continue;

            if (!_nodeVisuals.TryGetValue(n, out var vis)) continue;

            // channel 4 stays inside the node visual (school hue). Selection /
            // search highlight is an ORTHOGONAL accent border, not an evidence channel.
            bool active = ReferenceEquals(n, selected) || (hits.Count > 0 && hits.Contains(n.Id));
            double rel = Rel(n);
            DrawingContext.PushedState? dim = rel < 1 ? ctx.PushOpacity(rel) : (DrawingContext.PushedState?)null;
            try
            {
                if (IsOffChartTeacher(n)) DrawStub(ctx, n);   // ⊣… teacher off-chart
                if (n.IsSource) DrawSource(ctx, n, vis, active);
                else DrawMaster(ctx, n, vis, hNode, active);
            }
            finally { dim?.Dispose(); }
        }
    }

    private void DrawMaster(DrawingContext ctx, LineageNode n, NodeVisual vis, double hNode, bool active)
    {
        double hw = HalfW(n);
        var rect = new Rect(n.X - hw, n.Y - hNode / 2, hw * 2, hNode);
        ctx.DrawRectangle(vis.Fill, active ? _selectionPen : vis.Stroke, rect, 5, 5);

        // ── bilingual label: the romanized/English line ALWAYS renders, so a node
        //    is never hanja-only. ──
        if (vis.Hanja != null && vis.Roman != null)
        {
            DrawCentered(ctx, vis.Hanja, n.X, n.Y - hNode / 2 + 2);
            DrawCentered(ctx, vis.Roman, n.X, n.Y + 1);
        }
        else if (vis.Roman != null)
        {
            DrawCentered(ctx, vis.Roman, n.X, n.Y - vis.Roman.Height / 2);
        }
        if (vis.Dates != null)
            DrawCentered(ctx, vis.Dates, n.X, n.Y + hNode / 2 - vis.Dates.Height - 1);
    }

    // A book source: the folded-sutra capsule (a vertical leaf with a spine line
    // and three text ribs), replacing L4's plain capsule. Clickable (its rect is
    // in the hit-test via SOURCE_W) and bilingual (English first, hanja below —
    // never hanja-only), so L6's panel can surface its book_transmissions.
    private void DrawSource(DrawingContext ctx, LineageNode n, NodeVisual vis, bool active)
    {
        const double wpx = 22, hpx = 28;
        double x = n.X - wpx / 2, y = n.Y - hpx / 2;
        ctx.DrawRectangle(_sourceFill, active ? _selectionPen : _sourceStroke, new Rect(x, y, wpx, hpx), 2, 2);
        ctx.DrawLine(_sourceStroke, new Point(x + 5, y), new Point(x + 5, y + hpx));   // spine
        for (int i = 1; i <= 3; i++)                                                    // three ribs
            ctx.DrawLine(_sourceLinePen, new Point(x + 9, y + i * 7), new Point(x + wpx - 3, y + i * 7));

        double ly = y + hpx + 3;
        if (vis.Roman != null) { DrawCentered(ctx, vis.Roman, n.X, ly); ly += vis.Roman.Height + 1; }
        if (vis.Hanja != null) DrawCentered(ctx, vis.Hanja, n.X, ly);
    }

    // The off-chart teacher: a dashed drop rising from the node's top edge, capped
    // with ⊣ (the teacher exists but is not in the corpus). NODE-level marker,
    // independent of the three edge/contested channels.
    private void DrawStub(DrawingContext ctx, LineageNode n)
    {
        double topY = n.Y - LineageForestLayout.NODE_H / 2;
        ctx.DrawLine(_stubPen, new Point(n.X, topY - 24), new Point(n.X, topY));
        if (_zoom >= STUB_LABEL_ZOOM && _stubGlyph != null)
            ctx.DrawText(_stubGlyph, new Point(n.X - _stubGlyph.Width / 2, topY - 26 - _stubGlyph.Height / 2));
    }

    private static void DrawCentered(DrawingContext ctx, FormattedText ft, double cx, double top)
        => ctx.DrawText(ft, new Point(cx - ft.Width / 2, top));

    // ── cache building (theme-scoped; NO per-frame allocation) ──

    private void EnsureCaches()
    {
        var variant = ActualThemeVariant;
        if (_builtVariant == variant && _nodeVisuals.Count > 0) return;
        _builtVariant = variant;
        BuildCaches(variant == ThemeVariant.Dark);
    }

    private void BuildCaches(bool dark)
    {
        // theme tokens (parity with lineage-graph.js readTokens fallbacks)
        Color text = dark ? Color.FromRgb(237, 227, 209) : Color.FromRgb(58, 51, 42);
        Color muted = dark ? Color.FromRgb(139, 123, 105) : Color.FromRgb(138, 125, 106);
        Color accent = Color.FromRgb(212, 171, 88);
        _background = new SolidColorBrush(dark ? Color.FromRgb(19, 16, 12) : Color.FromRgb(245, 239, 227));
        _selectionPen = new Pen(new SolidColorBrush(accent), 2);

        // edge pens: opacity baked into the colour alpha so Render never re-allocs.
        _edgePens.Clear();
        foreach (var st in new[] { StyleA, StyleB, StyleC, StyleD })
        {
            var baseColor = st.Faint ? muted : text;
            var color = Color.FromArgb((byte)Math.Round(st.Opacity * 255), baseColor.R, baseColor.G, baseColor.B);
            var pen = new Pen(new SolidColorBrush(color), st.Width)
            {
                LineCap = PenLineCap.Round,
                DashStyle = st.Dash.Count > 0 ? new DashStyle(new List<double>(st.Dash), 0) : null,
            };
            _edgePens[st.Grade] = pen;
        }

        // ── L5 evidence-grammar resources (each channel's own ink, cached once) ──
        var textBrush = new SolidColorBrush(text);
        var mutedBrush = new SolidColorBrush(muted);
        var accentBrush = new SolidColorBrush(accent);

        // channel 2 (transmission geometry): glyph outlines + the 冊 book mark.
        _glyphPenText = new Pen(textBrush, 1.2);
        _glyphPenMuted = new Pen(mutedBrush, 1.2);
        _bookGlyph = new FormattedText("冊", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Georgia, Songti SC, serif"), 10,
            new SolidColorBrush(Color.FromArgb(179, text.R, text.G, text.B)));   // ~0.7 alpha

        // off-chart-teacher stub (node-level): dashed drop + ⊣ cap.
        _stubPen = new Pen(new SolidColorBrush(Color.FromArgb(128, muted.R, muted.G, muted.B)), 1)
        { DashStyle = new DashStyle(new List<double> { 2, 3 }, 0) };
        _stubGlyph = new FormattedText("⊣", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI, system-ui, sans-serif"), 10, mutedBrush);

        // channel 3 (contested): vermilion seal + rival arcs (accent hue only).
        _sealFill = new SolidColorBrush(Color.FromArgb((byte)Math.Round(0.18 * 255), accent.R, accent.G, accent.B));
        _sealStroke = new Pen(accentBrush, 1.5);
        var arcAccent = new SolidColorBrush(Color.FromArgb((byte)Math.Round(0.55 * 255), accent.R, accent.G, accent.B));
        _rivalPenStrong = new Pen(arcAccent, 2.25) { LineCap = PenLineCap.Round };
        _rivalPenWeak = new Pen(arcAccent, 1.4) { LineCap = PenLineCap.Round };

        // folded-sutra capsule (book source node).
        _sourceFill = new SolidColorBrush(dark ? Hsl(40, .08, .30) : Hsl(40, .14, .86));
        _sourceStroke = new Pen(mutedBrush, 1);
        _sourceLinePen = new Pen(new SolidColorBrush(Color.FromArgb(153, muted.R, muted.G, muted.B)), 1); // ~0.6 alpha

        // node visuals: fill/stroke/label brushes + cached FormattedText per node.
        _nodeVisuals.Clear();
        if (_vm == null) { _legend = null; return; }
        foreach (var n in _vm.Nodes)
            _nodeVisuals[n] = BuildNodeVisual(n, dark, muted);

        _legend = BuildLegend(text, muted, accent);
        ComputeWorldBounds();
    }

    private static NodeVisual BuildNodeVisual(LineageNode n, bool dark, Color muted)
    {
        var (fill, stroke, label) = NodeColors(n, dark);
        var vis = new NodeVisual
        {
            Fill = new SolidColorBrush(fill),
            Stroke = new Pen(new SolidColorBrush(stroke), 1),
        };
        var labelBrush = new SolidColorBrush(label);
        // Source labels sit BELOW the folded-sutra capsule (not inside the narrow
        // 22px glyph), so they get the full node width to read a book title.
        double w = LineageForestLayout.NODE_W - 8;

        string hanja = n.Cjk ?? "";
        string roman = n.Primary ?? "";
        // "Never hanja-only": if the only text is the hanja, still surface it as the
        // roman line rather than dropping to a bare glyph.
        if (string.IsNullOrEmpty(roman)) { roman = hanja; hanja = ""; }
        if (!string.IsNullOrEmpty(hanja) && string.Equals(hanja, roman, StringComparison.Ordinal))
            hanja = ""; // identical → one line only

        if (!string.IsNullOrEmpty(hanja))
            vis.Hanja = MakeText(hanja, 13, labelBrush, w, serif: true);
        if (!string.IsNullOrEmpty(roman))
            vis.Roman = MakeText(roman, 11, labelBrush, w, serif: false);
        if (!n.IsSource && !string.IsNullOrEmpty(n.DatesText))
            vis.Dates = MakeText(n.DatesText, 9, new SolidColorBrush(Color.FromArgb(160, label.R, label.G, label.B)), w, serif: false);

        return vis;
    }

    private static FormattedText MakeText(string s, double size, IBrush brush, double maxWidth, bool serif)
    {
        var face = serif
            ? new Typeface("Georgia, Songti SC, serif")
            : new Typeface("Segoe UI, system-ui, sans-serif");
        var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, size, brush)
        {
            MaxTextWidth = Math.Max(8, maxWidth),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Left,
        };
        return ft;
    }

    // Port of lineage-graph.js nodeColors(): source palette, achromatic
    // (pre-chan/other/unknown hue), or the school HSL band.
    private static (Color fill, Color stroke, Color label) NodeColors(LineageNode n, bool dark)
    {
        if (n.IsSource)
            return dark
                ? (Hsl(40, .08, .30), Hsl(40, .10, .46), Hsl(40, .10, .84))
                : (Hsl(40, .14, .86), Hsl(40, .14, .52), Hsl(40, .18, .28));

        int? hue = null;
        if (n.SchoolKey != null && LineageGraphBuilder.SchoolHues.TryGetValue(n.SchoolKey, out var h))
            hue = h;

        if (hue == null) // pre-chan / other / unknown key: achromatic
            return dark
                ? (Hsl(40, .06, .24), Hsl(40, .08, .40), Hsl(40, .08, .80))
                : (Hsl(40, .08, .88), Hsl(40, .10, .54), Hsl(40, .12, .32));

        double s = n.SchoolKey == "early-chan" ? 0.6 : 1.0; // early-chan desaturated
        double hh = hue.Value;
        return dark
            ? (Hsl(hh, .30 * s, .26), Hsl(hh, .35 * s, .42), Hsl(hh, .22 * s, .84))
            : (Hsl(hh, .38 * s, .90), Hsl(hh, .30 * s, .52), Hsl(hh, .45 * s, .22));
    }

    /// <summary>HSL (h in [0,360), s/l in [0,1]) → opaque Color. Pure; deterministic.</summary>
    private static Color Hsl(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360;
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double mm = l - c / 2;
        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }
        return Color.FromRgb(
            (byte)Math.Round((r + mm) * 255),
            (byte)Math.Round((g + mm) * 255),
            (byte)Math.Round((b + mm) * 255));
    }

    // ── legend (the chart's thesis) — a screen-space panel, theme-aware, guarded
    //    against clipping. Ports the SPA legend content (buildLegend): the four
    //    attestation lines, the transmission glyphs, the contested seal, and the
    //    school hues — one row per evidence channel, never mixing them. ──
    private sealed class LegendCache
    {
        public FormattedText Title = null!;
        public FormattedText[] Att = null!;      // A/B/C/D lines
        public FormattedText[] Glyph = null!;    // ○ ◇ 冊 ⊣
        public FormattedText Seal = null!;
        public FormattedText[] School = null!;   // 12 school labels
        public IBrush[] SchoolSwatch = null!;    // 12 hue brushes (cached, no per-frame alloc)
        public IBrush PanelFill = null!;
        public IPen PanelBorder = null!;
    }

    private LegendCache BuildLegend(Color text, Color muted, Color accent)
    {
        var face = new Typeface("Segoe UI, system-ui, sans-serif");
        var textBrush = new SolidColorBrush(text);
        FormattedText L(string s) => new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, 10, textBrush)
        { MaxTextWidth = 206, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };

        var order = new (string Key, string Label)[]
        {
            ("linji", "Linji"), ("caodong", "Caodong"), ("yunmen", "Yunmen"), ("fayan", "Fayan"),
            ("guiyang", "Guiyang"), ("hongzhou", "Hongzhou"), ("shitou", "Shitou"), ("niutou", "Niutou"),
            ("heze", "Heze"), ("korean-seon", "Korean Seon"), ("early-chan", "Early Chan"), ("pre-chan", "Pre-Chan"),
        };
        var school = new FormattedText[order.Length];
        var swatch = new IBrush[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            school[i] = L(order[i].Label);
            int? hue = LineageGraphBuilder.SchoolHues.TryGetValue(order[i].Key, out var hv) ? hv : null;
            swatch[i] = new SolidColorBrush(hue == null ? muted : Hsl(hue.Value, 0.40, 0.55));
        }

        bool dark = _builtVariant == ThemeVariant.Dark;
        var bg = dark ? Color.FromRgb(19, 16, 12) : Color.FromRgb(245, 239, 227);
        return new LegendCache
        {
            Title = new FormattedText("Key", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, 11, textBrush),
            Att = new[]
            {
                L("A  his own words, or his stone"),
                L("B  a living witness"),
                L("C  a lineage index"),
                L("D  a lamp record only"),
            },
            Glyph = new[]
            {
                L("posthumous succession (遙嗣)"),
                L("by proxy (代囑)"),
                L("from a book (冊)"),
                L("teacher off-chart"),
            },
            Seal = L("contested — an earlier source disagrees. Click it."),
            School = school,
            SchoolSwatch = swatch,
            PanelFill = new SolidColorBrush(Color.FromArgb(232, bg.R, bg.G, bg.B)),
            PanelBorder = new Pen(new SolidColorBrush(Color.FromArgb(64, accent.R, accent.G, accent.B)), 1),
        };
    }

    private void DrawLegend(DrawingContext ctx, Rect bounds)
    {
        var lc = _legend;
        if (lc == null) return;

        const double pad = 10, row = 15, gap = 6, sw = 22, panelW = 256;
        double schoolRows = Math.Ceiling(lc.School.Length / 2.0);
        double panelH = pad * 2 + row + gap + 4 * row + gap + 4 * row + gap + row + gap + schoolRows * row;

        // non-clipping guard: if the panel won't fit, don't draw it.
        if (bounds.Width < panelW + 24 || bounds.Height < panelH + 24) return;

        double x0 = 12, y0 = bounds.Height - panelH - 12;
        ctx.DrawRectangle(lc.PanelFill, lc.PanelBorder, new Rect(x0, y0, panelW, panelH), 8, 8);

        double x = x0 + pad, y = y0 + pad;
        ctx.DrawText(lc.Title, new Point(x, y));
        y += row + gap;

        // attestation ink samples (channel 1) — the actual cached edge pens.
        var grades = new[] { "A", "B", "C", "D" };
        for (int i = 0; i < 4; i++)
        {
            double ly = y + lc.Att[i].Height / 2;
            ctx.DrawLine(_edgePens[grades[i]], new Point(x, ly), new Point(x + sw, ly));
            ctx.DrawText(lc.Att[i], new Point(x + sw + 8, y));
            y += row;
        }
        y += gap;

        // transmission glyphs (channel 2).
        for (int i = 0; i < 4; i++)
        {
            double cy = y + lc.Glyph[i].Height / 2, cx = x + sw / 2;
            switch (i)
            {
                case 0:
                    ctx.DrawEllipse(_background, _glyphPenText, new Point(cx, cy), 4.5, 4.5);
                    break;
                case 1:
                    var d = new StreamGeometry();
                    using (var gc = d.Open())
                    {
                        gc.BeginFigure(new Point(cx, cy - 5), true);
                        gc.LineTo(new Point(cx + 5, cy));
                        gc.LineTo(new Point(cx, cy + 5));
                        gc.LineTo(new Point(cx - 5, cy));
                        gc.EndFigure(true);
                    }
                    ctx.DrawGeometry(_background, _glyphPenText, d);
                    break;
                case 2:
                    if (_bookGlyph != null) ctx.DrawText(_bookGlyph, new Point(cx - _bookGlyph.Width / 2, cy - _bookGlyph.Height / 2));
                    break;
                case 3:
                    if (_stubGlyph != null) ctx.DrawText(_stubGlyph, new Point(cx - _stubGlyph.Width / 2, cy - _stubGlyph.Height / 2));
                    break;
            }
            ctx.DrawText(lc.Glyph[i], new Point(x + sw + 8, y));
            y += row;
        }
        y += gap;

        // contested seal (channel 3).
        {
            double cy = y + lc.Seal.Height / 2, cx = x + sw / 2;
            using (ctx.PushTransform(Matrix.CreateRotation(-6 * Math.PI / 180) * Matrix.CreateTranslation(cx, cy)))
                ctx.DrawRectangle(_sealFill, _sealStroke, new Rect(-6, -6, 12, 12), 1, 1);
            ctx.DrawText(lc.Seal, new Point(x + sw + 8, y));
            y += row + gap;
        }

        // school hues (channel 4) — two columns.
        double colW = (panelW - pad * 2) / 2;
        for (int i = 0; i < lc.School.Length; i++)
        {
            int col = i % 2, r = i / 2;
            double sx = x + col * colW, sy = y + r * row;
            ctx.DrawRectangle(lc.SchoolSwatch[i], null, new Rect(sx, sy + 2, 10, 10), 2, 2);
            ctx.DrawText(lc.School[i], new Point(sx + 14, sy));
        }
    }

    private void ComputeWorldBounds()
    {
        _minX = double.PositiveInfinity; _maxX = double.NegativeInfinity;
        _minY = double.PositiveInfinity; _maxY = double.NegativeInfinity;
        if (_vm == null) return;
        double hh = LineageForestLayout.NODE_H / 2;
        foreach (var n in _vm.Nodes)
        {
            double hw = HalfW(n);
            _minX = Math.Min(_minX, n.X - hw); _maxX = Math.Max(_maxX, n.X + hw);
            _minY = Math.Min(_minY, n.Y - hh); _maxY = Math.Max(_maxY, n.Y + hh);
        }
        if (double.IsInfinity(_minX)) { _minX = _maxX = _minY = _maxY = 0; }
    }

    private void FitAll(Rect bounds)
    {
        if (_vm == null || _vm.Nodes.Count == 0) return;
        double gw = (_maxX - _minX) + 80;
        double gh = (_maxY - _minY) + 80;
        double z = Math.Min(Math.Min(bounds.Width / gw, bounds.Height / gh), 1.0);
        _zoom = Math.Clamp(z, ZOOM_MIN, ZOOM_MAX);
        _offsetX = bounds.Width / 2 - (_minX + _maxX) / 2 * _zoom;
        _offsetY = 40 - _minY * _zoom;
    }

    // ── helpers ──

    private static double HalfW(LineageNode n)
        => (n.IsSource ? LineageForestLayout.SOURCE_W : LineageForestLayout.NODE_W) / 2;

    /// <summary>Canonical grade for pen lookup — the fail-safe collapses everything unknown to "D".</summary>
    private static string Canon(string? att) => StyleFor(att).Grade;

    private LineageNode? HitTest(double sx, double sy)
    {
        if (_vm == null) return null;
        double wx = (sx - _offsetX) / _zoom;
        double wy = (sy - _offsetY) / _zoom;
        double pad = 6 / _zoom;
        double hh = LineageForestLayout.NODE_H / 2 + pad;
        var nodes = _vm.Nodes;
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            var n = nodes[i];
            double hw = HalfW(n) + pad;
            if (wx >= n.X - hw && wx <= n.X + hw && wy >= n.Y - hh && wy <= n.Y + hh)
                return n;
        }
        return null;
    }

    // ── interaction ──

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_vm == null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var pos = e.GetPosition(this);

        // A contested seal is its own hit target (checked FIRST, a 14px screen
        // radius like the SPA). Clicking it selects the contested edge and its
        // student, surfacing the dispute state the VM exposes for L6's panel.
        var seal = SealHit(pos.X, pos.Y);
        if (seal != null)
        {
            _vm.SelectedEdge = seal;
            _vm.SelectedNode = seal.To;   // resolves SelectedMaster -> its ContestedBy
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        // A node click selects it (book-source nodes included — they are first-class
        // hit targets via SOURCE_W, exposing their book metadata for L6's panel).
        var hit = HitTest(pos.X, pos.Y);
        if (hit != null)
        {
            _vm.SelectedEdge = null;
            _vm.SelectedNode = hit;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        // empty space: clear selection, then start panning
        if (_vm.SelectedNode != null || _vm.SelectedEdge != null)
        {
            _vm.SelectedNode = null;
            _vm.SelectedEdge = null;
            InvalidateVisual();
        }
        _isPanning = true;
        _lastPan = pos;
        e.Handled = true;
    }

    /// <summary>Hit-test the contested seals at a fixed 14px screen radius (zoom-
    /// independent, parity with the SPA sealHit). Seal centres are stored in world
    /// space each draw and mapped to screen here.</summary>
    private LineageEdge? SealHit(double sx, double sy)
    {
        const double r2 = 14 * 14;
        foreach (var (wx, wy, edge) in _seals)
        {
            double screenX = wx * _zoom + _offsetX;
            double screenY = wy * _zoom + _offsetY;
            double dx = sx - screenX, dy = sy - screenY;
            if (dx * dx + dy * dy <= r2) return edge;
        }
        return null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isPanning) return;
        var pos = e.GetPosition(this);
        _offsetX += pos.X - _lastPan.X;   // pan is screen-space (zoom-independent)
        _offsetY += pos.Y - _lastPan.Y;
        _lastPan = pos;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        double factor = e.Delta.Y > 0 ? 1.12 : 0.893;   // parity with the SPA wheel step
        double oldZoom = _zoom;
        _zoom = Math.Clamp(_zoom * factor, ZOOM_MIN, ZOOM_MAX);
        // keep the point under the cursor fixed
        _offsetX = pos.X - (pos.X - _offsetX) * (_zoom / oldZoom);
        _offsetY = pos.Y - (pos.Y - _offsetY) * (_zoom / oldZoom);
        if (Math.Abs(_zoom - oldZoom) > 1e-9) ZoomChanged?.Invoke(_zoom);
        InvalidateVisual();
        e.Handled = true;
    }
}
