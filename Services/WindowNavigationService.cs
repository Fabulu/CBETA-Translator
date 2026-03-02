using Avalonia.Threading;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Views;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Opens a new, fully independent reader window and navigates it to a specific location.
/// The secondary window has its own service instances; closing it never exits the app.
/// </summary>
public static class WindowNavigationService
{
    /// <summary>
    /// Creates a new <see cref="MainWindow"/> (secondary, non-main), shows it,
    /// then loads <paramref name="root"/> and navigates to <paramref name="request"/>.
    /// Must be called from the UI thread (or will be marshalled there automatically).
    /// </summary>
    public static void OpenAndNavigate(string root, NavigationRequest request)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var window = new MainWindow(isSecondaryWindow: true);
            window.Show();
            await window.OpenAtAsync(root, request);
        }, DispatcherPriority.Background);
    }
}
