using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Infrastructure;

public sealed class HoverDictionaryBehaviorEdit : IDisposable
{
    private readonly TextEditor _ed;
    private readonly ICedictDictionary _cedict;

    private readonly DispatcherTimer _debounce;

    private bool _isDisposed;

    private bool _hasLastPoint;
    private Point _lastPointInTextView;

    private int _lastOffset = -1;
    private string? _lastKeyShown;

    private bool _loadKickoff;
    private CancellationTokenSource? _loadCts;

    // TextView scroll hook (important: keep delegate instance)
    private EventHandler? _scrollOffsetChangedHandler;

    // Tooltip instance (we control chrome + can override via selector)
    private readonly ToolTip _tip;

    // Root-level guard to prevent sticky tooltip when leaving the editor (e.g., navbar)
    private IInputElement? _root;
    private bool _rootHooked;

    // knobs (snappy)
    private const int DebounceMs = 70;
    private const int MaxLenDefault = 19;
    private const int MaxEntriesShown = 10;
    private const int MaxSensesPerEntry = 3;

    public HoverDictionaryBehaviorEdit(TextEditor editor, ICedictDictionary cedict)
    {
        _ed = editor ?? throw new ArgumentNullException(nameof(editor));
        _cedict = cedict ?? throw new ArgumentNullException(nameof(cedict));

        _tip = new ToolTip
        {
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsHitTestVisible = false,
            Content = null
        };

        // Override global ToolTip styling via App.axaml selector (same as TextBox version)
        _tip.Classes.Add("dictTip");

        ToolTip.SetShowDelay(_ed, 0);
        ToolTip.SetTip(_ed, _tip);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DebounceMs) };
        _debounce.Tick += Debounce_Tick;

        _ed.AttachedToVisualTree += OnAttached;
        _ed.DetachedFromVisualTree += OnDetached;

        _ed.PointerMoved += OnPointerMoved;
        _ed.PointerExited += OnPointerExited;
        _ed.PointerPressed += OnPointerPressed;
        _ed.PointerWheelChanged += OnPointerWheelChanged;
        _ed.PointerCaptureLost += OnPointerCaptureLost;
        _ed.LostFocus += OnLostFocus;

        // kick load in background without blocking UI
        Dispatcher.UIThread.Post(() => KickoffLoadIfNeeded(), DispatcherPriority.Background);

        // hook scroll once TextView exists
        Dispatcher.UIThread.Post(() => HookTextViewScrollIfNeeded(), DispatcherPriority.Loaded);
        DispatcherTimer.RunOnce(() => HookTextViewScrollIfNeeded(), TimeSpan.FromMilliseconds(80));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _debounce.Stop();
            _debounce.Tick -= Debounce_Tick;
        }
        catch { }

        try
        {
            _ed.AttachedToVisualTree -= OnAttached;
            _ed.DetachedFromVisualTree -= OnDetached;

            _ed.PointerMoved -= OnPointerMoved;
            _ed.PointerExited -= OnPointerExited;
            _ed.PointerPressed -= OnPointerPressed;
            _ed.PointerWheelChanged -= OnPointerWheelChanged;
            _ed.PointerCaptureLost -= OnPointerCaptureLost;
            _ed.LostFocus -= OnLostFocus;
        }
        catch { }

        UnhookTextViewScroll();
        UnhookRootGuards();

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        ResetHoverState();
        HideTooltip();

        try { ToolTip.SetTip(_ed, null); } catch { }
    }

    private TextView? Tv => _ed.TextArea?.TextView;

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        HookRootGuards();
        HookTextViewScrollIfNeeded();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        UnhookTextViewScroll();
        UnhookRootGuards();
        ResetHoverState();
        HideTooltip();
    }

    // ==================== ROOT GUARDS (ANTI-STICKY) ====================

    private void HookRootGuards()
    {
        if (_isDisposed) return;

        var top = TopLevel.GetTopLevel(_ed);

        IInputElement? root = top;
        if (root == null)
        {
            try { root = _ed.GetVisualRoot() as IInputElement; } catch { root = null; }
        }

        if (ReferenceEquals(root, _root) && _rootHooked) return;

        UnhookRootGuards();
        _root = root;
        if (_root == null) return;

        _root.AddHandler(InputElement.PointerMovedEvent, RootPointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        _root.AddHandler(InputElement.PointerPressedEvent, RootPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        _rootHooked = true;
    }

    private void UnhookRootGuards()
    {
        if (_root != null && _rootHooked)
        {
            try
            {
                _root.RemoveHandler(InputElement.PointerMovedEvent, RootPointerMoved);
                _root.RemoveHandler(InputElement.PointerPressedEvent, RootPointerPressed);
            }
            catch
            {
                // root can be torn down; safe to ignore
            }
        }

        _rootHooked = false;
        _root = null;
    }

    private void RootPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;
        if (!ToolTip.GetIsOpen(_ed)) return;

        if (!IsPointerOverEditor(e))
        {
            ResetHoverState();
            HideTooltip();
        }
    }

    private void RootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDisposed) return;
        if (!ToolTip.GetIsOpen(_ed)) return;

        if (!IsPointerOverEditor(e))
        {
            ResetHoverState();
            HideTooltip();
        }
    }

    private bool IsPointerOverEditor(PointerEventArgs e)
    {
        var top = TopLevel.GetTopLevel(_ed);
        if (top == null) return false;

        Point pTop;
        try { pTop = e.GetPosition(top); }
        catch { return false; }

        var pEd = top.TranslatePoint(pTop, _ed);
        if (!pEd.HasValue) return false;

        return IsInsideBounds(_ed.Bounds, pEd.Value);
    }

    // ==================== TEXTVIEW SCROLL HOOK ====================

    private void HookTextViewScrollIfNeeded()
    {
        if (_isDisposed) return;

        var tv = Tv;
        if (tv == null) return;

        if (_scrollOffsetChangedHandler != null) return;

        _scrollOffsetChangedHandler = (object? s, EventArgs e) =>
        {
            if (_isDisposed) return;
            if (!_hasLastPoint) return;

            // Wait until render so visual lines reflect the new scroll offset
            Dispatcher.UIThread.Post(() =>
            {
                if (_isDisposed) return;
                if (!_hasLastPoint) return;
                UpdateTooltipFromTextViewPoint(_lastPointInTextView);
            }, DispatcherPriority.Render);
        };

        tv.ScrollOffsetChanged += _scrollOffsetChangedHandler;
    }

    private void UnhookTextViewScroll()
    {
        var tv = Tv;
        if (tv == null) { _scrollOffsetChangedHandler = null; return; }

        if (_scrollOffsetChangedHandler != null)
        {
            try { tv.ScrollOffsetChanged -= _scrollOffsetChangedHandler; } catch { }
            _scrollOffsetChangedHandler = null;
        }
    }

    // ==================== POINTER EVENTS ====================

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;

        var tv = Tv;
        if (tv == null)
        {
            ResetHoverState();
            HideTooltip();
            return;
        }

        var p = e.GetPosition(tv);
        _lastPointInTextView = p;
        _hasLastPoint = true;

        if (!IsInsideBounds(tv.Bounds, p))
        {
            ResetHoverState();
            HideTooltip();
            return;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;
        ResetHoverState();
        HideTooltip();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDisposed) return;
        ResetHoverState();
        HideTooltip();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_isDisposed) return;

        // Update last point (best effort) then re-eval after render
        var tv = Tv;
        if (tv != null)
        {
            _hasLastPoint = true;
            try { _lastPointInTextView = e.GetPosition(tv); } catch { _lastPointInTextView = default; }
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_isDisposed) return;
            if (!_hasLastPoint) return;
            UpdateTooltipFromTextViewPoint(_lastPointInTextView);
        }, DispatcherPriority.Render);
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isDisposed) return;
        ResetHoverState();
        HideTooltip();
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_isDisposed) return;
        ResetHoverState();
        HideTooltip();
    }

    // ==================== DEBOUNCE ====================

    private void Debounce_Tick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (_isDisposed) return;
        if (!_hasLastPoint) return;

        UpdateTooltipFromTextViewPoint(_lastPointInTextView);
    }

    private void ResetHoverState()
    {
        _hasLastPoint = false;
        _lastOffset = -1;
        _lastKeyShown = null;
    }

    // ==================== MAIN UPDATE ====================

    private void UpdateTooltipFromTextViewPoint(Point pInTextViewViewport)
    {
        if (_isDisposed) return;

        var tv = Tv;
        var doc = _ed.Document;

        if (tv == null || doc == null || doc.TextLength <= 0)
        {
            HideTooltip();
            return;
        }

        if (!IsInsideBounds(tv.Bounds, pInTextViewViewport))
        {
            ResetHoverState();
            HideTooltip();
            return;
        }

        // Safety clamp to viewport
        pInTextViewViewport = ClampPointToTextView(tv, pInTextViewViewport);

        if (!_cedict.IsLoaded)
        {
            ShowTooltip(BuildLoadingTooltip());
            KickoffLoadIfNeeded();
            return;
        }

        int offset = GetOffsetAtViewportPoint(tv, doc, pInTextViewViewport);
        if (offset < 0 || offset >= doc.TextLength)
        {
            HideTooltip();
            return;
        }

        char ch;
        try { ch = doc.GetCharAt(offset); }
        catch { HideTooltip(); return; }

        if (!IsCjk(ch))
        {
            HideTooltip();
            return;
        }

        var text = doc.Text;

        if (_cedict.TryLookupLongest(text, offset, out var match, maxLen: MaxLenDefault))
        {
            if (_lastKeyShown == match.Headword && _lastOffset == offset) return;

            _lastOffset = offset;
            _lastKeyShown = match.Headword;

            ShowTooltip(BuildTooltipForMatch(match));
            return;
        }

        if (_cedict.TryLookupChar(ch, out var entries) && entries.Count > 0)
        {
            var head = ch.ToString();
            if (_lastKeyShown == head && _lastOffset == offset) return;

            _lastOffset = offset;
            _lastKeyShown = head;

            ShowTooltip(BuildTooltipForEntries(head, entries));
            return;
        }

        HideTooltip();
    }

    private void KickoffLoadIfNeeded()
    {
        if (_loadKickoff) return;
        if (_cedict.IsLoaded) return;

        _loadKickoff = true;
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await _cedict.EnsureLoadedAsync(ct);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    ShowTooltip(BuildErrorTooltip("CEDICT load failed", ex.Message)));
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_isDisposed) return;
                if (_hasLastPoint) UpdateTooltipFromTextViewPoint(_lastPointInTextView);
            });
        });
    }

    // ==================== HIT TEST (SCROLL-SAFE) ====================

    private static int GetOffsetAtViewportPoint(TextView tv, TextDocument doc, Point pViewport)
    {
        try { tv.EnsureVisualLines(); } catch { }

        // AvaloniaEdit expects DOCUMENT-VISUAL coordinates here.
        // Convert viewport -> document by adding ScrollOffset.
        var so = tv.ScrollOffset; // Vector
        var pDoc = new Point(pViewport.X + so.X, pViewport.Y + so.Y);

        var pos = tv.GetPosition(pDoc) ?? tv.GetPositionFloor(pDoc);
        if (pos == null) return -1;

        try
        {
            var off = doc.GetOffset(pos.Value.Line, Math.Max(1, pos.Value.Column));
            return ClampOffset(doc, off);
        }
        catch
        {
            return -1;
        }
    }

    private static int ClampOffset(TextDocument doc, int off)
    {
        if (doc.TextLength <= 0) return -1;
        if (off < 0) return 0;
        if (off >= doc.TextLength) return doc.TextLength - 1;
        return off;
    }

    private static Point ClampPointToTextView(TextView tv, Point p)
    {
        var b = tv.Bounds;
        var x = Math.Clamp(p.X, 0, Math.Max(0, b.Width - 1));
        var y = Math.Clamp(p.Y, 0, Math.Max(0, b.Height - 1));
        return new Point(x, y);
    }

    // ==================== TOOLTIP UI (SAME STYLE SYSTEM AS TextBox) ====================

    private void ShowTooltip(Control content)
    {
        // Belt + suspenders against global ToolTip style:
        _tip.Padding = new Thickness(0);
        _tip.Background = Brushes.Transparent;
        _tip.BorderThickness = new Thickness(0);
        _tip.Content = content;

        ToolTip.SetIsOpen(_ed, true);
    }

    private void HideTooltip()
    {
        ToolTip.SetIsOpen(_ed, false);
        _tip.Content = null;
    }

    private Control BuildLoadingTooltip()
        => BuildTooltipContainer(new[] { MakeLine("Loading dictionary…", ResBrush("DictMetaFg", ThemeFallbackMeta()), false) });

    private Control BuildErrorTooltip(string title, string message)
        => BuildTooltipContainer(new[]
        {
            MakeLine(title, ResBrush("DictHeadwordFg", ThemeFallbackHead()), true),
            MakeLine(message, ResBrush("DictSenseFg", ThemeFallbackSense()), false)
        });

    private Control BuildTooltipForEntries(string headword, IReadOnlyList<CedictEntry> entries)
        => BuildTooltipForMatch(new CedictMatch(headword, 0, headword.Length, entries));

    private Control BuildTooltipForMatch(CedictMatch match)
    {
        var headFg = ResBrush("DictHeadwordFg", ThemeFallbackHead());
        var pinFg = ResBrush("DictPinyinFg", ThemeFallbackPinyin());
        var senseFg = ResBrush("DictSenseFg", ThemeFallbackSense());
        var metaFg = ResBrush("DictMetaFg", ThemeFallbackMeta());

        var entries = match.Entries.Where(e => e != null).Take(MaxEntriesShown).ToList();

        var lines = new List<TextBlock>
        {
            MakeLine(match.Headword.Trim(), headFg, true, 21)
        };

        if (entries.Count == 0)
        {
            lines.Add(MakeLine("(no entries)", metaFg, false));
            return BuildTooltipContainer(lines);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];

            var pin = string.IsNullOrWhiteSpace(e.Pinyin) ? "(no pinyin)" : e.Pinyin.Trim();
            lines.Add(MakeLine(pin, pinFg, false));

            var senses = (e.Senses ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Take(MaxSensesPerEntry)
                .ToList();

            foreach (var s in senses)
                lines.Add(MakeLine("• " + s, senseFg, false));

            if (i < entries.Count - 1)
                lines.Add(MakeLine(" ", metaFg, false));
        }

        if (match.Entries.Count > MaxEntriesShown)
            lines.Add(MakeLine($"…and {match.Entries.Count - MaxEntriesShown} more", metaFg, false));

        return BuildTooltipContainer(lines);
    }

    private Control BuildTooltipContainer(IEnumerable<TextBlock> lines)
    {
        var panel = new StackPanel { Spacing = 2 };
        foreach (var l in lines) panel.Children.Add(l);

        var bg = ResBrush("DictTipBg", ThemeFallbackBg());
        var br = ResBrush("DictTipBorder", ThemeFallbackBorder());

        return new Border
        {
            Background = bg,
            BorderBrush = br,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = panel,
            MaxWidth = 520,
            IsHitTestVisible = false
        };
    }

    private static TextBlock MakeLine(string text, IBrush fg, bool isBold, double fontSize = 14)
        => new()
        {
            Text = text,
            Foreground = fg,
            FontWeight = isBold ? FontWeight.SemiBold : FontWeight.Normal,
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap
        };

    private IBrush ResBrush(string key, IBrush fallback)
    {
        try
        {
            var app = Application.Current;
            if (app != null && app.TryFindResource(key, out var v) && v is IBrush b)
                return b;
        }
        catch { }
        return fallback;
    }

    private bool IsLightTheme()
    {
        try
        {
            var tv = Application.Current?.ActualThemeVariant;
            return ReferenceEquals(tv, ThemeVariant.Light);
        }
        catch
        {
            return false;
        }
    }

    // Theme fallbacks (so light mode doesn’t stay “night” even if Dict* active tokens aren’t wired yet)
    private IBrush ThemeFallbackBg()
        => IsLightTheme()
            ? new SolidColorBrush(Color.FromRgb(250, 250, 250))
            : new SolidColorBrush(Color.FromRgb(25, 25, 25));

    private IBrush ThemeFallbackBorder()
        => IsLightTheme()
            ? new SolidColorBrush(Color.FromRgb(205, 205, 205))
            : new SolidColorBrush(Color.FromRgb(70, 70, 70));

    private IBrush ThemeFallbackHead()
        => IsLightTheme()
            ? new SolidColorBrush(Color.FromRgb(120, 80, 0))
            : new SolidColorBrush(Color.FromRgb(255, 235, 130));

    private IBrush ThemeFallbackPinyin()
        => IsLightTheme()
            ? new SolidColorBrush(Color.FromRgb(40, 90, 150))
            : new SolidColorBrush(Color.FromRgb(170, 210, 255));

    private IBrush ThemeFallbackSense()
        => IsLightTheme()
            ? new SolidColorBrush(Color.FromRgb(35, 35, 35))
            : new SolidColorBrush(Color.FromRgb(220, 220, 220));

    private IBrush ThemeFallbackMeta()
        => IsLightTheme()
            ? new SolidColorBrush(Color.FromRgb(110, 110, 110))
            : new SolidColorBrush(Color.FromRgb(155, 155, 155));

    // ==================== UTIL ====================

    private static bool IsInsideBounds(Rect b, Point p)
        => p.X >= 0 && p.Y >= 0 && p.X < b.Width && p.Y < b.Height;

    private static bool IsCjk(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF)
            || (c >= 0x3400 && c <= 0x4DBF)
            || (c >= 0xF900 && c <= 0xFAFF)
            || (c >= 0x20000 && c <= 0x2A6DF)
            || (c >= 0x2A700 && c <= 0x2B73F)
            || (c >= 0x2B740 && c <= 0x2B81F)
            || (c >= 0x2B820 && c <= 0x2CEAF);
    }
}