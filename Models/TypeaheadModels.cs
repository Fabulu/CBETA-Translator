namespace ReadZen.App.Models;

public enum TypeaheadItemKind { SectionHeader, Master, Title, FullTextAction }

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

    // FullTextAction
    public string Query { get; init; } = "";
}
