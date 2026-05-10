// Views/CharacterVariantFlyout.cs
// Ctrl+click floating overlay for viewing apparatus info and witness sources.
// Works in normal reading mode (does NOT require time-travel reconstruction state).
// Maps click position → segment key → locus via LociMappingService, then looks up
// apparatus entries, anchor bases, and witness info to build a rich flyout.

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
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// Attaches to an AvaloniaEdit TextEditor and shows apparatus/variant information
/// in an overlay popup when the user Ctrl+clicks a character in normal reading mode.
/// </summary>
public sealed class CharacterVariantFlyout : IDisposable
{
    private readonly TextEditor _editor;
    private readonly Panel _overlayHost;
    private readonly Border _popupBorder;
    private bool _isDisposed;
    private bool _isVisible;

    // Data set via setter methods
    private Dictionary<string, LociEntry>? _lociMap;
    private ApparatusInfo? _apparatus;
    private List<AnchorBase>? _anchorBases;
    private List<AnchorEvent>? _anchorEvents;
    private ManifestInfo? _manifest;
    private RenderedDocument? _renderedDoc;

    // Lookup caches
    private Dictionary<string, ApparatusEntry>? _apparatusByLocus;
    private Dictionary<string, List<AnchorBase>>? _anchorBasesByLocus;

    /// <summary>
    /// Fired when user clicks a witness button.
    /// Args: (witnessId, locusId, manifest).
    /// </summary>
    public event Action<string, string, ManifestInfo?>? ViewWitnessRequested;

    public CharacterVariantFlyout(TextEditor editor, Panel overlayHost)
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
        _editor.KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isVisible && e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    // ==================== Data setters ====================

    /// <summary>Sets the loci map from LociMappingService.BuildFromXml.</summary>
    public void SetLociMap(Dictionary<string, LociEntry>? map)
    {
        _lociMap = map;
    }

    /// <summary>Sets apparatus data from ApparatusService.</summary>
    public void SetApparatus(ApparatusInfo? apparatus)
    {
        _apparatus = apparatus;

        // Build locus → entry cache
        _apparatusByLocus = null;
        if (apparatus?.Entries is { Count: > 0 })
        {
            _apparatusByLocus = new Dictionary<string, ApparatusEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in apparatus.Entries)
            {
                if (!string.IsNullOrEmpty(e.LocusId))
                    _apparatusByLocus[e.LocusId!] = e;
            }
        }
    }

    /// <summary>Sets anchor base/event data from AnchorService.</summary>
    public void SetAnchors(List<AnchorBase>? bases, List<AnchorEvent>? events)
    {
        _anchorBases = bases;
        _anchorEvents = events;

        // Build locus → anchor bases cache
        _anchorBasesByLocus = null;
        if (bases is { Count: > 0 })
        {
            _anchorBasesByLocus = new Dictionary<string, List<AnchorBase>>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in bases)
            {
                if (string.IsNullOrEmpty(b.LocusId)) continue;
                if (!_anchorBasesByLocus.TryGetValue(b.LocusId!, out var list))
                {
                    list = new List<AnchorBase>();
                    _anchorBasesByLocus[b.LocusId!] = list;
                }
                list.Add(b);
            }
        }
    }

    /// <summary>Sets manifest data for witness download URLs.</summary>
    public void SetManifest(ManifestInfo? manifest)
    {
        _manifest = manifest;
    }

    /// <summary>Sets the rendered document for segment lookup at click positions.</summary>
    public void SetRenderedDocument(RenderedDocument? doc)
    {
        _renderedDoc = doc;
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
        try { _editor.KeyDown -= OnKeyDown; } catch { }
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
            return;
        }

        // Only open on Ctrl+click
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        // Need rendered document and loci map to resolve segments
        if (_renderedDoc == null || _renderedDoc.IsEmpty || _lociMap == null || _lociMap.Count == 0)
            return;

        var tv = _editor.TextArea.TextView;
        if (tv == null) return;

        var doc = _editor.Document;
        if (doc == null || doc.TextLength == 0) return;

        // Get click position in text view coordinates
        var pointInTv = e.GetPosition(tv);

        // Convert to document offset
        int offset = GetOffsetAtViewportPoint(tv, doc, pointInTv);
        if (offset < 0 || offset >= doc.TextLength) return;

        // Find the segment at this offset
        var segment = _renderedDoc.FindSegmentAtOrBefore(offset);
        if (segment == null) return;

        var segmentKey = segment.Value.Key;

        // Map segment key → locus
        var locusUri = LociMappingService.TryGetLocus(_lociMap, segmentKey);
        var locusId = LociMappingService.StripLocusUrn(locusUri);

        if (string.IsNullOrEmpty(locusId))
            return;

        // Extract the character at click position for the header
        char ch;
        try { ch = doc.GetCharAt(offset); }
        catch { return; }

        // Get the line text excerpt
        var loc = doc.GetLocation(offset);
        string lineText;
        try
        {
            var line = doc.GetLineByNumber(loc.Line);
            lineText = doc.GetText(line.Offset, Math.Min(line.Length, 40));
        }
        catch { lineText = ""; }

        // Lookup apparatus entry
        ApparatusEntry? appEntry = null;
        _apparatusByLocus?.TryGetValue(locusId!, out appEntry);

        // Lookup anchor bases for this locus
        List<AnchorBase>? anchorBases = null;
        _anchorBasesByLocus?.TryGetValue(locusId!, out anchorBases);

        // Build and show the flyout
        var content = BuildFlyoutContent(ch, locusId!, lineText, appEntry, anchorBases);
        ShowAt(content, pointInTv, tv);

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

    private Control BuildFlyoutContent(
        char ch,
        string locusId,
        string lineExcerpt,
        ApparatusEntry? appEntry,
        List<AnchorBase>? anchorBases)
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
            ? Color.FromRgb(100, 100, 100) : Color.FromRgb(150, 150, 150));

        var stack = new StackPanel { Spacing = 6 };

        // ── Header: character + locus ──
        var headerPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 12,
        };
        headerPanel.Children.Add(new TextBlock
        {
            Text = ch.ToString(),
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = headFg,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = locusId,
            FontSize = 12,
            Foreground = labelFg,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        });
        stack.Children.Add(headerPanel);

        // Line excerpt for context
        if (!string.IsNullOrWhiteSpace(lineExcerpt))
        {
            stack.Children.Add(new TextBlock
            {
                Text = lineExcerpt,
                FontSize = 11,
                Foreground = dimFg,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 380,
                Margin = new Thickness(0, -2, 0, 0),
            });
        }

        // ── Separator ──
        stack.Children.Add(MakeSeparator(border));

        if (appEntry != null)
        {
            // ── Decision badge ──
            var decisionRaw = appEntry.Decision ?? "unknown";
            var decision = HumanizeDecision(decisionRaw);
            var (badgeBg, badgeFg) = GetDecisionBadgeColors(decisionRaw);
            var badge = new Border
            {
                Background = badgeBg,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = $"\u25cf {decision}",
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = badgeFg,
                },
            };
            stack.Children.Add(badge);

            // ── Accepted (lemma) ──
            if (!string.IsNullOrEmpty(appEntry.Lemma))
            {
                stack.Children.Add(MakeLabelValueRow("Accepted:", appEntry.Lemma!, labelFg, valueFg));
            }

            // ── Rejected readings ──
            if (appEntry.Readings is { Count: > 0 })
            {
                foreach (var reading in appEntry.Readings)
                {
                    var readingText = reading.Reading ?? "(empty)";
                    var witnessLabel = reading.WitnessId ?? "?";
                    stack.Children.Add(MakeLabelValueRow(
                        "Rejected:",
                        $"{readingText}  ({witnessLabel})",
                        labelFg, valueFg));
                }
            }

            // ── Decision basis ──
            if (!string.IsNullOrEmpty(appEntry.DecisionBasis))
            {
                stack.Children.Add(MakeLabelValueRow("Basis:", appEntry.DecisionBasis!, labelFg, dimFg));
            }
        }
        else
        {
            // ── No apparatus ──
            stack.Children.Add(new TextBlock
            {
                Text = "No editorial interventions",
                FontSize = 12,
                Foreground = valueFg,
            });

            var baseWitness = _manifest?.BaseWitnessId ?? "T1";
            stack.Children.Add(new TextBlock
            {
                Text = $"Reading accepted as-is from {baseWitness}",
                FontSize = 11,
                Foreground = dimFg,
            });
        }

        // ── Separator ──
        stack.Children.Add(MakeSeparator(border));

        // ── Evidence level message ──
        {
            bool hasCharBoxes = false;
            if (_anchorBasesByLocus != null && locusId != null &&
                _anchorBasesByLocus.TryGetValue(locusId, out var locusAnchors))
                hasCharBoxes = locusAnchors.Any(ab => ab.CharBoxes is { Count: > 0 });

            string evidenceText;
            IBrush evidenceFg;
            if (appEntry != null && hasCharBoxes)
            {
                evidenceText = "\U0001f50d Character-level evidence \u2014 click witness to zoom to exact character";
                evidenceFg = ThemeBrush(l => l ? Color.FromRgb(30, 120, 30) : Color.FromRgb(120, 220, 120));
            }
            else if (appEntry != null)
            {
                evidenceText = "\U0001f4c4 Line-level evidence \u2014 click witness to see the full line region";
                evidenceFg = ThemeBrush(l => l ? Color.FromRgb(180, 100, 0) : Color.FromRgb(255, 180, 80));
            }
            else
            {
                evidenceText = "No editorial intervention at this locus \u2014 click witness to view source page";
                evidenceFg = dimFg;
            }

            stack.Children.Add(new TextBlock
            {
                Text = evidenceText,
                FontSize = 10,
                Foreground = evidenceFg,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 4),
            });
        }

        // ── Witness buttons ──
        stack.Children.Add(new TextBlock
        {
            Text = "View witness source:",
            FontSize = 12,
            Foreground = labelFg,
            Margin = new Thickness(0, 0, 0, 4),
        });

        var witnessPanel = BuildWitnessButtons(locusId, appEntry, anchorBases, labelFg);
        stack.Children.Add(witnessPanel);

        // ── Separator ──
        stack.Children.Add(MakeSeparator(border));

        // ── Dismiss hint ──
        stack.Children.Add(new TextBlock
        {
            Text = "Click anywhere to dismiss",
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

    // ==================== Witness buttons ====================

    private Control BuildWitnessButtons(
        string locusId,
        ApparatusEntry? appEntry,
        List<AnchorBase>? anchorBases,
        IBrush labelFg)
    {
        // Collect all known witness IDs
        var witnessIds = new List<string>();
        var mentionedInApparatus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Base witness always first
        var baseId = _manifest?.BaseWitnessId ?? "T1";
        witnessIds.Add(baseId);

        // Add witnesses from manifest
        if (_manifest?.Witnesses is { Count: > 0 })
        {
            foreach (var w in _manifest.Witnesses)
            {
                if (!string.IsNullOrEmpty(w.Id) &&
                    !witnessIds.Contains(w.Id!, StringComparer.OrdinalIgnoreCase))
                {
                    witnessIds.Add(w.Id!);
                }
            }
        }

        // Track which witnesses are mentioned in apparatus for this locus
        if (appEntry != null)
        {
            if (appEntry.WitnessesSupporting != null)
            {
                foreach (var w in appEntry.WitnessesSupporting)
                    mentionedInApparatus.Add(w);
            }
            if (appEntry.Readings != null)
            {
                foreach (var r in appEntry.Readings)
                {
                    if (!string.IsNullOrEmpty(r.WitnessId))
                        mentionedInApparatus.Add(r.WitnessId!);
                }
            }
        }

        // Also add any witnesses from anchor bases not yet in the list
        if (anchorBases != null)
        {
            foreach (var ab in anchorBases)
            {
                if (!string.IsNullOrEmpty(ab.WitnessId) &&
                    !witnessIds.Contains(ab.WitnessId!, StringComparer.OrdinalIgnoreCase))
                {
                    witnessIds.Add(ab.WitnessId!);
                }
            }
        }

        var wrapPanel = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };

        var accentBrush = ThemeBrush(isLight => isLight
            ? Color.FromRgb(0, 100, 180) : Color.FromRgb(100, 180, 255));
        var normalBrush = ThemeBrush(isLight => isLight
            ? Color.FromRgb(80, 80, 80) : Color.FromRgb(170, 170, 170));

        foreach (var wid in witnessIds)
        {
            bool isHighlighted = mentionedInApparatus.Contains(wid);

            var btn = new Button
            {
                Content = wid,
                FontSize = 11,
                Margin = new Thickness(0, 0, 4, 4),
                Padding = new Thickness(8, 3),
                Foreground = isHighlighted ? accentBrush : normalBrush,
                FontWeight = isHighlighted ? FontWeight.Bold : FontWeight.Normal,
            };

            var capturedWid = wid;
            var capturedLocus = locusId;
            var capturedManifest = _manifest;
            btn.Click += (_, _) =>
            {
                Hide();
                ViewWitnessRequested?.Invoke(capturedWid, capturedLocus, capturedManifest);
            };

            wrapPanel.Children.Add(btn);
        }

        // If no witnesses at all, show at least the base witness
        if (wrapPanel.Children.Count == 0)
        {
            var btn = new Button
            {
                Content = baseId,
                FontSize = 11,
                Padding = new Thickness(8, 3),
            };
            var capturedLocus = locusId;
            var capturedManifest = _manifest;
            btn.Click += (_, _) =>
            {
                Hide();
                ViewWitnessRequested?.Invoke(baseId, capturedLocus, capturedManifest);
            };
            wrapPanel.Children.Add(btn);
        }

        return wrapPanel;
    }

    // ==================== Decision label humanization ====================

    private static string HumanizeDecision(string decision) => decision.ToLowerInvariant() switch
    {
        "supplied" => "Supplied by editor",
        "corrected" => "Corrected reading",
        "remapped" => "Remapped locus",
        "omitted_in_base" => "Omitted in base witness",
        _ => decision.Replace('_', ' ')
    };

    // ==================== Decision badge colors ====================

    private static (IBrush bg, IBrush fg) GetDecisionBadgeColors(string decision)
    {
        var d = decision.ToLowerInvariant();
        return d switch
        {
            "supplied" => (
                ThemeBrush(l => l ? Color.FromRgb(220, 245, 220) : Color.FromRgb(30, 60, 30)),
                ThemeBrush(l => l ? Color.FromRgb(30, 120, 30) : Color.FromRgb(120, 220, 120))),
            "corrected" => (
                ThemeBrush(l => l ? Color.FromRgb(255, 240, 210) : Color.FromRgb(60, 45, 20)),
                ThemeBrush(l => l ? Color.FromRgb(180, 100, 0) : Color.FromRgb(255, 180, 80))),
            "remapped" => (
                ThemeBrush(l => l ? Color.FromRgb(220, 235, 255) : Color.FromRgb(25, 40, 65)),
                ThemeBrush(l => l ? Color.FromRgb(30, 80, 180) : Color.FromRgb(100, 160, 255))),
            "omitted_in_base" => (
                ThemeBrush(l => l ? Color.FromRgb(255, 225, 225) : Color.FromRgb(60, 25, 25)),
                ThemeBrush(l => l ? Color.FromRgb(180, 30, 30) : Color.FromRgb(255, 120, 120))),
            _ => (
                ThemeBrush(l => l ? Color.FromRgb(240, 240, 240) : Color.FromRgb(45, 45, 50)),
                ThemeBrush(l => l ? Color.FromRgb(80, 80, 80) : Color.FromRgb(170, 170, 170))),
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

    private static Border MakeSeparator(IBrush borderBrush)
    {
        return new Border
        {
            Height = 1,
            Background = borderBrush,
            Margin = new Thickness(0, 2),
        };
    }

    private static StackPanel MakeLabelValueRow(string label, string value, IBrush labelBrush, IBrush valueBrush)
    {
        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 6,
        };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = labelBrush,
            FontWeight = FontWeight.SemiBold,
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 12,
            Foreground = valueBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300,
        });
        return panel;
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
