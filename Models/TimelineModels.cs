// Models/TimelineModels.cs
// Timeline event stream and state reconstruction models for critical editions.
// Loaded from timeline.json. Powers the timeline slider and event inspection UI.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>Top-level timeline document.</summary>
public sealed class TimelineInfo
{
    [JsonPropertyName("text_id")]
    public string? TextId { get; set; }

    /// <summary>
    /// Deduplicated reading lookup table. Key = locus_id, value = array of reading strings.
    /// Index 0 = initial reading, subsequent indices = readings introduced by text_changed events.
    /// text_changed events reference readings by index (reading_before, reading_after).
    /// </summary>
    [JsonPropertyName("readings")]
    public Dictionary<string, List<string>>? Readings { get; set; }

    /// <summary>Edition revision bookmarks. Append-only; new revisions get higher event ranges.</summary>
    [JsonPropertyName("revisions")]
    public List<TimelineRevision>? Revisions { get; set; }

    [JsonPropertyName("events")]
    public List<TimelineEvent>? Events { get; set; }
}

/// <summary>A revision bookmark in the timeline. Tracks publication milestones.</summary>
public sealed class TimelineRevision
{
    [JsonPropertyName("revision_id")]
    public string? RevisionId { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("event_range")]
    public int[]? EventRange { get; set; }

    [JsonPropertyName("witnesses_added")]
    public List<string>? WitnessesAdded { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>A single event in the edition build history.</summary>
public sealed class TimelineEvent
{
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    [JsonPropertyName("object_type")]
    public string? ObjectType { get; set; }

    [JsonPropertyName("object_id")]
    public string? ObjectId { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("actor_type")]
    public string? ActorType { get; set; }

    [JsonPropertyName("actor_id")]
    public string? ActorId { get; set; }

    [JsonPropertyName("inputs")]
    public List<string>? Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public List<string>? Outputs { get; set; }

    [JsonPropertyName("evidence_links")]
    public List<string>? EvidenceLinks { get; set; }

    [JsonPropertyName("state_effects")]
    public Dictionary<string, object>? StateEffects { get; set; }

    [JsonPropertyName("decision_ref")]
    public string? DecisionRef { get; set; }

    [JsonPropertyName("note_anchor_id")]
    public string? NoteAnchorId { get; set; }

    [JsonPropertyName("supersedes")]
    public List<string>? Supersedes { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Display-friendly stage name.</summary>
    [JsonIgnore]
    public string StageDisplay => Stage switch
    {
        "project_setup" => "Project Setup",
        "witness_search" => "Witness Search",
        "witness_validation" => "Witness Validation",
        "witness_lock" => "Witness Lock",
        "sigla_freeze" => "Sigla Freeze",
        "copy_text_selection" => "Copy Text Selection",
        "ocr" => "OCR",
        "segmentation" => "Segmentation",
        "collation" => "Collation",
        "apparatus" => "Apparatus",
        "reading_text" => "Reading Text",
        "review" => "Review",
        "publication" => "Publication",
        _ => Stage ?? "Unknown",
    };

    /// <summary>Display-friendly event type.</summary>
    [JsonIgnore]
    public string EventTypeDisplay => EventType?.Replace('_', ' ') ?? "unknown";
}

/// <summary>
/// Reconstructed edition state at a specific point in the timeline.
/// Derived by replaying events from sequence 1 to the target event.
/// </summary>
public sealed class EditionState
{
    public List<string> AcceptedWitnesses { get; set; } = new();
    public List<string> RejectedWitnesses { get; set; } = new();
    public Dictionary<string, string> WitnessTiers { get; set; } = new(); // witnessId → tier
    public string? CopyTextCandidate { get; set; }
    public string? CopyTextSelected { get; set; }
    public List<string> UnresolvedLoci { get; set; } = new();
    public int OcrRunsStarted { get; set; }
    public int OcrRunsCompleted { get; set; }
    public int OcrRunsFailed { get; set; }
    public int ApparatusEntryCount { get; set; }
    public string? EditionMaturity { get; set; }
    public string? CurrentStage { get; set; }
    public int TotalEvents { get; set; }
}
