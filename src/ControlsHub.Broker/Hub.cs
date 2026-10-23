using System.Globalization;
using ControlsHub.Protocol;
using Windows.Foundation.Collections;

namespace ControlsHub.Broker;

/// <summary>
/// Holds the last known state, connects the pipe client to the widget link.
/// P0 talks to one app (the dummy); P1 adds Alien, Dolby and DualSense pipes.
/// </summary>
internal sealed class Hub
{
    private readonly object _gate = new();
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
    private readonly PipeClient _pipe;
    private readonly WidgetLink? _widget;
    private readonly bool _consoleWantsNav;
    private int _changes;
    private int _nextId;
    private bool _navSent;

    public Hub(string pipeName, WidgetLink? widget, bool consoleWantsNav)
    {
        _pipe = new PipeClient(pipeName);
        _widget = widget;
        _consoleWantsNav = consoleWantsNav;
        _pipe.Connected += OnPipeConnected;
        _pipe.Disconnected += OnPipeDisconnected;
        _pipe.Message += OnPipeMessage;
        if (_widget != null)
        {
            _widget.ConnectedChanged += OnWidgetConnectedChanged;
            _widget.Request += OnWidgetRequest;
        }
    }

    public int Changes => Volatile.Read(ref _changes);

    public Task RunAsync(CancellationToken ct) => _pipe.RunAsync(ct);

    private bool NavWanted => _consoleWantsNav || (_widget?.IsConnected ?? false);

    private void OnPipeConnected()
    {
        _navSent = false;
        _ = SyncNavAsync();
        _ = SendToWidget(PipeStatus());
    }

    private void OnPipeDisconnected()
    {
        _ = SendToWidget(PipeStatus());
    }

    private void OnPipeMessage(HubMessage msg)
    {
        switch (msg)
        {
            case StateMessage s:
            {
                var rejected = new List<string>();
                var ok = KeySchema.Filter(_pipe.App!, s, rejected);
                if (rejected.Count > 0) Log.Write("state: rejected keys " + string.Join(",", rejected));
                if (ok.Values.Count == 0) return;
                var toWidget = new ValueSet { ["t"] = "state" };
                int count;
                lock (_gate)
                {
                    foreach (var kv in ok.Values)
                    {
                        _state[kv.Key] = kv.Value;
                        toWidget[kv.Key] = HubProtocol.FormatValue(kv.Value);
                    }
                    count = ++_changes;
                }
                Log.Write($"state {string.Join(" ", ok.Values.Select(kv => kv.Key + "=" + HubProtocol.FormatValue(kv.Value)))} (change {count})");
                if (count % 50 == 0) Log.Write($"working set {Log.WorkingSet()} after {count} state changes");
                _ = SendToWidget(toWidget);
                break;
            }
            case NavEventMessage n:
                Log.Write($"nav {n.Btn} {(n.Down ? "down" : "up")}");
                _ = SendToWidget(new ValueSet { ["t"] = "nav", ["btn"] = n.Btn, ["down"] = n.Down });
                break;
            case AckMessage a:
                Log.Write($"ack id={a.Id} ok={a.Ok}{(a.Error != null ? " error=" + a.Error : "")}");
                var vs = new ValueSet { ["t"] = "ack", ["id"] = a.Id, ["ok"] = a.Ok };
                if (a.Error != null) vs["error"] = a.Error;
                _ = SendToWidget(vs);
                break;
            default:
                Log.Write("pipe: ignored message " + msg.Type);
                break;
        }
    }

    private void OnWidgetConnectedChanged(bool connected)
    {
        _ = SyncNavAsync();
        if (!connected) return;
        // Fresh widget: send the whole cached state and the pipe status.
        var snapshot = new ValueSet { ["t"] = "state" };
        lock (_gate)
        {
            foreach (var kv in _state) snapshot[kv.Key] = HubProtocol.FormatValue(kv.Value);
        }
        _ = SendToWidget(PipeStatus());
        if (snapshot.Count > 1) _ = SendToWidget(snapshot);
    }

    private void OnWidgetRequest(ValueSet request)
    {
        var t = request.TryGetValue("t", out var tv) ? tv as string : null;
        switch (t)
        {
            case "set":
            {
                var key = request.TryGetValue("key", out var k) ? k as string : null;
                var value = request.TryGetValue("value", out var v) ? v : null;
                if (key == null) return;
                var set = new SetMessage { Key = key, Value = ToProtocolValue(value), Quiet = true, Id = Interlocked.Increment(ref _nextId) };
                var app = _pipe.App;
                if (app == null)
                {
                    Log.Write($"widget: set {key} dropped, app not connected");
                    return;
                }
                if (!KeySchema.TryValidateSet(app, set, out var error))
                {
                    Log.Write($"widget: set {key} rejected: {error}");
                    return;
                }
                Log.Write($"widget: set {key}={HubProtocol.FormatValue(set.Value)} id={set.Id}");
                _ = _pipe.SendAsync(set);
                break;
            }
            case "hello":
                OnWidgetConnectedChanged(true);
                break;
            default:
                Log.Write("widget: ignored request " + (t ?? "(none)"));
                break;
        }
    }

    private async Task SyncNavAsync()
    {
        var want = NavWanted;
        if (!_pipe.IsConnected) return;
        if (_navSent && want == _lastNav) return;
        _navSent = true;
        _lastNav = want;
        await _pipe.SendAsync(new NavControlMessage { On = want });
        Log.Write("nav stream " + (want ? "on" : "off"));
    }

    private bool _lastNav;

    private ValueSet PipeStatus() => new()
    {
        ["t"] = "pipe",
        ["connected"] = _pipe.IsConnected,
        ["app"] = _pipe.App ?? "",
    };

    private Task SendToWidget(ValueSet vs) => _widget?.IsConnected == true ? _widget.SendAsync(vs) : Task.CompletedTask;

    private static object? ToProtocolValue(object? v) => v switch
    {
        int i => (long)i,
        uint u => (long)u,
        float f => (double)f,
        string s when long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) => l,
        _ => v,
    };
}
