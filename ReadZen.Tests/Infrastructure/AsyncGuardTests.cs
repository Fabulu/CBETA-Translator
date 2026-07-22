// AsyncGuardTests — pins the app-wide async-exception backstop (audit P2.5 / R4-M2).
//
// AsyncGuard.Run wraps a fire-and-forget `async void` handler so an exception thrown
// inside it is CONFINED to a debug log line plus an optional Failed callback, instead
// of escaping to the Avalonia dispatcher and crashing the whole app. These tests prove
// the two guarantees that make it a safety net:
//   1. a throwing handler is SWALLOWED (never rethrown, never crashes the process), and
//   2. the failure is ROUTED to the Failed sink with the exact context + the same
//      exception instance — for BOTH the sync-throw and post-await-throw paths.
// A successful handler must NOT raise Failed, and a Failed sink that itself throws must
// be contained (the sink "must never throw" contract) without wedging the guard.
//
// NOTE: AsyncGuard.Failed is a process-global static event. Only this class touches it,
// and xUnit runs a class's tests sequentially, so every test unsubscribes in a finally
// to leave the static clean for the next one.

using System;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Infrastructure")]
public class AsyncGuardTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Run_SuccessfulAction_RunsToCompletion_WithoutRaisingFailed()
    {
        var failedRaised = false;
        void Handler(string _, Exception __) => failedRaised = true;
        var ran = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        AsyncGuard.Failed += Handler;
        try
        {
            AsyncGuard.Run(async () => { await Task.Yield(); ran.SetResult(true); }, "View.Ok.Click");

            Assert.True(await ran.Task.WaitAsync(Timeout));
            // Give any (unexpected) Failed callback a chance to land before asserting it did not.
            await Task.Delay(50);
            Assert.False(failedRaised);
        }
        finally { AsyncGuard.Failed -= Handler; }
    }

    [Fact]
    public async Task Run_ActionThrowsAfterAwait_SwallowsAndRoutesToFailed()
    {
        var tcs = new TaskCompletionSource<(string ctx, Exception ex)>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(string c, Exception e) => tcs.TrySetResult((c, e));
        var boom = new InvalidOperationException("boom-after-await");

        AsyncGuard.Failed += Handler;
        try
        {
            // The call itself returns void and must not throw — the exception is confined.
            AsyncGuard.Run(async () => { await Task.Yield(); throw boom; }, "View.Save.Click");

            var (ctx, ex) = await tcs.Task.WaitAsync(Timeout);
            Assert.Equal("View.Save.Click", ctx);   // exact context string forwarded
            Assert.Same(boom, ex);                    // the SAME exception instance, not a wrapper
        }
        finally { AsyncGuard.Failed -= Handler; }
    }

    [Fact]
    public async Task Run_ActionThrowsSynchronouslyBeforeAwait_IsAlsoGuarded()
    {
        var tcs = new TaskCompletionSource<(string ctx, Exception ex)>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(string c, Exception e) => tcs.TrySetResult((c, e));
        var boom = new ArgumentException("boom-sync");

        AsyncGuard.Failed += Handler;
        try
        {
            // Func<Task> whose body throws before returning a Task (no await ever reached).
            AsyncGuard.Run(() => throw boom, "View.Sync.Click");

            var (ctx, ex) = await tcs.Task.WaitAsync(Timeout);
            Assert.Equal("View.Sync.Click", ctx);
            Assert.Same(boom, ex);
        }
        finally { AsyncGuard.Failed -= Handler; }
    }

    [Fact]
    public async Task Run_WithNoSubscriber_ThrowingAction_DoesNotCrash()
    {
        // No subscriber attached: Failed?.Invoke must null-guard. The throw is still
        // swallowed. We prove the guard survived by routing a subsequent failure through
        // a freshly-attached sink.
        AsyncGuard.Run(async () => { await Task.Yield(); throw new Exception("unobserved"); }, "no-sink");
        await Task.Delay(50);   // let the unobserved failure settle; a leak would crash here

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(string c, Exception _) => tcs.TrySetResult(c);
        AsyncGuard.Failed += Handler;
        try
        {
            AsyncGuard.Run(async () => { await Task.Yield(); throw new Exception("second"); }, "after-null-sink");
            Assert.Equal("after-null-sink", await tcs.Task.WaitAsync(Timeout));
        }
        finally { AsyncGuard.Failed -= Handler; }
    }

    [Fact]
    public async Task Run_WhenFailedSinkThrows_IsContained_AndGuardStaysUsable()
    {
        // The sink "must never throw" — but if it does, AsyncGuard swallows it and keeps
        // working. First route a failure through a sink that records-then-throws; then
        // route another through a well-behaved sink to prove the guard was not wedged.
        var firstReached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void ThrowingSink(string _, Exception __) { firstReached.TrySetResult(true); throw new Exception("sink boom"); }

        AsyncGuard.Failed += ThrowingSink;
        try
        {
            AsyncGuard.Run(async () => { await Task.Yield(); throw new Exception("first"); }, "ctx-1");
            Assert.True(await firstReached.Task.WaitAsync(Timeout));   // the throwing sink ran
        }
        finally { AsyncGuard.Failed -= ThrowingSink; }

        await Task.Delay(50);   // let the sink's own exception settle (it must be contained)

        var secondReached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void GoodSink(string _, Exception __) => secondReached.TrySetResult(true);
        AsyncGuard.Failed += GoodSink;
        try
        {
            AsyncGuard.Run(async () => { await Task.Yield(); throw new Exception("second"); }, "ctx-2");
            Assert.True(await secondReached.Task.WaitAsync(Timeout));   // guard still routes failures
        }
        finally { AsyncGuard.Failed -= GoodSink; }
    }
}
