using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Infrastructure;

public static class LbHelper
{
    /// <summary>
    /// Extracts the n-value from a segment key like "lb|0292a27|T" -> "0292a27".
    /// Returns null if the key is null or not an lb-type key.
    /// </summary>
    public static string? ExtractLbNValue(string? segmentKey)
    {
        if (string.IsNullOrEmpty(segmentKey)) return null;
        if (!segmentKey.StartsWith("lb|", System.StringComparison.Ordinal)) return null;
        var parts = segmentKey.Split('|');
        return parts.Length >= 2 ? parts[1] : null;
    }

    /// <summary>
    /// Finds the nearest lb segment at or before the given rendered text offset.
    /// Scans backwards if the segment at the offset isn't lb-type.
    /// </summary>
    public static string? FindNearestLbNValue(RenderedDocument doc, int renderedOffset)
    {
        if (doc == null || doc.IsEmpty) return null;

        // First try the segment directly at/before the offset
        var seg = doc.FindSegmentAtOrBefore(renderedOffset);
        if (seg == null) return null;

        var nValue = ExtractLbNValue(seg.Value.Key);
        if (nValue != null) return nValue;

        // Scan backwards through segments to find nearest lb
        int segIdx = doc.Segments.IndexOf(seg.Value);
        for (int i = segIdx - 1; i >= 0; i--)
        {
            nValue = ExtractLbNValue(doc.Segments[i].Key);
            if (nValue != null) return nValue;
        }

        return null;
    }
}
