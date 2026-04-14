// Models/TranslationLicenseInfo.cs
// Per-file, per-user translation license choice.
// Stored in community/translation-licenses/{username}.jsonl.

using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class TranslationLicenseInfo
{
    [JsonPropertyName("rel_path")]
    public string? RelPath { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("license_url")]
    public string? LicenseUrl { get; set; }

    [JsonPropertyName("copyright_holder")]
    public string? CopyrightHolder { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("commercial_use_allowed")]
    public bool CommercialUseAllowed { get; set; }

    [JsonPropertyName("attribution_required")]
    public bool AttributionRequired { get; set; }

    [JsonPropertyName("share_alike_required")]
    public bool ShareAlikeRequired { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("chosen_utc")]
    public string? ChosenUtc { get; set; }
}
