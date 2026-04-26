using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class ScholarPassage
{
    public string Id { get; set; } = "";
    public string SourceRelPath { get; set; } = "";
    public string ZhText { get; set; } = "";
    public string EnText { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> MasterNames { get; set; } = new();
    public DateTimeOffset AddedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public int? StartBlockNumber { get; set; }
    public int? EndBlockNumber { get; set; }
    public string? FromLb { get; set; }
    public string? ToLb { get; set; }
    public SearchSide PreferredSide { get; set; } = SearchSide.Original;
    public string? TranslationUser { get; set; }

    // Facet categorization
    public string? DoctrinalTopic { get; set; }
    public string? LiteraryForm { get; set; }
    public string? Lineage { get; set; }
    public string? RhetoricalFunction { get; set; }

    // Linked texts: RelPaths of text files this passage appears in
    public List<string> LinkedTexts { get; set; } = new();

    // Scholar Tab redesign fields (Phase 4)
    public string? Summary { get; set; }
    public string? ReadingStatus { get; set; }
    public int? SortIndex { get; set; }
    public int? Importance { get; set; }
    public string? AnnotationType { get; set; }

    // Display helpers (not serialized)
    [JsonIgnore]
    public string DisplayTitle =>
        !string.IsNullOrWhiteSpace(Summary) ? Summary
        : !string.IsNullOrWhiteSpace(ZhText) ? ZhText
        : !string.IsNullOrWhiteSpace(EnText) ? EnText
        : !string.IsNullOrWhiteSpace(SourceRelPath) ? Path.GetFileNameWithoutExtension(SourceRelPath)
        : "(untitled passage)";

    [JsonIgnore]
    public string TagsSummary => Tags.Count > 0 ? "Tags: " + string.Join(", ", Tags) : "";
    [JsonIgnore]
    public bool HasTags => Tags.Count > 0;
    [JsonIgnore]
    public string MasterNamesSummary => MasterNames.Count > 0 ? "Masters: " + string.Join(", ", MasterNames) : "";
    [JsonIgnore]
    public bool HasMasterNames => MasterNames.Count > 0;
    [JsonIgnore]
    public string LinkedTextsSummary => LinkedTexts.Count > 0 ? "Texts: " + string.Join(", ", LinkedTexts.Select(t => Path.GetFileNameWithoutExtension(t))) : "";
    [JsonIgnore]
    public bool HasLinkedTexts => LinkedTexts.Count > 0;

    [JsonIgnore]
    public string ZhSnippet => ZhText?.Length > 20 ? ZhText[..20] + "\u2026" : ZhText ?? "";

    [JsonIgnore]
    public string AutoSummary =>
        !string.IsNullOrWhiteSpace(Summary) ? Summary
        : (!string.IsNullOrWhiteSpace(ZhText) ? (ZhText.Length > 30 ? ZhText[..30] : ZhText) : "")
          + (!string.IsNullOrWhiteSpace(EnText) ? " \u2014 " + (EnText.Contains('.') ? EnText[..EnText.IndexOf('.')] : EnText.Length > 40 ? EnText[..40] + "\u2026" : EnText) : "");

    [JsonIgnore]
    public bool IsSelectedForCompare { get; set; }
}

public sealed class ScholarCollection
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public List<ScholarPassage> Passages { get; set; } = new();
    public List<PassageLink> Links { get; set; } = new();
    public string StudyNotes { get; set; } = "";
    public ScholarGraphLayout GraphLayout { get; set; } = new();
}

public sealed class ScholarGraphLayout
{
    public Dictionary<string, GraphNodeLayout> NodePositions { get; set; } = new(StringComparer.Ordinal);
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Zoom { get; set; } = 1.0;
}

public sealed class GraphNodeLayout
{
    public double X { get; set; }
    public double Y { get; set; }
}
