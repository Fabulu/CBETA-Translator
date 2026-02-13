using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Infrastructure;

public sealed class HoverDictionaryBehaviorEdit : IDisposable
{
    private readonly TextEditor _ed;
    private readonly ICedictDictionary _cedict;

    private readonly DispatcherTimer _debounce;
    private readonly EventHandler _debounceTickHandler;
    private readonly EventHandler<VisualTreeAttachmentEventArgs> _attachedHandler;
    private readonly EventHandler<VisualTreeAttachmentEventArgs> _detachedHandler;

    // TextView scroll hook (important: keep delegate instance)
    private EventHandler? _scrollOffsetChangedHandler;

    private bool _isDisposed;
    private bool _hooked;

    private bool _hasLastPoint;
    private Point _lastPointInTextView;

    private int _lastOffset = -1;
    private string? _lastKeyShown;

    private bool _loadKickoff;
    private CancellationTokenSource? _loadCts;

    private const int DebounceMs = 120;
    private const int MaxLenDefault = 19;
    private const int MaxEntriesShown = 10;
    private const int MaxSensesPerEntry = 3;

    private static readonly IBrush BrushHeadword = new SolidColorBrush(Color.FromRgb(255, 235, 130));
    private static readonly IBrush BrushPinyin = new SolidColorBrush(Color.FromRgb(170, 210, 255));
    private static readonly IBrush BrushSense = new SolidColorBrush(Color.FromRgb(220, 220, 220));
    private static readonly IBrush BrushMeta = new SolidColorBrush(Color.FromRgb(155, 155, 155));

    public HoverDictionaryBehaviorEdit(TextEditor editor, ICedictDictionary cedict)
    {
        _ed = editor ?? throw new ArgumentNullException(nameof(editor));
        _cedict = cedict ?? throw new ArgumentNullException(nameof(cedict));

        ToolTip.SetShowDelay(_ed, 0);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DebounceMs) };
        _debounceTickHandler = (object? s, EventArgs e) => Debounce_Tick();
        _debounce.Tick += _debounceTickHandler;

        _attachedHandler = (object? s, VisualTreeAttachmentEventArgs e) => OnAttached();
        _detachedHandler = (object? s, VisualTreeAttachmentEventArgs e) => OnDetached();

        _ed.AttachedToVisualTree += _attachedHandler;
        _ed.DetachedFromVisualTree += _detachedHandler;

        HookHandlers();
        Dispatcher.UIThread.Post(() => HookHandlers(), DispatcherPriority.Loaded);

        Dispatcher.UIThread.Post(() => KickoffLoadIfNeeded(), DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _debounce.Stop();
            _debounce.Tick -= _debounceTickHandler;
        }
        catch { }

        try
        {
            _ed.AttachedToVisualTree -= _attachedHandler;
            _ed.DetachedFromVisualTree -= _detachedHandler;
        }
        catch { }

        UnhookHandlers();

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        HideTooltip();
    }

    private TextView? Tv => _ed.TextArea?.TextView;

    private void OnAttached()
    {
        HookHandlers();
        HookTextViewScrollIfNeeded();
    }

    private void OnDetached()
    {
        UnhookTextViewScroll();
        UnhookHandlers();
        ResetHoverState();
        HideTooltip();
    }

    // ==================== EVENT HOOKING ====================

    private void HookHandlers()
    {
        if (_isDisposed) return;
        if (_hooked) return;

        _hooked = true;

        _ed.AddHandler(InputElement.PointerMovedEvent,
            (object? s, PointerEventArgs e) => OnPointerMoved_Any(e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        _ed.AddHandler(InputElement.PointerPressedEvent,
            (object? s, PointerPressedEventArgs e) => OnPointerMoved_Any(e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        _ed.AddHandler(InputElement.PointerReleasedEvent,
            (object? s, PointerReleasedEventArgs e) => OnPointerMoved_Any(e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        _ed.AddHandler(InputElement.PointerWheelChangedEvent,
            (object? s, PointerWheelEventArgs e) => OnPointerWheel_Any(e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        _ed.AddHandler(InputElement.PointerExitedEvent,
            (object? s, PointerEventArgs e) => OnPointerExited_Any(),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        HookTextViewScrollIfNeeded();
    }

    private void UnhookHandlers()
    {
        if (!_hooked) return;
        _hooked = false;

        // Avalonia doesn't expose RemoveHandler; rely on control lifetime.
        UnhookTextViewScroll();
    }

    private void HookTextViewScrollIfNeeded()
    {
        var tv = Tv;
        if (tv == null) return;

        if (_scrollOffsetChangedHandler != null) return;

        _scrollOffsetChangedHandler = (object? s, EventArgs e) =>
        {
            if (_isDisposed) return;
            if (!_hasLastPoint) return;

            // wait until render so visual lines reflect the new scroll offset
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

    private void OnPointerMoved_Any(PointerEventArgs e)
    {
        if (_isDisposed) return;

        _hasLastPoint = true;
        var tv = Tv;
        if (tv != null)
        {
            try { _lastPointInTextView = e.GetPosition(tv); }
            catch { _lastPointInTextView = default; }
        }
        else
        {
            _lastPointInTextView = default;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    private void OnPointerWheel_Any(PointerWheelEventArgs e)
    {
        if (_isDisposed) return;

        // update last point
        var tv = Tv;
        if (tv != null)
        {
            _hasLastPoint = true;
            try { _lastPointInTextView = e.GetPosition(tv); }
            catch { _lastPointInTextView = default; }
        }

        // render pass after scroll
        Dispatcher.UIThread.Post(() =>
        {
            if (_isDisposed) return;
            if (!_hasLastPoint) return;
            UpdateTooltipFromTextViewPoint(_lastPointInTextView);
        }, DispatcherPriority.Render);
    }

    private void OnPointerExited_Any()
    {
        if (_isDisposed) return;
        ResetHoverState();
        HideTooltip();
    }

    // ==================== DEBOUNCE ====================

    private void Debounce_Tick()
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

        // safety clamp to viewport
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

        if (offset == _lastOffset && _lastKeyShown != null)
            return;

        _lastOffset = offset;

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
            if (_lastKeyShown == match.Headword) return;
            _lastKeyShown = match.Headword;
            ShowTooltip(BuildTooltipForMatch(match));
            return;
        }

        if (_cedict.TryLookupChar(ch, out var entries) && entries.Count > 0)
        {
            var head = ch.ToString();
            if (_lastKeyShown == head) return;
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
            catch
            {
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

    private static int GetOffsetAtViewportPoint(TextView tv, AvaloniaEdit.Document.TextDocument doc, Point pViewport)
    {
        try { tv.EnsureVisualLines(); } catch { }

        // IMPORTANT: AvaloniaEdit expects DOCUMENT-VISUAL coordinates here.
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

    private static int ClampOffset(AvaloniaEdit.Document.TextDocument doc, int off)
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

    // ==================== TOOLTIP UI ====================

    private void ShowTooltip(Control content)
    {
        try { content.IsHitTestVisible = false; } catch { }
        ToolTip.SetTip(_ed, content);
        ToolTip.SetIsOpen(_ed, true);
    }

    private void HideTooltip()
    {
        ToolTip.SetIsOpen(_ed, false);
        ToolTip.SetTip(_ed, null);
    }

    private static Control BuildLoadingTooltip()
        => BuildTooltipContainer(new[] { MakeLine("Loading dictionary…", BrushMeta, false) });

    private static Control BuildTooltipForEntries(string headword, IReadOnlyList<CedictEntry> entries)
        => BuildTooltipForMatch(new CedictMatch(headword, 0, headword.Length, entries));

    private static Control BuildTooltipForMatch(CedictMatch match)
    {
        var entries = match.Entries
            .Where(e => e != null)
            .Take(MaxEntriesShown)
            .ToList();

        var lines = new List<TextBlock> { MakeLine(match.Headword.Trim(), BrushHeadword, true) };

        if (entries.Count == 0)
        {
            lines.Add(MakeLine("(no entries)", BrushMeta, false));
            return BuildTooltipContainer(lines);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];

            var pin = string.IsNullOrWhiteSpace(e.Pinyin) ? "(no pinyin)" : e.Pinyin.Trim();
            lines.Add(MakeLine(pin, BrushPinyin, false));

            var senses = (e.Senses ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Take(MaxSensesPerEntry)
                .ToList();

            foreach (var s in senses)
                lines.Add(MakeLine("• " + s, BrushSense, false));

            if (i < entries.Count - 1)
                lines.Add(MakeLine(" ", BrushMeta, false));
        }

        if (match.Entries.Count > MaxEntriesShown)
            lines.Add(MakeLine("…and " + (match.Entries.Count - MaxEntriesShown) + " more", BrushMeta, false));

        return BuildTooltipContainer(lines);
    }

    private static Control BuildTooltipContainer(IEnumerable<TextBlock> lines)
    {
        var panel = new StackPanel { Spacing = 2 };
        foreach (var l in lines) panel.Children.Add(l);

        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = panel,
            MaxWidth = 520
        };
    }

    private static TextBlock MakeLine(string text, IBrush fg, bool isBold)
        => new()
        {
            Text = text,
            Foreground = fg,
            FontWeight = isBold ? FontWeight.SemiBold : FontWeight.Normal,
            TextWrapping = TextWrapping.Wrap
        };

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
