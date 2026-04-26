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
    private static readonly IBrush s_shadowBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));

    // School colors optimized for dark background contrast
    private static readonly Dictionary<string, IBrush> s_schoolBrushes = new()
    {
        ["Linji"] = new SolidColorBrush(Color.FromRgb(255, 107, 107)),      // #FF6B6B
        ["Caodong"] = new SolidColorBrush(Color.FromRgb(89, 179, 255)),     // #59B3FF
        ["Fayan"] = new SolidColorBrush(Color.FromRgb(81, 217, 150)),       // #51D996
        ["Yunmen"] = new SolidColorBrush(Color.FromRgb(255, 179, 71)),      // #FFB347
        ["Guiyang"] = new SolidColorBrush(Color.FromRgb(200, 84, 217)),     // #C854D9
        ["Early Chan"] = new SolidColorBrush(Color.FromRgb(126, 207, 255)), // #7ECFFF
    };

    public event EventHandler<string>? NodeSelected; // PassageId
    public event EventHandler<string>? NodeDoubleClicked; // PassageId
    public event EventHandler? GraphChanged;

    static LinkNetworkGraphControl()
    {
        // Use semantic edge color groups for better visual grouping
        foreach (var (key, hex) in LinkGraphViewModel.EdgeColorGroups)
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

        double zoom = _vm.Zoom;

        using var transform = context.PushTransform(
            Matrix.CreateScale(zoom, zoom) *
            Matrix.CreateTranslation(_vm.OffsetX, _vm.OffsetY));

        // Draw edges
        foreach (var edge in _vm.Edges)
        {
            var brush = s_edgeBrushes.GetValueOrDefault(edge.RelationType, Brushes.Gray);
            var pen = new Pen(brush, 1.5);
            context.DrawLine(pen, new Point(edge.From.X, edge.From.Y), new Point(edge.To.X, edge.To.Y));
        }

        // Adaptive label sizing
        double zoomFactor = Math.Min(zoom, 1.5) * 0.85;
        double primaryFontSize = Math.Max(7, 11.0 * zoomFactor);
        double secondaryFontSize = Math.Max(6, 8.0 * zoomFactor);

        // Draw nodes
        foreach (var node in _vm.Nodes)
        {
            bool sel = node.IsSelected;
            double r = sel ? 16 : 8 + Math.Min(node.Degree * 2, 14);
            var fill = sel ? s_selectedFill : GetNodeBrush(node);
            var outline = sel ? s_selectedOutline : s_nodeOutline;
            context.DrawEllipse(fill, outline, new Point(node.X, node.Y), r, r);

            // Label visibility: hide labels below 0.4x zoom (except selected)
            bool showLabel = node.IsSelected || zoom >= 0.4;
            if (!showLabel) continue;

            // At 0.4-0.7x zoom, only show hubs (degree >= 3) and selected
            if (zoom < 0.7 && !node.IsSelected && node.Degree < 3) continue;

            // Draw shadow text (offset 1,1)
            var shadowText = new FormattedText(node.Label, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas, 'Noto Sans CJK SC', 'Source Han Sans SC', sans-serif"),
                primaryFontSize, s_shadowBrush);
            context.DrawText(shadowText, new Point(node.X - shadowText.Width / 2 + 1, node.Y + r + 3));

            // Draw actual label text
            var ft = new FormattedText(node.Label, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas, 'Noto Sans CJK SC', 'Source Han Sans SC', sans-serif"),
                primaryFontSize, s_labelBrush);
            context.DrawText(ft, new Point(node.X - ft.Width / 2, node.Y + r + 2));
        }
    }

    /// <summary>
    /// Returns node brush based on the current color mode and node properties.
    /// </summary>
    private IBrush GetNodeBrush(GraphNode node)
    {
        if (_vm == null) return s_nodeFill;

        switch (_vm.CurrentColorMode)
        {
            case GraphColorMode.Importance:
                // Scale brightness by degree
                int brightness = Math.Min(255, 100 + node.Degree * 30);
                return new SolidColorBrush(Color.FromRgb((byte)brightness, (byte)(brightness * 0.6), 50));

            case GraphColorMode.School:
                // Would need school info on the node; fall through to default for now
                return s_nodeFill;

            case GraphColorMode.ReadingStatus:
            default:
                // Default blue with opacity based on degree (more connected = more prominent)
                byte alpha = (byte)Math.Min(255, 150 + node.Degree * 20);
                return new SolidColorBrush(Color.FromArgb(alpha, 66, 133, 244));
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
