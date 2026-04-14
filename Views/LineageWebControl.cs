// Views/LineageWebControl.cs
// Custom Avalonia control that renders the Zen lineage web graph.
// Handles pan, zoom, click, and hover for interactive navigation.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

public sealed class LineageWebControl : Control
{
    private LineageGraphViewModel? _vm;
    private double _offsetX, _offsetY;
    private double _zoom = 1.0;
    private Point _lastPan;
    private bool _isPanning;

    public event EventHandler<ZenMasterRecord>? NodeClicked;

    public LineageWebControl()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public void SetViewModel(LineageGraphViewModel vm)
    {
        _vm = vm;
        _offsetX = 0;
        _offsetY = 0;
        _zoom = 0.8; // start zoomed out a bit for overview
        InvalidateVisual();
    }

    public void CenterOnNode(LineageGraphNode node)
    {
        _offsetX = -(node.X * _zoom - Bounds.Width / 2 + LineageGraphViewModel.NodeWidth * _zoom / 2);
        _offsetY = -(node.Y * _zoom - Bounds.Height / 2 + LineageGraphViewModel.NodeHeight * _zoom / 2);
        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        if (_vm == null) return;

        var bounds = Bounds;
        ctx.FillRectangle(Brushes.Transparent, new Rect(bounds.Size));

        using var transform = ctx.PushTransform(Matrix.CreateTranslation(_offsetX, _offsetY) * Matrix.CreateScale(_zoom, _zoom));

        // Era bands
        var eraBandBrush = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
        var eraBandTextBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        foreach (var (century, y) in _vm.GetEraBands())
        {
            ctx.FillRectangle(eraBandBrush, new Rect(0, y, 5000, LineageGraphViewModel.PixelsPerYear * 100));
            var ft = new FormattedText($"{century}s", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, Typeface.Default, 10, eraBandTextBrush);
            ctx.DrawText(ft, new Point(5, y + 2));
        }

        // Edges
        var edgePen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1.2);
        foreach (var edge in _vm.Edges)
        {
            var fromCenter = new Point(
                edge.From.X + LineageGraphViewModel.NodeWidth / 2,
                edge.From.Y + LineageGraphViewModel.NodeHeight);
            var toCenter = new Point(
                edge.To.X + LineageGraphViewModel.NodeWidth / 2,
                edge.To.Y);

            // Simple straight line (Bezier curves would be nicer but more code)
            ctx.DrawLine(edgePen, fromCenter, toCenter);
        }

        // Nodes
        foreach (var node in _vm.Nodes)
        {
            var schoolColor = LineageGraphViewModel.GetSchoolColor(node.School);

            // Background
            var fillBrush = new SolidColorBrush(node.IsSelected
                ? Color.FromArgb(200, schoolColor.R, schoolColor.G, schoolColor.B)
                : Color.FromArgb(120, schoolColor.R, schoolColor.G, schoolColor.B));

            var rect = new Rect(node.X, node.Y, LineageGraphViewModel.NodeWidth, LineageGraphViewModel.NodeHeight);
            ctx.FillRectangle(fillBrush, rect, 4);

            // Highlight ring for search matches
            if (node.IsHighlighted)
            {
                var highlightPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 0)), 2.5);
                ctx.DrawRectangle(highlightPen, rect.Inflate(3), 6);
            }

            // Selection border
            if (node.IsSelected)
            {
                var selPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 160, 255)), 2);
                ctx.DrawRectangle(selPen, rect, 4);
            }

            // Name text
            var nameFt = new FormattedText(
                node.CanonicalName.Length > 18 ? node.CanonicalName[..17] + "..." : node.CanonicalName,
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI", FontStyle.Normal, FontWeight.SemiBold), 10.5, Brushes.White);
            ctx.DrawText(nameFt, new Point(node.X + 4, node.Y + 3));

            // Dates text
            var datesFt = new FormattedText(node.DatesSummary,
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Typeface.Default, 9, new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)));
            ctx.DrawText(datesFt, new Point(node.X + 4, node.Y + 20));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_vm == null) return;

        var pos = e.GetPosition(this);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Hit test for node click
            var canvasX = (pos.X - _offsetX) / _zoom;
            var canvasY = (pos.Y - _offsetY) / _zoom;
            var hit = _vm.HitTest(canvasX, canvasY);

            if (hit != null)
            {
                foreach (var n in _vm.Nodes) n.IsSelected = false;
                hit.IsSelected = true;
                _vm.SelectedNode = hit;
                if (hit.Record != null) NodeClicked?.Invoke(this, hit.Record);
                InvalidateVisual();
                return;
            }

            // Start panning
            _isPanning = true;
            _lastPan = pos;
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPanning)
        {
            var pos = e.GetPosition(this);
            _offsetX += pos.X - _lastPan.X;
            _offsetY += pos.Y - _lastPan.Y;
            _lastPan = pos;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta = e.Delta.Y > 0 ? 1.1 : 0.9;
        var pos = e.GetPosition(this);

        // Zoom toward cursor position
        var oldZoom = _zoom;
        _zoom = Math.Clamp(_zoom * delta, 0.1, 5.0);

        _offsetX = pos.X - (pos.X - _offsetX) * (_zoom / oldZoom);
        _offsetY = pos.Y - (pos.Y - _offsetY) * (_zoom / oldZoom);

        InvalidateVisual();
        e.Handled = true;
    }
}
