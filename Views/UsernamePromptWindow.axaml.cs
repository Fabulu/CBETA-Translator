// Views/UsernamePromptWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Input;
using CbetaTranslator.App.ViewModels;

namespace CbetaTranslator.App.Views;

public partial class UsernamePromptWindow : Window
{
    private bool _committed;

    public UsernamePromptWindow()
    {
        InitializeComponent();

        var vm = new UsernamePromptWindowViewModel();
        vm.CommitRequested = name =>
        {
            _committed = true;
            Close(name);
        };
        DataContext = vm;

        Closing += OnWindowClosing;

        // Enter key triggers commit
        var txtUsername = this.FindControl<TextBox>("TxtUsername");
        if (txtUsername != null)
            txtUsername.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    vm.CommitCommand.Execute(null);
            };
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_committed)
            e.Cancel = true;
    }
}
