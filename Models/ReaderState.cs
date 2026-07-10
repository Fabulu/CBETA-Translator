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
/// modes are appended (2..6); see the SPA-parity blueprint (Wave A) for the roadmap.
/// SyncedPanes/AlignedLines/AlignedBlocks/Interleaved/MergedStacked are scaffolded here
/// and render via a fallback ladder until Waves B/C implement them.
/// </para>
/// </summary>
public enum ReadingLayoutMode
{
    /// <summary>Per-lb lines (CBETA page layout). Wire value 0 — FROZEN.</summary>
    Page = 0,
    /// <summary>Text flows within &lt;p&gt;/&lt;lg&gt; boundaries (SPA default). Wire value 1 — FROZEN.</summary>
    MergedFlow = 1,
    /// <summary>Two-pane, scroll-synced by shared lb anchors (Wave B styling).</summary>
    SyncedPanes = 2,
    /// <summary>Two-pane with per-line alignment (Wave C).</summary>
    AlignedLines = 3,
    /// <summary>Two-pane with per-block alignment (Wave C).</summary>
    AlignedBlocks = 4,
    /// <summary>Single-column, ZH line then EN line, interleaved (Wave C).</summary>
    Interleaved = 5,
    /// <summary>Single-column, merged ZH paragraph then EN paragraph (Wave C).</summary>
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
