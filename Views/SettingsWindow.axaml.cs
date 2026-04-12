// Views/SettingsWindow.axaml.cs
using Avalonia.Controls;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow() : this(new AppConfig()) { }

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();

        var vm = new SettingsWindowViewModel(config);
        vm.CloseRequested = result => Close(result);
        DataContext = vm;

        var supportLink = this.FindControl<TextBlock>("BtnSupportSettings");
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
