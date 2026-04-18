// Infrastructure/VariantAwareTmAnnotator.cs
// Post-scoring annotator that detects variant readings between TM source
// and query text, attaching human-readable diff notes to matches.

using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Annotates TM matches with variant diff information. For each match where
/// the source text differs from the query by only a few character spans
/// (potential variant reading), sets <see cref="TranslationTmMatch.VariantNote"/>
/// and <see cref="TranslationTmMatch.IsVariantMatch"/>.
/// </summary>
public static class VariantAwareTmAnnotator
{
    /// <summary>Maximum changed spans to still count as a variant (not a completely different text).</summary>
    private const int MaxChangedSpans = 3;

    /// <summary>
    /// Annotates TM matches with variant diff information.
    /// For each match where the source text differs from the query text
    /// by only a few characters (potential variant reading), adds a
    /// VariantNote to the match explaining what changed.
    /// </summary>
    public static void Annotate(
        List<TranslationTmMatch> matches,
        string queryZhText,
        List<CorrectionEntry>? corrections = null)
    {
        if (matches == null || string.IsNullOrEmpty(queryZhText)) return;

        foreach (var match in matches)
        {
            if (string.IsNullOrEmpty(match.SourceText) || match.SourceText == queryZhText)
                continue;

            var spans = CjkCharDiff.Diff(match.SourceText, queryZhText);
            int changedSpanCount = spans.Count(s => s.Kind != CharDiffKind.Equal);

            if (changedSpanCount == 0 || changedSpanCount > MaxChangedSpans)
                continue;

            string diffText = CjkCharDiff.FormatCompact(spans);
            string suffix = IsKnownCorrection(match.SourceText, queryZhText, corrections)
                ? "known correction"
                : "variant reading";

            match.VariantNote = $"TM source differs: {diffText} — {suffix}";
            match.IsVariantMatch = true;
        }
    }

    /// <summary>
    /// Checks whether the difference between TM source and query corresponds
    /// to a known correction entry (the TM was made against a pre-correction reading).
    /// </summary>
    private static bool IsKnownCorrection(
        string tmSource, string queryText, List<CorrectionEntry>? corrections)
    {
        if (corrections == null || corrections.Count == 0) return false;

        foreach (var c in corrections)
        {
            // The TM source matches the pre-correction text and the query matches the post-correction text
            if (!string.IsNullOrEmpty(c.Before) && !string.IsNullOrEmpty(c.After)
                && tmSource.Contains(c.Before) && queryText.Contains(c.After))
                return true;
        }

        return false;
    }
}
