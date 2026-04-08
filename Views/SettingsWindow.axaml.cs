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
    }
}
