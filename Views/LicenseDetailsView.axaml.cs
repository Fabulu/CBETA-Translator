// Views/LicenseDetailsView.axaml.cs
// Flyout content shown when the user clicks the reader's license chip.
// Presents title, author, short license label, rights-basis text, source URLs,
// and provenance badges. Buttons copy attribution to clipboard or open the
// source URL in the default browser.
using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

public partial class LicenseDetailsView : UserControl
{
    private TextLicenseInfo? _license;

    public LicenseDetailsView()
    {
        InitializeComponent();
        var btnCopy = this.FindControl<Button>("BtnCopyAttribution");
        var btnOpen = this.FindControl<Button>("BtnOpenSource");
        if (btnCopy != null) btnCopy.Click += async (_, _) =>
        {
            var txt = AttributionFormatter.Plain(_license);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(txt);
        };
        if (btnOpen != null) btnOpen.Click += (_, _) =>
        {
            var url = _license?.StableRevisionUrl ?? _license?.SourceUrl;
            if (string.IsNullOrWhiteSpace(url)) return;
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void SetLicense(TextLicenseInfo? license)
    {
        _license = license;

        var txtTitle = this.FindControl<TextBlock>("TxtTitle");
        var txtAuthorYear = this.FindControl<TextBlock>("TxtAuthorYear");
        var txtShort = this.FindControl<TextBlock>("TxtShortLabel");
        var txtBasis = this.FindControl<TextBlock>("TxtRightsBasis");
        var txtSrc = this.FindControl<TextBlock>("TxtSourceUrl");
        var txtStable = this.FindControl<TextBlock>("TxtStableUrl");
        var badgeNoCbeta = this.FindControl<Border>("BadgeNoCbeta");
        var badgeConf = this.FindControl<Border>("BadgeConfidence");
        var txtConf = this.FindControl<TextBlock>("TxtConfidence");

        if (license == null)
        {
            if (txtTitle != null) txtTitle.Text = "No license metadata available";
            if (txtAuthorYear != null) txtAuthorYear.Text = "";
            if (txtShort != null) txtShort.Text = "";
            if (txtBasis != null) txtBasis.Text = "";
            if (txtSrc != null) txtSrc.Text = "";
            if (txtStable != null) txtStable.Text = "";
            if (badgeNoCbeta != null) badgeNoCbeta.IsVisible = false;
            if (badgeConf != null) badgeConf.IsVisible = false;
            return;
        }

        if (txtTitle != null) txtTitle.Text = license.Title ?? "(untitled)";
        if (txtAuthorYear != null)
        {
            string ay = license.Author ?? "";
            if (!string.IsNullOrWhiteSpace(license.YearComposed))
                ay = string.IsNullOrEmpty(ay) ? $"({license.YearComposed})" : $"{ay} — {license.YearComposed}";
            txtAuthorYear.Text = ay;
        }
        // Unknown class still gets the chip + flyout but with a clear
        // explanatory label so the user knows it's "we couldn't classify
        // it" not "we have no info at all".
        if (txtShort != null)
        {
            txtShort.Text = license.LicenseClass == LicenseClass.Unknown
                ? "License could not be classified"
                : license.ShortLabel;
        }
        if (txtBasis != null)
        {
            // For Unknown class, surface the raw availability text so the
            // user can read it themselves and verify the license manually.
            // For known classes, prefer the structured RightsBasisText.
            txtBasis.Text = license.LicenseClass == LicenseClass.Unknown
                ? (string.IsNullOrWhiteSpace(license.LongText) ? "(no availability text)" : license.LongText)
                : (license.RightsBasisText ?? license.LongText);
        }
        if (txtSrc != null) txtSrc.Text = license.SourceUrl ?? "—";
        if (txtStable != null) txtStable.Text = license.StableRevisionUrl ?? "—";
        if (badgeNoCbeta != null) badgeNoCbeta.IsVisible = license.NoCbetaMaterial;
        if (badgeConf != null && txtConf != null && !string.IsNullOrWhiteSpace(license.VettingConfidence))
        {
            txtConf.Text = $"vetting: {license.VettingConfidence}";
            badgeConf.IsVisible = true;
        }
        else if (badgeConf != null)
        {
            badgeConf.IsVisible = false;
        }
    }
}
