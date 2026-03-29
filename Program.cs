using Avalonia;
using System;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var singleInstance = new SingleInstanceManager();

        if (!singleInstance.TryAcquireOrForward(args))
        {
            // Another instance is already running; we forwarded our URI. Exit.
            singleInstance.Dispose();
            return;
        }

        App.SingleInstance = singleInstance;
        App.StartupArgs = args;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        singleInstance.Dispose();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
