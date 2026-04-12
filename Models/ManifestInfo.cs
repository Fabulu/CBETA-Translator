// Models/ManifestInfo.cs
// Data model for OpenZenTexts manifest.json provenance files.
// Deserialized with System.Text.Json using snake_case naming policy.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class ManifestInfo
{
    [JsonPropertyName("text_id")]
    public string? TextId { get; set; }

    [JsonPropertyName("work_name")]
    public string? WorkName { get; set; }

    [JsonPropertyName("work_name_zh")]
    public string? WorkNameZh { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("edition_kind")]
    public string? EditionKind { get; set; }

    [JsonPropertyName("edition_kind_note")]
    public string? EditionKindNote { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("license_basis")]
    public string? LicenseBasis { get; set; }

    [JsonPropertyName("commercial_use_allowed")]
    public bool CommercialUseAllowed { get; set; }

    [JsonPropertyName("attribution_required")]
    public bool AttributionRequired { get; set; }

    [JsonPropertyName("share_alike_required")]
    public bool ShareAlikeRequired { get; set; }

    [JsonPropertyName("no_cbeta_material")]
    public bool NoCbetaMaterial { get; set; }

    [JsonPropertyName("production_method")]
    public string? ProductionMethod { get; set; }

    [JsonPropertyName("production_notes")]
    public string? ProductionNotes { get; set; }

    [JsonPropertyName("captured_utc")]
    public string? CapturedUtc { get; set; }

    [JsonPropertyName("curator")]
    public string? Curator { get; set; }

    [JsonPropertyName("witnesses_consulted")]
    public List<WitnessInfo>? Witnesses { get; set; }
}

public sealed class WitnessInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("upstream_url")]
    public string? UpstreamUrl { get; set; }

    [JsonPropertyName("stable_revision_url")]
    public string? StableRevisionUrl { get; set; }

    [JsonPropertyName("captured_sha256")]
    public string? CapturedSha256 { get; set; }

    [JsonPropertyName("captured_bytes")]
    public long CapturedBytes { get; set; }

    [JsonPropertyName("captured_utc")]
    public string? CapturedUtc { get; set; }

    [JsonPropertyName("rights")]
    public string? Rights { get; set; }

    [JsonPropertyName("vetting_confidence")]
    public string? VettingConfidence { get; set; }

    [JsonPropertyName("role_in_production")]
    public string? RoleInProduction { get; set; }
}
