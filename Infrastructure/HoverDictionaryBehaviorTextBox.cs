using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Infrastructure;

public sealed class HoverDictionaryBehaviorTextBox : IDisposable
{
    private readonly TextBox _tb;
    private readonly ICedictDictionary _cedict;
    private readonly DispatcherTimer _debounce;
    private readonly ToolTip _tip;

    private bool _isDisposed;
    private Point _lastPoint;
    private bool _hasLastPoint;
    private string? _lastKeyShown;
    private int _lastOffset = -1;

    private CancellationTokenSource? _loadCts;
    private bool _loadKickoff;

    private IInputElement? _root;
    private bool _rootHooked;

    private const int DebounceMs = 70;
    private const int MaxLenDefault = 19;
    private const int MaxEntriesShown = 10;
    private const int MaxSensesPerEntry = 3;

    public HoverDictionaryBehaviorTextBox(TextBox textBox, ICedictDictionary cedict)
    {
        _tb = textBox ?? throw new ArgumentNullException(nameof(textBox));
        _cedict = cedict ?? throw new ArgumentNullException(nameof(cedict));

        _tip = new ToolTip
        {
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsHitTestVisible = false,
            Content = null
        };
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

        Dispatcher.UIThread.Post(() => KickoffLoadIfNeeded(), DispatcherPriority.Background);
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
            _tb.AttachedToVisualTree -= OnAttached;
            _tb.DetachedFromVisualTree -= OnDetached;
            _tb.PointerMoved -= OnPointerMoved;
            _tb.PointerExited -= OnPointerExited;
            _tb.PointerPressed -= OnPointerPressed;
            _tb.PointerWheelChanged -= OnPointerWheelChanged;
            _tb.PointerCaptureLost -= OnPointerCaptureLost;
            _tb.LostFocus -= OnLostFocus;
        }
        catch { }

        UnhookRootGuards();

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

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
        HideTooltip();
    }

    private void HookRootGuards()
    {
        if (_isDisposed) return;

        var top = TopLevel.GetTopLevel(_tb);
        IInputElement? root = top ?? _tb.GetVisualRoot() as IInputElement;

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
            catch { }
        }

        _rootHooked = false;
        _root = null;
    }

    private void RootPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;
        if (!ToolTip.GetIsOpen(_tb)) return;

        if (!IsPointerOverTextBox(e))
            HideTooltip();
    }

    private void RootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDisposed) return;
        if (!ToolTip.GetIsOpen(_tb)) return;

        if (!IsPointerOverTextBox(e))
            HideTooltip();
    }

    private bool IsPointerOverTextBox(PointerEventArgs e)
    {
        var top = TopLevel.GetTopLevel(_tb);
        if (top == null) return false;

        Point pTop;
        try { pTop = e.GetPosition(top); }
        catch { return false; }

        var pTb = top.TranslatePoint(pTop, _tb);
        if (!pTb.HasValue) return false;

        return pTb.Value.X >= 0 && pTb.Value.Y >= 0 && pTb.Value.X < _tb.Bounds.Width && pTb.Value.Y < _tb.Bounds.Height;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;

        _lastPoint = e.GetPosition(_tb);
        _hasLastPoint = true;

        if (_lastPoint.X < 0 || _lastPoint.Y < 0 || _lastPoint.X >= _tb.Bounds.Width || _lastPoint.Y >= _tb.Bounds.Height)
        {
            HideTooltip();
            return;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e) => HideTooltip();
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => HideTooltip();

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_isDisposed) return;
        _lastPoint = e.GetPosition(_tb);
        _hasLastPoint = true;

        Dispatcher.UIThread.Post(() =>
        {
            if (_isDisposed || !_hasLastPoint) return;
            UpdateTooltipAtPoint(_lastPoint);
        }, DispatcherPriority.Render);
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => HideTooltip();
    private void OnLostFocus(object? sender, RoutedEventArgs e) => HideTooltip();

    private void Debounce_Tick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (_isDisposed || !_hasLastPoint) return;
        UpdateTooltipAtPoint(_lastPoint);
    }

    private void UpdateTooltipAtPoint(Point pointInTextBox)
    {
        if (_isDisposed) return;

        string text = _tb.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            HideTooltip();
            return;
        }

        if (!_cedict.IsLoaded)
        {
            ShowTooltip(BuildLoadingTooltip());
            KickoffLoadIfNeeded();
            return;
        }

        int offset = TryGetOffsetAtPoint(pointInTextBox);
        if (offset < 0 || offset >= text.Length)
        {
            HideTooltip();
            return;
        }

        char ch = text[offset];
        if (!IsCjk(ch))
        {
            HideTooltip();
            return;
        }

        if (_cedict.TryLookupLongest(text, offset, out var match, maxLen: MaxLenDefault))
        {
            if (_lastKeyShown == match.Headword && _lastOffset == offset)
                return;

            _lastKeyShown = match.Headword;
            _lastOffset = offset;
            ShowTooltip(BuildTooltipForMatch(match));
            return;
        }

        if (_cedict.TryLookupChar(ch, out var entries) && entries.Count > 0)
        {
            string head = ch.ToString();
            if (_lastKeyShown == head && _lastOffset == offset)
                return;

            _lastKeyShown = head;
            _lastOffset = offset;
            ShowTooltip(BuildTooltipForEntries(head, entries));
            return;
        }

        HideTooltip();
    }

    private int TryGetOffsetAtPoint(Point p)
    {
        try
        {
            string text = _tb.Text ?? "";
            if (text.Length == 0)
                return -1;

            var presenter = _tb.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
            if (presenter == null)
                return EstimateOffsetFromTextBoxPoint(text, p);

            var pPresenter = _tb.TranslatePoint(p, presenter);
            if (!pPresenter.HasValue)
                return EstimateOffsetFromTextBoxPoint(text, p);

            var layout = presenter.TextLayout;
            if (layout == null)
                return EstimateOffsetFromTextBoxPoint(text, p);

            var hit = layout.HitTestPoint(pPresenter.Value);
            int index = hit.TextPosition;

            if (index < 0) index = 0;
            if (index >= text.Length) index = text.Length - 1;

            return index;
        }
        catch
        {
            try
            {
                return EstimateOffsetFromTextBoxPoint(_tb.Text ?? "", p);
            }
            catch
            {
                return -1;
            }
        }
    }

    private int EstimateOffsetFromTextBoxPoint(string text, Point p)
    {
        if (string.IsNullOrEmpty(text))
            return -1;

        double width = Math.Max(1, _tb.Bounds.Width);
        double height = Math.Max(1, _tb.Bounds.Height);

        double x = Math.Clamp(p.X, 0, width - 1);
        double y = Math.Clamp(p.Y, 0, height - 1);

        var lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0)
            return -1;

        double lineHeight = 20.0;
        int lineIndex = (int)(y / lineHeight);
        if (lineIndex < 0) lineIndex = 0;
        if (lineIndex >= lines.Length) lineIndex = lines.Length - 1;

        string line = lines[lineIndex] ?? "";
        int col;

        if (line.Length == 0)
        {
            col = 0;
        }
        else
        {
            double charWidth = 8.0;
            col = (int)(x / charWidth);
            if (col < 0) col = 0;
            if (col > line.Length) col = line.Length;
        }

        int offset = 0;
        for (int i = 0; i < lineIndex; i++)
            offset += lines[i].Length + 1;

        offset += col;

        if (offset < 0) offset = 0;
        if (offset >= text.Length) offset = text.Length - 1;

        return offset;
    }

    private void KickoffLoadIfNeeded()
    {
        if (_loadKickoff || _cedict.IsLoaded) return;

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
                if (_isDisposed || !_hasLastPoint) return;
                UpdateTooltipAtPoint(_lastPoint);
            });
        });
    }

    private void ShowTooltip(Control content)
    {
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
        _lastKeyShown = null;
        _lastOffset = -1;
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