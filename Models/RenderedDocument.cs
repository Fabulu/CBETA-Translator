// Models/RenderedDocument.cs
using System;
using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Infrastructure;

namespace CbetaTranslator.App.Models;

public sealed class RenderedDocument
{
    public string Text { get; }
    public List<RenderSegment> Segments { get; }

    public List<DocAnnotation> Annotations { get; }
    public List<AnnotationMarkerInserter.MarkerSpan> AnnotationMarkers { get; }

    /// <summary>
    /// Mapping from BASE rendered text -> absolute XML index in source.
    ///
    /// IMPORTANT:
    /// We support two shapes:
    ///  1) CHAR MAP (legacy): Length == baseText.Length
    ///     BaseToXmlIndex[i] is xml index associated with base character i.
    ///
    ///  2) POSITION MAP (preferred): Length == baseText.Length + 1
    ///     BaseToXmlIndex[p] is xml index to use for INSERTION at base caret position p
    ///     (between characters). This avoids the classic "always one line too high" drift.
    ///
    /// This class can’t know baseText.Length, so it cannot reliably infer which shape it is
    /// from the array alone. Therefore we ALSO store BaseTextLength when a map is provided.
    /// </summary>
    public int[]? BaseToXmlIndex { get; }

    /// <summary>
    /// Length of the BASE text (pre-marker insertion) that BaseToXmlIndex was built for.
    /// If null, we assume legacy behavior.
    /// </summary>
    public int? BaseTextLength { get; }

    private readonly Dictionary<string, RenderSegment> _byKey;

    public bool IsEmpty => string.IsNullOrEmpty(Text) || Segments.Count == 0;

    public static RenderedDocument Empty { get; }
        = new RenderedDocument(
            "",
            new List<RenderSegment>(),
            new List<DocAnnotation>(),
            new List<AnnotationMarkerInserter.MarkerSpan>(),
            baseToXmlIndex: null,
            baseTextLength: null);

    public RenderedDocument(
        string text,
        List<RenderSegment> segments,
        List<DocAnnotation> annotations,
        List<AnnotationMarkerInserter.MarkerSpan> markers,
        int[]? baseToXmlIndex = null,
        int? baseTextLength = null)
    {
        Text = text ?? "";
        Segments = segments ?? new List<RenderSegment>();
        Annotations = annotations ?? new List<DocAnnotation>();
        AnnotationMarkers = markers ?? new List<AnnotationMarkerInserter.MarkerSpan>();

        BaseToXmlIndex = baseToXmlIndex;
        BaseTextLength = baseTextLength;

        // CRITICAL: binary search requires sorted markers
        if (AnnotationMarkers.Count > 1)
            AnnotationMarkers.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        _byKey = Segments
            .GroupBy(s => s.Key, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToDictionary(s => s.Key, s => s, StringComparer.Ordinal);
    }

    public bool TryGetSegmentByKey(string key, out RenderSegment seg)
        => _byKey.TryGetValue(key, out seg);

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

    public bool TryGetAnnotationByMarkerAt(int renderedOffset, out DocAnnotation ann)
    {
        ann = default!;

        if (AnnotationMarkers == null || AnnotationMarkers.Count == 0) return false;
        if (Annotations == null || Annotations.Count == 0) return false;

        // ---- binary search (fast path) ----
        int lo = 0, hi = AnnotationMarkers.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            var m = AnnotationMarkers[mid];

            if (renderedOffset < m.Start) hi = mid - 1;
            else if (renderedOffset >= m.EndExclusive) lo = mid + 1;
            else
            {
                int idx = m.AnnotationIndex;
                if ((uint)idx < (uint)Annotations.Count)
                {
                    ann = Annotations[idx];
                    return true;
                }
                return false;
            }
        }

        // ---- fallback: marker list might be weird (or click lands near marker) ----
        const int radius = 10;
        int a = Math.Max(0, renderedOffset - radius);
        int b = renderedOffset + radius;

        for (int i = 0; i < AnnotationMarkers.Count; i++)
        {
            var m = AnnotationMarkers[i];

            if (m.EndExclusive <= a) continue;
            if (m.Start >= b) break;

            if (renderedOffset >= m.Start && renderedOffset < m.EndExclusive ||
                a >= m.Start && a < m.EndExclusive ||
                b >= m.Start && b < m.EndExclusive)
            {
                int idx = m.AnnotationIndex;
                if ((uint)idx < (uint)Annotations.Count)
                {
                    ann = Annotations[idx];
                    return true;
                }
            }
        }

        return false;
    }

    // Convert display Text index (with inserted superscript markers) -> base index (pre-inserted)
    // Key: if the display index lands INSIDE a marker, do NOT subtract that marker's length too.
    public int DisplayIndexToBaseIndex(int displayIndex)
    {
        if (displayIndex < 0) return -1;

        // No markers => identity mapping
        if (AnnotationMarkers == null || AnnotationMarkers.Count == 0)
            return displayIndex;

        int insertedBefore = 0;

        for (int i = 0; i < AnnotationMarkers.Count; i++)
        {
            var m = AnnotationMarkers[i];
            int len = Math.Max(0, m.EndExclusive - m.Start);

            if (displayIndex < m.Start)
                break;

            if (displayIndex >= m.Start && displayIndex < m.EndExclusive)
            {
                // inside marker => base insertion point
                int baseAtMarker = m.Start - insertedBefore;
                return baseAtMarker < 0 ? 0 : baseAtMarker;
            }

            insertedBefore += len;
        }

        int baseIndex = displayIndex - insertedBefore;
        return baseIndex < 0 ? 0 : baseIndex;
    }

    /// <summary>
    /// Display caret position -> XML insertion index.
    ///
    /// If BaseToXmlIndex is a POSITION MAP (len == baseLen+1), we use it directly.
    /// If it's a CHAR MAP (len == baseLen), we approximate using the nearest char
    /// mapping on the LEFT (baseIdx-1), because baseIdx is a caret position between chars.
    /// That fixes the classic "always one line too high" drift.
    /// </summary>
    public int DisplayIndexToXmlIndex(int displayIndex)
    {
        if (BaseToXmlIndex == null || BaseToXmlIndex.Length == 0)
            return -1;

        int baseIdx = DisplayIndexToBaseIndex(displayIndex);
        if (baseIdx < 0) return -1;

        bool isPosMap = IsPositionMap(BaseToXmlIndex, BaseTextLength);

        if (isPosMap)
        {
            // baseIdx is a caret position; baseIdx can be == baseLen (caret at end)
            if ((uint)baseIdx >= (uint)BaseToXmlIndex.Length)
                baseIdx = Math.Clamp(baseIdx, 0, BaseToXmlIndex.Length - 1);

            return BaseToXmlIndex[baseIdx];
        }

        // CHAR MAP: baseIdx is a caret position; use LEFT char mapping (baseIdx-1)
        int len = BaseToXmlIndex.Length;
        if (len <= 0) return -1;

        int charIdx;
        if (baseIdx <= 0) charIdx = 0;
        else if (baseIdx >= len) charIdx = len - 1;
        else charIdx = baseIdx - 1;

        charIdx = Math.Clamp(charIdx, 0, len - 1);
        return BaseToXmlIndex[charIdx];
    }

    public bool TryFindRenderedOffsetByXmlIndex(int xmlIndex, out int renderedOffset)
    {
        renderedOffset = -1;

        if (xmlIndex < 0) return false;
        if (BaseToXmlIndex == null || BaseToXmlIndex.Length == 0) return false;

        bool isPosMap = IsPositionMap(BaseToXmlIndex, BaseTextLength);

        // We search in the base map (char-map or pos-map). Either way it should be non-decreasing.
        int lo = 0;
        int hi = BaseToXmlIndex.Length - 1;
        int best = -1;

        // Find RIGHTMOST index whose xml position <= xmlIndex
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            int v = BaseToXmlIndex[mid];

            if (v <= xmlIndex)
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (best < 0) best = 0;

        // If POSITION MAP: best is a BASE caret position.
        // If CHAR MAP: best is a base character index; for caret we usually want "after that char" => best+1,
        // but for stability (and to avoid jumping) we keep best. You can flip to best+1 if needed.
        int baseIndex = isPosMap ? best : best;

        renderedOffset = BaseIndexToDisplayIndex(baseIndex, preferAfterMarkerAtSamePos: true);
        return renderedOffset >= 0;
    }

    private static bool IsPositionMap(int[] map, int? baseTextLength)
    {
        if (map == null || map.Length == 0) return false;

        // Reliable check when producer provides baseTextLength
        if (baseTextLength.HasValue)
            return map.Length == baseTextLength.Value + 1;

        // No reliable way without baseTextLength. Default to legacy char-map.
        return false;
    }

    /// <summary>
    /// Convert a BASE text index (pre-marker insertion) to FINAL rendered index (post insertion).
    /// If preferAfterMarkerAtSamePos is true, and there is a marker inserted exactly at baseIndex,
    /// the returned display index will be AFTER the inserted marker (i.e., where the base char moved to).
    /// </summary>
    private int BaseIndexToDisplayIndex(int baseIndex, bool preferAfterMarkerAtSamePos)
    {
        if (baseIndex < 0) return -1;

        if (AnnotationMarkers == null || AnnotationMarkers.Count == 0)
            return baseIndex;

        int insertedBefore = 0;

        for (int i = 0; i < AnnotationMarkers.Count; i++)
        {
            var m = AnnotationMarkers[i];
            int len = Math.Max(0, m.EndExclusive - m.Start);
            if (len == 0) continue;

            // m.Start is in FINAL coords; convert it to base insertion pos:
            int baseAtMarker = m.Start - insertedBefore;

            if (baseAtMarker > baseIndex)
                break;

            if (baseAtMarker == baseIndex && !preferAfterMarkerAtSamePos)
                break;

            insertedBefore += len;
        }

        return baseIndex + insertedBefore;
    }
}