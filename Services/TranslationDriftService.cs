// Services/TranslationDriftService.cs
// Detects "stale" translations — English segments that were translated
// against a Chinese reading that has since been corrected.

using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Infrastructure;

namespace ReadZen.App.Services;

/// <summary>
/// A single translation that's drifted from the current Chinese reading.
/// </summary>
public sealed class TranslationDriftEntry
{
    /// <summary>Locus where the drift occurred (e.g., T1-p007.l01).</summary>
    public string Locus { get; init; } = "";

    /// <summary>The Chinese text BEFORE the correction that caused drift.</summary>
    public string ChineseBefore { get; init; } = "";

    /// <summary>The Chinese text AFTER the correction (current reading).</summary>
    public string ChineseAfter { get; init; } = "";

    /// <summary>The English translation that's now stale.</summary>
    public string CurrentEnglish { get; init; } = "";

    /// <summary>Which correction step caused this drift.</summary>
    public int CorrectionStep { get; init; }

    /// <summary>Character-level diff showing what changed in the Chinese.</summary>
    public List<CharDiffSpan> ChineseDiff { get; init; } = new();

    /// <summary>Compact readable diff string (e.g., "至道無[-雅催焦择][+難唯嫌揀擇]").</summary>
    public string DiffSummary { get; init; } = "";
}

/// <summary>
/// Summary of translation drift for an entire edition.
/// </summary>
public sealed class TranslationDriftReport
{
    public int TotalSegments { get; init; }
    public int TranslatedSegments { get; init; }
    public int CurrentSegments { get; init; }
    public int StaleSegments { get; init; }
    public int UntranslatedSegments { get; init; }

    /// <summary>Percentage of translated segments that are current (not stale).</summary>
    public double CurrentPercent => TranslatedSegments > 0
        ? CurrentSegments * 100.0 / TranslatedSegments
        : 0;

    /// <summary>Individual drift entries for stale segments.</summary>
    public List<TranslationDriftEntry> Drifts { get; init; } = new();
}

/// <summary>
/// Detects which translations have drifted from the current Chinese reading
/// by cross-referencing the correction log against translation state.
/// </summary>
public static class TranslationDriftService
{
    /// <summary>
    /// Computes drift for all loci that have both a correction history
    /// and an existing English translation.
    /// </summary>
    /// <param name="corrections">Parsed correction log entries (ordered by step).</param>
    /// <param name="workingText">Current working text lines (locus → text).</param>
    /// <param name="translations">Current English translations keyed by locus. Null/empty = untranslated.</param>
    /// <param name="translatedAtStep">For each locus, the correction step at which the translation was last updated. Null = translated before any corrections (step 0).</param>
    public static TranslationDriftReport ComputeDrift(
        List<CorrectionEntry> corrections,
        List<(string Locus, string Text)> workingText,
        Dictionary<string, string> translations,
        Dictionary<string, int>? translatedAtStep = null)
    {
        translatedAtStep ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int totalSegments = workingText.Count;
        var drifts = new List<TranslationDriftEntry>();

        // Build a map of locus → latest correction step that changed it
        var latestCorrectionAtLocus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var correctionAtLocus = new Dictionary<string, CorrectionEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in corrections)
        {
            latestCorrectionAtLocus[c.Locus] = c.Index + 1; // 1-based step
            correctionAtLocus[c.Locus] = c;
        }

        int translated = 0;
        int current = 0;
        int stale = 0;

        foreach (var (locus, _) in workingText)
        {
            translations.TryGetValue(locus, out var en);
            bool hasTranslation = !string.IsNullOrWhiteSpace(en);

            if (!hasTranslation) continue;
            translated++;

            // Check if this locus was corrected AFTER the translation was made
            if (!latestCorrectionAtLocus.TryGetValue(locus, out var lastCorrStep))
            {
                // No corrections at this locus — translation is current
                current++;
                continue;
            }

            int translationStep = translatedAtStep.GetValueOrDefault(locus, 0);

            if (translationStep >= lastCorrStep)
            {
                // Translation was updated after (or at) the last correction — current
                current++;
                continue;
            }

            // Stale: the Chinese changed after the translation was made
            stale++;

            var correction = correctionAtLocus[locus];
            var diff = CjkCharDiff.Diff(correction.Before, correction.After);

            drifts.Add(new TranslationDriftEntry
            {
                Locus = locus,
                ChineseBefore = correction.Before,
                ChineseAfter = correction.After,
                CurrentEnglish = en!,
                CorrectionStep = lastCorrStep,
                ChineseDiff = diff,
                DiffSummary = CjkCharDiff.FormatCompact(diff),
            });
        }

        return new TranslationDriftReport
        {
            TotalSegments = totalSegments,
            TranslatedSegments = translated,
            CurrentSegments = current,
            StaleSegments = stale,
            UntranslatedSegments = totalSegments - translated,
            Drifts = drifts,
        };
    }
}
