using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// A 3-pane comparison window that shows original text side-by-side with
/// two users' tag highlights. Below the editors: inter-rater metrics,
/// memo preview, and a consensus disagreement panel.
/// </summary>
public partial class CompareTagsWindow : Window
{
    private TextEditor? _edOriginal, _edMyTags, _edOtherTags;
    private TextBlock? _txtHeader, _txtPane2Header, _txtPane3Header;
    private TextBlock? _txtOverallMetrics, _txtMemoPreview;
    private Border? _borderMemoPreview;
    private WrapPanel? _wrapPerCode;
    private StackPanel? _panelDisagreements;
    private Button? _btnApplyConsensus;
    private Expander? _expanderConsensus;
    private RenderedDocument? _doc;
    private bool _syncing;

    // Data retained for consensus + memo
    private string _communityRoot = "";
    private string _resolverUsername = "";
    private string _myUsername = "";
    private string _otherUsername = "";
    private List<DocumentTag> _myTags = new();
    private List<DocumentTag> _otherTags = new();
    private List<Disagreement> _disagreements = new();
    private readonly Dictionary<string, ComboBox> _resolutionCombos = new();

    public CompareTagsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Populates the three editors with the same text and applies tag highlights.
    /// Call once after construction.
    /// </summary>
    public void LoadComparison(
        string title,
        RenderedDocument doc,
        string myUsername,
        List<DocumentTag> myTags,
        TagVocabulary? myVocab,
        string otherUsername,
        List<DocumentTag> otherTags,
        TagVocabulary? otherVocab,
        string communityRoot = "",
        string resolverUsername = "")
    {
        _doc = doc;
        _communityRoot = communityRoot;
        _resolverUsername = resolverUsername;
        _myUsername = myUsername;
        _otherUsername = otherUsername;
        _myTags = myTags;
        _otherTags = otherTags;

        _edOriginal = this.FindControl<TextEditor>("EditorOriginal");
        _edMyTags = this.FindControl<TextEditor>("EditorMyTags");
        _edOtherTags = this.FindControl<TextEditor>("EditorOtherTags");
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");
        _txtPane2Header = this.FindControl<TextBlock>("TxtPane2Header");
        _txtPane3Header = this.FindControl<TextBlock>("TxtPane3Header");
        _txtOverallMetrics = this.FindControl<TextBlock>("TxtOverallMetrics");
        _txtMemoPreview = this.FindControl<TextBlock>("TxtMemoPreview");
        _borderMemoPreview = this.FindControl<Border>("BorderMemoPreview");
        _wrapPerCode = this.FindControl<WrapPanel>("WrapPerCode");
        _panelDisagreements = this.FindControl<StackPanel>("PanelDisagreements");
        _btnApplyConsensus = this.FindControl<Button>("BtnApplyConsensus");
        _expanderConsensus = this.FindControl<Expander>("ExpanderConsensus");

        if (_txtHeader != null) _txtHeader.Text = $"Compare Tags \u2014 {title}";
        if (_txtPane2Header != null) _txtPane2Header.Text = $"My Tags ({myUsername})";
        if (_txtPane3Header != null) _txtPane3Header.Text = $"{otherUsername}'s Tags";

        string text = doc.Text ?? "";

        if (_edOriginal != null) { _edOriginal.Text = text; ConfigureEditor(_edOriginal); }
        if (_edMyTags != null) { _edMyTags.Text = text; ConfigureEditor(_edMyTags); }
        if (_edOtherTags != null) { _edOtherTags.Text = text; ConfigureEditor(_edOtherTags); }

        // Apply tag highlights to pane 2 and 3
        ApplyTagHighlights(_edMyTags, doc, myTags, myVocab);
        ApplyTagHighlights(_edOtherTags, doc, otherTags, otherVocab);

        // Wire selection mirroring across all three editors
        WireSelectionMirroring();

        // Compute and display inter-rater metrics
        ComputeAndDisplayMetrics(doc, myUsername, myTags, myVocab, otherUsername, otherTags, otherVocab);

        // Build disagreement panel
        BuildDisagreementPanel(doc, myTags, otherTags, myVocab, otherVocab);

        // Wire memo preview on caret move
        WireMemoPreview();

        // Wire consensus save button
        if (_btnApplyConsensus != null)
            _btnApplyConsensus.Click += async (_, _) => await SaveConsensusResolutionsAsync();
    }

    // ── Metrics ─────────────────────────────────────────────────────────

    private void ComputeAndDisplayMetrics(
        RenderedDocument doc,
        string myUsername,
        List<DocumentTag> myTags,
        TagVocabulary? myVocab,
        string otherUsername,
        List<DocumentTag> otherTags,
        TagVocabulary? otherVocab)
    {
        var lbs = InterRaterService.ExtractLbValues(doc);
        if (lbs.Count == 0)
        {
            if (_txtOverallMetrics != null)
                _txtOverallMetrics.Text = "No lb segments found in this document.";
            return;
        }

        string relPath = myTags.FirstOrDefault()?.RelPath
                      ?? otherTags.FirstOrDefault()?.RelPath
                      ?? "";

        var result = InterRaterService.Compare(
            relPath, myUsername, otherUsername, lbs,
            myTags, otherTags, myVocab, otherVocab);

        if (_txtOverallMetrics != null)
        {
            _txtOverallMetrics.Text =
                $"Overall: {result.OverallPercentAgreement:P1} agreement, " +
                $"\u03BA = {result.OverallCohensKappa:F3}  " +
                $"({result.TotalUnits} lb units)";
        }

        if (_wrapPerCode != null)
        {
            _wrapPerCode.Children.Clear();
            foreach (var pc in result.PerCode)
            {
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2),
                    Margin = new Thickness(0, 0, 4, 4),
                    Background = GetKappaBrush(pc.CohensKappa),
                    Child = new TextBlock
                    {
                        Text = $"{pc.TagName}: {pc.PercentAgreement:P0} (\u03BA={pc.CohensKappa:F2})",
                        FontSize = 11,
                        Foreground = Brushes.White
                    }
                };
                _wrapPerCode.Children.Add(badge);
            }
        }
    }

    private static IBrush GetKappaBrush(double kappa)
    {
        if (kappa >= 0.8) return new SolidColorBrush(Color.FromRgb(39, 174, 96));   // green
        if (kappa >= 0.6) return new SolidColorBrush(Color.FromRgb(241, 196, 15));  // yellow
        if (kappa >= 0.4) return new SolidColorBrush(Color.FromRgb(230, 126, 34));  // orange
        return new SolidColorBrush(Color.FromRgb(231, 76, 60));                     // red
    }

    // ── Disagreements ───────────────────────────────────────────────────

    private void BuildDisagreementPanel(
        RenderedDocument doc,
        List<DocumentTag> myTags,
        List<DocumentTag> otherTags,
        TagVocabulary? myVocab,
        TagVocabulary? otherVocab)
    {
        if (_panelDisagreements == null) return;

        var lbs = InterRaterService.ExtractLbValues(doc);
        string relPath = myTags.FirstOrDefault()?.RelPath
                      ?? otherTags.FirstOrDefault()?.RelPath
                      ?? "";

        _disagreements = ConsensusService.FindDisagreements(
            relPath, lbs, myTags, otherTags, myVocab, otherVocab);

        _panelDisagreements.Children.Clear();
        _resolutionCombos.Clear();

        if (_disagreements.Count == 0)
        {
            _panelDisagreements.Children.Add(new TextBlock
            {
                Text = "No disagreements found. Both coders agree on all tags.",
                Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                Margin = new Thickness(4)
            });
            return;
        }

        if (_btnApplyConsensus != null)
            _btnApplyConsensus.IsEnabled = !string.IsNullOrEmpty(_communityRoot);

        foreach (var d in _disagreements)
        {
            string who = d.Coder1HasIt ? _myUsername : _otherUsername;
            string lbRange = d.FromLb == d.ToLb ? d.FromLb : $"{d.FromLb}\u2013{d.ToLb}";

            var combo = new ComboBox
            {
                Items = { _myUsername, _otherUsername },
                SelectedIndex = d.Coder1HasIt ? 0 : 1,
                MinWidth = 100,
                FontSize = 11
            };

            string key = $"{d.FromLb}|{d.TagId}";
            _resolutionCombos[key] = combo;

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(4, 1)
            };
            row.Children.Add(new TextBlock
            {
                Text = $"[{lbRange}] {d.TagName} \u2014 only {who}",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                MinWidth = 350
            });
            row.Children.Add(new TextBlock
            {
                Text = "Accept:",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });
            row.Children.Add(combo);

            _panelDisagreements.Children.Add(row);
        }
    }

    private async System.Threading.Tasks.Task SaveConsensusResolutionsAsync()
    {
        if (string.IsNullOrEmpty(_communityRoot) || string.IsNullOrEmpty(_resolverUsername))
            return;

        var resolutions = new List<ConsensusResolution>();
        string relPath = _myTags.FirstOrDefault()?.RelPath
                      ?? _otherTags.FirstOrDefault()?.RelPath
                      ?? "";

        foreach (var d in _disagreements)
        {
            string key = $"{d.FromLb}|{d.TagId}";
            if (!_resolutionCombos.TryGetValue(key, out var combo)) continue;

            string accepted = combo.SelectedItem as string ?? _myUsername;
            string rejected = accepted == _myUsername ? _otherUsername : _myUsername;

            resolutions.Add(new ConsensusResolution
            {
                Id = Guid.NewGuid().ToString("N"),
                RelPath = relPath,
                FromLb = d.FromLb,
                ToLb = d.ToLb,
                TagId = d.TagId,
                AcceptedCoder = accepted,
                RejectedCoder = rejected,
                ResolvedBy = _resolverUsername,
                ResolvedUtc = DateTimeOffset.UtcNow
            });
        }

        try
        {
            var svc = new ConsensusService();
            await svc.SaveResolutionsAsync(_communityRoot, _resolverUsername, resolutions);
            if (_btnApplyConsensus != null)
                _btnApplyConsensus.Content = $"Saved {resolutions.Count} resolutions";
        }
        catch (Exception ex)
        {
            if (_btnApplyConsensus != null)
                _btnApplyConsensus.Content = "Save failed: " + ex.Message;
        }
    }

    // ── Memo preview ────────────────────────────────────────────────────

    private void WireMemoPreview()
    {
        // When caret moves in pane 2 (my tags), show memo for the tag at that position
        if (_edMyTags?.TextArea == null || _doc == null) return;

        _edMyTags.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (_doc == null || _borderMemoPreview == null || _txtMemoPreview == null) return;

            int offset = _edMyTags.TextArea.Caret.Offset;
            var seg = _doc.FindSegmentAtOrBefore(offset);
            if (seg == null) { HideMemo(); return; }

            // Find lb at caret position
            string? lbAtCaret = FindLbForSegment(seg.Value);
            if (lbAtCaret == null) { HideMemo(); return; }

            // Find any of my tags that cover this lb
            var tagAtCaret = _myTags.FirstOrDefault(t =>
                string.Compare(t.FromLb, lbAtCaret, StringComparison.Ordinal) <= 0 &&
                string.Compare(lbAtCaret, t.ToLb, StringComparison.Ordinal) <= 0 &&
                !string.IsNullOrEmpty(t.Memo));

            if (tagAtCaret != null)
            {
                _txtMemoPreview.Text = tagAtCaret.Memo;
                _borderMemoPreview.IsVisible = true;
            }
            else
            {
                HideMemo();
            }
        };
    }

    private void HideMemo()
    {
        if (_borderMemoPreview != null) _borderMemoPreview.IsVisible = false;
        if (_txtMemoPreview != null) _txtMemoPreview.Text = "";
    }

    private static string? FindLbForSegment(RenderSegment seg)
    {
        if (!seg.Key.StartsWith("lb|", StringComparison.Ordinal))
            return null;
        var parts = seg.Key.Split('|');
        return parts.Length >= 2 ? parts[1] : null;
    }

    // ── Editor config + tag highlights (unchanged) ──────────────────────

    private static void ConfigureEditor(TextEditor ed)
    {
        ed.IsReadOnly = true;
        ed.ShowLineNumbers = false;
        ed.WordWrap = true;
        ed.FontFamily = new FontFamily("Consolas, Menlo, 'Noto Sans CJK SC', monospace");
    }

    private static void ApplyTagHighlights(
        TextEditor? editor,
        RenderedDocument doc,
        List<DocumentTag> tags,
        TagVocabulary? vocab)
    {
        if (editor?.TextArea?.TextView == null) return;

        var tagLookup = vocab?.Tags.ToDictionary(t => t.Id, StringComparer.Ordinal)
                        ?? new Dictionary<string, TagDefinition>(StringComparer.Ordinal);

        var ranges = new List<(int Start, int Length, IBrush Brush)>();

        foreach (var tag in tags)
        {
            if (string.IsNullOrEmpty(tag.FromLb)) continue;

            Color color;
            if (tagLookup.TryGetValue(tag.TagId, out var def))
            {
                try { color = Color.Parse(def.Color); }
                catch { color = Color.FromRgb(52, 152, 219); }
            }
            else
            {
                color = Color.FromRgb(128, 128, 128);
            }

            if (!TryFindSegmentByLb(doc, tag.FromLb, out var startSeg)) continue;
            int rangeStart = startSeg.Start;
            int rangeEnd = startSeg.EndExclusive;

            if (!string.IsNullOrEmpty(tag.ToLb) && tag.ToLb != tag.FromLb)
            {
                if (TryFindSegmentByLb(doc, tag.ToLb, out var endSeg))
                    rangeEnd = endSeg.EndExclusive;
            }

            if (rangeEnd > rangeStart)
            {
                var brush = new SolidColorBrush(Color.FromArgb(77, color.R, color.G, color.B));
                ranges.Add((rangeStart, rangeEnd - rangeStart, brush));
            }
        }

        var transformer = new CompareTagColorizer(ranges);
        editor.TextArea.TextView.LineTransformers.Add(transformer);
        editor.TextArea.TextView.Redraw();
    }

    /// <summary>
    /// Resolves an lb n-value to a segment, trying bare key, edition suffixes, and brute-force.
    /// Mirrors the logic in ReadableTabView.TryFindSegmentByLb.
    /// </summary>
    private static bool TryFindSegmentByLb(RenderedDocument doc, string nValue, out RenderSegment seg)
    {
        if (doc.TryGetSegmentByKey("lb|" + nValue, out seg))
            return true;

        foreach (var suffix in new[] { "CB", "CBETA", "T", "X", "J" })
        {
            if (doc.TryGetSegmentByKey("lb|" + nValue + "|" + suffix, out seg))
                return true;
        }

        foreach (var s in doc.Segments)
        {
            if (s.Key.StartsWith("lb|", StringComparison.Ordinal))
            {
                var parts = s.Key.Split('|');
                if (parts.Length >= 2 && parts[1] == nValue)
                {
                    seg = s;
                    return true;
                }
            }
        }

        seg = default;
        return false;
    }

    private void WireSelectionMirroring()
    {
        WireMirror(_edOriginal, _edMyTags, _edOtherTags);
        WireMirror(_edMyTags, _edOriginal, _edOtherTags);
        WireMirror(_edOtherTags, _edOriginal, _edMyTags);
    }

    private void WireMirror(TextEditor? source, TextEditor? target1, TextEditor? target2)
    {
        if (source?.TextArea == null) return;

        source.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (_syncing || _doc == null) return;
            _syncing = true;
            try
            {
                int offset = source.TextArea.Caret.Offset;
                var seg = _doc.FindSegmentAtOrBefore(offset);
                if (seg == null) return;

                int len = (source.Text ?? "").Length;
                int s = Math.Clamp(seg.Value.Start, 0, len);
                int e = Math.Clamp(seg.Value.EndExclusive, 0, len);

                SelectSegment(source, s, e);
                SelectSegment(target1, s, e);
                SelectSegment(target2, s, e);
            }
            finally { _syncing = false; }
        };
    }

    private static void SelectSegment(TextEditor? ed, int start, int end)
    {
        if (ed?.TextArea == null) return;
        try
        {
            int len = (ed.Text ?? "").Length;
            int s = Math.Clamp(start, 0, len);
            int e = Math.Clamp(end, 0, len);
            ed.TextArea.Selection = Selection.Create(ed.TextArea, s, e);

            var line = ed.Document?.GetLineByOffset(s);
            if (line != null) ed.ScrollToLine(line.LineNumber);
        }
        catch { /* guard against race conditions during load */ }
    }

    /// <summary>
    /// Colorizer that applies semi-transparent background highlights for tag ranges.
    /// </summary>
    private sealed class CompareTagColorizer : DocumentColorizingTransformer
    {
        private readonly List<(int Start, int Length, IBrush Brush)> _ranges;

        public CompareTagColorizer(List<(int Start, int Length, IBrush Brush)> ranges)
        {
            _ranges = ranges.OrderBy(r => r.Start).ToList();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lo = LowerBound(line.Offset);
            for (int i = lo; i < _ranges.Count; i++)
            {
                var (start, length, brush) = _ranges[i];
                if (start >= line.Offset + line.Length) break;
                int s = Math.Max(start, line.Offset);
                int e = Math.Min(start + length, line.Offset + line.Length);
                if (s >= e) continue;
                ChangeLinePart(s, e, el =>
                    el.TextRunProperties.SetBackgroundBrush(brush));
            }
        }

        private int LowerBound(int lineStart)
        {
            int lo = 0, hi = _ranges.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (_ranges[mid].Start + _ranges[mid].Length <= lineStart) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }
}
