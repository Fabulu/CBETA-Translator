using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

/// <summary>
/// Read-only viewer for a single witness's full delivered text.
/// Opened from the WitnessComparisonPanel "Open full text" button.
/// </summary>
public partial class WitnessTextViewerWindow : Window
{
    private string? _sourcePath;

    public WitnessTextViewerWindow()
    {
        InitializeComponent();
        WireEvents();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireEvents()
    {
        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null) btnClose.Click += (_, _) => Close();

        var btnCopy = this.FindControl<Button>("BtnCopyAll");
        if (btnCopy != null) btnCopy.Click += async (_, _) =>
        {
            var top = TopLevel.GetTopLevel(this);
            var body = this.FindControl<SelectableTextBlock>("TxtBody");
            if (top?.Clipboard != null && body?.Text != null)
                await top.Clipboard.SetTextAsync(body.Text);
        };

        var btnOpenSrc = this.FindControl<Button>("BtnOpenSource");
        if (btnOpenSrc != null) btnOpenSrc.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_sourcePath)) return;
            try { Process.Start(new ProcessStartInfo(_sourcePath) { UseShellExecute = true }); }
            catch { /* best-effort */ }
        };
    }

    /// <summary>
    /// Load and display a witness's full text.
    /// </summary>
    /// <param name="witness">Witness registry entry (provides siglum, label, status).</param>
    /// <param name="editionDir">Absolute path to the edition directory (for resolving relative paths).</param>
    public void LoadWitness(WitnessTextEntry witness, string editionDir)
    {
        Title = $"Witness · {witness.Siglum ?? witness.WitnessId ?? "(unknown)"}";

        var siglumBlock = this.FindControl<TextBlock>("TxtSiglum");
        if (siglumBlock != null) siglumBlock.Text = witness.Siglum ?? witness.WitnessId ?? "?";

        var labelBlock = this.FindControl<TextBlock>("TxtLabel");
        if (labelBlock != null) labelBlock.Text = witness.Label ?? witness.WitnessId ?? "(unlabeled)";

        var metaBlock = this.FindControl<TextBlock>("TxtMeta");
        if (metaBlock != null)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(witness.Role)) parts.Add(witness.Role!);
            if (!string.IsNullOrWhiteSpace(witness.FamilyId)) parts.Add($"family: {witness.FamilyId}");
            if (!string.IsNullOrWhiteSpace(witness.TextStatus)) parts.Add(witness.StatusDisplay);
            if (!string.IsNullOrWhiteSpace(witness.Confidence)) parts.Add($"confidence: {witness.Confidence}");
            if (!string.IsNullOrWhiteSpace(witness.Completeness)) parts.Add(witness.Completeness!);
            metaBlock.Text = string.Join(" · ", parts);
        }

        var bodyBlock = this.FindControl<SelectableTextBlock>("TxtBody");
        var statusBanner = this.FindControl<Border>("StatusBanner");
        var statusBlock = this.FindControl<TextBlock>("TxtStatus");
        var btnOpenSrc = this.FindControl<Button>("BtnOpenSource");

        // Resolve and load the definitive text file
        if (string.IsNullOrWhiteSpace(witness.DefinitiveTextFile))
        {
            ShowStatus(statusBanner, statusBlock,
                "No definitive text file declared for this witness. " +
                "The pipeline has not yet produced a per-witness text artifact.",
                isWarning: true);
            if (bodyBlock != null) bodyBlock.Text = "(no text available)";
            if (btnOpenSrc != null) btnOpenSrc.IsEnabled = false;
            return;
        }

        var resolved = ResolvePath(editionDir, witness.DefinitiveTextFile);
        _sourcePath = resolved;

        if (!File.Exists(resolved))
        {
            ShowStatus(statusBanner, statusBlock,
                $"Declared text file not found: {witness.DefinitiveTextFile}",
                isWarning: true);
            if (bodyBlock != null) bodyBlock.Text = "(file not found at expected location)";
            if (btnOpenSrc != null) btnOpenSrc.IsEnabled = false;
            return;
        }

        try
        {
            var content = File.ReadAllText(resolved);
            // For TEI/XML, optionally strip tags for readability — but the user explicitly
            // asked for the full witness text. Show raw content; user can view source separately.
            if (bodyBlock != null) bodyBlock.Text = content;

            var fmt = witness.TextFormat ?? "(unknown format)";
            ShowStatus(statusBanner, statusBlock,
                $"Source: {witness.DefinitiveTextFile} · Format: {fmt} · {content.Length:n0} characters",
                isWarning: false);
            if (btnOpenSrc != null) btnOpenSrc.IsEnabled = true;
        }
        catch (Exception ex)
        {
            ShowStatus(statusBanner, statusBlock, $"Error loading file: {ex.Message}", isWarning: true);
            if (bodyBlock != null) bodyBlock.Text = "(error reading file)";
            if (btnOpenSrc != null) btnOpenSrc.IsEnabled = false;
        }
    }

    private static string ResolvePath(string editionDir, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) return relativePath;
        return Path.GetFullPath(Path.Combine(editionDir, relativePath));
    }

    private static void ShowStatus(Border? banner, TextBlock? text, string message, bool isWarning)
    {
        if (banner == null || text == null) return;
        text.Text = message;
        banner.IsVisible = true;
        // Subtle color cue: warnings use a warmer tint
        banner.BorderBrush = isWarning
            ? Avalonia.Media.Brushes.OrangeRed
            : Avalonia.Media.Brushes.Gray;
        banner.Opacity = isWarning ? 0.9 : 0.6;
    }
}
