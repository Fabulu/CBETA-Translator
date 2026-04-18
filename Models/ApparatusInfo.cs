// Models/ApparatusInfo.cs
// Critical-apparatus entries from apparatus.json.
// Deserialized with System.Text.Json using JsonPropertyName attributes.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class ApparatusInfo
{
    [JsonPropertyName("entries")]
    public List<ApparatusEntry>? Entries { get; set; }
}

public sealed class ApparatusEntry
{
    [JsonPropertyName("locus_id")]
    public string? LocusId { get; set; }

    [JsonPropertyName("tei_target")]
    public string? TeiTarget { get; set; }

    [JsonPropertyName("section")]
    public string? Section { get; set; }

    [JsonPropertyName("lemma")]
    public string? Lemma { get; set; }

    [JsonPropertyName("readings")]
    public List<ApparatusReading>? Readings { get; set; }

    [JsonPropertyName("witnesses_supporting")]
    public List<string>? WitnessesSupporting { get; set; }

    [JsonPropertyName("decision")]
    public string? Decision { get; set; }

    [JsonPropertyName("decision_basis")]
    public string? DecisionBasis { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class ApparatusReading
{
    [JsonPropertyName("witness_id")]
    public string? WitnessId { get; set; }

    [JsonPropertyName("reading")]
    public string? Reading { get; set; }

    [JsonPropertyName("certainty")]
    public string? Certainty { get; set; }

    [JsonPropertyName("is_ocr_only")]
    public bool? IsOcrOnly { get; set; }

    [JsonPropertyName("is_human_checked")]
    public bool? IsHumanChecked { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } // om, add, transp, conj, subst — free text for now

    [JsonPropertyName("editor")]
    public string? Editor { get; set; } // attribution for conjectural emendations
}
