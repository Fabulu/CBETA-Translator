// Infrastructure/RowGridBuilder.cs
//
// Pure builder for the unified row-grid reading surface (DESIGN-rowgrid.md, Wave C).
// Turns a pair of rendered documents (original + translation) into an ordered list of
// per-row view-models the RowGridSurface (a virtualized ListBox) renders.
//
// Like ReadingLayoutSuppressionBuilder, this class is deliberately PURE — no Avalonia,
// no I/O — so it can run off the UI thread and be unit tested. The only dependency is
// CommunityToolkit.Mvvm.ObservableObject, used so the LIVE view-state fields (font size,
// apparatus, nav-highlight, find highlights) that later waves mutate raise change
// notifications. Identity/content fields are init-only and set once by the builder.
//
// C1 SLICE: only ReadingLayoutMode.AlignedLines is implemented (one two-column row per
// <lb/>, ZH from the original, EN from the translation's same-key segment or "" on a
// miss). All other modes throw NotSupportedException until their wave lands.

using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>The visual shape of a row: a two-column ZH|EN row, or a single-column row.</summary>
public enum RowShape
{
    /// <summary>[id][ZH][EN] — aligned bilingual row (AlignedLines/AlignedBlocks).</summary>
    TwoColumn,
    /// <summary>[id][text] — one language per row (Interleaved/MergedStacked).</summary>
    SingleColumn
}

/// <summary>Which language a single-column row carries. Ignored for two-column rows.</summary>
public enum RowSide
{
    Zh,
    En
}

/// <summary>Block-level horizontal alignment of a row's text.</summary>
public enum RowAlign
{
    Left,
    Center
}

/// <summary>
/// A highlight span within one cell's text (find matches / term hits). Local to a single
/// cell string; never crosses a row. C1 leaves these empty (populated by C2).
/// </summary>
public readonly record struct Hspan(int Start, int Length, bool IsCurrent);

/// <summary>
/// One row of the row-grid reading surface. Identity + content are init-only (set once by
/// <see cref="RowGridBuilder"/>); the "live view state" fields are observable so features
/// in later waves can mutate them on realized rows without a rebuild.
/// </summary>
public sealed class RowVm : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    // ---- identity (init-only) ----
    /// <summary>Zero-based position of this row in the model. Scroll/highlight target.</summary>
    public int Index { get; init; }

    /// <summary>The lb n-value that is this row's durable primary key (e.g. "0526c25").</summary>
    public string Lb { get; init; } = "";

    /// <summary>The id-column label (the lb when line ids are shown; otherwise empty).</summary>
    public string IdLabel { get; init; } = "";

    // ---- shape (init-only) ----
    public RowShape Shape { get; init; }

    /// <summary>Which language a single-column row shows. Unused for two-column rows.</summary>
    public RowSide Side { get; init; }

    // ---- content (init-only) ----
    public string ZhText { get; init; } = "";
    public string EnText { get; init; } = "";

    // ---- block styling (init-only; expresses the layout gaps, populated by C5) ----
    public string SegType { get; init; } = "";
    public bool IsHeader { get; init; }
    public RowAlign Align { get; init; }
    public double IndentEm { get; init; }
    public bool LeftBar { get; init; }

    // ---- live view state (mutable + observable; set by features, not the builder) ----
    private double _fontSize = 14.0;
    public double FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }

    private bool _hasApparatus;
    public bool HasApparatus { get => _hasApparatus; set => SetProperty(ref _hasApparatus, value); }

    private bool _isNavHighlighted;
    public bool IsNavHighlighted { get => _isNavHighlighted; set => SetProperty(ref _isNavHighlighted, value); }

    private IReadOnlyList<Hspan> _zhHighlights = Array.Empty<Hspan>();
    public IReadOnlyList<Hspan> ZhHighlights { get => _zhHighlights; set => SetProperty(ref _zhHighlights, value); }

    private IReadOnlyList<Hspan> _enHighlights = Array.Empty<Hspan>();
    public IReadOnlyList<Hspan> EnHighlights { get => _enHighlights; set => SetProperty(ref _enHighlights, value); }
}

/// <summary>
/// The immutable output of <see cref="RowGridBuilder.Build"/>: the ordered rows plus a
/// lb → row-index lookup (first row for each lb) that replaces offset-based addressing on
/// the grid surface.
/// </summary>
public sealed record RowGridModel(
    IReadOnlyList<RowVm> Rows,
    IReadOnlyDictionary<string, int> LbToRow);

/// <summary>
/// Builds a <see cref="RowGridModel"/> from a rendered original/translation pair. Pure and
/// allocation-light. C1 implements only <see cref="ReadingLayoutMode.AlignedLines"/>.
/// </summary>
public static class RowGridBuilder
{
    /// <summary>
    /// Builds the row-grid model for <paramref name="mode"/>. In C1 only AlignedLines is
    /// implemented; every other mode throws <see cref="NotSupportedException"/> (the render
    /// strategy router never sends them to the grid yet). AlignedLines pairs ZH and EN by
    /// shared lb key and needs NO segment map, so <paramref name="segMap"/> may be null.
    /// </summary>
    public static RowGridModel Build(
        RenderedDocument orig,
        RenderedDocument tran,
        SegmentMap? segMap,
        ReadingLayoutMode mode,
        ReaderViewMode view,
        bool showLineIds)
    {
        if (mode != ReadingLayoutMode.AlignedLines)
            throw new NotSupportedException(
                $"RowGridBuilder implements only AlignedLines in C1 (requested {mode}).");

        return BuildAlignedLines(orig, tran, view, showLineIds);
    }

    /// <summary>
    /// AlignedLines: one two-column row per original line-break. ZhText is the line's rendered
    /// text; EnText is the translation's text for the SAME lb n-value, or "" when the
    /// translation has no such line (graceful miss). Both strings are always populated
    /// regardless of <paramref name="view"/> — column hiding per view is a SURFACE concern
    /// (Wave C4), not a builder one.
    ///
    /// CRITICAL: an &lt;lb/&gt; segment is ZERO-LENGTH in the canonical CBETA "lb-before-p"
    /// markup (<c>&lt;lb/&gt;&lt;p&gt;text…</c>) — the line's text lives in the following
    /// <c>p|</c>/<c>l|</c> segment (TeiRenderer emits the lb as a zero-length anchor,
    /// Text/TeiRenderer.cs). So a line's text must span from its lb's Start to the NEXT lb's
    /// Start, crossing intervening non-lb segments (same model as
    /// ReadableTabView.ResolveSingleLbMeaningfulSpan). Slicing the lb segment's own
    /// [Start,EndExclusive) would drop the first line of every paragraph. Pairing is by lb
    /// N-VALUE (ed suffix stripped) so a translation whose lb keys carry a different edition
    /// suffix still matches (mirrors ReadableTabView.TryFindSegmentByLb).
    /// </summary>
    private static RowGridModel BuildAlignedLines(
        RenderedDocument orig,
        RenderedDocument tran,
        ReaderViewMode view,
        bool showLineIds)
    {
        _ = view; // C1: content is view-agnostic; the surface hides the unused column.

        var rows = new List<RowVm>();
        var lbToRow = new Dictionary<string, int>(StringComparer.Ordinal);

        if (orig == null)
            return new RowGridModel(rows, lbToRow);

        // Translation text keyed by lb n-value (ed suffix stripped) → tolerant EN lookup.
        var tranByNValue = new Dictionary<string, string>(StringComparer.Ordinal);
        if (tran != null)
            foreach (var (nv, text) in ExtractLbSpans(tran))
                tranByNValue.TryAdd(nv, text); // first line wins for a duplicate n-value

        foreach (var (lb, zh) in ExtractLbSpans(orig))
        {
            int index = rows.Count;
            rows.Add(new RowVm
            {
                Index = index,
                Lb = lb,
                IdLabel = showLineIds ? lb : "",
                Shape = RowShape.TwoColumn,
                Side = RowSide.Zh,
                ZhText = zh,
                EnText = tranByNValue.TryGetValue(lb, out var en) ? en : "",
            });

            // First row wins for a given lb (durable primary key → scroll target).
            lbToRow.TryAdd(lb, index);
        }

        return new RowGridModel(rows, lbToRow);
    }

    /// <summary>
    /// Yields (lbNValue, lineText) for each line-break in document order. A line's text spans
    /// from its lb segment's Start to the NEXT lb segment's Start (or end of text for the
    /// last), so it captures the paragraph/verse text that lives in the non-lb segments after
    /// a zero-length lb anchor. Surrounding whitespace/line-breaks are trimmed.
    /// </summary>
    private static IEnumerable<(string NValue, string Text)> ExtractLbSpans(RenderedDocument doc)
    {
        var text = doc.Text ?? "";
        var segs = doc.Segments;
        if (segs == null || segs.Count == 0)
            yield break;

        // (start, nValue) for every lb segment, in document order. Use a STABLE order by Start
        // (List.Sort is unstable): two zero-length lbs at the same offset (<lb/><lb/>) must keep
        // document order or a line's text is handed to the wrong n-value (review MINOR-B).
        var lbsRaw = new List<(int Start, string NValue)>();
        foreach (var seg in segs)
        {
            var nv = LbHelper.ExtractLbNValue(seg.Key);
            if (!string.IsNullOrEmpty(nv))
                lbsRaw.Add((seg.Start, nv!));
        }
        var lbs = lbsRaw.OrderBy(x => x.Start).ToList(); // OrderBy is stable

        for (int k = 0; k < lbs.Count; k++)
        {
            int spanStart = lbs[k].Start;
            int spanEnd = (k + 1 < lbs.Count) ? lbs[k + 1].Start : text.Length;
            yield return (lbs[k].NValue, Slice(text, spanStart, spanEnd));
        }
    }

    /// <summary>
    /// Extracts <c>[start, end)</c> from <paramref name="text"/> with clamped bounds and the
    /// surrounding whitespace/line-breaks trimmed so a cell renders cleanly.
    /// </summary>
    private static string Slice(string text, int start, int end)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        if (end <= start)
            return "";
        return text.Substring(start, end - start).Trim();
    }
}
