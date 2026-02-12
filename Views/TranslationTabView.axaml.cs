using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class TranslationTabView : UserControl
{
    private Button? _btnCopyPrompt;
    private Button? _btnPasteReplace;
    private Button? _btnSaveTranslated;
    private Button? _btnSelectNext50Tags;
    private Button? _btnCheckXml;

    private TextBox? _orig;
    private TextBox? _tran;
    private TextBlock? _txtHint;

    // Hover dictionary (same pattern as ReadableTabView)
    private HoverDictionaryBehavior? _hoverDict;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

    // Remember last "copy selection" range (so paste can work even if selection got lost)
    private int _lastCopyStart = -1;
    private int _lastCopyEnd = -1;

    public event EventHandler? SaveRequested;
    public event EventHandler<string>? Status;

    // -------------------------
    // Ctrl+F Find state
    // -------------------------
    private Border? _findBar;
    private TextBox? _findQuery;
    private TextBlock? _findCount;
    private TextBlock? _findScope;
    private Button? _btnPrev;
    private Button? _btnNext;
    private Button? _btnCloseFind;

    private SearchHighlightOverlay? _hlOrig;
    private SearchHighlightOverlay? _hlTran;

    private TextBox? _findTarget; // which editor we are searching in

    private readonly List<int> _matchStarts = new();
    private int _matchLen = 0;
    private int _matchIndex = -1;

    private static readonly TimeSpan FindRecomputeDebounce = TimeSpan.FromMilliseconds(140);
    private DispatcherTimer? _findDebounceTimer;

    // Track last user input editor for sane scope selection
    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private TextBox? _lastUserInputEditor;
    private const int UserInputPriorityWindowMs = 250;

    public TranslationTabView()
    {
        InitializeComponent();
        FindControls();
        WireEvents();

        AttachedToVisualTree += (_, _) =>
        {
            SetupHoverDictionary();
        };

        DetachedFromVisualTree += (_, _) =>
        {
            DisposeHoverDictionary();
        };

        UpdateHint("Select XML in Translated XML → Copy selection + prompt.");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _btnCopyPrompt = this.FindControl<Button>("BtnCopyPrompt");
        _btnPasteReplace = this.FindControl<Button>("BtnPasteReplace");
        _btnSaveTranslated = this.FindControl<Button>("BtnSaveTranslated");
        _btnSelectNext50Tags = this.FindControl<Button>("BtnSelectNext50Tags");
        _btnCheckXml = this.FindControl<Button>("BtnCheckXml");

        _orig = this.FindControl<TextBox>("EditorOrigXml");
        _tran = this.FindControl<TextBox>("EditorTranXml");

        _txtHint = this.FindControl<TextBlock>("TxtHint");

        if (_orig != null) _orig.IsReadOnly = true;
        if (_tran != null) _tran.IsReadOnly = false;

        // Find UI
        _findBar = this.FindControl<Border>("FindBar");
        _findQuery = this.FindControl<TextBox>("FindQuery");
        _findCount = this.FindControl<TextBlock>("FindCount");
        _findScope = this.FindControl<TextBlock>("FindScope");
        _btnPrev = this.FindControl<Button>("BtnPrev");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnCloseFind = this.FindControl<Button>("BtnCloseFind");

        // Highlight overlays
        _hlOrig = this.FindControl<SearchHighlightOverlay>("HlOrigXml");
        _hlTran = this.FindControl<SearchHighlightOverlay>("HlTranXml");

        if (_hlOrig != null) _hlOrig.Target = _orig;
        if (_hlTran != null) _hlTran.Target = _tran;
    }

    private void SetupHoverDictionary()
    {
        // We want it on the ORIGINAL (Chinese) pane, same as ReadableTabView.
        if (_orig == null) return;

        _hoverDict?.Dispose();
        _hoverDict = new HoverDictionaryBehavior(_orig, _cedict);
    }

    private void DisposeHoverDictionary()
    {
        _hoverDict?.Dispose();
        _hoverDict = null;
    }

    private void WireEvents()
    {
        if (_btnCopyPrompt != null) _btnCopyPrompt.Click += async (_, _) => await CopySelectionWithPromptAsync();
        if (_btnPasteReplace != null) _btnPasteReplace.Click += async (_, _) => await PasteReplaceSelectionAsync();

        // Save is gated by the SAME hacky XML check used by the Check button.
        if (_btnSaveTranslated != null) _btnSaveTranslated.Click += async (_, _) => await SaveIfValidAsync();

        if (_btnSelectNext50Tags != null) _btnSelectNext50Tags.Click += async (_, _) => await SelectNextTagsAsync(100);
        if (_btnCheckXml != null) _btnCheckXml.Click += async (_, _) => await CheckXmlWithPopupAsync();

        // Track selection range using user input events (Avalonia TextBox has no SelectionChanged event)
        if (_tran != null)
        {
            _tran.PointerReleased += (_, _) => RememberSelectionIfAny();
            _tran.KeyUp += (_, _) => RememberSelectionIfAny();
        }

        // Track “last user input editor” for find scope
        HookUserInputTracking(_orig);
        HookUserInputTracking(_tran);

        // Ctrl+F / Escape handling at control level
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        if (_findQuery != null)
        {
            _findQuery.KeyDown += FindQuery_KeyDown;
            _findQuery.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                    DebounceRecomputeMatches();
            };
        }

        if (_btnNext != null) _btnNext.Click += (_, _) => JumpNext();
        if (_btnPrev != null) _btnPrev.Click += (_, _) => JumpPrev();
        if (_btnCloseFind != null) _btnCloseFind.Click += (_, _) => CloseFind();

        // If user focuses an editor while find is open, switch scope (but keep current match if possible)
        if (_orig != null)
        {
            _orig.GotFocus += (_, _) =>
            {
                if (_findBar?.IsVisible == true)
                    SetFindTarget(_orig, preserveIndex: true);
            };
        }
        if (_tran != null)
        {
            _tran.GotFocus += (_, _) =>
            {
                if (_findBar?.IsVisible == true)
                    SetFindTarget(_tran, preserveIndex: true);
            };
        }
    }

    private void HookUserInputTracking(TextBox? tb)
    {
        if (tb == null) return;

        tb.PointerPressed += (_, _) => { _lastUserInputUtc = DateTime.UtcNow; _lastUserInputEditor = tb; };
        tb.PointerReleased += (_, _) => { _lastUserInputUtc = DateTime.UtcNow; _lastUserInputEditor = tb; };
        tb.KeyDown += (_, _) => { _lastUserInputUtc = DateTime.UtcNow; _lastUserInputEditor = tb; };
        tb.KeyUp += (_, _) => { _lastUserInputUtc = DateTime.UtcNow; _lastUserInputEditor = tb; };
    }

    private void RememberSelectionIfAny()
    {
        if (_tran == null) return;

        int s = _tran.SelectionStart;
        int e = _tran.SelectionEnd;

        if (e > s)
        {
            _lastCopyStart = s;
            _lastCopyEnd = e;
        }
    }

    public void Clear()
    {
        if (_orig != null) _orig.Text = "";
        if (_tran != null) _tran.Text = "";

        _lastCopyStart = -1;
        _lastCopyEnd = -1;
        ResetNavigationState();
        UpdateHint("Select a file to edit XML.");

        ClearFindState();
        CloseFind();
    }

    public void SetXml(string originalXml, string translatedXml)
    {
        if (_orig != null) _orig.Text = originalXml ?? "";
        if (_tran != null) _tran.Text = translatedXml ?? "";

        _lastCopyStart = -1;
        _lastCopyEnd = -1;
        ResetNavigationState();
        UpdateHint("Tip: select a chunk in Translated XML → Copy selection + prompt.");

        // keep find open, recompute
        if (_findBar?.IsVisible == true)
            RecomputeMatches(resetToFirst: false);
    }

    public string GetTranslatedXml()
        => _tran?.Text ?? string.Empty;

    // --------------------------
    // Clipboard workflow
    // --------------------------

    private async Task CopySelectionWithPromptAsync()
    {
        if (_tran == null)
        {
            Status?.Invoke(this, "Translated XML editor not available.");
            return;
        }

        var text = _tran.Text ?? "";
        if (text.Length == 0)
        {
            Status?.Invoke(this, "Translated XML is empty.");
            return;
        }

        int start = _tran.SelectionStart;
        int end = _tran.SelectionEnd;

        if (end <= start && _lastCopyEnd > _lastCopyStart)
        {
            start = _lastCopyStart;
            end = _lastCopyEnd;
        }

        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        if (end <= start)
        {
            Status?.Invoke(this, "No selection. Select an XML fragment first.");
            return;
        }

        string selectionXml = text.Substring(start, end - start);

        _lastCopyStart = start;
        _lastCopyEnd = end;

        string clipboardPayload = BuildChatGptPrompt(selectionXml);

        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            Status?.Invoke(this, "Clipboard not available (TopLevel.Clipboard is null).");
            return;
        }

        await clipboard.SetTextAsync(clipboardPayload);
        Status?.Invoke(this, $"Copied selection + prompt ({selectionXml.Length:n0} chars) to clipboard.");
    }

    private async Task PasteReplaceSelectionAsync()
    {
        if (_tran == null)
        {
            Status?.Invoke(this, "Translated XML editor not available.");
            return;
        }

        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            Status?.Invoke(this, "Clipboard not available (TopLevel.Clipboard is null).");
            return;
        }

        var clipText = await clipboard.TryGetTextAsync() ?? "";
        clipText = clipText.Trim();

        if (clipText.Length == 0)
        {
            Status?.Invoke(this, "Clipboard is empty.");
            return;
        }

        string pastedXml = ExtractXmlFromClipboard(clipText);
        if (string.IsNullOrWhiteSpace(pastedXml))
        {
            Status?.Invoke(this, "Could not find XML in clipboard text (no ```xml``` block, and content wasn't raw XML).");
            return;
        }

        int start = _tran.SelectionStart;
        int end = _tran.SelectionEnd;

        if (end <= start)
        {
            if (_lastCopyEnd > _lastCopyStart)
            {
                start = _lastCopyStart;
                end = _lastCopyEnd;
            }
            else
            {
                Status?.Invoke(this, "No active selection (and no remembered copy range). Select where to paste.");
                return;
            }
        }

        var all = _tran.Text ?? "";
        start = Math.Clamp(start, 0, all.Length);
        end = Math.Clamp(end, 0, all.Length);
        if (end < start) (start, end) = (end, start);

        var sb = new StringBuilder(all.Length - (end - start) + pastedXml.Length);
        sb.Append(all, 0, start);
        sb.Append(pastedXml);
        sb.Append(all, end, all.Length - end);

        _tran.Text = sb.ToString();

        _tran.SelectionStart = start;
        _tran.SelectionEnd = start + pastedXml.Length;
        try { _tran.CaretIndex = start; } catch { /* ignore */ }

        _lastCopyStart = start;
        _lastCopyEnd = start + pastedXml.Length;

        Status?.Invoke(this, $"Pasted & replaced selection with {pastedXml.Length:n0} chars.");
    }

    // --------------------------
    // Ctrl+F Find UI
    // --------------------------

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            OpenFind();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _findBar?.IsVisible == true)
        {
            CloseFind();
            e.Handled = true;
            return;
        }

        if (_findBar?.IsVisible == true && e.Key == Key.F3)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) JumpPrev();
            else JumpNext();
            e.Handled = true;
            return;
        }
    }

    private void FindQuery_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_findBar?.IsVisible != true) return;

        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) JumpPrev();
            else JumpNext();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseFind();
            e.Handled = true;
            return;
        }
    }

    private void OpenFind()
    {
        if (_findBar == null || _findQuery == null) return;

        _findBar.IsVisible = true;

        var target = DetermineCurrentPaneForFind();
        SetFindTarget(target, preserveIndex: false);

        _findQuery.Focus();
        _findQuery.SelectionStart = 0;
        _findQuery.SelectionEnd = (_findQuery.Text ?? "").Length;

        RecomputeMatches(resetToFirst: false);
    }

    private void CloseFind()
    {
        if (_findBar != null)
            _findBar.IsVisible = false;

        ClearHighlight();

        // restore focus without messing with selections
        _findTarget?.Focus();
    }

    private TextBox? DetermineCurrentPaneForFind()
    {
        if (_orig == null || _tran == null)
            return _tran;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;
        if (recentInput && _lastUserInputEditor != null)
            return _lastUserInputEditor;

        if (_tran.IsFocused || _tran.IsKeyboardFocusWithin) return _tran;
        if (_orig.IsFocused || _orig.IsKeyboardFocusWithin) return _orig;

        return _tran;
    }

    private void SetFindTarget(TextBox? tb, bool preserveIndex)
    {
        if (tb == null) return;

        _findTarget = tb;

        if (_findScope != null)
            _findScope.Text = ReferenceEquals(tb, _orig) ? "Find (Original):" : "Find (Translated):";

        RecomputeMatches(resetToFirst: !preserveIndex);
    }

    private void DebounceRecomputeMatches()
    {
        _findDebounceTimer ??= new DispatcherTimer { Interval = FindRecomputeDebounce };
        _findDebounceTimer.Stop();
        _findDebounceTimer.Tick -= FindDebounceTimer_Tick;
        _findDebounceTimer.Tick += FindDebounceTimer_Tick;
        _findDebounceTimer.Start();
    }

    private void FindDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _findDebounceTimer?.Stop();
        RecomputeMatches(resetToFirst: true);
    }

    private void RecomputeMatches(bool resetToFirst)
    {
        if (_findBar?.IsVisible != true) return;

        var tb = _findTarget;
        if (tb == null)
            return;

        string hay = tb.Text ?? "";
        string q = (_findQuery?.Text ?? "").Trim();

        int oldSelectedStart = -1;
        if (!resetToFirst && _matchIndex >= 0 && _matchIndex < _matchStarts.Count)
            oldSelectedStart = _matchStarts[_matchIndex];

        _matchStarts.Clear();
        _matchLen = 0;
        _matchIndex = -1;

        if (q.Length == 0 || hay.Length == 0)
        {
            UpdateFindCount();
            ClearHighlight();
            return;
        }

        _matchLen = q.Length;

        int idx = 0;
        while (true)
        {
            idx = hay.IndexOf(q, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            _matchStarts.Add(idx);
            idx = idx + Math.Max(1, q.Length);
        }

        if (_matchStarts.Count == 0)
        {
            UpdateFindCount();
            ClearHighlight();
            return;
        }

        if (resetToFirst)
        {
            int caret = tb.CaretIndex;
            int nearest = _matchStarts.FindIndex(s => s >= caret);
            _matchIndex = nearest >= 0 ? nearest : 0;
        }
        else
        {
            if (oldSelectedStart >= 0)
            {
                int exact = _matchStarts.IndexOf(oldSelectedStart);
                if (exact >= 0) _matchIndex = exact;
                else
                {
                    int nearest = _matchStarts.FindIndex(s => s >= oldSelectedStart);
                    _matchIndex = nearest >= 0 ? nearest : _matchStarts.Count - 1;
                }
            }
            else
            {
                _matchIndex = 0;
            }
        }

        UpdateFindCount();
        JumpToCurrentMatch(scroll: false);
    }

    private void UpdateFindCount()
    {
        if (_findCount == null) return;

        if (_matchStarts.Count == 0 || _matchIndex < 0)
            _findCount.Text = "0/0";
        else
            _findCount.Text = $"{_matchIndex + 1}/{_matchStarts.Count}";
    }

    private void JumpNext()
    {
        if (_matchStarts.Count == 0) return;
        _matchIndex = (_matchIndex + 1) % _matchStarts.Count;
        UpdateFindCount();
        JumpToCurrentMatch(scroll: true);
    }

    private void JumpPrev()
    {
        if (_matchStarts.Count == 0) return;
        _matchIndex = (_matchIndex - 1 + _matchStarts.Count) % _matchStarts.Count;
        UpdateFindCount();
        JumpToCurrentMatch(scroll: true);
    }

    private void JumpToCurrentMatch(bool scroll)
    {
        if (_findTarget == null) return;
        if (_matchIndex < 0 || _matchIndex >= _matchStarts.Count) return;

        int start = _matchStarts[_matchIndex];
        int len = _matchLen;

        // highlight immediately (works when already visible)
        ApplyHighlight(_findTarget, start, len);

        if (!scroll)
            return;

        try
        {
            _findTarget.Focus();

            // scroll via caret only (do NOT touch selection)
            _findTarget.CaretIndex = Math.Clamp(start, 0, (_findTarget.Text ?? "").Length);

            DispatcherTimer.RunOnce(() =>
            {
                try { CenterByCaretRect(_findTarget, start); } catch { /* ignore */ }
                ApplyHighlight(_findTarget, start, len);
            }, TimeSpan.FromMilliseconds(25));

            DispatcherTimer.RunOnce(() =>
            {
                try { CenterByCaretRect(_findTarget, start); } catch { /* ignore */ }
                ApplyHighlight(_findTarget, start, len);
            }, TimeSpan.FromMilliseconds(85));
        }
        catch
        {
            // ignore
        }
    }

    private void ApplyHighlight(TextBox target, int start, int len)
    {
        if (_hlOrig == null || _hlTran == null) return;

        if (ReferenceEquals(target, _orig))
        {
            _hlTran.Clear();
            _hlOrig.SetRange(start, len);
        }
        else
        {
            _hlOrig.Clear();
            _hlTran.SetRange(start, len);
        }
    }

    private void ClearHighlight()
    {
        _hlOrig?.Clear();
        _hlTran?.Clear();
    }

    private void ClearFindState()
    {
        _matchStarts.Clear();
        _matchLen = 0;
        _matchIndex = -1;
        UpdateFindCount();
        ClearHighlight();
    }

    // --------------------------
    // Helpers
    // --------------------------

    private IClipboard? GetClipboard()
    {
        var top = TopLevel.GetTopLevel(this);
        return top?.Clipboard;
    }

    private void UpdateHint(string s)
    {
        if (_txtHint != null) _txtHint.Text = s;
    }

    private static string BuildChatGptPrompt(string selectionXml)
    {
        return
$@"You are an XML-preserving translator.

ABSOLUTE TARGET LANGUAGE:
- Translate into ENGLISH ONLY.
- Do NOT rewrite or paraphrase text that is already in English; keep existing English EXACTLY as-is.

NON-NEGOTIABLE RULES (STRICT SPEC):

1) XML STRUCTURE MUST BE PRESERVED EXACTLY
   - Every start tag, end tag, self-closing tag, attribute name/value, namespace prefix,
     entity reference, CDATA marker, processing instruction, and tag order must remain IDENTICAL.
   - Do NOT add, remove, reorder, rename, or reformat any tags or attributes.
   - Do NOT move text across tag boundaries.
   - Do NOT duplicate the fragment or append a second copy.
   - Output must be well-formed XML.

2) TRANSLATE TEXT NODES ONLY (HUMAN-READABLE NATURAL LANGUAGE)
   - Translate ONLY natural-language text inside text nodes.
   - NEVER translate or modify:
     • tag names
     • attribute names or values
     • IDs, codes, catalog numbers, dates/times, line numbers, refs, witness marks (e.g., 【CB】), or other non-prose tokens
     • any text that is already English (leave it exactly unchanged)
   - You MUST translate ALL non-empty Chinese/Japanese/Korean (CJK) natural-language text that appears in text nodes.
   - You MAY smooth sentence flow across line-break tags (e.g. <lb/>, <pb/>) ONLY by choosing English wording that reads naturally,
     but you may NOT move text across tags. Punctuation is up to you as long as you make the text remain close to the original meaning.

3) WHITESPACE / PUNCTUATION PRESERVATION
   - Keep whitespace/newlines as close as possible to the input (do NOT rewrap or normalize).
   - Preserve the existing punctuation structure as long as you output readable English.
   - Do NOT add explanatory parentheses, glosses, or extra words like ""(i.e.)"".

4) SILENT INTERNAL SELF-CHECK (MANDATORY)
   - Check that the sequence and count of '<...>' XML tokens in your output EXACTLY matches the input.
   - Check that all attributes, namespaces, and processing instructions are unchanged.
   - Check that NO CJK characters remain in text nodes that should have been translated.
   - Check that you did NOT translate any existing English text.
   - If ANY check fails, output the ORIGINAL XML UNCHANGED (verbatim).

OUTPUT REQUIREMENTS:
- Output ONLY the XML fragment (exactly one copy).
- Put the entire output in ONE single ```xml code block``` and NOTHING ELSE.

XML fragment to translate:
```xml
{selectionXml}
```";
    }

    private static string ExtractXmlFromClipboard(string clipboardText)
    {
        var m = Regex.Match(
            clipboardText,
            @"```(?:xml)?\s*(?<xml>[\s\S]*?)\s*```",
            RegexOptions.IgnoreCase);

        if (m.Success)
            return m.Groups["xml"].Value.Trim();

        return clipboardText.Trim();
    }

    // Precompiled tag regex (fast + consistent)
    private static readonly Regex XmlTagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

    private async Task SelectNextTagsAsync(int tagCount)
    {
        if (_tran == null)
        {
            Status?.Invoke(this, "Translated XML editor not available.");
            return;
        }

        var text = _tran.Text ?? "";
        if (text.Length == 0)
        {
            Status?.Invoke(this, "Translated XML is empty.");
            return;
        }

        int start;
        int selStart = _tran.SelectionStart;
        int selEnd = _tran.SelectionEnd;

        if (selEnd > selStart)
        {
            start = selEnd;
            _lastCopyStart = selStart;
            _lastCopyEnd = selEnd;
        }
        else if (_lastCopyEnd > _lastCopyStart)
        {
            start = _lastCopyEnd;
        }
        else
        {
            start = _tran.CaretIndex;
        }

        start = Math.Clamp(start, 0, text.Length);

        var matches = XmlTagRegex.Matches(text, start);
        if (matches.Count == 0)
        {
            await ShowInfoPopupAsync("End reached", "No more XML tags found after the current position.");
            return;
        }

        int take = Math.Min(tagCount, matches.Count);
        var last = matches[take - 1];

        int end = last.Index + last.Length;
        end = Math.Clamp(end, 0, text.Length);

        end = ExtendToNextNewline(text, end);

        if (end <= start)
        {
            await ShowInfoPopupAsync("Selection failed", "Could not compute a valid selection range.");
            return;
        }

        _tran.Focus();
        _tran.SelectionStart = start;
        _tran.SelectionEnd = end;
        try { _tran.CaretIndex = start; } catch { /* ignore */ }

        _lastCopyStart = start;
        _lastCopyEnd = end;

        Status?.Invoke(this, $"Selected {take} tag(s) + newline boundary ({end - start:n0} chars).");
    }

    private static int ExtendToNextNewline(string text, int end)
    {
        if (string.IsNullOrEmpty(text)) return end;
        end = Math.Clamp(end, 0, text.Length);
        if (end >= text.Length) return text.Length;

        const int MaxScan = 4000;
        int scanLimit = Math.Min(text.Length, end + MaxScan);

        for (int i = end; i < scanLimit; i++)
        {
            if (text[i] == '\n')
                return i + 1;
        }

        int j = end;
        while (j < text.Length && (text[j] == ' ' || text[j] == '\t' || text[j] == '\r'))
            j++;

        return j;
    }

    // --------------------------
    // Hacky XML check (no parser) — SINGLE SOURCE OF TRUTH
    // --------------------------

    // Community note removal for structural comparison.
    // We allow these notes to exist ONLY in translated XML.
    private static readonly Regex CommunityNoteBlockRegex = new Regex(
        @"<note\b(?<attrs>[^>]*)\btype\s*=\s*""community""(?<attrs2>[^>]*)>(?<inner>[\s\S]*?)</note>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string StripCommunityNotes(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return xml ?? string.Empty;

        // Replace the whole block with nothing.
        // We don't try to preserve whitespace perfectly; this is only for structural validation.
        return CommunityNoteBlockRegex.Replace(xml, "");
    }


    private static readonly Regex LbTagRegex = new Regex(
        @"<lb\b(?<attrs>[^>]*)\/?>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AttrRegex = new Regex(
        @"\b(?<name>n|ed)\s*=\s*""(?<val>[^""]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (int totalLb, Dictionary<string, int> sigCounts) CollectLbSignatures(string xml)
    {
        int total = 0;
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match m in LbTagRegex.Matches(xml))
        {
            total++;

            string attrs = m.Groups["attrs"].Value;

            string? nVal = null;
            string? edVal = null;

            foreach (Match am in AttrRegex.Matches(attrs))
            {
                var name = am.Groups["name"].Value;
                var val = am.Groups["val"].Value;

                if (name.Equals("n", StringComparison.OrdinalIgnoreCase)) nVal = val;
                else if (name.Equals("ed", StringComparison.OrdinalIgnoreCase)) edVal = val;
            }

            string sig = $"n={nVal ?? "<missing>"}|ed={edVal ?? "<missing>"}";

            if (dict.TryGetValue(sig, out int c)) dict[sig] = c + 1;
            else dict[sig] = 1;
        }

        return (total, dict);
    }

    private static (bool ok, string message, int origTags, int tranTags, int origLb, int tranLb) VerifyXmlHacky(string orig, string tran)
    {
        if (string.IsNullOrEmpty(orig))
            return (false, "Original XML is empty. Nothing to compare.", 0, 0, 0, 0);

        tran ??= "";

        // IMPORTANT:
        // For structure comparison, we ignore community notes in translated XML.
        // Everything else must remain identical.
        string tranStripped = StripCommunityNotes(tran);

        int origTagCount = XmlTagRegex.Matches(orig).Count;
        int tranTagCount = XmlTagRegex.Matches(tran).Count;                 // informational (real)
        int tranTagCountStripped = XmlTagRegex.Matches(tranStripped).Count; // compared

        var (origLbTotal, origSigs) = CollectLbSignatures(orig);
        var (tranLbTotal, tranSigs) = CollectLbSignatures(tranStripped);

        var missing = origSigs.Keys.Where(k => !tranSigs.ContainsKey(k)).ToList();
        var extra = tranSigs.Keys.Where(k => !origSigs.ContainsKey(k)).ToList();

        var countDiffs = new List<string>();
        foreach (var k in origSigs.Keys.Intersect(tranSigs.Keys))
        {
            int a = origSigs[k];
            int b = tranSigs[k];
            if (a != b)
                countDiffs.Add($"{k}  original={a}  translated={b}");
        }

        var problems = new List<string>();

        // Compare tag counts AFTER stripping community notes.
        if (origTagCount != tranTagCountStripped)
            problems.Add(
                $"TAG COUNT MISMATCH (ignoring community notes):\n" +
                $"  original={origTagCount:n0}\n" +
                $"  translated_stripped={tranTagCountStripped:n0}\n" +
                $"  translated_raw={tranTagCount:n0}");

        if (origLbTotal != tranLbTotal)
            problems.Add($"LB TOTAL MISMATCH:\n  original={origLbTotal:n0}\n  translated={tranLbTotal:n0}");

        if (missing.Count > 0)
            problems.Add($"MISSING <lb> SIGNATURES in translated: {missing.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", missing.Take(15))}");

        if (extra.Count > 0)
            problems.Add($"EXTRA <lb> SIGNATURES in translated: {extra.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", extra.Take(15))}");

        if (countDiffs.Count > 0)
            problems.Add($"<lb> SIGNATURE COUNT DIFFERENCES: {countDiffs.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", countDiffs.Take(15))}");

        if (problems.Count == 0)
        {
            int removed = tranTagCount - tranTagCountStripped;

            string okMsg =
                $"OK ✅\n\n" +
                $"Tag count matches (ignoring community notes): {tranTagCountStripped:n0}\n" +
                $"<lb> count matches: {tranLbTotal:n0}\n" +
                $"All <lb n=... ed=...> signatures match.\n" +
                (removed > 0 ? $"\nCommunity-note tags ignored during check: {removed:n0}\n" : "\n") +
                $"(Hacky structural check only; not a full XML validator.)";

            return (true, okMsg, origTagCount, tranTagCount, origLbTotal, tranLbTotal);
        }

        return (false, string.Join("\n\n", problems), origTagCount, tranTagCount, origLbTotal, tranLbTotal);
    }


    private async Task<bool> EnsureXmlOkOrWarnAsync(bool showOkPopup)
    {
        if (_orig == null || _tran == null)
        {
            Status?.Invoke(this, "Editors not available.");
            if (showOkPopup)
                await ShowInfoPopupAsync("Check XML", "Editors not available.");
            return false;
        }

        var orig = _orig.Text ?? "";
        var tran = _tran.Text ?? "";

        var (ok, msg, _, tranTags, _, tranLb) = VerifyXmlHacky(orig, tran);

        if (ok)
        {
            Status?.Invoke(this, $"XML check OK: tags={tranTags:n0}, lb={tranLb:n0} (n/ed preserved).");
            if (showOkPopup)
                await ShowInfoPopupAsync("Check XML", msg);
            return true;
        }

        Status?.Invoke(this, "XML check failed (see popup).");
        await ShowInfoPopupAsync("Check XML (hacky)", msg);
        return false;
    }

    private Task CheckXmlWithPopupAsync()
        => EnsureXmlOkOrWarnAsync(showOkPopup: true);

    private async Task SaveIfValidAsync()
    {
        if (!await EnsureXmlOkOrWarnAsync(showOkPopup: false))
            return;

        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    // --------------------------
    // Popup (awaited)
    // --------------------------

    private async Task ShowInfoPopupAsync(string title, string message)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;

        var ok = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80
        };

        var text = new TextBox
        {
            Text = message,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 240
        };

        ScrollViewer.SetVerticalScrollBarVisibility(text, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(text, ScrollBarVisibility.Disabled);

        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };

        panel.Children.Add(text);
        panel.Children.Add(ok);

        var win = new Window
        {
            Title = title,
            Width = 700,
            Height = 380,
            Content = panel,
            WindowStartupLocation = owner != null
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen
        };

        if (owner != null)
        {
            ok.Click += (_, _) => win.Close();
            await win.ShowDialog(owner);
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        ok.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        win.Show();
        await tcs.Task;
    }

    private void ResetNavigationState()
    {
        if (_tran == null) return;

        _lastCopyStart = -1;
        _lastCopyEnd = -1;

        try
        {
            _tran.SelectionStart = 0;
            _tran.SelectionEnd = 0;
            _tran.CaretIndex = 0;
        }
        catch { /* ignore */ }

        try { _tran.Focus(); } catch { /* ignore */ }
    }

    // --------------------------
    // Scroll helper (vertical centering)
    // --------------------------

    private static ScrollViewer? FindScrollViewer(Control? c)
    {
        if (c == null) return null;
        return c.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
    }

    private static bool IsFinitePositive(double v)
        => !double.IsNaN(v) && !double.IsInfinity(v) && v > 0;

    private static double ClampY(double extentH, double viewportH, double y)
    {
        if (IsFinitePositive(extentH) && IsFinitePositive(viewportH))
        {
            double maxY = Math.Max(0, extentH - viewportH);
            return Math.Max(0, Math.Min(y, maxY));
        }
        return Math.Max(0, y);
    }

    private static void CenterByCaretRect(TextBox tb, int caretIndex)
    {
        var sv = FindScrollViewer(tb);
        if (sv == null) return;

        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;

        if (!IsFinitePositive(viewportH))
            return;

        if (!TryGetCaretYInScrollViewer(tb, sv, caretIndex, out double caretY))
            return;

        double targetY = viewportH / 2.0;
        double delta = caretY - targetY;

        if (Math.Abs(delta) <= Math.Max(6.0, viewportH * 0.03))
            return;

        double desiredY = sv.Offset.Y + delta;
        desiredY = ClampY(extentH, viewportH, desiredY);

        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }

    private static bool TryGetCaretYInScrollViewer(TextBox tb, ScrollViewer sv, int charIndex, out double caretY)
    {
        caretY = 0;

        int len = tb.Text?.Length ?? 0;
        if (len <= 0) return false;
        charIndex = Math.Clamp(charIndex, 0, len);

        try
        {
            var presenter = tb.GetVisualDescendants().FirstOrDefault(v => v.GetType().Name == "TextPresenter");
            if (presenter != null)
            {
                var mPr = presenter.GetType().GetMethod("GetRectFromCharacterIndex", new[] { typeof(int) });
                if (mPr != null)
                {
                    var val = mPr.Invoke(presenter, new object[] { charIndex });
                    if (val is Rect r2)
                    {
                        var p = presenter.TranslatePoint(new Point(r2.X, r2.Y), sv);
                        if (p != null)
                        {
                            caretY = p.Value.Y;
                            return true;
                        }
                    }
                }
            }
        }
        catch { /* ignore */ }

        var mTb = tb.GetType().GetMethod("GetRectFromCharacterIndex", new[] { typeof(int) });
        if (mTb != null)
        {
            try
            {
                var val = mTb.Invoke(tb, new object[] { charIndex });
                if (val is Rect r)
                {
                    var p = tb.TranslatePoint(new Point(r.X, r.Y), sv);
                    if (p != null)
                    {
                        caretY = p.Value.Y;
                        return true;
                    }
                }
            }
            catch { /* ignore */ }
        }

        return false;
    }
}
