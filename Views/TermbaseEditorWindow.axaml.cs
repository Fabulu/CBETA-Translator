using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class TermbaseEditorWindow : Window
{
    private readonly TermbaseEditorWindowViewModel _vm;

    public bool Saved => _vm.Saved;

    /// <summary>
    /// Fired after a successful save. MainWindow subscribes to refresh the assistant panel.
    /// </summary>
    public event EventHandler? TermsSaved;

    public TermbaseEditorWindow(string root, string? username = null)
    {
        InitializeComponent();

        var storage = App.Services.GetRequiredService<ITermbaseStorageService>();
        _vm = new TermbaseEditorWindowViewModel(storage, root);
        _vm.SetUsername(username);
        DataContext = _vm;

        _vm.CloseRequested = () => Close();
        _vm.FocusSourceTermRequested = () => this.FindControl<TextBox>("TxtSourceTerm")?.Focus();
        _vm.TermsSaved += (s, e) => TermsSaved?.Invoke(this, e);

        Opened += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
