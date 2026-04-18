// Services/TranslationDiffLogService.cs
// Parses and reconstructs from translation-diff-log.md files.
// Each entry records a Chinese correction + its corresponding English
// retranslation, enabling full bilingual time-travel through the
// editorial process.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReadZen.App.Services;

/// <summary>
/// A single entry in the translation diff log: one Chinese change + its
/// English retranslation, keyed to a correction step.
/// </summary>
public sealed class TranslationDiffEntry
{
    public int Step { get; set; }
    public string Locus { get; set; } = "";
    public string ChineseBefore { get; set; } = "";
    public string ChineseAfter { get; set; } = "";
    public string EnglishBefore { get; set; } = "";
    public string EnglishAfter { get; set; } = "";
    public string Basis { get; set; } = "";
}

/// <summary>
/// The reconstructed bilingual state at a given correction step.
/// </summary>
public sealed class BilingualTextState
{
    public List<(string Locus, string Chinese, string English)> Lines { get; set; } = new();
    public string? HighlightLocus { get; set; }
    public int Step { get; set; }
    public int TotalSteps { get; set; }

    public string ToChineseDisplay() =>
        string.Join("\n", Lines.Select(l => l.Chinese));

    public string ToEnglishDisplay() =>
        string.Join("\n", Lines.Select(l => l.English));
}

/// <summary>
/// Parses translation-diff-log.md and reconstructs the bilingual text
/// at any correction step. Works alongside CorrectionLogService: the
/// correction log tracks Chinese changes, the diff log tracks English
/// retranslations triggered by those changes.
/// </summary>
public static class TranslationDiffLogService
{
    private static readonly Regex TableRowRegex = new(
        @"^\|\s*(\d+)\s*\|\s*`([^`]+)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*([^|]*)\|",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a translation-diff-log.md file into ordered entries.
    /// </summary>
    public static List<TranslationDiffEntry> ParseDiffLog(string logPath)
    {
        if (!File.Exists(logPath)) return new();

        var entries = new List<TranslationDiffEntry>();
        foreach (var line in File.ReadAllLines(logPath))
        {
            var m = TableRowRegex.Match(line);
            if (!m.Success) continue;

            entries.Add(new TranslationDiffEntry
            {
                Step = int.TryParse(m.Groups[1].Value, out var s) ? s : 0,
                Locus = m.Groups[2].Value.Trim(),
                ChineseBefore = m.Groups[3].Value.Trim(),
                ChineseAfter = m.Groups[4].Value.Trim(),
                EnglishBefore = m.Groups[5].Value.Trim(),
                EnglishAfter = m.Groups[6].Value.Trim(),
                Basis = m.Groups[7].Value.Trim(),
            });
        }

        return entries;
    }

    /// <summary>
    /// Reconstructs the full bilingual text at a given correction step.
    /// Uses the Chinese working text (from CorrectionLogService) + the
    /// translation diff log to produce both languages at any point.
    ///
    /// Step 0 = initial state (raw OCR Chinese + initial translation).
    /// Step N = after N corrections, with English retranslations applied
    /// for each correction that had one.
    /// </summary>
    public static BilingualTextState ReconstructAtStep(
        List<(string Locus, string Text)> currentChineseText,
        List<CorrectionEntry> corrections,
        List<TranslationDiffEntry> translationDiffs,
        int step)
    {
        step = Math.Clamp(step, 0, corrections.Count);

        // Reconstruct Chinese at this step (reuse existing logic)
        var chineseState = CorrectionLogService.ReconstructAtStep(
            currentChineseText, corrections, step);

        // Build a lookup of locus → Chinese text at this step
        var chineseByLocus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (locus, text) in chineseState.Lines)
            chineseByLocus[locus] = text;

        // Reconstruct English: start from the latest English for each locus,
        // then un-apply translation diffs that happened after this step.
        //
        // The "latest English" for a locus is the EnglishAfter of the
        // highest-step diff entry for that locus.
        var englishByLocus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // First pass: collect the final English for each locus
        foreach (var diff in translationDiffs)
        {
            englishByLocus[diff.Locus] = diff.EnglishAfter;
        }

        // Second pass: un-apply diffs that are beyond the current step
        // (process in reverse order, same as Chinese reconstruction)
        for (int i = translationDiffs.Count - 1; i >= 0; i--)
        {
            var diff = translationDiffs[i];
            if (diff.Step > step)
            {
                // This diff hasn't happened yet at this step — revert to before
                if (string.IsNullOrEmpty(diff.EnglishBefore) && diff.Step > 0)
                {
                    // Step 0 entries with empty "before" mean the locus had
                    // no translation yet — remove it
                    englishByLocus.Remove(diff.Locus);
                }
                else
                {
                    englishByLocus[diff.Locus] = diff.EnglishBefore;
                }
            }
        }

        // Build the bilingual output
        var lines = new List<(string, string, string)>();
        foreach (var (locus, zhText) in chineseState.Lines)
        {
            var en = englishByLocus.GetValueOrDefault(locus, "");
            lines.Add((locus, zhText, en));
        }

        return new BilingualTextState
        {
            Lines = lines,
            HighlightLocus = chineseState.HighlightLocus,
            Step = step,
            TotalSteps = corrections.Count,
        };
    }
}
