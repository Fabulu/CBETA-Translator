using System.Collections.Generic;

namespace ReadZen.App.Models;

/// <summary>
/// Generated evidence for a dictionary head term, scoped to the Zen corpus (the prescriptive
/// allowlist). Summary-first: carries the counts + a capped page of per-text groups (each with a
/// few sample KWIC occurrences) + a master rollup — NOT a flat dump of every occurrence. The full
/// occurrence set stays in the search index and is re-queried on demand; the dictionary entry
/// persists only the lexicographer-curated defining occurrences. See RUN-20260711-1248 (occurrence
/// tiering decision).
/// </summary>
public sealed class DictionaryEvidence
{
    public string Term { get; set; } = "";

    /// <summary>Number of Zen texts containing the term — the document frequency within the Zen corpus.</summary>
    public int ZenTextCount { get; set; }

    /// <summary>Total occurrences across all Zen texts (sum of per-text hit counts).</summary>
    public int TotalHitCount { get; set; }

    /// <summary>True when more texts exist beyond the returned (capped) <see cref="Texts"/> page.</summary>
    public bool Truncated { get; set; }

    /// <summary>Per-text groups (capped), each with a few sample occurrences. Grouping keeps the UI navigable.</summary>
    public List<DictEvidenceGroup> Texts { get; set; } = new();

    /// <summary>Which masters use the term (rollup over the returned texts), ranked most-used first.</summary>
    public List<DictMasterUsage> Masters { get; set; } = new();
}

/// <summary>One Zen text's usage of the term: its hit count plus a few sample KWIC occurrences.</summary>
public sealed class DictEvidenceGroup
{
    public string RelPath { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int HitCount { get; set; }
    /// <summary>Primary-author master of this text, if known (from the master-corpus index).</summary>
    public string? MasterName { get; set; }
    public List<DictOccurrence> Samples { get; set; } = new();
}

/// <summary>A master's usage of the term, rolled up over the evidence texts.</summary>
public sealed class DictMasterUsage
{
    public string MasterName { get; set; } = "";
    public int TextCount { get; set; }
    public int HitCount { get; set; }
}
