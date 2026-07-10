using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class TranslationQaService : ITranslationQaService
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

        if (!string.IsNullOrWhiteSpace(en) && !string.IsNullOrWhiteSpace(zh)
            && string.Equals(StripSpaces(en), StripSpaces(zh), StringComparison.Ordinal))
        {
            issues.Add(new QaIssue
            {
                RuleId = "same-as-source",
                Severity = QaSeverity.Warning,
                Message = "EN text is identical to ZH — segment may be untranslated."
            });
        }

        if (!string.IsNullOrWhiteSpace(en) && ContainsCjk(en))
        {
            issues.Add(new QaIssue
            {
                RuleId = "chinese-in-en",
                Severity = QaSeverity.Error,
                Message = "EN contains Chinese characters — possible copy-paste error."
            });
        }

        if (!string.IsNullOrWhiteSpace(zh) && !string.IsNullOrWhiteSpace(en))
        {
            int zhLen = zh.Replace(" ", "").Length;
            int enWords = en.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (zhLen > 15 && enWords <= 2)
            {
                issues.Add(new QaIssue
                {
                    RuleId = "too-short",
                    Severity = QaSeverity.Warning,
                    Message = $"Translation seems very short ({enWords} word(s)) for a {zhLen}-character source."
                });
            }
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

    private static string StripSpaces(string s)
        => s.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

    private static bool ContainsCjk(string s) => ReadZen.App.Infrastructure.CjkText.ContainsIdeograph(s);
}