using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App;

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
        var sc = new ServiceCollection();
        sc.AddAppServices();
        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Always create and show the primary window (normal flow)
            desktop.MainWindow = new MainWindow();

            // Check for deep link after window is created
            var startupUri = StartupArgs?.FirstOrDefault(a =>
                a.StartsWith(CbetaUriParser.Scheme + "://", StringComparison.OrdinalIgnoreCase) ||
                a.StartsWith(CbetaUriParser.ShareableBase, StringComparison.OrdinalIgnoreCase));

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

        base.OnFrameworkInitializationCompleted();
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
                    var config = await acs.TryLoadAsync();
                    // Always re-register if the current scheme isn't registered
                    // (handles scheme rename from cbeta:// to zen://)
                    if (config != null && (!config.HasRegisteredProtocolHandler || !ProtocolRegistrationService.IsRegistered()))
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
        var request = CbetaUriParser.TryParse(uri);
        if (request == null) return;

        try
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
                Debug.WriteLine("Cannot handle deep link: no TextRootPath configured.");
                return;
            }

            WindowNavigationService.OpenAndNavigate(root, request);
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
        var request = CbetaUriParser.TryParse(uri);
        if (request == null) return;

        try
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow is not Views.MainWindow mainWin)
                return;

            // Wait for the primary window to finish its initial load
            // (config, auto-load, index build, etc.)
            // Poll until Root is set (config loaded and auto-loaded)
            for (int i = 0; i < 30; i++) // max ~21 seconds
            {
                await System.Threading.Tasks.Task.Delay(500);
                if (!string.IsNullOrWhiteSpace(mainWin.ViewModel?.Root))
                    break;
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

            // Navigate in the primary window
            await mainWin.OpenAtAsync(root, request);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Deep link in primary failed: " + ex.Message);
        }
    }
}
