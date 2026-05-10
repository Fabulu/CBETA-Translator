// Models/AnchorModels.cs
// Anchor-base register and anchor-event-log entries from provenance JSONL files.
// Deserialized with System.Text.Json using JsonPropertyName attributes.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class AnchorBase
{
    [JsonPropertyName("anchor_id")] public string? AnchorId { get; set; }
    [JsonPropertyName("witness_id")] public string? WitnessId { get; set; }
    [JsonPropertyName("page_id")] public string? PageId { get; set; }
    [JsonPropertyName("locus_id")] public string? LocusId { get; set; }
    [JsonPropertyName("source_asset_path")] public string? SourceAssetPath { get; set; }
    [JsonPropertyName("source_download_url")] public string? SourceDownloadUrl { get; set; }
    [JsonPropertyName("source_kind")] public string? SourceKind { get; set; }
    [JsonPropertyName("page_number")] public int? PageNumber { get; set; }
    [JsonPropertyName("page_bbox")] public double[]? PageBbox { get; set; }
    [JsonPropertyName("locus_bbox")] public double[]? LocusBbox { get; set; }
    [JsonPropertyName("polygon")] public double[][]? Polygon { get; set; }
    [JsonPropertyName("crop_asset_path")] public string? CropAssetPath { get; set; }
    [JsonPropertyName("ocr_region_ref")] public string? OcrRegionRef { get; set; }
    [JsonPropertyName("char_boxes")] public List<double[]>? CharBoxes { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}

public sealed class AnchorEvent
{
    [JsonPropertyName("event_id")] public string? EventId { get; set; }
    [JsonPropertyName("event_date")] public string? EventDate { get; set; }
    [JsonPropertyName("edition_slug")] public string? EditionSlug { get; set; }
    [JsonPropertyName("locus_id")] public string? LocusId { get; set; }
    [JsonPropertyName("witness_id")] public string? WitnessId { get; set; }
    [JsonPropertyName("before_text")] public string? BeforeText { get; set; }
    [JsonPropertyName("after_text")] public string? AfterText { get; set; }
    [JsonPropertyName("translation_before")] public string? TranslationBefore { get; set; }
    [JsonPropertyName("translation_after")] public string? TranslationAfter { get; set; }
    [JsonPropertyName("change_type")] public string? ChangeType { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("anchor_ids")] public List<string>? AnchorIds { get; set; }
    [JsonPropertyName("source_asset_path")] public string? SourceAssetPath { get; set; }
    [JsonPropertyName("source_download_url")] public string? SourceDownloadUrl { get; set; }
    [JsonPropertyName("page_id")] public string? PageId { get; set; }
    [JsonPropertyName("page_bbox")] public double[]? PageBbox { get; set; }
    [JsonPropertyName("locus_bbox")] public double[]? LocusBbox { get; set; }
    [JsonPropertyName("char_boxes")] public List<double[]>? CharBoxes { get; set; }
    [JsonPropertyName("evidence_type")] public string? EvidenceType { get; set; }
    [JsonPropertyName("confidence")] public string? Confidence { get; set; }
    [JsonPropertyName("basis_note")] public string? BasisNote { get; set; }
    [JsonPropertyName("comparison_reading")] public string? ComparisonReading { get; set; }

    [JsonIgnore]
    public string ChangeTypeDisplay => ChangeType?.Replace('_', ' ') ?? "";
}
