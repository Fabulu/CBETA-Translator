// Models/SegmentInfo.cs
// Represents a single semantic segment from a .segments.jsonl file.
// Each line in the JSONL maps an lb-range to a segment type (verse,
// dialogue, commentary, etc.) with optional sub-type and speaker.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>
/// A single segment entry deserialized from a .segments.jsonl line.
/// Properties match the JSONL schema: unit_id, lb_range, text_zh, type,
/// confidence, plus optional sub_type and speaker.
/// </summary>
public sealed class SegmentInfo
{
    [JsonPropertyName("unit_id")]
    public string? UnitId { get; set; }

    [JsonPropertyName("lb_range")]
    public List<string>? LbRange { get; set; }

    [JsonPropertyName("text_zh")]
    public string? TextZh { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("sub_type")]
    public string? SubType { get; set; }

    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("label_zh")]
    public string? LabelZh { get; set; }
}

/// <summary>
/// The fully loaded segment map for a single XML file: a dictionary of
/// lb-ID to SegmentInfo, plus the original ordered list of segments.
/// </summary>
public sealed class SegmentMap
{
    /// <summary>
    /// Ordered list of all segments in document order.
    /// </summary>
    public IReadOnlyList<SegmentInfo> Segments { get; }

    /// <summary>
    /// Fast lookup: lb-ID (e.g. "0526c25") to the segment that contains it.
    /// Multiple lb-IDs may map to the same SegmentInfo when a segment spans
    /// multiple lb lines.
    /// </summary>
    public IReadOnlyDictionary<string, SegmentInfo> ByLbId { get; }

    /// <summary>
    /// SHA-256 (lowercase hex) of the line-ending-normalized source XML this map was
    /// generated from, taken from the optional <c>source_sha256</c> header line of the
    /// .segments.jsonl (audit P3.1b). Null for maps produced before the staleness
    /// contract existed. When set, <see cref="SegmentMapService"/> refuses the map if
    /// the current source XML no longer matches this hash.
    /// </summary>
    public string? SourceSha256 { get; }

    public SegmentMap(
        IReadOnlyList<SegmentInfo> segments,
        IReadOnlyDictionary<string, SegmentInfo> byLbId,
        string? sourceSha256 = null)
    {
        Segments = segments;
        ByLbId = byLbId;
        SourceSha256 = sourceSha256;
    }
}
