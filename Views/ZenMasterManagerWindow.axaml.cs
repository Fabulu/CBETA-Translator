// Views/ZenMasterManagerWindow.axaml.cs
//
// Thin host window for the reusable ZenMasterManagerView. After the lineage-embed
// refactor the Zen Master explorer lives in MainWindow's Lineage tab by default;
// this window is kept so deep-links and a future pop-out can still float the same
// self-contained control. All the TabControl / graph / detail-panel wiring now
// lives in ZenMasterManagerView — this shell only constructs it, forwards landings
// and corpus navigation, and drives its lazy activation on Opened.

using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

public partial class ZenMasterManagerWindow : Window
{
    private readonly ZenMasterManagerView _view;

    /// <summary>Fired when the user double-clicks a corpus text to navigate to it in the reader.</summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    /// <summary>The shared explorer VM (exposed for deep-link / test callers that need it).</summary>
    public ZenMasterManagerWindowViewModel ViewModel => _view.ViewModel;

    public ZenMasterManagerWindow() : this(null, null, Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json"))
    {
    }

    public ZenMasterManagerWindow(string? repoRoot, string? parentRoot = null, string? baseFilePath = null)
    {
        InitializeComponent();

        _view = new ZenMasterManagerView(repoRoot, parentRoot, baseFilePath);
        _view.CorpusNavigationRequested += (_, req) => CorpusNavigationRequested?.Invoke(this, req);

        var host = this.FindControl<ContentControl>("Host");
        if (host != null) host.Content = _view;

        Opened += async (_, _) => await _view.EnsureActivatedAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>Land on a specific master (deep-link / dict "open master"): forwarded to the
    /// embedded control, which selects the Browse profile and applies the landing once loaded.</summary>
    public void ApplyLanding(string? name, string? user) => _view.ApplyLanding(name, user);
}
