// Models/WitnessTextRegistry.cs
// Registry of witness texts for locus-based comparison.
// Loaded from witness-texts.json in the edition package.
// Enables the Witness Comparison surface (separate from critical text and timeline).

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>Top-level witness text registry.</summary>
public sealed class WitnessTextRegistry
{
    [JsonPropertyName("text_id")]
    public string? TextId { get; set; }

    [JsonPropertyName("witnesses")]
    public List<WitnessTextEntry>? Witnesses { get; set; }
}

/// <summary>A single witness's text data for comparison.</summary>
public sealed class WitnessTextEntry
{
    /// <summary>Witness ID matching manifest and timeline (e.g., "ndl-1632", "korea-commons").</summary>
    [JsonPropertyName("witness_id")]
    public string? WitnessId { get; set; }

    /// <summary>Stable siglum (e.g., "T1", "A3").</summary>
    [JsonPropertyName("siglum")]
    public string? Siglum { get; set; }

    /// <summary>Human-readable label.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>Witness family (e.g., "standalone", "shiburoku").</summary>
    [JsonPropertyName("family_id")]
    public string? FamilyId { get; set; }

    /// <summary>
    /// How to access this witness's text. One of:
    /// - "ocr": text from OCR output files
    /// - "transcription": manually transcribed text
    /// - "tei": text extracted from a TEI file
    /// - "image": scan images only (no extracted text yet)
    /// </summary>
    [JsonPropertyName("text_type")]
    public string? TextType { get; set; }

    /// <summary>
    /// Relative path to the witness text file (from the edition package root).
    /// For OCR: path to the OCR output directory or file.
    /// For transcription: path to the transcribed text file.
    /// For TEI: path to a TEI file containing this witness's readings.
    /// Null if text_type is "image" (scan-only, no extracted text).
    /// </summary>
    [JsonPropertyName("text_path")]
    public string? TextPath { get; set; }

    /// <summary>
    /// Locus-level readings for this witness. Key = locus_id, value = reading text.
    /// Populated when locus-level comparison data is available (post-collation).
    /// Null before collation.
    /// </summary>
    [JsonPropertyName("readings")]
    public Dictionary<string, string>? Readings { get; set; }

    /// <summary>Whether this witness has been OCR'd.</summary>
    [JsonPropertyName("has_ocr")]
    public bool HasOcr { get; set; }

    /// <summary>Whether this witness has been human-checked.</summary>
    [JsonPropertyName("has_human_check")]
    public bool HasHumanCheck { get; set; }
}
