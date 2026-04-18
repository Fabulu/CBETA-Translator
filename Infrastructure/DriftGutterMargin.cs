// Infrastructure/DriftGutterMargin.cs
// Custom AvaloniaEdit margin that shows orange dots next to lines with stale translations.
// Attach to any TextEditor via: textArea.LeftMargins.Add(new DriftGutterMargin());

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Gutter margin that renders orange dots next to lines whose translations
/// have drifted from the current Chinese reading.
/// </summary>
public sealed class DriftGutterMargin : AbstractMargin
{
    private const double MarginWidth = 16;
    private const double DotDiameter = 8;
    private const double DotRadius = DotDiameter / 2;

    private static readonly IBrush DotBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));

    private HashSet<int> _staleLines = new();
    private Dictionary<int, string> _tooltips = new();

    /// <summary>
    /// Sets the 0-based line numbers that should show an orange dot.
    /// </summary>
    public void SetStaleLines(HashSet<int> staleLines)
    {
        _staleLines = staleLines ?? new();
        InvalidateVisual();
    }

    /// <summary>
    /// Sets per-line tooltip text (0-based line number -> diff summary).
    /// </summary>
    public void SetTooltips(Dictionary<int, string> tooltips)
    {
        _tooltips = tooltips ?? new();
    }

    /// <summary>
    /// Clears all stale indicators and tooltips.
    /// </summary>
    public void Clear()
    {
        _staleLines = new();
        _tooltips = new();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(MarginWidth, 0);
    }

    public override void Render(DrawingContext drawingContext)
    {
        var tv = TextView;
        if (tv == null || tv.VisualLinesValid == false)
            return;

        // Fill background to match the gutter area.
        drawingContext.FillRectangle(Brushes.Transparent, Bounds);

        foreach (var visualLine in tv.VisualLines)
        {
            int lineNumber = visualLine.FirstDocumentLine.LineNumber - 1; // 0-based
            if (!_staleLines.Contains(lineNumber))
                continue;

            double y = visualLine.GetTextLineVisualYPosition(
                visualLine.TextLines[0], VisualYPosition.LineMiddle) - tv.VerticalOffset;

            double x = (MarginWidth - DotDiameter) / 2;

            drawingContext.DrawEllipse(
                DotBrush,
                null,
                new Point(x + DotRadius, y),
                DotRadius,
                DotRadius);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var tv = TextView;
        if (tv == null || _tooltips.Count == 0)
        {
            ToolTip.SetTip(this, null);
            return;
        }

        var pos = e.GetPosition(this);
        int? hitLine = GetLineAtY(pos.Y);

        if (hitLine.HasValue && _tooltips.TryGetValue(hitLine.Value, out var tip))
        {
            ToolTip.SetTip(this, tip);
            ToolTip.SetIsOpen(this, true);
        }
        else
        {
            ToolTip.SetTip(this, null);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ToolTip.SetIsOpen(this, false);
        ToolTip.SetTip(this, null);
    }

    /// <summary>
    /// Finds the 0-based line number at the given Y coordinate within this margin,
    /// or null if no stale line is near that position.
    /// </summary>
    private int? GetLineAtY(double y)
    {
        var tv = TextView;
        if (tv == null)
            return null;

        foreach (var visualLine in tv.VisualLines)
        {
            int lineNumber = visualLine.FirstDocumentLine.LineNumber - 1;
            if (!_staleLines.Contains(lineNumber))
                continue;

            double lineY = visualLine.GetTextLineVisualYPosition(
                visualLine.TextLines[0], VisualYPosition.LineMiddle) - tv.VerticalOffset;

            // Hit test within the dot area (generous vertical tolerance).
            if (Math.Abs(y - lineY) <= DotDiameter)
                return lineNumber;
        }

        return null;
    }
}
