using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

/// <summary>
/// Rich Zen-dictionary editor window (schema v2). The legacy flat editor
/// (<see cref="TermbaseEditorWindow"/>) is untouched; this is its rich sibling. Code-behind stays
/// thin: it resolves services from <c>App.Services</c>, builds the VM, and wires the master picker
/// and reader navigation the way TermbaseEditorWindow does.
/// </summary>
public partial class DictionaryEditorWindow : Window
{
    // Parameterless ctor for the XAML/designer loader only; real opens go through the
    // (root, origDir, transDir, masterCacheDir, …) overload from MainWindow.
    public DictionaryEditorWindow() : this(string.Empty, string.Empty, string.Empty, null)
    {
    }

    private readonly DictionaryEditorWindowViewModel _vm;

    public bool Saved => _vm.Saved;

    /// <summary>Fired after a successful save. The host refreshes dependent panels.</summary>
    public event EventHandler? TermsSaved;

    /// <summary>Fired when the user opens an occurrence; the host navigates the reader to it.</summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    public DictionaryEditorWindow(string root, string origDir, string transDir, string? masterCacheDir, string? username = null)
    {
        InitializeComponent();

        var store = App.Services.GetRequiredService<IDictionaryStore>();
        var evidence = App.Services.GetRequiredService<IDictionaryEvidenceService>();

        _vm = new DictionaryEditorWindowViewModel(store, root);
        _vm.SetContext(evidence, origDir, transDir, masterCacheDir);
        _vm.SetUsername(username);
        DataContext = _vm;

        _vm.CloseRequested = () => Close();
        _vm.FocusSourceTermRequested = () => this.FindControl<TextBox>("TxtSourceTerm")?.Focus();
        _vm.TermsSaved += (_, e) => TermsSaved?.Invoke(this, e);
        _vm.OccurrenceNavigationRequested += (_, req) => CorpusNavigationRequested?.Invoke(this, req);

        var btnPick = this.FindControl<Button>("BtnPickMaster");
        if (btnPick != null)
            btnPick.Click += (_, _) => AsyncGuard.Run(PickMasterAsync, "DictionaryEditorWindow.PickMaster");

        Opened += (_, _) => AsyncGuard.Run(async () => await _vm.LoadCommand.ExecuteAsync(null), "DictionaryEditorWindow.Opened");
    }

    private async Task PickMasterAsync()
    {
        if (_vm.SelectedSense == null)
            return;

        var names = MasterDatesService.LoadBaseNameSet().OrderBy(n => n).ToList();
        if (names.Count == 0)
            return;

        var dialog = new MasterPickerDialog(names, searchWatermark: "Search masters...", okButtonText: "Select")
        {
            Title = "Select Zen Master"
        };

        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _vm.SelectedSense.MasterName = result;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
