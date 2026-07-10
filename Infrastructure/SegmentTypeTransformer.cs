// Infrastructure/SegmentTypeTransformer.cs
// View-layer colorizing transformer that applies semantic segment styling
// (verse, dialogue, commentary, heading, etc.) to AvaloniaEdit text lines.
// Receives the segment map + RenderedDocument.Segments to map rendered
// offsets to lb-IDs and then to segment types.
//
// Styling rules:
//   verse       → italic + gold foreground tint
//   dialogue    → accent foreground tint
//   commentary  → muted/dimmed foreground
//   heading     → bold
//   dharani     → italic + distinct color
//   All others  → no visual change (default prose)
//
// This transformer is purely additive — it never removes existing styling
// from other transformers (marker colorizers, highlights, etc.).

using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Colorizing transformer that overlays semantic segment type styling on
/// rendered Chinese text lines. Performs a lookup chain:
/// rendered line offset → RenderSegment (via binary search) → lb-ID
/// (extracted from segment key) → SegmentInfo (from SegmentMap) → visual style.
/// </summary>
public sealed class SegmentTypeTransformer : DocumentColorizingTransformer
{
    private readonly SegmentMap _segmentMap;
    private readonly IReadOnlyList<RenderSegment> _renderSegments;

    // Pre-built sorted array for binary search on rendered offsets
    private readonly int[] _segStartOffsets;

    public SegmentTypeTransformer(SegmentMap segmentMap, IReadOnlyList<RenderSegment> renderSegments)
    {
        _segmentMap = segmentMap ?? throw new ArgumentNullException(nameof(segmentMap));
        _renderSegments = renderSegments ?? throw new ArgumentNullException(nameof(renderSegments));

        _segStartOffsets = new int[_renderSegments.Count];
        for (int i = 0; i < _renderSegments.Count; i++)
            _segStartOffsets[i] = _renderSegments[i].Start;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_renderSegments.Count == 0)
            return;

        int lineStart = line.Offset;
        int lineEnd = line.EndOffset;

        // Find the render segment that contains the start of this line
        var segType = ResolveSegmentType(lineStart);
        if (segType == null)
            return;

        // Apply styling for the whole line based on segment type
        ApplyStyle(lineStart, lineEnd, segType);
    }

    /// <summary>
    /// Resolves the segment type string for a given rendered text offset.
    /// Returns null if no mapping exists.
    /// </summary>
    public string? ResolveSegmentType(int renderedOffset)
    {
        // Binary search for the render segment containing this offset
        int segIdx = FindSegmentIndex(renderedOffset);
        if (segIdx < 0)
            return null;

        var renderSeg = _renderSegments[segIdx];

        // Extract lb-ID from the segment key (format: "lb|{n}|{ed}")
        var lbId = ExtractLbId(renderSeg.Key);
        if (lbId == null)
            return null;

        // Look up in the segment map
        if (_segmentMap.ByLbId.TryGetValue(lbId, out var segInfo))
            return segInfo.Type;

        return null;
    }

    /// <summary>
    /// Resolves the full SegmentInfo for a given rendered text offset.
    /// Returns null if no mapping exists.
    /// </summary>
    public SegmentInfo? ResolveSegmentInfo(int renderedOffset)
    {
        int segIdx = FindSegmentIndex(renderedOffset);
        if (segIdx < 0)
            return null;

        var renderSeg = _renderSegments[segIdx];
        var lbId = ExtractLbId(renderSeg.Key);
        if (lbId == null)
            return null;

        _segmentMap.ByLbId.TryGetValue(lbId, out var segInfo);
        return segInfo;
    }

    private int FindSegmentIndex(int offset)
    {
        // Binary search for rightmost segment whose Start <= offset
        int lo = 0, hi = _segStartOffsets.Length - 1;
        int best = -1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (_segStartOffsets[mid] <= offset)
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return best;
    }

    /// <summary>
    /// Extracts the lb n-value from a RenderSegment key.
    /// Key format: "lb|{n}" or "lb|{n}|{ed}".
    /// Returns null for non-lb keys (e.g., "START", "p|...", "pb|...").
    /// </summary>
    internal static string? ExtractLbId(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (!key.StartsWith("lb|", StringComparison.Ordinal))
            return null;

        // Find the n-value between the first and second pipe
        int firstPipe = 2; // index of first '|'
        int secondPipe = key.IndexOf('|', firstPipe + 1);

        if (secondPipe < 0)
        {
            // Format: "lb|{n}" (no ed)
            return key.Substring(firstPipe + 1);
        }

        // Format: "lb|{n}|{ed}"
        return key.Substring(firstPipe + 1, secondPipe - firstPipe - 1);
    }

    private void ApplyStyle(int lineStart, int lineEnd, string segType)
    {
        if (lineStart >= lineEnd)
            return;

        switch (segType)
        {
            case "verse":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(VerseBrush);
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily,
                        FontStyle.Italic,
                        el.TextRunProperties.Typeface.Weight));
                });
                break;

            case "dialogue":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(DialogueBrush);
                });
                break;

            case "commentary":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(CommentaryBrush);
                    // Faint gray wash sets commentary apart from prose without shouting.
                    // Uses the run-level background ceiling of a colorizing transformer.
                    el.TextRunProperties.SetBackgroundBrush(CommentaryBgBrush);
                });
                break;

            case "heading":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily,
                        el.TextRunProperties.Typeface.Style,
                        FontWeight.Bold));
                });
                break;

            case "dharani":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(DharaniBrush);
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily,
                        FontStyle.Italic,
                        el.TextRunProperties.Typeface.Weight));
                });
                break;

            case "preface":
            case "colophon":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(MutedBrush);
                });
                break;

            case "byline":
                ChangeLinePart(lineStart, lineEnd, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(BylineBrush);
                    el.TextRunProperties.SetTypeface(new Typeface(
                        el.TextRunProperties.Typeface.FontFamily,
                        FontStyle.Italic,
                        el.TextRunProperties.Typeface.Weight));
                });
                break;

            // prose, fascicle_marker, table_of_contents, apparatus, unknown
            // → no visual change (default rendering)
        }
    }

    // Static brushes for segment type styling. These are intentionally subtle
    // to complement the existing text without overwhelming it.
    private static readonly IBrush VerseBrush = new SolidColorBrush(Color.FromRgb(218, 165, 32));  // goldenrod
    private static readonly IBrush DialogueBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237)); // cornflower blue
    private static readonly IBrush CommentaryBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160)); // gray
    private static readonly IBrush CommentaryBgBrush = new SolidColorBrush(Color.FromArgb(20, 160, 160, 160)); // faint gray wash
    private static readonly IBrush DharaniBrush = new SolidColorBrush(Color.FromRgb(186, 85, 211)); // medium orchid
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.FromRgb(140, 140, 140)); // dim gray
    private static readonly IBrush BylineBrush = new SolidColorBrush(Color.FromRgb(112, 128, 144)); // slate gray
}
