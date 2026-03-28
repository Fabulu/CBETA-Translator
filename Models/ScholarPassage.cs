using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace CbetaTranslator.App.Models;

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

    // Facet categorization
    public string? DoctrinalTopic { get; set; }
    public string? LiteraryForm { get; set; }
    public string? Lineage { get; set; }
    public string? RhetoricalFunction { get; set; }

    // Linked texts: RelPaths of text files this passage appears in
    public List<string> LinkedTexts { get; set; } = new();

    // Display helpers (not serialized)
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
}
