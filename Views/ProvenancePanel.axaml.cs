// Views/ProvenancePanel.axaml.cs
// Slim provenance sidebar: source/license facts only.
// Full edition details (witnesses, process, apparatus, stats, documents)
// live in EditionProcessDialog, opened via the "View Edition Details..." button.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Avalonia.Controls;
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
    private string? _xmlAbsPath;

    /// <summary>
    /// Fired when the user clicks "View Edition Details..." so the parent
    /// can open the EditionProcessDialog with the right services.
    /// </summary>
    public event Action<ManifestInfo, string?>? EditionDetailsRequested;

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

        var btnDetails = this.FindControl<Button>("BtnViewEditionDetails");
        if (btnDetails != null) btnDetails.Click += (_, _) =>
        {
            if (_manifest != null)
                EditionDetailsRequested?.Invoke(_manifest, _xmlAbsPath);
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Populates the slim sidebar from manifest data.
    /// </summary>
    public void SetProvenance(ManifestInfo? manifest, TextLicenseInfo? license, CorpusKind corpus, string? xmlAbsPath = null)
    {
        _manifest = manifest;
        _license = license;
        _xmlAbsPath = xmlAbsPath;

        var txtWorkName = this.FindControl<TextBlock>("TxtWorkName");
        var txtAuthor = this.FindControl<TextBlock>("TxtAuthor");
        var badgeEdition = this.FindControl<Border>("BadgeEditionKind");
        var txtEdition = this.FindControl<TextBlock>("TxtEditionKind");
        var txtLicenseSpdx = this.FindControl<TextBlock>("TxtLicenseSpdx");
        var txtLicenseFlags = this.FindControl<TextBlock>("TxtLicenseFlags");
        var badgeNoCbeta = this.FindControl<Border>("BadgeNoCbeta");
        var txtNoCbeta = this.FindControl<TextBlock>("TxtNoCbeta");
        var txtBaseWitness = this.FindControl<TextBlock>("TxtBaseWitness");
        var txtWitnessCount = this.FindControl<TextBlock>("TxtWitnessCount");
        var sourceLinksHost = this.FindControl<StackPanel>("SourceLinksHost");
        var badgeMaturity = this.FindControl<Border>("BadgeMaturity");
        var txtMaturity = this.FindControl<TextBlock>("TxtMaturity");
        var btnDetails = this.FindControl<Button>("BtnViewEditionDetails");
        var btnCopy = this.FindControl<Button>("BtnCopyCitation");
        var cbetaCard = this.FindControl<Border>("CbetaFallbackCard");
        var noManifestCard = this.FindControl<Border>("NoManifestCard");

        // Hide everything first
        if (badgeEdition != null) badgeEdition.IsVisible = false;
        if (txtWorkName != null) txtWorkName.Text = "";
        if (txtAuthor != null) txtAuthor.Text = "";
        if (txtLicenseSpdx != null) txtLicenseSpdx.Text = "";
        if (txtLicenseFlags != null) txtLicenseFlags.Text = "";
        if (badgeNoCbeta != null) badgeNoCbeta.IsVisible = false;
        if (txtBaseWitness != null) txtBaseWitness.IsVisible = false;
        if (txtWitnessCount != null) txtWitnessCount.IsVisible = false;
        if (sourceLinksHost != null) { sourceLinksHost.Children.Clear(); sourceLinksHost.IsVisible = false; }
        if (badgeMaturity != null) badgeMaturity.IsVisible = false;
        if (btnDetails != null) btnDetails.IsVisible = false;
        if (btnCopy != null) btnCopy.IsVisible = false;
        if (cbetaCard != null) cbetaCard.IsVisible = false;
        if (noManifestCard != null) noManifestCard.IsVisible = false;

        if (manifest == null)
        {
            if (corpus == CorpusKind.Cbeta)
            {
                if (cbetaCard != null) cbetaCard.IsVisible = true;

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

            if (manifest.EditionKind == "critical_edition")
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

        // Base witness (single line)
        if (txtBaseWitness != null && !string.IsNullOrWhiteSpace(manifest.BaseWitnessId))
        {
            var baseW = manifest.Witnesses?.FirstOrDefault(w =>
                string.Equals(w.Id, manifest.BaseWitnessId, StringComparison.OrdinalIgnoreCase));
            txtBaseWitness.Text = baseW != null
                ? $"Base: {baseW.Label ?? baseW.Id}"
                : $"Base: {manifest.BaseWitnessId}";
            txtBaseWitness.IsVisible = true;
        }

        // Witness count
        if (txtWitnessCount != null && manifest.Witnesses != null && manifest.Witnesses.Count > 0)
        {
            var count = manifest.Witnesses.Count;
            txtWitnessCount.Text = $"{count} witness{(count != 1 ? "es" : "")} consulted";
            txtWitnessCount.IsVisible = true;
        }

        // Source links (upstream URLs from witnesses)
        if (sourceLinksHost != null && manifest.Witnesses != null)
        {
            foreach (var w in manifest.Witnesses.Where(w => !string.IsNullOrWhiteSpace(w.UpstreamUrl)))
            {
                var label = w.Label ?? w.Id ?? "Source";
                var url = w.UpstreamUrl!;
                var linkBtn = new Button
                {
                    Content = label,
                    FontSize = 10,
                    Padding = new Avalonia.Thickness(0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 160, 255)),
                };
                ToolTip.SetTip(linkBtn, url);
                linkBtn.Click += (_, _) =>
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { }
                };
                sourceLinksHost.Children.Add(linkBtn);
            }
            if (sourceLinksHost.Children.Count > 0)
                sourceLinksHost.IsVisible = true;
        }

        // Edition maturity badge
        if (badgeMaturity != null && txtMaturity != null && !string.IsNullOrWhiteSpace(manifest.EditionMaturity))
        {
            txtMaturity.Text = FormatMaturity(manifest.EditionMaturity);
            badgeMaturity.IsVisible = true;

            badgeMaturity.Background = manifest.EditionMaturity switch
            {
                "published" => new SolidColorBrush(Color.FromArgb(40, 0, 180, 0)),
                "publication-candidate" => new SolidColorBrush(Color.FromArgb(40, 0, 140, 220)),
                "review" => new SolidColorBrush(Color.FromArgb(40, 220, 180, 0)),
                _ => new SolidColorBrush(Color.FromArgb(40, 160, 160, 160)),
            };
        }

        // Show "View Edition Details..." button for OpenZen texts
        if (btnDetails != null) btnDetails.IsVisible = true;

        // Show copy citation
        if (btnCopy != null) btnCopy.IsVisible = true;
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

    private static string FormatMaturity(string maturity) => maturity switch
    {
        "draft" => "Draft",
        "review" => "Under Review",
        "publication-candidate" => "Publication Candidate",
        "published" => "Published",
        _ => maturity
    };

    private IBrush? TryGetBrush(string resourceKey)
    {
        if (this.TryFindResource(resourceKey, out var resource) && resource is IBrush brush)
            return brush;
        return null;
    }
}
