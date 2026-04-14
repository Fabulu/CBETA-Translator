// Models/WitnessTextRegistry.cs
// Registry of definitive witness texts for locus-based comparison.
// Loaded from witnesses.json in the edition package.
// Enables the Witness Comparison surface (separate from critical text and timeline).

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>Top-level witness delivery registry.</summary>
public sealed class WitnessTextRegistry
{
    [JsonPropertyName("text_id")]
    public string? TextId { get; set; }

    [JsonPropertyName("witnesses")]
    public List<WitnessTextEntry>? Witnesses { get; set; }
}

/// <summary>A single witness's definitive delivered text data.</summary>
public sealed class WitnessTextEntry
{
    [JsonPropertyName("witness_id")]
    public string? WitnessId { get; set; }

    [JsonPropertyName("siglum")]
    public string? Siglum { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("family_id")]
    public string? FamilyId { get; set; }

    /// <summary>Role in the edition: "base", "primary_collation", "secondary_collation", "context_only".</summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>Relative path to the definitive witness text file.</summary>
    [JsonPropertyName("definitive_text_file")]
    public string? DefinitiveTextFile { get; set; }

    /// <summary>Format of the text file: "plain_text", "json_loci", "tei", "markdown".</summary>
    [JsonPropertyName("text_format")]
    public string? TextFormat { get; set; }

    /// <summary>Status: "raw_ocr", "normalized", "corrected_working", "definitive_witness_text".</summary>
    [JsonPropertyName("text_status")]
    public string? TextStatus { get; set; }

    /// <summary>Completeness: "complete", "partial", "fragment".</summary>
    [JsonPropertyName("completeness")]
    public string? Completeness { get; set; }

    /// <summary>Confidence in the text: "high", "medium", "low".</summary>
    [JsonPropertyName("confidence")]
    public string? Confidence { get; set; }

    /// <summary>Whether a locus map exists for this witness.</summary>
    [JsonPropertyName("has_locus_map")]
    public bool HasLocusMap { get; set; }

    /// <summary>Relative path to the locus map JSON file (locus_id → text).</summary>
    [JsonPropertyName("locus_map_file")]
    public string? LocusMapFile { get; set; }

    /// <summary>Relative path to the witness README / source documentation.</summary>
    [JsonPropertyName("source_readme")]
    public string? SourceReadme { get; set; }

    /// <summary>Whether this witness has been OCR'd.</summary>
    [JsonPropertyName("has_ocr")]
    public bool HasOcr { get; set; }

    /// <summary>Whether this witness has been human-checked.</summary>
    [JsonPropertyName("has_human_check")]
    public bool HasHumanCheck { get; set; }

    /// <summary>
    /// Inline locus-level readings. Key = locus_id, value = reading text.
    /// Used for comparison when a separate locus map file doesn't exist.
    /// </summary>
    [JsonPropertyName("readings")]
    public Dictionary<string, string>? Readings { get; set; }

    /// <summary>Display-friendly status.</summary>
    [JsonIgnore]
    public string StatusDisplay => TextStatus switch
    {
        "raw_ocr" => "Raw OCR",
        "normalized" => "Normalized",
        "corrected_working" => "Working (corrected)",
        "definitive_witness_text" => "Definitive",
        _ => TextStatus ?? "Unknown",
    };
}
