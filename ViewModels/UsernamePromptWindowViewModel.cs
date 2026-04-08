using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReadZen.App.ViewModels;

public partial class UsernamePromptWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private bool _showError;

    /// <summary>
    /// Wired by the code-behind to close the dialog with the validated name.
    /// </summary>
    public Action<string>? CommitRequested { get; set; }

    [RelayCommand]
    private void Commit()
    {
        var name = Username.Trim();
        if (name.Length == 0)
        {
            ShowError = true;
            return;
        }

        CommitRequested?.Invoke(name);
    }
}
