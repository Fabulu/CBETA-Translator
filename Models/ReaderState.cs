using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>
/// Reading layout modes for the side-by-side reader.
/// <see cref="Page"/> = per-lb lines (the CBETA "page" layout, one visual line per
/// source &lt;lb/&gt;). <see cref="MergedFlow"/> = text flows within &lt;p&gt;/&lt;lg&gt;
/// boundaries by suppressing non-leading line breaks (the SPA default).
/// </summary>
public enum ReadingLayoutMode
{
    Page = 0,
    MergedFlow = 1
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
    public ReadingLayoutMode LayoutMode { get; set; } = ReadingLayoutMode.Page;

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
