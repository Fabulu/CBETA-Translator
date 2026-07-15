using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

// Rich Zen-dictionary model (schema v2). Persisted to termbase.v2.json as a DictionaryFile
// envelope, read only by new clients. Legacy clients keep reading the downgraded
// termbase.json (a bare array of TermbaseEntry) that DictionaryStore emits alongside it.
// See runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/SPEC_v2.md / SPEC_v3.md.

/// <summary>Top-level envelope of the rich dictionary file (termbase.v2.json).</summary>
public sealed class DictionaryFile
{
    public int SchemaVersion { get; set; } = 2;
    public List<DictionaryEntry> Entries { get; set; } = new();
}

/// <summary>
/// One dictionary article: the Chinese head term plus one or more senses. A term can
/// carry a corpus-wide Zen sense (SenseKey == null) and one or more master-specific
/// senses (e.g. Nanquan's 水牯牛). Identity is <see cref="Id"/> (deterministic from the
/// head term), which is the community-merge key — decoupled from SourceTerm text.
/// </summary>
public sealed class DictionaryEntry
{
    /// <summary>Stable, deterministic id (derived from SourceTerm). The merge key.</summary>
    public string Id { get; set; } = "";

    /// <summary>Chinese head term (also the substring match key when scanning texts).</summary>
    public string SourceTerm { get; set; } = "";

    public List<DictionarySense> Senses { get; set; } = new();

    public string? CreatedBy { get; set; }
    public DateTimeOffset? WrittenUtc { get; set; }

    /// <summary>UI convenience: the first sense's preferred target (for the entry list). Not persisted.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string FirstSenseTarget => Senses is { Count: > 0 } ? (Senses[0].PreferredTarget ?? "") : "";
}

/// <summary>
/// A single sense of a head term. Corpus-wide sense = SenseKey null; master-specific
/// sense = SenseKey set to the master's CanonicalName (also mirrored in MasterName).
/// </summary>
public sealed class DictionarySense
{
    /// <summary>null = corpus-wide Zen sense; otherwise a discriminator (usually a master name).</summary>
    public string? SenseKey { get; set; }

    /// <summary>Owning Zen Master (CanonicalName) for a master-specific sense; null for corpus-wide.</summary>
    public string? MasterName { get; set; }

    public string PreferredTarget { get; set; } = "";
    public List<string> AlternateTargets { get; set; } = new();

    /// <summary>
    /// Non-display English lookup phrases. These improve retrieval without claiming that
    /// every reader synonym is an equally good translation target.
    /// </summary>
    public List<string> SearchAliases { get; set; } = new();

    /// <summary>preferred | allowed | deprecated | forbidden.</summary>
    public string Status { get; set; } = "preferred";

    /// <summary>Prose explanation of the Zen sense, in context.</summary>
    public string? Explanation { get; set; }

    /// <summary>provisional | multi-source | disputed.</summary>
    public string Validation { get; set; } = "provisional";

    public string Note { get; set; } = "";

    /// <summary>Curated + auto-suggested evidence from the Zen-corpus concordance.</summary>
    public List<DictOccurrence> Occurrences { get; set; } = new();

    /// <summary>RelPaths (Zen texts) this sense is grounded in — the multi-source check.</summary>
    public List<string> SourceTexts { get; set; } = new();

    /// <summary>Zen Masters (CanonicalNames) who use the term in this sense.</summary>
    public List<string> RelatedMasters { get; set; } = new();

    /// <summary>Cross-referenced head terms (e.g. 水牯牛 ↔ 異類中行).</summary>
    public List<string> RelatedTerms { get; set; } = new();
}

/// <summary>
/// A single occurrence of the term in a Zen text. Generated from the search index, not
/// hand-typed. Durable identity = RelPath + Kwic (+ CharOffset soft anchor); FromLb is
/// filled when the line can be resolved. Links via ZenUriParser.BuildUri.
/// </summary>
public sealed class DictOccurrence
{
    public string RelPath { get; set; } = "";
    public string? FromLb { get; set; }
    public string? ToLb { get; set; }
    public int? CharOffset { get; set; }

    /// <summary>Verbatim KWIC snippet (also the search-fallback re-anchor).</summary>
    public string Kwic { get; set; } = "";

    public string? MasterName { get; set; }
    /// <summary>
    /// Reviewed exact-actor exception. This is allowed only for an unnamed non-master participant
    /// or an impersonal grammatical construction; every master must be named in MasterName.
    /// </summary>
    public DictActorAttribution? ActorAttribution { get; set; }
    public List<DictContextMaster> ContextMasters { get; set; } = new();
    public string? ApproxDate { get; set; }

    /// <summary>True = lexicographer-picked as defining the sense; false = auto-suggested.</summary>
    public bool Curated { get; set; }

    /// <summary>Note for floating/disputed attributions (e.g. Da'an ↔ Nanquan).</summary>
    public string? AttributionNote { get; set; }
    public string? EvidenceRole { get; set; }
}

public sealed class DictActorAttribution
{
    public string Status { get; set; } = "";
    public string Kind { get; set; } = "";
    public string ActorLabel { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public List<string> RungsChecked { get; set; } = new();
    public string? GrammarEvidence { get; set; }
    public string ReviewedBy { get; set; } = "";
    public DateTimeOffset? ReviewedUtc { get; set; }
}

public sealed class DictContextMaster
{
    public string MasterName { get; set; } = "";
    public List<string> Roles { get; set; } = new();
}
