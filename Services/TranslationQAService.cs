using System;
using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class TranslationQaService
{
    public List<QaIssue> Check(CurrentSegmentContext ctx, List<TermHit> terms)
    {
        var issues = new List<QaIssue>();
        string en = ctx.EnText ?? "";
        string zh = ctx.ZhText ?? "";

        if (!string.IsNullOrWhiteSpace(zh) && string.IsNullOrWhiteSpace(en))
        {
            issues.Add(new QaIssue
            {
                RuleId = "empty-en",
                Severity = QaSeverity.Warning,
                Message = "EN is empty while ZH is non-empty."
            });
        }

        if (en.Contains('<') || en.Contains('>'))
        {
            issues.Add(new QaIssue
            {
                RuleId = "illegal-angle-brackets",
                Severity = QaSeverity.Error,
                Message = "EN contains '<' or '>' which is not allowed."
            });
        }

        foreach (var term in terms)
        {
            if (string.IsNullOrWhiteSpace(term.PreferredTarget))
                continue;

            bool usesPreferred = en.Contains(term.PreferredTarget, StringComparison.OrdinalIgnoreCase);
            bool usesAlternate = term.AlternateTargets.Any(a =>
                !string.IsNullOrWhiteSpace(a) &&
                en.Contains(a, StringComparison.OrdinalIgnoreCase));

            if (!usesPreferred && usesAlternate)
            {
                issues.Add(new QaIssue
                {
                    RuleId = "preferred-term-missing",
                    Severity = QaSeverity.Warning,
                    RelatedTerm = term.SourceTerm,
                    Message = $"Preferred rendering for {term.SourceTerm} is \"{term.PreferredTarget}\", but current EN uses an alternate rendering."
                });
            }

            if (!usesPreferred && !usesAlternate && !string.IsNullOrWhiteSpace(en))
            {
                issues.Add(new QaIssue
                {
                    RuleId = "recognized-term-unmatched",
                    Severity = QaSeverity.Info,
                    RelatedTerm = term.SourceTerm,
                    Message = $"Recognized term {term.SourceTerm} has preferred rendering \"{term.PreferredTarget}\", but it was not detected in current EN."
                });
            }
        }

        return issues;
    }
}