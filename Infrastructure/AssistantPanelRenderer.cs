// Infrastructure/AssistantPanelRenderer.cs
// Shared assistant panel rendering for TranslationTabView and ScholarTabView.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbetaTranslator.App.Infrastructure;

/// <summary>
/// Text range for assistant panel CJK highlighting.
/// </summary>
internal sealed record AssistantTextRange(int Start, int Length);

/// <summary>
/// Static helper that renders <see cref="TranslationAssistantSnapshot"/> content
/// into StackPanel hosts.  Shared between TranslationTabView and ScholarTabView.
/// </summary>
internal static class AssistantPanelRenderer
{
    /// <summary>
    /// Populates the four assistant host panels with TM, term, and QA cards.
    /// </summary>
    /// <param name="snapshot">The snapshot to render (null clears all hosts).</param>
    /// <param name="qaHost">Host for QA issue cards.</param>
    /// <param name="termHost">Host for terminology cards.</param>
    /// <param name="approvedTmHost">Host for approved TM match cards.</param>
    /// <param name="referenceTmHost">Host for reference TM match cards.</param>
    /// <param name="titleResolver">Resolves a RelPath to a display title.</param>
    /// <param name="brushResolver">Resolves a resource key to an IBrush.</param>
    /// <param name="postProcessor">Called on each TextEditor after creation (e.g. to attach hover).</param>
    /// <param name="navigationHandler">Attached as double-click handler on TM cards.</param>
    /// <param name="addToScholarHandler">If set, a right-click context menu is added to TM/term cards to add passages to the scholar collection.</param>
    public static void RenderSnapshot(
        TranslationAssistantSnapshot? snapshot,
        StackPanel? qaHost,
        StackPanel? termHost,
        StackPanel? approvedTmHost,
        StackPanel? referenceTmHost,
        Func<string?, string>? titleResolver = null,
        Func<string, IBrush?>? brushResolver = null,
        Action<TextEditor>? postProcessor = null,
        EventHandler<NavigationRequest>? navigationHandler = null,
        Action<ScholarPassage>? addToScholarHandler = null)
    {
        if (approvedTmHost != null) approvedTmHost.Children.Clear();
        if (referenceTmHost != null) referenceTmHost.Children.Clear();
        if (termHost != null) termHost.Children.Clear();
        if (qaHost != null) qaHost.Children.Clear();

        if (snapshot == null)
            return;

        if (approvedTmHost != null)
        {
            var matches = snapshot.ApprovedMatches ?? new List<TranslationTmMatch>();
            if (matches.Count > 0)
                approvedTmHost.Children.Add(BuildConsolidatedTmSection(snapshot, matches, titleResolver, brushResolver, postProcessor, navigationHandler, addToScholarHandler));
        }

        if (referenceTmHost != null)
        {
            var matches = snapshot.ReferenceMatches ?? new List<TranslationTmMatch>();
            if (matches.Count > 0)
                referenceTmHost.Children.Add(BuildConsolidatedTmSection(snapshot, matches, titleResolver, brushResolver, postProcessor, navigationHandler, addToScholarHandler));
        }

        if (termHost != null)
        {
            var terms = snapshot.Terms ?? new List<TermHit>();
            if (terms.Count > 0)
                termHost.Children.Add(BuildConsolidatedTermSection(snapshot, terms, brushResolver, postProcessor, addToScholarHandler));
        }

        if (qaHost != null)
        {
            var issues = snapshot.QaIssues ?? new List<QaIssue>();
            if (issues.Count > 0)
                qaHost.Children.Add(BuildConsolidatedQaSection(issues, brushResolver, postProcessor));
        }
    }

    private const string SectionSeparator = "────────────────────\n";

    /// <summary>
    /// Builds a single consolidated Border+TextEditor for all TM matches in a section,
    /// instead of one TextEditor per match. Dramatically reduces control count.
    /// </summary>
    private static Control BuildConsolidatedTmSection(
        TranslationAssistantSnapshot snapshot,
        List<TranslationTmMatch> matches,
        Func<string?, string>? titleResolver,
        Func<string, IBrush?>? brushResolver,
        Action<TextEditor>? postProcessor,
        EventHandler<NavigationRequest>? navigationHandler,
        Action<ScholarPassage>? addToScholarHandler)
    {
        string currentZh = snapshot.Segment?.ZhText ?? "";
        var combinedSb = new StringBuilder();
        var allRanges = new List<AssistantTextRange>();
        // Map from (startOffset, endOffset) to the match, for double-click and context menu.
        var matchMap = new List<(int Start, int End, TranslationTmMatch Match)>();

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            int entryStart = combinedSb.Length;

            string title = titleResolver?.Invoke(m.RelPath) ?? m.RelPath ?? "";
            string entryText = BuildTmEditorText(m, title);
            combinedSb.Append(entryText);

            // Build highlight ranges for this entry, offset by entryStart.
            var entryRanges = BuildTmHighlightRanges(entryText, m.SourceText ?? "", currentZh);
            foreach (var r in entryRanges)
                allRanges.Add(new AssistantTextRange(r.Start + entryStart, r.Length));

            int entryEnd = combinedSb.Length;
            matchMap.Add((entryStart, entryEnd, m));

            if (i < matches.Count - 1)
            {
                combinedSb.Append('\n');
                combinedSb.Append(SectionSeparator);
            }
        }

        string combinedText = combinedSb.ToString();
        var mergedRanges = allRanges.Count > 0 ? MergeRanges(allRanges) : (IReadOnlyList<AssistantTextRange>)Array.Empty<AssistantTextRange>();
        var editor = BuildAssistantEditor(combinedText, mergedRanges, minHeight: 90, maxHeight: 600, brushResolver, postProcessor);

        var border = new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor,
        };

        // Double-click navigation: find which match the click offset falls into.
        if (navigationHandler != null)
        {
            border.AddHandler(
                InputElement.PointerPressedEvent,
                (object? _, PointerPressedEventArgs e) =>
                {
                    if (e.ClickCount >= 2)
                    {
                        var hit = FindMatchAtPointer(editor, e, matchMap);
                        if (hit != null && !string.IsNullOrWhiteSpace(hit.RelPath) && !string.IsNullOrWhiteSpace(hit.SourceText))
                        {
                            navigationHandler.Invoke(border, new NavigationRequest
                            {
                                RelPath = hit.RelPath,
                                Side = SearchSide.Original,
                                MatchText = hit.SourceText,
                                AnchorOccurrenceHint = hit.BlockNumber > 0 ? hit.BlockNumber - 1 : null,
                                AnchorTextSignal = string.IsNullOrWhiteSpace(snapshot.Segment?.ZhContextText)
                                    ? snapshot.Segment?.ZhText
                                    : snapshot.Segment?.ZhContextText,
                            });
                        }
                    }
                },
                RoutingStrategies.Tunnel);
        }

        // Context menu: "Add to Scholar Collection" using the match under the pointer.
        if (addToScholarHandler != null)
        {
            border.ContextMenu = new ContextMenu();
            border.ContextMenu.Opening += (_, _) =>
            {
                border.ContextMenu.Items.Clear();
                // Determine which match region the caret is in.
                var caretMatch = FindMatchAtCaret(editor, matchMap);
                if (caretMatch != null)
                {
                    var menuItem = new MenuItem { Header = "Add to Scholar Collection" };
                    var captured = caretMatch;
                    menuItem.Click += (_, _) =>
                    {
                        addToScholarHandler(new ScholarPassage
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            ZhText = captured.SourceText ?? "",
                            EnText = captured.TargetText ?? "",
                            SourceRelPath = captured.RelPath ?? "",
                            AddedUtc = DateTimeOffset.UtcNow
                        });
                    };
                    border.ContextMenu.Items.Add(menuItem);
                }
            };
        }

        return border;
    }

    /// <summary>
    /// Builds a single consolidated Border+TextEditor for all term hits.
    /// </summary>
    private static Control BuildConsolidatedTermSection(
        TranslationAssistantSnapshot snapshot,
        List<TermHit> terms,
        Func<string, IBrush?>? brushResolver,
        Action<TextEditor>? postProcessor,
        Action<ScholarPassage>? addToScholarHandler)
    {
        string currentZh = snapshot.Segment?.ZhText ?? "";
        var combinedSb = new StringBuilder();
        var allRanges = new List<AssistantTextRange>();
        var termMap = new List<(int Start, int End, TermHit Term)>();

        for (int i = 0; i < terms.Count; i++)
        {
            var t = terms[i];
            int entryStart = combinedSb.Length;

            string entryText = BuildTermEditorText(t);
            combinedSb.Append(entryText);

            var entryRanges = BuildSingleLineChineseHighlightRanges(entryText, t.SourceTerm ?? "", currentZh);
            foreach (var r in entryRanges)
                allRanges.Add(new AssistantTextRange(r.Start + entryStart, r.Length));

            int entryEnd = combinedSb.Length;
            termMap.Add((entryStart, entryEnd, t));

            if (i < terms.Count - 1)
            {
                combinedSb.Append('\n');
                combinedSb.Append(SectionSeparator);
            }
        }

        string combinedText = combinedSb.ToString();
        var mergedRanges = allRanges.Count > 0 ? MergeRanges(allRanges) : (IReadOnlyList<AssistantTextRange>)Array.Empty<AssistantTextRange>();
        var editor = BuildAssistantEditor(combinedText, mergedRanges, minHeight: 70, maxHeight: 500, brushResolver, postProcessor);

        var border = new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor,
        };

        if (addToScholarHandler != null)
        {
            border.ContextMenu = new ContextMenu();
            border.ContextMenu.Opening += (_, _) =>
            {
                border.ContextMenu.Items.Clear();
                int caretOffset = editor.TextArea?.Caret?.Offset ?? 0;
                TermHit? caretTerm = null;
                foreach (var (start, end, term) in termMap)
                {
                    if (caretOffset >= start && caretOffset <= end)
                    {
                        caretTerm = term;
                        break;
                    }
                }
                if (caretTerm != null)
                {
                    var menuItem = new MenuItem { Header = "Add to Scholar Collection" };
                    var captured = caretTerm;
                    menuItem.Click += (_, _) =>
                    {
                        addToScholarHandler(new ScholarPassage
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            ZhText = captured.SourceTerm ?? "",
                            EnText = captured.PreferredTarget ?? "",
                            SourceRelPath = "",
                            AddedUtc = DateTimeOffset.UtcNow
                        });
                    };
                    border.ContextMenu.Items.Add(menuItem);
                }
            };
        }

        return border;
    }

    /// <summary>
    /// Builds a single consolidated Border+TextEditor for all QA issues.
    /// </summary>
    private static Control BuildConsolidatedQaSection(
        List<QaIssue> issues,
        Func<string, IBrush?>? brushResolver,
        Action<TextEditor>? postProcessor)
    {
        var combinedSb = new StringBuilder();
        for (int i = 0; i < issues.Count; i++)
        {
            combinedSb.Append($"[{issues[i].Severity}] {issues[i].Message}");
            if (i < issues.Count - 1)
            {
                combinedSb.Append('\n');
                combinedSb.Append(SectionSeparator);
            }
        }

        var editor = BuildAssistantEditor(
            combinedSb.ToString(),
            Array.Empty<AssistantTextRange>(),
            minHeight: 56,
            maxHeight: 400,
            brushResolver,
            postProcessor);

        return new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor,
        };
    }

    /// <summary>
    /// Given a pointer event on a consolidated TM editor, determines which match the click falls into.
    /// </summary>
    private static TranslationTmMatch? FindMatchAtPointer(
        TextEditor editor,
        PointerPressedEventArgs e,
        List<(int Start, int End, TranslationTmMatch Match)> matchMap)
    {
        try
        {
            var textView = editor.TextArea?.TextView;
            if (textView == null) return null;

            var pos = e.GetPosition(textView);
            var vp = textView.GetPosition(pos);
            if (vp == null) return null;

            int offset = editor.Document?.GetOffset(vp.Value.Location) ?? -1;
            if (offset < 0) return null;

            foreach (var (start, end, match) in matchMap)
            {
                if (offset >= start && offset <= end)
                    return match;
            }
        }
        catch
        {
            // Position mapping can fail if click is outside text bounds.
        }
        return null;
    }

    /// <summary>
    /// Finds which match the caret is currently positioned in (for context menu).
    /// </summary>
    private static TranslationTmMatch? FindMatchAtCaret(
        TextEditor editor,
        List<(int Start, int End, TranslationTmMatch Match)> matchMap)
    {
        int caretOffset = editor.TextArea?.Caret?.Offset ?? 0;
        foreach (var (start, end, match) in matchMap)
        {
            if (caretOffset >= start && caretOffset <= end)
                return match;
        }
        return null;
    }

    public static Control BuildTmEntryControl(
        TranslationAssistantSnapshot snapshot,
        TranslationTmMatch match,
        Func<string?, string>? titleResolver = null,
        Func<string, IBrush?>? brushResolver = null,
        Action<TextEditor>? postProcessor = null,
        EventHandler<NavigationRequest>? navigationHandler = null,
        Action<ScholarPassage>? addToScholarHandler = null)
    {
        string title = titleResolver?.Invoke(match.RelPath) ?? match.RelPath ?? "";
        string currentZh = snapshot.Segment?.ZhText ?? "";
        string editorText = BuildTmEditorText(match, title);
        var ranges = BuildTmHighlightRanges(editorText, match.SourceText ?? "", currentZh);

        var editor = BuildAssistantEditor(editorText, ranges, minHeight: 90, maxHeight: 220, brushResolver, postProcessor);

        var border = new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor,
        };

        // Double-click on the TM card opens the source file in a new window.
        // We use a tunnel handler so it fires even when the inner TextEditor absorbs pointer events.
        if (!string.IsNullOrWhiteSpace(match.RelPath) && !string.IsNullOrWhiteSpace(match.SourceText) && navigationHandler != null)
        {
            var capturedMatch = match; // capture for the lambda
            border.AddHandler(
                InputElement.PointerPressedEvent,
                (object? _, PointerPressedEventArgs e) =>
                {
                    if (e.ClickCount >= 2)
                    {
                        navigationHandler.Invoke(border, new NavigationRequest
                        {
                            RelPath = capturedMatch.RelPath,
                            Side = SearchSide.Original,  // navigate to the Chinese source pane
                            MatchText = capturedMatch.SourceText,
                            // Stable tie-break hint for repeated identical source segments.
                            AnchorOccurrenceHint = capturedMatch.BlockNumber > 0 ? capturedMatch.BlockNumber - 1 : null,
                            // Soft text signal used only to break ties when context is missing.
                            AnchorTextSignal = string.IsNullOrWhiteSpace(snapshot.Segment?.ZhContextText)
                                ? snapshot.Segment?.ZhText
                                : snapshot.Segment?.ZhContextText,
                        });
                    }
                },
                RoutingStrategies.Tunnel);
        }

        if (addToScholarHandler != null)
        {
            var menuItem = new MenuItem { Header = "Add to Scholar Collection" };
            menuItem.Click += (_, _) =>
            {
                var passage = new ScholarPassage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ZhText = match.SourceText ?? "",
                    EnText = match.TargetText ?? "",
                    SourceRelPath = match.RelPath ?? "",
                    AddedUtc = DateTimeOffset.UtcNow
                };
                addToScholarHandler(passage);
            };
            border.ContextMenu = new ContextMenu { Items = { menuItem } };
        }

        return border;
    }

    public static Control BuildTermEntryControl(
        TranslationAssistantSnapshot snapshot,
        TermHit term,
        Func<string, IBrush?>? brushResolver = null,
        Action<TextEditor>? postProcessor = null,
        Action<ScholarPassage>? addToScholarHandler = null)
    {
        string currentZh = snapshot.Segment?.ZhText ?? "";
        string editorText = BuildTermEditorText(term);
        var ranges = BuildSingleLineChineseHighlightRanges(editorText, term.SourceTerm ?? "", currentZh);

        var editor = BuildAssistantEditor(editorText, ranges, minHeight: 70, maxHeight: 180, brushResolver, postProcessor);

        var border = new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor
        };

        if (addToScholarHandler != null)
        {
            var menuItem = new MenuItem { Header = "Add to Scholar Collection" };
            menuItem.Click += (_, _) =>
            {
                var passage = new ScholarPassage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ZhText = term.SourceTerm ?? "",
                    EnText = term.PreferredTarget ?? "",
                    SourceRelPath = "",
                    AddedUtc = DateTimeOffset.UtcNow
                };
                addToScholarHandler(passage);
            };
            border.ContextMenu = new ContextMenu { Items = { menuItem } };
        }

        return border;
    }

    public static Control BuildQaEntryControl(
        QaIssue issue,
        Func<string, IBrush?>? brushResolver = null,
        Action<TextEditor>? postProcessor = null)
    {
        var editor = BuildAssistantEditor(
            $"[{issue.Severity}] {issue.Message}",
            Array.Empty<AssistantTextRange>(),
            minHeight: 56,
            maxHeight: 140,
            brushResolver,
            postProcessor);

        return new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor
        };
    }

    public static TextEditor BuildAssistantEditor(
        string text,
        IReadOnlyList<AssistantTextRange> highlightRanges,
        double minHeight,
        double maxHeight,
        Func<string, IBrush?>? brushResolver = null,
        Action<TextEditor>? postProcessor = null)
    {
        var editor = new TextEditor
        {
            Text = text ?? "",
            IsReadOnly = true,
            ShowLineNumbers = false,
            WordWrap = true,
            FontFamily = new FontFamily("Consolas, Menlo, 'DejaVu Sans Mono', 'Noto Sans CJK SC', 'Source Han Sans SC', monospace"),
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = brushResolver?.Invoke("XmlViewerBg"),
            Foreground = brushResolver?.Invoke("TextFg"),
            MinHeight = minHeight,
            MaxHeight = maxHeight
        };

        if (editor.TextArea?.TextView != null && highlightRanges.Count > 0)
        {
            var colorizer = new SharedChineseColorizer(highlightRanges);
            editor.TextArea.TextView.LineTransformers.Add(colorizer);
            editor.TextArea.TextView.Redraw();
        }

        if (editor.TextArea != null)
        {
            editor.TextArea.Caret.Offset = 0;
            editor.TextArea.Selection = Selection.Create(editor.TextArea, 0, 0);
        }

        postProcessor?.Invoke(editor);
        return editor;
    }

    // ---- Text formatters (pure functions) ----

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

    // ---- Highlight builders (pure functions) ----

    public static IReadOnlyList<AssistantTextRange> BuildTmHighlightRanges(string wholeText, string suggestionZh, string currentZh)
    {
        int zhLineStart = wholeText.IndexOf("ZH: ", StringComparison.Ordinal);
        if (zhLineStart < 0)
            return Array.Empty<AssistantTextRange>();

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

    public static IReadOnlyList<AssistantTextRange> BuildSingleLineChineseHighlightRanges(string wholeText, string lineText, string currentZh)
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

    public static IReadOnlyList<AssistantTextRange> BuildSharedChineseRangesInWholeText(
        string wholeText,
        int targetStart,
        int targetLength,
        string suggestionZh,
        string currentZh)
    {
        var result = new List<AssistantTextRange>();

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
                result.Add(new AssistantTextRange(absStart, len));
        }

        return MergeRanges(result);
    }

    public static IReadOnlyList<AssistantTextRange> MergeRanges(List<AssistantTextRange> ranges)
    {
        if (ranges.Count == 0)
            return ranges;

        var ordered = ranges.OrderBy(r => r.Start).ThenBy(r => r.Length).ToList();
        var merged = new List<AssistantTextRange> { ordered[0] };

        for (int i = 1; i < ordered.Count; i++)
        {
            var last = merged[^1];
            var cur = ordered[i];

            int lastEnd = last.Start + last.Length;
            int curEnd = cur.Start + cur.Length;

            if (cur.Start <= lastEnd)
            {
                merged[^1] = new AssistantTextRange(last.Start, Math.Max(lastEnd, curEnd) - last.Start);
            }
            else
            {
                merged.Add(cur);
            }
        }

        return merged;
    }

    // ---- Colorizer ----

    /// <summary>
    /// AvaloniaEdit line transformer that highlights shared CJK characters
    /// (blue foreground + semi-bold) in assistant panel TextEditor instances.
    /// </summary>
    internal sealed class SharedChineseColorizer : DocumentColorizingTransformer
    {
        private readonly IReadOnlyList<AssistantTextRange> _ranges;

        public SharedChineseColorizer(IReadOnlyList<AssistantTextRange> ranges)
        {
            _ranges = ranges ?? Array.Empty<AssistantTextRange>();
        }

        private static IBrush Brush(string key, IBrush fallback)
        {
            var app = Application.Current;
            if (app != null && app.TryFindResource(key, out var res) && res is IBrush b)
                return b;
            return fallback;
        }

        protected override void ColorizeLine(AvaloniaEdit.Document.DocumentLine line)
        {
            if (_ranges.Count == 0)
                return;

            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            var fg = Brush("NoteMarkerCommunityFg", Avalonia.Media.Brushes.DodgerBlue);

            for (int i = 0; i < _ranges.Count; i++)
            {
                var r = _ranges[i];
                int rStart = r.Start;
                int rEnd = r.Start + r.Length;

                if (rEnd <= lineStart)
                    continue;

                if (rStart >= lineEnd)
                    break;

                int s = Math.Max(rStart, lineStart);
                int e = Math.Min(rEnd, lineEnd);

                if (e <= s)
                    continue;

                ChangeLinePart(s, e, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(fg);
                    el.TextRunProperties.SetTypeface(
                        new Typeface(
                            el.TextRunProperties.Typeface.FontFamily,
                            el.TextRunProperties.Typeface.Style,
                            FontWeight.SemiBold,
                            el.TextRunProperties.Typeface.Stretch));
                });
            }
        }
    }
}
