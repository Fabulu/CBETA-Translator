// Infrastructure/AnnotationMarkerInserter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

public static class AnnotationMarkerInserter
{
    public enum MarkerKind
    {
        Normal,     // grey
        Yuanwu,      // yellow (inline commentary notes, but NOT CBETA/Taisho apparatus)
        Community,   // blue (type="community")
        Apparatus    // indian red (critical-edition textual variants)
    }

    // Span in FINAL rendered text that maps to an annotation index + kind
    public readonly record struct MarkerSpan(int Start, int EndExclusive, int AnnotationIndex, MarkerKind Kind);

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

            var kind = GetMarkerKind(it.Ann);
            markers.Add(new MarkerSpan(markerStartFinal, markerEndFinal, it.Index, kind));
            inserts.Add(new InsertEvent(insertAt, marker.Length));
        }

        if (srcPos < text.Length)
            sb.Append(text, srcPos, text.Length - srcPos);

        string newText = sb.ToString();

        var shiftedSegs = ShiftSegments(segs, inserts);
        markers.Sort((a, b) => a.Start.CompareTo(b.Start));

        return (newText, shiftedSegs, markers);
    }

    // =========================
    // Marker kind detection (GENERAL + SAFE)
    // =========================

    public static MarkerKind GetMarkerKind(DocAnnotation ann)
    {
        if (ann == null) return MarkerKind.Normal;

        // 0) Apparatus entries (critical-edition textual variants)
        if ((ann.Kind ?? "").Trim().Equals("apparatus", StringComparison.OrdinalIgnoreCase))
            return MarkerKind.Apparatus;

        // 1) Community always wins (your custom notes)
        if (IsCommunity(ann))
            return MarkerKind.Community;

        // 2) Yellow only for inline commentary notes that are NOT editorial apparatus.
        // This is the "Yuanwu-style" behavior you want, but generalized across texts.
        if (IsInlineNote(ann) && !LooksLikeEditorialApparatus(ann))
            return MarkerKind.Yuanwu;

        // 3) Everything else stays grey
        return MarkerKind.Normal;
    }

    // These predicates used to reflect over DocAnnotation probing for members it has
    // never had (Type/Source/Place/IsInline/...) — per annotation, per render, in the
    // hot path (audit P2.6 / R2-L1). DocAnnotation's live signals are exactly Kind and
    // Resp; the rewrites below preserve the reachable behavior of the old probes.

    private static bool IsCommunity(DocAnnotation ann)
        => (ann.Kind ?? "").Trim().Equals("community", StringComparison.OrdinalIgnoreCase);

    private static bool IsInlineNote(DocAnnotation ann)
        => (ann.Kind ?? "").Contains("inline", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeEditorialApparatus(DocAnnotation ann)
    {
        // We want to KEEP these grey even if they're inline:
        // - CBETA / Taisho editorial / apparatus notes
        // - variant/orig/modification markers
        // Conservative: only excludes on strong signals.
        static bool HasToken(string? hay, string token)
            => !string.IsNullOrWhiteSpace(hay) && hay.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        var resp = ann.Resp;
        if (resp != null)
        {
            if (HasToken(resp, "cbeta")) return true;
            if (HasToken(resp, "taisho") || HasToken(resp, "taishō")) return true;
            // Preserved quirk from the reflection version: any other resp containing
            // a 't' vetoed the Kind checks below ("too weak, ignore").
            if (HasToken(resp, "t")) return false;
        }

        var k = (ann.Kind ?? "").Trim();
        if (HasToken(k, "cbeta")) return true;
        if (HasToken(k, "taisho") || HasToken(k, "taishō")) return true;
        if (HasToken(k, "variant") || HasToken(k, "apparatus") || HasToken(k, "orig") || HasToken(k, "mod"))
            return true;

        return false;
    }

    // =========================
    // Segment + map shifting
    // =========================

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

    // NOTE: a 4-arg InsertMarkers overload with a ShiftBaseToXmlIndex helper used to
    // live here. It had no production caller and assumed a char-map
    // (baseMap.Length == baseTextLen) while TeiRenderer produces POSITION maps
    // (length + 1) — if ever wired up it would have silently returned the unshifted
    // map. Deleted per audit P2.6 / R2-L2 rather than left as a trap.

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