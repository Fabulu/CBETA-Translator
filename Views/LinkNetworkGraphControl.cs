using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReadZen.App.Views;

public class LinkNetworkGraphControl : Control
{
    private LinkGraphViewModel? _vm;
    private bool _isDraggingNode;
    private bool _isDraggingBackground;
    private GraphNode? _dragNode;
    private Point _lastDragPoint;

    private static readonly Dictionary<string, IBrush> s_edgeBrushes = new();
    private static readonly IBrush s_nodeFill = new SolidColorBrush(Color.FromArgb(200, 66, 133, 244));
    private static readonly IBrush s_selectedFill = new SolidColorBrush(Color.FromArgb(255, 255, 152, 0));
    private static readonly IPen s_nodeOutline = new Pen(Brushes.White, 2);
    private static readonly IPen s_selectedOutline = new Pen(Brushes.White, 3);
    private static readonly IBrush s_labelBrush = Brushes.White;
    private static readonly IBrush s_bgBrush = new SolidColorBrush(Color.FromRgb(30, 30, 35));

    public event EventHandler<string>? NodeSelected; // PassageId
    public event EventHandler<string>? NodeDoubleClicked; // PassageId
    public event EventHandler? GraphChanged;

    static LinkNetworkGraphControl()
    {
        foreach (var (key, hex) in LinkGraphViewModel.RelationColors)
        {
            if (Color.TryParse(hex, out var c))
                s_edgeBrushes[key] = new SolidColorBrush(c);
        }
    }

    public void SetViewModel(LinkGraphViewModel vm)
    {
        _vm = vm;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.DrawRectangle(s_bgBrush, null, new Rect(0, 0, bounds.Width, bounds.Height));

        if (_vm == null || _vm.Nodes.Count == 0)
        {
            var ft = new FormattedText("No graph data", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, Typeface.Default, 14, Brushes.Gray);
            context.DrawText(ft, new Point(bounds.Width / 2 - ft.Width / 2, bounds.Height / 2 - ft.Height / 2));
            return;
        }

        using var transform = context.PushTransform(
            Matrix.CreateScale(_vm.Zoom, _vm.Zoom) *
            Matrix.CreateTranslation(_vm.OffsetX, _vm.OffsetY));

        // Draw edges
        foreach (var edge in _vm.Edges)
        {
            var brush = s_edgeBrushes.GetValueOrDefault(edge.RelationType, Brushes.Gray);
            var pen = new Pen(brush, 1.5);
            context.DrawLine(pen, new Point(edge.From.X, edge.From.Y), new Point(edge.To.X, edge.To.Y));
        }

        // Draw nodes
        foreach (var node in _vm.Nodes)
        {
            bool sel = node.IsSelected;
            double r = sel ? 16 : 12;
            var fill = sel ? s_selectedFill : s_nodeFill;
            var outline = sel ? s_selectedOutline : s_nodeOutline;
            context.DrawEllipse(fill, outline, new Point(node.X, node.Y), r, r);

            // Label
            var ft = new FormattedText(node.Label, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas, 'Noto Sans CJK SC', 'Source Han Sans SC', sans-serif"),
                10, s_labelBrush);
            context.DrawText(ft, new Point(node.X - ft.Width / 2, node.Y + r + 2));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_vm == null) return;
        var pos = e.GetPosition(this);
        var hit = _vm.HitTest(pos.X, pos.Y);

        if (hit != null)
        {
            // Check for double-click
            if (e.ClickCount >= 2)
            {
                NodeDoubleClicked?.Invoke(this, hit.PassageId);
                return;
            }
            // Select and start drag
            foreach (var n in _vm.Nodes) n.IsSelected = false;
            hit.IsSelected = true;
            _vm.SelectedNode = hit;
            _isDraggingNode = true;
            _dragNode = hit;
            _lastDragPoint = pos;
            NodeSelected?.Invoke(this, hit.PassageId);
        }
        else
        {
            // Deselect all, start background drag
            foreach (var n in _vm.Nodes) n.IsSelected = false;
            _vm.SelectedNode = null;
            _isDraggingBackground = true;
            _lastDragPoint = pos;
        }
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_vm == null) return;
        var pos = e.GetPosition(this);

        if (_isDraggingNode && _dragNode != null)
        {
            double dx = (pos.X - _lastDragPoint.X) / _vm.Zoom;
            double dy = (pos.Y - _lastDragPoint.Y) / _vm.Zoom;
            _dragNode.X += dx;
            _dragNode.Y += dy;
            _lastDragPoint = pos;
            InvalidateVisual();
        }
        else if (_isDraggingBackground)
        {
            _vm.OffsetX += pos.X - _lastDragPoint.X;
            _vm.OffsetY += pos.Y - _lastDragPoint.Y;
            _lastDragPoint = pos;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        bool changed = _isDraggingNode || _isDraggingBackground;
        _isDraggingNode = false;
        _isDraggingBackground = false;
        _dragNode = null;
        if (changed)
            GraphChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_vm == null) return;
        double delta = e.Delta.Y > 0 ? 1.15 : 0.87;
        _vm.Zoom = Math.Max(0.3, Math.Min(3.0, _vm.Zoom * delta));
        InvalidateVisual();
        GraphChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }
}
