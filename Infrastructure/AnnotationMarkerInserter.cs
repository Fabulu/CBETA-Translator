// Infrastructure/AnnotationMarkerInserter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Infrastructure;

public static class AnnotationMarkerInserter
{
    public enum MarkerKind
    {
        Normal,     // grey
        Yuanwu,      // yellow (inline commentary notes, but NOT CBETA/Taisho apparatus)
        Community    // blue (type="community")
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

            var kind = GetMarkerKind(it.Ann);
            markers.Add(new MarkerSpan(markerStartFinal, markerEndFinal, it.Index, kind));
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

    // =========================
    // Marker kind detection (GENERAL + SAFE)
    // =========================

    private static MarkerKind GetMarkerKind(DocAnnotation ann)
    {
        if (ann == null) return MarkerKind.Normal;

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

    private static bool IsCommunity(DocAnnotation ann)
    {
        var k = (ann.Kind ?? "").Trim();
        if (k.Equals("community", StringComparison.OrdinalIgnoreCase))
            return true;

        if (TryGetStringPropOrField(ann, "Type", out var type) &&
            type?.Trim().Equals("community", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (TryGetStringPropOrField(ann, "Source", out var src) &&
            src?.Trim().Equals("community", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }

    private static bool IsInlineNote(DocAnnotation ann)
    {
        // Renderer might store TEI @place="inline" under many names.
        // We check a small set of common ones so this works across your texts.
        static bool EqInline(string? s)
            => !string.IsNullOrWhiteSpace(s) && s.Trim().Equals("inline", StringComparison.OrdinalIgnoreCase);

        if (TryGetStringPropOrField(ann, "Place", out var place) && EqInline(place)) return true;
        if (TryGetStringPropOrField(ann, "NotePlace", out place) && EqInline(place)) return true;
        if (TryGetStringPropOrField(ann, "XmlPlace", out place) && EqInline(place)) return true;
        if (TryGetStringPropOrField(ann, "place", out place) && EqInline(place)) return true;

        // Some renderers encode this into Kind
        var k = (ann.Kind ?? "").Trim();
        if (k.Equals("inline", StringComparison.OrdinalIgnoreCase)) return true;
        if (k.Contains("inline", StringComparison.OrdinalIgnoreCase)) return true;

        // Some renderers use bool flags
        if (TryGetBoolPropOrField(ann, "IsInline", out var b) && b) return true;
        if (TryGetBoolPropOrField(ann, "Inline", out b) && b) return true;

        return false;
    }

    private static bool LooksLikeEditorialApparatus(DocAnnotation ann)
    {
        // We want to KEEP these grey even if they're inline:
        // - CBETA / Taisho editorial / apparatus notes
        // - variant/orig/modification markers
        //
        // This stays conservative: only excludes when we see strong signals.
        bool HasToken(string? hay, string token)
            => !string.IsNullOrWhiteSpace(hay) && hay.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        // resp signals
        if (TryGetStringPropOrField(ann, "Resp", out var resp) ||
            TryGetStringPropOrField(ann, "resp", out resp) ||
            TryGetStringPropOrField(ann, "Responsibility", out resp))
        {
            if (HasToken(resp, "cbeta")) return true;
            if (HasToken(resp, "taisho") || HasToken(resp, "taishō")) return true;
            if (HasToken(resp, "t")) return false; // too weak, ignore
        }

        // type signals
        if (TryGetStringPropOrField(ann, "Type", out var type) ||
            TryGetStringPropOrField(ann, "type", out type))
        {
            // Your community already handled above; this is for editorial types.
            if (HasToken(type, "orig")) return true;
            if (HasToken(type, "mod")) return true;
            if (HasToken(type, "variant")) return true;
            if (HasToken(type, "apparatus")) return true;
            if (HasToken(type, "editor")) return true;
            if (HasToken(type, "corr")) return true;
            if (HasToken(type, "sic")) return true;
        }

        // source signals
        if (TryGetStringPropOrField(ann, "Source", out var src) ||
            TryGetStringPropOrField(ann, "source", out src))
        {
            if (HasToken(src, "cbeta")) return true;
            if (HasToken(src, "taisho") || HasToken(src, "taishō")) return true;
        }

        // kind sometimes carries these tags too
        var k = (ann.Kind ?? "").Trim();
        if (HasToken(k, "cbeta")) return true;
        if (HasToken(k, "taisho") || HasToken(k, "taishō")) return true;
        if (HasToken(k, "variant") || HasToken(k, "apparatus") || HasToken(k, "orig") || HasToken(k, "mod"))
            return true;

        return false;
    }

    private static bool TryGetStringPropOrField(object obj, string name, out string? value)
    {
        value = null;
        try
        {
            var t = obj.GetType();

            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi?.GetValue(obj) is string s)
            {
                value = s;
                return true;
            }

            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi?.GetValue(obj) is string s2)
            {
                value = s2;
                return true;
            }
        }
        catch { }

        return false;
    }

    private static bool TryGetBoolPropOrField(object obj, string name, out bool value)
    {
        value = false;
        try
        {
            var t = obj.GetType();

            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null)
            {
                var v = pi.GetValue(obj);
                if (v is bool b) { value = b; return true; }
            }

            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
            {
                var v = fi.GetValue(obj);
                if (v is bool b) { value = b; return true; }
            }
        }
        catch { }

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