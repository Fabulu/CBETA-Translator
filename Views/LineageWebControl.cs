// Views/LineageWebControl.cs
// Custom Avalonia control that renders the Zen lineage web graph.
// Handles pan, zoom, click, and hover for interactive navigation.

using System;
using System.Globalization;
using System.Linq;
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
    public event EventHandler<ZenMasterRecord>? NodeDoubleClicked;

    /// <summary>Current zoom level (0.1 to 5.0). Used by zoom slider.</summary>
    public double Zoom => _zoom;

    /// <summary>Set zoom level centered on the viewport.</summary>
    public void SetZoom(double newZoom)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var oldZoom = _zoom;
        _zoom = Math.Clamp(newZoom, 0.1, 5.0);
        _offsetX = center.X - (center.X - _offsetX) * (_zoom / oldZoom);
        _offsetY = center.Y - (center.Y - _offsetY) * (_zoom / oldZoom);
        InvalidateVisual();
    }

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

        // Legend (fixed position, top-right)
        DrawLegend(ctx, bounds);

        using var transform = ctx.PushTransform(
            Matrix.CreateScale(_zoom, _zoom) *
            Matrix.CreateTranslation(_offsetX, _offsetY));

        // Compute visible viewport in canvas coordinates for culling
        double vLeft = -_offsetX / _zoom - 200;
        double vTop = -_offsetY / _zoom - 200;
        double vRight = (bounds.Width - _offsetX) / _zoom + 200;
        double vBottom = (bounds.Height - _offsetY) / _zoom + 200;

        // Edges (only if both endpoints potentially visible)
        //
        // When a focus set is active (user clicked a master), dim edges whose
        // endpoints aren't both in the focus set so the traced line stays
        // readable against the dimmed background.
        bool focusActive = _vm.FocusedNodes.Count > 0;
        foreach (var edge in _vm.Edges)
        {
            if (edge.From.X > vRight && edge.To.X > vRight) continue;
            if (edge.From.Y > vBottom && edge.To.Y > vBottom) continue;
            if (edge.From.X + LineageGraphViewModel.NodeWidth < vLeft && edge.To.X + LineageGraphViewModel.NodeWidth < vLeft) continue;

            var fromPt = new Point(
                edge.From.X + LineageGraphViewModel.NodeWidth,
                edge.From.Y + LineageGraphViewModel.NodeHeight / 2);
            var toPt = new Point(
                edge.To.X,
                edge.To.Y + LineageGraphViewModel.NodeHeight / 2);

            bool dimmed = focusActive && !(_vm.FocusedNodes.Contains(edge.From) && _vm.FocusedNodes.Contains(edge.To));
            byte alpha = dimmed ? (byte)12 : (byte)60;

            // Attestation-based edge style
            var att = edge.To.Attestation ?? "";
            DashStyle? dash = att switch
            {
                "D" => new DashStyle(new double[] { 2, 4 }, 0),
                "C" => new DashStyle(new double[] { 3, 3 }, 0),
                "B" => new DashStyle(new double[] { 6, 3 }, 0),
                _ => null
            };
            byte attAlpha = att == "D" ? (byte)(alpha / 2) : alpha;
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(attAlpha, 255, 255, 255)), 1.2)
            {
                DashStyle = dash
            };
            ctx.DrawLine(pen, fromPt, toPt);
        }

        // Orphan section label
        if (_vm.OrphanSectionY > 0)
        {
            var labelFt = new FormattedText("─── Unconnected Masters ───",
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI", FontStyle.Normal, FontWeight.SemiBold), 12,
                new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)));
            ctx.DrawText(labelFt, new Point(60, _vm.OrphanSectionY));
        }

        // Nodes (viewport culled)
        foreach (var node in _vm.Nodes.Where(n => !n.IsHidden))
        {
            // Viewport culling
            if (node.X + LineageGraphViewModel.NodeWidth < vLeft || node.X > vRight) continue;
            if (node.Y + LineageGraphViewModel.NodeHeight < vTop || node.Y > vBottom) continue;

            var schoolColor = LineageGraphViewModel.GetSchoolColor(node.School);

            // Focus dimming: when a focus set is active, nodes outside it drop
            // to ~20% opacity so the selected master's lineage chain stands out.
            bool isOutOfFocus = focusActive && !_vm.FocusedNodes.Contains(node);
            double focusAttenuation = isOutOfFocus ? 0.25 : 1.0;

            // Background (orphans rendered dimmer)
            byte baseAlpha = node.IsOrphan ? (byte)60 : (byte)120;
            byte activeAlpha = (byte)(node.IsSelected ? 200 : baseAlpha);
            byte finalAlpha = (byte)(activeAlpha * focusAttenuation);
            var fillBrush = new SolidColorBrush(Color.FromArgb(finalAlpha, schoolColor.R, schoolColor.G, schoolColor.B));

            var rect = new Rect(node.X, node.Y, LineageGraphViewModel.NodeWidth, LineageGraphViewModel.NodeHeight);
            ctx.FillRectangle(fillBrush, rect, 4);

            // Node border — dashed for Korean Seon
            bool isKorean = node.School?.Contains("Korean", StringComparison.OrdinalIgnoreCase) == true;
            var borderPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(180 * focusAttenuation), schoolColor.R, schoolColor.G, schoolColor.B)), 1.0)
            {
                DashStyle = isKorean ? new DashStyle(new double[] { 4, 3 }, 0) : null
            };
            ctx.DrawRectangle(borderPen, rect, 4);

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

            // Text labels — also dim when out-of-focus so they don't read through
            byte textAlpha = (byte)(255 * focusAttenuation);
            var textBrush = new SolidColorBrush(Color.FromArgb(textAlpha, 255, 255, 255));
            byte datesAlpha = (byte)(180 * focusAttenuation);
            var datesBrush = new SolidColorBrush(Color.FromArgb(datesAlpha, 255, 255, 255));

            // Name text
            var nameFt = new FormattedText(
                node.CanonicalName.Length > 18 ? node.CanonicalName[..17] + "..." : node.CanonicalName,
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI", FontStyle.Normal, FontWeight.SemiBold), 10.5, textBrush);
            ctx.DrawText(nameFt, new Point(node.X + 4, node.Y + 3));

            // Dates text
            var datesFt = new FormattedText(node.DatesSummary,
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Typeface.Default, 9, datesBrush);
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

                // Focus lineage view on clicked master — dims unrelated nodes
                // + edges. Double-click keeps the focus and also opens the
                // profile window.
                _vm.FocusOn(hit);

                if (e.ClickCount >= 2 && hit.Record != null)
                    NodeDoubleClicked?.Invoke(this, hit.Record);
                else if (hit.Record != null)
                    NodeClicked?.Invoke(this, hit.Record);

                InvalidateVisual();
                return;
            }

            // Empty-space click clears focus so everyone is visible again,
            // and releases the selection outline. Panning still starts so
            // dragging an empty spot still pans the canvas.
            if (_vm.FocusedNodes.Count > 0 || _vm.SelectedNode != null)
            {
                _vm.ClearFocus();
                foreach (var n in _vm.Nodes) n.IsSelected = false;
                _vm.SelectedNode = null;
                InvalidateVisual();
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
            // Pan delta is in screen space, independent of zoom
            _offsetX += pos.X - _lastPan.X;
            _offsetY += pos.Y - _lastPan.Y;
            _lastPan = pos;
            InvalidateVisual();
            e.Handled = true;
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

    private static void DrawLegend(DrawingContext ctx, Rect bounds)
    {
        var legendBg = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30));
        double legendX = bounds.Width - 170;
        double legendY = 8;
        double lineHeight = 18;

        var entries = LineageGraphViewModel.SchoolColors;
        double legendHeight = entries.Count * lineHeight + 12;

        ctx.FillRectangle(legendBg, new Rect(legendX, legendY, 160, legendHeight), 6);

        int i = 0;
        foreach (var (school, color) in entries)
        {
            double y = legendY + 6 + i * lineHeight;
            ctx.FillRectangle(new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B)),
                new Rect(legendX + 8, y + 2, 12, 12), 2);

            var ft = new FormattedText(school, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, Typeface.Default, 10,
                new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)));
            ctx.DrawText(ft, new Point(legendX + 26, y));
            i++;
        }
    }
}
