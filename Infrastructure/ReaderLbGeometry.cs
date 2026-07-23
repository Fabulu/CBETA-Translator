// Infrastructure/ReaderLbGeometry.cs
//
// Pure line-break (<lb/>) and segment-geometry helpers for the reader surfaces.
// Extracted verbatim from ReadableTabView.axaml.cs (MVVM renovation P7) so the
// lb-range/anchor math can be unit tested and reused off the UI thread.
//
// Like RowGridBuilder, this class is deliberately PURE — no Avalonia, no I/O — it
// operates only on RenderedDocument / RenderSegment values. Callers: the row-grid
// builder path, resume re-anchoring, bookmarks, and lb navigation.

using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Pure helpers that resolve line-break (lb) n-values to rendered-text offsets and
/// extract text/apparatus spanning an lb range. Coordinates are always in rendered
/// text coordinates (the same string the reader editors display).
/// </summary>
public static class ReaderLbGeometry
{
    /// <summary>
    /// Resolves the line-break n-value (and its rendered start offset) for the segment
    /// at or before <paramref name="offset"/>. Returns (null, 0) when no lb segment
    /// covers the offset. Used to re-anchor bookmarks so they survive re-rendering.
    /// </summary>
    public static (string? lb, int segStart) ResolveLbAtOffset(RenderedDocument? doc, int offset)
    {
        if (doc?.Segments == null || doc.Segments.Count == 0)
            return (null, 0);

        string? bestLb = null;
        int bestStart = 0;
        foreach (var seg in doc.Segments)
        {
            if (seg.Start > offset) break; // segments are in ascending Start order
            var lb = LbHelper.ExtractLbNValue(seg.Key);
            if (lb != null)
            {
                bestLb = lb;
                bestStart = seg.Start;
            }
        }
        return (bestLb, bestStart);
    }

    /// <summary>
    /// Resolves an lb-based range to rendered text offsets.
    /// Looks up segments by key "lb|{fromLb}" and optionally "lb|{toLb}".
    /// Returns (start, length) in rendered text coordinates, or (-1, 0) if not found.
    /// </summary>
    public static (int start, int length) ResolveLbRange(
        RenderedDocument doc, string fromLb, string? toLb)
    {
        if (!TryFindSegmentByLb(doc, fromLb, out var startSeg))
            return (-1, 0);

        int rangeStart;
        int rangeEnd;

        if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
        {
            rangeStart = startSeg.Start;
            rangeEnd = startSeg.EndExclusive;
            if (TryFindSegmentByLb(doc, toLb, out var endSeg))
                rangeEnd = endSeg.EndExclusive;
        }
        else
        {
            (rangeStart, rangeEnd) = ResolveSingleLbMeaningfulSpan(doc, startSeg);
        }

        if (rangeEnd <= rangeStart)
            return (-1, 0);

        return (rangeStart, rangeEnd - rangeStart);
    }

    public static int FindSingleLbRangeEnd(RenderedDocument doc, RenderSegment startSeg, int rangeStart)
    {
        int segIndex = doc.Segments.IndexOf(startSeg);
        if (segIndex < 0)
            return Math.Max(rangeStart, doc.Text?.Length ?? rangeStart);

        for (int i = segIndex + 1; i < doc.Segments.Count; i++)
        {
            var seg = doc.Segments[i];
            var lb = LbHelper.ExtractLbNValue(seg.Key);
            if (!string.IsNullOrWhiteSpace(lb) && seg.Start > rangeStart)
                return seg.Start;
        }

        for (int i = segIndex + 1; i < doc.Segments.Count; i++)
        {
            var seg = doc.Segments[i];
            if (seg.EndExclusive > rangeStart)
                return seg.EndExclusive;
        }

        return Math.Max(rangeStart, doc.Text?.Length ?? rangeStart);
    }

    public static (int start, int end) ResolveSingleLbMeaningfulSpan(RenderedDocument doc, RenderSegment startSeg)
    {
        var text = doc.Text ?? string.Empty;
        int segIndex = doc.Segments.IndexOf(startSeg);
        if (segIndex < 0)
            return (-1, 0);

        // Start from the segment's own Start so its rendered text is included.
        // (Anchor exclusion made lb segments span their full text range;
        //  starting at EndExclusive would skip the segment's own content.)
        int cursor = Math.Clamp(startSeg.Start, 0, text.Length);

        for (int i = segIndex + 1; i < doc.Segments.Count; i++)
        {
            var seg = doc.Segments[i];
            var lb = LbHelper.ExtractLbNValue(seg.Key);
            if (string.IsNullOrWhiteSpace(lb) || seg.Start <= cursor)
                continue;

            int start = FindFirstNonWhitespace(text, cursor, seg.Start);
            if (start >= 0 && start < seg.Start)
                return (start, seg.Start);

            cursor = Math.Clamp(Math.Max(cursor, seg.EndExclusive), 0, text.Length);
        }

        int finalStart = FindFirstNonWhitespace(text, cursor, text.Length);
        if (finalStart >= 0 && finalStart < text.Length)
            return (finalStart, text.Length);

        int fallbackStart = Math.Clamp(startSeg.Start, 0, text.Length);
        int fallbackEnd = FindSingleLbRangeEnd(doc, startSeg, fallbackStart);
        return fallbackEnd > fallbackStart ? (fallbackStart, fallbackEnd) : (-1, 0);
    }

    public static int FindFirstNonWhitespace(string text, int start, int endExclusive)
    {
        int safeStart = Math.Clamp(start, 0, text.Length);
        int safeEnd = Math.Clamp(endExclusive, 0, text.Length);
        for (int i = safeStart; i < safeEnd; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Attempts to find a segment by lb n-value, trying both bare key "lb|{nValue}"
    /// and common edition suffixes like "lb|{nValue}|CB".
    /// </summary>
    public static bool TryFindSegmentByLb(
        RenderedDocument doc, string nValue, out RenderSegment seg)
    {
        // Try bare key first
        if (doc.TryGetSegmentByKey("lb|" + nValue, out seg))
            return true;

        // Try with common edition suffixes
        foreach (var suffix in new[] { "CB", "CBETA", "T", "X", "J" })
        {
            if (doc.TryGetSegmentByKey("lb|" + nValue + "|" + suffix, out seg))
                return true;
        }

        // Brute-force: scan segments for any key containing this n-value
        foreach (var s in doc.Segments)
        {
            if (s.Key.StartsWith("lb|", StringComparison.Ordinal))
            {
                var parts = s.Key.Split('|');
                if (parts.Length >= 2 && parts[1] == nValue)
                {
                    seg = s;
                    return true;
                }
            }
        }

        seg = default;
        return false;
    }

    /// <summary>
    /// Extracts rendered text spanning from <paramref name="fromLb"/> to <paramref name="toLb"/> (inclusive).
    /// Returns empty string if the document is empty or the segments cannot be found.
    /// </summary>
    public static string ExtractTextBetweenLbs(RenderedDocument doc, string fromLb, string? toLb)
    {
        if (doc == null || doc.IsEmpty || string.IsNullOrEmpty(fromLb)) return "";

        if (!TryFindSegmentByLb(doc, fromLb, out var startSeg)) return "";
        int start;
        int end;

        if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
        {
            start = startSeg.Start;
            end = startSeg.EndExclusive;
            if (TryFindSegmentByLb(doc, toLb, out var endSeg))
                end = endSeg.EndExclusive;
        }
        else
        {
            (start, end) = ResolveSingleLbMeaningfulSpan(doc, startSeg);
        }

        var text = doc.Text ?? "";
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        return end > start ? text.Substring(start, end - start) : "";
    }

    /// <summary>
    /// Extracts apparatus entries from annotations in the rendered document
    /// that fall within the lb range. Returns null if none found.
    /// Annotation text format: "Lem: X\nRdg: Y [wit]"
    /// </summary>
    public static List<ApparatusEntry>? ExtractApparatusForLbRange(
        RenderedDocument doc, string? fromLb, string? toLb)
    {
        if (doc == null || doc.IsEmpty || string.IsNullOrEmpty(fromLb))
            return null;
        if (doc.Annotations == null || doc.Annotations.Count == 0)
            return null;

        var (rangeStart, rangeLen) = ResolveLbRange(doc, fromLb, toLb);
        if (rangeStart < 0 || rangeLen <= 0)
            return null;

        int rangeEnd = rangeStart + rangeLen;
        List<ApparatusEntry>? result = null;

        foreach (var ann in doc.Annotations)
        {
            if (!string.Equals(ann.Kind, "apparatus", StringComparison.OrdinalIgnoreCase))
                continue;
            // Annotation Start is the anchor position in rendered text;
            // include if it falls within or at the edges of the passage range.
            if (ann.Start < rangeStart || ann.Start > rangeEnd)
                continue;

            var entry = ApparatusAnnotationParser.Parse(ann.Text);
            if (entry != null)
            {
                result ??= new List<ApparatusEntry>();
                result.Add(entry);
            }
        }

        return result;
    }

    /// <summary>True when <paramref name="c"/> is a CJK ideograph.</summary>
    public static bool IsCjkChar(char c) => CjkText.IsIdeograph(c);
}
