using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Infrastructure;

public sealed class HoverDictionaryBehavior : IDisposable
{
    private readonly TextBox _tb;
    private readonly ICedictDictionary _cedict;

    private readonly DispatcherTimer _debounce;

    private Point _lastPointInTextBox;
    private bool _hasLastPoint;

    private int _lastIndex = -1;
    private string? _lastKeyShown;

    private bool _isDisposed;

    private bool _loadKickoff;
    private CancellationTokenSource? _loadCts;

    private ScrollViewer? _sv;
    private Visual? _presenter;

    // Tooltip instance (we control chrome)
    private readonly ToolTip _tip;

    // Root-level guard to prevent sticky tooltip when leaving the textbox (e.g., navbar)
    private IInputElement? _root;
    private bool _rootHooked;

    // knobs (snappy)
    private const int DebounceMs = 70;
    private const int MaxLenDefault = 19;
    private const int MaxEntriesShown = 10;
    private const int MaxSensesPerEntry = 3;

    public HoverDictionaryBehavior(TextBox tb, ICedictDictionary cedict)
    {
        _tb = tb ?? throw new ArgumentNullException(nameof(tb));
        _cedict = cedict ?? throw new ArgumentNullException(nameof(cedict));

        _tip = new ToolTip
        {
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsHitTestVisible = false,
            Content = null
        };

        // This class lets us override global ToolTip styling via App.axaml selector
        _tip.Classes.Add("dictTip");

        ToolTip.SetShowDelay(_tb, 0);
        ToolTip.SetTip(_tb, _tip);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DebounceMs) };
        _debounce.Tick += Debounce_Tick;

        _tb.AttachedToVisualTree += OnAttached;
        _tb.DetachedFromVisualTree += OnDetached;

        _tb.PointerMoved += OnPointerMoved;
        _tb.PointerExited += OnPointerExited;
        _tb.PointerPressed += OnPointerPressed;
        _tb.PointerWheelChanged += OnPointerWheelChanged;
        _tb.PointerCaptureLost += OnPointerCaptureLost;
        _tb.LostFocus += OnLostFocus;

        _tb.TemplateApplied += OnTemplateApplied;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _debounce.Stop();
        _debounce.Tick -= Debounce_Tick;

        _tb.AttachedToVisualTree -= OnAttached;
        _tb.DetachedFromVisualTree -= OnDetached;

        _tb.PointerMoved -= OnPointerMoved;
        _tb.PointerExited -= OnPointerExited;
        _tb.PointerPressed -= OnPointerPressed;
        _tb.PointerWheelChanged -= OnPointerWheelChanged;
        _tb.PointerCaptureLost -= OnPointerCaptureLost;
        _tb.LostFocus -= OnLostFocus;

        _tb.TemplateApplied -= OnTemplateApplied;

        UnhookRootGuards();

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        ResetHoverState();
        HideTooltip();

        try { ToolTip.SetTip(_tb, null); } catch { }
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        HookRootGuards();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        UnhookRootGuards();
        ResetHoverState();
        HideTooltip();
    }

    private void HookRootGuards()
    {
        if (_isDisposed) return;

        // Public + stable across Avalonia versions:
        // TopLevel is a Control and also an IInputElement (so AddHandler works).
        var top = TopLevel.GetTopLevel(_tb);

        // Fallback for weird cases: try visual root as IInputElement if your Avalonia exposes it
        IInputElement? root = top;
        if (root == null)
        {
            try { root = _tb.GetVisualRoot() as IInputElement; } catch { root = null; }
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
                // RemoveHandler can throw if root is already torn down; safe to ignore.
            }
        }

        _rootHooked = false;
        _root = null;
    }

    private void RootPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;
        if (!ToolTip.GetIsOpen(_tb)) return;

        // If pointer is NOT over the textbox -> kill immediately (prevents navbar stickiness).
        if (!IsPointerOverTextBox(e))
        {
            ResetHoverState();
            HideTooltip();
        }
    }

    private void RootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDisposed) return;
        if (!ToolTip.GetIsOpen(_tb)) return;

        // Clicking anywhere else should close it instantly.
        if (!IsPointerOverTextBox(e))
        {
            ResetHoverState();
            HideTooltip();
        }
    }

    private bool IsPointerOverTextBox(PointerEventArgs e)
    {
        // Use TopLevel as the coordinate root (public API).
        var top = TopLevel.GetTopLevel(_tb);
        if (top == null) return false;

        Point pTop;
        try { pTop = e.GetPosition(top); }
        catch { return false; }

        // Convert TopLevel coords -> TextBox coords
        var pTb = top.TranslatePoint(pTop, _tb);
        if (!pTb.HasValue) return false;

        return IsInsideBounds(_tb.Bounds, pTb.Value);
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        try { _sv = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer"); }
        catch { _sv = null; }

        _sv ??= _tb.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        Dispatcher.UIThread.Post(() =>
        {
            if (_isDisposed) return;
            RefreshPresenterCache();
        }, DispatcherPriority.Loaded);

        DispatcherTimer.RunOnce(() =>
        {
            if (_isDisposed) return;
            RefreshPresenterCache();
        }, TimeSpan.FromMilliseconds(80));
    }

    private void RefreshPresenterCache()
    {
        _sv ??= _tb.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        _presenter = _tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => string.Equals(v.GetType().Name, "TextPresenter", StringComparison.Ordinal));

        _presenter ??= _tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => (v.GetType().Name?.Contains("Text", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private void Debounce_Tick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (_isDisposed) return;
        if (!_hasLastPoint) return;

        UpdateTooltipFromPoint(_lastPointInTextBox);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;

        var p = e.GetPosition(_tb);
        _lastPointInTextBox = p;
        _hasLastPoint = true;

        if (!IsInsideBounds(_tb.Bounds, p))
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
        ResetHoverState();
        HideTooltip();
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

    private void ResetHoverState()
    {
        _hasLastPoint = false;
        _lastIndex = -1;
        _lastKeyShown = null;
    }

    private void UpdateTooltipFromPoint(Point pointInTextBox)
    {
        if (_isDisposed) return;

        if (!IsInsideBounds(_tb.Bounds, pointInTextBox))
        {
            ResetHoverState();
            HideTooltip();
            return;
        }

        if (!_cedict.IsLoaded)
        {
            ShowTooltip(BuildLoadingTooltip());
            KickoffLoadIfNeeded();
            return;
        }

        int idx = GetCharIndexFromPoint(pointInTextBox);

        var text = _tb.Text ?? string.Empty;
        if (idx < 0 || idx >= text.Length)
        {
            HideTooltip();
            return;
        }

        char ch = text[idx];
        if (!IsCjk(ch))
        {
            HideTooltip();
            return;
        }

        if (_cedict.TryLookupLongest(text, idx, out var match, maxLen: MaxLenDefault))
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
                if (_hasLastPoint)
                    UpdateTooltipFromPoint(_lastPointInTextBox);
            });
        });
    }

    private int GetCharIndexFromPoint(Point pointInTextBox)
    {
        var text = _tb.Text ?? string.Empty;
        if (text.Length == 0) return -1;

        if (_presenter == null || _sv == null)
            RefreshPresenterCache();

        if (_presenter == null)
            return -1;

        if (!TryMapPointToPresenter(pointInTextBox, out var pPresenter))
            return -1;

        int idx = TryHitTestViaTextLayout(_presenter, pPresenter, text.Length);
        if (idx >= 0)
        {
            _lastIndex = idx;
            return idx;
        }

        if (_lastIndex >= 0 && _lastIndex < text.Length)
            return _lastIndex;

        return -1;
    }

    private static int TryHitTestViaTextLayout(Visual presenter, Point pPresenter, int textLen)
    {
        try
        {
            var layoutProp = presenter.GetType().GetProperty(
                "TextLayout",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (layoutProp == null)
                return -1;

            var layoutObj = layoutProp.GetValue(presenter);
            if (layoutObj is not TextLayout tl)
                return -1;

            var r = tl.HitTestPoint(pPresenter);

            int idx = r.TextPosition + (r.IsTrailing ? 1 : 0);

            if (textLen <= 0) return -1;

            if (idx < 0) idx = 0;
            if (idx >= textLen) idx = textLen - 1;

            return idx;
        }
        catch
        {
            return -1;
        }
    }

    private bool TryMapPointToPresenter(Point pointInTextBox, out Point pointInPresenter)
    {
        pointInPresenter = default;

        if (_presenter == null) return false;

        var direct = _tb.TranslatePoint(pointInTextBox, _presenter);
        if (direct.HasValue)
        {
            pointInPresenter = direct.Value;
            return true;
        }

        if (_sv == null) return false;

        var pSv = _tb.TranslatePoint(pointInTextBox, _sv);
        if (!pSv.HasValue) return false;

        var corrected = new Point(pSv.Value.X + _sv.Offset.X, pSv.Value.Y + _sv.Offset.Y);

        var pPres = _sv.TranslatePoint(corrected, _presenter);
        if (!pPres.HasValue) return false;

        pointInPresenter = pPres.Value;
        return true;
    }

    // -------------------------- TOOLTIP UI --------------------------

    private void ShowTooltip(Control content)
    {
        // Belt + suspenders against global ToolTip style:
        _tip.Padding = new Thickness(0);
        _tip.Background = Brushes.Transparent;
        _tip.BorderThickness = new Thickness(0);
        _tip.Content = content;

        ToolTip.SetIsOpen(_tb, true);
    }

    private void HideTooltip()
    {
        ToolTip.SetIsOpen(_tb, false);
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