// Services/ForensicProvenanceServices.cs
// Parsers for the 4 forensic provenance log types produced by critical
// edition agents. Each log lives at provenance/{slug}/process/*.md.
// All are read-only — logs are produced by the editorial agent, not the app.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ReadZen.App.Services;

// ═══════════════════════════════════════════════════════════════════
// Models
// ═══════════════════════════════════════════════════════════════════

public sealed class OcrConsensusEntry
{
    public string Locus { get; set; } = "";
    public string Tesseract { get; set; } = "";
    public string RapidOCR { get; set; } = "";
    public string PaddleOCR { get; set; } = "";
    public string EasyOCR { get; set; } = "";
    public string Agreement { get; set; } = "";
    public string Adopted { get; set; } = "";
    public string Basis { get; set; } = "";
}

public sealed class RejectedReadingEntry
{
    public string Locus { get; set; } = "";
    public string Rejected { get; set; } = "";
    public string Source { get; set; } = "";
    public string Adopted { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Date { get; set; } = "";
}

public sealed class TranslationReasoningEntry
{
    public int Step { get; set; }
    public string Locus { get; set; } = "";
    public string Chinese { get; set; } = "";
    public string ChosenEnglish { get; set; } = "";
    public string AlternativesConsidered { get; set; } = "";
    public string Reasoning { get; set; } = "";
}

public sealed class CharacterProvenanceEntry
{
    public string Locus { get; set; } = "";
    public int Position { get; set; }
    public string Character { get; set; } = "";
    public string Source { get; set; } = "";
    public string Confidence { get; set; } = "";
    public string Witness { get; set; } = "";
}

// ═══════════════════════════════════════════════════════════════════
// Parsers
// ═══════════════════════════════════════════════════════════════════

public static class OcrConsensusLogService
{
    private static readonly Regex Row = new(
        @"^\|\s*`([^`]+)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*([^|]+)\|\s*`([^`]*)`\s*\|\s*([^|]+)\|",
        RegexOptions.Compiled);

    public static List<OcrConsensusEntry> Parse(string path)
    {
        if (!File.Exists(path)) return new();
        var entries = new List<OcrConsensusEntry>();
        foreach (var line in File.ReadAllLines(path))
        {
            var m = Row.Match(line);
            if (!m.Success) continue;
            entries.Add(new OcrConsensusEntry
            {
                Locus = m.Groups[1].Value.Trim(),
                Tesseract = m.Groups[2].Value.Trim(),
                RapidOCR = m.Groups[3].Value.Trim(),
                PaddleOCR = m.Groups[4].Value.Trim(),
                EasyOCR = m.Groups[5].Value.Trim(),
                Agreement = m.Groups[6].Value.Trim(),
                Adopted = m.Groups[7].Value.Trim(),
                Basis = m.Groups[8].Value.Trim(),
            });
        }
        return entries;
    }
}

public static class RejectedReadingsLogService
{
    private static readonly Regex Row = new(
        @"^\|\s*`([^`]+)`\s*\|\s*`([^`]*)`\s*\|\s*([^|]+)\|\s*`([^`]*)`\s*\|\s*([^|]+)\|\s*(\S+)\s*\|",
        RegexOptions.Compiled);

    public static List<RejectedReadingEntry> Parse(string path)
    {
        if (!File.Exists(path)) return new();
        var entries = new List<RejectedReadingEntry>();
        foreach (var line in File.ReadAllLines(path))
        {
            var m = Row.Match(line);
            if (!m.Success) continue;
            entries.Add(new RejectedReadingEntry
            {
                Locus = m.Groups[1].Value.Trim(),
                Rejected = m.Groups[2].Value.Trim(),
                Source = m.Groups[3].Value.Trim(),
                Adopted = m.Groups[4].Value.Trim(),
                Reason = m.Groups[5].Value.Trim(),
                Date = m.Groups[6].Value.Trim(),
            });
        }
        return entries;
    }
}

public static class TranslationReasoningLogService
{
    private static readonly Regex Row = new(
        @"^\|\s*(\d+)\s*\|\s*`([^`]+)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]*)`\s*\|\s*([^|]+)\|\s*([^|]+)\|",
        RegexOptions.Compiled);

    public static List<TranslationReasoningEntry> Parse(string path)
    {
        if (!File.Exists(path)) return new();
        var entries = new List<TranslationReasoningEntry>();
        foreach (var line in File.ReadAllLines(path))
        {
            var m = Row.Match(line);
            if (!m.Success) continue;
            entries.Add(new TranslationReasoningEntry
            {
                Step = int.TryParse(m.Groups[1].Value, out var s) ? s : 0,
                Locus = m.Groups[2].Value.Trim(),
                Chinese = m.Groups[3].Value.Trim(),
                ChosenEnglish = m.Groups[4].Value.Trim(),
                AlternativesConsidered = m.Groups[5].Value.Trim(),
                Reasoning = m.Groups[6].Value.Trim(),
            });
        }
        return entries;
    }
}

public static class CharacterProvenanceLogService
{
    private static readonly Regex Row = new(
        @"^\|\s*`([^`]+)`\s*\|\s*(\d+)\s*\|\s*`([^`]*)`\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|",
        RegexOptions.Compiled);

    public static List<CharacterProvenanceEntry> Parse(string path)
    {
        if (!File.Exists(path)) return new();
        var entries = new List<CharacterProvenanceEntry>();
        foreach (var line in File.ReadAllLines(path))
        {
            var m = Row.Match(line);
            if (!m.Success) continue;
            entries.Add(new CharacterProvenanceEntry
            {
                Locus = m.Groups[1].Value.Trim(),
                Position = int.TryParse(m.Groups[2].Value, out var p) ? p : 0,
                Character = m.Groups[3].Value.Trim(),
                Source = m.Groups[4].Value.Trim(),
                Confidence = m.Groups[5].Value.Trim(),
                Witness = m.Groups[6].Value.Trim(),
            });
        }
        return entries;
    }
}
