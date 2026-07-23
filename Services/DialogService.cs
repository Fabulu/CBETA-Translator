using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReadZen.App.Models;
using ReadZen.App.Views;

namespace ReadZen.App.Services;

/// <summary>
/// Default <see cref="IDialogService"/> implementation. Resolves the active desktop
/// <c>MainWindow</c> lazily at call time (via the classic-desktop lifetime) so it can
/// be a plain DI singleton with no injected window reference. Each operation degrades
/// gracefully to a safe default when no window is available (e.g. headless/startup).
/// </summary>
public sealed class DialogService : IDialogService
{
    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<string?> PickFolderAsync(string title)
    {
        var owner = MainWindow;
        if (owner?.StorageProvider is not { } storage) return null;

        var picked = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title
        });

        return picked.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<AppConfig?> ShowSettingsDialogAsync(AppConfig current)
    {
        var owner = MainWindow;
        if (owner is null) return null;

        var settingsWindow = new SettingsWindow(current);
        return await settingsWindow.ShowDialog<AppConfig?>(owner);
    }

    public async Task<string?> ShowUsernamePromptAsync()
    {
        var owner = MainWindow;
        if (owner is null) return null;

        var prompt = new UsernamePromptWindow();
        return await prompt.ShowDialog<string?>(owner);
    }

    public async Task ShowLicensesAsync(string? root)
    {
        var owner = MainWindow;
        if (owner is null) return;

        await new LicensesWindow(root).ShowDialog(owner);
    }

    public async Task<bool> ShowYesNoAsync(string title, string message)
    {
        var owner = MainWindow;
        if (owner is null) return false;

        var btnYes = new Button { Content = "Yes", MinWidth = 90 };
        var btnNo = new Button { Content = "No", MinWidth = 90 };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(btnNo);
        buttons.Children.Add(btnYes);

        var text = new TextBox { Text = message, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Height = 200 };
        ScrollViewer.SetVerticalScrollBarVisibility(text, ScrollBarVisibility.Auto);

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        panel.Children.Add(text);
        panel.Children.Add(buttons);

        var win = new Window
        {
            Title = title,
            Width = 620,
            Height = 360,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Topmost = false
        };
        win.RequestedThemeVariant = owner.ActualThemeVariant;

        var tcs = new TaskCompletionSource<bool>();
        btnYes.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        btnNo.Click += (_, _) => { win.Close(); tcs.TrySetResult(false); };
        // Safety net: if the user closes via the window's X button (or Alt+F4),
        // treat it as "No" so tcs.Task doesn't hang forever → app freeze.
        win.Closed += (_, _) => tcs.TrySetResult(false);

        await win.ShowDialog(owner);
        return await tcs.Task;
    }
}
