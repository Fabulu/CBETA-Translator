using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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

    // Cached pens for node rendering
    private static readonly IPen DefaultNodePen = new Pen(Brushes.White, 1.5);
    private static readonly IPen SelectedNodePen = new Pen(new SolidColorBrush(Color.Parse("#FFD700")), 3);
    private static readonly Dictionary<string, IBrush> _edgeBrushCache = new();

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
    private static readonly IBrush DimmedBrush = new SolidColorBrush(Color.FromArgb(90, 130, 130, 130));
    private static readonly IPen EdgePen = new Pen(new SolidColorBrush(Color.FromArgb(150, 150, 150, 150)), 1.5);
    private static readonly IPen HandlePen = new Pen(new SolidColorBrush(Color.Parse("#51D996")), 2);
    private static readonly IPen PreviewPen = new Pen(new SolidColorBrush(Color.Parse("#51D996")), 2) { DashStyle = DashStyle.Dash };

    public event EventHandler<ResearchGraphNode>? NodeClicked;
    public event EventHandler<ResearchGraphNode>? NodeDoubleClicked;
    public event EventHandler<(ResearchGraphNode From, ResearchGraphNode To)>? EdgeDropped;

    public void SetViewModel(ResearchGraphViewModel vm)
    {
        _vm = vm;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#1E1E23")), null, new Rect(bounds.Size));

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
        var center = new Point(node.X, node.Y);
        var brush = node.IsDimmed ? DimmedBrush : (NodeBrushes.GetValueOrDefault(node.NodeType) ?? NodeBrushes[ScholarNodeType.Passage]);
        var pen = node.IsSelected ? SelectedNodePen : DefaultNodePen;

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

        // Label (only at sufficient zoom)
        if (_zoom >= 0.5)
        {
            var labelSize = Math.Max(10, 13 * _zoom);
            var ft = new FormattedText(
                node.Label.Length > 25 ? node.Label[..24] + "\u2026" : node.Label,
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), labelSize, Brushes.White);
            ctx.DrawText(ft, new Point(node.X - ft.Width / 2, node.Y + r + 3));
        }
    }

    private void DrawDiamond(DrawingContext ctx, IBrush fill, IPen pen, Point center, double size)
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

    private void DrawHexagon(DrawingContext ctx, IBrush fill, IPen pen, Point center, double size)
    {
        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 3 * i - Math.PI / 2; // pointy-top hexagon
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
        double dx = to.X - from.X, dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1) return; // Skip degenerate edges

        var hex = edge.ColorHex ?? "#9E9E9E";
        if (!_edgeBrushCache.TryGetValue(hex, out var brush))
        {
            Color c;
            try { c = Color.Parse(hex); } catch { c = Color.Parse("#9E9E9E"); }
            brush = new SolidColorBrush(c);
            _edgeBrushCache[hex] = brush;
        }
        var pen = new Pen(brush, 1.5);
        ctx.DrawLine(pen, from, to);

        // Arrowhead for directional edges
        if (edge.IsDirectional)
        {
            double nx = dx / len, ny = dy / len;
            double targetR = GetNodeRadius(edge.To);
            var tip = new Point(to.X - nx * targetR, to.Y - ny * targetR);
            double angle = Math.Atan2(dy, dx);
            double sz = 7;
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
            ctx.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 81, 217, 150)), HandlePen, pos, 5, 5);
        }
    }

    private double GetNodeRadius(ResearchGraphNode node)
    {
        var degree = Math.Max(0, node.Degree);
        return node.NodeType switch
        {
            ScholarNodeType.Passage => 10 + Math.Min(degree * 2, 18),
            ScholarNodeType.Concept => 12 + Math.Min(degree * 2, 20),
            ScholarNodeType.ZenMaster => 14 + Math.Min(degree * 1.5, 14),
            ScholarNodeType.TermbaseEntry => 12 + Math.Min(degree * 1.5, 14),
            ScholarNodeType.Collection => 14 + Math.Min(degree * 2, 18),
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
            double r = GetNodeRadius(node) + 4;
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
            _isPanning = true;
            _panStart = pos;
        }
        e.Pointer.Capture(this);
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
}
