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

    // File pointers (critical-edition support)

    [JsonPropertyName("process_file")]
    public string? ProcessFile { get; set; }

    [JsonPropertyName("apparatus_file")]
    public string? ApparatusFile { get; set; }

    [JsonPropertyName("stats_file")]
    public string? StatsFile { get; set; }

    [JsonPropertyName("documents_file")]
    public string? DocumentsFile { get; set; }

    [JsonPropertyName("timeline_file")]
    public string? TimelineFile { get; set; }

    [JsonPropertyName("commentary_file")]
    public string? CommentaryFile { get; set; }

    /// <summary>
    /// Reader-facing language whitelist for the commentary panel. When set,
    /// `CommentaryService.TryLoad(xml, manifest.CommentaryReaderLanguages)`
    /// surfaces only entries whose `language` matches (case-insensitive,
    /// BCP-47 prefix). When null, the reader panel stays hidden (edition
    /// opts out of commentary surfacing — zero footprint on non-opt-in
    /// editions). Provenance/admin call sites pass null regardless and
    /// see every entry unfiltered.
    /// </summary>
    [JsonPropertyName("commentary_reader_languages")]
    public List<string>? CommentaryReaderLanguages { get; set; }

    // Edition fields (critical-edition support)

    [JsonPropertyName("base_witness_id")]
    public string? BaseWitnessId { get; set; }

    [JsonPropertyName("edition_maturity")]
    public string? EditionMaturity { get; set; }

    [JsonPropertyName("ocr_maximal")]
    public bool? OcrMaximal { get; set; }

    [JsonPropertyName("human_intervention_required")]
    public bool? HumanInterventionRequired { get; set; }

    [JsonPropertyName("human_intervention_note")]
    public string? HumanInterventionNote { get; set; }

    // Previously missing schema fields

    [JsonPropertyName("work_name_alt")]
    public List<string>? WorkNameAlt { get; set; }

    [JsonPropertyName("compiler")]
    public string? Compiler { get; set; }

    [JsonPropertyName("year_composed")]
    public string? YearComposed { get; set; }

    // ─── Structured date provenance (Phase D of RUN-20260416-2302) ───
    // Critical editions have four meaningfully-distinct dates that the old
    // `year_composed` alone collapses. These fields are optional; for
    // backward-compat, editions without them fall back to displaying
    // `year_composed`. Strings rather than ints so we can carry "ca. 1228"
    // or "1632 (Waseda recut 1752)".

    /// <summary>
    /// When the original work was composed (e.g. 1228 for Wumen Huikai's
    /// compilation). Distinct from when any specific physical witness was
    /// made.
    /// </summary>
    [JsonPropertyName("composition_date")]
    public string? CompositionDate { get; set; }

    /// <summary>
    /// When the physical manuscript / imprint used as the base witness was
    /// produced (e.g. "1632" for the NDL Wumenguan woodblock).
    /// </summary>
    [JsonPropertyName("manuscript_date")]
    public string? ManuscriptDate { get; set; }

    /// <summary>
    /// When the text was shaped by later hands after initial composition —
    /// source-critical material like Anwan's case 49 addendum to the
    /// Wumenguan. Free-text since redaction can span periods or involve
    /// multiple hands.
    /// </summary>
    [JsonPropertyName("redaction_date")]
    public string? RedactionDate { get; set; }

    /// <summary>
    /// When the editorial decisions about readings in THIS edition were
    /// made (e.g. "2026-04" for our reading edition work). The layer of
    /// modern textual criticism on top of the physical witness.
    /// </summary>
    [JsonPropertyName("textual_criticism_date")]
    public string? TextualCriticismDate { get; set; }
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

    [JsonPropertyName("family_id")]
    public string? FamilyId { get; set; }

    [JsonPropertyName("page_count")]
    public int? PageCount { get; set; }

    [JsonPropertyName("completeness")]
    public string? Completeness { get; set; }

    [JsonPropertyName("validation_method")]
    public string? ValidationMethod { get; set; }

    [JsonPropertyName("source_page_snapshot")]
    public string? SourcePageSnapshot { get; set; }

    [JsonPropertyName("license_snapshot")]
    public string? LicenseSnapshot { get; set; }

    [JsonPropertyName("rights_basis_text")]
    public string? RightsBasisText { get; set; }

    [JsonPropertyName("provenance_check")]
    public string? ProvenanceCheck { get; set; }

    [JsonPropertyName("captured_local_path")]
    public string? CapturedLocalPath { get; set; }

    [JsonPropertyName("captured_filename")]
    public string? CapturedFilename { get; set; }

    [JsonPropertyName("stable_revision_id")]
    public string? StableRevisionId { get; set; }
}
