using System;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.Tests.Stubs;

/// <summary>
/// Headless <see cref="IDialogService"/> used by ViewModel tests. Each operation is
/// backed by an overridable delegate so a test can script a result, and call counts
/// are tracked for assertions. Defaults are safe: pickers/prompts return null,
/// yes/no confirms return <c>true</c> (matching the pre-migration navigation-guard
/// fallback), and licenses is a no-op.
/// </summary>
public sealed class StubDialogService : IDialogService
{
    public Func<string, Task<string?>> OnPickFolder { get; set; } = _ => Task.FromResult<string?>(null);
    public Func<AppConfig, Task<AppConfig?>> OnShowSettings { get; set; } = _ => Task.FromResult<AppConfig?>(null);
    public Func<Task<string?>> OnUsernamePrompt { get; set; } = () => Task.FromResult<string?>(null);
    public Func<string?, Task> OnShowLicenses { get; set; } = _ => Task.CompletedTask;
    public Func<string, string, Task<bool>> OnYesNo { get; set; } = (_, _) => Task.FromResult(true);

    public int PickFolderCalls { get; private set; }
    public int ShowSettingsCalls { get; private set; }
    public int UsernamePromptCalls { get; private set; }
    public int ShowLicensesCalls { get; private set; }
    public int YesNoCalls { get; private set; }

    public Task<string?> PickFolderAsync(string title)
    {
        PickFolderCalls++;
        return OnPickFolder(title);
    }

    public Task<AppConfig?> ShowSettingsDialogAsync(AppConfig current)
    {
        ShowSettingsCalls++;
        return OnShowSettings(current);
    }

    public Task<string?> ShowUsernamePromptAsync()
    {
        UsernamePromptCalls++;
        return OnUsernamePrompt();
    }

    public Task ShowLicensesAsync(string? root)
    {
        ShowLicensesCalls++;
        return OnShowLicenses(root);
    }

    public Task<bool> ShowYesNoAsync(string title, string message)
    {
        YesNoCalls++;
        return OnYesNo(title, message);
    }
}
