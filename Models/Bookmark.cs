using System;

namespace ReadZen.App.Models;

/// <summary>
/// A user bookmark pointing to a specific offset within a document.
/// </summary>
public sealed class Bookmark
{
    /// <summary>Relative file path within the corpus (e.g. "T/T2076_.xml").</summary>
    public string RelPath { get; set; } = "";

    /// <summary>Character offset in the rendered (display) text at the time the bookmark was created.</summary>
    public int DisplayOffset { get; set; }

    /// <summary>User-supplied label or auto-generated snippet.</summary>
    public string Label { get; set; } = "";

    /// <summary>When the bookmark was created (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The line-break n-value (e.g. "0526c25") the bookmark was anchored to, when known.
    /// Preferred over <see cref="DisplayOffset"/> for navigation because it survives
    /// re-rendering and layout changes (page ↔ merged flow). Null for legacy
    /// offset-only bookmarks created before lb re-anchoring existed.
    /// </summary>
    public string? LbAnchor { get; set; }

    /// <summary>Which pane the anchor was captured from: "orig" or "tran". Null for legacy bookmarks.</summary>
    public string? Side { get; set; }

    /// <summary>
    /// Character offset of the caret within the anchored lb line, so navigation can
    /// refine position beyond the line start. Null for legacy offset-only bookmarks.
    /// </summary>
    public int? IntraLineOffset { get; set; }
}
