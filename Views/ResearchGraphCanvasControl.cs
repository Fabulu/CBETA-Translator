using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReadZen.App.Views;

/// <summary>
/// Custom canvas control that renders a force-directed research graph with 5 node shapes,
/// edge arrows, pan/zoom, node dragging, and edge creation handles.
/// </summary>
public class ResearchGraphCanvasControl : Control
{
    private ResearchGraphViewModel? _vm;
    private double _zoom = 1.0;
    private double _offsetX, _offsetY;
    private bool _isPanning;
    private Point _panStart;
    private ResearchGraphNode? _hoverNode;
    private ResearchGraphNode? _dragNode;
    private bool _isCreatingEdge;
    private ResearchGraphNode? _edgeSource;
    private Point _edgePreviewEnd;
    private ResearchGraphEdgeVm? _hoverEdge;

    // Physics simulation
    private DispatcherTimer? _physicsTimer;
    private bool _physicsEnabled = false;

    // Entry animation
    private double _entryProgress = 0;
    private DateTime _entryStart;
    private DispatcherTimer? _entryTimer;
    private readonly System.Diagnostics.Stopwatch _rippleStopwatch = System.Diagnostics.Stopwatch.StartNew();
    private DispatcherTimer? _rippleTimer;

    // Node type colors (lazy-initialized to avoid TypeInitializationException in tests)
    private static readonly Lazy<Dictionary<ScholarNodeType, IBrush>> _nodeBrushesLazy = new(() => new Dictionary<ScholarNodeType, IBrush>
    {
        [ScholarNodeType.Passage] = new SolidColorBrush(Color.Parse("#6EAFF8")),
        [ScholarNodeType.Concept] = new SolidColorBrush(Color.Parse("#FF8A65")),
        [ScholarNodeType.ZenMaster] = new SolidColorBrush(Color.Parse("#FFB74D")),
        [ScholarNodeType.TermbaseEntry] = new SolidColorBrush(Color.Parse("#81C784")),
        [ScholarNodeType.Collection] = new SolidColorBrush(Color.Parse("#AB47BC")),
        [ScholarNodeType.Book] = new SolidColorBrush(Color.Parse("#D4A574")),
        [ScholarNodeType.Link] = new SolidColorBrush(Color.Parse("#78909C")),
    });
    private static Dictionary<ScholarNodeType, IBrush> NodeBrushes => _nodeBrushesLazy.Value;

    private static readonly Lazy<IBrush> _selectedBrushLazy = new(() => new SolidColorBrush(Color.Parse("#FFD700")));
    private static IBrush SelectedBrush => _selectedBrushLazy.Value;

    private static readonly Lazy<IBrush> _dimmedBrushLazy = new(() => new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)));
    private static IBrush DimmedBrush => _dimmedBrushLazy.Value;

    private static readonly Lazy<IPen> _edgePenLazy = new(() => new Pen(new SolidColorBrush(Color.FromArgb(150, 150, 150, 150)), 1.5));
    private static IPen EdgePen => _edgePenLazy.Value;

    private static readonly Lazy<IPen> _handlePenLazy = new(() => new Pen(new SolidColorBrush(Color.Parse("#51D996")), 2));
    private static IPen HandlePen => _handlePenLazy.Value;

    private static readonly Lazy<IPen> _previewPenLazy = new(() => new Pen(new SolidColorBrush(Color.Parse("#51D996")), 2) { DashStyle = DashStyle.Dash });
    private static IPen PreviewPen => _previewPenLazy.Value;

    private static readonly Lazy<IBrush> _handleFillBrushLazy = new(() => new SolidColorBrush(Color.FromArgb(180, 81, 217, 150)));
    private static IBrush _handleFillBrush => _handleFillBrushLazy.Value;

    // --- Cached brushes and pens (lazy-initialized to avoid per-frame allocation) ---
    private static readonly Lazy<IBrush> _bgBrushLazy = new(() => new SolidColorBrush(Color.Parse("#1E1E23")));
    private static IBrush _bgBrush => _bgBrushLazy.Value;

    private static readonly Lazy<IBrush> _labelShadowBrushLazy = new(() => new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)));
    private static IBrush _labelShadowBrush => _labelShadowBrushLazy.Value;

    private static readonly Lazy<IBrush> _shadowBrushOuterLazy = new(() => new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)));
    private static IBrush _shadowBrushOuter => _shadowBrushOuterLazy.Value;

    private static readonly Lazy<IBrush> _shadowBrushInnerLazy = new(() => new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)));
    private static IBrush _shadowBrushInner => _shadowBrushInnerLazy.Value;

    private static readonly Lazy<IPen> _selectedPenLazy = new(() => new Pen(SelectedBrush, 3));
    private static IPen _selectedPen => _selectedPenLazy.Value;

    private static readonly Lazy<IPen> _hoverPenLazy = new(() => new Pen(new SolidColorBrush(Color.Parse("#FFD700")), 2.5));
    private static IPen _hoverPen => _hoverPenLazy.Value;

    private static readonly Lazy<IPen> _defaultNodePenLazy = new(() => new Pen(new SolidColorBrush(Color.FromArgb(153, 255, 255, 255)), 1.2));
    private static IPen _defaultNodePen => _defaultNodePenLazy.Value;

    private static readonly Lazy<IPen> _searchHighlightPenLazy = new(() => new Pen(new SolidColorBrush(Color.Parse("#00E5FF")), 3));
    private static IPen _searchHighlightPen => _searchHighlightPenLazy.Value;

    private static readonly Lazy<IBrush> _startingGlowOuterLazy = new(() =>
        new SolidColorBrush(Color.FromArgb(30, 255, 200, 50)));
    private static IBrush StartingGlowOuter => _startingGlowOuterLazy.Value;

    private static readonly Lazy<IBrush> _startingGlowInnerLazy = new(() =>
        new SolidColorBrush(Color.FromArgb(50, 255, 200, 50)));
    private static IBrush StartingGlowInner => _startingGlowInnerLazy.Value;

    private static readonly Lazy<IPen> _startingGlowPenLazy = new(() =>
        new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 180, 0)), 4));
    private static IPen StartingGlowPen => _startingGlowPenLazy.Value;

    /// <summary>Whether node labels are drawn. Toggled via toolbar.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Whether the minimap overlay is drawn. Toggled via toolbar.</summary>
    public bool ShowMinimap { get; set; } = true;

    /// <summary>Whether type-based cluster backgrounds are drawn. Off by default.</summary>
    public bool ShowClusters { get; set; } = false;

    // Minimap cached resources
    private static readonly Lazy<IBrush> _minimapBgLazy = new(() =>
        new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)));
    private static IBrush _minimapBg => _minimapBgLazy.Value;

    private static readonly Lazy<IPen> _minimapBorderPenLazy = new(() =>
        new Pen(new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)), 1));
    private static IPen _minimapBorderPen => _minimapBorderPenLazy.Value;

    private static readonly Lazy<IPen> _minimapViewportPenLazy = new(() =>
        new Pen(new SolidColorBrush(Color.Parse("#00E5FF")), 1));
    private static IPen _minimapViewportPen => _minimapViewportPenLazy.Value;

    private static readonly Lazy<IPen> _minimapEdgePenLazy = new(() =>
        new Pen(new SolidColorBrush(Color.FromArgb(80, 150, 150, 150)), 0.5));
    private static IPen _minimapEdgePen => _minimapEdgePenLazy.Value;

    // Cached cluster brushes to avoid per-frame allocations in DrawClusters
    private readonly Dictionary<Color, SolidColorBrush> _clusterBrushCache = new();

    public event EventHandler<(ResearchGraphNode Node, bool IsCtrlHeld)>? NodeClicked;
    public event EventHandler<ResearchGraphNode>? NodeDoubleClicked;
    public event EventHandler<(ResearchGraphNode From, ResearchGraphNode To)>? EdgeDropped;
    public event EventHandler<ResearchGraphEdgeVm>? EdgeClicked;

    /// <summary>Returns all nodes that currently have IsSelected = true.</summary>
    public IReadOnlyList<ResearchGraphNode> GetSelectedNodes()
    {
        if (_vm == null) return Array.Empty<ResearchGraphNode>();
        return _vm.Nodes.Where(n => n.IsSelected).ToList();
    }

    public void SetViewModel(ResearchGraphViewModel vm)
    {
        _vm = vm;
        // Don't start animation immediately — the window may not be visible yet.
        // Defer to AttachedToVisualTree so the user actually sees the scale-in.
        _entryProgress = 0;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_vm != null && _entryProgress < 1.0)
        {
            // Small delay so the window has time to paint its first frame
            Avalonia.Threading.Dispatcher.UIThread.Post(() => StartEntryAnimation(),
                Avalonia.Threading.DispatcherPriority.Loaded);
        }
        if (_physicsEnabled) StartPhysics();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.DrawRectangle(_bgBrush, null, new Rect(bounds.Size));

        if (_vm == null) return;

        // Start/stop ripple animation timer for starting node
        if (!string.IsNullOrEmpty(_vm.StartingNodeId) && _rippleTimer == null)
        {
            _rippleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30fps
            _rippleTimer.Tick += (_, _) => InvalidateVisual();
            _rippleTimer.Start();
        }
        else if (string.IsNullOrEmpty(_vm.StartingNodeId) && _rippleTimer != null)
        {
            _rippleTimer.Stop();
            _rippleTimer = null;
        }

        // Apply zoom + pan
        using (context.PushTransform(
            Matrix.CreateScale(_zoom, _zoom) *
            Matrix.CreateTranslation(_offsetX, _offsetY)))
        {
            var visibleEdges = _vm.GetVisibleEdges();
            var visibleNodes = _vm.GetVisibleNodes();

            // Draw cluster backgrounds (behind everything)
            DrawClusters(context);

            // Draw edges
            foreach (var edge in visibleEdges)
            {
                DrawEdge(context, edge);
            }

            // Draw edge creation preview
            if (_isCreatingEdge && _edgeSource != null)
            {
                context.DrawLine(PreviewPen, new Point(_edgeSource.X, _edgeSource.Y), _edgePreviewEnd);
            }

            // Draw nodes
            foreach (var node in visibleNodes)
            {
                DrawNode(context, node);
            }

            // Draw handles on hover node
            if (_hoverNode != null && !_isCreatingEdge)
            {
                DrawHandles(context, _hoverNode);
            }
        }

        // Minimap overlay (screen space, after transform pop)
        DrawMinimap(context);
    }

    private void DrawNode(DrawingContext ctx, ResearchGraphNode node)
    {
        double r = GetNodeRadius(node);
        double entryScale = EaseOutCubic(_entryProgress);
        r *= entryScale;
        if (r < 0.5) return; // skip tiny nodes during animation start
        var center = new Point(node.X, node.Y);
        var brush = node.IsDimmed ? DimmedBrush : (NodeBrushes.GetValueOrDefault(node.NodeType) ?? NodeBrushes[ScholarNodeType.Passage]);

        // Starting node: 1.3x size + ripple pulse + outer ring
        bool isStartingNode = node.NodeId == _vm?.StartingNodeId;
        if (isStartingNode && !node.IsDimmed)
        {
            r *= 1.3; // size boost

            // Ripple pulse: two expanding rings that fade out
            double time = _rippleStopwatch.Elapsed.TotalSeconds;
            for (int i = 0; i < 2; i++)
            {
                double t = ((time + i * 0.75) % 1.5) / 1.5; // 0→1 over 1.5s, staggered
                double rippleR = r + (r * 2.0 * t);
                double rippleOpacity = 0.5 * (1.0 - t);
                var ripplePen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(rippleOpacity * 255), 255, 200, 50)), 2.5 * (1.0 - t) + 0.5);
                ctx.DrawEllipse(null, ripplePen, center, rippleR, rippleR);
            }

            // Thin outer ring with gap (halo)
            var haloPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 255, 200, 50)), 1.5);
            ctx.DrawEllipse(null, haloPen, center, r + 6, r + 6);

            // Soft glow behind node
            ctx.DrawEllipse(StartingGlowOuter, null, center, r + 10, r + 10);
            ctx.DrawEllipse(StartingGlowInner, null, center, r + 5, r + 5);
        }

        IPen pen;
        if (node.IsSelected)
            pen = _selectedPen;
        else if (node == _hoverNode)
            pen = _hoverPen;
        else if (_vm!.HighlightedNodeIds.Contains(node.NodeId))
            pen = _searchHighlightPen;
        else
            pen = _defaultNodePen;

        if (isStartingNode && pen == _defaultNodePen)
            pen = StartingGlowPen;

        // Drop shadow (skip for dimmed nodes; limit to selected/hovered on large graphs)
        bool drawShadow = !node.IsDimmed &&
            (_vm!.Nodes.Count < 500 || node.IsSelected || node == _hoverNode);
        if (drawShadow)
        {
            var shadowCenter = new Point(node.X + 2.5, node.Y + 2.5);
            switch (node.NodeType)
            {
                case ScholarNodeType.Passage:
                    ctx.DrawEllipse(_shadowBrushOuter, null, shadowCenter, r + 2, r + 2);
                    ctx.DrawEllipse(_shadowBrushInner, null, shadowCenter, r + 1, r + 1);
                    break;
                case ScholarNodeType.Concept:
                    DrawDiamond(ctx, _shadowBrushOuter, null, shadowCenter, r + 1);
                    break;
                case ScholarNodeType.ZenMaster:
                    DrawHexagon(ctx, _shadowBrushOuter, null, shadowCenter, r + 1);
                    break;
                case ScholarNodeType.TermbaseEntry:
                case ScholarNodeType.Collection:
                    ctx.DrawRectangle(_shadowBrushOuter, null,
                        new Rect(node.X - r + 2.5, node.Y - r * 0.7 + 2.5, r * 2, r * 1.4),
                        r * 0.2, r * 0.2);
                    break;
                case ScholarNodeType.Book:
                    ctx.DrawRectangle(_shadowBrushOuter, null,
                        new Rect(node.X - r * 0.6 + 2.5, node.Y - r * 0.9 + 2.5, r * 1.2, r * 1.8),
                        r * 0.15, r * 0.15);
                    break;
                case ScholarNodeType.Link:
                    ctx.DrawEllipse(_shadowBrushOuter, null, shadowCenter, r + 1, r * 0.7 + 1);
                    break;
            }
        }

        switch (node.NodeType)
        {
            case ScholarNodeType.Passage:
                ctx.DrawEllipse(brush, pen, center, r, r);
                break;
            case ScholarNodeType.Concept:
                DrawDiamond(ctx, brush, pen, center, r);
                break;
            case ScholarNodeType.ZenMaster:
                DrawHexagon(ctx, brush, pen, center, r);
                break;
            case ScholarNodeType.TermbaseEntry:
                // Wide rounded pill shape (distinct from Collection's square)
                ctx.DrawRectangle(brush, pen, new Rect(node.X - r * 1.2, node.Y - r * 0.5, r * 2.4, r * 1.0), r * 0.4, r * 0.4);
                break;
            case ScholarNodeType.Collection:
                // Square with minimal rounding
                ctx.DrawRectangle(brush, pen, new Rect(node.X - r * 0.8, node.Y - r * 0.8, r * 1.6, r * 1.6), r * 0.1, r * 0.1);
                break;
            case ScholarNodeType.Book:
                // Tall rectangle (book shape)
                ctx.DrawRectangle(brush, pen, new Rect(node.X - r * 0.6, node.Y - r * 0.9, r * 1.2, r * 1.8), r * 0.15, r * 0.15);
                break;
            case ScholarNodeType.Link:
                ctx.DrawEllipse(brush, pen, center, r, r * 0.7);
                break;
        }

        // Pin indicator: small dot at top-right corner of pinned nodes
        if (node.IsPinned)
        {
            double pinR = 3.5;
            var pinCenter = new Point(node.X + r * 0.7, node.Y - r * 0.7);
            ctx.DrawEllipse(Brushes.White, null, pinCenter, pinR, pinR);
            ctx.DrawEllipse(new SolidColorBrush(Color.Parse("#FF5252")), null, pinCenter, pinR - 0.8, pinR - 0.8);
        }

        // Label with shadow outline (only at sufficient zoom and after entry animation starts)
        if (ShowLabels && _zoom >= 0.5 && _entryProgress > 0.3)
        {
            var labelSize = Math.Max(9, 11 * Math.Min(_zoom, 1.5));
            if (isStartingNode) labelSize += 1; // slightly larger label for starting node
            var labelText = node.Label.Length > 25 ? node.Label[..24] + "\u2026" : node.Label;
            var labelY = node.Y + r + 3;
            var typeface = isStartingNode
                ? new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold)
                : new Typeface("Segoe UI");

            // Shadow pass: draw black text at 4 offsets (up/down/left/right) for outline effect
            var shadowFt = new FormattedText(
                labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, labelSize, _labelShadowBrush);
            var baseX = node.X - shadowFt.Width / 2;
            double so = 1.2; // shadow offset
            ctx.DrawText(shadowFt, new Point(baseX - so, labelY));
            ctx.DrawText(shadowFt, new Point(baseX + so, labelY));
            ctx.DrawText(shadowFt, new Point(baseX, labelY - so));
            ctx.DrawText(shadowFt, new Point(baseX, labelY + so));

            // Main pass: white text on top
            var mainFt = new FormattedText(
                labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, labelSize, Brushes.White);
            ctx.DrawText(mainFt, new Point(baseX, labelY));
        }
    }

    private void DrawDiamond(DrawingContext ctx, IBrush fill, IPen? pen, Point center, double size)
    {
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(new Point(center.X, center.Y - size), true);
            gc.LineTo(new Point(center.X + size, center.Y));
            gc.LineTo(new Point(center.X, center.Y + size));
            gc.LineTo(new Point(center.X - size, center.Y));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(fill, pen, geo);
    }

    private void DrawHexagon(DrawingContext ctx, IBrush fill, IPen? pen, Point center, double size)
    {
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 3 * i - Math.PI / 2;
                var pt = new Point(center.X + size * Math.Cos(angle), center.Y + size * Math.Sin(angle));
                if (i == 0) gc.BeginFigure(pt, true);
                else gc.LineTo(pt);
            }
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(fill, pen, geo);
    }

    private void DrawEdge(DrawingContext ctx, ResearchGraphEdgeVm edge)
    {
        var from = new Point(edge.From.X, edge.From.Y);
        var to = new Point(edge.To.X, edge.To.Y);
        Color edgeColor;
        try { edgeColor = Color.Parse(edge.ColorHex ?? "#9E9E9E"); }
        catch { edgeColor = Color.Parse("#9E9E9E"); }

        bool isHovered = edge == _hoverEdge;
        double thickness = (isHovered ? 3.0 : 1.5) * Math.Clamp(edge.Weight, 0.5, 4.0);

        // Ego-aware alpha: 0.6 default, 0.8 ego-relevant, 0.35 non-relevant
        byte alpha;
        bool hasEgo = _vm!.SelectedNode != null;
        if (isHovered)
            alpha = 230;
        else if (!hasEgo)
            alpha = 153; // 0.6 * 255
        else
        {
            string egoId = _vm.SelectedNode!.NodeId;
            bool relevant = edge.From.NodeId == egoId || edge.To.NodeId == egoId;
            alpha = relevant ? (byte)204 : (byte)89; // 0.8 vs 0.35
        }
        var alphaColor = Color.FromArgb(alpha, edgeColor.R, edgeColor.G, edgeColor.B);

        // Entry animation fade
        double entryScale = EaseOutCubic(_entryProgress);
        var finalColor = Color.FromArgb((byte)(alphaColor.A * entryScale), alphaColor.R, alphaColor.G, alphaColor.B);

        // Calculate edge length to decide straight vs curved
        double edgeDx = to.X - from.X, edgeDy = to.Y - from.Y;
        double edgeLen = Math.Sqrt(edgeDx * edgeDx + edgeDy * edgeDy);
        bool useCurve = edgeLen >= 50;

        // Calculate Bezier control point (perpendicular offset)
        Point controlPt = default;
        if (useCurve)
        {
            double perpX = -edgeDy / edgeLen;
            double perpY = edgeDx / edgeLen;
            double curveOffset = Math.Min(20, edgeLen * 0.12);
            double midX2 = (from.X + to.X) / 2;
            double midY2 = (from.Y + to.Y) / 2;
            controlPt = new Point(midX2 + perpX * curveOffset, midY2 + perpY * curveOffset);
        }

        // Glow layer for hovered edge (wide, semi-transparent)
        if (isHovered)
        {
            var glowBrush = new SolidColorBrush(Color.FromArgb(60, edgeColor.R, edgeColor.G, edgeColor.B));
            var glowPen = new Pen(glowBrush, 8);
            if (useCurve)
            {
                var glowGeo = new StreamGeometry();
                using (var gc = glowGeo.Open())
                {
                    gc.BeginFigure(from, false);
                    gc.QuadraticBezierTo(controlPt, to);
                    gc.EndFigure(false);
                }
                ctx.DrawGeometry(null, glowPen, glowGeo);
            }
            else
            {
                ctx.DrawLine(glowPen, from, to);
            }
        }

        var brush = new SolidColorBrush(finalColor);
        var pen = new Pen(brush, thickness);

        // Non-directional edges: dashed
        if (!edge.IsDirectional)
        {
            pen = new Pen(brush, thickness) { DashStyle = DashStyle.Dash };
        }

        if (useCurve)
        {
            var edgeGeo = new StreamGeometry();
            using (var gc = edgeGeo.Open())
            {
                gc.BeginFigure(from, false);
                gc.QuadraticBezierTo(controlPt, to);
                gc.EndFigure(false);
            }
            ctx.DrawGeometry(null, pen, edgeGeo);
        }
        else
        {
            ctx.DrawLine(pen, from, to);
        }

        // Edge type label: show on hover OR on ego-relevant edges at sufficient zoom
        // Respect ShowLabels toggle — when labels are hidden, suppress edge labels too
        bool showLabel = false;
        if (ShowLabels && isHovered && !string.IsNullOrEmpty(edge.RelationType))
            showLabel = true;
        else if (ShowLabels && hasEgo && _zoom >= 0.7 && !string.IsNullOrEmpty(edge.RelationType))
        {
            string egoId = _vm.SelectedNode!.NodeId;
            if (edge.From.NodeId == egoId || edge.To.NodeId == egoId)
                showLabel = true;
        }

        if (showLabel)
        {
            // For curved edges, use the midpoint on the curve (t=0.5 of quadratic Bezier)
            double midX, midY;
            if (useCurve)
            {
                // B(0.5) = 0.25*from + 0.5*control + 0.25*to
                midX = 0.25 * from.X + 0.5 * controlPt.X + 0.25 * to.X;
                midY = 0.25 * from.Y + 0.5 * controlPt.Y + 0.25 * to.Y;
            }
            else
            {
                midX = (from.X + to.X) / 2;
                midY = (from.Y + to.Y) / 2;
            }
            var edgeLabelText = edge.Label ?? edge.RelationType;
            var labelColor = new SolidColorBrush(Color.FromArgb((byte)(isHovered ? 255 : 200), edgeColor.R, edgeColor.G, edgeColor.B));
            var labelFt = new FormattedText(
                edgeLabelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 9, labelColor);
            // Shadow behind label for readability
            var bgFt = new FormattedText(
                edgeLabelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 9, _labelShadowBrush);
            ctx.DrawText(bgFt, new Point(midX - bgFt.Width / 2 + 0.8, midY - 12 + 0.8));
            ctx.DrawText(labelFt, new Point(midX - labelFt.Width / 2, midY - 12));
        }

        // Arrowhead for directional edges
        if (edge.IsDirectional)
        {
            // For curved edges, use the tangent at t=1 for arrowhead direction
            double arrowDx, arrowDy;
            if (useCurve)
            {
                // Tangent at t=1 of quadratic Bezier: B'(1) = 2*(to - control)
                arrowDx = to.X - controlPt.X;
                arrowDy = to.Y - controlPt.Y;
            }
            else
            {
                arrowDx = to.X - from.X;
                arrowDy = to.Y - from.Y;
            }
            double arrowLen = Math.Sqrt(arrowDx * arrowDx + arrowDy * arrowDy);
            if (arrowLen < 1) return;
            double nx = arrowDx / arrowLen, ny = arrowDy / arrowLen;
            double targetR = GetNodeRadius(edge.To);
            var tip = new Point(to.X - nx * targetR, to.Y - ny * targetR);
            double angle = Math.Atan2(arrowDy, arrowDx);
            double sz = isHovered ? 10 : 7;
            var p1 = new Point(tip.X - sz * Math.Cos(angle - 0.4), tip.Y - sz * Math.Sin(angle - 0.4));
            var p2 = new Point(tip.X - sz * Math.Cos(angle + 0.4), tip.Y - sz * Math.Sin(angle + 0.4));
            var arrow = new StreamGeometry();
            using (var gc = arrow.Open())
            {
                gc.BeginFigure(tip, true);
                gc.LineTo(p1);
                gc.LineTo(p2);
                gc.EndFigure(true);
            }
            ctx.DrawGeometry(brush, null, arrow);
        }
    }

    private void DrawHandles(DrawingContext ctx, ResearchGraphNode node)
    {
        double r = GetNodeRadius(node);
        var positions = new[]
        {
            new Point(node.X, node.Y - r - 8),
            new Point(node.X + r + 8, node.Y),
            new Point(node.X, node.Y + r + 8),
            new Point(node.X - r - 8, node.Y),
        };
        foreach (var pos in positions)
        {
            ctx.DrawEllipse(_handleFillBrush, HandlePen, pos, 5, 5);
        }
    }

    private double GetNodeRadius(ResearchGraphNode node)
    {
        return node.NodeType switch
        {
            ScholarNodeType.Passage => 10 + Math.Min(node.Degree * 2, 12),
            ScholarNodeType.Concept => 12 + Math.Min(node.Degree * 2, 14),
            ScholarNodeType.ZenMaster => 14 + Math.Min(node.Degree * 1.5, 10),
            ScholarNodeType.TermbaseEntry => 12 + Math.Min(node.Degree * 1.5, 10),
            ScholarNodeType.Collection => 14 + Math.Min(node.Degree * 2, 12),
            ScholarNodeType.Link => 12 + Math.Min(node.Degree * 1.5, 10),
            _ => 12
        };
    }

    private ResearchGraphNode? HitTest(double x, double y)
    {
        if (_vm == null) return null;
        // Transform screen coords to graph coords
        double gx = (x - _offsetX) / _zoom;
        double gy = (y - _offsetY) / _zoom;

        foreach (var node in _vm.GetVisibleNodes().Reverse())
        {
            double r = GetNodeRadius(node) + 15;  // Include edge-creation handles (drawn at r+8, radius 5)
            double dx = gx - node.X, dy = gy - node.Y;
            if (dx * dx + dy * dy <= r * r) return node;
        }
        return null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);

        // Minimap click-to-pan
        if (ShowMinimap)
        {
            const double mmW = 150, mmH = 100, margin = 10;
            double bw = Bounds.Width, bh = Bounds.Height;
            double mmX = bw - mmW - margin;
            double mmY = bh - mmH - margin;

            if (pos.X >= mmX && pos.X <= mmX + mmW &&
                pos.Y >= mmY && pos.Y <= mmY + mmH)
            {
                var nodes = _vm?.GetVisibleNodes();
                if (nodes != null && nodes.Count > 0)
                {
                    double minGX = double.MaxValue, minGY = double.MaxValue;
                    double maxGX = double.MinValue, maxGY = double.MinValue;
                    foreach (var n in nodes)
                    {
                        if (n.X < minGX) minGX = n.X;
                        if (n.Y < minGY) minGY = n.Y;
                        if (n.X > maxGX) maxGX = n.X;
                        if (n.Y > maxGY) maxGY = n.Y;
                    }
                    double gw = Math.Max(maxGX - minGX, 1);
                    double gh = Math.Max(maxGY - minGY, 1);
                    double pad = Math.Max(gw, gh) * 0.1;
                    minGX -= pad; minGY -= pad; gw += 2 * pad; gh += 2 * pad;
                    double scaleM = Math.Min(mmW / gw, mmH / gh);

                    double graphX = minGX + (pos.X - mmX) / scaleM;
                    double graphY = minGY + (pos.Y - mmY) / scaleM;

                    _offsetX = bw / 2 - graphX * _zoom;
                    _offsetY = bh / 2 - graphY * _zoom;
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
            }
        }

        // Edge handle detection
        if (_hoverNode != null && !_isCreatingEdge)
        {
            double gx = (pos.X - _offsetX) / _zoom;
            double gy = (pos.Y - _offsetY) / _zoom;
            double r = GetNodeRadius(_hoverNode);
            var handles = new (double X, double Y)[]
            {
                (_hoverNode.X, _hoverNode.Y - r - 8),
                (_hoverNode.X + r + 8, _hoverNode.Y),
                (_hoverNode.X, _hoverNode.Y + r + 8),
                (_hoverNode.X - r - 8, _hoverNode.Y),
            };
            foreach (var h in handles)
            {
                double dx = gx - h.X, dy = gy - h.Y;
                if (dx * dx + dy * dy <= 64)
                {
                    StartEdgeCreation(_hoverNode);
                    e.Pointer.Capture(this);
                    return;
                }
            }
        }

        var hit = HitTest(pos.X, pos.Y);

        if (hit != null)
        {
            if (e.ClickCount >= 2)
            {
                // Double-click always navigates (unpin moved to right-click menu)
                NodeDoubleClicked?.Invoke(this, hit);
                return;
            }
            _dragNode = hit;
            bool ctrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            NodeClicked?.Invoke(this, (hit, ctrlHeld));
        }
        else
        {
            // Try edge hit before panning
            var hitEdge = HitTestEdge(pos.X, pos.Y);
            if (hitEdge != null)
            {
                EdgeClicked?.Invoke(this, hitEdge);
            }
            else
            {
                // Click on empty space — deselect all nodes
                if (_vm != null)
                {
                    foreach (var n in _vm.Nodes) n.IsSelected = false;
                    _vm.SelectedNode = null;
                }
                _isPanning = true;
                _panStart = pos;
                InvalidateVisual();
            }
        }
        e.Pointer.Capture(this);
    }

    private ResearchGraphEdgeVm? HitTestEdge(double x, double y)
    {
        if (_vm == null) return null;
        double gx = (x - _offsetX) / _zoom;
        double gy = (y - _offsetY) / _zoom;
        const double hitThreshold = 8;
        ResearchGraphEdgeVm? nearest = null;
        double minDist = double.MaxValue;
        foreach (var edge in _vm.GetVisibleEdges())
        {
            double x1 = edge.From.X, y1 = edge.From.Y;
            double x2 = edge.To.X, y2 = edge.To.Y;
            double dx = x2 - x1, dy = y2 - y1;
            double len2 = dx * dx + dy * dy;
            if (len2 < 0.01) continue;
            double edgeLen = Math.Sqrt(len2);

            double dist;
            if (edgeLen >= 50)
            {
                // Curved edges: match the quadratic Bezier from DrawEdge
                double perpX = -dy / edgeLen, perpY = dx / edgeLen;
                double curveOffset = Math.Min(20, edgeLen * 0.12);
                double mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
                double cpx = mx + perpX * curveOffset, cpy = my + perpY * curveOffset;
                dist = DistToQuadBezier(gx, gy, x1, y1, cpx, cpy, x2, y2);
            }
            else
            {
                // Short straight edges: point-to-segment distance
                double t = Math.Clamp(((gx - x1) * dx + (gy - y1) * dy) / len2, 0, 1);
                double cx2 = x1 + t * dx, cy2 = y1 + t * dy;
                dist = Math.Sqrt((gx - cx2) * (gx - cx2) + (gy - cy2) * (gy - cy2));
            }

            if (dist <= hitThreshold && dist < minDist) { minDist = dist; nearest = edge; }
        }
        return nearest;
    }

    /// <summary>
    /// Approximate minimum distance from point (px,py) to a quadratic Bezier
    /// curve defined by start (x0,y0), control (cx,cy), end (x1,y1).
    /// Samples 16 points along the curve — accurate enough for hit-testing.
    /// </summary>
    private static double DistToQuadBezier(double px, double py,
        double x0, double y0, double cx, double cy, double x1, double y1)
    {
        double best = double.MaxValue;
        const int steps = 16;
        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps;
            double u = 1 - t;
            double bx = u * u * x0 + 2 * u * t * cx + t * t * x1;
            double by = u * u * y0 + 2 * u * t * cy + t * t * y1;
            double d2 = (px - bx) * (px - bx) + (py - by) * (py - by);
            if (d2 < best) best = d2;
        }
        return Math.Sqrt(best);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);

        if (_isPanning)
        {
            _offsetX += pos.X - _panStart.X;
            _offsetY += pos.Y - _panStart.Y;
            _panStart = pos;
            InvalidateVisual();
            return;
        }

        if (_isCreatingEdge)
        {
            _edgePreviewEnd = new Point((pos.X - _offsetX) / _zoom, (pos.Y - _offsetY) / _zoom);
            InvalidateVisual();
            return;
        }

        if (_dragNode != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragNode.X = (pos.X - _offsetX) / _zoom;
            _dragNode.Y = (pos.Y - _offsetY) / _zoom;
            InvalidateVisual();
            return;
        }

        // Hover detection
        var hover = HitTest(pos.X, pos.Y);
        if (hover != _hoverNode)
        {
            _hoverNode = hover;
            InvalidateVisual();
        }

        // Edge hover detection (only when not over a node)
        if (hover == null)
        {
            var edgeHover = HitTestEdge(pos.X, pos.Y);
            if (edgeHover != _hoverEdge)
            {
                _hoverEdge = edgeHover;
                InvalidateVisual();
            }
        }
        else if (_hoverEdge != null)
        {
            _hoverEdge = null;
            // InvalidateVisual() already called by node hover change
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isCreatingEdge && _edgeSource != null)
        {
            var pos = e.GetPosition(this);
            var target = HitTest(pos.X, pos.Y);
            if (target != null && target != _edgeSource)
            {
                EdgeDropped?.Invoke(this, (_edgeSource, target));
            }
            _isCreatingEdge = false;
            _edgeSource = null;
            InvalidateVisual();
        }

        _isPanning = false;
        if (_dragNode != null)
        {
            _dragNode.IsPinned = true;
            _dragNode = null;
        }
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        double factor = e.Delta.Y > 0 ? 1.15 : 0.87;
        double newZoom = Math.Clamp(_zoom * factor, 0.2, 4.0);

        // Zoom toward cursor
        _offsetX = pos.X - (pos.X - _offsetX) * (newZoom / _zoom);
        _offsetY = pos.Y - (pos.Y - _offsetY) * (newZoom / _zoom);
        _zoom = newZoom;
        InvalidateVisual();
    }

    public void StartEdgeCreation(ResearchGraphNode source)
    {
        _isCreatingEdge = true;
        _edgeSource = source;
        _edgePreviewEnd = new Point(source.X, source.Y);
    }

    /// <summary>
    /// Adjusts zoom and pan so that all visible nodes fit within the viewport with padding.
    /// </summary>
    public void FitToView()
    {
        if (_vm == null || _vm.Nodes.Count == 0) return;

        var visible = _vm.GetVisibleNodes();
        if (visible.Count == 0) return;

        double minX = visible.Min(n => n.X);
        double maxX = visible.Max(n => n.X);
        double minY = visible.Min(n => n.Y);
        double maxY = visible.Max(n => n.Y);

        double graphWidth = maxX - minX + 80;  // padding
        double graphHeight = maxY - minY + 80;

        double viewWidth = Bounds.Width > 0 ? Bounds.Width : 800;
        double viewHeight = Bounds.Height > 0 ? Bounds.Height : 600;

        double scaleX = viewWidth / graphWidth;
        double scaleY = viewHeight / graphHeight;
        _zoom = Math.Min(scaleX, scaleY);
        _zoom = Math.Clamp(_zoom, 0.1, 5.0);

        double cx = (minX + maxX) / 2.0;
        double cy = (minY + maxY) / 2.0;
        _offsetX = viewWidth / 2.0 - cx * _zoom;
        _offsetY = viewHeight / 2.0 - cy * _zoom;

        InvalidateVisual();
    }

    /// <summary>Current zoom/pan state for save/restore.</summary>
    public double CurrentZoom => _zoom;
    public double CurrentOffsetX => _offsetX;
    public double CurrentOffsetY => _offsetY;

    public void SetViewport(double zoom, double offsetX, double offsetY)
    {
        _zoom = Math.Clamp(zoom, 0.1, 5.0);
        _offsetX = offsetX;
        _offsetY = offsetY;
        InvalidateVisual();
    }

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    // --- Physics simulation ---

    /// <summary>
    /// Gets or sets whether the continuous physics simulation is active.
    /// </summary>
    public bool IsPhysicsEnabled
    {
        get => _physicsEnabled;
        set
        {
            _physicsEnabled = value;
            if (value) StartPhysics();
            else StopPhysics();
        }
    }

    private void StartPhysics()
    {
        if (_physicsTimer != null) return;
        _physicsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _physicsTimer.Tick += PhysicsTick;
        _physicsTimer.Start();
    }

    private void StopPhysics()
    {
        if (_physicsTimer != null)
        {
            _physicsTimer.Stop();
            _physicsTimer.Tick -= PhysicsTick;
            _physicsTimer = null;
        }
    }

    private void PhysicsTick(object? sender, EventArgs e)
    {
        if (_vm == null || _vm.Nodes.Count <= 1 || _vm.Nodes.Count > 300) return;
        if (_dragNode != null) return;  // pause physics while user is dragging

        var nodes = _vm.Nodes;
        int N = nodes.Count;

        double R = Math.Sqrt(N) * 80;
        double k = Math.Sqrt((R * R * 4) / N);
        double alpha = 0.005;

        // Gravity pulls toward FIXED viewport center (not center of mass,
        // which drifts with the nodes and can never pull them back)
        double cx = Bounds.Width > 0 ? Bounds.Width / 2.0 / (_zoom > 0 ? _zoom : 1) - _offsetX / (_zoom > 0 ? _zoom : 1) : 400;
        double cy = Bounds.Height > 0 ? Bounds.Height / 2.0 / (_zoom > 0 ? _zoom : 1) - _offsetY / (_zoom > 0 ? _zoom : 1) : 300;

        // Dampen existing velocities (inter-frame decay for subtle wobble)
        foreach (var n in nodes) { n.Vx *= 0.92; n.Vy *= 0.92; }

        // Repulsion (all pairs)
        for (int i = 0; i < N; i++)
        {
            if (nodes[i].IsPinned || nodes[i] == _dragNode) continue;
            for (int j = 0; j < N; j++)
            {
                if (i == j) continue;
                double dx = nodes[i].X - nodes[j].X;
                double dy = nodes[i].Y - nodes[j].Y;
                double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                double force = (k * k) / dist * alpha;
                nodes[i].Vx += (dx / dist) * force;
                nodes[i].Vy += (dy / dist) * force;
            }
        }

        // Gravity toward center of mass
        foreach (var n in nodes)
        {
            if (n.IsPinned || n == _dragNode) continue;
            n.Vx -= (n.X - cx) * 0.006;
            n.Vy -= (n.Y - cy) * 0.006;
        }

        // Edge attraction (keeps connected nodes loosely together)
        if (_vm != null)
        {
            foreach (var edge in _vm.GetVisibleEdges())
            {
                if (edge.From.IsPinned && edge.To.IsPinned) continue;
                double dx = edge.To.X - edge.From.X;
                double dy = edge.To.Y - edge.From.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                double attract = dist * 0.002;
                if (!edge.From.IsPinned && edge.From != _dragNode)
                {
                    edge.From.Vx += (dx / dist) * attract;
                    edge.From.Vy += (dy / dist) * attract;
                }
                if (!edge.To.IsPinned && edge.To != _dragNode)
                {
                    edge.To.Vx -= (dx / dist) * attract;
                    edge.To.Vy -= (dy / dist) * attract;
                }
            }
        }

        // Apply with max displacement clamp (damping already applied above)
        bool moved = false;
        foreach (var n in nodes)
        {
            if (n.IsPinned || n == _dragNode) continue;
            double disp = Math.Sqrt(n.Vx * n.Vx + n.Vy * n.Vy);
            if (disp > 0.3)
            {
                n.Vx = n.Vx / disp * 0.3;
                n.Vy = n.Vy / disp * 0.3;
            }
            if (disp > 0.01)
            {
                n.X += n.Vx;
                n.Y += n.Vy;
                // Soft bounds: clamp to [-2000, 2000]
                n.X = Math.Clamp(n.X, -2000, 2000);
                n.Y = Math.Clamp(n.Y, -2000, 2000);
                moved = true;
            }
        }

        if (moved) InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopPhysics();
        _entryTimer?.Stop();
        _entryTimer = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void DrawClusters(DrawingContext ctx)
    {
        if (_vm == null || !ShowClusters) return;
        var nodes = _vm.GetVisibleNodes();
        if (nodes.Count == 0) return;

        var groups = nodes.GroupBy(n => n.NodeType)
            .Where(g => g.Count() >= 2);

        foreach (var group in groups)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var n in group)
            {
                double r = GetNodeRadius(n);
                if (n.X - r < minX) minX = n.X - r;
                if (n.Y - r < minY) minY = n.Y - r;
                if (n.X + r > maxX) maxX = n.X + r;
                if (n.Y + r > maxY) maxY = n.Y + r;
            }

            const double pad = 30;
            minX -= pad; minY -= pad;
            maxX += pad; maxY += pad;

            double w = maxX - minX;
            double h = maxY - minY;
            double cornerRadius = Math.Min(w, h) * 0.3;

            var typeBrush = NodeBrushes.GetValueOrDefault(group.Key)
                ?? NodeBrushes[ScholarNodeType.Passage];
            Color baseColor;
            if (typeBrush is SolidColorBrush scb)
                baseColor = scb.Color;
            else
                baseColor = Color.Parse("#6EAFF8");

            var clusterColor = Color.FromArgb(25, baseColor.R, baseColor.G, baseColor.B);
            if (!_clusterBrushCache.TryGetValue(clusterColor, out var clusterBrush))
            {
                clusterBrush = new SolidColorBrush(clusterColor);
                _clusterBrushCache[clusterColor] = clusterBrush;
            }

            ctx.DrawRectangle(clusterBrush, null,
                new Rect(minX, minY, w, h),
                cornerRadius, cornerRadius);
        }
    }

    private void DrawMinimap(DrawingContext ctx)
    {
        if (_vm == null || !ShowMinimap) return;
        var nodes = _vm.GetVisibleNodes();
        if (nodes.Count == 0) return;

        const double mmW = 150, mmH = 100, margin = 10;
        double bw = Bounds.Width, bh = Bounds.Height;
        double mmX = bw - mmW - margin;
        double mmY = bh - mmH - margin;

        // Compute graph bounding box
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var n in nodes)
        {
            if (n.X < minX) minX = n.X;
            if (n.Y < minY) minY = n.Y;
            if (n.X > maxX) maxX = n.X;
            if (n.Y > maxY) maxY = n.Y;
        }
        double gw = Math.Max(maxX - minX, 1);
        double gh = Math.Max(maxY - minY, 1);
        double pad = Math.Max(gw, gh) * 0.1;
        minX -= pad; minY -= pad; gw += 2 * pad; gh += 2 * pad;

        double scaleX = mmW / gw, scaleY = mmH / gh;
        double scale = Math.Min(scaleX, scaleY);

        // Background
        ctx.DrawRectangle(_minimapBg, _minimapBorderPen,
            new Rect(mmX, mmY, mmW, mmH));

        // Draw edges as thin gray lines
        foreach (var edge in _vm.GetVisibleEdges())
        {
            double x1 = mmX + (edge.From.X - minX) * scale;
            double y1 = mmY + (edge.From.Y - minY) * scale;
            double x2 = mmX + (edge.To.X - minX) * scale;
            double y2 = mmY + (edge.To.Y - minY) * scale;
            ctx.DrawLine(_minimapEdgePen, new Point(x1, y1), new Point(x2, y2));
        }

        // Draw nodes as 2px dots
        foreach (var n in nodes)
        {
            double nx = mmX + (n.X - minX) * scale;
            double ny = mmY + (n.Y - minY) * scale;
            var brush = NodeBrushes.GetValueOrDefault(n.NodeType)
                ?? NodeBrushes[ScholarNodeType.Passage];
            ctx.DrawEllipse(brush, null, new Point(nx, ny), 2, 2);
        }

        // Viewport rectangle
        double vLeft = (0 - _offsetX) / _zoom;
        double vTop = (0 - _offsetY) / _zoom;
        double vRight = (bw - _offsetX) / _zoom;
        double vBottom = (bh - _offsetY) / _zoom;

        double vrX = mmX + (vLeft - minX) * scale;
        double vrY = mmY + (vTop - minY) * scale;
        double vrW = (vRight - vLeft) * scale;
        double vrH = (vBottom - vTop) * scale;

        // Clamp to minimap bounds
        vrX = Math.Max(vrX, mmX);
        vrY = Math.Max(vrY, mmY);
        vrW = Math.Min(vrW, mmX + mmW - vrX);
        vrH = Math.Min(vrH, mmY + mmH - vrY);

        if (vrW > 0 && vrH > 0)
            ctx.DrawRectangle(null, _minimapViewportPen,
                new Rect(vrX, vrY, vrW, vrH));
    }

    private void StartEntryAnimation()
    {
        _entryProgress = 0;
        _entryStart = DateTime.UtcNow;
        _entryTimer?.Stop();
        _entryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _entryTimer.Tick += (_, _) =>
        {
            double elapsed = (DateTime.UtcNow - _entryStart).TotalMilliseconds;
            _entryProgress = Math.Min(elapsed / 400.0, 1.0);
            InvalidateVisual();
            if (_entryProgress >= 1.0)
            {
                _entryProgress = 1.0;
                _entryTimer.Stop();
            }
        };
        _entryTimer.Start();
    }
}
