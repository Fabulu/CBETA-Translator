// Infrastructure/ConfidenceHeatmapRenderer.cs
// AvaloniaEdit background renderer that tints each line by textual confidence.
// Green = all witnesses agree, Amber = majority with dissent, Red = single-witness / unresolved.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

public enum ConfidenceLevel { High, Medium, Low, Unknown }

public sealed class ConfidenceHeatmapRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private Dictionary<int, ConfidenceLevel> _levels = new();

    private static readonly IBrush HighFill = new SolidColorBrush(Color.FromArgb(15, 0, 200, 0));
    private static readonly IBrush MediumFill = new SolidColorBrush(Color.FromArgb(20, 255, 180, 0));
    private static readonly IBrush LowFill = new SolidColorBrush(Color.FromArgb(25, 255, 50, 50));

    public ConfidenceHeatmapRenderer(TextView textView)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void SetConfidenceLevels(Dictionary<int, ConfidenceLevel> levels)
    {
        _levels = levels ?? new();
        _textView.InvalidateVisual();
    }

    public void Clear()
    {
        _levels = new();
        _textView.InvalidateVisual();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_levels.Count == 0) return;
        if (textView.Document == null) return;

        foreach (var vl in textView.VisualLines)
        {
            int line = vl.FirstDocumentLine.LineNumber - 1; // 0-based
            if (!_levels.TryGetValue(line, out var level)) continue;

            var brush = level switch
            {
                ConfidenceLevel.High => HighFill,
                ConfidenceLevel.Medium => MediumFill,
                ConfidenceLevel.Low => LowFill,
                _ => null,
            };
            if (brush == null) continue;

            var rect = new Rect(0, vl.VisualTop - textView.VerticalOffset,
                textView.Bounds.Width, vl.Height);
            drawingContext.FillRectangle(brush, rect);
        }
    }
}

public static class ConfidenceAnalyzer
{
    /// <summary>
    /// Analyzes apparatus entries and returns per-line confidence levels.
    /// Lines with no apparatus entry default to High (all witnesses agree).
    /// Lines with multiple readings = Medium (majority with dissent).
    /// Lines where a reading has only one witness or status is unresolved = Low.
    /// </summary>
    public static Dictionary<int, ConfidenceLevel> Analyze(
        ApparatusInfo? apparatus, int totalLines)
    {
        var result = new Dictionary<int, ConfidenceLevel>(totalLines);

        // Default all lines to High (consensus).
        for (int i = 0; i < totalLines; i++)
            result[i] = ConfidenceLevel.High;

        if (apparatus?.Entries == null) return result;

        foreach (var entry in apparatus.Entries)
        {
            // Extract 1-based line number from locus_id (e.g. "line-5" -> 4 as 0-based).
            int line = ParseLineFromLocus(entry.LocusId);
            if (line < 0 || line >= totalLines) continue;

            var readings = entry.Readings;
            if (readings == null || readings.Count <= 1)
            {
                // Single or no reading — check status for unresolved.
                if (string.Equals(entry.Status, "unresolved", StringComparison.OrdinalIgnoreCase))
                    result[line] = ConfidenceLevel.Low;
                continue;
            }

            // Multiple readings exist — check if any is single-witness.
            bool hasSingleWitness = readings.Any(r =>
                r.IsOcrOnly == true || r.IsHumanChecked == false);

            if (hasSingleWitness ||
                string.Equals(entry.Status, "unresolved", StringComparison.OrdinalIgnoreCase))
            {
                result[line] = ConfidenceLevel.Low;
            }
            else
            {
                result[line] = ConfidenceLevel.Medium;
            }
        }

        return result;
    }

    private static int ParseLineFromLocus(string? locusId)
    {
        if (string.IsNullOrEmpty(locusId)) return -1;
        // Try "line-N" format first.
        int idx = locusId.LastIndexOf('-');
        if (idx >= 0 && int.TryParse(locusId.AsSpan(idx + 1), out int n))
            return n - 1; // Convert to 0-based.
        // Fallback: parse trailing digits.
        int start = locusId.Length;
        while (start > 0 && char.IsDigit(locusId[start - 1])) start--;
        if (start < locusId.Length && int.TryParse(locusId.AsSpan(start), out int m))
            return m - 1;
        return -1;
    }
}
