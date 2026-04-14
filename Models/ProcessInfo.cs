// Models/ProcessInfo.cs
// Machine-readable editorial workflow record from process.json.
// Deserialized with System.Text.Json using JsonPropertyName attributes.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class ProcessInfo
{
    [JsonPropertyName("project")]
    public ProcessProject? Project { get; set; }

    [JsonPropertyName("base_witness")]
    public ProcessBaseWitness? BaseWitness { get; set; }

    [JsonPropertyName("witness_families")]
    public List<ProcessWitnessFamily>? WitnessFamilies { get; set; }

    [JsonPropertyName("ocr_pipeline")]
    public OcrPipelineInfo? OcrPipeline { get; set; }

    [JsonPropertyName("segmentation_pipeline")]
    public SegmentationPipelineInfo? SegmentationPipeline { get; set; }

    [JsonPropertyName("human_passes")]
    public List<HumanPassInfo>? HumanPasses { get; set; }

    [JsonPropertyName("decision_records")]
    public List<DecisionRecordInfo>? DecisionRecords { get; set; }

    [JsonPropertyName("coverage")]
    public CoverageInfo? Coverage { get; set; }

    [JsonPropertyName("unresolved_loci")]
    public List<UnresolvedLocusInfo>? UnresolvedLoci { get; set; }

    // Timeline + log integration (added by timeline-provenance spec)
    [JsonPropertyName("current_stage")]
    public string? CurrentStage { get; set; }

    [JsonPropertyName("edition_maturity")]
    public string? EditionMaturity { get; set; }

    [JsonPropertyName("timeline_file")]
    public string? TimelineFile { get; set; }

    [JsonPropertyName("human_log_file")]
    public string? HumanLogFile { get; set; }

    [JsonPropertyName("publication_checks")]
    public PublicationChecks? PublicationChecks { get; set; }
}

public sealed class ProcessProject
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("edition_kind")]
    public string? EditionKind { get; set; }

    [JsonPropertyName("target_maturity")]
    public string? TargetMaturity { get; set; }

    [JsonPropertyName("curator")]
    public string? Curator { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }
}

public sealed class ProcessBaseWitness
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("selection_rationale")]
    public string? SelectionRationale { get; set; }
}

public sealed class ProcessWitnessFamily
{
    [JsonPropertyName("family_id")]
    public string? FamilyId { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("members")]
    public List<string>? Members { get; set; }

    [JsonPropertyName("relationship_notes")]
    public string? RelationshipNotes { get; set; }
}

public sealed class OcrPipelineInfo
{
    [JsonPropertyName("engines")]
    public List<OcrEngineInfo>? Engines { get; set; }

    [JsonPropertyName("default_engine")]
    public string? DefaultEngine { get; set; }

    [JsonPropertyName("evaluation_method")]
    public string? EvaluationMethod { get; set; }
}

public sealed class OcrEngineInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("parameters")]
    public string? Parameters { get; set; }

    [JsonPropertyName("run_date")]
    public string? RunDate { get; set; }
}

public sealed class HumanPassInfo
{
    [JsonPropertyName("pass_id")]
    public string? PassId { get; set; }

    [JsonPropertyName("witness_id")]
    public string? WitnessId { get; set; }

    [JsonPropertyName("pages_or_loci")]
    public string? PagesOrLoci { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("change_type")]
    public string? ChangeType { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public sealed class DecisionRecordInfo
{
    [JsonPropertyName("decision_id")]
    public string? DecisionId { get; set; }

    [JsonPropertyName("locus")]
    public string? Locus { get; set; }

    [JsonPropertyName("issue")]
    public string? Issue { get; set; }

    [JsonPropertyName("options_considered")]
    public List<string>? OptionsConsidered { get; set; }

    [JsonPropertyName("evidence")]
    public string? Evidence { get; set; }

    [JsonPropertyName("chosen_reading")]
    public string? ChosenReading { get; set; }

    [JsonPropertyName("reversibility")]
    public string? Reversibility { get; set; }

    [JsonPropertyName("affected_loci")]
    public List<string>? AffectedLoci { get; set; }
}

public sealed class CoverageInfo
{
    [JsonPropertyName("total_pages")]
    public int? TotalPages { get; set; }

    [JsonPropertyName("segmented_pages")]
    public int? SegmentedPages { get; set; }

    [JsonPropertyName("ocr_pages")]
    public int? OcrPages { get; set; }

    [JsonPropertyName("human_checked_pages")]
    public int? HumanCheckedPages { get; set; }

    [JsonPropertyName("percent_complete")]
    public double? PercentComplete { get; set; }
}

public sealed class UnresolvedLocusInfo
{
    [JsonPropertyName("locus_id")]
    public string? LocusId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("missing_evidence")]
    public string? MissingEvidence { get; set; }

    [JsonPropertyName("publication_status")]
    public string? PublicationStatus { get; set; }
}

public sealed class SegmentationPipelineInfo
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("page_map_file")]
    public string? PageMapFile { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public sealed class PublicationChecks
{
    [JsonPropertyName("all_witness_rights_confirmed")]
    public bool? AllWitnessRightsConfirmed { get; set; }

    [JsonPropertyName("all_hashes_valid")]
    public bool? AllHashesValid { get; set; }

    [JsonPropertyName("segmentation_complete")]
    public bool? SegmentationComplete { get; set; }

    [JsonPropertyName("ocr_recorded")]
    public bool? OcrRecorded { get; set; }

    [JsonPropertyName("ocr_benchmark_exists")]
    public bool? OcrBenchmarkExists { get; set; }

    [JsonPropertyName("human_passes_logged")]
    public bool? HumanPassesLogged { get; set; }

    [JsonPropertyName("apparatus_exists")]
    public bool? ApparatusExists { get; set; }

    [JsonPropertyName("unresolved_classified")]
    public bool? UnresolvedClassified { get; set; }

    [JsonPropertyName("tei_validates")]
    public bool? TeiValidates { get; set; }

    [JsonPropertyName("all_artifacts_validate")]
    public bool? AllArtifactsValidate { get; set; }
}
