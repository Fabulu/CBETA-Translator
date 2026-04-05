using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CbetaTranslator.App.ViewModels;

public partial class TranslationTabViewModel : ViewModelBase
{
    // -------------------------
    // Observable state
    // -------------------------

    [ObservableProperty]
    private TranslationEditMode _currentMode = TranslationEditMode.Body;

    [ObservableProperty]
    private string _modeInfoText = "Translation Editor";

    [ObservableProperty]
    private string _quickInfoText = "";

    [ObservableProperty]
    private string _reviewStateText = "Unreviewed";

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private bool _hoverDictionaryEnabled = true;

    [ObservableProperty]
    private bool _isModeHeadEnabled = true;

    [ObservableProperty]
    private bool _isModeBodyEnabled;

    [ObservableProperty]
    private bool _isModeNotesEnabled = true;

    // -------------------------
    // File path state
    // -------------------------
    private string? _origPath;
    private string? _tranPath;

    /// <summary>Current original file path (for Scholar context menu).</summary>
    public string? CurrentOriginalPath => _origPath;

    // -------------------------
    // Projection text (maintained in parallel with editor)
    // -------------------------
    public string CurrentProjection { get; set; } = "";

    // -------------------------
    // Assistant state
    // -------------------------
    public TranslationAssistantSnapshot? LastAssistantSnapshot { get; set; }
    private Func<string, string>? _assistantTitleResolver;

    // -------------------------
    // Events
    // -------------------------
    public event EventHandler<TranslationEditMode>? ModeChanged;
    public event EventHandler? SaveRequested;
    public event EventHandler? RevertRequested;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler? BuildReferenceTmRequested;
    public event EventHandler? ManageTermsRequested;
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? ReviewActionRequested;
    public event EventHandler? NextUnapprovedRequested;

    // -------------------------
    // Status helper
    // -------------------------
    public void Say(string msg) => StatusChanged?.Invoke(this, msg);

    // -------------------------
    // Mode switching
    // -------------------------
    public void SwitchMode(TranslationEditMode mode)
    {
        if (CurrentMode == mode) return;

        CurrentMode = mode;
        UpdateModeInfo();
        UpdateModeButtons();
        ModeChanged?.Invoke(this, mode);
    }

    private void UpdateModeButtons()
    {
        IsModeHeadEnabled = CurrentMode != TranslationEditMode.Head;
        IsModeBodyEnabled = CurrentMode != TranslationEditMode.Body;
        IsModeNotesEnabled = CurrentMode != TranslationEditMode.Notes;
    }

    public void UpdateModeInfo()
    {
        var modeText = CurrentMode switch
        {
            TranslationEditMode.Head => "Head of File",
            TranslationEditMode.Body => "Body of File",
            TranslationEditMode.Notes => "Notes",
            _ => "Translation Editor"
        };

        var fileLabel = string.IsNullOrWhiteSpace(_tranPath)
            ? ""
            : $" - {System.IO.Path.GetFileName(_tranPath)}";

        ModeInfoText = $"{modeText}{fileLabel} - One EN line per block";
    }

    // -------------------------
    // File paths
    // -------------------------
    public void SetCurrentFilePaths(string originalPath, string translatedPath)
    {
        _origPath = originalPath;
        _tranPath = translatedPath;
        UpdateModeInfo();
    }

    // -------------------------
    // Review state display
    // -------------------------
    public void SetCurrentReviewState(string? status, string? reviewer, DateTime? reviewedUtc, SegmentReviewAggregation? agg = null)
    {
        status = (status ?? "").Trim().ToLowerInvariant();

        // Multi-reviewer aggregation display
        if (agg != null && agg.ByReviewer.Count > 1)
        {
            ReviewStateText = FormatAggregatedReview(agg);
            return;
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            ReviewStateText = "Unreviewed";
            return;
        }

        string stateLabel = status switch
        {
            TranslationReviewStatuses.Approved => "Approved",
            TranslationReviewStatuses.NeedsWork => "Needs work",
            TranslationReviewStatuses.Rejected => "Rejected",
            _ => status
        };

        if (!string.IsNullOrWhiteSpace(reviewer) && reviewedUtc.HasValue)
        {
            ReviewStateText = $"{stateLabel} — {reviewer} — {reviewedUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
        else if (!string.IsNullOrWhiteSpace(reviewer))
        {
            ReviewStateText = $"{stateLabel} — {reviewer}";
        }
        else
        {
            ReviewStateText = stateLabel;
        }
    }

    private static string FormatAggregatedReview(SegmentReviewAggregation agg)
    {
        var parts = new List<string>();

        var approved = agg.ApprovedBy.ToList();
        if (approved.Count > 0)
            parts.Add($"Approved ({approved.Count}): {string.Join(", ", approved)}");

        var needsWork = agg.NeedsWorkBy.ToList();
        if (needsWork.Count > 0)
            parts.Add($"Needs work ({needsWork.Count}): {string.Join(", ", needsWork)}");

        var rejected = agg.RejectedBy.ToList();
        if (rejected.Count > 0)
            parts.Add($"Rejected ({rejected.Count}): {string.Join(", ", rejected)}");

        return parts.Count > 0 ? string.Join(" | ", parts) : "Unreviewed";
    }

    // -------------------------
    // Progress stats
    // -------------------------
    public void SetProgressStats(int approved, int needsWork, int total)
    {
        ProgressText = total == 0
            ? ""
            : $"{approved}/{total} approved · {needsWork} needs work";
    }

    // -------------------------
    // Quick info
    // -------------------------
    public void UpdateQuickInfo(string editorText)
    {
        try
        {
            var blocks = ParseProjectionBlocksWithOffsets(editorText);
            int total = blocks.Count;
            int emptyEn = blocks.Count(b => string.IsNullOrWhiteSpace(b.En));
            int untranslated = blocks.Count(b => ShouldJumpToUntranslated(b));

            QuickInfoText = total > 0
                ? $"Blocks: {total}  Empty EN: {emptyEn}  Untranslated: {untranslated}"
                : "";
        }
        catch
        {
            QuickInfoText = "";
        }
    }

    // -------------------------
    // Assistant title resolver
    // -------------------------
    public void SetAssistantTitleResolver(Func<string, string>? resolver)
    {
        _assistantTitleResolver = resolver;
    }

    public string ResolveAssistantTitle(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath))
            return "";

        if (_assistantTitleResolver != null)
        {
            var resolved = _assistantTitleResolver(relPath);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
        }

        return relPath ?? "";
    }

    // -------------------------
    // Event raisers (called from code-behind)
    // -------------------------
    public void RaiseSaveRequested() => SaveRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseRevertRequested() => RevertRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseBuildReferenceTmRequested() => BuildReferenceTmRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseManageTermsRequested() => ManageTermsRequested?.Invoke(this, EventArgs.Empty);
    public void RaiseNextUnapprovedRequested() => NextUnapprovedRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseReviewAction(string action) => ReviewActionRequested?.Invoke(this, action);

    public void RaiseNavigationRequested(NavigationRequest request)
        => NavigationRequested?.Invoke(this, request);

    // -------------------------
    // Clear
    // -------------------------
    public void Clear()
    {
        CurrentProjection = "";
        _origPath = null;
        _tranPath = null;
        LastAssistantSnapshot = null;
        SetCurrentReviewState(null, null, null, null);
        UpdateModeInfo();
        UpdateModeButtons();
        QuickInfoText = "";
    }

    // -------------------------
    // Set mode projection (non-editor state)
    // -------------------------
    public void SetModeProjectionState(TranslationEditMode mode, string projectionText)
    {
        CurrentMode = mode;
        CurrentProjection = projectionText ?? "";
        UpdateModeInfo();
        UpdateModeButtons();
    }

    // -------------------------
    // Static projection parsing (shared with code-behind)
    // -------------------------
    public sealed class ProjectionBlockInfo
    {
        public int BlockNumber { get; set; }
        public string Zh { get; set; } = "";
        public string En { get; set; } = "";
        public int BlockStartOffset { get; set; }
        public int BlockEndOffsetExclusive { get; set; }
        public int EnValueStartOffset { get; set; }
        public int EnValueLength { get; set; }
    }

    public static List<ProjectionBlockInfo> ParseProjectionBlocksWithOffsets(string text)
    {
        text ??= "";

        var rx = new Regex(
            @"(?m)^(?<hdr><(?<num>\d+)>)\s*\r?\n" +
            @"ZH:\s?(?<zh>[^\r\n]*)\r?\n" +
            @"EN:\s?(?<en>[^\r\n]*)",
            RegexOptions.Compiled);

        var ms = rx.Matches(text);
        var list = new List<ProjectionBlockInfo>(ms.Count);

        foreach (Match m in ms)
        {
            if (!m.Success) continue;

            if (!int.TryParse(m.Groups["num"].Value, out int num))
                continue;

            var enGroup = m.Groups["en"];
            var blockStart = m.Index;
            var blockEnd = m.Index + m.Length;

            list.Add(new ProjectionBlockInfo
            {
                BlockNumber = num,
                Zh = m.Groups["zh"].Value,
                En = enGroup.Value,
                BlockStartOffset = blockStart,
                BlockEndOffsetExclusive = blockEnd,
                EnValueStartOffset = enGroup.Index,
                EnValueLength = enGroup.Length
            });
        }

        for (int i = 0; i < list.Count; i++)
        {
            int end = (i + 1 < list.Count) ? list[i + 1].BlockStartOffset : text.Length;
            list[i].BlockEndOffsetExclusive = end;
        }

        return list;
    }

    public static int FindBlockIndexAtOrAfterCaret(List<ProjectionBlockInfo> blocks, int caretOffset)
    {
        if (blocks.Count == 0) return -1;

        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            if (caretOffset >= b.BlockStartOffset && caretOffset < b.BlockEndOffsetExclusive)
                return i;
        }

        if (caretOffset < blocks[0].BlockStartOffset)
            return 0;

        return blocks.Count - 1;
    }

    public static bool ShouldIncludeForCopy(ProjectionBlockInfo block, bool requireUntranslated)
    {
        if (block == null) return false;
        if (IsSkippableForCopyOrJump(block)) return false;
        if (requireUntranslated && !string.IsNullOrWhiteSpace(block.En)) return false;
        return true;
    }

    public static bool ShouldJumpToUntranslated(ProjectionBlockInfo block)
    {
        if (block == null) return false;
        if (IsSkippableForCopyOrJump(block)) return false;
        return string.IsNullOrWhiteSpace(block.En);
    }

    public static bool IsSkippableForCopyOrJump(ProjectionBlockInfo block)
    {
        var zh = block.Zh ?? "";
        var en = block.En ?? "";

        if (string.IsNullOrWhiteSpace(zh) && string.IsNullOrWhiteSpace(en))
            return true;

        if (!ContainsChineseChar(zh))
            return true;

        return false;
    }

    public static bool ContainsChineseChar(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        foreach (char ch in s)
        {
            if ((ch >= '\u3400' && ch <= '\u4DBF') ||
                (ch >= '\u4E00' && ch <= '\u9FFF') ||
                (ch >= '\uF900' && ch <= '\uFAFF'))
            {
                return true;
            }
        }

        return false;
    }

    public static void ValidateEnglish(string en, int blockNumber)
    {
        en ??= "";

        if (en.Contains('<') || en.Contains('>'))
            throw new InvalidOperationException($"Block <{blockNumber}> EN contains '<' or '>' which is not allowed.");

        for (int i = 0; i < en.Length; i++)
        {
            char ch = en[i];

            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < en.Length && char.IsLowSurrogate(en[i + 1]))
                {
                    i++;
                    continue;
                }

                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (unpaired high surrogate U+{((int)ch):X4}) at position {i + 1}.");
            }

            if (char.IsLowSurrogate(ch))
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (unpaired low surrogate U+{((int)ch):X4}) at position {i + 1}.");
            }

            bool ok =
                ch == '\t' ||
                ch == '\n' ||
                ch == '\r' ||
                (ch >= 0x20 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD);

            if (!ok)
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (U+{((int)ch):X4}) at position {i + 1}.");
            }
        }
    }

    public static string BuildPrompt(string selectedProjection)
    {
        return
$@"You are translating a CBETA projection block.

STRICT RULES:
- Edit ONLY EN: lines.
- Keep <n> and all ZH: lines unchanged.
- Keep the same number of EN[n] lines as ZH[n] lines.
- Do NOT merge lines.
- Do NOT split lines.
- Do NOT add commentary.
- Do NOT add or remove blocks.
- Do NOT use angle brackets < or > in EN text.
- Output ONLY one markdown code block.
- Translate common Zen honorifics/titles like 「和尚」 as ""the master"" (or ""Venerable"") in EN, not left as Chinese.

```markdown
{selectedProjection}
```";
    }

    public static string ExtractCodeBlockOrRaw(string text)
    {
        var m = Regex.Match(text, @"```(?:markdown|md|text)?\s*(?<x>[\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["x"].Value.Trim() : text.Trim();
    }

    // -------------------------
    // Assistant data formatting
    // -------------------------
    public static string BuildTmEditorText(TranslationTmMatch match, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Math.Round(match.Score)}%  [{match.ReviewStatus}]");
        if (!string.IsNullOrWhiteSpace(title))
            sb.AppendLine(title);
        sb.AppendLine($"ZH: {match.SourceText}");
        sb.AppendLine($"EN: {match.TargetText}");
        if (!string.IsNullOrWhiteSpace(match.Translator))
            sb.AppendLine($"Translator: {match.Translator}");
        return sb.ToString().TrimEnd();
    }

    public static string BuildTermEditorText(TermHit t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {t.SourceTerm}");
        sb.AppendLine($"Preferred: {t.PreferredTarget}");
        if (t.AlternateTargets != null && t.AlternateTargets.Count > 0)
            sb.AppendLine($"Alternates: {string.Join(", ", t.AlternateTargets)}");
        if (!string.IsNullOrWhiteSpace(t.Status))
            sb.AppendLine($"Status: {t.Status}");
        if (!string.IsNullOrWhiteSpace(t.Note))
            sb.AppendLine($"Note: {t.Note}");
        return sb.ToString().TrimEnd();
    }

    public sealed record TextRange(int Start, int Length);

    public static IReadOnlyList<TextRange> BuildTmHighlightRanges(string wholeText, string suggestionZh, string currentZh)
    {
        int zhLineStart = wholeText.IndexOf("ZH: ", StringComparison.Ordinal);
        if (zhLineStart < 0)
            return Array.Empty<TextRange>();

        zhLineStart += 4;
        int zhLineEnd = wholeText.IndexOf('\n', zhLineStart);
        if (zhLineEnd < 0)
            zhLineEnd = wholeText.Length;

        return BuildSharedChineseRangesInWholeText(
            wholeText,
            zhLineStart,
            zhLineEnd - zhLineStart,
            suggestionZh,
            currentZh);
    }

    public static IReadOnlyList<TextRange> BuildSingleLineChineseHighlightRanges(string wholeText, string lineText, string currentZh)
    {
        int lineEnd = wholeText.IndexOf('\n');
        if (lineEnd < 0)
            lineEnd = wholeText.Length;

        int colon = wholeText.IndexOf(": ", StringComparison.Ordinal);
        int contentStart = colon >= 0 ? colon + 2 : 0;
        int contentLength = Math.Max(0, lineEnd - contentStart);

        return BuildSharedChineseRangesInWholeText(
            wholeText,
            contentStart,
            contentLength,
            lineText,
            currentZh);
    }

    public static IReadOnlyList<TextRange> BuildSharedChineseRangesInWholeText(
        string wholeText,
        int targetStart,
        int targetLength,
        string suggestionZh,
        string currentZh)
    {
        var result = new List<TextRange>();

        if (string.IsNullOrWhiteSpace(wholeText) ||
            string.IsNullOrWhiteSpace(suggestionZh) ||
            string.IsNullOrWhiteSpace(currentZh) ||
            targetLength <= 0)
            return result;

        var localRanges = CjkMatchNormalizer.FindSharedRawRanges(
            suggestionZh,
            currentZh,
            minPhraseLen: 2);
        foreach (var r in localRanges)
        {
            int absStart = targetStart + r.Start;
            if (absStart < 0 || absStart >= wholeText.Length)
                continue;

            int len = Math.Min(r.Length, wholeText.Length - absStart);
            if (len > 0)
                result.Add(new TextRange(absStart, len));
        }

        return MergeRanges(result);
    }

    public static IReadOnlyList<TextRange> MergeRanges(List<TextRange> ranges)
    {
        if (ranges.Count == 0)
            return ranges;

        var ordered = ranges.OrderBy(r => r.Start).ThenBy(r => r.Length).ToList();
        var merged = new List<TextRange> { ordered[0] };

        for (int i = 1; i < ordered.Count; i++)
        {
            var last = merged[^1];
            var cur = ordered[i];

            int lastEnd = last.Start + last.Length;
            int curEnd = cur.Start + cur.Length;

            if (cur.Start <= lastEnd)
            {
                merged[^1] = new TextRange(last.Start, Math.Max(lastEnd, curEnd) - last.Start);
            }
            else
            {
                merged.Add(cur);
            }
        }

        return merged;
    }

    public static string FirstChars(string s, int count)
    {
        if (string.IsNullOrEmpty(s) || count <= 0)
            return "";
        return s.Length <= count ? s : s[..count];
    }

    public static string LastChars(string s, int count)
    {
        if (string.IsNullOrEmpty(s) || count <= 0)
            return "";
        return s.Length <= count ? s : s[^count..];
    }

    // -------------------------
    // Segment range finding (for highlighting)
    // -------------------------
    public static bool TryFindSegmentRange(
        string docText,
        string segmentText,
        IReadOnlyList<string>? signalTerms,
        string? tmSourceSignal,
        int? preferredOffset,
        int? preferredOccurrenceHint,
        string? anchorTextSignal,
        out int start,
        out int length)
    {
        start = -1;
        length = 0;
        if (string.IsNullOrEmpty(docText) || string.IsNullOrEmpty(segmentText))
            return false;

        var candidates = new List<(int start, int length)>();
        var seen = new HashSet<(int start, int length)>();

        int from = 0;
        while (from < docText.Length)
        {
            int idx = docText.IndexOf(segmentText, from, StringComparison.Ordinal);
            if (idx < 0) break;
            var c = (idx, segmentText.Length);
            if (seen.Add(c)) candidates.Add(c);
            from = idx + 1;
        }

        bool cjkSeg = CjkMatchNormalizer.ContainsCjk(segmentText);
        var nDoc = cjkSeg ? CjkMatchNormalizer.NormalizeWithMap(docText) : null;
        string nSeg = cjkSeg ? CjkMatchNormalizer.Normalize(segmentText) : "";
        if (cjkSeg && !string.IsNullOrEmpty(nSeg) && !string.IsNullOrEmpty(nDoc!.Normalized))
        {
            int nFrom = 0;
            while (nFrom < nDoc.Normalized.Length)
            {
                int nIdx = nDoc.Normalized.IndexOf(nSeg, nFrom, StringComparison.Ordinal);
                if (nIdx < 0) break;

                int rawStart = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, nIdx);
                int rawEnd = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, nIdx + nSeg.Length);
                if (rawEnd > rawStart)
                {
                    var c = (rawStart, rawEnd - rawStart);
                    if (seen.Add(c)) candidates.Add(c);
                }

                nFrom = nIdx + 1;
            }
        }

        if (candidates.Count == 0)
            return false;

        int AnchorSharedScore(string segRaw, string? signal)
        {
            if (string.IsNullOrWhiteSpace(signal))
                return 0;

            if (CjkMatchNormalizer.ContainsCjk(segRaw) || CjkMatchNormalizer.ContainsCjk(signal))
            {
                int shared = 0;
                var ranges = CjkMatchNormalizer.FindSharedRawRanges(segRaw, signal, minPhraseLen: 2);
                for (int i = 0; i < ranges.Count; i++)
                    shared += Math.Max(1, ranges[i].Length);
                return shared;
            }

            return segRaw.Contains(signal, StringComparison.Ordinal) ? Math.Max(2, signal.Length) : 0;
        }

        (int score, int tmSharedTotal, int termSignalHits, int anchorSharedTotal, int proximity) Score((int start, int length) c)
        {
            int docLen = docText.Length;
            int segStart = Math.Clamp(c.start, 0, Math.Max(0, docLen - 1));
            int segEnd = Math.Clamp(segStart + c.length, 0, docLen);
            if (segEnd <= segStart) return (int.MinValue / 4, 0, 0, 0, int.MaxValue);

            string segRaw = docText.Substring(segStart, segEnd - segStart);
            int score = 0;
            int tmSharedTotal = 0;
            int termSignalHits = 0;

            if (signalTerms != null)
            {
                foreach (var term in signalTerms)
                {
                    if (string.IsNullOrWhiteSpace(term)) continue;
                    if (!CjkMatchNormalizer.ContainsCjk(term))
                    {
                        if (segRaw.Contains(term, StringComparison.Ordinal))
                        {
                            score += 4;
                            termSignalHits++;
                        }
                        continue;
                    }

                    string nTerm = CjkMatchNormalizer.Normalize(term);
                    if (string.IsNullOrEmpty(nTerm)) continue;
                    string nSegRaw = CjkMatchNormalizer.Normalize(segRaw);
                    if (nSegRaw.Contains(nTerm, StringComparison.Ordinal))
                    {
                        score += 6;
                        termSignalHits++;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(tmSourceSignal))
            {
                var shared = CjkMatchNormalizer.FindSharedRawRanges(segRaw, tmSourceSignal, minPhraseLen: 2);
                foreach (var r in shared)
                {
                    int s = Math.Max(1, r.Length);
                    score += s;
                    tmSharedTotal += s;
                }
            }

            int anchorSharedTotal = AnchorSharedScore(segRaw, anchorTextSignal);
            score += anchorSharedTotal;

            int proximity = preferredOffset.HasValue ? Math.Abs(segStart - preferredOffset.Value) : int.MaxValue;
            return (score, tmSharedTotal, termSignalHits, anchorSharedTotal, proximity);
        }

        var orderedByStart = candidates
            .OrderBy(c => c.start)
            .ToList();

        int OccurrenceProximity((int start, int length) c)
        {
            if (!preferredOccurrenceHint.HasValue)
                return int.MaxValue;

            for (int i = 0; i < orderedByStart.Count; i++)
            {
                if (orderedByStart[i].start == c.start && orderedByStart[i].length == c.length)
                    return Math.Abs(i - preferredOccurrenceHint.Value);
            }

            return int.MaxValue;
        }

        var best = candidates
            .Select(c => (candidate: c, key: Score(c)))
            .OrderByDescending(x => x.key.score)
            .ThenByDescending(x => x.key.tmSharedTotal)
            .ThenByDescending(x => x.key.termSignalHits)
            .ThenByDescending(x => x.key.anchorSharedTotal)
            .ThenBy(x => OccurrenceProximity(x.candidate))
            .ThenBy(x => x.key.proximity)
            .ThenBy(x => x.candidate.start)
            .First().candidate;

        start = best.start;
        length = best.length;
        return true;
    }

    public static void AddTermOccurrencesInSegment(
        List<(int Start, int Length)> ranges,
        string docText,
        int segmentStart,
        int segmentLength,
        string term)
    {
        if (string.IsNullOrWhiteSpace(term) || segmentLength <= 0)
            return;

        int segmentEnd = Math.Min(docText.Length, segmentStart + segmentLength);
        if (segmentStart < 0 || segmentStart >= segmentEnd)
            return;

        if (!CjkMatchNormalizer.ContainsCjk(term))
        {
            int from = segmentStart;
            while (from < segmentEnd)
            {
                int max = segmentEnd - from;
                if (max <= 0) break;
                int idx = docText.IndexOf(term, from, max, StringComparison.Ordinal);
                if (idx < 0) break;
                ranges.Add((idx, term.Length));
                from = idx + 1;
            }
            return;
        }

        string segmentRaw = docText.Substring(segmentStart, segmentEnd - segmentStart);
        var nSeg = CjkMatchNormalizer.NormalizeWithMap(segmentRaw);
        string nTerm = CjkMatchNormalizer.Normalize(term);
        if (string.IsNullOrEmpty(nSeg.Normalized) || string.IsNullOrEmpty(nTerm))
            return;

        int fromNorm = 0;
        while (fromNorm < nSeg.Normalized.Length)
        {
            int nIdx = nSeg.Normalized.IndexOf(nTerm, fromNorm, StringComparison.Ordinal);
            if (nIdx < 0) break;

            int rawStartLocal = CjkMatchNormalizer.RawIndexFromNormalizedPos(nSeg, nIdx);
            int rawEndLocal = CjkMatchNormalizer.RawIndexFromNormalizedPos(nSeg, nIdx + nTerm.Length);
            if (rawEndLocal > rawStartLocal)
                ranges.Add((segmentStart + rawStartLocal, rawEndLocal - rawStartLocal));

            fromNorm = nIdx + 1;
        }
    }
}
