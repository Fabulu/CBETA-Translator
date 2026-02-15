using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
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

    // knobs
    private const int DebounceMs = 140;
    private const int MaxLenDefault = 19; // your dict can do 19 anyway
    private const int MaxEntriesShown = 10;
    private const int MaxSensesPerEntry = 3;

    private const bool DebugHover = true;

    // colors (tweak freely)
    private static readonly IBrush BrushHeadword = new SolidColorBrush(Color.FromRgb(255, 235, 130)); // warm yellow
    private static readonly IBrush BrushPinyin = new SolidColorBrush(Color.FromRgb(170, 210, 255)); // soft blue
    private static readonly IBrush BrushSense = new SolidColorBrush(Color.FromRgb(220, 220, 220)); // light gray
    private static readonly IBrush BrushMeta = new SolidColorBrush(Color.FromRgb(155, 155, 155)); // dim

    public HoverDictionaryBehavior(TextBox tb, ICedictDictionary cedict)
    {
        _tb = tb ?? throw new ArgumentNullException(nameof(tb));
        _cedict = cedict ?? throw new ArgumentNullException(nameof(cedict));

        ToolTip.SetShowDelay(_tb, 0);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DebounceMs) };
        _debounce.Tick += Debounce_Tick;

        _tb.PointerMoved += OnPointerMoved;
        _tb.PointerExited += OnPointerExited;
        _tb.DetachedFromVisualTree += OnDetached;
        _tb.TemplateApplied += OnTemplateApplied;

      //  if (DebugHover)
      //      Debug.WriteLine($"[HOVER] Attached to TextBox. Name={_tb.Name} IsHitTestVisible={_tb.IsHitTestVisible}");
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _debounce.Stop();
        _debounce.Tick -= Debounce_Tick;

        _tb.PointerMoved -= OnPointerMoved;
        _tb.PointerExited -= OnPointerExited;
        _tb.DetachedFromVisualTree -= OnDetached;
        _tb.TemplateApplied -= OnTemplateApplied;

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        HideTooltip();

     //   if (DebugHover)
     //       Debug.WriteLine("[HOVER] Disposed.");
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
     //   if (DebugHover)
     //       Debug.WriteLine("[HOVER] TextBox detached -> disposing behavior.");
     //   Dispose();
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

        //    if (DebugHover)
        //    {
        //        Debug.WriteLine($"[HOVER] TemplateApplied: sv={(_sv != null)} presenter={(_presenter != null)} type={_presenter?.GetType().FullName ?? "(null)"}");
        //        if (_sv != null)
        //            Debug.WriteLine($"[HOVER] SV Offset={_sv.Offset} Viewport={_sv.Viewport} Extent={_sv.Extent}");
        //    }
        }, DispatcherPriority.Loaded);

        DispatcherTimer.RunOnce(() =>
        {
            if (_isDisposed) return;
            RefreshPresenterCache();

     //       if (DebugHover)
     //           Debug.WriteLine($"[HOVER] TemplateApplied+delay: presenter={(_presenter != null)} type={_presenter?.GetType().FullName ?? "(null)"}");
        }, TimeSpan.FromMilliseconds(120));
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

        _lastPointInTextBox = e.GetPosition(_tb);
        _hasLastPoint = true;

        _debounce.Stop();
        _debounce.Start();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_isDisposed) return;

      //  if (DebugHover)
      //      Debug.WriteLine("[HOVER] PointerExited -> hide");

        _hasLastPoint = false;
        _lastIndex = -1;
        _lastKeyShown = null;
        HideTooltip();
    }

    private void UpdateTooltipFromPoint(Point pointInTextBox)
    {
        if (_isDisposed) return;

        if (!_cedict.IsLoaded)
        {
            ShowTooltip(BuildLoadingTooltip());

            if (!_loadKickoff)
            {
                _loadKickoff = true;
                _loadCts = new CancellationTokenSource();
                var ct = _loadCts.Token;

            //    if (DebugHover)
            //        Debug.WriteLine("[HOVER] Kicking off dictionary load...");

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

           //             if (DebugHover)
           //                 Debug.WriteLine("[HOVER] Dictionary loaded -> rerun hover lookup");

                        if (_hasLastPoint)
                            UpdateTooltipFromPoint(_lastPointInTextBox);
                    });
                });
            }

            return;
        }

        int idx = GetCharIndexFromPoint(pointInTextBox);

   //     if (DebugHover)
   //         Debug.WriteLine($"[HOVER] HitTest idx={idx} textLen={(_tb.Text?.Length ?? 0)}");

        var text = _tb.Text ?? "";
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
            string head = ch.ToString();
            if (_lastKeyShown == head) return;

            _lastKeyShown = head;
            ShowTooltip(BuildTooltipForEntries(head, entries));
            return;
        }

        HideTooltip();
    }

    private int GetCharIndexFromPoint(Point pointInTextBox)
    {
        var text = _tb.Text ?? "";
        if (text.Length == 0) return -1;

        if (_presenter == null || _sv == null)
            RefreshPresenterCache();

        if (_presenter == null)
            return -1;

        if (!TryMapPointToPresenter(pointInTextBox, out var pPresenter))
            return -1;

   //     if (DebugHover)
   //         Debug.WriteLine($"[HOVER] CoordMap(direct): tb={pointInTextBox} pres={pPresenter}");

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

            // Do NOT require IsInside. TextPosition is still the nearest hit.
            int idx = r.TextPosition + (r.IsTrailing ? 1 : 0);

     //       if (DebugHover)
     //           Debug.WriteLine($"[HOVER] TextLayout hit: inside={r.IsInside} pos={r.TextPosition} trailing={r.IsTrailing} -> idx={idx}");

            if (textLen <= 0) return -1;

            if (idx < 0) idx = 0;
            if (idx >= textLen) idx = textLen - 1;

            return idx;
        }
        catch (Exception)
        {
            //      if (DebugHover)
            //          Debug.WriteLine("[HOVER] TextLayout HitTest exception: " + ex.Message);
            return -1;
        }
    }

    private bool TryMapPointToPresenter(Point pointInTextBox, out Point pointInPresenter)
    {
        pointInPresenter = default;

        if (_presenter == null) return false;

        var direct = _tb.TranslatePoint(pointInTextBox, _presenter);
        if (direct != null)
        {
            pointInPresenter = direct.Value;
            return true;
        }

        if (_sv == null) return false;

        var pSv = _tb.TranslatePoint(pointInTextBox, _sv);
        if (pSv == null) return false;

        var corrected = new Point(pSv.Value.X + _sv.Offset.X, pSv.Value.Y + _sv.Offset.Y);

        var pPres = _sv.TranslatePoint(corrected, _presenter);
        if (pPres == null) return false;

        pointInPresenter = pPres.Value;
        return true;
    }

    // -------------------------- TOOLTIP UI --------------------------

    private void ShowTooltip(Control content)
    {
        ToolTip.SetTip(_tb, content);
        ToolTip.SetIsOpen(_tb, true);
    }

    private void HideTooltip()
    {
        ToolTip.SetIsOpen(_tb, false);
        ToolTip.SetTip(_tb, null);
    }

    private static Control BuildLoadingTooltip()
    {
        return BuildTooltipContainer(new[]
        {
            MakeLine("Loading dictionary…", BrushMeta, isBold:false)
        });
    }

    private static Control BuildErrorTooltip(string title, string message)
    {
        return BuildTooltipContainer(new[]
        {
            MakeLine(title, BrushHeadword, isBold:true),
            MakeLine(message, BrushSense, isBold:false)
        });
    }

    private static Control BuildTooltipForEntries(string headword, IReadOnlyList<CedictEntry> entries)
        => BuildTooltipForMatch(new CedictMatch(headword, 0, headword.Length, entries));

    private static Control BuildTooltipForMatch(CedictMatch match)
    {
        // Up to 10 entries, with up to N senses each.
        var entries = match.Entries
            .Where(e => e != null)
            .Take(MaxEntriesShown)
            .ToList();

        var lines = new List<TextBlock>();

        // header
        lines.Add(MakeLine(match.Headword.Trim(), BrushHeadword, isBold: true, fontSize: 21));

        if (entries.Count == 0)
        {
            lines.Add(MakeLine("(no entries)", BrushMeta, false));
            return BuildTooltipContainer(lines);
        }

        // entry blocks
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];

            string pin = string.IsNullOrWhiteSpace(e.Pinyin) ? "(no pinyin)" : e.Pinyin.Trim();
            lines.Add(MakeLine(pin, BrushPinyin, isBold: false));

            var senses = (e.Senses ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Take(MaxSensesPerEntry)
                .ToList();

            foreach (var s in senses)
                lines.Add(MakeLine("• " + s, BrushSense, isBold: false));

            if (i < entries.Count - 1)
                lines.Add(MakeLine(" ", BrushMeta, false)); // spacer
        }

        // footer when truncated
        if (match.Entries.Count > MaxEntriesShown)
            lines.Add(MakeLine($"…and {match.Entries.Count - MaxEntriesShown} more", BrushMeta, false));

        return BuildTooltipContainer(lines);
    }

    private static Control BuildTooltipContainer(IEnumerable<TextBlock> lines)
    {
        var panel = new StackPanel
        {
            Spacing = 2
        };

        foreach (var l in lines)
            panel.Children.Add(l);

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

    private static TextBlock MakeLine(string text, IBrush fg, bool isBold, double fontSize = 14)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = fg,
            FontWeight = isBold ? FontWeight.SemiBold : FontWeight.Normal,
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap
        };
    }

    // -------------------------- misc --------------------------

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
