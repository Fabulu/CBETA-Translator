using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Views;

/// <summary>
/// A 3-pane comparison window that shows original text side-by-side with
/// two users' tag highlights. Clicking a segment in any pane selects the
/// corresponding segment in the other two.
/// </summary>
public partial class CompareTagsWindow : Window
{
    private TextEditor? _edOriginal, _edMyTags, _edOtherTags;
    private TextBlock? _txtHeader, _txtPane2Header, _txtPane3Header;
    private RenderedDocument? _doc;
    private bool _syncing;

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
        TagVocabulary? otherVocab)
    {
        _doc = doc;

        _edOriginal = this.FindControl<TextEditor>("EditorOriginal");
        _edMyTags = this.FindControl<TextEditor>("EditorMyTags");
        _edOtherTags = this.FindControl<TextEditor>("EditorOtherTags");
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");
        _txtPane2Header = this.FindControl<TextBlock>("TxtPane2Header");
        _txtPane3Header = this.FindControl<TextBlock>("TxtPane3Header");

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
    }

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
