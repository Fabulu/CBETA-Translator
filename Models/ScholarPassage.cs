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

    // Scholar Tab Phase 4 fields
    public string? Summary { get; set; }
    public string? ReadingStatus { get; set; }
    public int? SortIndex { get; set; }
    public int? Importance { get; set; }
    public string? AnnotationType { get; set; }

    // Critical apparatus entries (null for passages without apparatus data)
    public List<ApparatusEntry>? Apparatus { get; set; }

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
    public bool IsSelectedForCompare { get; set; }

    /// <summary>Auto-generates a summary from passage content for graph labels and list display.</summary>
    public string GenerateAutoSummary()
    {
        // Priority 1: First sentence of English text
        if (!string.IsNullOrWhiteSpace(EnText))
        {
            var en = EnText.Trim();
            // Find first sentence boundary
            var match = System.Text.RegularExpressions.Regex.Match(en, @"^(.+?[.!?])(?:\s|$)");
            var sentence = match.Success ? match.Groups[1].Value.Trim() : en;
            if (sentence.Length > 60)
                sentence = sentence[..57].TrimEnd() + "\u2026";
            if (sentence.Length >= 10)
                return sentence;
        }

        // Priority 2: Master name + first Chinese phrase
        var firstMaster = MasterNames?.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
        if (firstMaster != null && !string.IsNullOrWhiteSpace(ZhText))
        {
            var zh = ZhText.Trim();
            var phrase = zh.Length > 12 ? zh[..12] : zh;
            // Break on CJK punctuation if possible
            var lastBreak = phrase.LastIndexOfAny(new[] { '\u3002', '\uFF0C', '\uFF1B', '\u3001' });
            if (lastBreak > 3) phrase = phrase[..lastBreak];
            return $"{firstMaster}: {phrase}";
        }

        // Priority 3: First 20 chars of Chinese text
        if (!string.IsNullOrWhiteSpace(ZhText))
        {
            var zh = ZhText.Trim();
            return zh.Length > 20 ? zh[..20] + "\u2026" : zh;
        }

        // Priority 4: English snippet
        if (!string.IsNullOrWhiteSpace(EnText))
        {
            var en = EnText.Trim();
            return en.Length > 40 ? en[..37] + "\u2026" : en;
        }

        // Priority 5: File name
        if (!string.IsNullOrWhiteSpace(SourceRelPath))
            return Path.GetFileNameWithoutExtension(SourceRelPath) ?? "(untitled)";

        return "(untitled passage)";
    }
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
    public string? ParentCollectionId { get; set; }
    public List<ScholarPassage> Passages { get; set; } = new();
    public List<PassageLink> Links { get; set; } = new();

    // Research Graph schema v2
    public int SchemaVersion { get; set; } = 1;
    public List<ConceptNode> Concepts { get; set; } = new();
    public List<ScholarGraphEdge> Edges { get; set; } = new();
    public List<EdgeTypeDefinition> CustomEdgeTypes { get; set; } = new();
    public List<CollectionRefNode> CollectionRefs { get; set; } = new();
    public EdgeTypePreferences EdgePreferences { get; set; } = new();

    // Nesting display helpers (not serialized)
    [JsonIgnore]
    public int NestingDepth { get; set; }

    [JsonIgnore]
    public string IndentedName => new string(' ', NestingDepth * 4) + (Name ?? "Untitled");

    public string StudyNotes { get; set; } = "";
    public ScholarGraphLayout GraphLayout { get; set; } = new();
    public Dictionary<string, string> NodeAnnotations { get; set; } = new();
    public HashSet<string> SuppressedAutoNodeIds { get; set; } = new();
    public HashSet<string> SuppressedAutoEdgeIds { get; set; } = new();

    /// <summary>Manually-added master names (not derived from passages).</summary>
    public List<string> ExtraMasters { get; set; } = new();

    /// <summary>Node ID of the starting/root node highlighted with a glow effect.</summary>
    public string? StartingNodeId { get; set; }

    /// <summary>Web link nodes added to the graph.</summary>
    public List<LinkNode> LinkNodes { get; set; } = new();

    /// <summary>
    /// Manually-added dictionary term references, materialized as term nodes in the research graph.
    /// Additive/optional (no schema bump; reading is never version-gated) — old files default to empty.
    /// </summary>
    public List<DictionaryEntryRef> DictionaryEntries { get; set; } = new();
}

/// <summary>
/// A reference to a rich Zen-dictionary entry, materialized as a term node in the research graph.
/// HARD RULE (dictionary-not-shareable): this stores ONLY the reference fields below — never the
/// dictionary body (senses/explanations/occurrences). The full entry is resolved read-only at
/// display time from the local dictionary artifact; nothing here is a share/write/merge path.
/// </summary>
public sealed class DictionaryEntryRef
{
    /// <summary>Deterministic dictionary entry id (DictionaryStore.ComputeId of SourceTerm).</summary>
    public string Id { get; set; } = "";

    /// <summary>Raw CJK headword — never slugified. The graph node id is "term:" + this value.</summary>
    public string SourceTerm { get; set; } = "";

    /// <summary>English gloss snapshot, shown when the live entry cannot be resolved.</summary>
    public string PreferredTarget { get; set; } = "";

    /// <summary>Optional sense key selecting a specific sense of the entry.</summary>
    public string? SenseKey { get; set; }

    /// <summary>Optional master name for a master-specific sense.</summary>
    public string? MasterName { get; set; }
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

