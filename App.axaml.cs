using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static SingleInstanceManager? SingleInstance { get; set; }
    public static string[]? StartupArgs { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Global CJK font for LiveCharts2 tooltips and labels
        var cjkTypeface = SKTypeface.FromFamilyName(
            OperatingSystem.IsWindows() ? "Microsoft YaHei" :
            OperatingSystem.IsMacOS() ? "PingFang SC" : "Noto Sans CJK SC");
        LiveCharts.Configure(config => config.HasGlobalSKTypeface(cjkTypeface));

        var sc = new ServiceCollection();
        sc.AddAppServices();
        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Always create and show the primary window (normal flow)
            desktop.MainWindow = new MainWindow();

            // Check for deep link after window is created
            var startupUri = StartupArgs?.FirstOrDefault(a =>
                ZenUriParser.TryParseDeepLink(a) != null);

            Dispatcher.UIThread.Post(async () =>
            {
                try { TryAutoRegisterProtocol(); } catch { }
                SetupPipeListener();

                // If launched via deep link, navigate in the primary window directly
                if (!string.IsNullOrEmpty(startupUri))
                {
                    try { await HandleDeepLinkInPrimaryAsync(startupUri); } catch { }
                }
            }, DispatcherPriority.Background);
        }

        // Non-blocking update check
        _ = CheckForUpdateAsync();

        base.OnFrameworkInitializationCompleted();
    }

    private async System.Threading.Tasks.Task CheckForUpdateAsync()
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(5000); // Don't slow down startup

            var updater = Services.GetService<AppUpdateService>();
            if (updater == null) return;

            var result = await updater.CheckForUpdatesAsync();
            if (result.AvailableVersion == null) return; // no update available

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow is Views.MainWindow mainWin)
                {
                    mainWin.ShowUpdateNotification(result, updater);
                }
            });
        }
        catch
        {
            // Never crash on update check failure -- network may be unavailable
        }
    }

    private void SetupPipeListener()
    {
        try
        {
            if (SingleInstance != null)
            {
                SingleInstance.UriReceived += uri =>
                {
                    try { Dispatcher.UIThread.Post(async () => await HandleDeepLinkAsync(uri)); }
                    catch { }
                };
                SingleInstance.StartListening();
            }
        }
        catch { }
    }

    private void TryAutoRegisterProtocol()
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var configService = Services.GetService<IAppConfigService>();
                if (configService is AppConfigService acs)
                {
                    var config = await acs.TryLoadAsync() ?? new AppConfig();
                    // Always re-register if the current scheme isn't registered
                    // (handles scheme rename from cbeta:// to zen://)
                    if (!config.HasRegisteredProtocolHandler || !ProtocolRegistrationService.IsRegistered())
                    {
                        ProtocolRegistrationService.Register();
                        config.HasRegisteredProtocolHandler = true;
                        await acs.SaveAsync(config);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Protocol auto-registration failed: " + ex.Message);
            }
        });
    }

    private async System.Threading.Tasks.Task HandleDeepLinkAsync(string uri)
    {
        var deepLink = ZenUriParser.TryParseDeepLink(uri);
        if (deepLink == null) return;

        try
        {
            if (deepLink.Kind == DeepLinkKind.Passage && deepLink.Passage != null)
            {
                string? root = null;
                var configService = Services.GetService<IAppConfigService>();
                if (configService is AppConfigService acs)
                {
                    var config = await acs.TryLoadAsync();
                    root = config?.TextRootPath;
                }

                if (string.IsNullOrEmpty(root))
                {
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        && desktop.MainWindow?.DataContext is ViewModels.MainWindowViewModel vm)
                    {
                        vm.SetStatus("To open this link, first download texts via the Community tab.");
                    }
                    return;
                }

                WindowNavigationService.OpenAndNavigate(root, deepLink.Passage);
            }
            else
            {
                // Non-passage deep links: route to the primary window
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow is Views.MainWindow mainWin)
                {
                    await mainWin.HandleDeepLinkAsync(deepLink);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Deep link handling failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Handles a deep link by navigating the primary window directly.
    /// Waits for the primary window to finish loading before navigating.
    /// </summary>
    private async System.Threading.Tasks.Task HandleDeepLinkInPrimaryAsync(string uri)
    {
        var deepLink = ZenUriParser.TryParseDeepLink(uri);
        if (deepLink == null) return;

        try
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow is not Views.MainWindow mainWin)
                return;

            // Wait for the primary window to finish its initial load
            // (config, auto-load, index cache, etc.)
            // Poll quickly so deep-link navigation doesn't add a perceptible delay.
            for (int i = 0; i < 400; i++) // max ~20 seconds at 50ms intervals
            {
                if (!string.IsNullOrWhiteSpace(mainWin.ViewModel?.Root))
                    break;
                await System.Threading.Tasks.Task.Delay(50);
            }

            var root = mainWin.ViewModel?.Root;
            if (string.IsNullOrEmpty(root))
            {
                // No texts downloaded yet — show a friendly message
                Dispatcher.UIThread.Post(() =>
                {
                    mainWin.ViewModel?.SetStatus(
                        "Someone shared a link with you! To view it, first download the text collection using the Git tab's Sync button.");
                });
                return;
            }

            if (deepLink.Kind == DeepLinkKind.Passage && deepLink.Passage != null)
            {
                // Navigate in the primary window (existing passage behavior)
                await mainWin.OpenAtAsync(root, deepLink.Passage);
            }
            else
            {
                // Non-passage deep links
                await mainWin.HandleDeepLinkAsync(deepLink);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Deep link in primary failed: " + ex.Message);
        }
    }
}
