namespace ReadZen.App.Models;

public enum TypeaheadItemKind
{
    SectionHeader,
    Master,
    Title,
    FullTextAction,
    RecentSearch,   // 4C
    CoocTerm,       // 4D
    CountFooter     // 4B (trailing count line, not selectable)
}

public sealed class TypeaheadDisplayItem
{
    public TypeaheadItemKind Kind { get; init; }

    // SectionHeader
    public string? HeaderText { get; init; }

    // Master
    public ZenMasterRecord? Master { get; init; }
    public string DisplayName { get; init; } = "";
    public string Meta { get; init; } = "";

    // Title
    public FileNavItem? FileItem { get; init; }
    public string ZhTitle { get; init; } = "";
    public string EnTitle { get; init; } = "";

    // FullTextAction / RecentSearch / CoocTerm
    public string Query { get; init; } = "";

    // 4A: set on Title items when the title's relPath appears in inverted index results
    public bool InIndex { get; set; }

    // 4B: human-readable count string, e.g. "~42 texts"
    public string? CountLabel { get; set; }
}
