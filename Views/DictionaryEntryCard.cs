// Views/DictionaryEntryCard.cs
// Reader-facing Zen/Chan dictionary entry card (wave 2 — renderer only; no triggers/hover/panels).
// Implements DICTIONARY_DISPLAY_DESIGN.md: an evidence-first, quiet, single-column read view for a
// DictionaryEntry. Code-only UserControl (content is fully dynamic, so no XAML), matching the
// dynamic-control-building style of Infrastructure/AssistantPanelRenderer. Pure presentation logic
// lives in Infrastructure/DictionaryCardModel; this file is the Avalonia rendering only.
//
// Hosting (wave 3): drop one instance into any already-scrollable docked StackPanel host
// (the Reader StudyPanel dictionary card, the Translation AssistantPane, the Scholar detail panel),
// set BrushResolver for theme parity, call Render(entry), and subscribe to the two events below.
//   • OpenOccurrenceRequested(RelPath, FromLb, ToLb) — the hero "open in reader" affordance per witness.
//   • NavigateRequested(Kind, Target)                — related-term / related-master links.
// The control raises these; it never navigates or edits anything itself.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

/// <summary>What a <see cref="DictionaryEntryCard.NavigateRequested"/> target is.</summary>
public enum DictionaryNavigateKind
{
    Term = 0,
    Master = 1,
}

/// <summary>Raised when the reader clicks a witness "open" affordance (design §5.1). Wave 3 wires it to the reader pane.</summary>
public sealed class OpenOccurrenceRequestedEventArgs : EventArgs
{
    public OpenOccurrenceRequestedEventArgs(string relPath, string? fromLb, string? toLb)
    {
        RelPath = relPath;
        FromLb = fromLb;
        ToLb = toLb;
    }

    public string RelPath { get; }
    public string? FromLb { get; }
    public string? ToLb { get; }
}

/// <summary>Raised when the reader clicks a related term or master link (design §3/§4 related region).</summary>
public sealed class DictionaryNavigateRequestedEventArgs : EventArgs
{
    public DictionaryNavigateRequestedEventArgs(DictionaryNavigateKind kind, string target)
    {
        Kind = kind;
        Target = target;
    }

    public DictionaryNavigateKind Kind { get; }
    public string Target { get; }
}

/// <summary>
/// Renders one <see cref="DictionaryEntry"/> as the designed read view. Reusable and self-contained:
/// call <see cref="Render"/> to (re)build. Restraint (design §2): one quiet column, no cards-in-cards,
/// type + whitespace do the grouping, one accent (links + curated marker share it), the only tint is disputed.
/// </summary>
public sealed class DictionaryEntryCard : UserControl
{
    // Chinese-capable font stack, shared with the assistant panels.
    private static readonly FontFamily CjkFont =
        new("'Noto Sans CJK SC', 'Source Han Sans SC', 'Microsoft YaHei', 'PingFang SC', sans-serif");

    private readonly StackPanel _root;

    /// <summary>Resolves a theme resource key to a brush (e.g. "TextFg"). Set by the host for theme parity.</summary>
    public Func<string, IBrush?>? BrushResolver { get; set; }

    /// <summary>The hero interaction: open the cited source in the reader at the exact line (design §8).</summary>
    public event EventHandler<OpenOccurrenceRequestedEventArgs>? OpenOccurrenceRequested;

    /// <summary>Navigate to a related headword or master page (wave 3 wires the target resolution).</summary>
    public event EventHandler<DictionaryNavigateRequestedEventArgs>? NavigateRequested;

    public DictionaryEntryCard()
    {
        _root = new StackPanel { Spacing = 10, Margin = new Thickness(4) };
        Content = _root;
    }

    /// <summary>Optional convenience: assign an entry to render it immediately.</summary>
    public DictionaryEntry? Entry
    {
        set => Render(value);
    }

    // ---- brush helpers (theme-aware via resolver, neutral fallbacks for host-less use/tests) ----

    private IBrush Fg => BrushResolver?.Invoke("TextFg") ?? new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a));
    private IBrush Muted => BrushResolver?.Invoke("TextMutedFg") ?? new SolidColorBrush(Color.FromRgb(0x8a, 0x8a, 0x8a));
    private IBrush Accent => BrushResolver?.Invoke("AccentLinkFg") ?? new SolidColorBrush(Color.FromRgb(0x2f, 0x6f, 0xb0));
    private IBrush Hairline => BrushResolver?.Invoke("BorderBrush") ?? new SolidColorBrush(Color.FromRgb(0xd0, 0xd0, 0xd0));
    // The one earned tint (design §6 disputed) — a restrained muted rust, not an alarm color.
    private static IBrush Rust => new SolidColorBrush(Color.FromRgb(0xa6, 0x5a, 0x3a));

    /// <summary>(Re)build the card for <paramref name="entry"/>. Clearing on null yields an empty column.</summary>
    public void Render(DictionaryEntry? entry)
    {
        _root.Children.Clear();
        if (entry == null || entry.Senses.Count == 0)
            return;

        bool singleSense = DictionaryCardModel.IsSingleSense(entry);

        // 1. HEADWORD BLOCK (design §1). Validation rides the gloss: entry-level for single-sense,
        //    per-sense for multi-sense.
        AddHeadwordBlock(entry, singleSense);

        // 2. SENSE REGION.
        if (singleSense)
        {
            // §3: suppress all sense chrome — the sole sense renders as the entry body.
            AddSenseBody(entry.Senses[0], showValidation: false /* already shown at entry level */);
        }
        // 3. RELATED REGION (design §4.3): de-duplicate to the entry foot when senses share it,
        //    otherwise render per-sense at each sense's foot.
        bool relatedUniform = DictionaryCardModel.RelatedRegionsUniform(entry);

        if (!singleSense)
        {
            for (int i = 0; i < entry.Senses.Count; i++)
                AddSenseSection(entry.Senses[i], number: i + 1, renderPerSenseRelated: !relatedUniform);
        }

        if (singleSense || relatedUniform)
            AddRelatedRegion(entry.Senses[0], topRule: true);
    }

    // ================================================================= headword

    private void AddHeadwordBlock(DictionaryEntry entry, bool singleSense)
    {
        var block = new StackPanel { Spacing = 3 };

        // SourceTerm — the single loudest element (design §2.1).
        block.Children.Add(new TextBlock
        {
            Text = entry.SourceTerm,
            FontFamily = CjkFont,
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            Foreground = Fg,
            TextWrapping = TextWrapping.Wrap,
        });

        var head = entry.Senses[0];

        if (singleSense)
        {
            // Gloss (PreferredTarget) — second loudest.
            if (!string.IsNullOrWhiteSpace(head.PreferredTarget))
                block.Children.Add(new TextBlock
                {
                    Text = head.PreferredTarget,
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Fg,
                    TextWrapping = TextWrapping.Wrap,
                });

            AddAlternateTargets(block, head.AlternateTargets);
            AddValidationSignal(block, head.Validation);
        }
        // Multi-sense: gloss + validation live in each sense header, not here.

        _root.Children.Add(block);
    }

    // "also rendered: …" — the quiet alternate-targets line (design §3). Omitted when empty (§9).
    private void AddAlternateTargets(StackPanel host, List<string>? alternates)
    {
        if (alternates == null || alternates.Count == 0)
            return;

        host.Children.Add(new TextBlock
        {
            Text = "also rendered: " + string.Join(", ", alternates),
            FontSize = 12,
            FontStyle = FontStyle.Italic,
            Foreground = Muted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0),
        });
    }

    // Quiet validation note beside the gloss (design §6). Multi-source shows NOTHING.
    private void AddValidationSignal(StackPanel host, string? validation)
    {
        var signal = DictionaryCardModel.GetValidationSignal(validation);
        if (signal == null)
            return;

        host.Children.Add(new TextBlock
        {
            Text = signal.Label,
            FontSize = 11,
            Foreground = signal.Tier == ValidationTier.Disputed ? Rust : Muted,
            FontStyle = signal.Tier == ValidationTier.Disputed ? FontStyle.Normal : FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
        });
    }

    // ================================================================= senses

    // A titled, numbered multi-sense section (design §4). Master-specific senses earn a master chip.
    private void AddSenseSection(DictionarySense sense, int number, bool renderPerSenseRelated)
    {
        var section = new StackPanel { Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };

        // Master chip (eyebrow) — the only extra element a master-specific sense earns (§4.2).
        if (!string.IsNullOrWhiteSpace(sense.MasterName))
            section.Children.Add(BuildMasterChip(sense.MasterName!));

        // Numbered title carrying the sense's own gloss (§4.1).
        section.Children.Add(new TextBlock
        {
            Text = $"{number} · {sense.PreferredTarget}",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Fg,
            TextWrapping = TextWrapping.Wrap,
        });

        AddAlternateTargets(section, sense.AlternateTargets);
        AddValidationSignal(section, sense.Validation);

        _root.Children.Add(section);
        AddSenseBody(sense, showValidation: false /* shown in the header above */);

        // Per-sense related region only when senses differ (design §4.3); uniform senses roll up to the entry foot.
        if (renderPerSenseRelated)
            AddRelatedRegion(sense, topRule: false);
    }

    // The master name as a promoted eyebrow chip (§4.1). Plain text when no page target (§9).
    private Control BuildMasterChip(string masterName)
    {
        return new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = Hairline,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 0, 0, 1),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = MakeLink(masterName, DictionaryNavigateKind.Master, masterName, sizeOverride: 11, uppercaseTracking: true),
        };
    }

    // The shared sense body (design §1 sub-order): gloss (single-sense only) → explanation →
    // scope note → evidence. Related is handled at the entry/sense foot by the caller.
    private void AddSenseBody(DictionarySense sense, bool showValidation)
    {
        if (showValidation)
            AddValidationSignal(_root, sense.Validation);

        // Explanation — the evidence-first prose survey (§1b, §9: one flowing paragraph, never bulletized).
        if (!string.IsNullOrWhiteSpace(sense.Explanation))
            _root.Children.Add(new TextBlock
            {
                Text = sense.Explanation,
                FontSize = 13,
                LineHeight = 20,
                Foreground = Fg,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
            });

        // Scope note — quietest tier; corpus counts live here, never as a badge (§1c, §2).
        if (!string.IsNullOrWhiteSpace(sense.Note))
            _root.Children.Add(new TextBlock
            {
                Text = sense.Note,
                FontSize = 11,
                FontStyle = FontStyle.Italic,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
            });

        AddEvidence(sense);
    }

    // ================================================================= evidence (§7)

    private void AddEvidence(DictionarySense sense)
    {
        var ordered = DictionaryCardModel.OrderCuratedFirst(sense.Occurrences);
        if (ordered.Count == 0)
            return;

        // "Defining witnesses" heading + optional collapse control (§7.3, long senses only, default-open).
        var header = new DockPanel { Margin = new Thickness(0, 8, 0, 2) };
        var headingText = new TextBlock
        {
            Text = "Defining witnesses",
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Muted,
        };

        // Collapsible container for the non-core (broader) witnesses; core (curated) always shown.
        var furtherHost = new StackPanel { Spacing = 8 };
        bool offerCollapse = DictionaryCardModel.ShouldOfferCollapse(sense);

        if (offerCollapse)
        {
            var toggle = MakeCollapseToggle(sense, furtherHost);
            DockPanel.SetDock(toggle, Dock.Right);
            header.Children.Add(toggle);
        }
        header.Children.Add(headingText);
        _root.Children.Add(header);

        // Curated (defining) witnesses first, each with the subtle curated marker (§7.2).
        bool anyFurtherHeading = false;
        foreach (var occ in ordered)
        {
            if (occ.Curated)
            {
                _root.Children.Add(BuildCitation(occ, curated: true));
            }
            else
            {
                if (!anyFurtherHeading)
                {
                    // "Further witnesses" subheading before the broader set (§7.2). Omitted if none.
                    furtherHost.Children.Add(new TextBlock
                    {
                        Text = "Further witnesses",
                        FontSize = 10,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = Muted,
                        Margin = new Thickness(0, 4, 0, 0),
                    });
                    anyFurtherHeading = true;
                }
                furtherHost.Children.Add(BuildCitation(occ, curated: false));
            }
        }

        if (anyFurtherHeading)
            _root.Children.Add(furtherHost);
    }

    // Optional user-initiated, default-OPEN collapse (§7.3). Collapsing folds to the curated core;
    // reopening restores the full wall. Only ever attached to long (> ~6 witness) senses.
    private Control MakeCollapseToggle(DictionarySense sense, StackPanel furtherHost)
    {
        int total = sense.Occurrences?.Count ?? 0;
        int works = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in sense.Occurrences ?? new List<DictOccurrence>())
            if (!string.IsNullOrWhiteSpace(o.RelPath) && seen.Add(o.RelPath)) works++;

        var link = new TextBlock
        {
            Text = "collapse evidence",
            FontSize = 10,
            Foreground = Accent,
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Center,
        };

        bool collapsed = false; // default OPEN (§7.1)
        link.PointerPressed += (_, _) =>
        {
            collapsed = !collapsed;
            // Fold the non-curated broader set on collapse; the curated survey core stays visible.
            furtherHost.IsVisible = !collapsed;
            link.Text = collapsed
                ? $"show all {total} witnesses ({works} works)"
                : "collapse evidence";
        };
        return link;
    }

    // ================================================================= one citation (§5)

    // A single occurrence as a citation line (NOT a card): label → KWIC quotation → attribution gloss
    // → optional nested-master provenance. Quiet indent + left hairline, never a heavy box (§2, §5).
    private Control BuildCitation(DictOccurrence occ, bool curated)
    {
        var col = new StackPanel { Spacing = 3 };

        // --- label line: [◆] English work title · master · line ref · ↗open ---
        var label = new WrapPanel { Orientation = Orientation.Horizontal };

        if (curated)
            label.Children.Add(new TextBlock
            {
                Text = "◆",
                FontSize = 11,
                Foreground = Accent, // the one accent, shared with links (§2)
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });

        var parts = DictionaryCardModel.SplitAttributionNote(occ.AttributionNote);

        // 1. English work title — loudest thing in the label (§5.1). Falls back to RelPath if the note lacked one.
        string workTitle = parts.WorkTitleEnglish ?? occ.RelPath;
        label.Children.Add(new TextBlock
        {
            Text = workTitle,
            FontSize = 12,
            FontWeight = curated ? FontWeight.SemiBold : FontWeight.Normal,
            FontStyle = FontStyle.Italic,
            Foreground = curated ? Fg : Muted,
            VerticalAlignment = VerticalAlignment.Center,
        });

        // 2. Master name — the voice speaking here; links to master page (§5.1).
        if (!string.IsNullOrWhiteSpace(occ.MasterName))
        {
            AddDot(label);
            label.Children.Add(MakeLink(occ.MasterName!, DictionaryNavigateKind.Master, occ.MasterName!, sizeOverride: 12));
        }

        // 3. Line reference — monospace-ish, quiet (§5.1).
        var lineRef = DictionaryCardModel.FormatLineRef(occ.FromLb, occ.ToLb);
        if (!string.IsNullOrWhiteSpace(lineRef))
        {
            AddDot(label);
            label.Children.Add(new TextBlock
            {
                Text = lineRef,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, 'DejaVu Sans Mono', monospace"),
                Foreground = Muted,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        // 4. Open-in-reader affordance — the single most important interaction (§5.1). Obvious on every witness.
        if (!string.IsNullOrWhiteSpace(occ.RelPath))
        {
            AddDot(label);
            var open = new TextBlock
            {
                Text = "open ↗",
                FontSize = 11,
                Foreground = Accent,
                Cursor = new Cursor(StandardCursorType.Hand),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var captured = occ;
            open.PointerPressed += (_, e) =>
            {
                e.Handled = true;
                OpenOccurrenceRequested?.Invoke(
                    this,
                    new OpenOccurrenceRequestedEventArgs(captured.RelPath, captured.FromLb, captured.ToLb));
            };
            label.Children.Add(open);
        }

        // 5. Secondary metadata (CBETA id, Chinese title) — folded into a tooltip, never primary (§5.1).
        ToolTip.SetTip(label, BuildSecondaryMetadata(occ, parts));

        col.Children.Add(label);

        // --- KWIC passage — verbatim Chinese in a quotation register, never clipped (§5.2) ---
        if (!string.IsNullOrWhiteSpace(occ.Kwic))
            col.Children.Add(new TextBlock
            {
                Text = "「" + occ.Kwic + "」",
                FontFamily = CjkFont,
                FontSize = 14,
                Foreground = Fg,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 1, 0, 0),
            });

        // --- attribution gloss — quiet tier; tells the reader what the passage shows (§5.3) ---
        if (!string.IsNullOrWhiteSpace(parts.Gloss))
            col.Children.Add(new TextBlock
            {
                Text = parts.Gloss,
                FontSize = 12,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
            });

        // --- nested-master provenance — only when >1 context master (§5.4) ---
        var provenance = DictionaryCardModel.BuildProvenanceLine(occ);
        if (!string.IsNullOrWhiteSpace(provenance))
            col.Children.Add(new TextBlock
            {
                Text = provenance,
                FontSize = 11,
                FontStyle = FontStyle.Italic,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
            });

        // Quotation treatment: indent + a single left hairline rule — never a nested shadowed box (§2).
        return new Border
        {
            BorderBrush = Hairline,
            BorderThickness = new Thickness(2, 0, 0, 0),
            Padding = new Thickness(10, 2, 0, 2),
            Margin = new Thickness(0, 4, 0, 0),
            Child = col,
        };
    }

    private static string BuildSecondaryMetadata(DictOccurrence occ, DictAttributionParts parts)
    {
        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(occ.RelPath)) bits.Add(occ.RelPath);
        if (!string.IsNullOrWhiteSpace(parts.WorkTitleChinese)) bits.Add(parts.WorkTitleChinese!);
        return bits.Count > 0 ? string.Join("  ·  ", bits) : occ.RelPath;
    }

    private void AddDot(Panel host)
    {
        host.Children.Add(new TextBlock
        {
            Text = " · ",
            FontSize = 11,
            Foreground = Muted,
            VerticalAlignment = VerticalAlignment.Center,
        });
    }

    // ================================================================= related region (§3/§4)

    private void AddRelatedRegion(DictionarySense sense, bool topRule)
    {
        var terms = sense.RelatedTerms ?? new List<string>();
        var masters = sense.RelatedMasters ?? new List<string>();
        if (terms.Count == 0 && masters.Count == 0)
            return; // §9: omit the heading entirely, never "Related: (none)"

        if (topRule)
            _root.Children.Add(new Border
            {
                BorderBrush = Hairline,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 8, 0, 0),
            });

        if (terms.Count > 0)
            _root.Children.Add(BuildLinkRow("Related", terms, DictionaryNavigateKind.Term));
        if (masters.Count > 0)
            _root.Children.Add(BuildLinkRow("Masters", masters, DictionaryNavigateKind.Master));
    }

    private Control BuildLinkRow(string heading, IReadOnlyList<string> items, DictionaryNavigateKind kind)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        wrap.Children.Add(new TextBlock
        {
            Text = heading + ": ",
            FontSize = 12,
            Foreground = Muted,
            VerticalAlignment = VerticalAlignment.Center,
        });

        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) AddDot(wrap);
            bool cjk = kind == DictionaryNavigateKind.Term;
            wrap.Children.Add(MakeLink(items[i], kind, items[i], sizeOverride: 12, cjk: cjk));
        }
        return wrap;
    }

    // A quiet clickable link that raises NavigateRequested. Shares the single accent color (§2).
    private Control MakeLink(
        string text,
        DictionaryNavigateKind kind,
        string target,
        double sizeOverride = 12,
        bool uppercaseTracking = false,
        bool cjk = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = sizeOverride,
            FontFamily = cjk ? CjkFont : FontFamily.Default,
            FontWeight = uppercaseTracking ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = Accent,
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var capturedTarget = target;
        var capturedKind = kind;
        tb.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            NavigateRequested?.Invoke(this, new DictionaryNavigateRequestedEventArgs(capturedKind, capturedTarget));
        };
        return tb;
    }
}
