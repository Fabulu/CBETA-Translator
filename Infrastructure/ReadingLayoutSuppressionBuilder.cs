// Infrastructure/ReadingLayoutSuppressionBuilder.cs
// Pure builder for the suppressedLbNValues set used by merged-flow reading layout.
//
// Merged flow makes text flow within <p>/<lg> boundaries by suppressing the
// newlines of all but the FIRST <lb/> in each semantic segment. The first lb's
// newline becomes the paragraph break between segments.
//
// EXCEPTION (audit R2.2 / "verse"): verse and dharani segments must keep every
// line break, because a poem's line structure IS its content — merging its lines
// into a single flowing paragraph would destroy the reading. For those segment
// types we suppress NOTHING, so all their lbs render on their own lines.
//
// This class is deliberately pure (no AvaloniaEdit, no I/O) so it can be unit
// tested and reused off the UI thread.

using System;
using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Builds the set of lb n-values whose line breaks should be suppressed to produce
/// merged-flow reading layout, skipping verse/dharani segments so their poem line
/// breaks survive.
/// </summary>
public static class ReadingLayoutSuppressionBuilder
{
    /// <summary>
    /// Segment types whose internal line breaks must be preserved (never suppressed).
    /// </summary>
    private static readonly HashSet<string> PreservedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "verse", "dharani" };

    /// <summary>
    /// Builds the suppression set from a loaded segment map. Returns an empty set
    /// when the map is null/empty or has no suppressible line breaks.
    /// </summary>
    public static HashSet<string> Build(SegmentMap? segMap)
        => Build(segMap?.Segments);

    /// <summary>
    /// Builds the suppression set from an ordered segment list. For each segment,
    /// every lb except the first in its range is added to the set — UNLESS the
    /// segment's type is verse or dharani, in which case none of its lbs are added.
    /// </summary>
    public static HashSet<string> Build(IReadOnlyList<SegmentInfo>? segments)
    {
        var suppressed = new HashSet<string>(StringComparer.Ordinal);
        if (segments == null)
            return suppressed;

        foreach (var seg in segments)
        {
            if (seg?.LbRange == null || seg.LbRange.Count <= 1)
                continue;

            // Poem/mantra line structure is content — keep every line break.
            if (seg.Type != null && PreservedTypes.Contains(seg.Type))
                continue;

            for (int i = 1; i < seg.LbRange.Count; i++)
            {
                var lb = seg.LbRange[i];
                if (!string.IsNullOrEmpty(lb))
                    suppressed.Add(lb);
            }
        }

        return suppressed;
    }
}
