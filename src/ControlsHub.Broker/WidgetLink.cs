using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace ControlsHub.Broker;

/// <summary>
/// AppService link to the widget ("ControlsHub.Broker", hosted in process by the widget app).
/// The broker opens it only when the widget asks (the widget launches the broker with --connect),
/// so a closed Game Bar costs nothing. Messages are ValueSets with a "t" key.
/// </summary>
internal sealed class WidgetLink
{
    public const string ServiceName = "ControlsHub.Broker";

    private readonly SemaphoreSlim _openGate = new(1, 1);
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly object _connLock = new();
    private AppServiceConnection? _conn;

    public bool IsConnected { get { lock (_connLock) return _conn != null; } }

    public event Action<ValueSet>? Request;
    public event Action<bool>? ConnectedChanged;

    public async Task ConnectAsync()
    {
        await _openGate.WaitAsync();
        try
        {
            if (IsConnected) return;
            var c = new AppServiceConnection
            {
                AppServiceName = ServiceName,
                PackageFamilyName = Package.Current.Id.FamilyName,
            };
            c.RequestReceived += OnRequestReceived;
            c.ServiceClosed += OnServiceClosed;
            var status = await c.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                Log.Write($"widget: AppService open failed: {status}");
                c.Dispose();
                return;
            }
            lock (_connLock) _conn = c;
            Log.Write("widget: AppService connected");
        }
        finally
        {
            _openGate.Release();
        }
        ConnectedChanged?.Invoke(true);
    }

    public async Task SendAsync(ValueSet message)
    {
        // One at a time so nav events arrive in order.
        await _sendGate.WaitAsync();
        try
        {
            AppServiceConnection? c;
            lock (_connLock) c = _conn;
            if (c == null) return;
            var r = await c.SendMessageAsync(message);
            if (r.Status != AppServiceResponseStatus.Success)
                Log.Write($"widget: send failed: {r.Status}");
        }
        catch (Exception e)
        {
            Log.Write("widget: send error: " + e.Message);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
    {
        var d = args.GetDeferral();
        try { Request?.Invoke(args.Request.Message); }
        finally { d.Complete(); }
    }

    private void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
    {
        Log.Write($"widget: AppService closed ({args.Status})");
        lock (_connLock)
        {
            if (_conn == sender) _conn = null;
        }
        ConnectedChanged?.Invoke(false);
    }
}
