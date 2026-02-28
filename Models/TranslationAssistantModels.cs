using CbetaTranslator.App.Services;
using System;
using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

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

    public TranslationEditMode Mode { get; set; } = TranslationEditMode.Body;
}

public sealed class TranslationTmMatch
{
    public string SourceText { get; set; } = "";
    public string TargetText { get; set; } = "";

    public string RelPath { get; set; } = "";
    public string SourceRef { get; set; } = "";
    public string Translator { get; set; } = "";

    public TranslationResourceTrust Trust { get; set; } = TranslationResourceTrust.AiReference;
    public string ReviewStatus { get; set; } = "";
    public double Score { get; set; }
}

public sealed class TermHit
{
    public string SourceTerm { get; set; } = "";
    public string PreferredTarget { get; set; } = "";
    public List<string> AlternateTargets { get; set; } = new();
    public string Status { get; set; } = ""; // preferred / allowed / deprecated / forbidden
    public string Note { get; set; } = "";
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

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}