using System;
using System.Collections.Generic;
using System.Linq;

namespace CbetaTranslator.App.Models;

public sealed class RenderedDocument
{
    public string Text { get; }
    public List<RenderSegment> Segments { get; }

    private readonly Dictionary<string, RenderSegment> _byKey;

    public bool IsEmpty => string.IsNullOrEmpty(Text) || Segments.Count == 0;

    public static RenderedDocument Empty { get; } = new RenderedDocument("", new List<RenderSegment>());

    public RenderedDocument(string text, List<RenderSegment> segments)
    {
        Text = text ?? "";
        Segments = segments ?? new List<RenderSegment>();

        // If keys repeat, keep first occurrence (stable enough for our use case).
        _byKey = Segments
            .GroupBy(s => s.Key, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToDictionary(s => s.Key, s => s, StringComparer.Ordinal);
    }

    public bool TryGetSegmentByKey(string key, out RenderSegment seg)
        => _byKey.TryGetValue(key, out seg);

    /// <summary>
    /// Find the segment whose Start is the nearest Start <= offset.
    /// This is intentionally "at or before" (robust when caret is on whitespace/newlines).
    /// </summary>
    public RenderSegment? FindSegmentAtOrBefore(int renderedOffset)
    {
        if (Segments.Count == 0) return null;

        int lo = 0, hi = Segments.Count - 1;
        int best = -1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            int s = Segments[mid].Start;

            if (s <= renderedOffset)
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return best >= 0 ? Segments[best] : Segments[0];
    }
}
