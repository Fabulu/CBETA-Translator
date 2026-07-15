using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>
/// A single Zen master record from the rich lineage roster
/// (<c>Assets/Data/lineage-masters.json</c>, 609 records). This is a NEW,
/// additive data layer that backs the forthcoming tidy-forest lineage chart.
/// It is deliberately SEPARATE from <see cref="MasterDateEntry"/> /
/// <c>master-dates.json</c> (the thin 301-master roster that hover-dict, the
/// master manager, and text-scan depend on) — see plan decision D3. Do not
/// unify the two rosters here.
///
/// Deserialized with System.Text.Json from snake_case JSON; unknown/missing
/// JSON fields are tolerated (System.Text.Json ignores unmapped properties and
/// leaves unset members at their defaults).
/// </summary>
public sealed class LineageMasterRecord
{
    /// <summary>All known names/aliases (pinyin, hanja, epithets). Never null.</summary>
    [JsonPropertyName("names")]
    public List<string> Names { get; set; } = new();

    [JsonPropertyName("birth")]
    public int? Birth { get; set; }

    [JsonPropertyName("death")]
    public int? Death { get; set; }

    [JsonPropertyName("floruit")]
    public int? Floruit { get; set; }

    [JsonPropertyName("school")]
    public string? School { get; set; }

    /// <summary>Human-readable teacher name (may be display-only).</summary>
    [JsonPropertyName("teacher")]
    public string? Teacher { get; set; }

    /// <summary>Canonical parent-NODE id — the reliable edge key for graph building.</summary>
    [JsonPropertyName("teacher_key")]
    public string? TeacherKey { get; set; }

    /// <summary>True when the teacher edge points off-chart / cannot be resolved.</summary>
    [JsonPropertyName("teacher_dangling")]
    public bool TeacherDangling { get; set; }

    /// <summary>Transmission type marker (e.g. 遙嗣, 代囑) driving edge/node geometry.</summary>
    [JsonPropertyName("transmission")]
    public string? Transmission { get; set; }

    [JsonPropertyName("book_transmissions")]
    public List<LineageBookTransmission> BookTransmissions { get; set; } = new();

    [JsonPropertyName("contested")]
    public bool Contested { get; set; }

    [JsonPropertyName("contested_by")]
    public LineageContestedBy? ContestedBy { get; set; }

    [JsonPropertyName("edge_note")]
    public string? EdgeNote { get; set; }

    [JsonPropertyName("steles")]
    public List<LineageStele> Steles { get; set; } = new();

    [JsonPropertyName("provenance")]
    public LineageProvenance? Provenance { get; set; }

    [JsonPropertyName("dates_conjectural")]
    public bool DatesConjectural { get; set; }

    [JsonPropertyName("dates_conflict")]
    public bool DatesConflict { get; set; }

    [JsonPropertyName("date_note")]
    public string? DateNote { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("links")]
    public List<LineageLink> Links { get; set; } = new();

    [JsonPropertyName("students")]
    public List<string> Students { get; set; } = new();

    /// <summary>Evidence attestation grade. Expected one of A/B/C/D (fail-safe to D downstream).</summary>
    [JsonPropertyName("attestation")]
    public string? Attestation { get; set; }

    // --- Additional fields present in the source roster (not in the PR-L1 brief,
    //     modeled for fidelity; all optional). ---

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("added_node")]
    public bool AddedNode { get; set; }

    [JsonPropertyName("forgery_corrected")]
    public bool ForgeryCorrected { get; set; }

    [JsonPropertyName("hunted")]
    public bool Hunted { get; set; }

    [JsonPropertyName("quote_needs_human")]
    public bool QuoteNeedsHuman { get; set; }

    [JsonPropertyName("unverified")]
    public bool Unverified { get; set; }
}

/// <summary>A text attributed to a master, optionally present in the corpus.</summary>
public sealed class LineageBookTransmission
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title_en")]
    public string? TitleEn { get; set; }

    [JsonPropertyName("title_hanja")]
    public string? TitleHanja { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("in_corpus")]
    public bool InCorpus { get; set; }
}

/// <summary>Records a contested teacher edge and its rival hypothesis.</summary>
public sealed class LineageContestedBy
{
    [JsonPropertyName("keep_teacher")]
    public string? KeepTeacher { get; set; }

    [JsonPropertyName("rival")]
    public string? Rival { get; set; }

    [JsonPropertyName("rival_rung")]
    public string? RivalRung { get; set; }

    [JsonPropertyName("rival_evidence")]
    public string? RivalEvidence { get; set; }

    [JsonPropertyName("kept_rung")]
    public string? KeptRung { get; set; }

    [JsonPropertyName("kept_evidence")]
    public string? KeptEvidence { get; set; }

    [JsonPropertyName("stake")]
    public string? Stake { get; set; }
}

/// <summary>A stele / inscription cited as evidence.</summary>
public sealed class LineageStele
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("lb")]
    public string? Lb { get; set; }

    [JsonPropertyName("quote")]
    public string? Quote { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("author_note")]
    public string? AuthorNote { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>Per-claim provenance evidence, grouped by the claim it supports.</summary>
public sealed class LineageProvenance
{
    [JsonPropertyName("teacher")]
    public List<LineageProvenanceItem> Teacher { get; set; } = new();

    [JsonPropertyName("dates")]
    public List<LineageProvenanceItem> Dates { get; set; } = new();

    [JsonPropertyName("school")]
    public List<LineageProvenanceItem> School { get; set; } = new();

    [JsonPropertyName("bio")]
    public List<LineageProvenanceItem> Bio { get; set; } = new();
}

/// <summary>A single provenance citation (source line + evidence rung + quote).</summary>
public sealed class LineageProvenanceItem
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("lb")]
    public string? Lb { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("rung")]
    public string? Rung { get; set; }

    [JsonPropertyName("quote")]
    public string? Quote { get; set; }

    [JsonPropertyName("quote_zh")]
    public string? QuoteZh { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("birth")]
    public int? Birth { get; set; }

    [JsonPropertyName("death")]
    public int? Death { get; set; }
}

/// <summary>An external reference link with optional verification metadata.</summary>
public sealed class LineageLink
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("confirms")]
    public string? Confirms { get; set; }

    [JsonPropertyName("verified")]
    public string? Verified { get; set; }
}
