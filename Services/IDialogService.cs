using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Abstraction over the UI-shell operations a ViewModel cannot perform directly —
/// folder/file pickers, modal dialogs, clipboard, and TopLevel access. The concrete
/// <see cref="DialogService"/> resolves the active desktop window at call time, so
/// ViewModels can request shell interactions without holding a <c>Window</c> reference
/// or a bridge delegate back into code-behind.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a folder picker and returns the chosen folder's local path, or
    /// <c>null</c> if the user cancels or no window/storage provider is available.
    /// </summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>
    /// Opens the settings dialog seeded with <paramref name="current"/> and returns
    /// the edited config, or <c>null</c> if the user cancels.
    /// </summary>
    Task<AppConfig?> ShowSettingsDialogAsync(AppConfig current);

    /// <summary>
    /// Prompts the user for a username and returns the entered value, or <c>null</c>
    /// if the user cancels.
    /// </summary>
    Task<string?> ShowUsernamePromptAsync();

    /// <summary>Opens the licenses window scoped to <paramref name="root"/>.</summary>
    Task ShowLicensesAsync(string? root);

    /// <summary>
    /// Shows a modal yes/no confirmation dialog and returns <c>true</c> only if the
    /// user explicitly confirms (defaults to <c>false</c> on close/cancel).
    /// </summary>
    Task<bool> ShowYesNoAsync(string title, string message);
}
