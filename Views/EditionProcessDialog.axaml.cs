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
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;

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
    public void Load(
        ManifestInfo manifest,
        string? xmlAbsPath,
        ProcessService? processService,
        ApparatusService? apparatusService,
        EditionStatsService? statsService,
        DocumentsService? documentsService)
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

        if (xmlAbsPath != null)
        {
            process = processService?.TryLoad(xmlAbsPath);
            apparatus = apparatusService?.TryLoad(xmlAbsPath);
            stats = statsService?.TryLoad(xmlAbsPath);
            documents = documentsService?.TryLoad(xmlAbsPath);
        }

        PopulateSources(manifest);
        PopulateProcess(process, manifest);
        PopulateApparatus(apparatus);
        PopulateStats(stats);
        PopulateDocuments(documents, xmlAbsPath, manifest.TextId);
    }

    // ── Sources tab ──────────────────────────────────────────────────────

    private void PopulateSources(ManifestInfo manifest)
    {
        var host = this.FindControl<StackPanel>("SourcesHost");
        if (host == null) return;

        if (manifest.Witnesses == null || manifest.Witnesses.Count == 0)
        {
            host.Children.Add(MakeEmptyState("No witnesses recorded."));
            return;
        }

        foreach (var w in manifest.Witnesses)
            host.Children.Add(BuildWitnessCard(w, manifest.BaseWitnessId));
    }

    private static Border BuildWitnessCard(WitnessInfo w, string? baseWitnessId)
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
            return;
        }

        host.Children.Add(MakeSection($"Apparatus ({apparatus.Entries.Count} entries)"));

        foreach (var entry in apparatus.Entries)
        {
            var card = new StackPanel { Spacing = 2 };

            // Locus + status
            var header = $"{entry.LocusId ?? "?"} [{entry.Status ?? "?"}]";
            if (!string.IsNullOrWhiteSpace(entry.Section)) header = $"{entry.Section} / {header}";
            card.Children.Add(new TextBlock { Text = header, FontWeight = FontWeight.SemiBold, FontSize = 11 });

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
                    absPath = Path.GetFullPath(Path.Combine(xmlDir, doc.Path ?? ""));

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
}
