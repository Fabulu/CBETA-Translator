// Models/CommentaryInfo.cs
// Commentary entries from commentary.json — scholarly side-channel parallel
// to apparatus footnotes. Mirrors ApparatusInfo / ApparatusEntry shape.
// Deserialized with System.Text.Json using JsonPropertyName attributes.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class CommentaryInfo
{
    [JsonPropertyName("entries")]
    public List<CommentaryEntry>? Entries { get; set; }
}

public sealed class CommentaryEntry
{
    [JsonPropertyName("commentary_id")]
    public string? CommentaryId { get; set; }            // e.g. "C3"

    [JsonPropertyName("witness_id")]
    public string? WitnessId { get; set; }               // siglum cross-ref

    [JsonPropertyName("language")]
    public string? Language { get; set; }                // BCP-47, e.g. "ja" / "zh-Hant"

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("locus_id")]
    public string? LocusId { get; set; }                 // nullable — entry may be whole-text

    [JsonPropertyName("anchor_text")]
    public string? AnchorText { get; set; }              // optional lemma quote

    [JsonPropertyName("body")]
    public string? Body { get; set; }                    // commentary prose

    [JsonPropertyName("source")]
    public string? Source { get; set; }                  // human-readable source attribution
}
