// Infrastructure/AnnotationMarkerInserter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Infrastructure;

public static class AnnotationMarkerInserter
{
    // Span in FINAL rendered text that maps to an annotation index
    public readonly record struct MarkerSpan(int Start, int EndExclusive, int AnnotationIndex);

    private readonly record struct InsertEvent(int OriginalPos, int InsertedLen);

    /// <summary>
    /// Inserts visible markers (¹²³...) into the text at annotation.Start positions.
    /// Returns: updated text, shifted segments, marker spans.
    ///
    /// IMPORTANT: DocAnnotation is NOT modified (yours is immutable).
    /// MarkerSpan.AnnotationIndex points back into the original annotations list.
    /// </summary>
    public static (string Text, List<RenderSegment> Segments, List<MarkerSpan> Markers)
        InsertMarkers(string text, IReadOnlyList<DocAnnotation> annotations, IReadOnlyList<RenderSegment> segments)
    {
        text ??= "";
        var anns = annotations?.ToList() ?? new List<DocAnnotation>();
        var segs = segments?.ToList() ?? new List<RenderSegment>();

        if (anns.Count == 0 || text.Length == 0)
            return (text, segs, new List<MarkerSpan>());

        // sort annotations by Start (stable), but remember original index
        var items = anns
            .Select((a, idx) => (Ann: a, Index: idx))
            .Select(x =>
            {
                int start = Clamp(x.Ann.Start, 0, text.Length);
                return (x.Ann, x.Index, Start: start);
            })
            .OrderBy(x => x.Start)
            .ThenBy(x => x.Index)
            .ToList();

        var sb = new StringBuilder(text.Length + items.Count * 4);
        var markers = new List<MarkerSpan>(items.Count);
        var inserts = new List<InsertEvent>(items.Count);

        int srcPos = 0;

        foreach (var it in items)
        {
            int insertAt = it.Start;

            // copy original text up to insertion point
            if (insertAt > srcPos)
            {
                sb.Append(text, srcPos, insertAt - srcPos);
                srcPos = insertAt;
            }

            // marker text (1-based human number)
            string marker = ToSuperscriptNumber(it.Index + 1);

            int markerStartFinal = sb.Length;
            sb.Append(marker);
            int markerEndFinal = sb.Length;

            markers.Add(new MarkerSpan(markerStartFinal, markerEndFinal, it.Index));
            inserts.Add(new InsertEvent(insertAt, marker.Length));
        }

        // tail
        if (srcPos < text.Length)
            sb.Append(text, srcPos, text.Length - srcPos);

        string newText = sb.ToString();

        // shift segments by inserted marker lengths (prefix sums)
        var shiftedSegs = ShiftSegments(segs, inserts);

        // markers must be sorted by Start for binary search usage
        markers.Sort((a, b) => a.Start.CompareTo(b.Start));

        return (newText, shiftedSegs, markers);
    }

    private static List<RenderSegment> ShiftSegments(List<RenderSegment> segs, List<InsertEvent> inserts)
    {
        if (segs.Count == 0 || inserts.Count == 0)
            return segs;

        // inserts already in ascending original pos because we built in sorted order,
        // but let's ensure:
        inserts.Sort((a, b) => a.OriginalPos.CompareTo(b.OriginalPos));

        int PrefixInsertedLenAtOrBefore(int pos)
        {
            int sum = 0;
            for (int i = 0; i < inserts.Count; i++)
            {
                if (inserts[i].OriginalPos <= pos) sum += inserts[i].InsertedLen;
                else break;
            }
            return sum;
        }

        var outSegs = new List<RenderSegment>(segs.Count);

        for (int i = 0; i < segs.Count; i++)
        {
            var s = segs[i];

            int startShift = PrefixInsertedLenAtOrBefore(s.Start);
            int endShift = PrefixInsertedLenAtOrBefore(s.EndExclusive);

            outSegs.Add(new RenderSegment(
                s.Key,
                s.Start + startShift,
                s.EndExclusive + endShift));
        }

        // ensure sorted by Start (your selection sync assumes this)
        outSegs.Sort((a, b) => a.Start.CompareTo(b.Start));
        return outSegs;
    }

    // Unicode superscript digits
    private static readonly char[] SupDigits =
    {
        '⁰','¹','²','³','⁴','⁵','⁶','⁷','⁸','⁹'
    };

    private static string ToSuperscriptNumber(int n)
    {
        if (n <= 0) return "⁰";

        var s = n.ToString();
        var sb = new StringBuilder(s.Length);

        foreach (var ch in s)
        {
            if (ch >= '0' && ch <= '9')
                sb.Append(SupDigits[ch - '0']);
            else
                sb.Append(ch);
        }

        return sb.ToString();
    }

    private static int Clamp(int v, int lo, int hi)
        => v < lo ? lo : (v > hi ? hi : v);
}
