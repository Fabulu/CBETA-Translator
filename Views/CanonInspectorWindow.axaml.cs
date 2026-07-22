using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

/// <summary>
/// Read-only inspector for the prescriptive Zen canon (Assets/Data/zen-corpus.json). Not a
/// prominent tab — reached from the command palette, mirroring the dictionary editor. Code-behind
/// stays thin: it resolves the loaded <see cref="IZenTextsService"/>, resolves English/Chinese
/// titles from the corpus title index, builds the VM, and bridges its navigation event.
/// </summary>
public partial class CanonInspectorWindow : Window
{
    // Parameterless ctor for the XAML/designer loader only; real opens pass the corpus root.
    public CanonInspectorWindow() : this(null)
    {
    }

    /// <summary>Fired when the user opens a text; the host navigates the reader to it.</summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    public CanonInspectorWindow(string? parentRoot)
    {
        InitializeComponent();

        var zen = App.Services.GetRequiredService<IZenTextsService>();
        var titles = LoadTitles(parentRoot);

        var vm = new CanonInspectorWindowViewModel(
            zen,
            rel =>
            {
                var key = (rel ?? "").Replace('\\', '/').TrimStart('/');
                return titles.TryGetValue(key, out var t) ? (t.En, t.Zh) : (null, null);
            });

        vm.NavigationRequested += (_, req) => CorpusNavigationRequested?.Invoke(this, req);
        DataContext = vm;
    }

    /// <summary>
    /// Merges titles.jsonl (English + Chinese) across all discovered corpora for the given parent
    /// root. Returns an empty map when the root is unknown or no title index exists, in which case
    /// rows fall back to the CBETA id.
    /// </summary>
    private static Dictionary<string, (string? Zh, string? En)> LoadTitles(string? parentRoot)
    {
        var merged = new Dictionary<string, (string? Zh, string? En)>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(parentRoot))
            return merged;

        try
        {
            foreach (var layout in AppPaths.DiscoverAllCorpora(parentRoot))
            {
                if (string.IsNullOrWhiteSpace(layout.TranslationsRepoRoot)) continue;
                foreach (var kv in MasterCorpusSearchService.LoadTitles(layout.TranslationsRepoRoot))
                    merged[kv.Key] = kv.Value;
            }
        }
        catch
        {
            // Title index is best-effort; the inspector still lists texts by CBETA id.
        }

        return merged;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
