// Infrastructure/EvidenceGutterMargin.cs
// Custom AvaloniaEdit margin that shows colored dots next to lines with apparatus entries.
// Attach to any TextEditor via: textArea.LeftMargins.Add(new EvidenceGutterMargin());

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
/// Gutter margin that renders IndianRed dots next to lines whose Chinese text
/// has apparatus entries (variant readings, corrections, etc.) in the critical edition.
/// </summary>
public sealed class EvidenceGutterMargin : AbstractMargin
{
    private const double MarginWidth = 16;
    private const double DotDiameter = 8;
    private const double DotRadius = DotDiameter / 2;

    private static readonly IBrush DotBrush = new SolidColorBrush(Color.FromRgb(205, 92, 92)); // IndianRed

    /// <summary>
    /// Line number (0-based) to tooltip text for lines that have apparatus entries.
    /// </summary>
    private Dictionary<int, string>? _evidenceLines;

    /// <summary>
    /// Sets the 0-based line numbers that should show an evidence dot, along with
    /// per-line tooltip text describing the decision type.
    /// </summary>
    public void SetEvidenceLines(Dictionary<int, string>? lines)
    {
        _evidenceLines = lines;
        InvalidateVisual();
    }

    /// <summary>
    /// Clears all evidence indicators.
    /// </summary>
    public void Clear()
    {
        _evidenceLines = null;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(MarginWidth, 0);
    }

    public override void Render(DrawingContext drawingContext)
    {
        var tv = TextView;
        if (tv == null || tv.VisualLinesValid == false || _evidenceLines == null || _evidenceLines.Count == 0)
            return;

        // Fill background to match the gutter area.
        drawingContext.FillRectangle(Brushes.Transparent, Bounds);

        foreach (var visualLine in tv.VisualLines)
        {
            int lineNumber = visualLine.FirstDocumentLine.LineNumber - 1; // 0-based
            if (!_evidenceLines.ContainsKey(lineNumber))
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
        if (tv == null || _evidenceLines == null || _evidenceLines.Count == 0)
        {
            ToolTip.SetTip(this, null);
            return;
        }

        var pos = e.GetPosition(this);
        int? hitLine = GetLineAtY(pos.Y);

        if (hitLine.HasValue && _evidenceLines.TryGetValue(hitLine.Value, out var tip))
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
    /// or null if no evidence line is near that position.
    /// </summary>
    private int? GetLineAtY(double y)
    {
        var tv = TextView;
        if (tv == null || _evidenceLines == null)
            return null;

        foreach (var visualLine in tv.VisualLines)
        {
            int lineNumber = visualLine.FirstDocumentLine.LineNumber - 1;
            if (!_evidenceLines.ContainsKey(lineNumber))
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
