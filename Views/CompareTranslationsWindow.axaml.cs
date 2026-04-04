using System;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

/// <summary>
/// A 3-pane comparison window that shows original Chinese text side-by-side with
/// two different translations. Clicking a segment in any pane selects the
/// corresponding segment in the other two using segment KEY matching (since each
/// pane has different text from different RenderedDocuments).
/// </summary>
public partial class CompareTranslationsWindow : Window
{
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();
    private HoverDictionaryBehaviorEdit? _hoverDictOrig;
    private Canvas? _dictOverlayCanvas;
    private TextEditor? _edOriginal, _edTransA, _edTransB;
    private TextBlock? _txtHeader, _txtPaneAHeader, _txtPaneBHeader;
    private RenderedDocument? _docOriginal, _docTransA, _docTransB;
    private bool _syncing;

    public CompareTranslationsWindow()
    {
        InitializeComponent();
        _dictOverlayCanvas = this.FindControl<Canvas>("DictOverlayCanvas");
        Closed += (_, _) => DisposeHoverDictionary();
    }

    /// <summary>
    /// Populates the three editors with their respective document texts and wires
    /// segment-key-based selection mirroring. Call once after construction.
    /// </summary>
    public void LoadComparison(CompareTranslationsRequestData data)
    {
        _docOriginal = data.OriginalDoc;
        _docTransA = data.TranslationADoc;
        _docTransB = data.TranslationBDoc;

        _edOriginal = this.FindControl<TextEditor>("EditorOriginal");
        _edTransA = this.FindControl<TextEditor>("EditorTransA");
        _edTransB = this.FindControl<TextEditor>("EditorTransB");
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");
        _txtPaneAHeader = this.FindControl<TextBlock>("TxtPaneAHeader");
        _txtPaneBHeader = this.FindControl<TextBlock>("TxtPaneBHeader");

        if (_txtHeader != null) _txtHeader.Text = $"Compare Translations — {data.Title}";
        if (_txtPaneAHeader != null) _txtPaneAHeader.Text = data.TranslationALabel;
        if (_txtPaneBHeader != null) _txtPaneBHeader.Text = data.TranslationBLabel;

        if (_edOriginal != null) { _edOriginal.Text = _docOriginal.Text ?? ""; ConfigureEditor(_edOriginal); }
        if (_edTransA != null) { _edTransA.Text = _docTransA.Text ?? ""; ConfigureEditor(_edTransA); }
        if (_edTransB != null) { _edTransB.Text = _docTransB.Text ?? ""; ConfigureEditor(_edTransB); }

        WireSelectionMirroring();
        SetupHoverDictionary();
    }

    private void SetupHoverDictionary()
    {
        DisposeHoverDictionary();
        if (_edOriginal == null || _dictOverlayCanvas == null) return;
        try { _hoverDictOrig = new HoverDictionaryBehaviorEdit(_edOriginal, _cedict, _grammar, _dictOverlayCanvas); }
        catch { _hoverDictOrig = null; }
    }

    private void DisposeHoverDictionary()
    {
        try { _hoverDictOrig?.Dispose(); } catch { }
        _hoverDictOrig = null;
    }

    private static void ConfigureEditor(TextEditor ed)
    {
        ed.IsReadOnly = true;
        ed.ShowLineNumbers = false;
        ed.WordWrap = true;
        ed.FontFamily = new FontFamily("Consolas, Menlo, 'Noto Sans CJK SC', monospace");
    }

    private void WireSelectionMirroring()
    {
        WireMirror(_edOriginal, _docOriginal, _edTransA, _docTransA, _edTransB, _docTransB);
        WireMirror(_edTransA, _docTransA, _edOriginal, _docOriginal, _edTransB, _docTransB);
        WireMirror(_edTransB, _docTransB, _edOriginal, _docOriginal, _edTransA, _docTransA);
    }

    private void WireMirror(
        TextEditor? source, RenderedDocument? sourceDoc,
        TextEditor? target1, RenderedDocument? targetDoc1,
        TextEditor? target2, RenderedDocument? targetDoc2)
    {
        if (source?.TextArea == null) return;

        source.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (_syncing || sourceDoc == null) return;
            _syncing = true;
            try
            {
                int offset = source.TextArea.Caret.Offset;
                var seg = sourceDoc.FindSegmentAtOrBefore(offset);
                if (seg == null) return;

                string key = seg.Value.Key;
                SelectSegment(source, seg.Value.Start, seg.Value.EndExclusive);
                SelectByKey(target1, targetDoc1, key);
                SelectByKey(target2, targetDoc2, key);
            }
            finally { _syncing = false; }
        };
    }

    private static void SelectByKey(TextEditor? ed, RenderedDocument? doc, string key)
    {
        if (ed?.TextArea == null || doc == null) return;

        if (doc.TryGetSegmentByKey(key, out var seg))
        {
            SelectSegment(ed, seg.Start, seg.EndExclusive);
            return;
        }

        var nValue = LbHelper.ExtractLbNValue(key);
        if (nValue != null)
        {
            foreach (var suffix in new[] { "", "CB", "CBETA", "T", "X", "J" })
            {
                var tryKey = string.IsNullOrEmpty(suffix)
                    ? "lb|" + nValue
                    : "lb|" + nValue + "|" + suffix;

                if (doc.TryGetSegmentByKey(tryKey, out seg))
                {
                    SelectSegment(ed, seg.Start, seg.EndExclusive);
                    return;
                }
            }

            foreach (var s in doc.Segments)
            {
                if (s.Key.StartsWith("lb|", StringComparison.Ordinal))
                {
                    var parts = s.Key.Split('|');
                    if (parts.Length >= 2 && parts[1] == nValue)
                    {
                        SelectSegment(ed, s.Start, s.EndExclusive);
                        return;
                    }
                }
            }
        }
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
        catch { }
    }
}
