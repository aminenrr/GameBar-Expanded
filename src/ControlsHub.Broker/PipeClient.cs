using System.IO.Pipes;
using System.Text;
using ControlsHub.Protocol;

namespace ControlsHub.Broker;

/// <summary>
/// Pipe client for one app. Connects with a single attempt, and when the app is not there
/// waits 1 s, 2 s, 4 s ... up to 30 s before the next attempt (no tight loop).
/// Once connected everything is pushed: the reader blocks on overlapped IO.
/// </summary>
internal sealed class PipeClient
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly string _pipeName;
    private readonly SemaphoreSlim _write = new(1, 1);
    private NamedPipeClientStream? _stream;

    public PipeClient(string pipeName) { _pipeName = pipeName; }

    public string? App { get; private set; }
    public bool IsConnected => App != null;

    /// <summary>Called after hello for every message the app pushes (already parsed).</summary>
    public event Action<HubMessage>? Message;
    public event Action? Connected;
    public event Action? Disconnected;

    public async Task RunAsync(CancellationToken ct)
    {
        var backoff = MinBackoff;
        while (!ct.IsCancellationRequested)
        {
            var stream = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                stream.Connect(0); // one attempt, throws TimeoutException when the server is absent
            }
            catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException)
            {
                stream.Dispose();
                Log.Write($"pipe {_pipeName}: not available ({e.GetType().Name}), retry in {backoff.TotalSeconds:0} s");
                try { await Task.Delay(backoff, ct); } catch (OperationCanceledException) { break; }
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
                continue;
            }

            backoff = MinBackoff;
            _stream = stream;
            try
            {
                await SessionAsync(stream, ct);
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException or OperationCanceledException)
            {
            }
            finally
            {
                var wasUp = App != null;
                App = null;
                _stream = null;
                stream.Dispose();
                if (wasUp) Disconnected?.Invoke();
                Log.Write($"pipe {_pipeName}: closed");
            }
            if (ct.IsCancellationRequested) break;
            try { await Task.Delay(MinBackoff, ct); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task SessionAsync(NamedPipeClientStream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
        await SendAsync(new HelloMessage { Client = "ControlsHub" });
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) return;
            HubMessage msg;
            try { msg = HubProtocol.Parse(line); }
            catch (ProtocolException e) { Log.Write($"pipe {_pipeName}: dropped line ({e.Message})"); continue; }

            if (App == null)
            {
                if (msg is HelloMessage h && h.App != null && KeySchema.IsKnownApp(h.App))
                {
                    App = h.App;
                    Log.Write($"pipe {_pipeName}: hello from {h.App} {h.AppVersion} (protocol v{h.V})");
                    await SendAsync(new SubMessage());
                    Connected?.Invoke();
                }
                else
                {
                    Log.Write($"pipe {_pipeName}: expected hello from a known app, closing");
                    return;
                }
                continue;
            }
            Message?.Invoke(msg);
        }
    }

    public async Task<bool> SendAsync(HubMessage msg)
    {
        var s = _stream;
        if (s == null) return false;
        var bytes = Encoding.UTF8.GetBytes(HubProtocol.Serialize(msg) + "\n");
        await _write.WaitAsync();
        try
        {
            await s.WriteAsync(bytes);
            await s.FlushAsync();
            return true;
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException)
        {
            return false;
        }
        finally
        {
            _write.Release();
        }
    }
}
