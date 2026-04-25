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
}
