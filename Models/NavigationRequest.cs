namespace ReadZen.App.Models;

/// <summary>
/// Describes a navigation target: a file location to open in a new reader window and scroll to.
/// </summary>
public sealed class NavigationRequest
{
    /// <summary>File path relative to the text root.</summary>
    public string RelPath { get; set; } = "";

    /// <summary>Which reader pane to navigate in (Original or Translated).</summary>
    public SearchSide Side { get; set; } = SearchSide.Original;

    /// <summary>
    /// Text to locate within the rendered document.
    /// For search hits this is the matched query term.
    /// For TM matches this is the Chinese source text.
    /// The best-scoring occurrence (ranked by left/right context) is highlighted.
    /// </summary>
    public string? MatchText { get; set; }

    /// <summary>Left KWIC context used to disambiguate repeated occurrences.</summary>
    public string? LeftContext { get; set; }

    /// <summary>Right KWIC context used to disambiguate repeated occurrences.</summary>
    public string? RightContext { get; set; }

    /// <summary>
    /// TEI &lt;lb&gt; n-value of the first line in the selection (e.g. "0001a01").
    /// When set, navigation uses segment-key lookup instead of text search.
    /// </summary>
    public string? FromLb { get; set; }

    /// <summary>
    /// TEI &lt;lb&gt; n-value of the last line in the selection (optional).
    /// If null or same as <see cref="FromLb"/>, only one line is targeted.
    /// </summary>
    public string? ToLb { get; set; }

    /// <summary>
    /// Optional source-provided position hint (typically search-hit index in searchable text).
    /// This is treated as a soft preference, not a strict offset.
    /// </summary>
    public int? AnchorStartHint { get; set; }

    /// <summary>
    /// Optional preferred occurrence index (0-based) for repeated identical matches.
    /// TM flow can use block order as a stable tie-break signal when context is missing.
    /// </summary>
    public int? AnchorOccurrenceHint { get; set; }

    /// <summary>
    /// Optional auxiliary text used as a soft ranking signal when multiple candidates tie.
    /// </summary>
    public string? AnchorTextSignal { get; set; }

    /// <summary>
    /// Optional user whose translation is being linked to.
    /// When set, deep links carry per-user context; when null, the link is community/global.
    /// </summary>
    public string? User { get; set; }
}
