using ReadZen.App.Services;
using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

public enum TranslationResourceTrust
{
    Approved = 0,
    Draft = 1,
    AiReference = 2
}

public enum QaSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed class CurrentSegmentContext
{
    public string RelPath { get; set; } = "";
    public string TextId { get; set; } = "";
    public int BlockNumber { get; set; }
    public int ProjectionOffsetStart { get; set; }
    public int ProjectionOffsetEndExclusive { get; set; }

    public string ZhText { get; set; } = "";
    public string EnText { get; set; } = "";

    /// <summary>
    /// Last ~4 chars of previous block + current block + first ~4 chars of next block.
    /// Used for TM search so phrases spanning tag boundaries are matched without adding full-neighbor noise.
    /// Falls back to ZhText when not set.
    /// </summary>
    public string ZhContextText { get; set; } = "";

    public TranslationEditMode Mode { get; set; } = TranslationEditMode.Body;
}

public sealed class TranslationTmMatch
{
    public string SourceText { get; set; } = "";
    public string TargetText { get; set; } = "";

    public string RelPath { get; set; } = "";
    public int BlockNumber { get; set; }
    public string SourceRef { get; set; } = "";
    public string Translator { get; set; } = "";

    public TranslationResourceTrust Trust { get; set; } = TranslationResourceTrust.AiReference;
    public string ReviewStatus { get; set; } = "";
    public double Score { get; set; }

    /// <summary>Human-readable note describing how the TM source differs from the query (variant reading).</summary>
    public string? VariantNote { get; set; }

    /// <summary>True when the TM source text is a textual variant of the query, not an exact match.</summary>
    public bool IsVariantMatch { get; set; }
}

public sealed class TermHit
{
    public string SourceTerm { get; set; } = "";
    public string PreferredTarget { get; set; } = "";
    public List<string> AlternateTargets { get; set; } = new();
    public string Status { get; set; } = ""; // preferred / allowed / deprecated / forbidden
    public string Note { get; set; } = "";
    public string? CreatedBy { get; set; }
}

public sealed class QaIssue
{
    public string RuleId { get; set; } = "";
    public QaSeverity Severity { get; set; }
    public string Message { get; set; } = "";
    public string RelatedTerm { get; set; } = "";
}

public sealed class TranslationAssistantSnapshot
{
    public CurrentSegmentContext Segment { get; set; } = new();

    public List<TranslationTmMatch> ApprovedMatches { get; set; } = new();
    public List<TranslationTmMatch> ReferenceMatches { get; set; } = new();
    public List<TermHit> Terms { get; set; } = new();
    public List<QaIssue> QaIssues { get; set; } = new();

    /// <summary>
    /// Chinese-only concordance hits from untranslated files, populated async
    /// after TM results. Null when concordance is disabled or not yet loaded;
    /// empty list when loaded but nothing matched.
    /// </summary>
    public List<ConcordanceHit>? ConcordanceHits { get; set; }

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A concordance match — a similar Chinese passage found in an untranslated
/// (or differently-translated) file. No English translation available; the
/// assistant panel shows the Chinese snippet only with a "(no translation)"
/// label. Populated by querying the existing search index.
/// </summary>
public sealed class ConcordanceHit
{
    /// <summary>Relative path of the file containing the match.</summary>
    public string RelPath { get; set; } = "";

    /// <summary>Human-readable file name (from title index or filename).</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>The Chinese text snippet around the match.</summary>
    public string SnippetZh { get; set; } = "";
}
