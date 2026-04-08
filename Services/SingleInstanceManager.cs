using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

/// <summary>
/// Mutex-based single-instance detection with named-pipe IPC.
/// The first instance acquires the mutex and listens for URIs on a named pipe.
/// Subsequent instances forward their <c>zen://</c> URI argument and exit.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "ReadZen_SingleInstance";
    private const string PipeName = "ReadZen_DeepLink";

    private Mutex? _mutex;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Raised on the pipe-listener thread when a second instance sends a URI.
    /// Subscribers must marshal to the UI thread if needed.
    /// </summary>
    public event Action<string>? UriReceived;

    /// <summary>
    /// Tries to become the single instance. If another instance already owns the mutex,
    /// forwards the first <c>zen://</c> argument via named pipe and returns <c>false</c>.
    /// </summary>
    /// <returns><c>true</c> if this is the first instance; <c>false</c> if a URI was forwarded.</returns>
    public bool TryAcquireOrForward(string[] args)
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (createdNew)
                return true;

            // The mutex exists â€” try to acquire it in case the previous owner crashed
            try
            {
                if (_mutex.WaitOne(500))
                    return true; // Previous instance was dead (abandoned mutex), we now own it
            }
            catch (AbandonedMutexException)
            {
                return true; // Previous instance crashed, we now own the mutex
            }
        }
        catch (Exception)
        {
            // Mutex creation failed entirely â€” proceed as single instance
            return true;
        }

        // Second instance â€” forward the zen:// URI to the first instance and signal exit.
        var cbetaArg = args.FirstOrDefault(a =>
            a.StartsWith(CbetaUriParser.Scheme + "://", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(cbetaArg))
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(timeout: 3000);
                using var writer = new StreamWriter(client);
                writer.Write(cbetaArg);
                writer.Flush();
            }
            catch
            {
                // Best-effort; the first instance may not be listening yet.
            }
        }

        return false;
    }

    /// <summary>
    /// Starts a background loop that listens for URIs from second instances via named pipe.
    /// Each received URI raises <see cref="UriReceived"/>.
    /// </summary>
    public void StartListening()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);

                    using var reader = new StreamReader(server);
                    var uri = await reader.ReadToEndAsync(token);

                    if (!string.IsNullOrWhiteSpace(uri))
                        UriReceived?.Invoke(uri.Trim());
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Pipe error â€” brief pause then retry.
                    try { await Task.Delay(500, token); } catch { break; }
                }
            }
        }, token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* not held */ }
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
