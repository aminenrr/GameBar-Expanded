using System.Collections.Generic;

namespace ControlsHub.Protocol
{
    /// <summary>Base of every pipe message. One message is one UTF 8 JSON line.</summary>
    public abstract class HubMessage
    {
        public abstract string Type { get; }
    }

    /// <summary>{"t":"hello",...}. The broker sends Client, the app answers with App and Version.</summary>
    public sealed class HelloMessage : HubMessage
    {
        public override string Type => "hello";
        public string? Client { get; set; }
        public string? App { get; set; }
        public int V { get; set; } = HubProtocol.Version;
        public string? AppVersion { get; set; }
    }

    /// <summary>{"t":"sub"}: start pushing state.</summary>
    public sealed class SubMessage : HubMessage
    {
        public override string Type => "sub";
    }

    /// <summary>{"t":"state",...}: full state once after sub, then only changed keys.</summary>
    public sealed class StateMessage : HubMessage
    {
        public override string Type => "state";
        public Dictionary<string, object?> Values { get; } = new Dictionary<string, object?>();
    }

    /// <summary>{"t":"set","key":...,"value":...,"quiet":true,"id":7}</summary>
    public sealed class SetMessage : HubMessage
    {
        public override string Type => "set";
        public string Key { get; set; } = "";
        public object? Value { get; set; }
        public bool Quiet { get; set; }
        public int Id { get; set; }
    }

    /// <summary>{"t":"ack","id":7,"ok":true}</summary>
    public sealed class AckMessage : HubMessage
    {
        public override string Type => "ack";
        public int Id { get; set; }
        public bool Ok { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>App to broker: {"t":"nav","btn":"cross","down":true}</summary>
    public sealed class NavEventMessage : HubMessage
    {
        public override string Type => "nav";
        public string Btn { get; set; } = "";
        public bool Down { get; set; } = true;
    }

    /// <summary>Broker to app: {"t":"nav","on":true}. Nav events flow only while on.</summary>
    public sealed class NavControlMessage : HubMessage
    {
        public override string Type => "nav";
        public bool On { get; set; }
    }

    /// <summary>{"t":"watch","sensors":true}: sensors pushed only while watched.</summary>
    public sealed class WatchMessage : HubMessage
    {
        public override string Type => "watch";
        public bool Sensors { get; set; }
    }

    /// <summary>{"t":"open"}: show the app's main window.</summary>
    public sealed class OpenMessage : HubMessage
    {
        public override string Type => "open";
    }

    /// <summary>A JSON array or object value kept as raw text (outputs list, prox block).</summary>
    public sealed class JsonRaw
    {
        public JsonRaw(string json) { Json = json; }
        public string Json { get; }
        public override string ToString() => Json;
        public override bool Equals(object? obj) => obj is JsonRaw r && r.Json == Json;
        public override int GetHashCode() => Json.GetHashCode();
    }

    public sealed class ProtocolException : System.Exception
    {
        public ProtocolException(string message) : base(message) { }
    }
}
