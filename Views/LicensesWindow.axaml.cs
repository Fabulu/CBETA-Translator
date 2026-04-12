// Views/LicensesWindow.axaml.cs
using Avalonia.Controls;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

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

        var supportLink = this.FindControl<TextBlock>("BtnSupportLicenses");
        if (supportLink != null)
        {
            supportLink.PointerPressed += (_, _) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://ko-fi.com/readzen") { UseShellExecute = true }); }
                catch { }
            };
        }
    }
}

