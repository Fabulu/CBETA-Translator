// Models/WitnessLocusMap.cs
// Machine-readable witness-to-locus alignment data.
// Loaded from per-witness .loci.json companion files.
// Supports both direct-locus and locus-to-span alignment modes.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>A witness's locus alignment companion file.</summary>
public sealed class WitnessLocusMap
{
    [JsonPropertyName("witness_id")]
    public string? WitnessId { get; set; }

    /// <summary>
    /// Witness-local anchors (for locus_to_span mode).
    /// Key = anchor id (e.g., "T1-p027.l01"), value = anchor data with text.
    /// </summary>
    [JsonPropertyName("anchors")]
    public Dictionary<string, WitnessAnchor>? Anchors { get; set; }

    /// <summary>Locus alignment entries.</summary>
    [JsonPropertyName("loci")]
    public List<WitnessLocusEntry>? Loci { get; set; }
}

/// <summary>A witness-local anchor point (page-line, segment, etc.).</summary>
public sealed class WitnessAnchor
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>A single locus alignment entry.</summary>
public sealed class WitnessLocusEntry
{
    /// <summary>The edition locus ID (shared namespace with critical text and apparatus).</summary>
    [JsonPropertyName("locus_id")]
    public string? LocusId { get; set; }

    /// <summary>
    /// Alignment status: "present", "omitted", "lacuna", "unreadable", "uncertain_span".
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>The witness's reading at this locus (for direct_locus mode).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Start anchor in witness-local space (for locus_to_span mode).</summary>
    [JsonPropertyName("start_anchor")]
    public string? StartAnchor { get; set; }

    /// <summary>End anchor in witness-local space (for locus_to_span mode).</summary>
    [JsonPropertyName("end_anchor")]
    public string? EndAnchor { get; set; }

    /// <summary>Display-friendly status.</summary>
    [JsonIgnore]
    public string StatusDisplay => Status switch
    {
        "present" => "Present",
        "omitted" => "Omitted",
        "lacuna" => "Lacuna (damaged/missing)",
        "unreadable" => "Unreadable",
        "uncertain_span" => "Uncertain span",
        _ => Status ?? "Unknown",
    };
}
