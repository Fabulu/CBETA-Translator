using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>
/// Reading layout modes for the side-by-side reader.
/// <see cref="Page"/> = per-lb lines (the CBETA "page" layout, one visual line per
/// source &lt;lb/&gt;). <see cref="MergedFlow"/> = text flows within &lt;p&gt;/&lt;lg&gt;
/// boundaries by suppressing non-leading line breaks (the SPA default).
/// <para>
/// WIRE-FROZEN: <see cref="Page"/> = 0 and <see cref="MergedFlow"/> = 1 are persisted
/// as raw integers in reader-state.json. Their numeric values MUST NOT change. New
/// modes are appended (2..6); the 7-item selector is frozen.
/// <para>
/// All seven modes are SHIPPED and render their own layout (no scaffolds). Modes 2-6:
/// <see cref="SyncedPanes"/> renders the per-lb two-editor surface with always-on forced
/// viewport scroll-sync; <see cref="AlignedLines"/>, <see cref="AlignedBlocks"/>,
/// <see cref="Interleaved"/>, and <see cref="MergedStacked"/> render on the combined
/// RowGrid surface (RowGridBuilder). The two segment-map-dependent modes downgrade before
/// render when no .segments.jsonl map is available (<see cref="AlignedBlocks"/> →
/// <see cref="AlignedLines"/>, <see cref="MergedStacked"/> → <see cref="Interleaved"/>);
/// a user-initiated pick persists the downgraded mode, while a sticky reapply renders the
/// downgrade but re-persists the stored preference. Zero-row or exception cases fall back
/// to <see cref="Page"/>.
/// </para>
/// </summary>
public enum ReadingLayoutMode
{
    /// <summary>Per-lb lines (CBETA page layout). Wire value 0 — FROZEN.</summary>
    Page = 0,
    /// <summary>Text flows within &lt;p&gt;/&lt;lg&gt; boundaries (SPA default). Wire value 1 — FROZEN.</summary>
    MergedFlow = 1,
    /// <summary>Per-lb two-editor surface with always-on forced viewport scroll-sync by shared line id.</summary>
    SyncedPanes = 2,
    /// <summary>RowGrid surface: per-lb row alignment of ZH/EN in one combined grid (no segment map needed).</summary>
    AlignedLines = 3,
    /// <summary>RowGrid surface: per-unit block alignment (needs a segment map; downgrades to AlignedLines without one).</summary>
    AlignedBlocks = 4,
    /// <summary>RowGrid surface, single column: ZH row then EN row, interleaved per lb.</summary>
    Interleaved = 5,
    /// <summary>RowGrid surface, single column: merged ZH paragraph then merged EN paragraph per unit (needs a segment map; downgrades to Interleaved without one).</summary>
    MergedStacked = 6
}

/// <summary>
/// Which language pane(s) the reader shows: Chinese only, both, or English only.
/// Bound to the toolbar view selector (0=ZH, 1=Both, 2=EN → ordinal-aligned).
/// </summary>
public enum ReaderViewMode
{
    Zh = 0,
    Both = 1,
    En = 2
}

/// <summary>
/// A per-document resume anchor: the top-visible line-break id at the moment the user
/// last left the file, so the reader can quietly restore the reading position.
/// </summary>
public sealed class ResumeAnchor
{
    /// <summary>The lb n-value of the top-visible line (e.g. "0526c25"). Null when unknown.</summary>
    [JsonPropertyName("lb")]
    public string? Lb { get; set; }

    /// <summary>Which pane the anchor was captured from: "orig" or "tran".</summary>
    [JsonPropertyName("side")]
    public string? Side { get; set; }

    /// <summary>When the anchor was last updated (UTC).</summary>
    [JsonPropertyName("updatedUtc")]
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-document reader state: the persisted reading layout mode and the resume anchor.
/// </summary>
public sealed class ReaderDocumentState
{
    [JsonPropertyName("layoutMode")]
    public ReadingLayoutMode LayoutMode { get; set; } = ReadingLayoutMode.MergedFlow;

    [JsonPropertyName("resume")]
    public ResumeAnchor? Resume { get; set; }
}

/// <summary>
/// Root document of reader-state.json: a map of relative corpus path to that document's
/// persisted reader state. Serialized as a sidecar next to the executable (portable
/// install layout, matching <see cref="Services.BookmarkService"/>).
/// </summary>
public sealed class ReaderState
{
    /// <summary>Per-relPath reader state, keyed by the corpus-relative XML path.</summary>
    [JsonPropertyName("documents")]
    public Dictionary<string, ReaderDocumentState> Documents { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
