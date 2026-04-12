// Views/ProvenancePanel.axaml.cs
// Collapsible provenance panel that displays per-text source documentation
// from OpenZenTexts manifest.json files. Follows the LicenseDetailsView.SetLicense() pattern.

using System;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class ProvenancePanel : UserControl
{
    private ManifestInfo? _manifest;

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
    public void SetProvenance(ManifestInfo? manifest, TextLicenseInfo? license, CorpusKind corpus)
    {
        _manifest = manifest;

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
            // Fallback cards
            if (corpus == CorpusKind.Cbeta)
            {
                if (cbetaCard != null) cbetaCard.IsVisible = true;
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
            // Truncate long production methods for display
            var method = manifest.ProductionMethod;
            if (method.Length > 400)
                method = method.Substring(0, 400) + "...";
            txtProduction.Text = method;
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

        // Show copy citation button when we have manifest data
        if (btnCopy != null) btnCopy.IsVisible = true;
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

        // Upstream URL
        if (!string.IsNullOrWhiteSpace(w.UpstreamUrl))
        {
            var urlText = w.UpstreamUrl;
            if (urlText.Length > 80)
                urlText = urlText.Substring(0, 77) + "...";
            stack.Children.Add(new TextBlock
            {
                Text = urlText,
                FontSize = 10,
                Opacity = 0.65,
                TextWrapping = TextWrapping.Wrap
            });
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
        if (_manifest == null) return "";

        var sb = new StringBuilder();
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
