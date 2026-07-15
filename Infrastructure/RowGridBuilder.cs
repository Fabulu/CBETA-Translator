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
// Implemented grid modes: ReadingLayoutMode.AlignedLines (one two-column row per <lb/>, ZH
// from the original, EN from the translation's same-key segment or "" on a miss),
// ReadingLayoutMode.AlignedBlocks (two-column like AlignedLines but grouped by segment unit and
// re-aligned at each unit boundary — segment-map-driven), ReadingLayoutMode.Interleaved
// (single-column: each source line's ZH row then its EN row), and
// ReadingLayoutMode.MergedStacked (single-column, segment-map-driven: per unit a healed ZH
// paragraph row then a healed EN — or "(untranslated)" — row). All other modes throw
// NotSupportedException until their wave lands.

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

    /// <summary>
    /// The ZH/Both/EN view filter this row was built under. Only meaningful for two-column
    /// rows (AlignedLines/AlignedBlocks): <see cref="ReaderViewMode.Both"/> shows both columns,
    /// <see cref="ReaderViewMode.Zh"/> collapses to ZH-only, <see cref="ReaderViewMode.En"/>
    /// collapses to EN-only (the primary cell then carries the EN text). Single-column rows
    /// (Interleaved/MergedStacked) IGNORE it — the SPA suppresses the toggle there
    /// (passage.js:499) — so this stays <see cref="ReaderViewMode.Both"/> and the getters below
    /// gate the view logic on <see cref="RowShape.TwoColumn"/> to keep them byte-identical.
    /// </summary>
    public ReaderViewMode View { get; init; } = ReaderViewMode.Both;

    // ---- content (init-only) ----
    public string ZhText { get; init; } = "";
    public string EnText { get; init; } = "";

    // ---- derived shape/content helpers (pure getters; init-only inputs → no notify) ----
    /// <summary>
    /// The text shown in the surface's primary (left) content column. Single-column rows show
    /// the side's own text (a <see cref="RowSide.En"/> row carries its text in
    /// <see cref="EnText"/>). Two-column rows show ZH normally, but under an EN-only
    /// <see cref="View"/> the primary cell carries the EN text instead (the EN column collapses,
    /// so its text must move into the visible cell — never leave a ZH cell showing). Keeps the
    /// default (Both / single-column) rendering byte-identical.
    /// </summary>
    public string PrimaryText => Side == RowSide.En
        ? EnText
        : (Shape == RowShape.TwoColumn && View == ReaderViewMode.En ? EnText : ZhText);

    /// <summary>True only for a two-column row in <see cref="ReaderViewMode.Both"/>: the EN column
    /// is shown alongside the primary ZH column. A ZH-only view hides it, and an EN-only view
    /// folds EN into the primary cell so the second column hides too. Single-column rows always
    /// fold everything into the primary column (no empty gutter).</summary>
    public bool ShowEnColumn => Shape == RowShape.TwoColumn && View == ReaderViewMode.Both;

    /// <summary>Column span of the primary content cell: 1 when the EN column is shown beside it,
    /// otherwise 2 so the single visible cell fills the EN column's slot (no empty gutter). This
    /// covers single-column rows AND view-collapsed two-column rows uniformly.</summary>
    public int PrimaryColumnSpan => ShowEnColumn ? 1 : 2;

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
    /// Builds the row-grid model for <paramref name="mode"/>. Four grid modes are implemented:
    /// <see cref="ReadingLayoutMode.AlignedLines"/> (two-column ZH|EN per lb),
    /// <see cref="ReadingLayoutMode.AlignedBlocks"/> (two-column ZH|EN like AlignedLines, but the
    /// columns are grouped and re-aligned at segment-map unit boundaries so each thought-unit's
    /// ZH and EN start on the same row),
    /// <see cref="ReadingLayoutMode.Interleaved"/> (single-column: each source line's ZH row
    /// then, when it has a non-spacer translation, its EN row), and
    /// <see cref="ReadingLayoutMode.MergedStacked"/> (single-column: per segment unit, a healed
    /// ZH paragraph row then its healed EN paragraph — or an "(untranslated)" placeholder — row).
    /// AlignedLines and Interleaved pair ZH and EN by shared lb key and need NO segment map, so
    /// <paramref name="segMap"/> may be null for them. AlignedBlocks and MergedStacked REQUIRE
    /// <paramref name="segMap"/> to group lines into units (the view downgrades before rendering
    /// when no map exists — MergedStacked→Interleaved, AlignedBlocks→AlignedLines,
    /// FINDINGS-spa-textmodes §5); a null map yields an empty model rather than throwing. Every
    /// other mode throws <see cref="NotSupportedException"/> (the render strategy router never
    /// sends them here).
    /// </summary>
    public static RowGridModel Build(
        RenderedDocument orig,
        RenderedDocument tran,
        SegmentMap? segMap,
        ReadingLayoutMode mode,
        ReaderViewMode view,
        bool showLineIds)
    {
        return mode switch
        {
            ReadingLayoutMode.AlignedLines => BuildAlignedLines(orig, tran, view, showLineIds),
            ReadingLayoutMode.AlignedBlocks => BuildAlignedBlocks(orig, tran, segMap, view, showLineIds),
            ReadingLayoutMode.Interleaved => BuildInterleaved(orig, tran, view, showLineIds),
            ReadingLayoutMode.MergedStacked => BuildMergedStacked(orig, tran, segMap, showLineIds),
            _ => throw new NotSupportedException(
                $"RowGridBuilder implements only AlignedLines, AlignedBlocks, Interleaved and MergedStacked (requested {mode})."),
        };
    }

    /// <summary>
    /// AlignedLines: one two-column row per original line-break. ZhText is the line's rendered
    /// text; EnText is the translation's text for the SAME lb n-value, or "" when the
    /// translation has no such line (graceful miss). Both strings are always populated; the
    /// per-row <paramref name="view"/> is STAMPED onto each RowVm so its derived getters
    /// (ShowEnColumn/PrimaryText/PrimaryColumnSpan) collapse a column when the user picks ZH-only
    /// or EN-only — the surface just follows the getters. <see cref="ReaderViewMode.Both"/> is
    /// byte-identical to the pre-view-filter output.
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
                View = view,   // ZH/Both/EN column collapse is expressed via the row's getters
                ZhText = zh,
                EnText = tranByNValue.TryGetValue(lb, out var en) ? en : "",
            });

            // First row wins for a given lb (durable primary key → scroll target).
            lbToRow.TryAdd(lb, index);
        }

        return new RowGridModel(rows, lbToRow);
    }

    /// <summary>
    /// Interleaved (SPA parity, passage.js buildBodiesHtml §interleaved): SINGLE-COLUMN. For
    /// each SOURCE line emit a ZH row, then — IF that line has a non-empty paired translation
    /// AND its id is not a <c>__</c> spacer — emit its EN row immediately after. Translation is
    /// paired by shared lb n-value (the desktop's <c>pairTranslation</c>): an empty EN text
    /// skips only the EN row, never the ZH row. Everything goes in ONE column; there is NO
    /// second pane, and source lines are NEVER placed into an English column. The EN rows come
    /// solely from translation data. Needs no segment map (line-by-line).
    ///
    /// <paramref name="view"/> is intentionally ignored: single-column modes suppress the
    /// ZH/Both/EN toggle in the SPA (passage.js:499); the combined stream is the whole surface.
    /// </summary>
    private static RowGridModel BuildInterleaved(
        RenderedDocument orig,
        RenderedDocument tran,
        ReaderViewMode view,
        bool showLineIds)
    {
        _ = view; // single-column: the view toggle is suppressed (SPA passage.js:499).

        var rows = new List<RowVm>();
        var lbToRow = new Dictionary<string, int>(StringComparer.Ordinal);

        if (orig == null)
            return new RowGridModel(rows, lbToRow);

        // Translation text keyed by lb n-value (ed suffix stripped) → tolerant EN lookup,
        // identical plumbing to BuildAlignedLines.
        var tranByNValue = new Dictionary<string, string>(StringComparer.Ordinal);
        if (tran != null)
            foreach (var (nv, text) in ExtractLbSpans(tran))
                tranByNValue.TryAdd(nv, text); // first line wins for a duplicate n-value

        foreach (var (lb, zh) in ExtractLbSpans(orig))
        {
            // ZH row — always emitted (mirrors the SPA loop rendering every source line).
            int zhIndex = rows.Count;
            rows.Add(new RowVm
            {
                Index = zhIndex,
                Lb = lb,
                IdLabel = showLineIds ? lb : "",
                Shape = RowShape.SingleColumn,
                Side = RowSide.Zh,
                ZhText = zh,
            });

            // First row wins for a given lb (the ZH row is the durable scroll target).
            lbToRow.TryAdd(lb, zhIndex);

            // EN row — only when the paired translation is non-empty AND the id is not a
            // spacer (SPA: `if trn[i].text && not __ spacer`). ExtractLbSpans trims, so an
            // empty/whitespace translation collapses to "" and skips only the EN row.
            bool isSpacer = lb.StartsWith("__", StringComparison.Ordinal);
            if (!isSpacer
                && tranByNValue.TryGetValue(lb, out var en)
                && !string.IsNullOrEmpty(en))
            {
                int enIndex = rows.Count;
                rows.Add(new RowVm
                {
                    Index = enIndex,
                    Lb = lb,
                    IdLabel = "",            // continuation of the ZH row above; no id echo
                    Shape = RowShape.SingleColumn,
                    Side = RowSide.En,
                    EnText = en,
                });
            }
        }

        return new RowGridModel(rows, lbToRow);
    }

    /// <summary>Segment types that break OUT of the running paragraph as their own standalone
    /// block (renderMergedHtml headingIds/bylineIds, format.js:260-268). Matched
    /// case-insensitively but culture-invariantly (data types are lowercase, e.g. "heading").</summary>
    private static bool IsHeadingType(string? type)
        => string.Equals(type, "heading", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "byline", StringComparison.OrdinalIgnoreCase);

    /// <summary>A run of consecutive source lines sharing one segment unit (or an unmapped
    /// solo run). Mirrors the group accumulator in renderMergedHtml (format.js:253-279).</summary>
    private sealed class MergedGroup
    {
        public string Key = "";
        public string Type = "";
        public readonly List<(string Lb, string Zh)> Lines = new();
    }

    /// <summary>
    /// MergedStacked (SPA parity, renderMergedHtml format.js:245-329 with <c>{stacked:true}</c>):
    /// SINGLE-COLUMN, segment-map-driven. Group CONSECUTIVE source lines by their segment
    /// <c>unitId</c>; for each unit emit the healed ZH paragraph row (the unit's line texts joined
    /// — Chinese has no inter-line spaces, so SPA <c>join('')</c>), then the healed EN paragraph
    /// row (paired translations joined with a space, SPA <c>join(' ')</c>) — or an
    /// "(untranslated)" placeholder row when the unit has NO translation (never a blank). A
    /// heading/byline unit breaks OUT as its own standalone block: one header row, no EN
    /// companion. Unmapped lines heal into the running group (SPA <c>cur.key</c> fallback);
    /// consecutive lines with a fresh <c>unitId</c> start a new group. Everything is in ONE
    /// column (RowShape.SingleColumn); there is NO second pane. Every lb in a unit resolves to
    /// that unit's ZH paragraph row so scroll-by-lb still lands.
    ///
    /// REQUIRES <paramref name="segMap"/>: the caller (ReadableTabView.ApplyRowGridLayoutAsync)
    /// loads it and, when absent, downgrades to Interleaved BEFORE rendering (Interleaved needs
    /// no map). A null/empty map here therefore yields an EMPTY model — the view's zero-row guard
    /// then falls back — rather than throwing.
    /// </summary>
    private static RowGridModel BuildMergedStacked(
        RenderedDocument orig,
        RenderedDocument tran,
        SegmentMap? segMap,
        bool showLineIds)
    {
        var rows = new List<RowVm>();
        var lbToRow = new Dictionary<string, int>(StringComparer.Ordinal);

        if (orig == null || segMap == null)
            return new RowGridModel(rows, lbToRow);

        var byLbId = segMap.ByLbId;

        // Translation text keyed by lb n-value (ed suffix stripped) → tolerant EN lookup, the
        // same plumbing the other builders use.
        var tranByNValue = new Dictionary<string, string>(StringComparer.Ordinal);
        if (tran != null)
            foreach (var (nv, text) in ExtractLbSpans(tran))
                tranByNValue.TryAdd(nv, text); // first line wins for a duplicate n-value

        // Group consecutive source lines by segment unitId (format.js:253-279).
        var groups = new List<MergedGroup>();
        MergedGroup? cur = null;
        int lineIndex = -1;
        foreach (var (lb, zh) in ExtractLbSpans(orig))
        {
            lineIndex++;

            // Layout-only spacer anchors never join a paragraph (SPA skips __lg_break_/__pb_break_).
            if (lb.StartsWith("__", StringComparison.Ordinal))
                continue;

            byLbId.TryGetValue(lb, out var seg);
            string? unitId = seg?.UnitId;
            // A mapped line uses its unitId; an unmapped line heals into the running group, or
            // starts a solo group when none is open (SPA: seg.unitId || (cur ? cur.key : 'solo:'+i)).
            string key = !string.IsNullOrEmpty(unitId)
                ? unitId!
                : (cur != null ? cur.Key : "solo:" + lineIndex);

            if (cur == null || !string.Equals(key, cur.Key, StringComparison.Ordinal))
            {
                cur = new MergedGroup { Key = key, Type = seg?.Type ?? "" };
                groups.Add(cur);
            }
            else if (string.IsNullOrEmpty(cur.Type) && !string.IsNullOrEmpty(seg?.Type))
            {
                cur.Type = seg!.Type!; // first typed line in the run names the segment (SPA:275-277)
            }

            cur.Lines.Add((lb, zh));
        }

        foreach (var g in groups)
        {
            if (g.Lines.Count == 0)
                continue;

            string firstLb = g.Lines[0].Lb;
            bool isHeader = IsHeadingType(g.Type);

            // Heal the ZH paragraph: join the unit's (already-trimmed) line texts with no
            // separator, closing the ~17-char woodblock cuts (SPA zhSpans.join('')).
            string zhPara = string.Concat(g.Lines.Select(l => l.Zh));

            int zhRow = rows.Count;
            rows.Add(new RowVm
            {
                Index = zhRow,
                Lb = firstLb,
                IdLabel = showLineIds ? firstLb : "",
                Shape = RowShape.SingleColumn,
                Side = RowSide.Zh,
                ZhText = zhPara,
                SegType = g.Type,
                IsHeader = isHeader,
            });

            // Every lb in the unit scrolls to the unit's ZH paragraph row (first row wins).
            foreach (var (lb, _) in g.Lines)
                lbToRow.TryAdd(lb, zhRow);

            // Heading/byline breaks out as its own block — no EN companion row.
            if (isHeader)
                continue;

            // Heal the EN paragraph from the paired translations (space-joined; SPA enSpans
            // .filter(Boolean).join(' ')). No translation for any line → "(untranslated)".
            var enParts = g.Lines
                .Select(l => tranByNValue.TryGetValue(l.Lb, out var t) ? t : "")
                .Where(t => !string.IsNullOrEmpty(t));
            string enPara = string.Join(" ", enParts);

            int enRow = rows.Count;
            rows.Add(new RowVm
            {
                Index = enRow,
                Lb = firstLb,
                IdLabel = "",            // continuation of the ZH row above; no id echo
                Shape = RowShape.SingleColumn,
                Side = RowSide.En,
                EnText = string.IsNullOrEmpty(enPara) ? "(untranslated)" : enPara,
                SegType = g.Type,
            });
        }

        return new RowGridModel(rows, lbToRow);
    }

    /// <summary>
    /// AlignedBlocks (SPA parity, `blocks` bilingual mode — FINDINGS-spa-textmodes §1/§3): a
    /// TWO-COLUMN ZH|EN surface like <see cref="BuildAlignedLines"/>, but the columns are grouped
    /// by segment <c>unitId</c> and RE-ALIGNED at each unit boundary. In the SPA this is two
    /// independently-scrolling panes that <c>syncSegmentBlocks</c> pads to the same baseline at
    /// every segment block; the desktop's single row-grid can't pixel-sync two panes, so the
    /// block alignment is expressed STRUCTURALLY: within a unit the ZH lines and the unit's
    /// (non-empty) paired EN lines are laid out INDEPENDENTLY and zipped row-by-row, and each new
    /// unit RESETS both columns to a shared row baseline. EN for a unit is the paired translations
    /// of that unit's source lbs (shared-id pairing, empties dropped), so an untranslated line in
    /// the MIDDLE of a unit lets the following EN reflow up and the unit's trailing EN cell pads
    /// blank — never bleeding the next unit's EN onto this unit's ZH. EN can never be LONGER than
    /// ZH within a unit (it is derived from the unit's own source lbs), so the padded (blank) cell
    /// is always on the EN side; every emitted row therefore carries a real source lb.
    ///
    /// REQUIRES <paramref name="segMap"/>: the caller (ReadableTabView.ApplyRowGridLayoutAsync)
    /// loads it and, when absent, downgrades to <see cref="ReadingLayoutMode.AlignedLines"/> — the
    /// map-free two-column analog — BEFORE rendering. A null map here therefore yields an EMPTY
    /// model (the view's zero-row guard then falls back) rather than throwing. Grouping mirrors
    /// <see cref="BuildMergedStacked"/> (consecutive lines by unitId, unmapped lines heal into the
    /// running group, "__" spacer anchors skipped).
    /// </summary>
    private static RowGridModel BuildAlignedBlocks(
        RenderedDocument orig,
        RenderedDocument tran,
        SegmentMap? segMap,
        ReaderViewMode view,
        bool showLineIds)
    {
        var rows = new List<RowVm>();
        var lbToRow = new Dictionary<string, int>(StringComparer.Ordinal);

        if (orig == null || segMap == null)
            return new RowGridModel(rows, lbToRow);

        var byLbId = segMap.ByLbId;

        // Translation text keyed by lb n-value (ed suffix stripped) → tolerant EN lookup, the
        // same plumbing the other builders use.
        var tranByNValue = new Dictionary<string, string>(StringComparer.Ordinal);
        if (tran != null)
            foreach (var (nv, text) in ExtractLbSpans(tran))
                tranByNValue.TryAdd(nv, text); // first line wins for a duplicate n-value

        // Group consecutive source lines by segment unitId — identical accumulator to
        // BuildMergedStacked (format.js:253-279), kept local so MergedStacked stays byte-identical.
        var groups = new List<MergedGroup>();
        MergedGroup? cur = null;
        int lineIndex = -1;
        foreach (var (lb, zh) in ExtractLbSpans(orig))
        {
            lineIndex++;

            // Layout-only spacer anchors never join a paragraph (SPA skips __lg_break_/__pb_break_).
            if (lb.StartsWith("__", StringComparison.Ordinal))
                continue;

            byLbId.TryGetValue(lb, out var seg);
            string? unitId = seg?.UnitId;
            string key = !string.IsNullOrEmpty(unitId)
                ? unitId!
                : (cur != null ? cur.Key : "solo:" + lineIndex);

            if (cur == null || !string.Equals(key, cur.Key, StringComparison.Ordinal))
            {
                cur = new MergedGroup { Key = key, Type = seg?.Type ?? "" };
                groups.Add(cur);
            }
            else if (string.IsNullOrEmpty(cur.Type) && !string.IsNullOrEmpty(seg?.Type))
            {
                cur.Type = seg!.Type!;
            }

            cur.Lines.Add((lb, zh));
        }

        foreach (var g in groups)
        {
            if (g.Lines.Count == 0)
                continue;

            bool isHeader = IsHeadingType(g.Type);

            // ZH column = the unit's source lines, in order. EN column = the unit's paired
            // translations with empties dropped (independent length ≤ the ZH count). Zipping to
            // the ZH count re-aligns both columns at the unit start: the k-th EN sits on the k-th
            // ZH row, and any shortfall pads the unit's trailing EN cells blank.
            var zhLines = g.Lines;
            var enLines = g.Lines
                .Select(l => tranByNValue.TryGetValue(l.Lb, out var t) ? t : "")
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            for (int k = 0; k < zhLines.Count; k++)
            {
                var (lb, zh) = zhLines[k];
                string en = k < enLines.Count ? enLines[k] : "";

                int index = rows.Count;
                rows.Add(new RowVm
                {
                    Index = index,
                    Lb = lb,
                    IdLabel = showLineIds ? lb : "",
                    Shape = RowShape.TwoColumn,
                    Side = RowSide.Zh,
                    View = view,   // ZH/Both/EN column collapse is expressed via the row's getters
                    ZhText = zh,
                    EnText = en,
                    SegType = g.Type,
                    IsHeader = isHeader,
                });

                // First row wins for a given lb (durable primary key → scroll target).
                lbToRow.TryAdd(lb, index);
            }
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
