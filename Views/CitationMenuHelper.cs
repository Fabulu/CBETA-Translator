// Views/CitationMenuHelper.cs
// Builds a "Cite as..." flyout MenuItem with sub-items for each CitationStyle.
// Shared across all context menus (Reader, Editor, Search, Scholar, Compare).

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// Builds a "Cite as..." flyout MenuItem with sub-items for each CitationStyle.
/// Shared across all context menus.
/// </summary>
internal static class CitationMenuHelper
{
    private static readonly (CitationStyle Style, string Label)[] Styles =
    {
        (CitationStyle.Chicago, "Chicago"),
        (CitationStyle.Apa, "APA"),
        (CitationStyle.Mla, "MLA"),
        (CitationStyle.BibTeX, "BibTeX"),
        (CitationStyle.CbetaReference, "CBETA Reference"),
        (CitationStyle.Sbl, "SBL"),
        (CitationStyle.Ris, "RIS"),
        (CitationStyle.CslJson, "CSL-JSON"),
        (CitationStyle.Plain, "Plain"),
    };

    /// <summary>
    /// Resolve the user's preferred citation style from AppConfig via DI.
    /// Falls back to Chicago if config is unavailable.
    /// </summary>
    internal static CitationStyle GetPreferredStyle()
    {
        try
        {
            var svc = App.Services.GetService<IAppConfigService>();
            var cfg = svc?.TryLoadAsync().GetAwaiter().GetResult();
            return cfg?.PreferredCitationStyle ?? CitationStyle.Chicago;
        }
        catch
        {
            return CitationStyle.Chicago;
        }
    }

    /// <summary>
    /// Creates a "Cite as..." MenuItem with a flyout submenu.
    /// <paramref name="buildMetadata"/> is called lazily when the user picks a style.
    /// <paramref name="copyToClipboard"/> writes the citation text to clipboard.
    /// <paramref name="onCopied"/> is called after clipboard write (for toast/status).
    /// </summary>
    public static MenuItem BuildCiteAsFlyout(
        ICitationService citationService,
        CitationStyle preferredStyle,
        Func<CitationMetadata> buildMetadata,
        Func<string, Task> copyToClipboard,
        Action<string>? onCopied = null)
    {
        var flyout = new MenuItem { Header = "Cite as\u2026" };

        foreach (var (style, label) in Styles)
        {
            var displayLabel = style == preferredStyle ? $"{label} (default)" : label;
            var item = new MenuItem { Header = displayLabel };
            var capturedStyle = style;
            item.Click += async (_, _) =>
            {
                var metadata = buildMetadata();
                var citation = citationService.Generate(metadata, capturedStyle);
                await copyToClipboard(citation);
                onCopied?.Invoke($"{label} citation copied.");
            };
            flyout.Items.Add(item);
        }

        return flyout;
    }
}
