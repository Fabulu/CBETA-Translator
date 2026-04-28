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
using ReadZen.App.Models;
using ReadZen.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Text range for assistant panel CJK highlighting.
/// </summary>
internal sealed record AssistantTextRange(int Start, int Length);

internal enum AssistantHighlightStyle
{
    None = 0,
    Term = 1,
    Tm = 2
}

/// <summary>
/// Static helper that renders <see cref="TranslationAssistantSnapshot"/> content
/// into StackPanel hosts.  Shared between TranslationTabView and ScholarTabView.
/// </summary>
internal static class AssistantPanelRenderer
{
    /// <summary>
    /// Renders italic guidance text into a host panel (used when no snapshot is available yet).
    /// </summary>
    public static void RenderEmptyGuidance(StackPanel host, string message)
    {
        host.Children.Clear();
        var tb = new TextBlock
        {
            Text = message,
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(Color.Parse("#999999")),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8),
            FontSize = 12,
        };
        host.Children.Add(tb);
    }

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
            foreach (var m in snapshot.ApprovedMatches ?? new List<TranslationTmMatch>())
                approvedTmHost.Children.Add(BuildTmEntryControl(snapshot, m, titleResolver, brushResolver, postProcessor, navigationHandler, addToScholarHandler, isApproved: true));
        }

        if (referenceTmHost != null)
        {
            foreach (var m in snapshot.ReferenceMatches ?? new List<TranslationTmMatch>())
                referenceTmHost.Children.Add(BuildTmEntryControl(snapshot, m, titleResolver, brushResolver, postProcessor, navigationHandler, addToScholarHandler, isApproved: false));
        }

        if (termHost != null)
        {
            foreach (var t in snapshot.Terms ?? new List<TermHit>())
                termHost.Children.Add(BuildTermEntryControl(snapshot, t, brushResolver, postProcessor, addToScholarHandler));
        }

        if (qaHost != null)
        {
            foreach (var q in snapshot.QaIssues ?? new List<QaIssue>())
                qaHost.Children.Add(BuildQaEntryControl(q, brushResolver, postProcessor));
        }

        AddEmptyPlaceholderIfNeeded(approvedTmHost, "No approved matches for this passage.", brushResolver);
        AddEmptyPlaceholderIfNeeded(referenceTmHost, "No reference translations for this passage.", brushResolver);
        AddEmptyPlaceholderIfNeeded(termHost, "No glossary entries for this passage.", brushResolver);
        AddEmptyPlaceholderIfNeeded(qaHost, "No quality issues detected.", brushResolver);
    }

    /// <summary>
    /// Renders concordance hits into a host panel. Called AFTER <see cref="RenderSnapshot"/>
    /// so TM results are already visible and concordance appears below them asynchronously.
    /// This is the async-populate pattern: TM renders instantly, concordance streams in.
    /// </summary>
    public static void RenderConcordance(
        IReadOnlyList<ConcordanceHit>? hits,
        StackPanel? host,
        Func<string, IBrush?>? brushResolver = null,
        EventHandler<NavigationRequest>? navigationHandler = null)
    {
        if (host == null) return;

        // Remove any previous concordance cards (tagged with "concordance" class)
        for (int i = host.Children.Count - 1; i >= 0; i--)
        {
            if (host.Children[i] is Border b && b.Tag is string tag && tag == "concordance")
                host.Children.RemoveAt(i);
        }

        if (hits == null || hits.Count == 0) return;

        // Section header
        var header = new Border
        {
            Tag = "concordance",
            Margin = new Thickness(0, 8, 0, 4),
            Child = new TextBlock
            {
                Text = $"Concordance — {hits.Count} match{(hits.Count == 1 ? "" : "es")} from untranslated texts",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = brushResolver?.Invoke("TextMutedFg") ?? Brushes.Gray,
            }
        };
        host.Children.Add(header);

        foreach (var hit in hits)
        {
            var stack = new StackPanel { Spacing = 2 };

            // File name (clickable if navigation handler exists)
            var nameBlock = new TextBlock
            {
                Text = hit.DisplayName,
                FontSize = 10,
                Foreground = brushResolver?.Invoke("AccentLinkFg") ?? new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                TextDecorations = TextDecorations.Underline,
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            if (navigationHandler != null)
            {
                var capturedRel = hit.RelPath;
                nameBlock.PointerPressed += (s, e) =>
                    navigationHandler(s, new NavigationRequest { RelPath = capturedRel, Side = SearchSide.Original });
            }
            stack.Children.Add(nameBlock);

            // Chinese snippet
            stack.Children.Add(new TextBlock
            {
                Text = hit.SnippetZh,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = brushResolver?.Invoke("TextFg") ?? Brushes.White,
            });

            // "(no translation)" label
            stack.Children.Add(new TextBlock
            {
                Text = "(no translation available)",
                FontSize = 10,
                FontStyle = FontStyle.Italic,
                Foreground = brushResolver?.Invoke("TextMutedFg") ?? Brushes.Gray,
            });

            var card = new Border
            {
                Tag = "concordance",
                Background = brushResolver?.Invoke("ControlBg") ?? Brushes.Transparent,
                BorderBrush = brushResolver?.Invoke("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6),
                Margin = new Thickness(0, 2),
                Child = stack,
            };
            host.Children.Add(card);
        }
    }

    private static void AddEmptyPlaceholderIfNeeded(StackPanel? host, string text, Func<string, IBrush?>? brushResolver)
    {
        if (host == null || host.Children.Count > 0)
            return;

        host.Children.Add(new Border
        {
            BorderBrush = brushResolver?.Invoke("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = brushResolver?.Invoke("TextMutedFg") ?? Brushes.Gray
            }
        });
    }

    public static Control BuildTmEntryControl(
        TranslationAssistantSnapshot snapshot,
        TranslationTmMatch match,
        Func<string?, string>? titleResolver = null,
        Func<string, IBrush?>? brushResolver = null,
        Action<TextEditor>? postProcessor = null,
        EventHandler<NavigationRequest>? navigationHandler = null,
        Action<ScholarPassage>? addToScholarHandler = null,
        bool isApproved = true)
    {
        string title = titleResolver?.Invoke(match.RelPath) ?? match.RelPath ?? "";
        string currentZh = snapshot.Segment?.ZhText ?? "";
        string editorText = BuildTmEditorText(match, title);
        var ranges = BuildTmHighlightRanges(editorText, match.SourceText ?? "", currentZh);

        var editor = BuildAssistantEditor(
            editorText,
            ranges,
            minHeight: 90,
            maxHeight: 220,
            brushResolver,
            postProcessor,
            AssistantHighlightStyle.Tm);

        // Wrap editor + optional variant note in a vertical stack
        var cardContent = new StackPanel { Spacing = 4 };
        cardContent.Children.Add(editor);

        if (match.IsVariantMatch && !string.IsNullOrEmpty(match.VariantNote))
        {
            cardContent.Children.Add(new TextBlock
            {
                Text = match.VariantNote,
                FontStyle = FontStyle.Italic,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#D4A017")), // amber
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(2, 0, 0, 0),
            });
        }

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse(isApproved ? "#4CAF50" : "#42A5F5")),
            BorderThickness = new Thickness(3, 0, 0, 0),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = cardContent,
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

        var editor = BuildAssistantEditor(
            editorText,
            ranges,
            minHeight: 70,
            maxHeight: 180,
            brushResolver,
            postProcessor,
            AssistantHighlightStyle.Term);

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#FFA726")),
            BorderThickness = new Thickness(3, 0, 0, 0),
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
        Action<TextEditor>? postProcessor = null,
        AssistantHighlightStyle highlightStyle = AssistantHighlightStyle.None)
    {
        var editor = BuildAssistantEditor(
            $"[{issue.Severity}] {issue.Message}",
            Array.Empty<AssistantTextRange>(),
            minHeight: 56,
            maxHeight: 140,
            brushResolver,
            postProcessor,
            AssistantHighlightStyle.None);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#EF5350")),
            BorderThickness = new Thickness(3, 0, 0, 0),
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
        Action<TextEditor>? postProcessor = null,
        AssistantHighlightStyle highlightStyle = AssistantHighlightStyle.None)
    {
        var editor = new TextEditor
        {
            IsReadOnly = true,
            ShowLineNumbers = false,
            WordWrap = true,
            FontFamily = new FontFamily("Consolas, Menlo, 'DejaVu Sans Mono', 'Noto Sans CJK SC', 'Source Han Sans SC', monospace"),
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = brushResolver?.Invoke("XmlViewerBg") ?? Brushes.Black,
            Foreground = brushResolver?.Invoke("TextFg") ?? Brushes.White,
            MinHeight = minHeight,
            MaxHeight = maxHeight
        };

        // Ensure Document is initialized before setting text (AvaloniaEdit
        // may not create it during the object initializer).
        editor.Document ??= new AvaloniaEdit.Document.TextDocument();
        editor.Document.Text = text ?? "";

        if (editor.TextArea?.TextView != null && highlightRanges.Count > 0)
        {
            var colorizer = new AssistantHighlightColorizer(highlightRanges, highlightStyle);
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

        if (!string.IsNullOrWhiteSpace(t.CreatedBy))
            sb.AppendLine($"By: {t.CreatedBy}");

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
    internal sealed class AssistantHighlightColorizer : DocumentColorizingTransformer
    {
        private readonly IReadOnlyList<AssistantTextRange> _ranges;
        private readonly AssistantHighlightStyle _style;

        public AssistantHighlightColorizer(
            IReadOnlyList<AssistantTextRange> ranges,
            AssistantHighlightStyle style)
        {
            _ranges = ranges ?? Array.Empty<AssistantTextRange>();
            _style = style;
        }

        private static IBrush Brush(string key, IBrush fallback)
        {
            var app = Application.Current;
            if (app != null && app.TryFindResource(key, out var res) && res is IBrush b)
                return b;
            return fallback;
        }

        internal static string GetBrushResourceKey(AssistantHighlightStyle style)
            => style switch
            {
                AssistantHighlightStyle.Term => "WarningBrush",
                AssistantHighlightStyle.Tm => "NoteMarkerCommunityFg",
                _ => "TextFg"
            };

        internal static bool UsesSemiBold(AssistantHighlightStyle style)
            => style != AssistantHighlightStyle.None;

        protected override void ColorizeLine(AvaloniaEdit.Document.DocumentLine line)
        {
            if (_ranges.Count == 0)
                return;

            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            var fg = _style switch
            {
                AssistantHighlightStyle.Term => Brush(GetBrushResourceKey(_style), new SolidColorBrush(Color.FromRgb(214, 145, 0))),
                AssistantHighlightStyle.Tm => Brush(GetBrushResourceKey(_style), Avalonia.Media.Brushes.DodgerBlue),
                _ => Brush(GetBrushResourceKey(_style), Avalonia.Media.Brushes.Black)
            };

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
                    if (UsesSemiBold(_style))
                    {
                        el.TextRunProperties.SetTypeface(
                            new Typeface(
                                el.TextRunProperties.Typeface.FontFamily,
                                el.TextRunProperties.Typeface.Style,
                                FontWeight.SemiBold,
                                el.TextRunProperties.Typeface.Stretch));
                    }
                });
            }
        }
    }
}

