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

    // Entry animation
    private double _entryProgress = 0;
    private DateTime _entryStart;
    private DispatcherTimer? _entryTimer;

    // Node type colors
    private static readonly Dictionary<ScholarNodeType, IBrush> NodeBrushes = new()
    {
        [ScholarNodeType.Passage] = new SolidColorBrush(Color.Parse("#6EAFF8")),
        [ScholarNodeType.Concept] = new SolidColorBrush(Color.Parse("#FF8A65")),
        [ScholarNodeType.ZenMaster] = new SolidColorBrush(Color.Parse("#64B5F6")),
        [ScholarNodeType.TermbaseEntry] = new SolidColorBrush(Color.Parse("#81C784")),
        [ScholarNodeType.Collection] = new SolidColorBrush(Color.Parse("#AB47BC")),
    };

    private static readonly IBrush SelectedBrush = new SolidColorBrush(Color.Parse("#FFD700"));
    private static readonly IBrush DimmedBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
    private static readonly IPen EdgePen = new Pen(new SolidColorBrush(Color.FromArgb(150, 150, 150, 150)), 1.5);
    private static readonly IPen HandlePen = new Pen(new SolidColorBrush(Color.Parse("#51D996")), 2);
    private static readonly IPen PreviewPen = new Pen(new SolidColorBrush(Color.Parse("#51D996")), 2) { DashStyle = DashStyle.Dash };
    private static readonly IBrush _handleFillBrush = new SolidColorBrush(Color.FromArgb(180, 81, 217, 150));

    // --- Cached brushes and pens (avoid per-frame allocation) ---
    private static readonly IBrush _bgBrush = new SolidColorBrush(Color.Parse("#1E1E23"));
    private static readonly IBrush _labelShadowBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
    private static readonly IBrush _shadowBrushOuter = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
    private static readonly IBrush _shadowBrushInner = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
    private static readonly IPen _selectedPen = new Pen(SelectedBrush, 3);
    private static readonly IPen _hoverPen = new Pen(new SolidColorBrush(Color.Parse("#FFD700")), 2.5);
    private static readonly IPen _defaultNodePen = new Pen(new SolidColorBrush(Color.FromArgb(153, 255, 255, 255)), 1.2);
    private static readonly IPen _searchHighlightPen = new Pen(new SolidColorBrush(Color.Parse("#00E5FF")), 3);

    public event EventHandler<ResearchGraphNode>? NodeClicked;
    public event EventHandler<ResearchGraphNode>? NodeDoubleClicked;
    public event EventHandler<(ResearchGraphNode From, ResearchGraphNode To)>? EdgeDropped;
    public event EventHandler<ResearchGraphEdgeVm>? EdgeClicked;

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
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.DrawRectangle(_bgBrush, null, new Rect(bounds.Size));

        if (_vm == null) return;

        // Apply zoom + pan
        using (context.PushTransform(
            Matrix.CreateScale(_zoom, _zoom) *
            Matrix.CreateTranslation(_offsetX, _offsetY)))
        {
            var visibleEdges = _vm.GetVisibleEdges();
            var visibleNodes = _vm.GetVisibleNodes();

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
    }

    private void DrawNode(DrawingContext ctx, ResearchGraphNode node)
    {
        double r = GetNodeRadius(node);
        double entryScale = EaseOutCubic(_entryProgress);
        r *= entryScale;
        if (r < 0.5) return; // skip tiny nodes during animation start
        var center = new Point(node.X, node.Y);
        var brush = node.IsDimmed ? DimmedBrush : (NodeBrushes.GetValueOrDefault(node.NodeType) ?? NodeBrushes[ScholarNodeType.Passage]);
        IPen pen;
        if (node.IsSelected)
            pen = _selectedPen;
        else if (node == _hoverNode)
            pen = _hoverPen;
        else if (_vm!.HighlightedNodeIds.Contains(node.NodeId))
            pen = _searchHighlightPen;
        else
            pen = _defaultNodePen;

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
            case ScholarNodeType.Collection:
                ctx.DrawRectangle(brush, pen, new Rect(node.X - r, node.Y - r * 0.7, r * 2, r * 1.4), r * 0.2, r * 0.2);
                break;
        }

        // Label with shadow outline (only at sufficient zoom and after entry animation starts)
        if (_zoom >= 0.5 && _entryProgress > 0.3)
        {
            var labelSize = Math.Max(9, 11 * Math.Min(_zoom, 1.5));
            var labelText = node.Label.Length > 25 ? node.Label[..24] + "\u2026" : node.Label;
            var labelY = node.Y + r + 3;

            // Shadow pass: draw black text at 4 offsets (up/down/left/right) for outline effect
            var shadowFt = new FormattedText(
                labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), labelSize, _labelShadowBrush);
            var baseX = node.X - shadowFt.Width / 2;
            double so = 1.2; // shadow offset
            ctx.DrawText(shadowFt, new Point(baseX - so, labelY));
            ctx.DrawText(shadowFt, new Point(baseX + so, labelY));
            ctx.DrawText(shadowFt, new Point(baseX, labelY - so));
            ctx.DrawText(shadowFt, new Point(baseX, labelY + so));

            // Main pass: white text on top
            var mainFt = new FormattedText(
                labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), labelSize, Brushes.White);
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
        double thickness = isHovered ? 3.0 : 1.5;

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

        // Glow layer for hovered edge (wide, semi-transparent)
        if (isHovered)
        {
            var glowBrush = new SolidColorBrush(Color.FromArgb(60, edgeColor.R, edgeColor.G, edgeColor.B));
            var glowPen = new Pen(glowBrush, 8);
            ctx.DrawLine(glowPen, from, to);
        }

        var brush = new SolidColorBrush(finalColor);
        var pen = new Pen(brush, thickness);

        // Non-directional edges: dashed
        if (!edge.IsDirectional)
        {
            pen = new Pen(brush, thickness) { DashStyle = DashStyle.Dash };
        }

        ctx.DrawLine(pen, from, to);

        // Edge type label: show on hover OR on ego-relevant edges at sufficient zoom
        bool showLabel = false;
        if (isHovered && !string.IsNullOrEmpty(edge.RelationType))
            showLabel = true;
        else if (hasEgo && _zoom >= 0.7 && !string.IsNullOrEmpty(edge.RelationType))
        {
            string egoId = _vm.SelectedNode!.NodeId;
            if (edge.From.NodeId == egoId || edge.To.NodeId == egoId)
                showLabel = true;
        }

        if (showLabel)
        {
            double midX = (from.X + to.X) / 2;
            double midY = (from.Y + to.Y) / 2;
            var labelColor = new SolidColorBrush(Color.FromArgb((byte)(isHovered ? 255 : 200), edgeColor.R, edgeColor.G, edgeColor.B));
            var labelFt = new FormattedText(
                edge.RelationType, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 9, labelColor);
            // Shadow behind label for readability
            var bgFt = new FormattedText(
                edge.RelationType, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 9, _labelShadowBrush);
            ctx.DrawText(bgFt, new Point(midX - bgFt.Width / 2 + 0.8, midY - 12 + 0.8));
            ctx.DrawText(labelFt, new Point(midX - labelFt.Width / 2, midY - 12));
        }

        // Arrowhead for directional edges
        if (edge.IsDirectional)
        {
            double dx = to.X - from.X, dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;
            double nx = dx / len, ny = dy / len;
            double targetR = GetNodeRadius(edge.To);
            var tip = new Point(to.X - nx * targetR, to.Y - ny * targetR);
            double angle = Math.Atan2(dy, dx);
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
                NodeDoubleClicked?.Invoke(this, hit);
                return;
            }
            _dragNode = hit;
            NodeClicked?.Invoke(this, hit);
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
                _isPanning = true;
                _panStart = pos;
            }
        }
        e.Pointer.Capture(this);
    }

    private ResearchGraphEdgeVm? HitTestEdge(double x, double y)
    {
        if (_vm == null) return null;
        double gx = (x - _offsetX) / _zoom;
        double gy = (y - _offsetY) / _zoom;
        ResearchGraphEdgeVm? nearest = null;
        double minDist = double.MaxValue;
        foreach (var edge in _vm.GetVisibleEdges())
        {
            double x1 = edge.From.X, y1 = edge.From.Y;
            double x2 = edge.To.X, y2 = edge.To.Y;
            double dx = x2 - x1, dy = y2 - y1;
            double len2 = dx * dx + dy * dy;
            if (len2 < 0.01) continue;
            double t = Math.Clamp(((gx - x1) * dx + (gy - y1) * dy) / len2, 0, 1);
            double cx2 = x1 + t * dx, cy2 = y1 + t * dy;
            double dist = Math.Sqrt((gx - cx2) * (gx - cx2) + (gy - cy2) * (gy - cy2));
            if (dist <= 5 && dist < minDist) { minDist = dist; nearest = edge; }
        }
        return nearest;
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
        _dragNode = null;
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

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _entryTimer?.Stop();
        _entryTimer = null;
        base.OnDetachedFromVisualTree(e);
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
