using Avalonia;
using System;
using System.Linq;
using System.Reflection;
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
        // Headless tool mode: regenerate segment maps and exit (the C# replacement
        // for the retired eng/tools/build-structural-segments.js — audit P3.1c 2/2).
        // Must run before Velopack/single-instance/Avalonia: no UI, no side effects.
        if (args.Length > 0 && args[0] == "--build-segments")
        {
            Environment.Exit(SegmentMapGenerator.Run(args, Console.Out));
            return;
        }

        // Headless tool mode: full-rebuild a shippable search index for a corpus into an
        // output dir, then exit (S7 bundled prebuilt index). CI runs this to stage the
        // artifacts into Assets/PrebuiltIndex before publish; the app then seeds them into a
        // virgin user index root on first run. Must run before Velopack/single-instance/
        // Avalonia: no UI, no side effects beyond the output dir.
        if (args.Length > 0 && args[0] == "--build-search-index")
        {
            Environment.Exit(SearchIndexService.RunHeadlessBuild(args, Console.Out));
            return;
        }

        // Headless tool mode: print the current search index family BuildGuids and exit
        // (S7 bundled prebuilt index, PR-S3). CI's release workflow calls this on the release
        // binary itself to (a) assert the staged bundle manifest guid == the binary's guid and
        // (b) drive the guid-bundle-guard (a guid bump with no matching bundle fails the tag).
        // Must run before Velopack/single-instance/Avalonia: no UI, no side effects.
        if (args.Length > 0 && args[0] == "--print-build-guid")
        {
            Environment.Exit(SearchIndexService.RunPrintBuildGuid(Console.Out));
            return;
        }

        // Global exception handlers — catch crashes that happen during rendering
        // or on background threads, so Linux users see the real error, not just
        // "Dispatcher shut down".
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.Error.WriteLine($"FATAL UNHANDLED: {e.ExceptionObject}");
            Console.Error.Flush();
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.Error.WriteLine($"UNOBSERVED TASK: {e.Exception}");
            Console.Error.Flush();
        };

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
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        // Set AFTER .LogToTrace() so our sink takes precedence (LogToTrace may overwrite)
        Avalonia.Logging.Logger.Sink = new StderrLogSink();

        // On Linux, configure X11 with software rendering fallback and disable
        // DBus menu to avoid Wayland dispatcher shutdown crashes (Issue #19523).
        // Uses reflection because Avalonia.X11 types are only available at runtime
        // on Linux (not at compile time on Windows).
        if (OperatingSystem.IsLinux())
        {
            try
            {
                var x11OptionsType = Type.GetType("Avalonia.X11.X11PlatformOptions, Avalonia.X11");
                var renderModeType = Type.GetType("Avalonia.X11.X11RenderingMode, Avalonia.X11");
                if (x11OptionsType != null && renderModeType != null)
                {
                    var options = Activator.CreateInstance(x11OptionsType)!;

                    // RenderingMode = [Glx, Software]
                    var glx = Enum.Parse(renderModeType, "Glx");
                    var sw = Enum.Parse(renderModeType, "Software");
                    var arr = Array.CreateInstance(renderModeType, 2);
                    arr.SetValue(glx, 0);
                    arr.SetValue(sw, 1);
                    x11OptionsType.GetProperty("RenderingMode")?.SetValue(options, arr);

                    // UseDBusMenu = false
                    x11OptionsType.GetProperty("UseDBusMenu")?.SetValue(options, false);

                    // builder.With(options) — call the generic With<T> method
                    var withMethod = typeof(AppBuilder).GetMethods()
                        .Where(m => m.Name == "With" && m.IsGenericMethod)
                        .FirstOrDefault();
                    if (withMethod != null)
                    {
                        var generic = withMethod.MakeGenericMethod(x11OptionsType);
                        builder = (AppBuilder)generic.Invoke(builder, new[] { options })!;
                    }
                }
            }
            catch
            {
                // X11 types may not be available on some configurations — proceed without
            }
        }

        return builder;
    }
}

/// <summary>
/// Writes Avalonia's internal log messages (errors, warnings) to stderr.
/// On Linux, the X11 backend logs the real crash cause before the dispatcher
/// shuts down — but only if a sink is attached.
/// </summary>
internal sealed class StderrLogSink : Avalonia.Logging.ILogSink
{
    public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area)
        => level >= Avalonia.Logging.LogEventLevel.Fatal;

    public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate)
    {
        try { Console.Error.WriteLine($"[Avalonia {level}] {area}: {messageTemplate}"); Console.Error.Flush(); } catch { }
    }

    public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        try
        {
            // Don't try string.Format — Avalonia uses {PropertyName} not {0} style
            var vals = propertyValues != null ? string.Join(", ", propertyValues) : "";
            Console.Error.WriteLine($"[Avalonia {level}] {area}: {messageTemplate} [{vals}]");
            Console.Error.Flush();
        }
        catch { }
    }
}
