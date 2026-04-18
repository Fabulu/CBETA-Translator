// Views/CharacterProvenanceFlyout.cs
// Click-to-inspect provenance flyout for the time-travel Reader pane.
// When the user clicks a CJK character in _aeOrig during time-travel mode,
// this shows OCR consensus and provenance data for that character position.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// Attaches to an AvaloniaEdit TextEditor and shows character-level provenance
/// information in an overlay popup when the user clicks a character during
/// time-travel mode.
/// </summary>
public sealed class CharacterProvenanceFlyout : IDisposable
{
    private readonly TextEditor _editor;
    private readonly Panel _overlayHost;
    private readonly Border _popupBorder;
    private bool _isDisposed;
    private bool _isVisible;

    // Provenance data
    private List<OcrConsensusEntry>? _ocrConsensus;
    private List<CharacterProvenanceEntry>? _charProvenance;

    // Reconstruction state: line index -> (Locus, Text)
    private List<(string Locus, string Text)>? _reconstructionLines;

    // Lookup caches built from flat lists
    private Dictionary<string, List<OcrConsensusEntry>>? _ocrByLocus;
    private Dictionary<(string Locus, int Position), CharacterProvenanceEntry>? _provByLocusPos;

    /// <summary>
    /// Fired when user clicks a "View in witness" button.
    /// Args: (witnessLabel, locus).
    /// </summary>
    public event Action<string, string>? ViewWitnessPageRequested;

    public CharacterProvenanceFlyout(TextEditor editor, Panel overlayHost)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _overlayHost = overlayHost ?? throw new ArgumentNullException(nameof(overlayHost));

        _popupBorder = new Border
        {
            IsVisible = false,
            IsHitTestVisible = true,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MaxWidth = 420,
        };
        _overlayHost.Children.Add(_popupBorder);

        _editor.TextArea.PointerPressed += OnPointerPressed;
    }

    /// <summary>
    /// Loads OCR consensus and character provenance data for lookup.
    /// Either or both may be null/empty.
    /// </summary>
    public void SetProvenanceData(
        List<OcrConsensusEntry>? ocrConsensus,
        List<CharacterProvenanceEntry>? charProvenance)
    {
        _ocrConsensus = ocrConsensus;
        _charProvenance = charProvenance;

        // Build lookup caches
        _ocrByLocus = null;
        _provByLocusPos = null;

        if (ocrConsensus is { Count: > 0 })
        {
            _ocrByLocus = new Dictionary<string, List<OcrConsensusEntry>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var e in ocrConsensus)
            {
                if (!_ocrByLocus.TryGetValue(e.Locus, out var list))
                {
                    list = new List<OcrConsensusEntry>();
                    _ocrByLocus[e.Locus] = list;
                }
                list.Add(e);
            }
        }

        if (charProvenance is { Count: > 0 })
        {
            _provByLocusPos = new Dictionary<(string, int), CharacterProvenanceEntry>();
            foreach (var e in charProvenance)
                _provByLocusPos[(e.Locus, e.Position)] = e;
        }
    }

    /// <summary>
    /// Sets the current reconstruction state so clicks can be mapped to loci.
    /// Each element is one display line: (Locus, Text).
    /// </summary>
    public void SetReconstructionState(List<(string Locus, string Text)>? lines)
    {
        _reconstructionLines = lines;
    }

    /// <summary>Detaches event handlers and removes the overlay.</summary>
    public void Detach()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try { _editor.TextArea.PointerPressed -= OnPointerPressed; } catch { }
        Hide();
        try { _overlayHost.Children.Remove(_popupBorder); } catch { }
    }

    // ==================== Event handling ====================

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDisposed) return;

        var props = e.GetCurrentPoint(_editor.TextArea).Properties;

        // Right-click or middle-click: ignore
        if (!props.IsLeftButtonPressed) return;

        // If popup is already visible, hide it and let the click propagate
        if (_isVisible)
        {
            Hide();
            // Don't handle the event — let normal text selection work
            return;
        }

        // Only open on Ctrl+click to avoid conflicting with text selection
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        // Only activate when reconstruction state is loaded (time-travel mode)
        if (_reconstructionLines == null || _reconstructionLines.Count == 0)
            return;

        var tv = _editor.TextArea.TextView;
        if (tv == null) return;

        var doc = _editor.Document;
        if (doc == null || doc.TextLength == 0) return;

        // Get click position in text view coordinates
        var pointInTv = e.GetPosition(tv);

        // Convert to document offset using same approach as hover dictionary
        int offset = GetOffsetAtViewportPoint(tv, doc, pointInTv);
        if (offset < 0 || offset >= doc.TextLength) return;

        char ch;
        try { ch = doc.GetCharAt(offset); }
        catch { return; }

        if (!IsCjk(ch)) return;

        // Map offset -> line number (1-based in AvaloniaEdit) -> locus
        var loc = doc.GetLocation(offset);
        int lineIndex = loc.Line - 1; // 0-based index into _reconstructionLines

        if (lineIndex < 0 || lineIndex >= _reconstructionLines.Count) return;

        var (locus, lineText) = _reconstructionLines[lineIndex];
        int column = loc.Column - 1; // 0-based position within the line

        // Build and show the flyout
        var content = BuildFlyoutContent(ch, locus, column);
        ShowAt(content, pointInTv, tv);

        // Mark handled so AvaloniaEdit doesn't start text selection
        e.Handled = true;
    }

    // ==================== Hit testing ====================

    private static int GetOffsetAtViewportPoint(TextView tv, AvaloniaEdit.Document.TextDocument doc, Point pViewport)
    {
        try { tv.EnsureVisualLines(); } catch { }

        var so = tv.ScrollOffset;
        var pDoc = new Point(pViewport.X + so.X, pViewport.Y + so.Y);

        var pos = tv.GetPosition(pDoc) ?? tv.GetPositionFloor(pDoc);
        if (pos == null) return -1;

        try
        {
            var off = doc.GetOffset(pos.Value.Line, Math.Max(1, pos.Value.Column));
            if (off < 0) return 0;
            if (off >= doc.TextLength) return doc.TextLength - 1;
            return off;
        }
        catch { return -1; }
    }

    // ==================== Flyout content ====================

    private Control BuildFlyoutContent(char ch, string locus, int position)
    {
        var bg = ThemeBrush(isLight => isLight
            ? Color.FromRgb(252, 252, 252) : Color.FromRgb(30, 30, 35));
        var border = ThemeBrush(isLight => isLight
            ? Color.FromRgb(190, 190, 200) : Color.FromRgb(75, 75, 85));
        var headFg = ThemeBrush(isLight => isLight
            ? Color.FromRgb(120, 40, 0) : Color.FromRgb(255, 200, 100));
        var labelFg = ThemeBrush(isLight => isLight
            ? Color.FromRgb(80, 80, 80) : Color.FromRgb(170, 170, 170));
        var valueFg = ThemeBrush(isLight => isLight
            ? Color.FromRgb(30, 30, 30) : Color.FromRgb(230, 230, 230));
        var dimFg = ThemeBrush(isLight => isLight
            ? Color.FromRgb(140, 140, 140) : Color.FromRgb(120, 120, 120));

        var stack = new StackPanel { Spacing = 6 };

        // Header: the character + locus
        var headerPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
        headerPanel.Children.Add(new TextBlock
        {
            Text = ch.ToString(),
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = headFg,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        });
        headerPanel.Children.Add(new StackPanel
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = $"Locus: {locus}", FontSize = 12, Foreground = labelFg },
                new TextBlock { Text = $"Position: {position}", FontSize = 11, Foreground = dimFg },
            }
        });
        stack.Children.Add(headerPanel);

        // Separator
        stack.Children.Add(new Border
        {
            Height = 1,
            Background = border,
            Margin = new Thickness(0, 2),
        });

        bool hasData = false;

        // OCR Consensus section
        if (_ocrByLocus != null && _ocrByLocus.TryGetValue(locus, out var ocrEntries) && ocrEntries.Count > 0)
        {
            // Find the entry whose Adopted character matches (or the first one if no exact match)
            var relevant = ocrEntries.FirstOrDefault(e => e.Adopted == ch.ToString())
                           ?? ocrEntries[0];

            stack.Children.Add(new TextBlock
            {
                Text = "OCR Consensus",
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = labelFg,
                Margin = new Thickness(0, 2, 0, 2),
            });

            // Engine readings table
            var engines = new (string Name, string Reading)[]
            {
                ("Tesseract", relevant.Tesseract),
                ("RapidOCR", relevant.RapidOCR),
                ("PaddleOCR", relevant.PaddleOCR),
                ("EasyOCR", relevant.EasyOCR),
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,8,Auto"),
                RowDefinitions = new RowDefinitions(string.Join(",",
                    Enumerable.Repeat("Auto", engines.Length + 2))),
            };

            int row = 0;
            foreach (var (name, reading) in engines)
            {
                var nameBlock = new TextBlock
                {
                    Text = name + ":",
                    FontSize = 12,
                    Foreground = labelFg,
                };
                Grid.SetRow(nameBlock, row);
                Grid.SetColumn(nameBlock, 0);
                grid.Children.Add(nameBlock);

                var readingBlock = new TextBlock
                {
                    Text = string.IsNullOrEmpty(reading) ? "-" : reading,
                    FontSize = 12,
                    Foreground = valueFg,
                    FontWeight = reading == ch.ToString() ? FontWeight.Bold : FontWeight.Normal,
                };
                Grid.SetRow(readingBlock, row);
                Grid.SetColumn(readingBlock, 2);
                grid.Children.Add(readingBlock);

                row++;
            }

            // Agreement + Basis
            var agreementLabel = new TextBlock { Text = "Agreement:", FontSize = 12, Foreground = labelFg };
            Grid.SetRow(agreementLabel, row); Grid.SetColumn(agreementLabel, 0);
            grid.Children.Add(agreementLabel);

            var agreementValue = new TextBlock { Text = relevant.Agreement, FontSize = 12, Foreground = valueFg };
            Grid.SetRow(agreementValue, row); Grid.SetColumn(agreementValue, 2);
            grid.Children.Add(agreementValue);
            row++;

            var basisLabel = new TextBlock { Text = "Basis:", FontSize = 12, Foreground = labelFg };
            Grid.SetRow(basisLabel, row); Grid.SetColumn(basisLabel, 0);
            grid.Children.Add(basisLabel);

            var basisValue = new TextBlock
            {
                Text = relevant.Basis,
                FontSize = 12,
                Foreground = valueFg,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280,
            };
            Grid.SetRow(basisValue, row); Grid.SetColumn(basisValue, 2);
            grid.Children.Add(basisValue);

            stack.Children.Add(grid);
            hasData = true;
        }

        // Character Provenance section
        CharacterProvenanceEntry? provEntry = null;
        if (_provByLocusPos != null)
            _provByLocusPos.TryGetValue((locus, position), out provEntry);

        if (provEntry != null)
        {
            if (hasData)
                stack.Children.Add(new Border { Height = 1, Background = border, Margin = new Thickness(0, 2) });

            stack.Children.Add(new TextBlock
            {
                Text = "Character Provenance",
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = labelFg,
                Margin = new Thickness(0, 2, 0, 2),
            });

            var provGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,8,Auto"),
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            };

            var items = new (string Label, string Value)[]
            {
                ("Source:", provEntry.Source),
                ("Confidence:", provEntry.Confidence),
                ("Witness:", provEntry.Witness),
            };

            for (int i = 0; i < items.Length; i++)
            {
                var lbl = new TextBlock { Text = items[i].Label, FontSize = 12, Foreground = labelFg };
                Grid.SetRow(lbl, i); Grid.SetColumn(lbl, 0);
                provGrid.Children.Add(lbl);

                var val = new TextBlock { Text = items[i].Value, FontSize = 12, Foreground = valueFg };
                Grid.SetRow(val, i); Grid.SetColumn(val, 2);
                provGrid.Children.Add(val);
            }

            stack.Children.Add(provGrid);

            // "View in witness" button
            if (!string.IsNullOrWhiteSpace(provEntry.Witness))
            {
                var btn = new Button
                {
                    Content = $"View in {provEntry.Witness}",
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 0),
                    Padding = new Thickness(8, 3),
                };
                var witness = provEntry.Witness;
                var loc = locus;
                btn.Click += (_, _) =>
                {
                    Hide();
                    ViewWitnessPageRequested?.Invoke(witness, loc);
                };
                stack.Children.Add(btn);
            }

            hasData = true;
        }

        if (!hasData)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "No provenance data available for this character.",
                FontSize = 12,
                Foreground = dimFg,
                FontStyle = FontStyle.Italic,
            });
        }

        // Close hint
        stack.Children.Add(new TextBlock
        {
            Text = "Click anywhere to dismiss \u2022 Ctrl+click a character for provenance details",
            FontSize = 10,
            Foreground = dimFg,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0),
        });

        return new Border
        {
            Background = bg,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Child = stack,
        };
    }

    // ==================== Show / Hide / Position ====================

    private void ShowAt(Control content, Point pointInTextView, TextView tv)
    {
        _popupBorder.Child = content;

        // Position relative to overlay host
        var pointInOverlay = tv.TranslatePoint(pointInTextView, _overlayHost);
        if (!pointInOverlay.HasValue) return;

        _popupBorder.Measure(new Size(420, double.PositiveInfinity));
        var size = _popupBorder.DesiredSize;

        double x = pointInOverlay.Value.X + 16;
        double y = pointInOverlay.Value.Y + 20;

        double maxX = _overlayHost.Bounds.Width - size.Width - 8;
        double maxY = _overlayHost.Bounds.Height - size.Height - 8;
        if (x > maxX) x = Math.Max(0, pointInOverlay.Value.X - size.Width - 8);
        if (y > maxY) y = Math.Max(0, pointInOverlay.Value.Y - size.Height - 8);
        x = Math.Max(0, x);
        y = Math.Max(0, y);

        Canvas.SetLeft(_popupBorder, x);
        Canvas.SetTop(_popupBorder, y);

        _popupBorder.IsVisible = true;
        _isVisible = true;
    }

    private void Hide()
    {
        _popupBorder.IsVisible = false;
        _isVisible = false;
    }

    // ==================== Utility ====================

    private static bool IsCjk(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF)
            || (c >= 0x3400 && c <= 0x4DBF)
            || (c >= 0xF900 && c <= 0xFAFF);
    }

    private static bool IsLightTheme()
    {
        try
        {
            var tv = Application.Current?.ActualThemeVariant;
            return ReferenceEquals(tv, ThemeVariant.Light);
        }
        catch { return false; }
    }

    private static IBrush ThemeBrush(Func<bool, Color> pick)
        => new SolidColorBrush(pick(IsLightTheme()));
}
