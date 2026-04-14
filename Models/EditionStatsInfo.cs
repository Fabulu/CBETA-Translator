// Models/EditionStatsInfo.cs
// Compact derived summary from stats.json.
// Deserialized with System.Text.Json using JsonPropertyName attributes.

using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class EditionStatsInfo
{
    [JsonPropertyName("witness_count")]
    public int? WitnessCount { get; set; }

    [JsonPropertyName("witness_family_count")]
    public int? WitnessFamilyCount { get; set; }

    [JsonPropertyName("page_count")]
    public int? PageCount { get; set; }

    [JsonPropertyName("leaf_count")]
    public int? LeafCount { get; set; }

    [JsonPropertyName("ocr_engine_count")]
    public int? OcrEngineCount { get; set; }

    [JsonPropertyName("percent_machine_resolved")]
    public double? PercentMachineResolved { get; set; }

    [JsonPropertyName("percent_human_intervention")]
    public double? PercentHumanIntervention { get; set; }

    [JsonPropertyName("unresolved_count")]
    public int? UnresolvedCount { get; set; }

    [JsonPropertyName("apparatus_entry_count")]
    public int? ApparatusEntryCount { get; set; }

    [JsonPropertyName("base_text_confidence")]
    public BaseTextConfidenceInfo? BaseTextConfidence { get; set; }

    [JsonPropertyName("generated_utc")]
    public string? GeneratedUtc { get; set; }
}

public sealed class BaseTextConfidenceInfo
{
    [JsonPropertyName("high")]
    public int? High { get; set; }

    [JsonPropertyName("medium")]
    public int? Medium { get; set; }

    [JsonPropertyName("low")]
    public int? Low { get; set; }
}
