using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class TermbaseEditorWindow : Window
{
    public TermbaseEditorWindow() : this(string.Empty, null)
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

    public TermbaseEditorWindow(string root, string? username = null)
    {
        InitializeComponent();

        var storage = App.Services.GetRequiredService<ITermbaseStorageService>();
        _vm = new TermbaseEditorWindowViewModel(storage, root);
        _vm.SetUsername(username);
        DataContext = _vm;

        // Provide search context for corpus usage tab
        var searchIndex = App.Services.GetRequiredService<ISearchIndexService>();
        var origDir = AppPaths.GetOriginalDir(root);
        var transDir = AppPaths.GetTranslatedDir(root);
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

        Opened += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}

