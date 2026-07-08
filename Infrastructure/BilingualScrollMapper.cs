// Infrastructure/BilingualScrollMapper.cs
using System;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Cross-pane scroll mapping for the bilingual reader (audit P4.3a; design in
/// RUN-20260513-2238/ARCHITECT_SYNTHESIS_v3 "Bilingual scroll sync").
///
/// Both reader panes render segment lists whose keys are shared (lb-based sync keys
/// from TeiRenderer), so a character offset in one pane maps to the other by:
/// find the segment at-or-before the offset → find the SAME KEY in the peer document
/// → interpolate proportionally within the segment. When the exact key is missing in
/// the peer (translation shorter / structure differs), walk back to the nearest
/// preceding source segment whose key the peer knows.
///
/// Pure logic, no Avalonia types — the view supplies "top visible offset" and applies
/// the returned offset (see ReadableTabView's scroll-sync section).
/// </summary>
public static class BilingualScrollMapper
{
    /// <summary>
    /// Maps <paramref name="sourceOffset"/> (a character offset in the source pane's
    /// rendered text) to the equivalent offset in the target pane, or null when no
    /// segment of the source at/before the offset has a counterpart in the target.
    /// </summary>
    public static int? MapOffset(RenderedDocument source, RenderedDocument target, int sourceOffset)
    {
        if (source == null || target == null) return null;
        if (source.IsEmpty || target.IsEmpty) return null;
        var segs = source.Segments;
        if (segs == null || segs.Count == 0) return null;

        sourceOffset = Math.Clamp(sourceOffset, 0, Math.Max(0, source.Text.Length));

        // Binary search: last segment with Start <= sourceOffset (segments are sorted
        // by Start — AnnotationMarkerInserter sorts them before returning).
        int lo = 0, hi = segs.Count - 1, idx = -1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (segs[mid].Start <= sourceOffset) { idx = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        if (idx < 0) idx = 0;

        // Walk back to the nearest source segment whose key exists in the target.
        for (int i = idx; i >= 0; i--)
        {
            var srcSeg = segs[i];
            if (string.IsNullOrEmpty(srcSeg.Key)) continue;
            if (!target.TryGetSegmentByKey(srcSeg.Key, out var tgtSeg)) continue;

            double fraction = 0;
            int srcLen = srcSeg.EndExclusive - srcSeg.Start;
            if (i == idx && srcLen > 0)
                fraction = Math.Clamp((sourceOffset - srcSeg.Start) / (double)srcLen, 0, 1);

            int tgtLen = tgtSeg.EndExclusive - tgtSeg.Start;
            int mapped = tgtSeg.Start + (int)Math.Round(fraction * tgtLen);
            return Math.Clamp(mapped, 0, Math.Max(0, target.Text.Length));
        }

        return null;
    }
}
