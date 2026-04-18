// Views/EditionProcessDialog.axaml.cs
// Full edition details dialog with 5 tabs: Sources, Process, Apparatus, Stats, Documents.
// Opened from ProvenancePanel's "View Edition Details..." button.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

public partial class EditionProcessDialog : Window
{
    public EditionProcessDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Loads all edition data and populates the dialog tabs.
    /// </summary>
    private List<TimelineEvent> _timelineEvents = new();
    private List<TimelineEvent> _filteredEvents = new();
    private int _timelineIndex;
    private TimelineInfo? _timeline;
    private RenderedDocument? _finalRenderedDoc;
    private string? _finalText;
    private TextEditor? _editorPreview;
    private LocusHighlightRenderer? _locusHighlighter;
    private WitnessTextRegistry? _witnessRegistry;
    private ApparatusInfo? _apparatus;
    private string? _editionDir;
    private bool _leidenMode;

    public void Load(
        ManifestInfo manifest,
        string? xmlAbsPath,
        ProcessService? processService,
        ApparatusService? apparatusService,
        EditionStatsService? statsService,
        DocumentsService? documentsService,
        TimelineService? timelineService = null,
        HumanLogService? humanLogService = null,
        RenderedDocument? renderedTranslation = null,
        WitnessTextService? witnessTextService = null)
    {
        // Header
        var txtTitle = this.FindControl<TextBlock>("TxtDialogTitle");
        var txtSub = this.FindControl<TextBlock>("TxtDialogSubtitle");
        if (txtTitle != null) txtTitle.Text = manifest.WorkName ?? "(untitled)";
        if (txtSub != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(manifest.Author)) parts.Add(manifest.Author);
            if (!string.IsNullOrWhiteSpace(manifest.EditionKind)) parts.Add(FormatEditionKind(manifest.EditionKind));
            if (!string.IsNullOrWhiteSpace(manifest.EditionMaturity)) parts.Add(FormatMaturity(manifest.EditionMaturity));
            txtSub.Text = string.Join(" \u2022 ", parts);
        }

        // Load optional JSON data
        ProcessInfo? process = null;
        ApparatusInfo? apparatus = null;
        EditionStatsInfo? stats = null;
        DocumentsInfo? documents = null;
        TimelineInfo? timeline = null;
        string? humanLog = null;

        WitnessTextRegistry? witnessRegistry = null;
        if (xmlAbsPath != null)
        {
            process = processService?.TryLoad(xmlAbsPath);
            apparatus = apparatusService?.TryLoad(xmlAbsPath);
            stats = statsService?.TryLoad(xmlAbsPath);
            documents = documentsService?.TryLoad(xmlAbsPath);
            timeline = timelineService?.TryLoad(xmlAbsPath);
            humanLog = humanLogService?.TryLoad(xmlAbsPath);
            witnessRegistry = witnessTextService?.TryLoad(xmlAbsPath);

            // Cache for locus-aware actions in the apparatus + witnesses tabs
            _witnessRegistry = witnessRegistry;
            _apparatus = apparatus;
            try { _editionDir = Path.GetDirectoryName(xmlAbsPath); } catch { _editionDir = null; }
        }

        // Store rendered doc for timeline text preview
        _finalRenderedDoc = renderedTranslation;
        _finalText = renderedTranslation?.Text;

        PopulateSources(manifest, witnessRegistry);
        PopulateTimeline(timeline);
        PopulateLog(humanLog);
        PopulateProcess(process, manifest);
        PopulateApparatus(apparatus);
        PopulateCollation(apparatus, witnessRegistry);

        var chkLeiden = this.FindControl<CheckBox>("ChkLeidenView");
        if (chkLeiden != null)
        {
            chkLeiden.IsCheckedChanged += (_, _) =>
            {
                _leidenMode = chkLeiden.IsChecked == true;
                var host = this.FindControl<StackPanel>("ApparatusHost");
                if (host != null) host.Children.Clear();
                if (_leidenMode && _apparatus?.Entries is { Count: > 0 })
                    RenderLeidenApparatus(_apparatus);
                else
                    PopulateApparatus(_apparatus);
            };
        }

        PopulateCorrections(process, xmlAbsPath);
        PopulateForensicData();
        PopulateStats(stats);
        PopulateDocuments(documents, xmlAbsPath, manifest.TextId);
    }

    // ── Sources tab ──────────────────────────────────────────────────────

    private void PopulateSources(ManifestInfo manifest, WitnessTextRegistry? witnessRegistry)
    {
        var host = this.FindControl<StackPanel>("SourcesHost");
        if (host == null) return;

        // Structured date provenance (Phase D). Renders only when at least
        // one of the four new date fields is present — editions that still
        // use bare year_composed fall through silently.
        var datesPanel = BuildDatesSection(manifest);
        if (datesPanel != null) host.Children.Add(datesPanel);

        if (manifest.Witnesses == null || manifest.Witnesses.Count == 0)
        {
            host.Children.Add(MakeEmptyState("No witnesses recorded."));
            return;
        }

        // If witnesses.json exists, show a banner noting the richer delivery data
        if (witnessRegistry?.Witnesses is { Count: > 0 } entries)
        {
            var banner = new Border
            {
                Background = Avalonia.Media.Brushes.Transparent,
                BorderBrush = Application.Current?.Resources["AccentBrush"] as Avalonia.Media.IBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = $"✓ witnesses.json delivered — {entries.Count} witness(es) with locus-level comparison ready",
                    FontSize = 11,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
            };
            host.Children.Add(banner);
        }

        // Build lookup of witness_id -> registry entry for enriched cards
        var registryByWitnessId = new Dictionary<string, WitnessTextEntry>(StringComparer.OrdinalIgnoreCase);
        if (witnessRegistry?.Witnesses != null)
        {
            foreach (var entry in witnessRegistry.Witnesses)
            {
                if (!string.IsNullOrWhiteSpace(entry.WitnessId))
                    registryByWitnessId[entry.WitnessId] = entry;
            }
        }

        foreach (var w in manifest.Witnesses)
        {
            WitnessTextEntry? enrichment = null;
            if (!string.IsNullOrWhiteSpace(w.Id))
                registryByWitnessId.TryGetValue(w.Id, out enrichment);
            host.Children.Add(BuildWitnessCard(w, manifest.BaseWitnessId, enrichment));
        }
    }

    /// <summary>
    /// Renders the 4-field structured date section at the top of the Sources
    /// tab when any of composition_date / manuscript_date / redaction_date /
    /// textual_criticism_date is present. Returns null when no structured
    /// dates are available so callers can skip adding an empty block.
    ///
    /// Backward-compat: if only `year_composed` is set (no structured dates),
    /// renders a minimal single-line "Composed: YEAR" row instead.
    /// </summary>
    private static Border? BuildDatesSection(ManifestInfo m)
    {
        bool hasAnyStructured =
            !string.IsNullOrWhiteSpace(m.CompositionDate) ||
            !string.IsNullOrWhiteSpace(m.ManuscriptDate) ||
            !string.IsNullOrWhiteSpace(m.RedactionDate) ||
            !string.IsNullOrWhiteSpace(m.TextualCriticismDate);

        bool hasLegacyOnly = !hasAnyStructured && !string.IsNullOrWhiteSpace(m.YearComposed);

        if (!hasAnyStructured && !hasLegacyOnly) return null;

        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = "Dates",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
        });

        if (hasLegacyOnly)
        {
            // Editions predating the 4-field structure just get one line.
            stack.Children.Add(BuildDateRow("Composed", m.YearComposed!));
        }
        else
        {
            // Four distinct axes, in chronological-source order:
            //   composition → manuscript → redaction → textual criticism
            if (!string.IsNullOrWhiteSpace(m.CompositionDate))
                stack.Children.Add(BuildDateRow("Composition", m.CompositionDate!));
            if (!string.IsNullOrWhiteSpace(m.ManuscriptDate))
                stack.Children.Add(BuildDateRow("Manuscript", m.ManuscriptDate!));
            if (!string.IsNullOrWhiteSpace(m.RedactionDate))
                stack.Children.Add(BuildDateRow("Redaction", m.RedactionDate!));
            if (!string.IsNullOrWhiteSpace(m.TextualCriticismDate))
                stack.Children.Add(BuildDateRow("Textual criticism", m.TextualCriticismDate!));
        }

        return new Border
        {
            BorderBrush = Application.Current?.Resources["BorderBrush"] as Avalonia.Media.IBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 10),
            Child = stack,
        };
    }

    /// <summary>
    /// One labeled row in the Dates section: "Label: value". Bolds the label,
    /// wraps the value, keeps them on one visual line via inline runs so long
    /// values can wrap without breaking label alignment.
    /// </summary>
    private static TextBlock BuildDateRow(string label, string value)
    {
        var tb = new TextBlock
        {
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 1),
        };
        tb.Inlines ??= new Avalonia.Controls.Documents.InlineCollection();
        tb.Inlines.Add(new Avalonia.Controls.Documents.Run($"{label}: ")
        {
            FontWeight = FontWeight.SemiBold,
        });
        tb.Inlines.Add(new Avalonia.Controls.Documents.Run(value));
        return tb;
    }

    private static Border BuildWitnessCard(WitnessInfo w, string? baseWitnessId, WitnessTextEntry? enrichment = null)
    {
        var stack = new StackPanel { Spacing = 2 };

        // Label + base badge
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerPanel.Children.Add(new TextBlock
        {
            Text = w.Label ?? w.Id ?? "(unknown witness)",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });

        if (!string.IsNullOrWhiteSpace(baseWitnessId) &&
            string.Equals(w.Id, baseWitnessId, StringComparison.OrdinalIgnoreCase))
        {
            headerPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 0, 140, 220)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = "BASE", FontSize = 9, FontWeight = FontWeight.Bold },
            });
        }
        stack.Children.Add(headerPanel);

        // Kind + Role + Family
        var meta = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(w.Kind)) meta.Append(FormatWitnessKind(w.Kind));
        if (!string.IsNullOrWhiteSpace(w.RoleInProduction))
        {
            if (meta.Length > 0) meta.Append(" | ");
            meta.Append(w.RoleInProduction);
        }
        if (!string.IsNullOrWhiteSpace(w.FamilyId))
        {
            if (meta.Length > 0) meta.Append(" | ");
            meta.Append($"Family: {w.FamilyId}");
        }
        if (meta.Length > 0)
            stack.Children.Add(new TextBlock { Text = meta.ToString(), FontSize = 11, Opacity = 0.8, TextWrapping = TextWrapping.Wrap });

        // Completeness + page count
        var detail2 = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(w.Completeness))
            detail2.Append(w.Completeness);
        if (w.PageCount > 0)
        {
            if (detail2.Length > 0) detail2.Append(" | ");
            detail2.Append($"{w.PageCount} pages");
        }
        if (detail2.Length > 0)
            stack.Children.Add(new TextBlock { Text = detail2.ToString(), FontSize = 10, Opacity = 0.7 });

        // Upstream URL
        if (!string.IsNullOrWhiteSpace(w.UpstreamUrl))
        {
            var url = w.UpstreamUrl;
            var displayUrl = url.Length > 70 ? url[..67] + "..." : url;
            var linkBtn = MakeLinkButton(displayUrl, url, 10);
            stack.Children.Add(linkBtn);
        }

        // SHA-256 + bytes
        if (!string.IsNullOrWhiteSpace(w.CapturedSha256))
        {
            var sha = w.CapturedSha256.Length > 16 ? w.CapturedSha256[..16] + "..." : w.CapturedSha256;
            var detail = $"SHA-256: {sha}";
            if (w.CapturedBytes > 0) detail += $" | {FormatBytes(w.CapturedBytes)}";
            stack.Children.Add(new TextBlock
            {
                Text = detail, FontSize = 10,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"), Opacity = 0.6,
            });
        }

        // Capture date
        if (!string.IsNullOrWhiteSpace(w.CapturedUtc))
            stack.Children.Add(new TextBlock { Text = $"Captured: {w.CapturedUtc}", FontSize = 10, Opacity = 0.6 });

        // Vetting confidence badge
        if (!string.IsNullOrWhiteSpace(w.VettingConfidence))
        {
            var bg = w.VettingConfidence switch
            {
                "high" => Color.FromArgb(40, 0, 180, 0),
                "medium" => Color.FromArgb(40, 220, 180, 0),
                _ => Color.FromArgb(40, 220, 60, 0),
            };
            stack.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1),
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 0),
                Background = new SolidColorBrush(bg),
                Child = new TextBlock { Text = $"vetting: {w.VettingConfidence}", FontSize = 10, FontWeight = FontWeight.SemiBold },
            });
        }

        // Enrichment from witnesses.json (delivery registry)
        if (enrichment != null)
        {
            var sep = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)),
                Margin = new Thickness(0, 4, 0, 4),
            };
            stack.Children.Add(sep);

            var deliveryPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            deliveryPanel.Children.Add(new TextBlock
            {
                Text = "Delivery:",
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Opacity = 0.8,
                VerticalAlignment = VerticalAlignment.Center,
            });
            if (!string.IsNullOrWhiteSpace(enrichment.Siglum))
            {
                deliveryPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 140, 0, 200)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(5, 1),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = enrichment.Siglum, FontSize = 10, FontWeight = FontWeight.Bold },
                });
            }
            deliveryPanel.Children.Add(new TextBlock
            {
                Text = enrichment.StatusDisplay,
                FontSize = 10, Opacity = 0.85,
                VerticalAlignment = VerticalAlignment.Center,
            });
            stack.Children.Add(deliveryPanel);

            var bits = new List<string>();
            if (!string.IsNullOrWhiteSpace(enrichment.AlignmentMode))
                bits.Add($"alignment: {enrichment.AlignmentMode}");
            if (!string.IsNullOrWhiteSpace(enrichment.Completeness))
                bits.Add(enrichment.Completeness);
            if (!string.IsNullOrWhiteSpace(enrichment.Confidence))
                bits.Add($"confidence: {enrichment.Confidence}");
            if (enrichment.HasLocusMap) bits.Add("locus-map ready");
            if (bits.Count > 0)
                stack.Children.Add(new TextBlock
                {
                    Text = string.Join(" | ", bits),
                    FontSize = 10, Opacity = 0.7,
                });
        }

        return new Border
        {
            Padding = new Thickness(10), CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 4),
            Child = stack,
        };
    }

    // ── Process tab ──────────────────────────────────────────────────────

    private void PopulateProcess(ProcessInfo? process, ManifestInfo manifest)
    {
        var host = this.FindControl<StackPanel>("ProcessHost");
        if (host == null) return;

        if (process == null)
        {
            host.Children.Add(MakeEmptyState("No process.json available for this text."));

            // Still show basic production info from manifest
            if (!string.IsNullOrWhiteSpace(manifest.ProductionMethod))
            {
                host.Children.Add(MakeSection("Production method"));
                host.Children.Add(new TextBlock { Text = manifest.ProductionMethod, FontSize = 11, TextWrapping = TextWrapping.Wrap, Opacity = 0.85 });
            }
            if (!string.IsNullOrWhiteSpace(manifest.Curator))
                host.Children.Add(new TextBlock { Text = $"Curator: {manifest.Curator}", FontSize = 11, Opacity = 0.7, Margin = new Thickness(0, 4, 0, 0) });
            return;
        }

        // Project info
        if (process.Project != null)
        {
            host.Children.Add(MakeSection("Project"));
            var p = process.Project;
            if (!string.IsNullOrWhiteSpace(p.Name)) host.Children.Add(MakeKV("Name", p.Name));
            if (!string.IsNullOrWhiteSpace(p.EditionKind)) host.Children.Add(MakeKV("Edition kind", p.EditionKind));
            if (!string.IsNullOrWhiteSpace(p.TargetMaturity)) host.Children.Add(MakeKV("Target maturity", p.TargetMaturity));
            if (!string.IsNullOrWhiteSpace(p.Curator)) host.Children.Add(MakeKV("Curator", p.Curator));
            if (!string.IsNullOrWhiteSpace(p.StartDate)) host.Children.Add(MakeKV("Start date", p.StartDate));
        }

        // Base witness
        if (process.BaseWitness != null)
        {
            host.Children.Add(MakeSection("Base witness"));
            host.Children.Add(MakeKV("ID", process.BaseWitness.Id ?? ""));
            if (!string.IsNullOrWhiteSpace(process.BaseWitness.Label))
                host.Children.Add(MakeKV("Label", process.BaseWitness.Label));
            if (!string.IsNullOrWhiteSpace(process.BaseWitness.SelectionRationale))
                host.Children.Add(MakeKV("Rationale", process.BaseWitness.SelectionRationale));
        }

        // Witness families
        if (process.WitnessFamilies is { Count: > 0 })
        {
            host.Children.Add(MakeSection($"Witness families ({process.WitnessFamilies.Count})"));
            foreach (var f in process.WitnessFamilies)
            {
                host.Children.Add(MakeKV(f.FamilyName ?? f.FamilyId ?? "?",
                    f.Members != null ? string.Join(", ", f.Members) : ""));
            }
        }

        // OCR pipeline
        if (process.OcrPipeline != null)
        {
            host.Children.Add(MakeSection("OCR pipeline"));
            if (!string.IsNullOrWhiteSpace(process.OcrPipeline.DefaultEngine))
                host.Children.Add(MakeKV("Default engine", process.OcrPipeline.DefaultEngine));
            if (process.OcrPipeline.Engines is { Count: > 0 })
            {
                foreach (var eng in process.OcrPipeline.Engines)
                    host.Children.Add(MakeKV(eng.Name ?? "?", $"v{eng.Version ?? "?"} ({eng.RunDate ?? "?"})"));
            }
        }

        // Human passes
        if (process.HumanPasses is { Count: > 0 })
        {
            host.Children.Add(MakeSection($"Human passes ({process.HumanPasses.Count})"));
            foreach (var hp in process.HumanPasses)
                host.Children.Add(MakeKV(hp.ChangeType ?? "pass", $"{hp.PagesOrLoci ?? "?"} — {hp.Reason ?? ""}"));
        }

        // Coverage
        if (process.Coverage != null)
        {
            host.Children.Add(MakeSection("Coverage"));
            var c = process.Coverage;
            if (c.TotalPages > 0) host.Children.Add(MakeKV("Total pages", c.TotalPages.ToString()!));
            if (c.OcrPages > 0) host.Children.Add(MakeKV("OCR'd", c.OcrPages.ToString()!));
            if (c.HumanCheckedPages > 0) host.Children.Add(MakeKV("Human-checked", c.HumanCheckedPages.ToString()!));
            if (c.PercentComplete > 0) host.Children.Add(MakeKV("Complete", $"{c.PercentComplete:F0}%"));
        }

        // Publication checks
        if (process.PublicationChecks != null)
        {
            host.Children.Add(MakeSection("Publication checks"));
            var pc = process.PublicationChecks;
            AddCheck(host, "Witness rights confirmed", pc.AllWitnessRightsConfirmed);
            AddCheck(host, "Hashes valid", pc.AllHashesValid);
            AddCheck(host, "Segmentation complete", pc.SegmentationComplete);
            AddCheck(host, "OCR recorded", pc.OcrRecorded);
            AddCheck(host, "OCR benchmark exists", pc.OcrBenchmarkExists);
            AddCheck(host, "Human passes logged", pc.HumanPassesLogged);
            AddCheck(host, "Apparatus exists", pc.ApparatusExists);
            AddCheck(host, "Unresolved classified", pc.UnresolvedClassified);
            AddCheck(host, "TEI validates", pc.TeiValidates);
            AddCheck(host, "All artifacts validate", pc.AllArtifactsValidate);
        }

        // Unresolved loci
        if (process.UnresolvedLoci is { Count: > 0 })
        {
            host.Children.Add(MakeSection($"Unresolved loci ({process.UnresolvedLoci.Count})"));
            foreach (var u in process.UnresolvedLoci)
                host.Children.Add(MakeKV(u.LocusId ?? "?",
                    $"{u.Reason ?? ""} [{u.PublicationStatus ?? "?"}]"));
        }
    }

    // ── Apparatus tab ────────────────────────────────────────────────────

    private void PopulateApparatus(ApparatusInfo? apparatus)
    {
        var host = this.FindControl<StackPanel>("ApparatusHost");
        if (host == null) return;

        if (apparatus?.Entries == null || apparatus.Entries.Count == 0)
        {
            host.Children.Add(MakeEmptyState("No apparatus entries available."));
            // If no apparatus but witnesses are delivered, still expose witness-level access
            if (_witnessRegistry?.Witnesses is { Count: > 0 } witnesses)
            {
                host.Children.Add(MakeSection($"Delivered witnesses ({witnesses.Count})"));
                host.Children.Add(new TextBlock
                {
                    Text = "This edition has witness texts but no apparatus entries yet. " +
                           "Open any witness's full text below.",
                    FontSize = 11, Opacity = 0.7, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8),
                });
                foreach (var w in witnesses)
                    host.Children.Add(BuildWitnessOpenButton(w));
            }
            return;
        }

        host.Children.Add(MakeSection($"Apparatus ({apparatus.Entries.Count} entries)"));

        foreach (var entry in apparatus.Entries)
        {
            var card = new StackPanel { Spacing = 2 };

            // Locus + status + Compare button (right-aligned)
            var headerDock = new DockPanel();
            var headerText = $"{entry.LocusId ?? "?"} [{entry.Status ?? "?"}]";
            if (!string.IsNullOrWhiteSpace(entry.Section)) headerText = $"{entry.Section} / {headerText}";
            headerDock.Children.Add(new TextBlock
            {
                Text = headerText, FontWeight = FontWeight.SemiBold, FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
            });

            // Compare witnesses button — only when registry is loaded
            if (_witnessRegistry?.Witnesses is { Count: > 0 } && !string.IsNullOrWhiteSpace(entry.LocusId))
            {
                var compareBtn = new Button
                {
                    Content = "Compare witnesses",
                    FontSize = 10, Padding = new Thickness(6, 2),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Tag = entry.LocusId,
                };
                DockPanel.SetDock(compareBtn, Dock.Right);
                compareBtn.Click += (_, _) => OpenWitnessComparisonForLocus(entry);
                headerDock.Children.Insert(0, compareBtn);
            }

            card.Children.Add(headerDock);

            // Lemma
            if (!string.IsNullOrWhiteSpace(entry.Lemma))
                card.Children.Add(MakeKV("Lemma", entry.Lemma));

            // Readings
            if (entry.Readings is { Count: > 0 })
            {
                foreach (var r in entry.Readings)
                {
                    var rText = $"{r.WitnessId}: \"{r.Reading}\" [{r.Certainty ?? "?"}]";
                    if (r.IsOcrOnly == true) rText += " (OCR only)";
                    if (r.IsHumanChecked == true) rText += " \u2713";
                    card.Children.Add(new TextBlock { Text = rText, FontSize = 10, Opacity = 0.8, Margin = new Thickness(12, 0, 0, 0) });
                }
            }

            // Decision
            if (!string.IsNullOrWhiteSpace(entry.Decision))
                card.Children.Add(MakeKV("Decision", entry.Decision));
            if (!string.IsNullOrWhiteSpace(entry.DecisionBasis))
                card.Children.Add(new TextBlock { Text = entry.DecisionBasis, FontSize = 10, Opacity = 0.7, FontStyle = FontStyle.Italic, TextWrapping = TextWrapping.Wrap });

            host.Children.Add(new Border
            {
                Child = card, Padding = new Thickness(10), CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 4),
            });
        }
    }

    // ── Collation tab ────────────────────────────────────────────────────

    /// <summary>
    /// Populates the collation table grid and stemma visualization.
    /// The table shows loci as rows and witnesses as columns, with color-coded
    /// cells indicating agreement (green), divergence (amber), or absence (red).
    /// </summary>
    private void PopulateCollation(ApparatusInfo? apparatus, WitnessTextRegistry? witnessRegistry)
    {
        var collationGrid = this.FindControl<Grid>("CollationGrid");
        var txtInfo = this.FindControl<TextBlock>("TxtCollationInfo");

        if (collationGrid == null) return;

        if (apparatus?.Entries is not { Count: > 0 })
        {
            if (txtInfo != null) txtInfo.Text = "No apparatus data for collation.";
            // Still try stemma
            PopulateStemma(witnessRegistry);
            return;
        }

        // Collect unique witness IDs from apparatus entries
        var witnessIds = new List<string>();
        var witnessIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in apparatus.Entries)
        {
            if (entry.Readings == null) continue;
            foreach (var r in entry.Readings)
            {
                if (!string.IsNullOrWhiteSpace(r.WitnessId) && witnessIdSet.Add(r.WitnessId))
                    witnessIds.Add(r.WitnessId);
            }
        }

        if (witnessIds.Count == 0)
        {
            if (txtInfo != null) txtInfo.Text = "No witness readings in apparatus.";
            PopulateStemma(witnessRegistry);
            return;
        }

        // Build witness siglum lookup
        var sigla = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (witnessRegistry?.Witnesses != null)
        {
            foreach (var w in witnessRegistry.Witnesses)
            {
                if (!string.IsNullOrWhiteSpace(w.WitnessId) && !string.IsNullOrWhiteSpace(w.Siglum))
                    sigla[w.WitnessId] = w.Siglum;
            }
        }

        int lociCount = apparatus.Entries.Count;
        int witnessCount = witnessIds.Count;
        if (txtInfo != null) txtInfo.Text = $"{lociCount} loci \u00d7 {witnessCount} witnesses";

        // Define columns: Locus + one per witness
        collationGrid.ColumnDefinitions.Clear();
        collationGrid.ColumnDefinitions.Add(new ColumnDefinition(120, GridUnitType.Pixel)); // locus
        for (int w = 0; w < witnessCount; w++)
            collationGrid.ColumnDefinitions.Add(new ColumnDefinition(100, GridUnitType.Pixel));

        // Define rows: header + one per locus
        collationGrid.RowDefinitions.Clear();
        collationGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // header
        for (int r = 0; r < lociCount; r++)
            collationGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // Header row
        var locusHeader = MakeCollationCell("Locus", isHeader: true);
        Grid.SetRow(locusHeader, 0);
        Grid.SetColumn(locusHeader, 0);
        collationGrid.Children.Add(locusHeader);

        for (int w = 0; w < witnessCount; w++)
        {
            var siglum = sigla.TryGetValue(witnessIds[w], out var s) ? s : witnessIds[w];
            var header = MakeCollationCell(siglum, isHeader: true);
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, w + 1);
            collationGrid.Children.Add(header);
        }

        // Data rows
        for (int r = 0; r < lociCount; r++)
        {
            var entry = apparatus.Entries[r];
            int row = r + 1;

            // Locus label
            var locusLabel = entry.LocusId ?? $"#{r + 1}";
            if (!string.IsNullOrWhiteSpace(entry.Section))
                locusLabel = $"{entry.Section}/{locusLabel}";
            var locusCell = MakeCollationCell(locusLabel, isHeader: false, bg: null);
            Grid.SetRow(locusCell, row);
            Grid.SetColumn(locusCell, 0);
            collationGrid.Children.Add(locusCell);

            // Build reading lookup for this entry
            var readingByWitness = new Dictionary<string, ApparatusReading>(StringComparer.OrdinalIgnoreCase);
            if (entry.Readings != null)
            {
                foreach (var rd in entry.Readings)
                {
                    if (!string.IsNullOrWhiteSpace(rd.WitnessId))
                        readingByWitness.TryAdd(rd.WitnessId, rd);
                }
            }

            for (int w = 0; w < witnessCount; w++)
            {
                string cellText;
                IBrush cellBg;

                if (readingByWitness.TryGetValue(witnessIds[w], out var reading))
                {
                    cellText = reading.Reading ?? "\u2014";
                    bool matchesLemma = !string.IsNullOrWhiteSpace(entry.Lemma) &&
                        string.Equals(reading.Reading?.Trim(), entry.Lemma.Trim(), StringComparison.Ordinal);

                    if (matchesLemma)
                        cellBg = new SolidColorBrush(Color.FromArgb(50, 76, 175, 80));   // green
                    else if (string.IsNullOrWhiteSpace(reading.Reading) || reading.Reading == "\u2014")
                        cellBg = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54));    // red (lacuna)
                    else
                        cellBg = new SolidColorBrush(Color.FromArgb(50, 255, 152, 0));    // amber (variant)
                }
                else
                {
                    cellText = "\u2014";
                    cellBg = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54)); // red (absent)
                }

                var cell = MakeCollationCell(cellText, isHeader: false, bg: cellBg);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, w + 1);
                collationGrid.Children.Add(cell);
            }
        }

        // Also populate stemma
        PopulateStemma(witnessRegistry);
    }

    /// <summary>Creates a styled cell for the collation grid.</summary>
    private static Border MakeCollationCell(string text, bool isHeader, IBrush? bg = null)
    {
        return new Border
        {
            Background = bg ?? Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(4, 2),
            Child = new TextBlock
            {
                Text = text,
                FontSize = isHeader ? 11 : 10,
                FontWeight = isHeader ? FontWeight.SemiBold : FontWeight.Normal,
                Opacity = isHeader ? 0.9 : 0.85,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }

    /// <summary>
    /// Loads and displays the witness stemma in the Stemma sub-tab.
    /// Tries family-stemma.md first, falls back to registry-based generation.
    /// </summary>
    private void PopulateStemma(WitnessTextRegistry? witnessRegistry)
    {
        var stemmaHost = this.FindControl<Border>("StemmaHost");
        var tabStemma = this.FindControl<TabItem>("TabStemma");
        var txtStemmaInfo = this.FindControl<TextBlock>("TxtStemmaInfo");

        if (stemmaHost == null) return;

        // Try to load stemma from provenance directory
        StemmaParserService.StemmaData? stemma = null;

        if (_editionDir != null)
        {
            var slug = Path.GetFileName(_editionDir);
            var provRoot = Path.GetFullPath(Path.Combine(_editionDir, "..", "..", "..", "provenance", slug));
            if (Directory.Exists(provRoot))
            {
                var stemmaPath = Path.Combine(provRoot, "collation", "family-stemma.md");
                stemma = StemmaParserService.TryParseFile(stemmaPath);
            }
        }

        // Fallback: generate from witness registry families
        stemma ??= StemmaParserService.GenerateFromRegistry(witnessRegistry);

        if (stemma == null || stemma.Edges.Count == 0)
        {
            if (tabStemma != null) tabStemma.IsVisible = false;
            return;
        }

        if (txtStemmaInfo != null)
            txtStemmaInfo.Text = $"{stemma.NodeNames.Count} witnesses, {stemma.Edges.Count} relationships";

        // Build the view model and create the control
        var vm = StemmaViewModel.Build(stemma, witnessRegistry);
        var webControl = new LineageWebControl
        {
            MinHeight = 400,
        };
        webControl.SetViewModel(vm);

        stemmaHost.Child = webControl;
    }

    /// <summary>
    /// Renders apparatus entries in traditional Leiden notation:
    /// lemma ] reading1 W1 W3 | reading2 W2 A1
    /// </summary>
    private void RenderLeidenApparatus(ApparatusInfo apparatus)
    {
        var host = this.FindControl<StackPanel>("ApparatusHost");
        if (host == null || apparatus.Entries == null) return;

        host.Children.Add(MakeSection($"Apparatus – Leiden notation ({apparatus.Entries.Count} entries)"));

        var siglaBrush = new SolidColorBrush(Color.FromRgb(100, 140, 220));

        foreach (var entry in apparatus.Entries)
        {
            if (entry.Readings is not { Count: > 0 }) continue;

            var tb = new SelectableTextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 2) };

            // Lemma in bold
            var lemmaText = entry.Lemma ?? "?";
            tb.Inlines!.Add(new Avalonia.Controls.Documents.Run(lemmaText)
            {
                FontWeight = FontWeight.Bold,
            });

            // Separator
            tb.Inlines.Add(new Avalonia.Controls.Documents.Run(" ] "));

            // Group readings by identical text, collecting witness sigla
            var groups = entry.Readings
                .GroupBy(r => r.Reading ?? "")
                .ToList();

            for (int gi = 0; gi < groups.Count; gi++)
            {
                if (gi > 0)
                    tb.Inlines.Add(new Avalonia.Controls.Documents.Run(" | "));

                var g = groups[gi];
                var readingText = g.Key;

                // Check for type prefix (from any reading in the group)
                var firstReading = g.First();
                var typePrefix = firstReading.Type switch
                {
                    "om" => "om. ",
                    "add" => "add. ",
                    _ => !string.IsNullOrWhiteSpace(firstReading.Type) ? firstReading.Type + " " : "",
                };

                if (!string.IsNullOrEmpty(typePrefix))
                {
                    tb.Inlines.Add(new Avalonia.Controls.Documents.Run(typePrefix)
                    {
                        FontStyle = FontStyle.Italic,
                    });
                }

                // Reading text
                tb.Inlines.Add(new Avalonia.Controls.Documents.Run(readingText));

                // Witness sigla in accent color
                var sigla = string.Join(" ", g.Select(r => r.WitnessId ?? "?"));
                tb.Inlines.Add(new Avalonia.Controls.Documents.Run(" " + sigla)
                {
                    Foreground = siglaBrush,
                    FontSize = 10,
                });

                // Editor attribution if present
                var editor = g.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Editor))?.Editor;
                if (!string.IsNullOrEmpty(editor))
                {
                    tb.Inlines.Add(new Avalonia.Controls.Documents.Run($" ({editor})")
                    {
                        FontStyle = FontStyle.Italic,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(180, 160, 160, 160)),
                    });
                }
            }

            // Section prefix if present
            var prefix = !string.IsNullOrWhiteSpace(entry.Section)
                ? $"{entry.Section} / {entry.LocusId ?? "?"}: "
                : $"{entry.LocusId ?? "?"}: ";

            var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            line.Children.Add(new TextBlock { Text = prefix, FontSize = 10, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center });
            line.Children.Add(tb);

            host.Children.Add(new Border
            {
                Child = line, Padding = new Thickness(8, 4), CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 2),
            });
        }
    }

    private Border BuildWitnessOpenButton(WitnessTextEntry witness)
    {
        var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        stack.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 140, 0, 200)),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(5, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = witness.Siglum ?? witness.WitnessId ?? "?", FontSize = 10, FontWeight = FontWeight.Bold },
        });
        stack.Children.Add(new TextBlock
        {
            Text = witness.Label ?? witness.WitnessId ?? "(unlabeled)",
            FontSize = 11, VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        var btn = new Button
        {
            Content = "Open full text",
            FontSize = 10, Padding = new Thickness(6, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        btn.Click += (_, _) => OpenWitnessFullText(witness);
        DockPanel.SetDock(btn, Dock.Right);

        var dock = new DockPanel();
        dock.Children.Add(btn);
        dock.Children.Add(stack);

        return new Border
        {
            Padding = new Thickness(10, 6), CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 4),
            Child = dock,
        };
    }

    private void OpenWitnessComparisonForLocus(ApparatusEntry entry)
    {
        if (_witnessRegistry == null || string.IsNullOrWhiteSpace(entry.LocusId)) return;

        var groups = WitnessTextService.GetComparisonAtLocus(
            _witnessRegistry, _apparatus, entry.LocusId, entry.Lemma);

        if (groups == null || groups.Count == 0)
        {
            // No data for this locus yet — open viewer with empty state
            ShowComparisonInPopup(entry.LocusId, entry.Lemma ?? "", new List<WitnessReadingGroup>(), entry);
            return;
        }

        ShowComparisonInPopup(entry.LocusId, entry.Lemma ?? "", groups, entry);
    }

    private void ShowComparisonInPopup(string locusId, string lemma, List<WitnessReadingGroup> groups, ApparatusEntry? entry)
    {
        // Open in a small modal-style window so the user can copy + close without leaving the dialog
        var win = new Window
        {
            Title = $"Witness Comparison · {locusId}",
            Width = 640,
            Height = 540,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Application.Current?.Resources["AppBg"] as IBrush,
        };

        var panel = new WitnessComparisonPanel();
        panel.SetComparison(locusId, lemma, groups);
        // Wire the per-witness "open full text" event
        panel.OpenWitnessFullTextRequested += (_, w) => OpenWitnessFullText(w);
        panel.OpenWitnessPageImageRequested += (_, args) => OpenWitnessPageImage(args.WitnessId, args.Locus);

        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        Grid.SetRow(panel, 0);
        root.Children.Add(panel);

        var closeBtn = new Button
        {
            Content = "Close", MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 12, 8),
        };
        closeBtn.Click += (_, _) => win.Close();
        Grid.SetRow(closeBtn, 1);
        root.Children.Add(closeBtn);

        win.Content = root;
        win.Show(this);
    }

    private void OpenWitnessFullText(WitnessTextEntry witness)
    {
        if (string.IsNullOrEmpty(_editionDir)) return;
        var viewer = new WitnessTextViewerWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        viewer.LoadWitness(witness, _editionDir);
        viewer.Show(this);
    }

    private void OpenWitnessPageImage(string siglum, string locus)
    {
        if (string.IsNullOrEmpty(_editionDir)) return;

        var ocrBaseDir = System.IO.Path.Combine(_editionDir, "ocr");
        var pagePath = PdfEvidenceService.ResolvePageImagePath(ocrBaseDir, siglum, locus);

        if (pagePath == null)
        {
            // Fallback: show message in a simple dialog
            var msgWin = new Window
            {
                Title = "Page Image Not Found",
                Width = 400, Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = $"No page image found for witness {siglum} at locus {locus}.\n" +
                           $"Expected in: {ocrBaseDir}/{siglum}/page-images/",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(16),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            msgWin.Show(this);
            return;
        }

        var pdfService = new PdfEvidenceService();
        var viewer = new PdfEvidenceWindow(pdfService)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        viewer.LoadPageImageEvidence(pagePath, siglum);

        // Load OCR readings for the side panel
        var ocrPageId = PdfEvidenceWindow.ExtractPageIdFromLocus(locus);
        if (ocrPageId != null)
            viewer.LoadOcrReadings(ocrBaseDir, siglum, ocrPageId);

        // Set up witness selector with all available sigla from ocr directory
        var sigla = new System.Collections.Generic.List<string>();
        try
        {
            if (System.IO.Directory.Exists(ocrBaseDir))
            {
                foreach (var dir in System.IO.Directory.GetDirectories(ocrBaseDir))
                {
                    var dirName = System.IO.Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(dirName))
                        sigla.Add(dirName);
                }
            }
        }
        catch { /* best-effort */ }

        if (sigla.Count > 1)
            viewer.SetWitnessSelector(sigla, siglum, locus, ocrBaseDir);

        viewer.Show(this);
    }

    // ── Stats tab ────────────────────────────────────────────────────────

    private void PopulateStats(EditionStatsInfo? stats)
    {
        var host = this.FindControl<StackPanel>("StatsHost");
        if (host == null) return;

        if (stats == null)
        {
            host.Children.Add(MakeEmptyState("No stats.json available for this text."));
            return;
        }

        host.Children.Add(MakeSection("Edition statistics"));

        if (stats.WitnessCount > 0) host.Children.Add(MakeKV("Witnesses", stats.WitnessCount.ToString()!));
        if (stats.WitnessFamilyCount > 0) host.Children.Add(MakeKV("Witness families", stats.WitnessFamilyCount.ToString()!));
        if (stats.PageCount > 0) host.Children.Add(MakeKV("Pages", stats.PageCount.ToString()!));
        if (stats.LeafCount > 0) host.Children.Add(MakeKV("Leaves", stats.LeafCount.ToString()!));
        if (stats.OcrEngineCount > 0) host.Children.Add(MakeKV("OCR engines", stats.OcrEngineCount.ToString()!));
        if (stats.ApparatusEntryCount > 0) host.Children.Add(MakeKV("Apparatus entries", stats.ApparatusEntryCount.ToString()!));
        if (stats.UnresolvedCount > 0) host.Children.Add(MakeKV("Unresolved loci", stats.UnresolvedCount.ToString()!));

        // Resolution ratios
        if (stats.PercentMachineResolved > 0 || stats.PercentHumanIntervention > 0)
        {
            host.Children.Add(MakeSection("Resolution"));
            if (stats.PercentMachineResolved > 0)
                host.Children.Add(MakeKV("Machine-resolved", $"{stats.PercentMachineResolved:F1}%"));
            if (stats.PercentHumanIntervention > 0)
                host.Children.Add(MakeKV("Human intervention", $"{stats.PercentHumanIntervention:F1}%"));
        }

        // Confidence distribution
        if (stats.BaseTextConfidence != null)
        {
            host.Children.Add(MakeSection("Base text confidence"));
            host.Children.Add(MakeKV("High", stats.BaseTextConfidence.High?.ToString() ?? "0"));
            host.Children.Add(MakeKV("Medium", stats.BaseTextConfidence.Medium?.ToString() ?? "0"));
            host.Children.Add(MakeKV("Low", stats.BaseTextConfidence.Low?.ToString() ?? "0"));
        }

        if (!string.IsNullOrWhiteSpace(stats.GeneratedUtc))
            host.Children.Add(new TextBlock { Text = $"Generated: {stats.GeneratedUtc}", FontSize = 10, Opacity = 0.6, Margin = new Thickness(0, 8, 0, 0) });

        // Drift statistics — only when correction data is available
        var driftSection = BuildDriftStatsSection();
        if (driftSection != null)
            host.Children.Add(driftSection);
    }

    // ── Drift stats (embedded in Stats tab) ────────────────────────────

    /// <summary>
    /// Builds a bordered card showing translation drift statistics.
    /// Returns null when correction or translation data is unavailable.
    /// </summary>
    private Border? BuildDriftStatsSection()
    {
        if (_corrections == null || _corrections.Count == 0 ||
            _workingTextLines == null || _workingTextLines.Count == 0 ||
            _finalRenderedDoc == null || _finalText == null)
            return null;

        // Build a translations dictionary keyed by segment key (locus)
        var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seg in _finalRenderedDoc.Segments)
        {
            if (seg.Start >= 0 && seg.EndExclusive <= _finalText.Length && seg.EndExclusive > seg.Start)
            {
                var text = _finalText.Substring(seg.Start, seg.EndExclusive - seg.Start).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    translations[seg.Key] = text;
            }
        }

        if (translations.Count == 0) return null;

        var report = TranslationDriftService.ComputeDrift(
            _corrections, _workingTextLines, translations);

        if (report.TranslatedSegments == 0) return null;

        var card = new StackPanel { Spacing = 4 };

        card.Children.Add(new TextBlock
        {
            Text = "Translation Drift",
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4),
        });

        // Progress bar: green (current) + orange (stale)
        double currentPct = report.CurrentPercent;
        double stalePct = report.TranslatedSegments > 0
            ? report.StaleSegments * 100.0 / report.TranslatedSegments
            : 0;

        var barContainer = new Border
        {
            Height = 16,
            CornerRadius = new CornerRadius(4),
            ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Margin = new Thickness(0, 2, 0, 4),
        };

        var barStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        // We use a Grid to make the bars proportional to parent width
        var barGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        barGrid.ColumnDefinitions.Add(new ColumnDefinition(currentPct, GridUnitType.Star));
        if (stalePct > 0)
            barGrid.ColumnDefinitions.Add(new ColumnDefinition(stalePct, GridUnitType.Star));
        double remainPct = 100.0 - currentPct - stalePct;
        if (remainPct > 0.1)
            barGrid.ColumnDefinitions.Add(new ColumnDefinition(remainPct, GridUnitType.Star));

        var greenBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            CornerRadius = new CornerRadius(4, 0, 0, 4),
        };
        Grid.SetColumn(greenBar, 0);
        barGrid.Children.Add(greenBar);

        if (stalePct > 0)
        {
            var orangeBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            };
            Grid.SetColumn(orangeBar, 1);
            barGrid.Children.Add(orangeBar);
        }

        barContainer.Child = barGrid;
        card.Children.Add(barContainer);

        // Summary text
        var summaryParts = new List<string>
        {
            $"{currentPct:F0}% current",
            $"{stalePct:F0}% stale",
        };
        if (report.UntranslatedSegments > 0)
            summaryParts.Add($"{report.UntranslatedSegments} untranslated");
        else
            summaryParts.Add("0 untranslated");

        card.Children.Add(new TextBlock
        {
            Text = string.Join(" \u00b7 ", summaryParts),
            FontSize = 11,
            Opacity = 0.85,
        });

        // Counts detail
        card.Children.Add(new TextBlock
        {
            Text = $"{report.CurrentSegments} current / {report.StaleSegments} stale / {report.TranslatedSegments} translated of {report.TotalSegments} total",
            FontSize = 10,
            Opacity = 0.6,
            Margin = new Thickness(0, 2, 0, 0),
        });

        // Collapsible stale entries
        if (report.Drifts.Count > 0)
        {
            var driftList = new StackPanel { Spacing = 4 };
            foreach (var d in report.Drifts)
            {
                var entryPanel = new StackPanel { Spacing = 1 };
                entryPanel.Children.Add(new TextBlock
                {
                    Text = $"{d.Locus}  (step {d.CorrectionStep})",
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Opacity = 0.9,
                });
                entryPanel.Children.Add(new TextBlock
                {
                    Text = d.DiffSummary,
                    FontSize = 10,
                    Opacity = 0.75,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                });
                driftList.Children.Add(entryPanel);
            }

            var expander = new Expander
            {
                Header = $"Stale segments ({report.Drifts.Count})",
                Content = driftList,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0),
            };
            card.Children.Add(expander);
        }

        return new Border
        {
            Background = Application.Current?.Resources["CardBackgroundBrush"] as IBrush
                ?? new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 10, 0, 0),
            Child = card,
        };
    }

    // ── Documents tab ────────────────────────────────────────────────────

    private void PopulateDocuments(DocumentsInfo? documents, string? xmlAbsPath, string? textId)
    {
        var host = this.FindControl<StackPanel>("DocumentsHost");
        if (host == null) return;

        if (documents?.Documents == null || documents.Documents.Count == 0)
        {
            // Fall back to autodiscovery for texts that don't have documents.json yet
            var discovered = DiscoverDocuments(xmlAbsPath, textId);
            if (discovered.Count == 0)
            {
                host.Children.Add(MakeEmptyState("No documents registered for this text."));
                return;
            }

            host.Children.Add(MakeSection($"Documents ({discovered.Count} discovered)"));
            foreach (var (name, path) in discovered)
                host.Children.Add(BuildDocumentExpander(name, path));
            return;
        }

        // Group by category
        var groups = documents.Documents
            .OrderBy(d => d.SortOrder ?? int.MaxValue)
            .ThenBy(d => d.Title ?? "")
            .GroupBy(d => d.Category ?? "general")
            .OrderBy(g => CategoryOrder(g.Key));

        foreach (var group in groups)
        {
            host.Children.Add(MakeSection(FormatCategory(group.Key)));

            var xmlDir = !string.IsNullOrWhiteSpace(xmlAbsPath) ? Path.GetDirectoryName(xmlAbsPath) : null;

            foreach (var doc in group)
            {
                var absPath = doc.Path;
                if (xmlDir != null && !Path.IsPathRooted(doc.Path ?? ""))
                {
                    // Try relative to the XML directory first
                    absPath = Path.GetFullPath(Path.Combine(xmlDir, doc.Path ?? ""));

                    // Fallback: try relative to the repo root (3 levels up from xml-open/{prefix}/{slug}/)
                    if (!File.Exists(absPath))
                    {
                        try
                        {
                            var repoRoot = Path.GetFullPath(Path.Combine(xmlDir, "..", "..", ".."));
                            var fromRoot = Path.GetFullPath(Path.Combine(repoRoot, doc.Path ?? ""));
                            if (File.Exists(fromRoot)) absPath = fromRoot;
                        }
                        catch { }
                    }
                }

                var title = doc.Title ?? Path.GetFileNameWithoutExtension(doc.Path ?? "");
                if (!string.IsNullOrWhiteSpace(doc.Description))
                    title += $" — {doc.Description}";

                host.Children.Add(BuildDocumentExpander(title, absPath ?? ""));
            }
        }
    }

    private static Expander BuildDocumentExpander(string displayName, string filePath)
    {
        string? content = null;
        try { content = File.ReadAllText(filePath); }
        catch { content = "(Could not read file)"; }

        var rendered = MarkdownRenderer.Render(content ?? "");

        var scroll = new ScrollViewer
        {
            Content = rendered,
            MaxHeight = 500,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        return new Expander
        {
            Header = displayName,
            Content = scroll,
            IsExpanded = false,
            FontSize = 11,
        };
    }

    /// <summary>
    /// Fallback document discovery for texts without documents.json.
    /// Scans provenance/{slug}/ and docs/curation/exemplars/{slug}/.
    /// </summary>
    private static List<(string Name, string Path)> DiscoverDocuments(string? xmlAbsPath, string? textId)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(xmlAbsPath) || string.IsNullOrWhiteSpace(textId))
            return result;

        try
        {
            var slug = textId;
            var dotIdx = textId.IndexOf('.');
            if (dotIdx > 0 && dotIdx < textId.Length - 1)
                slug = textId[(dotIdx + 1)..];

            var xmlDir = Path.GetDirectoryName(xmlAbsPath);
            if (xmlDir == null) return result;
            var repoRoot = Path.GetFullPath(Path.Combine(xmlDir, "..", "..", ".."));

            var provenanceDir = Path.Combine(repoRoot, "provenance", slug);
            if (Directory.Exists(provenanceDir))
            {
                foreach (var mdFile in Directory.EnumerateFiles(provenanceDir, "*.md"))
                {
                    var name = Path.GetFileNameWithoutExtension(mdFile).Replace('-', ' ').Replace('_', ' ');
                    if (name.Length > 0) name = char.ToUpperInvariant(name[0]) + name[1..];
                    result.Add((name, mdFile));
                }
            }

            var exemplarDir = Path.Combine(repoRoot, "docs", "curation", "exemplars", slug);
            if (Directory.Exists(exemplarDir))
            {
                foreach (var mdFile in Directory.EnumerateFiles(exemplarDir, "*.md"))
                {
                    var name = Path.GetFileNameWithoutExtension(mdFile).Replace('-', ' ').Replace('_', ' ');
                    if (name.Length > 0) name = char.ToUpperInvariant(name[0]) + name[1..];
                    result.Add((name, mdFile));
                }
            }
        }
        catch { }

        return result;
    }

    // ── Timeline tab ────────────────────────────────────────────────────

    private void PopulateTimeline(TimelineInfo? timeline)
    {
        // Set up text preview editor
        _editorPreview = this.FindControl<TextEditor>("EditorTimelinePreview");
        if (_editorPreview != null)
        {
            _editorPreview.IsReadOnly = true;
            _editorPreview.ShowLineNumbers = false;
            _editorPreview.WordWrap = true;

            if (!string.IsNullOrEmpty(_finalText))
                _editorPreview.Text = _finalText;

            // Install locus highlighter
            if (_editorPreview.TextArea?.TextView != null)
            {
                _locusHighlighter = new LocusHighlightRenderer(_editorPreview.TextArea.TextView);
                _editorPreview.TextArea.TextView.BackgroundRenderers.Add(_locusHighlighter);
            }
        }

        if (timeline?.Events == null || timeline.Events.Count == 0)
        {
            var host = this.FindControl<StackPanel>("TimelineDetailsHost");
            host?.Children.Add(MakeEmptyState("No timeline events available for this text."));
            return;
        }

        _timeline = timeline;
        _timelineEvents = timeline.Events;
        _filteredEvents = new List<TimelineEvent>(_timelineEvents);

        // Set up slider
        var slider = this.FindControl<Slider>("SliderTimeline");
        if (slider != null)
        {
            slider.Maximum = _filteredEvents.Count - 1;
            slider.Value = _filteredEvents.Count - 1; // start at latest
            slider.ValueChanged += (_, _) =>
            {
                _timelineIndex = (int)slider.Value;
                ShowTimelineEvent(_timelineIndex);
            };
        }

        // Prev/Next buttons
        var btnPrev = this.FindControl<Button>("BtnTimelinePrev");
        var btnNext = this.FindControl<Button>("BtnTimelineNext");
        if (btnPrev != null) btnPrev.Click += (_, _) =>
        {
            if (slider != null && slider.Value > 0) slider.Value--;
        };
        if (btnNext != null) btnNext.Click += (_, _) =>
        {
            if (slider != null && slider.Value < slider.Maximum) slider.Value++;
        };

        // Multi-step buttons
        void WireStep(string name, int delta)
        {
            var btn = this.FindControl<Button>(name);
            if (btn != null) btn.Click += (_, _) =>
            {
                if (slider == null) return;
                slider.Value = Math.Clamp(slider.Value + delta, 0, slider.Maximum);
            };
        }
        WireStep("BtnBack50", -50);
        WireStep("BtnBack10", -10);
        WireStep("BtnBack5", -5);
        WireStep("BtnFwd5", 5);
        WireStep("BtnFwd10", 10);
        WireStep("BtnFwd50", 50);

        // Stage jump combo
        var cmbStage = this.FindControl<ComboBox>("CmbTimelineStage");
        if (cmbStage != null)
        {
            var stages = TimelineService.GetStages(_timelineEvents);
            var items = new List<ComboBoxItem> { new() { Content = "(all stages)", Tag = (string?)null } };
            foreach (var s in stages)
                items.Add(new ComboBoxItem { Content = FormatStage(s), Tag = s });
            cmbStage.ItemsSource = items;
            cmbStage.SelectedIndex = 0;
            cmbStage.SelectionChanged += (_, _) =>
            {
                var selected = (cmbStage.SelectedItem as ComboBoxItem)?.Tag as string;
                ApplyTimelineFilter(selected);
            };
        }

        // Text changes only checkbox
        var chkText = this.FindControl<CheckBox>("ChkTextChangesOnly");
        if (chkText != null)
        {
            chkText.IsCheckedChanged += (_, _) =>
            {
                var stageTag = (this.FindControl<ComboBox>("CmbTimelineStage")?.SelectedItem as ComboBoxItem)?.Tag as string;
                ApplyTimelineFilter(stageTag);
            };
        }

        // Show the latest event
        ShowTimelineEvent(_filteredEvents.Count - 1);
    }

    private void ApplyTimelineFilter(string? stage)
    {
        var textOnly = this.FindControl<CheckBox>("ChkTextChangesOnly")?.IsChecked == true;
        _filteredEvents = TimelineService.Filter(_timelineEvents, stage: stage, textChangingOnly: textOnly);

        var slider = this.FindControl<Slider>("SliderTimeline");
        if (slider != null)
        {
            slider.Maximum = Math.Max(_filteredEvents.Count - 1, 0);
            slider.Value = slider.Maximum;
        }

        ShowTimelineEvent(_filteredEvents.Count > 0 ? _filteredEvents.Count - 1 : -1);
    }

    private void ShowTimelineEvent(int index)
    {
        var txtSummary = this.FindControl<TextBlock>("TxtEventSummary");
        var txtMeta = this.FindControl<TextBlock>("TxtEventMeta");
        var host = this.FindControl<StackPanel>("TimelineDetailsHost");

        if (index < 0 || index >= _filteredEvents.Count)
        {
            if (txtSummary != null) txtSummary.Text = "No events match the current filter.";
            if (txtMeta != null) txtMeta.Text = "";
            host?.Children.Clear();
            return;
        }

        var evt = _filteredEvents[index];

        if (txtSummary != null) txtSummary.Text = evt.Summary ?? "(no summary)";
        if (txtMeta != null)
            txtMeta.Text = $"#{evt.Sequence} \u2022 {evt.StageDisplay} \u2022 {evt.EventTypeDisplay} \u2022 {evt.ActorType}:{evt.ActorId} \u2022 {evt.Timestamp}";

        if (host == null) return;
        host.Children.Clear();

        // Apply reading patches to the text preview
        ApplyReadingPatches(evt);

        // Details
        if (!string.IsNullOrWhiteSpace(evt.Details))
        {
            host.Children.Add(MakeSection("Details"));
            host.Children.Add(new TextBlock { Text = evt.Details, FontSize = 11, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Opacity = 0.85 });
        }

        // State at this event
        var state = TimelineService.ReconstructState(_timelineEvents, evt.Sequence);
        host.Children.Add(MakeSection("State at this point"));
        host.Children.Add(MakeKV("Stage", state.CurrentStage ?? "?"));
        host.Children.Add(MakeKV("Accepted witnesses", state.AcceptedWitnesses.Count.ToString()));
        host.Children.Add(MakeKV("Rejected witnesses", state.RejectedWitnesses.Count.ToString()));
        if (state.CopyTextSelected != null) host.Children.Add(MakeKV("Copy text", state.CopyTextSelected));
        else if (state.CopyTextCandidate != null) host.Children.Add(MakeKV("Copy text candidate", state.CopyTextCandidate));
        host.Children.Add(MakeKV("Unresolved loci", state.UnresolvedLoci.Count.ToString()));
        host.Children.Add(MakeKV("OCR runs", $"{state.OcrRunsCompleted}/{state.OcrRunsStarted} completed"));
        host.Children.Add(MakeKV("Apparatus entries", state.ApparatusEntryCount.ToString()));
        if (state.EditionMaturity != null) host.Children.Add(MakeKV("Maturity", state.EditionMaturity));

        // Evidence links
        if (evt.EvidenceLinks is { Count: > 0 })
        {
            host.Children.Add(MakeSection("Evidence"));
            foreach (var link in evt.EvidenceLinks)
                host.Children.Add(MakeLinkButton(link, link, 10));
        }

        // Decision reference
        if (!string.IsNullOrWhiteSpace(evt.DecisionRef))
            host.Children.Add(MakeKV("Decision", evt.DecisionRef));

        // Note anchor
        if (!string.IsNullOrWhiteSpace(evt.NoteAnchorId))
            host.Children.Add(MakeKV("Note anchor", evt.NoteAnchorId));

        // Inputs/outputs
        if (evt.Inputs is { Count: > 0 })
        {
            host.Children.Add(MakeSection("Inputs"));
            foreach (var inp in evt.Inputs)
                host.Children.Add(new TextBlock { Text = inp, FontSize = 10, Opacity = 0.7 });
        }
        if (evt.Outputs is { Count: > 0 })
        {
            host.Children.Add(MakeSection("Outputs"));
            foreach (var outp in evt.Outputs)
                host.Children.Add(new TextBlock { Text = outp, FontSize = 10, Opacity = 0.7 });
        }
    }

    private void ApplyReadingPatches(TimelineEvent evt)
    {
        if (_editorPreview == null || _timeline == null || string.IsNullOrEmpty(_finalText)) return;

        // Get patches for this timeline position
        var patches = TimelineService.GetReadingPatchesAtPosition(_timeline, evt.Sequence);

        if (patches.Count == 0)
        {
            // At or past the final state — show the final text
            _editorPreview.Text = _finalText;
            _locusHighlighter?.Clear();
        }
        else
        {
            // Apply patches to the final text — replace only the FIRST occurrence
            // of each final reading to avoid clobbering identical text at other loci.
            var text = _finalText;
            foreach (var (locusId, reading) in patches)
            {
                if (_timeline.Readings?.TryGetValue(locusId, out var readings) == true && readings.Count > 0)
                {
                    var finalReading = readings[^1];
                    if (!string.IsNullOrEmpty(finalReading))
                    {
                        var idx = text.IndexOf(finalReading, StringComparison.Ordinal);
                        if (idx >= 0)
                            text = string.Concat(text.AsSpan(0, idx), reading, text.AsSpan(idx + finalReading.Length));
                    }
                }
            }
            _editorPreview.Text = text;
        }

        // Highlight the affected locus if this is a text_changed event
        _locusHighlighter?.Clear();
        if (evt.EventType == "text_changed" && evt.StateEffects != null)
        {
            if (evt.StateEffects.TryGetValue("locus_id", out var locusObj))
            {
                var locusId = locusObj?.ToString();
                if (!string.IsNullOrEmpty(locusId))
                {
                    // Find the reading at this position for highlighting
                    var readingAtPos = TimelineService.GetReadingAtPosition(_timeline, locusId, evt.Sequence);
                    if (!string.IsNullOrEmpty(readingAtPos))
                    {
                        var idx = _editorPreview.Text?.IndexOf(readingAtPos, StringComparison.Ordinal) ?? -1;
                        if (idx >= 0)
                        {
                            _locusHighlighter?.SetHighlight(idx, readingAtPos.Length);

                            // Scroll to the highlighted locus
                            try
                            {
                                var line = _editorPreview.Document?.GetLineByOffset(idx);
                                if (line != null) _editorPreview.ScrollToLine(line.LineNumber);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
    }

    private static string FormatStage(string s) => s.Replace('_', ' ');

    // ── Log tab ──────────────────────────────────────────────────────

    private void PopulateLog(string? humanLogMarkdown)
    {
        var host = this.FindControl<StackPanel>("LogHost");
        if (host == null) return;

        if (string.IsNullOrWhiteSpace(humanLogMarkdown))
        {
            host.Children.Add(MakeEmptyState("No human-readable log available for this text."));
            return;
        }

        host.Children.Add(MakeSection("Edition Build Log"));
        var rendered = MarkdownRenderer.Render(humanLogMarkdown);
        host.Children.Add(rendered);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static TextBlock MakeSection(string title) => new()
    {
        Text = title,
        FontWeight = FontWeight.Bold,
        FontSize = 12,
        Margin = new Thickness(0, 8, 0, 2),
    };

    private static StackPanel MakeKV(string key, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = key + ":", FontSize = 11, FontWeight = FontWeight.SemiBold, Opacity = 0.9 });
        panel.Children.Add(new TextBlock { Text = value, FontSize = 11, Opacity = 0.8, TextWrapping = TextWrapping.Wrap });
        return panel;
    }

    private static void AddCheck(StackPanel host, string label, bool? value)
    {
        var icon = value == true ? "\u2713" : value == false ? "\u2717" : "?";
        var color = value == true ? Color.FromRgb(0, 180, 0) : value == false ? Color.FromRgb(220, 60, 0) : Color.FromRgb(160, 160, 160);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = icon, FontSize = 11, Foreground = new SolidColorBrush(color), FontWeight = FontWeight.Bold });
        panel.Children.Add(new TextBlock { Text = label, FontSize = 11, Opacity = 0.85 });
        host.Children.Add(panel);
    }

    private static TextBlock MakeEmptyState(string text) => new()
    {
        Text = text, FontSize = 11, Opacity = 0.5, FontStyle = FontStyle.Italic,
        Margin = new Thickness(0, 16),
    };

    private static Button MakeLinkButton(string displayText, string url, double fontSize)
    {
        var btn = new Button
        {
            Content = displayText, FontSize = fontSize,
            Padding = new Thickness(0), Background = Brushes.Transparent,
            BorderThickness = new Thickness(0), Cursor = new Cursor(StandardCursorType.Hand),
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 160, 255)),
        };
        ToolTip.SetTip(btn, url);
        btn.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        };
        return btn;
    }

    private static string FormatEditionKind(string kind) => kind switch
    {
        "transcription" => "Transcription",
        "critical_edition" => "Critical Edition",
        "scan_ocr" => "Scan + OCR",
        "derived" => "Derived",
        _ => kind,
    };

    private static string FormatMaturity(string m) => m switch
    {
        "draft" => "Draft",
        "review" => "Under Review",
        "publication-candidate" => "Publication Candidate",
        "published" => "Published",
        _ => m,
    };

    private static string FormatWitnessKind(string kind) => kind switch
    {
        "wiki_transcription" => "Wiki transcription",
        "woodblock_scan" => "Woodblock scan",
        "printed_edition" => "Printed edition",
        "manuscript" => "Manuscript",
        "other" => "Other",
        _ => kind,
    };

    private static string FormatCategory(string cat) => cat switch
    {
        "process" => "Process",
        "apparatus" => "Apparatus",
        "witness" => "Witness Documentation",
        "editorial" => "Editorial Notes",
        "general" => "General",
        _ => cat,
    };

    private static int CategoryOrder(string cat) => cat switch
    {
        "general" => 0,
        "witness" => 1,
        "process" => 2,
        "apparatus" => 3,
        "editorial" => 4,
        _ => 5,
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    // ── Corrections tab (CE time-travel) ─────────────────────────────────

    private List<CorrectionEntry>? _corrections;
    private List<(string Locus, string Text)>? _workingTextLines;
    private CorrectionEntry? _currentEvidenceCorrection;

    // Forensic provenance logs (parsed from provenance/{slug}/process/)
    private List<OcrConsensusEntry>? _ocrConsensus;
    private List<RejectedReadingEntry>? _rejectedReadings;
    private List<TranslationReasoningEntry>? _translationReasoning;
    private List<CharacterProvenanceEntry>? _characterProvenance;

    private void PopulateCorrections(ProcessInfo? process, string? xmlAbsPath)
    {
        var tabCorrections = this.FindControl<TabItem>("TabCorrections");
        if (tabCorrections == null) return;

        // Discover correction-log.md and working text from the provenance chain.
        // Convention: edition dir is xml-open/{kind}/{slug}/, provenance is at
        // ../../../provenance/{slug}/. Correction log at process/correction-log.md,
        // working text at transcription/corrected/*working*.txt.
        string? corrLogPath = null;
        string? workingTextPath = null;

        if (_editionDir != null)
        {
            var slug = Path.GetFileName(_editionDir);
            // Try relative provenance path (3 levels up from edition dir)
            var provRoot = Path.GetFullPath(Path.Combine(_editionDir, "..", "..", "..", "provenance", slug));
            if (Directory.Exists(provRoot))
            {
                var candidate = Path.Combine(provRoot, "process", "correction-log.md");
                if (File.Exists(candidate)) corrLogPath = candidate;

                var correctedDir = Path.Combine(provRoot, "transcription", "corrected");
                if (Directory.Exists(correctedDir))
                {
                    try
                    {
                        var wt = Directory.GetFiles(correctedDir, "*working*").FirstOrDefault();
                        if (wt != null) workingTextPath = wt;
                    }
                    catch { }
                }

                // Parse forensic provenance logs from the same process directory
                var processDir = Path.Combine(provRoot, "process");
                _ocrConsensus = OcrConsensusLogService.Parse(
                    Path.Combine(processDir, "ocr-consensus-log.md"));
                _rejectedReadings = RejectedReadingsLogService.Parse(
                    Path.Combine(processDir, "rejected-readings-log.md"));
                _translationReasoning = TranslationReasoningLogService.Parse(
                    Path.Combine(processDir, "translation-reasoning-log.md"));
                _characterProvenance = CharacterProvenanceLogService.Parse(
                    Path.Combine(processDir, "character-provenance-log.md"));
            }
        }

        if (corrLogPath == null || !File.Exists(corrLogPath) ||
            workingTextPath == null || !File.Exists(workingTextPath))
        {
            // No correction log found — hide the tab entirely
            tabCorrections.IsVisible = false;
            return;
        }

        _corrections = CorrectionLogService.ParseCorrectionLog(corrLogPath);
        _workingTextLines = CorrectionLogService.ParseWorkingText(workingTextPath);

        if (_corrections.Count == 0)
        {
            tabCorrections.IsVisible = false;
            return;
        }

        // Wire the slider
        var slider = this.FindControl<Slider>("SliderCorrections");
        var editor = this.FindControl<AvaloniaEdit.TextEditor>("EditorCorrectionPreview");
        var txtProgress = this.FindControl<TextBlock>("TxtCorrectionProgress");
        var txtInfo = this.FindControl<TextBlock>("TxtCorrectionInfo");
        var txtBasis = this.FindControl<TextBlock>("TxtCorrectionBasis");

        if (slider == null || editor == null) return;

        slider.Maximum = _corrections.Count;
        slider.Value = _corrections.Count; // start at fully corrected

        // Wire evidence button
        var btnEvidence = this.FindControl<Button>("BtnViewEvidence");
        if (btnEvidence != null)
        {
            btnEvidence.Click += (_, _) =>
            {
                if (_currentEvidenceCorrection is not { HasImageEvidence: true } c) return;

                var pdfService = new PdfEvidenceService();
                var viewer = new PdfEvidenceWindow(pdfService);
                viewer.LoadEvidence(
                    c.EvidencePdf!,
                    c.EvidencePage!.Value,
                    $"{c.Locus} ({c.ChangeType})",
                    c.EvidenceRegionX ?? -1,
                    c.EvidenceRegionY ?? -1,
                    c.EvidenceRegionWidth ?? 1.0,
                    c.EvidenceRegionHeight ?? 1.0);
                viewer.Show(this);
            };
        }

        // Show the initial state
        ShowCorrectionState(_corrections.Count, editor, txtProgress, txtInfo, txtBasis);

        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property.Name != "Value") return;
            int step = (int)slider.Value;
            ShowCorrectionState(step, editor, txtProgress, txtInfo, txtBasis);
        };
    }

    private void ShowCorrectionState(
        int step,
        AvaloniaEdit.TextEditor editor,
        TextBlock? txtProgress,
        TextBlock? txtInfo,
        TextBlock? txtBasis)
    {
        if (_corrections == null || _workingTextLines == null) return;

        var state = CorrectionLogService.ReconstructAtStep(_workingTextLines, _corrections, step);
        editor.Text = state.ToDisplayText();

        if (txtProgress != null)
            txtProgress.Text = $"Correction {step} of {state.TotalCorrections}";

        var btnEvidence = this.FindControl<Button>("BtnViewEvidence");

        // Translation reasoning display (shown below basis when available)
        var txtReasoning = this.FindControl<TextBlock>("TxtCorrectionReasoning");

        if (step > 0 && step <= _corrections.Count)
        {
            var c = _corrections[step - 1];
            if (txtInfo != null)
                txtInfo.Text = $"[{c.Date}] {c.Locus}: {c.ChangeType} — \"{c.Before}\" → \"{c.After}\"";
            if (txtBasis != null)
                txtBasis.Text = c.Basis;

            // Show translation reasoning if available for this step+locus
            if (txtReasoning != null)
            {
                var reasoning = _translationReasoning?.FirstOrDefault(
                    r => r.Step == step &&
                         string.Equals(r.Locus, c.Locus, StringComparison.OrdinalIgnoreCase));
                if (reasoning != null)
                {
                    txtReasoning.IsVisible = true;
                    txtReasoning.Text = $"Translation: \"{reasoning.Chinese}\" → \"{reasoning.ChosenEnglish}\"\n" +
                                        $"Reasoning: {reasoning.Reasoning}";
                }
                else
                {
                    txtReasoning.IsVisible = false;
                }
            }

            // Show/hide evidence button based on whether this correction has image coords
            if (btnEvidence != null)
            {
                btnEvidence.IsVisible = c.HasImageEvidence;
                // Rewire click handler for current correction
                _currentEvidenceCorrection = c;
            }

            // Try to scroll to the changed locus
            if (state.HighlightLocus != null)
            {
                for (int i = 0; i < state.Lines.Count; i++)
                {
                    if (string.Equals(state.Lines[i].Locus, state.HighlightLocus, StringComparison.OrdinalIgnoreCase))
                    {
                        var line = Math.Min(i + 1, editor.Document.LineCount);
                        editor.ScrollToLine(line);
                        break;
                    }
                }
            }
        }
        else
        {
            if (txtInfo != null) txtInfo.Text = "Raw OCR output — no corrections applied";
            if (txtBasis != null) txtBasis.Text = "Slide right to see how corrections improve the text.";
            if (btnEvidence != null) btnEvidence.IsVisible = false;
            if (txtReasoning != null) txtReasoning.IsVisible = false;
        }
    }

    // ── Forensic provenance data (appended to Apparatus + Stats tabs) ───

    /// <summary>
    /// Appends forensic provenance sections to existing tabs when log data
    /// was parsed from the provenance directory.
    /// </summary>
    private void PopulateForensicData()
    {
        PopulateForensicApparatus();
        PopulateForensicStats();
    }

    private void PopulateForensicApparatus()
    {
        var host = this.FindControl<StackPanel>("ApparatusHost");
        if (host == null) return;

        // OCR Engine Consensus section
        if (_ocrConsensus is { Count: > 0 })
        {
            host.Children.Add(MakeSection($"OCR Engine Consensus ({_ocrConsensus.Count} loci)"));

            // Column headers
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Margin = new Thickness(0, 2, 0, 4) };
            headerRow.Children.Add(MakeTableCell("Locus", 80, true));
            headerRow.Children.Add(MakeTableCell("Tesseract", 72, true));
            headerRow.Children.Add(MakeTableCell("RapidOCR", 72, true));
            headerRow.Children.Add(MakeTableCell("PaddleOCR", 72, true));
            headerRow.Children.Add(MakeTableCell("EasyOCR", 72, true));
            headerRow.Children.Add(MakeTableCell("Agreement", 80, true));
            headerRow.Children.Add(MakeTableCell("Adopted", 72, true));

            var rowsPanel = new StackPanel { Spacing = 1 };
            rowsPanel.Children.Add(headerRow);

            foreach (var e in _ocrConsensus)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
                row.Children.Add(MakeTableCell(e.Locus, 80, false));
                row.Children.Add(MakeTableCell(e.Tesseract, 72, false));
                row.Children.Add(MakeTableCell(e.RapidOCR, 72, false));
                row.Children.Add(MakeTableCell(e.PaddleOCR, 72, false));
                row.Children.Add(MakeTableCell(e.EasyOCR, 72, false));
                row.Children.Add(MakeTableCell(e.Agreement, 80, false));
                row.Children.Add(MakeTableCell(e.Adopted, 72, false));
                rowsPanel.Children.Add(row);
            }

            var scroll = new ScrollViewer
            {
                Content = rowsPanel,
                MaxHeight = 240,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            };

            host.Children.Add(new Border
            {
                Child = scroll, Padding = new Thickness(8), CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 8),
            });
        }

        // Rejected Readings section
        if (_rejectedReadings is { Count: > 0 })
        {
            host.Children.Add(MakeSection($"Rejected Readings ({_rejectedReadings.Count} entries)"));

            var rejectedPanel = new StackPanel { Spacing = 4 };

            foreach (var r in _rejectedReadings)
            {
                var card = new StackPanel { Spacing = 2 };
                card.Children.Add(new TextBlock
                {
                    Text = $"{r.Locus}: \"{r.Rejected}\" (from {r.Source}) — adopted \"{r.Adopted}\"",
                    FontSize = 11, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap,
                });
                card.Children.Add(new TextBlock
                {
                    Text = r.Reason, FontSize = 10, Opacity = 0.8,
                    FontStyle = FontStyle.Italic, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(12, 0, 0, 0),
                });

                rejectedPanel.Children.Add(new Border
                {
                    Child = card, Padding = new Thickness(8, 4), CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 2),
                });
            }

            var scroll = new ScrollViewer
            {
                Content = rejectedPanel,
                MaxHeight = 300,
            };

            host.Children.Add(new Border
            {
                Child = scroll, Padding = new Thickness(4), CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 8),
            });
        }
    }

    private void PopulateForensicStats()
    {
        var host = this.FindControl<StackPanel>("StatsHost");
        if (host == null) return;

        if (_characterProvenance is not { Count: > 0 }) return;

        host.Children.Add(MakeSection("Character Provenance Summary"));

        // Count characters by confidence level
        var byConfidence = _characterProvenance
            .GroupBy(c => c.Confidence.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ToList();

        var total = _characterProvenance.Count;
        host.Children.Add(MakeKV("Total characters traced", total.ToString()));

        foreach (var group in byConfidence)
        {
            var pct = total > 0 ? (group.Count() * 100.0 / total) : 0;
            host.Children.Add(MakeKV(group.Key, $"{group.Count()} ({pct:F1}%)"));
        }

        // Source distribution
        var bySrc = _characterProvenance
            .GroupBy(c => c.Source.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ToList();

        if (bySrc.Count > 1)
        {
            host.Children.Add(MakeSection("Character Source Distribution"));
            foreach (var group in bySrc)
            {
                var pct = total > 0 ? (group.Count() * 100.0 / total) : 0;
                host.Children.Add(MakeKV(group.Key, $"{group.Count()} ({pct:F1}%)"));
            }
        }
    }

    private static TextBlock MakeTableCell(string text, double width, bool isHeader)
    {
        return new TextBlock
        {
            Text = text,
            Width = width,
            FontSize = 10,
            FontWeight = isHeader ? FontWeight.SemiBold : FontWeight.Normal,
            Opacity = isHeader ? 0.9 : 0.8,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Padding = new Thickness(2, 1),
        };
    }
}
