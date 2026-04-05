namespace CbetaTranslator.App.Models;

public enum DeepLinkKind { Passage, Dictionary, Scholar, Search, Tags, Termbase }

public sealed class DeepLinkRequest
{
    public DeepLinkKind Kind { get; init; }

    // Passage (wraps existing NavigationRequest)
    public NavigationRequest? Passage { get; init; }

    // Dictionary
    public string? DictTerm { get; init; }

    // Scholar
    public string? ScholarCollectionId { get; init; }
    public string? ScholarPassageId { get; init; }
    public string? ScholarUser { get; init; }

    // Search
    public string? SearchQuery { get; init; }
    public string? SearchCorpus { get; init; }

    // Tags
    public string? TagsRelPath { get; init; }
    public string? TagsUser { get; init; }
    public string? TagsTagId { get; init; }

    // Termbase
    public string? TermbaseEntry { get; init; }
    public string? TermbaseUser { get; init; }
}
