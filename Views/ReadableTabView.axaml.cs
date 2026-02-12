// Views/ReadableTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class ReadableTabView : UserControl
{
    private TextBox? _editorOriginal;
    private TextBox? _editorTranslated;

    private HoverDictionaryBehavior? _hoverDict;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

    private ScrollViewer? _svOriginal;
    private ScrollViewer? _svTranslated;

    // Cached template parts for better scroll/geometry (like HoverDictionaryBehavior)
    private Visual? _presOriginal;
    private Visual? _presTranslated;
    private ScrollContentPresenter? _scpOriginal;
    private ScrollContentPresenter? _scpTranslated;

    private readonly ISelectionSyncService _selectionSync = new SelectionSyncService();

    private RenderedDocument _renderOrig = RenderedDocument.Empty;
    private RenderedDocument _renderTran = RenderedDocument.Empty;

    private bool _syncingSelection;

    private DateTime _ignoreProgrammaticUntilUtc = DateTime.MinValue;
    private const int IgnoreProgrammaticWindowMs = 180;

    private DateTime _suppressPollingUntilUtc = DateTime.MinValue;
    private const int SuppressPollingAfterUserActionMs = 220;

    private DispatcherTimer? _selTimer;
    private int _lastOrigSelStart = -1, _lastOrigSelEnd = -1;
    private int _lastTranSelStart = -1, _lastTranSelEnd = -1;
    private int _lastOrigCaret = -1, _lastTranCaret = -1;

    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private TextBox? _lastUserInputEditor;
    private const int UserInputPriorityWindowMs = 250;

    // Coalesced mirroring: last request wins
    private bool _mirrorQueued;
    private bool _mirrorSourceIsTranslated;

    // -------------------------
    // Find (Ctrl+F) state
    // -------------------------
    private Border? _findBar;
    private TextBox? _findQuery;
    private TextBlock? _findCount;
    private TextBlock? _findScope;
    private Button? _btnPrev;
    private Button? _btnNext;
    private Button? _btnCloseFind;

    private SearchHighlightOverlay? _hlOriginal;
    private SearchHighlightOverlay? _hlTranslated;

    private TextBox? _findTarget;
    private List<int> _matchStarts = new();
    private int _matchLen = 0;
    private int _matchIndex = -1;

    private static readonly TimeSpan FindRecomputeDebounce = TimeSpan.FromMilliseconds(140);
    private DispatcherTimer? _findDebounceTimer;

    private bool _findIsApplyingSelection;
    private DateTime _suppressMirrorUntilUtc = DateTime.MinValue;
    private const int SuppressMirrorAfterFindMs = 900;

    public ReadableTabView()
    {
        InitializeComponent();
        FindControls();
        WireEvents();

        AttachedToVisualTree += (_, _) =>
        {
            _svOriginal = FindScrollViewer(_editorOriginal);
            _svTranslated = FindScrollViewer(_editorTranslated);

            RefreshPresenterCache(_editorOriginal, isOriginal: true);
            RefreshPresenterCache(_editorTranslated, isOriginal: false);

            SetupHoverDictionary();
            StartSelectionTimer(); // slow safety-net polling
        };

        DetachedFromVisualTree += (_, _) =>
        {
            StopSelectionTimer();
            DisposeHoverDictionary();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _editorOriginal = this.FindControl<TextBox>("EditorOriginal");
        _editorTranslated = this.FindControl<TextBox>("EditorTranslated");

        if (_editorOriginal != null) _editorOriginal.IsReadOnly = true;
        if (_editorTranslated != null) _editorTranslated.IsReadOnly = true;

        // Cache template parts like your HoverDictionaryBehavior
        if (_editorOriginal != null) _editorOriginal.TemplateApplied += (_, _) => RefreshPresenterCache(_editorOriginal, isOriginal: true);
        if (_editorTranslated != null) _editorTranslated.TemplateApplied += (_, _) => RefreshPresenterCache(_editorTranslated, isOriginal: false);

        _findBar = this.FindControl<Border>("FindBar");
        _findQuery = this.FindControl<TextBox>("FindQuery");
        _findCount = this.FindControl<TextBlock>("FindCount");
        _findScope = this.FindControl<TextBlock>("FindScope");
        _btnPrev = this.FindControl<Button>("BtnPrev");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnCloseFind = this.FindControl<Button>("BtnCloseFind");

        _hlOriginal = this.FindControl<SearchHighlightOverlay>("HlOriginal");
        _hlTranslated = this.FindControl<SearchHighlightOverlay>("HlTranslated");

        if (_hlOriginal != null) _hlOriginal.Target = _editorOriginal;
        if (_hlTranslated != null) _hlTranslated.Target = _editorTranslated;
    }

    private void WireEvents()
    {
        HookUserInputTracking(_editorOriginal);
        HookUserInputTracking(_editorTranslated);

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
    }

    private void RefreshPresenterCache(TextBox? tb, bool isOriginal)
    {
        if (tb == null) return;

        var sv = FindScrollViewer(tb);
        var scp = sv != null ? FindScrollContentPresenter(sv) : null;

        var presenter = tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => string.Equals(v.GetType().Name, "TextPresenter", StringComparison.Ordinal));

        presenter ??= tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => (v.GetType().Name?.Contains("Text", StringComparison.OrdinalIgnoreCase) ?? false));

        if (isOriginal)
        {
            _svOriginal = sv ?? _svOriginal;
            _scpOriginal = scp;
            _presOriginal = presenter;
        }
        else
        {
            _svTranslated = sv ?? _svTranslated;
            _scpTranslated = scp;
            _presTranslated = presenter;
        }
    }

    private void SetupHoverDictionary()
    {
        if (_editorOriginal == null) return;

        _hoverDict?.Dispose();
        _hoverDict = new HoverDictionaryBehavior(_editorOriginal, _cedict);
    }

    private void DisposeHoverDictionary()
    {
        _hoverDict?.Dispose();
        _hoverDict = null;
    }

    public void Clear()
    {
        _renderOrig = RenderedDocument.Empty;
        _renderTran = RenderedDocument.Empty;

        if (_editorOriginal != null) _editorOriginal.Text = "";
        if (_editorTranslated != null) _editorTranslated.Text = "";

        _lastOrigSelStart = _lastOrigSelEnd = -1;
        _lastTranSelStart = _lastTranSelEnd = -1;
        _lastOrigCaret = _lastTranCaret = -1;

        ResetScroll(_svOriginal);
        ResetScroll(_svTranslated);

        ClearFindState();
        CloseFind();
    }

    public void SetRendered(RenderedDocument orig, RenderedDocument tran)
    {
        _renderOrig = orig ?? RenderedDocument.Empty;
        _renderTran = tran ?? RenderedDocument.Empty;

        if (_editorOriginal != null) _editorOriginal.Text = _renderOrig.Text;
        if (_editorTranslated != null) _editorTranslated.Text = _renderTran.Text;

        // after text set, layout/template settles later — refresh caches now + later
        RefreshPresenterCache(_editorOriginal, isOriginal: true);
        RefreshPresenterCache(_editorTranslated, isOriginal: false);

        Dispatcher.UIThread.Post(() =>
        {
            RefreshPresenterCache(_editorOriginal, isOriginal: true);
            RefreshPresenterCache(_editorTranslated, isOriginal: false);
        }, DispatcherPriority.Loaded);

        DispatcherTimer.RunOnce(() =>
        {
            RefreshPresenterCache(_editorOriginal, isOriginal: true);
            RefreshPresenterCache(_editorTranslated, isOriginal: false);
        }, TimeSpan.FromMilliseconds(90));

        ResetScroll(_svOriginal);
        ResetScroll(_svTranslated);

        if (_editorOriginal != null)
        {
            _lastOrigSelStart = _editorOriginal.SelectionStart;
            _lastOrigSelEnd = _editorOriginal.SelectionEnd;
            _lastOrigCaret = _editorOriginal.CaretIndex;
        }

        if (_editorTranslated != null)
        {
            _lastTranSelStart = _editorTranslated.SelectionStart;
            _lastTranSelEnd = _editorTranslated.SelectionEnd;
            _lastTranCaret = _editorTranslated.CaretIndex;
        }

        if (_findBar?.IsVisible == true)
            RecomputeMatches(resetToFirst: false);
    }

    private void HookUserInputTracking(TextBox? tb)
    {
        if (tb == null) return;

        tb.PointerPressed += OnEditorUserInput;
        tb.PointerReleased += OnEditorPointerReleased;
        tb.KeyDown += OnEditorUserInput;
        tb.KeyUp += OnEditorKeyUp;

        tb.GotFocus += (_, _) =>
        {
            if (_findBar?.IsVisible == true && !_findIsApplyingSelection)
                SetFindTarget(tb);
        };
    }

    private void OnEditorUserInput(object? sender, EventArgs e)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = sender as TextBox;
    }

    private void OnEditorPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = sender as TextBox;

        bool sourceIsTranslated = ReferenceEquals(_lastUserInputEditor, _editorTranslated);

        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressPollingAfterUserActionMs);
        RequestMirrorFromUserAction(sourceIsTranslated);
    }

    private void OnEditorKeyUp(object? sender, KeyEventArgs e)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = sender as TextBox;

        bool sourceIsTranslated = ReferenceEquals(_lastUserInputEditor, _editorTranslated);

        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressPollingAfterUserActionMs);
        RequestMirrorFromUserAction(sourceIsTranslated);
    }

    // -------------------------
    // Polling + mirroring
    // -------------------------

    private void StartSelectionTimer()
    {
        if (_selTimer != null) return;

        _selTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
        _selTimer.Tick += (_, _) => PollSelectionChanges();
        _selTimer.Start();
    }

    private void StopSelectionTimer()
    {
        if (_selTimer == null) return;
        _selTimer.Stop();
        _selTimer = null;
    }

    private void PollSelectionChanges()
    {
        if (DateTime.UtcNow <= _suppressPollingUntilUtc) return;
        if (_syncingSelection) return;
        if (DateTime.UtcNow <= _ignoreProgrammaticUntilUtc) return;

        if (_findIsApplyingSelection) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        if (_editorOriginal == null || _editorTranslated == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

        bool anyFocused =
            _editorOriginal.IsFocused || _editorOriginal.IsKeyboardFocusWithin ||
            _editorTranslated.IsFocused || _editorTranslated.IsKeyboardFocusWithin;

        if (!anyFocused) return;

        int oS = _editorOriginal.SelectionStart;
        int oE = _editorOriginal.SelectionEnd;
        int tS = _editorTranslated.SelectionStart;
        int tE = _editorTranslated.SelectionEnd;
        int oC = _editorOriginal.CaretIndex;
        int tC = _editorTranslated.CaretIndex;

        bool origSelChanged = (oS != _lastOrigSelStart) || (oE != _lastOrigSelEnd);
        bool tranSelChanged = (tS != _lastTranSelStart) || (tE != _lastTranSelEnd);
        bool origCaretChanged = (oC != _lastOrigCaret);
        bool tranCaretChanged = (tC != _lastTranCaret);

        if (!origSelChanged && !tranSelChanged && !origCaretChanged && !tranCaretChanged)
            return;

        _lastOrigSelStart = oS;
        _lastOrigSelEnd = oE;
        _lastTranSelStart = tS;
        _lastTranSelEnd = tE;
        _lastOrigCaret = oC;
        _lastTranCaret = tC;

        bool sourceIsTranslated = DetermineSourcePane(origSelChanged || origCaretChanged, tranSelChanged || tranCaretChanged);
        RequestMirrorFromUserAction(sourceIsTranslated);
    }

    private bool DetermineSourcePane(bool origChanged, bool tranChanged)
    {
        if (_editorOriginal == null || _editorTranslated == null)
            return true;

        bool origFocused = _editorOriginal.IsFocused || _editorOriginal.IsKeyboardFocusWithin;
        bool tranFocused = _editorTranslated.IsFocused || _editorTranslated.IsKeyboardFocusWithin;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;

        if (origFocused && !tranFocused) return false;
        if (tranFocused && !origFocused) return true;

        if (origChanged && !tranChanged) return false;
        if (tranChanged && !origChanged) return true;

        if (recentInput && _lastUserInputEditor != null)
            return ReferenceEquals(_lastUserInputEditor, _editorTranslated);

        if (tranFocused) return true;
        if (origFocused) return false;

        return true;
    }

    private void MirrorSelectionOneWay(bool sourceIsTranslated)
    {
        if (_editorOriginal == null || _editorTranslated == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

        var srcEditor = sourceIsTranslated ? _editorTranslated : _editorOriginal;
        var dstEditor = sourceIsTranslated ? _editorOriginal : _editorTranslated;

        var srcDoc = sourceIsTranslated ? _renderTran : _renderOrig;
        var dstDoc = sourceIsTranslated ? _renderOrig : _renderTran;

        int caret = srcEditor.CaretIndex;

        if (!_selectionSync.TryGetDestinationSegment(srcDoc, dstDoc, caret, out var dstSeg))
            return;

        try
        {
            _syncingSelection = true;

            ApplyDestinationSelection(dstEditor, dstSeg.Start, dstSeg.EndExclusive, center: true);

            if (ReferenceEquals(dstEditor, _editorOriginal))
            {
                _lastOrigSelStart = dstEditor.SelectionStart;
                _lastOrigSelEnd = dstEditor.SelectionEnd;
                _lastOrigCaret = dstEditor.CaretIndex;
            }
            else
            {
                _lastTranSelStart = dstEditor.SelectionStart;
                _lastTranSelEnd = dstEditor.SelectionEnd;
                _lastTranCaret = dstEditor.CaretIndex;
            }

            _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(IgnoreProgrammaticWindowMs);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void ApplyDestinationSelection(TextBox dst, int start, int endExclusive, bool center)
    {
        int len = dst.Text?.Length ?? 0;
        start = Math.Max(0, Math.Min(start, len));
        endExclusive = Math.Max(0, Math.Min(endExclusive, len));
        if (endExclusive < start) (start, endExclusive) = (endExclusive, start);

        dst.SelectionStart = start;
        dst.SelectionEnd = endExclusive;

        try { dst.CaretIndex = start; } catch { }

        if (!center) return;

        int anchor = start + Math.Max(0, (endExclusive - start) / 2);
        CenterByTextLayoutReliable(dst, anchor);
    }

    private void RequestMirrorFromUserAction(bool sourceIsTranslated)
    {
        if (_findIsApplyingSelection) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        _mirrorSourceIsTranslated = sourceIsTranslated;
        if (_mirrorQueued) return;
        _mirrorQueued = true;

        Dispatcher.UIThread.Post(() =>
        {
            _mirrorQueued = false;

            if (_syncingSelection) return;
            if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

            MirrorSelectionOneWay(_mirrorSourceIsTranslated);
        }, DispatcherPriority.Background);
    }

    // -------------------------
    // Ctrl+F Find UI
    // -------------------------

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
        SetFindTarget(target);

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
        _findTarget?.Focus();
    }

    private TextBox? DetermineCurrentPaneForFind()
    {
        if (_editorOriginal == null || _editorTranslated == null)
            return _editorTranslated;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;
        if (recentInput && _lastUserInputEditor != null)
            return _lastUserInputEditor;

        if (_editorOriginal.IsFocused || _editorOriginal.IsKeyboardFocusWithin) return _editorOriginal;
        if (_editorTranslated.IsFocused || _editorTranslated.IsKeyboardFocusWithin) return _editorTranslated;

        return _editorTranslated;
    }

    private void SetFindTarget(TextBox? tb)
    {
        if (tb == null) return;
        _findTarget = tb;

        if (_findScope != null)
            _findScope.Text = ReferenceEquals(tb, _editorOriginal) ? "Find (Original):" : "Find (Translated):";

        RecomputeMatches(resetToFirst: false);
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
                if (exact >= 0)
                {
                    _matchIndex = exact;
                }
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

        ApplyHighlight(_findTarget, start, len);

        if (!scroll)
            return;

        try
        {
            _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(420);
            _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(420);
            _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressMirrorAfterFindMs);

            _findTarget.Focus();
            _findTarget.CaretIndex = Math.Clamp(start, 0, (_findTarget.Text ?? "").Length);

            CenterByTextLayoutReliable(_findTarget, start);

            ApplyHighlight(_findTarget, start, len);
        }
        catch { }
    }

    private void ApplyHighlight(TextBox target, int start, int len)
    {
        if (_hlOriginal == null || _hlTranslated == null) return;

        if (ReferenceEquals(target, _editorOriginal))
        {
            _hlTranslated.Clear();
            _hlOriginal.SetRange(start, len);
        }
        else
        {
            _hlOriginal.Clear();
            _hlTranslated.SetRange(start, len);
        }
    }

    private void ClearHighlight()
    {
        _hlOriginal?.Clear();
        _hlTranslated?.Clear();
    }

    private void ClearFindState()
    {
        _matchStarts.Clear();
        _matchLen = 0;
        _matchIndex = -1;
        UpdateFindCount();
        ClearHighlight();
    }

    // -------------------------
    // Scroll helpers (Avalonia 11.3.11)
    // -------------------------

    private static void ResetScroll(ScrollViewer? sv)
    {
        if (sv == null) return;
        sv.Offset = new Vector(0, 0);
    }

    private static ScrollViewer? FindScrollViewer(Control? c)
    {
        if (c == null) return null;
        return c.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
    }

    private static ScrollContentPresenter? FindScrollContentPresenter(ScrollViewer? sv)
    {
        if (sv == null) return null;
        return sv.GetVisualDescendants().OfType<ScrollContentPresenter>().FirstOrDefault();
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

    // Key fix: use TextLayout geometry (works with wrapping; no drift late in file)
    private void CenterByTextLayoutReliable(TextBox tb, int charIndex)
    {
        ScrollViewer? sv;
        ScrollContentPresenter? scp;
        Visual? presenter;

        if (ReferenceEquals(tb, _editorOriginal))
        {
            sv = _svOriginal ?? FindScrollViewer(tb);
            scp = _scpOriginal ?? FindScrollContentPresenter(sv);
            presenter = _presOriginal;
        }
        else
        {
            sv = _svTranslated ?? FindScrollViewer(tb);
            scp = _scpTranslated ?? FindScrollContentPresenter(sv);
            presenter = _presTranslated;
        }

        if (sv == null) return;

        int len = tb.Text?.Length ?? 0;
        if (len <= 0) return;
        charIndex = Math.Clamp(charIndex, 0, len);

        // layout settles over multiple render ticks in huge docs
        Dispatcher.UIThread.Post(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), DispatcherPriority.Render);
        DispatcherTimer.RunOnce(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), TimeSpan.FromMilliseconds(28));
        DispatcherTimer.RunOnce(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), TimeSpan.FromMilliseconds(60));
        DispatcherTimer.RunOnce(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), TimeSpan.FromMilliseconds(110));
    }

    private static void TryCenterOnce_TextLayout(TextBox tb, ScrollViewer sv, ScrollContentPresenter? scp, Visual? presenter, int charIndex)
    {
        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;
        if (!IsFinitePositive(viewportH) || !IsFinitePositive(extentH))
            return;

        // target visual for TranslatePoint: prefer ScrollContentPresenter if present, else ScrollViewer
        Visual target = (Visual?)scp ?? (Visual)sv;

        presenter ??= tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => string.Equals(v.GetType().Name, "TextPresenter", StringComparison.Ordinal));

        if (presenter == null) return;

        // get TextLayout from presenter (same pattern as HoverDictionaryBehavior)
        TextLayout? tl = null;
        try
        {
            var prop = presenter.GetType().GetProperty("TextLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.GetValue(presenter) is TextLayout got)
                tl = got;
        }
        catch { }

        if (tl == null) return;

        Rect r;
        try { r = tl.HitTestTextPosition(charIndex); }
        catch { return; }

        var p = presenter.TranslatePoint(new Point(r.X, r.Y), target);
        if (p == null) return;

        double yInViewport = p.Value.Y;

        // slightly above center feels better for reading and avoids "just barely at bottom"
        double targetY = viewportH * 0.40;
        double topBand = viewportH * 0.15;
        double bottomBand = viewportH * 0.85;

        if (yInViewport >= topBand && yInViewport <= bottomBand)
            return;

        double desiredY = sv.Offset.Y + (yInViewport - targetY);

        desiredY = ClampY(extentH, viewportH, desiredY);
        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }

    // Wrapper for old call sites
    private void CenterByCharacterRectReliable(TextBox tb, int charIndex)
        => CenterByTextLayoutReliable(tb, charIndex);

    private static bool TryInvokeRectFromIndex(object target, int charIndex, out Rect rect)
    {
        rect = default;

        // (kept for compatibility / fallback use if you ever want it)
        string[] names =
        {
            "GetRectFromCharacterIndex",
            "GetRectangleFromCharacterIndex",
            "GetCursorRectangle",
            "GetCaretRectangle",
            "GetCaretRect",
        };

        var t = target.GetType();

        foreach (var name in names)
        {
            var mi = t.GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null);

            if (mi == null) continue;

            try
            {
                var val = mi.Invoke(target, new object[] { charIndex });
                if (val is Rect r)
                {
                    rect = r;
                    return true;
                }
            }
            catch { }
        }

        return false;
    }
}
