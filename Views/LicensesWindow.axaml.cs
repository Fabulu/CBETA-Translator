// Views/LicensesWindow.axaml.cs
using Avalonia.Controls;
using CbetaTranslator.App.ViewModels;

namespace CbetaTranslator.App.Views;

public partial class LicensesWindow : Window
{
    public LicensesWindow() : this(null)
    {
    }

    public LicensesWindow(string? root)
    {
        InitializeComponent();

        var vm = new LicensesWindowViewModel(root);
        vm.CloseRequested = Close;
        DataContext = vm;
    }
}

