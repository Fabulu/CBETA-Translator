namespace CbetaTranslator.App.Models;

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
}
