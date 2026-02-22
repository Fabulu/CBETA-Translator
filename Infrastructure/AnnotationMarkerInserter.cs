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
        var (t, s, m, _) = InsertMarkers(text, annotations, segments, baseToXmlIndexBase: null);
        return (t, s, m);
    }

    /// <summary>
    /// Same as InsertMarkers, but also shifts the base-to-xml index map so it matches FINAL text offsets.
    /// This prevents click/caret -> xmlIndex drift when markers are inserted into the visible text.
    /// </summary>
    public static (string Text, List<RenderSegment> Segments, List<MarkerSpan> Markers, int[]? BaseToXmlIndexFinal)
        InsertMarkers(
            string text,
            IReadOnlyList<DocAnnotation> annotations,
            IReadOnlyList<RenderSegment> segments,
            int[]? baseToXmlIndexBase)
    {
        text ??= "";
        var anns = annotations?.ToList() ?? new List<DocAnnotation>();
        var segs = segments?.ToList() ?? new List<RenderSegment>();

        if (anns.Count == 0 || text.Length == 0)
            return (text, segs, new List<MarkerSpan>(), baseToXmlIndexBase);

        // Sort by Start in BASE text coords; stable by original index
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

        for (int k = 0; k < items.Count; k++)
        {
            var it = items[k];
            int insertAt = it.Start;

            if (insertAt < srcPos)
                insertAt = srcPos; // safety

            if (insertAt > srcPos)
            {
                sb.Append(text, srcPos, insertAt - srcPos);
                srcPos = insertAt;
            }

            string marker = ToSuperscriptNumber(it.Index + 1);

            int markerStartFinal = sb.Length;
            sb.Append(marker);
            int markerEndFinal = sb.Length;

            markers.Add(new MarkerSpan(markerStartFinal, markerEndFinal, it.Index));
            inserts.Add(new InsertEvent(insertAt, marker.Length));
        }

        if (srcPos < text.Length)
            sb.Append(text, srcPos, text.Length - srcPos);

        string newText = sb.ToString();

        var shiftedSegs = ShiftSegments(segs, inserts);
        markers.Sort((a, b) => a.Start.CompareTo(b.Start));

        int[]? baseToXmlFinal = ShiftBaseToXmlIndex(baseToXmlIndexBase, text.Length, newText.Length, inserts);

        return (newText, shiftedSegs, markers, baseToXmlFinal);
    }

    private static List<RenderSegment> ShiftSegments(List<RenderSegment> segs, List<InsertEvent> inserts)
    {
        if (segs.Count == 0 || inserts.Count == 0)
            return segs;

        // Ensure inserts are in ascending OriginalPos
        inserts.Sort((a, b) => a.OriginalPos.CompareTo(b.OriginalPos));

        // Build prefix sums of inserted lengths
        // prefix[i] = total inserted length for inserts[0..i] inclusive
        var prefix = new int[inserts.Count];
        int running = 0;
        for (int i = 0; i < inserts.Count; i++)
        {
            running += Math.Max(0, inserts[i].InsertedLen);
            prefix[i] = running;
        }

        int InsertedLenAtOrBefore(int pos)
        {
            // rightmost insert with OriginalPos <= pos
            int lo = 0, hi = inserts.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) / 2);
                if (inserts[mid].OriginalPos <= pos)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }
            return best >= 0 ? prefix[best] : 0;
        }

        var outSegs = new List<RenderSegment>(segs.Count);

        for (int i = 0; i < segs.Count; i++)
        {
            var s = segs[i];

            // IMPORTANT:
            // - Inserts at OriginalPos == Start shift Start (marker appears before the char at Start).
            // - Inserts at OriginalPos == EndExclusive shift EndExclusive (exclusive boundary moves right).
            int startShift = InsertedLenAtOrBefore(s.Start);
            int endShift = InsertedLenAtOrBefore(s.EndExclusive);

            outSegs.Add(new RenderSegment(
                s.Key,
                s.Start + startShift,
                s.EndExclusive + endShift));
        }

        outSegs.Sort((a, b) => a.Start.CompareTo(b.Start));
        return outSegs;
    }

    private static int[]? ShiftBaseToXmlIndex(int[]? baseMap, int baseTextLen, int finalTextLen, List<InsertEvent> inserts)
    {
        if (baseMap == null)
            return null;

        if (baseMap.Length != baseTextLen)
        {
            // Defensive: if mismatch, don't attempt to "fix" it into something wrong.
            return baseMap;
        }

        if (inserts.Count == 0 || baseTextLen == 0)
            return baseMap;

        // Ensure inserts are in ascending OriginalPos
        inserts.Sort((a, b) => a.OriginalPos.CompareTo(b.OriginalPos));

        var finalMap = new int[finalTextLen];

        int basePos = 0;
        int finalPos = 0;
        int insIdx = 0;

        while (basePos < baseTextLen && finalPos < finalTextLen)
        {
            // Insert marker(s) at this basePos
            while (insIdx < inserts.Count && inserts[insIdx].OriginalPos == basePos)
            {
                int insertedLen = Math.Max(0, inserts[insIdx].InsertedLen);

                // For inserted chars, map to a nearby sensible xml index:
                // Prefer the xml index at the insertion point; else fall back to previous.
                int xmlIdx =
                    (basePos < baseMap.Length) ? baseMap[basePos]
                    : (basePos > 0 ? baseMap[basePos - 1] : 0);

                for (int k = 0; k < insertedLen && finalPos < finalMap.Length; k++)
                    finalMap[finalPos++] = xmlIdx;

                insIdx++;
            }

            // Copy one real base character mapping
            finalMap[finalPos++] = baseMap[basePos++];
        }

        // Remaining inserts at end (if any)
        while (insIdx < inserts.Count && finalPos < finalMap.Length)
        {
            int insertedLen = Math.Max(0, inserts[insIdx].InsertedLen);
            int xmlIdx = baseTextLen > 0 ? baseMap[baseTextLen - 1] : 0;

            for (int k = 0; k < insertedLen && finalPos < finalMap.Length; k++)
                finalMap[finalPos++] = xmlIdx;

            insIdx++;
        }

        // Pad if needed (shouldn't happen, but safe)
        int pad = (finalPos > 0) ? finalMap[finalPos - 1] : 0;
        while (finalPos < finalMap.Length)
            finalMap[finalPos++] = pad;

        return finalMap;
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