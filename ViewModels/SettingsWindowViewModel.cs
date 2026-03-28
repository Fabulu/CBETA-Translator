using System;
using CbetaTranslator.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CbetaTranslator.App.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _enableHoverDictionary;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private bool _showUsernameError;

    [ObservableProperty]
    private bool _restartTourRequested;

    private readonly AppConfig _template;

    /// <summary>
    /// The resulting config after Apply, or null if cancelled.
    /// </summary>
    public AppConfig? Result { get; private set; }

    /// <summary>
    /// Wired by the code-behind to close the dialog window.
    /// </summary>
    public Action<AppConfig?>? CloseRequested { get; set; }

    public SettingsWindowViewModel(AppConfig config)
    {
        _template = config;
        _isDarkTheme = config.IsDarkTheme;
        _enableHoverDictionary = config.EnableHoverDictionary;
        _username = config.Username ?? string.Empty;
    }

    [RelayCommand]
    private void Apply()
    {
        var name = Username.Trim();
        if (name.Length == 0)
        {
            ShowUsernameError = true;
            return;
        }

        Result = new AppConfig
        {
            TextRootPath = _template.TextRootPath,
            LastSelectedRelPath = _template.LastSelectedRelPath,
            IsDarkTheme = IsDarkTheme,
            ZenOnly = _template.ZenOnly,
            EnableHoverDictionary = EnableHoverDictionary,
            Username = name,
            GitHubAccessToken = _template.GitHubAccessToken,
            GitHubUsername = _template.GitHubUsername,
            HasCompletedOnboarding = RestartTourRequested ? false : _template.HasCompletedOnboarding,
            Version = _template.Version
        };

        CloseRequested?.Invoke(Result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(null);
    }
}
