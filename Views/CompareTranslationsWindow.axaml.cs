using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

/// <summary>
/// A 3-pane comparison window that shows original Chinese text side-by-side with
/// two different translations. Clicking a segment in any pane selects the
/// corresponding segment in the other two using segment KEY matching.
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
    private string _relPath = string.Empty;
    private string _sourceAKey = "community";
    private string _sourceBKey = "community";
    private string? _actualUsername;

    public CompareTranslationsWindow()
    {
        InitializeComponent();
        _dictOverlayCanvas = this.FindControl<Canvas>("DictOverlayCanvas");
        Closed += (_, _) => DisposeHoverDictionary();
    }

    public void LoadComparison(CompareTranslationsRequestData data, string? actualUsername = null)
    {
        _relPath = data.RelPath ?? string.Empty;
        _sourceAKey = data.SourceAKey ?? "community";
        _sourceBKey = data.SourceBKey ?? "community";
        _actualUsername = actualUsername;
        _docOriginal = data.OriginalDoc;
        _docTransA = data.TranslationADoc;
        _docTransB = data.TranslationBDoc;

        _edOriginal = this.FindControl<TextEditor>("EditorOriginal");
        _edTransA = this.FindControl<TextEditor>("EditorTransA");
        _edTransB = this.FindControl<TextEditor>("EditorTransB");
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");
        _txtPaneAHeader = this.FindControl<TextBlock>("TxtPaneAHeader");
        _txtPaneBHeader = this.FindControl<TextBlock>("TxtPaneBHeader");

        if (_txtHeader != null) _txtHeader.Text = $"Compare Translations - {data.Title}";
        if (_txtPaneAHeader != null) _txtPaneAHeader.Text = data.TranslationALabel;
        if (_txtPaneBHeader != null) _txtPaneBHeader.Text = data.TranslationBLabel;

        if (_edOriginal != null) { _edOriginal.Text = _docOriginal.Text ?? string.Empty; ConfigureEditor(_edOriginal); }
        if (_edTransA != null) { _edTransA.Text = _docTransA.Text ?? string.Empty; ConfigureEditor(_edTransA); }
        if (_edTransB != null) { _edTransB.Text = _docTransB.Text ?? string.Empty; ConfigureEditor(_edTransB); }

        AttachContextMenus();
        WireSelectionMirroring();
        SetupHoverDictionary();

        if (data.LandingPane.HasValue && data.LandingNavigation != null)
        {
            Dispatcher.UIThread.Post(async () => await NavigateToAsync(data.LandingPane.Value, data.LandingNavigation), DispatcherPriority.Loaded);
        }
    }

    public async Task NavigateToAsync(ComparePaneTarget pane, NavigationRequest request)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);

        var (editor, doc) = GetPaneState(pane);
        if (editor?.Document == null || doc == null || doc.IsEmpty)
            return;

        if (!string.IsNullOrWhiteSpace(request.FromLb) && TrySelectByLbRange(editor, doc, request.FromLb!, request.ToLb, out var lbKey))
        {
            MirrorSelection(pane, request.FromLb!, request.ToLb, lbKey);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.MatchText))
            return;

        var docText = doc.Text ?? string.Empty;
        var idx = docText.IndexOf(request.MatchText, StringComparison.Ordinal);
        if (idx < 0)
            idx = docText.IndexOf(request.MatchText, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return;

        SelectSegment(editor, idx, idx + request.MatchText.Length);
        var seg = doc.FindSegmentAtOrBefore(idx);
        MirrorSelection(pane, null, null, seg?.Key);
    }

    private void AttachContextMenus()
    {
        if (_edOriginal != null && _docOriginal != null)
            _edOriginal.ContextMenu = BuildContextMenu(ComparePaneTarget.Original, _edOriginal, _docOriginal);
        if (_edTransA != null && _docTransA != null)
            _edTransA.ContextMenu = BuildContextMenu(ComparePaneTarget.TranslationA, _edTransA, _docTransA);
        if (_edTransB != null && _docTransB != null)
            _edTransB.ContextMenu = BuildContextMenu(ComparePaneTarget.TranslationB, _edTransB, _docTransB);
    }

    private ContextMenu BuildContextMenu(ComparePaneTarget pane, TextEditor editor, RenderedDocument doc)
    {
        var menu = new ContextMenu();
        var copyLink = new MenuItem { Header = "Copy Link" };
        copyLink.Click += async (_, _) => await CopyCompareLinkAsync(pane, editor, doc, shareable: false);
        menu.Items.Add(copyLink);

        var copyReddit = new MenuItem { Header = "Copy Reddit Link" };
        copyReddit.Click += async (_, _) => await CopyCompareLinkAsync(pane, editor, doc, shareable: true);
        menu.Items.Add(copyReddit);

        return menu;
    }

    private async Task CopyCompareLinkAsync(ComparePaneTarget pane, TextEditor editor, RenderedDocument doc, bool shareable)
    {
        if (string.IsNullOrWhiteSpace(_relPath))
            return;

        GetSelectionAnchor(editor, doc, out var fromLb, out var toLb, out var highlight);

        // For shareable links, replace "me" with the actual username so others can see your translation
        var keyA = shareable && _sourceAKey == "me" && !string.IsNullOrWhiteSpace(_actualUsername) ? _actualUsername : _sourceAKey;
        var keyB = shareable && _sourceBKey == "me" && !string.IsNullOrWhiteSpace(_actualUsername) ? _actualUsername : _sourceBKey;

        var link = shareable
            ? ZenUriParser.BuildShareableCompareUrl(_relPath, pane, keyA, keyB, fromLb, toLb, highlight)
            : ZenUriParser.BuildCompareUri(_relPath, pane, _sourceAKey, _sourceBKey, fromLb, toLb, highlight);

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
            await top.Clipboard.SetTextAsync(link);
    }

    private void GetSelectionAnchor(TextEditor editor, RenderedDocument doc, out string? fromLb, out string? toLb, out string? highlight)
    {
        fromLb = null;
        toLb = null;
        highlight = null;

        int selStart = Math.Min(editor.SelectionStart, editor.SelectionStart + editor.SelectionLength);
        int selEnd = Math.Max(editor.SelectionStart, editor.SelectionStart + editor.SelectionLength);
        bool hasSelection = selEnd > selStart;

        if (hasSelection)
        {
            fromLb = LbHelper.FindNearestLbNValue(doc, selStart);
            toLb = LbHelper.FindNearestLbNValue(doc, Math.Max(selStart, selEnd - 1));

            if (string.IsNullOrWhiteSpace(fromLb))
            {
                highlight = editor.SelectedText;
                if (string.IsNullOrWhiteSpace(highlight))
                    highlight = null;
                else if (highlight.Length > 60)
                    highlight = highlight[..60];
            }
        }
        else
        {
            int caret = editor.TextArea?.Caret.Offset ?? 0;
            fromLb = LbHelper.FindNearestLbNValue(doc, caret);
            toLb = fromLb;
        }

        if (!string.IsNullOrWhiteSpace(fromLb))
            highlight = null;
    }

    private void MirrorSelection(ComparePaneTarget sourcePane, string? fromLb, string? toLb, string? key)
    {
        if (!string.IsNullOrWhiteSpace(fromLb))
        {
            if (sourcePane != ComparePaneTarget.Original)
                TrySelectByLbRange(_edOriginal, _docOriginal, fromLb!, toLb, out _);
            if (sourcePane != ComparePaneTarget.TranslationA)
                TrySelectByLbRange(_edTransA, _docTransA, fromLb!, toLb, out _);
            if (sourcePane != ComparePaneTarget.TranslationB)
                TrySelectByLbRange(_edTransB, _docTransB, fromLb!, toLb, out _);
            return;
        }

        if (string.IsNullOrWhiteSpace(key))
            return;

        if (sourcePane != ComparePaneTarget.Original)
            SelectByKey(_edOriginal, _docOriginal, key);
        if (sourcePane != ComparePaneTarget.TranslationA)
            SelectByKey(_edTransA, _docTransA, key);
        if (sourcePane != ComparePaneTarget.TranslationB)
            SelectByKey(_edTransB, _docTransB, key);
    }

    private (TextEditor? Editor, RenderedDocument? Doc) GetPaneState(ComparePaneTarget pane) => pane switch
    {
        ComparePaneTarget.Original => (_edOriginal, _docOriginal),
        ComparePaneTarget.TranslationA => (_edTransA, _docTransA),
        ComparePaneTarget.TranslationB => (_edTransB, _docTransB),
        _ => (_edOriginal, _docOriginal),
    };

    private static bool TrySelectByLbRange(TextEditor? editor, RenderedDocument? doc, string fromLb, string? toLb, out string? matchedKey)
    {
        matchedKey = null;
        if (editor?.TextArea == null || doc == null)
            return false;

        int start = -1;
        int end = -1;

        foreach (var seg in doc.Segments)
        {
            if (TryMatchLb(seg.Key, fromLb))
            {
                start = seg.Start;
                matchedKey = seg.Key;
                break;
            }
        }

        if (start < 0)
            return false;

        var targetTo = string.IsNullOrWhiteSpace(toLb) ? fromLb : toLb!;
        foreach (var seg in doc.Segments)
        {
            if (TryMatchLb(seg.Key, targetTo))
                end = seg.EndExclusive;
        }

        if (end < start)
            end = start;

        SelectSegment(editor, start, end);
        return true;
    }

    private static bool TryMatchLb(string key, string lb)
    {
        var nValue = LbHelper.ExtractLbNValue(key);
        return !string.IsNullOrWhiteSpace(nValue) && string.Equals(nValue, lb, StringComparison.OrdinalIgnoreCase);
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
            int len = (ed.Text ?? string.Empty).Length;
            int s = Math.Clamp(start, 0, len);
            int e = Math.Clamp(end, 0, len);
            ed.TextArea.Selection = Selection.Create(ed.TextArea, s, e);

            var line = ed.Document?.GetLineByOffset(s);
            if (line != null) ed.ScrollToLine(line.LineNumber);
        }
        catch { }
    }
}
