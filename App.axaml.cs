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
            // Check for deep link BEFORE creating any window
            var startupUri = StartupArgs?.FirstOrDefault(a =>
                a.StartsWith(CbetaUriParser.Scheme + "://", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(startupUri))
            {
                // Deep link launch: don't show a primary window at all.
                // Create a hidden one (Avalonia requires MainWindow to be set).
                var hiddenWindow = new MainWindow();
                hiddenWindow.ShowInTaskbar = false;
                hiddenWindow.Opacity = 0;
                hiddenWindow.Width = 1;
                hiddenWindow.Height = 1;
                hiddenWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;
                hiddenWindow.Position = new Avalonia.PixelPoint(-9999, -9999);
                desktop.MainWindow = hiddenWindow;
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnLastWindowClose;

                // Handle the deep link immediately (no 5-second delay)
                Dispatcher.UIThread.Post(async () =>
                {
                    try { TryAutoRegisterProtocol(); } catch { }
                    try { await HandleDeepLinkAsync(startupUri); } catch { }
                    SetupPipeListener();
                }, DispatcherPriority.Background);
            }
            else
            {
                // Normal launch: show the primary window
                desktop.MainWindow = new MainWindow();

                Dispatcher.UIThread.Post(() =>
                {
                    try { TryAutoRegisterProtocol(); } catch { }
                    SetupPipeListener();
                }, DispatcherPriority.Background);
            }
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
}
