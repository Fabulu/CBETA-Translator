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

    /// <summary>
    /// Single-instance manager created in <see cref="Program.Main"/>.
    /// </summary>
    public static SingleInstanceManager? SingleInstance { get; set; }

    /// <summary>
    /// Command-line arguments captured in <see cref="Program.Main"/>.
    /// </summary>
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
            desktop.MainWindow = new MainWindow();

            // Defer non-critical startup tasks to avoid blocking window display
            Dispatcher.UIThread.Post(() =>
            {
                try { TryAutoRegisterProtocol(); } catch { }
                try { HandleStartupUri(); } catch { }
                try
                {
                    if (SingleInstance != null)
                    {
                        SingleInstance.UriReceived += uri =>
                        {
                            try { HandleDeepLink(uri); } catch { }
                        };
                        SingleInstance.StartListening();
                    }
                }
                catch { }
            }, DispatcherPriority.Background);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TryAutoRegisterProtocol()
    {
        // Run entirely in background to never block startup
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var configService = Services.GetService<IAppConfigService>();
                if (configService is AppConfigService acs)
                {
                    var config = await acs.TryLoadAsync();
                    if (config != null && !config.HasRegisteredProtocolHandler)
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

    private void HandleStartupUri()
    {
        var uri = StartupArgs?.FirstOrDefault(a =>
            a.StartsWith(CbetaUriParser.Scheme + "://", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(uri))
        {
            // Defer deep link handling to let primary window finish initialization
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(5000); // Wait for primary window to be ready
                HandleDeepLink(uri);
            });
        }
    }

    private void HandleDeepLink(string uri)
    {
        var request = CbetaUriParser.TryParse(uri);
        if (request == null)
            return;

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                // Read the text root from config (async, no UI thread blocking)
                var configService = Services.GetService<IAppConfigService>();
                string? root = null;
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

                // Open in a secondary window to avoid conflicting with primary window state
                WindowNavigationService.OpenAndNavigate(root, request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Deep link handling failed: " + ex.Message);
            }
        }, DispatcherPriority.Background);
    }
}