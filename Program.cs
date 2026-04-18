using Avalonia;
using System;
using ReadZen.App.Services;
using Velopack;

namespace ReadZen.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack MUST run before anything else — it intercepts --veloapp-install /
        // --veloapp-update / --veloapp-restarted command-line flags and exits the
        // process early when appropriate. Calling this on builds that weren't packaged
        // with `vpk pack` is a no-op, so it's safe for zip-extract users too.
        try
        {
            // Run() reads command-line args from Environment internally; we don't
            // need to pass them through. This is a no-op on non-Velopack builds.
            VelopackApp.Build().Run();
        }
        catch
        {
            // Never block startup on a Velopack failure — the app must still run
            // from a plain zip extract even if Velopack's state is missing/corrupt.
        }

        SingleInstanceManager? singleInstance = null;

        try
        {
            singleInstance = new SingleInstanceManager();

            if (!singleInstance.TryAcquireOrForward(args))
            {
                // Another instance is already running; we forwarded our URI. Exit.
                singleInstance.Dispose();
                return;
            }

            App.SingleInstance = singleInstance;
        }
        catch
        {
            // Single-instance check failed — proceed anyway.
            // Deep links from second instances won't work, but the app will launch.
        }

        App.StartupArgs = args;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        try { singleInstance?.Dispose(); } catch { }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
