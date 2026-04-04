using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CbetaTranslator.App.ViewModels;

public partial class ReadableTabViewModel : ViewModelBase
{
    // -------------------------
    // Observable state
    // -------------------------

    [ObservableProperty]
    private bool _isEmptyState = true;

    [ObservableProperty]
    private bool _isZenText;

    [ObservableProperty]
    private bool _isZenEnabled;

    [ObservableProperty]
    private bool _hoverDictionaryEnabled = true;

    [ObservableProperty]
    private string _defaultResp = "";

    [ObservableProperty]
    private string? _currentRelPathForZen;

    [ObservableProperty]
    private bool _notesPanelVisible;

    [ObservableProperty]
    private bool _studyPanelVisible;

    /// <summary>Holds the last study panel snapshot so it can be re-rendered when the panel is toggled on.</summary>
    public TranslationAssistantSnapshot? LastStudySnapshot { get; set; }

    [ObservableProperty]
    private string _notesHeaderText = "Note";

    [ObservableProperty]
    private string _notesBodyText = "";

    [ObservableProperty]
    private bool _canDeleteCommunityNote;

    [ObservableProperty]
    private bool _canMoveFootnote;

    [ObservableProperty]
    private bool _canAddCommunityNote;

    [ObservableProperty]
    private bool _isMoveFootnoteEnabled;

    // -------------------------
    // Rendered docs state (non-UI)
    // -------------------------
    public RenderedDocument RenderOrig { get; set; } = RenderedDocument.Empty;
    public RenderedDocument RenderTran { get; set; } = RenderedDocument.Empty;

    // -------------------------
    // Current annotation state
    // -------------------------
    public DocAnnotation? CurrentAnnotation { get; set; }
    public bool CurrentAnnotationFromTranslatedPane { get; set; }

    // Move-mode state
    public bool AwaitingMoveTargetClick { get; set; }
    public DocAnnotation? MoveSourceAnnotation { get; set; }

    // Pending refresh gate
    public bool PendingRefresh { get; set; }
    public DateTime PendingSinceUtc { get; set; }
    public const int PendingTimeoutMs = 2500;

    // -------------------------
    // Events to host
    // -------------------------
    public event EventHandler<(int XmlIndex, string NoteText, string? Resp)>? CommunityNoteInsertRequested;
    public event EventHandler<(int XmlStart, int XmlEndExclusive)>? CommunityNoteDeleteRequested;
    public event EventHandler<(string RelPath, bool IsZen)>? ZenFlagChanged;
    public event EventHandler<string>? StatusChanged;

    public sealed record MoveFootnoteRequest(
        int OldXmlStart,
        int OldXmlEndExclusive,
        int NewXmlIndex,
        string NoteText,
        string? Resp,
        bool SourceWasTranslatedPane
    );

    public event EventHandler<MoveFootnoteRequest>? FootnoteMoveRequested;

    // -------------------------
    // Status helper
    // -------------------------
    private long _seq;
    public void Say(string msg) => StatusChanged?.Invoke(this, msg);

    public void Log(string msg)
    {
        var line = $"[ReadableTabView #{++_seq}] {msg}";
        try { Say(line); } catch { }
        try { System.Diagnostics.Debug.WriteLine(line); } catch { }
    }

    // -------------------------
    // Zen toggle logic
    // -------------------------
    public void SetZenContext(string? relPath, bool isZen)
    {
        CurrentRelPathForZen = relPath;
        IsZenEnabled = !string.IsNullOrWhiteSpace(relPath);
        // Suppress zen events for programmatic toggle
        _suppressZenEvents = true;
        try { IsZenText = isZen; }
        finally { _suppressZenEvents = false; }
    }

    private bool _suppressZenEvents;

    partial void OnIsZenTextChanged(bool value)
    {
        if (_suppressZenEvents) return;
        if (string.IsNullOrWhiteSpace(CurrentRelPathForZen)) return;
        ZenFlagChanged?.Invoke(this, (CurrentRelPathForZen!, value));
    }

    // -------------------------
    // Notes panel logic
    // -------------------------
    public void ShowNotes(DocAnnotation ann, bool fromTranslatedPane)
    {
        CurrentAnnotation = ann;
        CurrentAnnotationFromTranslatedPane = fromTranslatedPane;

        CancelMoveMode(keepPanelOpen: true);

        var kind = TryGetXmlCommunitySpanStrict(ann, out _, out _) ? "Community" : "Note";
        var resp = GetAnnotationResp(ann);
        NotesHeaderText = string.IsNullOrWhiteSpace(resp) ? kind : $"{kind} ({resp})";
        NotesBodyText = ann.Text ?? "";
        NotesPanelVisible = true;

        UpdateButtonsState();
    }

    public void HideNotes()
    {
        NotesPanelVisible = false;
        NotesBodyText = "";
        CurrentAnnotation = null;
        CurrentAnnotationFromTranslatedPane = false;
        UpdateButtonsState();
    }

    public void UpdateButtonsState()
    {
        if (PendingRefresh)
        {
            CanAddCommunityNote = false;
            CanDeleteCommunityNote = false;
            CanMoveFootnote = false;
            IsMoveFootnoteEnabled = false;
            return;
        }

        CanAddCommunityNote = !RenderTran.IsEmpty;

        if (CurrentAnnotation != null && TryGetXmlCommunitySpanStrict(CurrentAnnotation, out var xs, out var xe) && xe > xs)
            CanDeleteCommunityNote = true;
        else
            CanDeleteCommunityNote = false;

        if (CurrentAnnotation != null && TryGetXmlSpanLoose(CurrentAnnotation, out var xs2, out var xe2) && xe2 > xs2)
        {
            CanMoveFootnote = true;
            IsMoveFootnoteEnabled = !AwaitingMoveTargetClick;
        }
        else
        {
            CanMoveFootnote = false;
            IsMoveFootnoteEnabled = false;
        }
    }

    // -------------------------
    // Community note actions (validation only - actual insert/delete via events)
    // -------------------------
    public void RequestDeleteCurrentCommunityNote()
    {
        if (PendingRefresh) return;
        if (CurrentAnnotation == null) return;
        if (!TryGetXmlCommunitySpanStrict(CurrentAnnotation, out int xs, out int xe)) return;

        EnterPending($"delete xs={xs} xe={xe}");
        CommunityNoteDeleteRequested?.Invoke(this, (xs, xe));
        HideNotes();
    }

    public void RequestInsertCommunityNote(int xmlIndex, string noteText, string? resp)
    {
        EnterPending($"insert xmlIndex={xmlIndex}");
        CommunityNoteInsertRequested?.Invoke(this, (xmlIndex, noteText, resp));
    }

    // -------------------------
    // Move footnote logic
    // -------------------------
    public void StartMoveMode()
    {
        if (PendingRefresh) return;
        if (CurrentAnnotation == null) return;
        if (!TryGetXmlSpanLoose(CurrentAnnotation, out var xs, out var xe) || xe <= xs)
        {
            Say("This note cannot be moved (missing XML span).");
            return;
        }

        AwaitingMoveTargetClick = true;
        MoveSourceAnnotation = CurrentAnnotation;
        NotesHeaderText = "Note (click new location to move)";
        Say("Move mode: click in the reader where you want this footnote.");
        UpdateButtonsState();
    }

    public void CompleteMoveFootnote(int newXmlIndex)
    {
        if (MoveSourceAnnotation == null) return;
        if (!TryGetXmlSpanLoose(MoveSourceAnnotation, out int oldXs, out int oldXe) || oldXe <= oldXs)
        {
            Say("Move failed: source note missing XML span.");
            CancelMoveMode(keepPanelOpen: true);
            return;
        }

        var text = MoveSourceAnnotation.Text ?? "";
        var resp = GetAnnotationResp(MoveSourceAnnotation);

        EnterPending($"move old {oldXs}..{oldXe} -> new {newXmlIndex}");
        FootnoteMoveRequested?.Invoke(this, new MoveFootnoteRequest(
            OldXmlStart: oldXs,
            OldXmlEndExclusive: oldXe,
            NewXmlIndex: newXmlIndex,
            NoteText: text,
            Resp: resp,
            SourceWasTranslatedPane: CurrentAnnotationFromTranslatedPane
        ));

        CancelMoveModeAndHideNotes();
    }

    public void CancelMoveMode(bool keepPanelOpen)
    {
        AwaitingMoveTargetClick = false;
        MoveSourceAnnotation = null;

        if (keepPanelOpen && CurrentAnnotation != null)
        {
            var kind = TryGetXmlCommunitySpanStrict(CurrentAnnotation, out _, out _) ? "Community" : "Note";
            var resp = GetAnnotationResp(CurrentAnnotation);
            NotesHeaderText = string.IsNullOrWhiteSpace(resp) ? kind : $"{kind} ({resp})";
        }

        UpdateButtonsState();
    }

    public void CancelMoveModeAndHideNotes()
    {
        CancelMoveMode(keepPanelOpen: false);
        HideNotes();
    }

    // -------------------------
    // Pending refresh gate
    // -------------------------
    public void EnterPending(string why)
    {
        PendingRefresh = true;
        PendingSinceUtc = DateTime.UtcNow;
        UpdateButtonsState();
        Log("Pending enter: " + why);
    }

    public void ExitPending(string why)
    {
        if (!PendingRefresh) return;
        PendingRefresh = false;
        UpdateButtonsState();
        Log("Pending exit: " + why);
    }

    public void CheckPendingTimeout()
    {
        if (PendingRefresh && (DateTime.UtcNow - PendingSinceUtc).TotalMilliseconds > PendingTimeoutMs)
        {
            PendingRefresh = false;
            UpdateButtonsState();
        }
    }

    // -------------------------
    // Clear
    // -------------------------
    public void Clear()
    {
        RenderOrig = RenderedDocument.Empty;
        RenderTran = RenderedDocument.Empty;
        IsEmptyState = true;
        SetZenContext(null, isZen: false);
        CancelMoveModeAndHideNotes();
        PendingRefresh = false;
        UpdateButtonsState();
    }

    // -------------------------
    // Annotation span detection (community strict vs note loose)
    // -------------------------
    public static bool TryGetXmlCommunitySpanStrict(DocAnnotation ann, out int xmlStart, out int xmlEndExclusive)
    {
        xmlStart = -1;
        xmlEndExclusive = -1;

        if (ann == null) return false;

        var kind = (ann.Kind ?? "").Trim();
        bool isCommunity = kind.Equals("community", StringComparison.OrdinalIgnoreCase);

        if (!isCommunity)
        {
            if (TryGetStringProp(ann, "Type", out var t) && !string.IsNullOrWhiteSpace(t) &&
                t.Trim().Equals("community", StringComparison.OrdinalIgnoreCase))
                isCommunity = true;

            if (!isCommunity &&
                TryGetStringProp(ann, "Source", out var s) && !string.IsNullOrWhiteSpace(s) &&
                s.Trim().Equals("community", StringComparison.OrdinalIgnoreCase))
                isCommunity = true;
        }

        if (!isCommunity) return false;

        bool gotStart =
            TryGetIntProp(ann, "XmlStart", out xmlStart) ||
            TryGetIntProp(ann, "XmlStartIndex", out xmlStart) ||
            TryGetIntProp(ann, "XmlFrom", out xmlStart);

        bool gotEnd =
            TryGetIntProp(ann, "XmlEndExclusive", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlEnd", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlTo", out xmlEndExclusive);

        if (!gotStart || !gotEnd) return false;
        if (xmlStart < 0) return false;
        if (xmlEndExclusive <= xmlStart) return false;

        const int MaxReasonableSpan = 20_000;
        if (xmlEndExclusive - xmlStart > MaxReasonableSpan) return false;

        return true;
    }

    public static bool TryGetXmlSpanLoose(DocAnnotation ann, out int xmlStart, out int xmlEndExclusive)
    {
        xmlStart = -1;
        xmlEndExclusive = -1;

        if (ann == null) return false;

        bool gotStart =
            TryGetIntProp(ann, "XmlStart", out xmlStart) ||
            TryGetIntProp(ann, "XmlStartIndex", out xmlStart) ||
            TryGetIntProp(ann, "XmlFrom", out xmlStart);

        bool gotEnd =
            TryGetIntProp(ann, "XmlEndExclusive", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlEnd", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlTo", out xmlEndExclusive);

        if (!gotStart || !gotEnd) return false;
        if (xmlStart < 0) return false;
        if (xmlEndExclusive <= xmlStart) return false;

        const int MaxReasonableSpan = 200_000;
        if (xmlEndExclusive - xmlStart > MaxReasonableSpan) return false;

        return true;
    }

    // -------------------------
    // Reflection helpers
    // -------------------------
    public static string? GetAnnotationResp(DocAnnotation ann)
    {
        try
        {
            var pi = ann.GetType().GetProperty("Resp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi?.GetValue(ann) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }
        catch { }

        if (TryGetStringProp(ann, "Author", out var a) && !string.IsNullOrWhiteSpace(a)) return a.Trim();
        if (TryGetStringProp(ann, "By", out var b) && !string.IsNullOrWhiteSpace(b)) return b.Trim();
        if (TryGetStringProp(ann, "Name", out var n) && !string.IsNullOrWhiteSpace(n)) return n.Trim();

        return null;
    }

    public static bool TryGetIntProp(object obj, string name, out int value)
    {
        value = 0;
        try
        {
            var t = obj.GetType();

            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null && TryConvertNumber(pi.GetValue(obj), out value))
                return true;

            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null && TryConvertNumber(fi.GetValue(obj), out value))
                return true;
        }
        catch { }

        return false;
    }

    public static bool TryConvertNumber(object? raw, out int value)
    {
        value = 0;
        if (raw == null) return false;

        try
        {
            switch (raw)
            {
                case int i: value = i; return true;
                case long l: value = l > int.MaxValue ? int.MaxValue : l < int.MinValue ? int.MinValue : (int)l; return true;
                case short s: value = s; return true;
                case byte b: value = b; return true;
                case uint ui: value = ui > int.MaxValue ? int.MaxValue : (int)ui; return true;
                case ulong ul: value = ul > (ulong)int.MaxValue ? int.MaxValue : (int)ul; return true;
                case float f: value = (int)f; return true;
                case double d: value = (int)d; return true;
                case decimal m: value = (int)m; return true;
                default:
                    if (raw is IConvertible) { value = Convert.ToInt32(raw); return true; }
                    return false;
            }
        }
        catch { return false; }
    }

    public static bool TryGetStringProp(object obj, string name, out string? value)
    {
        value = null;
        try
        {
            var pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi?.GetValue(obj) is string s) { value = s; return true; }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Maps a rendered (display) index to an XML index using the RenderedDocument.
    /// </summary>
    public static bool TryMapRenderedIndexToXmlIndex(RenderedDocument doc, int displayIndex, out int xmlIndex)
    {
        xmlIndex = -1;
        if (doc == null || doc.IsEmpty) return false;
        if (doc.BaseToXmlIndex == null || doc.BaseToXmlIndex.Length == 0) return false;

        try
        {
            xmlIndex = doc.DisplayIndexToXmlIndex(displayIndex);
            return xmlIndex >= 0;
        }
        catch
        {
            xmlIndex = -1;
            return false;
        }
    }

    /// <summary>
    /// Resolves annotation from marker spans at a given caret index.
    /// </summary>
    public static bool TryResolveAnnotationFromMarkerSpans(RenderedDocument doc, int idx, out DocAnnotation ann)
    {
        ann = default!;
        var markers = doc.AnnotationMarkers;
        var anns = doc.Annotations;

        if (markers == null || markers.Count == 0) return false;
        if (anns == null || anns.Count == 0) return false;

        const int radius = 8;

        int lo = 0, hi = markers.Count - 1, firstGreater = markers.Count;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (markers[mid].Start > idx) { firstGreater = mid; hi = mid - 1; }
            else lo = mid + 1;
        }

        int bestMarkerIndex = -1;
        int bestDist = int.MaxValue;

        int startScan = Math.Max(0, firstGreater - 6);
        int endScan = Math.Min(markers.Count - 1, firstGreater + 6);

        for (int i = startScan; i <= endScan; i++)
        {
            var m = markers[i];
            if (m.Start > idx + radius) break;
            if (m.EndExclusive < idx - radius) continue;

            int dist = idx < m.Start ? (m.Start - idx)
                     : idx > m.EndExclusive ? (idx - m.EndExclusive)
                     : 0;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestMarkerIndex = i;
                if (dist == 0) break;
            }
        }

        if (bestMarkerIndex < 0 || bestDist > radius) return false;

        var best = markers[bestMarkerIndex];
        int annIndex = best.AnnotationIndex;
        if ((uint)annIndex >= (uint)anns.Count) return false;

        ann = anns[annIndex];
        return true;
    }

    /// <summary>
    /// Finds the best-scoring match range in the rendered text (static, no editor dependency).
    /// </summary>
    public static (int start, int length) FindBestMatchRange(
        string docText,
        string match,
        string? left,
        string? right,
        int? anchorStartHint,
        int? anchorOccurrenceHint,
        string? anchorTextSignal)
    {
        if (string.IsNullOrEmpty(docText) || string.IsNullOrEmpty(match))
            return (-1, 0);

        static (int start, int length) FindExact(
            string text,
            string m,
            string? l,
            string? r,
            int? startHint,
            int? occurrenceHint,
            string? textSignal)
        {
            var candidates = new List<int>();
            int searchFrom = 0;
            while (searchFrom < text.Length)
            {
                int idx = text.IndexOf(m, searchFrom, StringComparison.Ordinal);
                if (idx < 0) break;
                candidates.Add(idx);
                searchFrom = idx + 1;
            }

            if (candidates.Count == 0)
                return (-1, 0);

            var ranked = candidates
                .Select((idx, occurrence) =>
                {
                    int contextScore = 0;

                    if (!string.IsNullOrEmpty(l))
                    {
                        int winStart = Math.Max(0, idx - l.Length * 2);
                        string pre = text.Substring(winStart, idx - winStart);
                        if (pre.Contains(l, StringComparison.Ordinal)) contextScore += 2;
                    }

                    if (!string.IsNullOrEmpty(r))
                    {
                        int matchEnd = idx + m.Length;
                        int winEnd = Math.Min(text.Length, matchEnd + r.Length * 2);
                        string post = text.Substring(matchEnd, winEnd - matchEnd);
                        if (post.Contains(r, StringComparison.Ordinal)) contextScore += 2;
                    }

                    int signalScore = 0;
                    if (!string.IsNullOrWhiteSpace(textSignal))
                    {
                        string raw = text.Substring(idx, m.Length);
                        var shared = CjkMatchNormalizer.FindSharedRawRanges(raw, textSignal, minPhraseLen: 2);
                        foreach (var s in shared)
                            signalScore += Math.Max(1, s.Length);
                    }

                    int startDistance = startHint.HasValue ? Math.Abs(idx - startHint.Value) : int.MaxValue;
                    int occurrenceDistance = occurrenceHint.HasValue ? Math.Abs(occurrence - occurrenceHint.Value) : int.MaxValue;

                    return (idx, contextScore, signalScore, startDistance, occurrenceDistance);
                })
                .OrderByDescending(x => x.contextScore)
                .ThenByDescending(x => x.signalScore)
                .ThenBy(x => x.startDistance)
                .ThenBy(x => x.occurrenceDistance)
                .ThenBy(x => x.idx)
                .First();

            return (ranked.idx, m.Length);
        }

        var exact = FindExact(docText, match, left, right, anchorStartHint, anchorOccurrenceHint, anchorTextSignal);
        if (exact.start >= 0)
            return exact;

        bool anyCjk = CjkMatchNormalizer.ContainsCjk(match)
                      || CjkMatchNormalizer.ContainsCjk(left)
                      || CjkMatchNormalizer.ContainsCjk(right);
        if (!anyCjk)
            return (-1, 0);

        var nDoc = CjkMatchNormalizer.NormalizeWithMap(docText);
        string nMatch = CjkMatchNormalizer.Normalize(match);
        if (string.IsNullOrEmpty(nDoc.Normalized) || string.IsNullOrEmpty(nMatch))
            return (-1, 0);

        string? nLeft = string.IsNullOrEmpty(left) ? null : CjkMatchNormalizer.Normalize(left);
        string? nRight = string.IsNullOrEmpty(right) ? null : CjkMatchNormalizer.Normalize(right);

        var normCandidates = new List<(int normStart, int rawStart, int rawEnd)>();
        int from = 0;
        while (from < nDoc.Normalized.Length)
        {
            int idx = nDoc.Normalized.IndexOf(nMatch, from, StringComparison.Ordinal);
            if (idx < 0) break;

            int rawStart = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, idx);
            int rawEnd = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, idx + nMatch.Length);
            if (rawEnd > rawStart)
                normCandidates.Add((idx, rawStart, rawEnd));

            from = idx + 1;
        }

        if (normCandidates.Count == 0)
            return (-1, 0);

        var bestNorm = normCandidates
            .Select((c, occurrence) =>
            {
                int score = 0;
                if (!string.IsNullOrEmpty(nLeft))
                {
                    int winStart = Math.Max(0, c.normStart - nLeft.Length * 2);
                    string pre = nDoc.Normalized.Substring(winStart, c.normStart - winStart);
                    if (pre.Contains(nLeft, StringComparison.Ordinal)) score += 2;
                }

                if (!string.IsNullOrEmpty(nRight))
                {
                    int matchEnd = c.normStart + nMatch.Length;
                    int winEnd = Math.Min(nDoc.Normalized.Length, matchEnd + nRight.Length * 2);
                    string post = nDoc.Normalized.Substring(matchEnd, winEnd - matchEnd);
                    if (post.Contains(nRight, StringComparison.Ordinal)) score += 2;
                }

                int signalScore = 0;
                if (!string.IsNullOrWhiteSpace(anchorTextSignal))
                {
                    string raw = docText.Substring(c.rawStart, c.rawEnd - c.rawStart);
                    var shared = CjkMatchNormalizer.FindSharedRawRanges(raw, anchorTextSignal, minPhraseLen: 2);
                    foreach (var s in shared)
                        signalScore += Math.Max(1, s.Length);
                }

                int startDistance = anchorStartHint.HasValue ? Math.Abs(c.rawStart - anchorStartHint.Value) : int.MaxValue;
                int occurrenceDistance = anchorOccurrenceHint.HasValue ? Math.Abs(occurrence - anchorOccurrenceHint.Value) : int.MaxValue;

                return (c.rawStart, rawLength: c.rawEnd - c.rawStart, score, signalScore, startDistance, occurrenceDistance);
            })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.signalScore)
            .ThenBy(x => x.startDistance)
            .ThenBy(x => x.occurrenceDistance)
            .ThenBy(x => x.rawStart)
            .First();

        return (bestNorm.rawStart, bestNorm.rawLength);
    }
}
