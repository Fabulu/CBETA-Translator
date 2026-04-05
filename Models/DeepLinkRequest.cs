namespace CbetaTranslator.App.Models;

public enum DeepLinkKind { Passage, Dictionary, Scholar, Search, Tags, Termbase, Master }

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
    public bool? SearchOriginal { get; init; }
    public bool? SearchTranslated { get; init; }
    public bool? SearchZenOnly { get; init; }
    public int? SearchStatusIndex { get; init; }
    public string? SearchTagId { get; init; }
    public int? SearchContextIndex { get; init; }
    public string? SearchTranslationSource { get; init; }

    // Tags
    public string? TagsRelPath { get; init; }
    public string? TagsUser { get; init; }
    public string? TagsTagId { get; init; }

    // Termbase
    public string? TermbaseEntry { get; init; }
    public string? TermbaseUser { get; init; }

    // Master
    public string? MasterName { get; init; }
    public string? MasterUser { get; init; }
}
