using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

public partial class TermbaseEditorWindow : Window
{
    // Parameterless ctor for the XAML/designer loader only; real opens go through the
    // (root, origDir, transDir, …) overload from MainWindow.
    public TermbaseEditorWindow() : this(string.Empty, string.Empty, string.Empty)
    {
    }

    private readonly TermbaseEditorWindowViewModel _vm;

    public bool Saved => _vm.Saved;

    /// <summary>
    /// Fired after a successful save. MainWindow subscribes to refresh the assistant panel.
    /// </summary>
    public event EventHandler? TermsSaved;

    /// <summary>
    /// Fired when user wants to navigate to a corpus hit in the reader.
    /// </summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    /// <summary>
    /// Fired when user wants to add a corpus hit to Scholar.
    /// </summary>
    public event EventHandler<CorpusUsageHit>? AddToScholarRequested;

    public TermbaseEditorWindow(string root, string origDir, string transDir, string? username = null, string? landingTerm = null, string? landingCommunityUser = null)
    {
        InitializeComponent();

        var storage = App.Services.GetRequiredService<ITermbaseStorageService>();
        _vm = new TermbaseEditorWindowViewModel(storage, root);
        _vm.SetUsername(username);
        _vm.ConfigureLanding(landingTerm, landingCommunityUser);
        DataContext = _vm;

        // Provide search context for the corpus-usage tab using the ACTIVE corpus's dirs
        // passed in from the main window. Do NOT re-derive via GetOriginalDir(parentRoot):
        // in a multi-corpus install that returns the FIRST-discovered corpus, so the tab
        // would key its search on the wrong corpus's index (review M1).
        var searchIndex = App.Services.GetRequiredService<ISearchIndexService>();
        _vm.SetSearchContext(searchIndex, origDir, transDir);

        _vm.CloseRequested = () => Close();
        _vm.FocusSourceTermRequested = () => this.FindControl<TextBox>("TxtSourceTerm")?.Focus();
        _vm.TermsSaved += (s, e) => TermsSaved?.Invoke(this, e);
        _vm.CorpusNavigationRequested += (_, req) => CorpusNavigationRequested?.Invoke(this, req);
        _vm.AddToScholarRequested += (_, hit) => AddToScholarRequested?.Invoke(this, hit);

        // Wire corpus hit double-click
        var corpusList = this.FindControl<ListBox>("CorpusHitsList");
        if (corpusList != null)
        {
            corpusList.DoubleTapped += (_, _) =>
            {
                if (corpusList.SelectedItem is CorpusUsageHit hit)
                    _vm.RaiseCorpusNavigation(hit);
            };
        }

        // Wire context menu items
        var mnuNavigate = this.FindControl<MenuItem>("MnuNavigateToHit");
        if (mnuNavigate != null)
        {
            mnuNavigate.Click += (_, _) =>
            {
                if (corpusList?.SelectedItem is CorpusUsageHit hit)
                    _vm.RaiseCorpusNavigation(hit);
            };
        }

        var mnuAddScholar = this.FindControl<MenuItem>("MnuAddToScholar");
        if (mnuAddScholar != null)
        {
            mnuAddScholar.Click += (_, _) =>
            {
                if (corpusList?.SelectedItem is CorpusUsageHit hit)
                    _vm.RaiseAddToScholar(hit);
            };
        }

        var mainTabs = this.FindControl<TabControl>("MainTabControl");
        if (mainTabs != null)
        {
            mainTabs.SelectionChanged += (_, _) =>
            {
                if (mainTabs.SelectedItem is TabItem tab && string.Equals(tab.Header?.ToString(), "Corpus Usage", StringComparison.Ordinal))
                    _vm.ActivateCorpusUsageSearch();
            };
        }

        Opened += (_, _) => AsyncGuard.Run(async () => await _vm.LoadCommand.ExecuteAsync(null), "TermbaseEditorWindow.Opened");
    }

    public void ApplyLanding(string? term, string? communityUser = null)
    {
        _vm.ConfigureLanding(term, communityUser);
        _vm.ApplyLandingRequest();
    }

    /// <summary>
    /// Creates a new termbase entry with the source term pre-filled.
    /// Called after the window is opened via the "Create Termbase Entry" context menu.
    /// </summary>
    public void PreFillNewEntry(string sourceTerm) => _vm.PreFillNewEntry(sourceTerm);
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}

