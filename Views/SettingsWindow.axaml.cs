// Views/SettingsWindow.axaml.cs
using System.Reflection;
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

        var verBlock = this.FindControl<TextBlock>("TxtAppVersion");
        if (verBlock != null)
        {
            var asm = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
            var ver = asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString()
                      ?? "unknown";
            verBlock.Text = $"Version {ver}";
        }

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
