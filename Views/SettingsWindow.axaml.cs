// Views/SettingsWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Views;

public partial class SettingsWindow : Window
{
    private RadioButton? _radioLightTheme;
    private RadioButton? _radioDarkTheme;
    private CheckBox? _chkEnableHoverDictionary;

    private Button? _btnApply;
    private Button? _btnCancel;

    private readonly AppConfig _working;

    public SettingsWindow() : this(new AppConfig())
    {
    }

    public SettingsWindow(AppConfig config)
    {
        _working = CloneConfig(config);
        InitializeComponent();
        BindFromConfig();
    }

    private static AppConfig CloneConfig(AppConfig cfg) => new()
    {
        TextRootPath = cfg.TextRootPath,
        LastSelectedRelPath = cfg.LastSelectedRelPath,
        IsDarkTheme = cfg.IsDarkTheme,
        ZenOnly = cfg.ZenOnly,
        EnableHoverDictionary = cfg.EnableHoverDictionary,
        Version = cfg.Version
    };

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _radioLightTheme = this.FindControl<RadioButton>("RadioLightTheme");
        _radioDarkTheme = this.FindControl<RadioButton>("RadioDarkTheme");
        _chkEnableHoverDictionary = this.FindControl<CheckBox>("ChkEnableHoverDictionary");

        _btnApply = this.FindControl<Button>("BtnApply");
        _btnCancel = this.FindControl<Button>("BtnCancel");

        if (_btnApply != null)
            _btnApply.Click += OnApplyClicked;
        if (_btnCancel != null)
            _btnCancel.Click += OnCancelClicked;
    }

    private void BindFromConfig()
    {
        if (_radioLightTheme != null)
            _radioLightTheme.IsChecked = !_working.IsDarkTheme;

        if (_radioDarkTheme != null)
            _radioDarkTheme.IsChecked = _working.IsDarkTheme;

        if (_chkEnableHoverDictionary != null)
            _chkEnableHoverDictionary.IsChecked = _working.EnableHoverDictionary;
    }

    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        _working.IsDarkTheme = _radioDarkTheme?.IsChecked == true;
        _working.EnableHoverDictionary = _chkEnableHoverDictionary?.IsChecked == true;

        Close(_working);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}