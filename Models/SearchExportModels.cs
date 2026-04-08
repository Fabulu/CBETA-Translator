using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

public enum SearchExportFormat
{
    Json,
    Html,
    Markdown,
    PlainText,
    Csv,
    Tsv
}

public sealed class SearchExportSnapshot
{
    public string Query { get; init; } = "";
    public bool SearchOriginal { get; init; } = true;
    public bool SearchTranslated { get; init; }
    public bool ZenOnly { get; init; }
    public string StatusFilter { get; init; } = "All";
    public string TagFilter { get; init; } = "All Tags";
    public string ContextLabel { get; init; } = "160 chars";
    public DateTimeOffset ExportedUtc { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<SearchResultGroup> Groups { get; init; } = Array.Empty<SearchResultGroup>();
}
