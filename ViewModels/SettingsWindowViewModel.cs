using System;
using System.Text.Json;
using ReadZen.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReadZen.App.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _enableHoverDictionary;

    [ObservableProperty]
    private bool _enableBilingualScrollSync;

    [ObservableProperty]
    private bool _showApparatusNotes;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private bool _showUsernameError;

    [ObservableProperty]
    private bool _restartTourRequested;

    [ObservableProperty]
    private bool _enableConcordance;

    [ObservableProperty]
    private bool _instantSearch;

    [ObservableProperty]
    private int _tmMaxResults;

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
        _enableBilingualScrollSync = config.EnableBilingualScrollSync;
        _showApparatusNotes = config.ShowApparatusNotes;
        _enableConcordance = config.EnableConcordance;
        _instantSearch = config.InstantSearch;
        _tmMaxResults = config.TmMaxResults;
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

        // Clone the template and override ONLY the fields this dialog edits. The
        // previous hand-written field list silently RESET every unlisted AppConfig
        // field (citation style, study/provenance panels, window geometry, search
        // history, corpus, ...) to defaults on every settings save — cloning makes
        // field preservation hold by construction for future AppConfig additions.
        var result = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(_template))!;
        result.IsDarkTheme = IsDarkTheme;
        result.EnableHoverDictionary = EnableHoverDictionary;
        result.EnableBilingualScrollSync = EnableBilingualScrollSync;
        result.ShowApparatusNotes = ShowApparatusNotes;
        result.EnableConcordance = EnableConcordance;
        result.InstantSearch = InstantSearch;
        result.TmMaxResults = Math.Clamp(TmMaxResults, 4, 20);
        result.Username = name;
        if (RestartTourRequested)
            result.HasCompletedOnboarding = false;

        Result = result;
        CloseRequested?.Invoke(Result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(null);
    }
}
