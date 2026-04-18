// Services/CorrectionLogService.cs
// Parses correction-log.md files from critical edition provenance chains
// into structured data for time-travel scrubbing.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReadZen.App.Services;

/// <summary>
/// A single correction entry from a critical edition's correction log.
/// Represents one OCR fix or editorial change at a specific locus.
/// </summary>
public sealed class CorrectionEntry
{
    public int Index { get; set; }
    public string Date { get; set; } = "";
    public string Locus { get; set; } = "";
    public string ChangeType { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Basis { get; set; } = "";
    public string Status { get; set; } = "";

    // Image evidence coordinates (populated by edition agent Phase 6+).
    // When set, the UI shows a "View Evidence" button.

    /// <summary>PDF filename or path for the witness page image.</summary>
    public string? EvidencePdf { get; set; }

    /// <summary>Zero-based page number in the evidence PDF.</summary>
    public int? EvidencePage { get; set; }

    /// <summary>Region coordinates (0.0-1.0) within the page: X, Y, Width, Height.</summary>
    public double? EvidenceRegionX { get; set; }
    public double? EvidenceRegionY { get; set; }
    public double? EvidenceRegionWidth { get; set; }
    public double? EvidenceRegionHeight { get; set; }

    /// <summary>Whether this correction has image evidence coordinates.</summary>
    public bool HasImageEvidence =>
        !string.IsNullOrWhiteSpace(EvidencePdf) && EvidencePage.HasValue;
}

/// <summary>
/// The full state of a working text at a given correction step.
/// </summary>
public sealed class CorrectionTextState
{
    /// <summary>Ordered lines of the text, keyed by locus ID.</summary>
    public List<(string Locus, string Text)> Lines { get; set; } = new();

    /// <summary>The locus that was most recently changed (for highlighting).</summary>
    public string? HighlightLocus { get; set; }

    /// <summary>How many corrections have been applied (0 = raw OCR).</summary>
    public int CorrectionCount { get; set; }

    /// <summary>Total corrections available.</summary>
    public int TotalCorrections { get; set; }

    /// <summary>Renders the text as a single string for display.</summary>
    public string ToDisplayText()
    {
        return string.Join("\n", Lines.Select(l => l.Text));
    }
}

/// <summary>
/// Parses correction-log.md + the working text file and enables time-travel
/// scrubbing through the correction history. Given a step N (0..total), it
/// reconstructs what the text looked like after N corrections were applied.
/// </summary>
public sealed class CorrectionLogService
{
    private static readonly Regex TableRowRegex = new(
        @"^\|\s*(\S+)\s*\|\s*`([^`]+)`\s*\|\s*([^|]+)\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*([^|]+)\|\s*(\w+)\s*\|",
        RegexOptions.Compiled);

    private static readonly Regex LocusLineRegex = new(
        @"^\[([^\]]+)\]\s*(.*)",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a correction-log.md file into ordered entries.
    /// </summary>
    public static List<CorrectionEntry> ParseCorrectionLog(string logPath)
    {
        if (!File.Exists(logPath)) return new();

        var entries = new List<CorrectionEntry>();
        int idx = 0;

        foreach (var line in File.ReadAllLines(logPath))
        {
            var m = TableRowRegex.Match(line);
            if (!m.Success) continue;

            entries.Add(new CorrectionEntry
            {
                Index = idx++,
                Date = m.Groups[1].Value.Trim(),
                Locus = m.Groups[2].Value.Trim(),
                ChangeType = m.Groups[3].Value.Trim(),
                Before = m.Groups[4].Value.Trim(),
                After = m.Groups[5].Value.Trim(),
                Basis = m.Groups[6].Value.Trim(),
                Status = m.Groups[7].Value.Trim(),
            });
        }

        return entries;
    }

    /// <summary>
    /// Parses a working text file with [locus] prefixes into an ordered list.
    /// </summary>
    public static List<(string Locus, string Text)> ParseWorkingText(string textPath)
    {
        if (!File.Exists(textPath)) return new();

        var lines = new List<(string, string)>();
        foreach (var raw in File.ReadAllLines(textPath))
        {
            var m = LocusLineRegex.Match(raw);
            if (m.Success)
                lines.Add((m.Groups[1].Value, m.Groups[2].Value));
        }
        return lines;
    }

    /// <summary>
    /// Reconstructs the text state at a given correction step.
    /// Step 0 = raw OCR (all corrections un-applied).
    /// Step N = first N corrections applied.
    /// Step total = current fully-corrected text.
    /// </summary>
    public static CorrectionTextState ReconstructAtStep(
        List<(string Locus, string Text)> currentText,
        List<CorrectionEntry> corrections,
        int step)
    {
        step = Math.Clamp(step, 0, corrections.Count);

        // Start from the current (fully corrected) text
        var textByLocus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (locus, text) in currentText)
            textByLocus[locus] = text;

        // Un-apply corrections from (total) down to (step+1) to reconstruct
        // the state after exactly 'step' corrections.
        for (int i = corrections.Count - 1; i >= step; i--)
        {
            var c = corrections[i];
            if (textByLocus.ContainsKey(c.Locus))
                textByLocus[c.Locus] = c.Before;
        }

        // Rebuild ordered lines from the original ordering
        var result = new List<(string, string)>();
        foreach (var (locus, _) in currentText)
        {
            result.Add((locus, textByLocus.GetValueOrDefault(locus, "")));
        }

        return new CorrectionTextState
        {
            Lines = result,
            HighlightLocus = step > 0 && step <= corrections.Count
                ? corrections[step - 1].Locus
                : null,
            CorrectionCount = step,
            TotalCorrections = corrections.Count,
        };
    }
}
