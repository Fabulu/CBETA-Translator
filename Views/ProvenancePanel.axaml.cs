// Views/ProvenancePanel.axaml.cs
// Collapsible provenance panel that displays per-text source documentation
// from OpenZenTexts manifest.json files. Follows the LicenseDetailsView.SetLicense() pattern.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class ProvenancePanel : UserControl
{
    private ManifestInfo? _manifest;
    private TextLicenseInfo? _license;

    public ProvenancePanel()
    {
        InitializeComponent();

        var btnCopy = this.FindControl<Button>("BtnCopyCitation");
        if (btnCopy != null) btnCopy.Click += async (_, _) =>
        {
            var txt = BuildCitationText();
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null && !string.IsNullOrEmpty(txt))
                await top.Clipboard.SetTextAsync(txt);
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Populates all controls from manifest data. Follows LicenseDetailsView.SetLicense() pattern.
    /// </summary>
    public void SetProvenance(ManifestInfo? manifest, TextLicenseInfo? license, CorpusKind corpus, string? xmlAbsPath = null)
    {
        _manifest = manifest;
        _license = license;

        var txtWorkName = this.FindControl<TextBlock>("TxtWorkName");
        var txtAuthor = this.FindControl<TextBlock>("TxtAuthor");
        var badgeEdition = this.FindControl<Border>("BadgeEditionKind");
        var txtEdition = this.FindControl<TextBlock>("TxtEditionKind");
        var txtLicenseSpdx = this.FindControl<TextBlock>("TxtLicenseSpdx");
        var txtLicenseFlags = this.FindControl<TextBlock>("TxtLicenseFlags");
        var badgeNoCbeta = this.FindControl<Border>("BadgeNoCbeta");
        var txtNoCbeta = this.FindControl<TextBlock>("TxtNoCbeta");
        var witnessesSection = this.FindControl<StackPanel>("WitnessesSection");
        var witnessHost = this.FindControl<StackPanel>("WitnessHost");
        var productionSection = this.FindControl<StackPanel>("ProductionSection");
        var txtProduction = this.FindControl<TextBlock>("TxtProductionMethod");
        var curatorSection = this.FindControl<StackPanel>("CuratorSection");
        var txtCurator = this.FindControl<TextBlock>("TxtCurator");
        var txtCaptured = this.FindControl<TextBlock>("TxtCapturedUtc");
        var btnCopy = this.FindControl<Button>("BtnCopyCitation");
        var cbetaCard = this.FindControl<Border>("CbetaFallbackCard");
        var noManifestCard = this.FindControl<Border>("NoManifestCard");

        // Hide everything first, then selectively show
        if (badgeEdition != null) badgeEdition.IsVisible = false;
        if (txtWorkName != null) txtWorkName.Text = "";
        if (txtAuthor != null) txtAuthor.Text = "";
        if (txtLicenseSpdx != null) txtLicenseSpdx.Text = "";
        if (txtLicenseFlags != null) txtLicenseFlags.Text = "";
        if (badgeNoCbeta != null) badgeNoCbeta.IsVisible = false;
        if (witnessesSection != null) witnessesSection.IsVisible = false;
        if (witnessHost != null) witnessHost.Children.Clear();
        if (productionSection != null) productionSection.IsVisible = false;
        if (curatorSection != null) curatorSection.IsVisible = false;
        if (btnCopy != null) btnCopy.IsVisible = false;
        if (cbetaCard != null) cbetaCard.IsVisible = false;
        if (noManifestCard != null) noManifestCard.IsVisible = false;

        if (manifest == null)
        {
            if (corpus == CorpusKind.Cbeta)
            {
                if (cbetaCard != null) cbetaCard.IsVisible = true;

                // Enhance the CBETA fallback with whatever the TEI header has
                if (license != null)
                {
                    if (txtWorkName != null && !string.IsNullOrWhiteSpace(license.Title))
                        txtWorkName.Text = license.Title;
                    if (txtAuthor != null && !string.IsNullOrWhiteSpace(license.Author))
                        txtAuthor.Text = license.Author;
                    if (txtLicenseSpdx != null)
                        txtLicenseSpdx.Text = string.IsNullOrWhiteSpace(license.ShortLabel) ? "CBETA Non-Commercial" : license.ShortLabel;
                    if (txtLicenseFlags != null)
                    {
                        var flags = new StringBuilder("Non-commercial only");
                        if (license.AttributionRequired) flags.Append(" | Attribution required");
                        txtLicenseFlags.Text = flags.ToString();
                    }
                    if (!string.IsNullOrWhiteSpace(license.RequiredAttribution) && btnCopy != null)
                        btnCopy.IsVisible = true;
                }
            }
            else
            {
                if (noManifestCard != null) noManifestCard.IsVisible = true;
            }
            return;
        }

        // Title + author
        if (txtWorkName != null) txtWorkName.Text = manifest.WorkName ?? "(untitled)";
        if (txtAuthor != null) txtAuthor.Text = manifest.Author ?? "";

        // Edition kind badge
        if (badgeEdition != null && txtEdition != null && !string.IsNullOrWhiteSpace(manifest.EditionKind))
        {
            txtEdition.Text = FormatEditionKind(manifest.EditionKind);
            badgeEdition.IsVisible = true;

            // Green for transcription/scan_ocr, blue for critical_edition
            var kind = manifest.EditionKind;
            if (kind == "critical_edition")
            {
                badgeEdition.Background = TryGetBrush("BarBg");
                txtEdition.Foreground = TryGetBrush("TextFg");
            }
            else
            {
                badgeEdition.Background = TryGetBrush("SuccessBg");
                txtEdition.Foreground = TryGetBrush("SuccessFg");
            }
        }

        // License summary
        if (txtLicenseSpdx != null)
            txtLicenseSpdx.Text = manifest.License ?? "Unknown";

        if (txtLicenseFlags != null)
        {
            var flags = new StringBuilder();
            flags.Append(manifest.CommercialUseAllowed ? "Commercial use allowed" : "Non-commercial only");
            if (manifest.AttributionRequired) flags.Append(" | Attribution required");
            if (manifest.ShareAlikeRequired) flags.Append(" | Share-alike");
            txtLicenseFlags.Text = flags.ToString();
        }

        // No-CBETA badge
        if (badgeNoCbeta != null && txtNoCbeta != null && manifest.NoCbetaMaterial)
        {
            txtNoCbeta.Text = "\u2713 No CBETA material";
            badgeNoCbeta.IsVisible = true;
        }

        // Witnesses
        if (witnessesSection != null && witnessHost != null &&
            manifest.Witnesses != null && manifest.Witnesses.Count > 0)
        {
            witnessesSection.IsVisible = true;
            foreach (var w in manifest.Witnesses)
                witnessHost.Children.Add(BuildWitnessCard(w));
        }

        // Production method
        if (productionSection != null && txtProduction != null &&
            !string.IsNullOrWhiteSpace(manifest.ProductionMethod))
        {
            productionSection.IsVisible = true;
            txtProduction.Text = manifest.ProductionMethod;
        }

        // Curator + capture date
        if (curatorSection != null && txtCurator != null)
        {
            if (!string.IsNullOrWhiteSpace(manifest.Curator) || !string.IsNullOrWhiteSpace(manifest.CapturedUtc))
            {
                curatorSection.IsVisible = true;
                txtCurator.Text = !string.IsNullOrWhiteSpace(manifest.Curator)
                    ? $"Curator: {manifest.Curator}"
                    : "";
                if (txtCaptured != null)
                    txtCaptured.Text = !string.IsNullOrWhiteSpace(manifest.CapturedUtc)
                        ? $"Captured: {manifest.CapturedUtc}"
                        : "";
            }
        }

        // Discover and display .md documents from provenance/ and exemplars/
        var docsSection = this.FindControl<StackPanel>("DocumentsSection");
        var docsHost = this.FindControl<StackPanel>("DocumentsHost");
        if (docsSection != null && docsHost != null)
        {
            docsHost.Children.Clear();
            var docs = DiscoverDocuments(xmlAbsPath, manifest.TextId);
            if (docs.Count > 0)
            {
                docsSection.IsVisible = true;
                foreach (var (name, path) in docs)
                    docsHost.Children.Add(BuildDocumentExpander(name, path));
            }
        }

        // Show copy citation button when we have manifest data
        if (btnCopy != null) btnCopy.IsVisible = true;
    }

    /// <summary>
    /// Discovers .md documentation files in provenance/{slug}/ and
    /// docs/curation/exemplars/{slug}/ relative to the corpus root.
    /// Returns (display name, absolute path) pairs.
    /// </summary>
    private static List<(string Name, string Path)> DiscoverDocuments(string? xmlAbsPath, string? textId)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(xmlAbsPath) || string.IsNullOrWhiteSpace(textId))
            return result;

        try
        {
            // Derive the slug from the text_id: "pd.wumenguan-1632" -> "wumenguan-1632"
            var slug = textId;
            var dotIdx = textId.IndexOf('.');
            if (dotIdx > 0 && dotIdx < textId.Length - 1)
                slug = textId[(dotIdx + 1)..];

            // The XML lives under xml-open/{publisher}/{slug}/{slug}.xml
            // The repo root is 3 levels up from the XML file's directory
            var xmlDir = System.IO.Path.GetDirectoryName(xmlAbsPath);
            if (xmlDir == null) return result;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(xmlDir, "..", "..", ".."));

            // Check provenance/{slug}/
            var provenanceDir = System.IO.Path.Combine(repoRoot, "provenance", slug);
            if (Directory.Exists(provenanceDir))
            {
                foreach (var mdFile in Directory.EnumerateFiles(provenanceDir, "*.md"))
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(mdFile)
                        .Replace('-', ' ').Replace('_', ' ');
                    // Capitalize first letter
                    if (name.Length > 0) name = char.ToUpperInvariant(name[0]) + name[1..];
                    result.Add((name, mdFile));
                }
            }

            // Check docs/curation/exemplars/{slug}/
            var exemplarDir = System.IO.Path.Combine(repoRoot, "docs", "curation", "exemplars", slug);
            if (Directory.Exists(exemplarDir))
            {
                foreach (var mdFile in Directory.EnumerateFiles(exemplarDir, "*.md"))
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(mdFile)
                        .Replace('-', ' ').Replace('_', ' ');
                    if (name.Length > 0) name = char.ToUpperInvariant(name[0]) + name[1..];
                    result.Add((name, mdFile));
                }
            }
        }
        catch { /* never crash on doc discovery */ }

        return result;
    }

    private static Expander BuildDocumentExpander(string displayName, string filePath)
    {
        string? content = null;
        try { content = File.ReadAllText(filePath); }
        catch { content = "(Could not read file)"; }

        var rendered = RenderMarkdown(content ?? "");

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
    /// Minimal markdown-to-Avalonia renderer for provenance documents.
    /// Handles: # headings, **bold**, - / * bullet lists, | tables |,
    /// --- separators, and plain text with wrapping. No NuGet dependencies.
    /// </summary>
    private static StackPanel RenderMarkdown(string markdown)
    {
        var panel = new StackPanel { Spacing = 3 };
        var lines = markdown.Split('\n');
        var tableRows = new List<string>();
        bool inTable = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            // Flush table if we're leaving one
            if (inTable && !line.TrimStart().StartsWith("|"))
            {
                FlushTable(panel, tableRows);
                tableRows.Clear();
                inTable = false;
            }

            // Table row
            if (line.TrimStart().StartsWith("|"))
            {
                inTable = true;
                // Skip separator rows (|---|---|)
                if (!System.Text.RegularExpressions.Regex.IsMatch(line, @"^\|[\s\-:|]+\|$"))
                    tableRows.Add(line);
                continue;
            }

            // Blank line
            if (string.IsNullOrWhiteSpace(line))
            {
                panel.Children.Add(new Border { Height = 4 });
                continue;
            }

            // Horizontal rule
            if (line.TrimStart().StartsWith("---") && line.Trim().All(c => c == '-' || c == ' '))
            {
                panel.Children.Add(new Border
                {
                    Height = 1,
                    Margin = new Avalonia.Thickness(0, 4),
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
                });
                continue;
            }

            // Headings
            if (line.StartsWith("### "))
            {
                panel.Children.Add(MakeTextBlock(line[4..].Trim(), 11.5, FontWeight.SemiBold));
                continue;
            }
            if (line.StartsWith("## "))
            {
                panel.Children.Add(MakeTextBlock(line[3..].Trim(), 12, FontWeight.Bold));
                continue;
            }
            if (line.StartsWith("# "))
            {
                panel.Children.Add(MakeTextBlock(line[2..].Trim(), 13, FontWeight.Bold));
                continue;
            }

            // Bullet list
            if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
            {
                var indent = line.Length - line.TrimStart().Length;
                var bulletText = line.TrimStart()[2..];
                var tb = MakeRichTextBlock(bulletText, 11);
                tb.Margin = new Avalonia.Thickness(8 + indent * 4, 0, 0, 0);
                // Prepend bullet
                if (tb.Inlines != null && tb.Inlines.Count > 0)
                    tb.Inlines.Insert(0, new Avalonia.Controls.Documents.Run("\u2022 "));
                panel.Children.Add(tb);
                continue;
            }

            // Numbered list
            if (line.TrimStart().Length > 2 && char.IsDigit(line.TrimStart()[0]) &&
                line.TrimStart().IndexOf(". ", StringComparison.Ordinal) > 0 &&
                line.TrimStart().IndexOf(". ", StringComparison.Ordinal) < 5)
            {
                var tb = MakeRichTextBlock(line.TrimStart(), 11);
                tb.Margin = new Avalonia.Thickness(8, 0, 0, 0);
                panel.Children.Add(tb);
                continue;
            }

            // Regular text with inline formatting
            panel.Children.Add(MakeRichTextBlock(line, 11));
        }

        // Flush any remaining table
        if (inTable && tableRows.Count > 0)
            FlushTable(panel, tableRows);

        return panel;
    }

    private static TextBlock MakeTextBlock(string text, double fontSize, FontWeight weight)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.9,
            Margin = new Avalonia.Thickness(0, 2, 0, 1)
        };
    }

    /// <summary>Renders inline **bold** and `code` within a line.</summary>
    private static TextBlock MakeRichTextBlock(string text, double fontSize)
    {
        var tb = new TextBlock
        {
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        };

        // Simple inline parsing: **bold** and `code`
        int pos = 0;
        while (pos < text.Length)
        {
            // Bold: **...**
            if (pos + 2 < text.Length && text[pos] == '*' && text[pos + 1] == '*')
            {
                var end = text.IndexOf("**", pos + 2, StringComparison.Ordinal);
                if (end > pos + 2)
                {
                    tb.Inlines!.Add(new Avalonia.Controls.Documents.Run(text[(pos + 2)..end])
                    {
                        FontWeight = FontWeight.Bold
                    });
                    pos = end + 2;
                    continue;
                }
            }

            // Code: `...`
            if (text[pos] == '`')
            {
                var end = text.IndexOf('`', pos + 1);
                if (end > pos + 1)
                {
                    tb.Inlines!.Add(new Avalonia.Controls.Documents.Run(text[(pos + 1)..end])
                    {
                        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                        Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
                    });
                    pos = end + 1;
                    continue;
                }
            }

            // Plain text: consume until next special char
            var nextSpecial = text.Length;
            var nextBold = text.IndexOf("**", pos, StringComparison.Ordinal);
            var nextCode = text.IndexOf('`', pos);
            if (nextBold >= 0 && nextBold < nextSpecial) nextSpecial = nextBold;
            if (nextCode >= 0 && nextCode < nextSpecial) nextSpecial = nextCode;
            if (nextSpecial == pos) nextSpecial = pos + 1; // advance at least 1 char

            tb.Inlines!.Add(new Avalonia.Controls.Documents.Run(text[pos..nextSpecial]));
            pos = nextSpecial;
        }

        return tb;
    }

    /// <summary>Renders a markdown table as a simple grid of TextBlocks.</summary>
    private static void FlushTable(StackPanel parent, List<string> rows)
    {
        if (rows.Count == 0) return;

        var tablePanel = new StackPanel { Spacing = 1, Margin = new Avalonia.Thickness(0, 2) };
        bool isHeader = true;

        foreach (var row in rows)
        {
            var cells = row.Split('|', StringSplitOptions.None)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c) || row.Contains('|'))
                .ToArray();

            // Filter out empty leading/trailing from the split
            var cleanCells = cells.Where(c => c.Length > 0 || cells.Length <= 2).ToList();
            if (cleanCells.Count == 0) continue;

            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
            };

            foreach (var cell in cleanCells)
            {
                rowPanel.Children.Add(new TextBlock
                {
                    Text = cell,
                    FontSize = 10.5,
                    FontWeight = isHeader ? FontWeight.SemiBold : FontWeight.Normal,
                    TextWrapping = TextWrapping.NoWrap,
                    MinWidth = 60,
                    Opacity = isHeader ? 0.9 : 0.8,
                });
            }

            tablePanel.Children.Add(rowPanel);
            isHeader = false;
        }

        var tableScroll = new ScrollViewer
        {
            Content = tablePanel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };

        parent.Children.Add(new Border
        {
            Child = tableScroll,
            Padding = new Avalonia.Thickness(4),
            CornerRadius = new Avalonia.CornerRadius(4),
            Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
        });
    }

    private static Border BuildWitnessCard(WitnessInfo w)
    {
        var stack = new StackPanel { Spacing = 2 };

        // Label (bold)
        stack.Children.Add(new TextBlock
        {
            Text = w.Label ?? w.Id ?? "(unknown witness)",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        // Kind + Role
        if (!string.IsNullOrWhiteSpace(w.Kind) || !string.IsNullOrWhiteSpace(w.RoleInProduction))
        {
            var parts = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(w.Kind))
                parts.Append(FormatWitnessKind(w.Kind));
            if (!string.IsNullOrWhiteSpace(w.RoleInProduction))
            {
                if (parts.Length > 0) parts.Append(" | ");
                parts.Append(w.RoleInProduction);
            }
            stack.Children.Add(new TextBlock
            {
                Text = parts.ToString(),
                FontSize = 11,
                Opacity = 0.8,
                TextWrapping = TextWrapping.Wrap
            });
        }

        // Upstream URL — clickable, opens in browser
        if (!string.IsNullOrWhiteSpace(w.UpstreamUrl))
        {
            var fullUrl = w.UpstreamUrl;
            var displayUrl = fullUrl.Length > 60 ? fullUrl.Substring(0, 57) + "..." : fullUrl;
            var linkBtn = new Button
            {
                Content = displayUrl,
                FontSize = 10,
                Padding = new Avalonia.Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 160, 255)),
            };
            ToolTip.SetTip(linkBtn, fullUrl);
            linkBtn.Click += (_, _) =>
            {
                try { Process.Start(new ProcessStartInfo(fullUrl) { UseShellExecute = true }); }
                catch { /* ignore if no browser */ }
            };
            stack.Children.Add(linkBtn);
        }

        // Stable revision URL — clickable if different from upstream
        if (!string.IsNullOrWhiteSpace(w.StableRevisionUrl) &&
            !string.Equals(w.StableRevisionUrl, w.UpstreamUrl, StringComparison.OrdinalIgnoreCase))
        {
            var fullUrl = w.StableRevisionUrl;
            var displayUrl = fullUrl.Length > 60 ? fullUrl.Substring(0, 57) + "..." : fullUrl;
            var revBtn = new Button
            {
                Content = "Stable revision: " + displayUrl,
                FontSize = 10,
                Padding = new Avalonia.Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 160, 255)),
            };
            ToolTip.SetTip(revBtn, fullUrl);
            revBtn.Click += (_, _) =>
            {
                try { Process.Start(new ProcessStartInfo(fullUrl) { UseShellExecute = true }); }
                catch { /* ignore */ }
            };
            stack.Children.Add(revBtn);
        }

        // SHA-256 (first 16 chars) + bytes
        if (!string.IsNullOrWhiteSpace(w.CapturedSha256))
        {
            var sha = w.CapturedSha256.Length > 16
                ? w.CapturedSha256.Substring(0, 16) + "..."
                : w.CapturedSha256;

            var detail = $"SHA-256: {sha}";
            if (w.CapturedBytes > 0)
                detail += $" | {FormatBytes(w.CapturedBytes)}";

            stack.Children.Add(new TextBlock
            {
                Text = detail,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                Opacity = 0.6
            });
        }

        // Capture date
        if (!string.IsNullOrWhiteSpace(w.CapturedUtc))
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"Captured: {w.CapturedUtc}",
                FontSize = 10,
                Opacity = 0.6
            });
        }

        // Vetting confidence badge
        if (!string.IsNullOrWhiteSpace(w.VettingConfidence))
        {
            var confBadge = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(6, 1),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0, 2, 0, 0),
                Child = new TextBlock
                {
                    Text = $"vetting: {w.VettingConfidence}",
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold
                }
            };

            // Color by confidence level
            if (w.VettingConfidence == "high")
            {
                confBadge.Background = new SolidColorBrush(Color.FromArgb(40, 0, 180, 0));
            }
            else if (w.VettingConfidence == "medium")
            {
                confBadge.Background = new SolidColorBrush(Color.FromArgb(40, 220, 180, 0));
            }
            else
            {
                confBadge.Background = new SolidColorBrush(Color.FromArgb(40, 220, 60, 0));
            }

            stack.Children.Add(confBadge);
        }

        return new Border
        {
            Padding = new Avalonia.Thickness(8),
            CornerRadius = new Avalonia.CornerRadius(6),
            BorderThickness = new Avalonia.Thickness(1),
            Child = stack
        };
    }

    private string BuildCitationText()
    {
        var sb = new StringBuilder();

        if (_manifest != null)
        {
            sb.Append(_manifest.WorkName ?? "Unknown work");
            if (!string.IsNullOrWhiteSpace(_manifest.Author))
                sb.Append($" by {_manifest.Author}");
            if (!string.IsNullOrWhiteSpace(_manifest.TextId))
                sb.Append($" [{_manifest.TextId}]");
            if (!string.IsNullOrWhiteSpace(_manifest.License))
                sb.Append($". License: {_manifest.License}");
            if (!string.IsNullOrWhiteSpace(_manifest.Curator))
                sb.Append($". {_manifest.Curator}");
            sb.Append('.');
        }
        else if (_license != null && !string.IsNullOrWhiteSpace(_license.RequiredAttribution))
        {
            sb.Append(_license.RequiredAttribution);
        }
        else if (_license != null)
        {
            if (!string.IsNullOrWhiteSpace(_license.Title)) sb.Append(_license.Title);
            if (!string.IsNullOrWhiteSpace(_license.Author)) sb.Append($" by {_license.Author}");
            if (!string.IsNullOrWhiteSpace(_license.ShortLabel)) sb.Append($". {_license.ShortLabel}");
            sb.Append('.');
        }

        return sb.ToString();
    }

    private static string FormatEditionKind(string kind) => kind switch
    {
        "transcription" => "Transcription",
        "critical_edition" => "Critical Edition",
        "scan_ocr" => "Scan + OCR",
        "derived" => "Derived",
        _ => kind
    };

    private static string FormatWitnessKind(string kind) => kind switch
    {
        "wiki_transcription" => "Wiki transcription",
        "woodblock_scan" => "Woodblock scan",
        "printed_edition" => "Printed edition",
        "manuscript" => "Manuscript",
        "other" => "Other",
        _ => kind
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private IBrush? TryGetBrush(string resourceKey)
    {
        if (this.TryFindResource(resourceKey, out var resource) && resource is IBrush brush)
            return brush;
        return null;
    }
}
