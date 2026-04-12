using System;
using System.Threading;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

namespace ReadZen.Tests;

/// <summary>
/// Provides a running Avalonia headless dispatcher for tests that need
/// Dispatcher.UIThread to pump (e.g. ViewModels that post search results).
/// Runs a manual dispatcher pump on a dedicated background thread.
/// </summary>
public static class AvaloniaTestInfrastructure
{
    private static readonly Lazy<bool> _init = new(InitCore, LazyThreadSafetyMode.ExecutionAndPublication);

    private static bool InitCore()
    {
        var ready = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();

            ready.Set();

            // Pump the dispatcher by repeatedly running pending jobs.
            while (true)
            {
                try { Dispatcher.UIThread.RunJobs(); }
                catch { /* ignore pump errors */ }
                Thread.Sleep(5);
            }
        })
        {
            IsBackground = true,
            Name = "Avalonia-Test-UIThread"
        };
        thread.Start();
        ready.Wait();
        return true;
    }

    /// <summary>
    /// Ensures the headless Avalonia platform and dispatcher are running.
    /// Idempotent; safe to call from any thread.
    /// </summary>
    public static void EnsureInitialized() => _ = _init.Value;
}
