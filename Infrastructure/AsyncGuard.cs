// Infrastructure/AsyncGuard.cs
using System;
using System.Threading.Tasks;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Fire-and-forget wrapper for async UI event handlers (audit P2.5 / R4-M2).
/// An exception thrown inside an <c>async (_, _) =&gt; await ...</c> handler escapes to
/// the dispatcher and crashes the whole app; routing the handler through
/// <see cref="Run"/> confines the failure to a log line and an optional status
/// callback instead. Usage:
/// <code>btn.Click += (_, _) =&gt; AsyncGuard.Run(async () =&gt; await DoAsync(), "View.btn.Click");</code>
/// </summary>
public static class AsyncGuard
{
    /// <summary>
    /// Central failure sink. MainWindow subscribes to surface failures in the status
    /// bar; without a subscriber failures still go to the debug log.
    /// </summary>
    public static event Action<string, Exception>? Failed;

    public static async void Run(Func<Task> action, string context)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AsyncGuard] {context} failed: {ex}");
            try { Failed?.Invoke(context, ex); } catch { /* the sink must never throw */ }
        }
    }
}
