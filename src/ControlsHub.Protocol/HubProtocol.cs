using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ControlsHub.Protocol
{
    /// <summary>
    /// JSON lines codec. Uses only Utf8JsonWriter and JsonDocument (no reflection),
    /// so it is safe under NativeAOT and trimming.
    /// </summary>
    public static class HubProtocol
    {
        public const int Version = 1;

        /// <summary>Longest accepted line in characters. Anything longer is dropped.</summary>
        public const int MaxLineLength = 64 * 1024;

        public static readonly string[] NavButtons =
        {
            "l1", "r1", "up", "down", "left", "right", "cross", "circle", "square", "triangle", "options",
        };

        public static string Serialize(HubMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            using (var ms = new MemoryStream())
            {
                using (var w = new Utf8JsonWriter(ms))
                {
                    w.WriteStartObject();
                    w.WriteString("t", message.Type);
                    switch (message)
                    {
                        case HelloMessage h:
                            if (h.Client != null) w.WriteString("client", h.Client);
                            if (h.App != null) w.WriteString("app", h.App);
                            w.WriteNumber("v", h.V);
                            if (h.AppVersion != null) w.WriteString("version", h.AppVersion);
                            break;
                        case StateMessage s:
                            foreach (var kv in s.Values)
                            {
                                w.WritePropertyName(kv.Key);
                                WriteValue(w, kv.Value);
                            }
                            break;
                        case SetMessage set:
                            w.WriteString("key", set.Key);
                            w.WritePropertyName("value");
                            WriteValue(w, set.Value);
                            if (set.Quiet) w.WriteBoolean("quiet", true);
                            w.WriteNumber("id", set.Id);
                            break;
                        case AckMessage a:
                            w.WriteNumber("id", a.Id);
                            w.WriteBoolean("ok", a.Ok);
                            if (a.Error != null) w.WriteString("error", a.Error);
                            break;
                        case NavEventMessage n:
                            w.WriteString("btn", n.Btn);
                            w.WriteBoolean("down", n.Down);
                            break;
                        case NavControlMessage nc:
                            w.WriteBoolean("on", nc.On);
                            break;
                        case WatchMessage wm:
                            w.WriteBoolean("sensors", wm.Sensors);
                            break;
                    }
                    w.WriteEndObject();
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static HubMessage Parse(string line)
        {
            if (line == null) throw new ProtocolException("null line");
            if (line.Length > MaxLineLength) throw new ProtocolException("line too long");
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 8 });
            }
            catch (JsonException e)
            {
                throw new ProtocolException("bad json: " + e.Message);
            }
            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) throw new ProtocolException("not an object");
                if (!root.TryGetProperty("t", out var tEl) || tEl.ValueKind != JsonValueKind.String)
                    throw new ProtocolException("missing t");
                var t = tEl.GetString();
                switch (t)
                {
                    case "hello":
                        return new HelloMessage
                        {
                            Client = OptString(root, "client"),
                            App = OptString(root, "app"),
                            V = OptInt(root, "v") ?? 0,
                            AppVersion = OptString(root, "version"),
                        };
                    case "sub":
                        return new SubMessage();
                    case "open":
                        return new OpenMessage();
                    case "state":
                    {
                        var s = new StateMessage();
                        foreach (var p in root.EnumerateObject())
                        {
                            if (p.Name == "t") continue;
                            s.Values[p.Name] = ReadValue(p.Value);
                        }
                        return s;
                    }
                    case "set":
                    {
                        var key = OptString(root, "key");
                        if (string.IsNullOrEmpty(key)) throw new ProtocolException("set without key");
                        if (!root.TryGetProperty("value", out var v)) throw new ProtocolException("set without value");
                        return new SetMessage
                        {
                            Key = key!,
                            Value = ReadValue(v),
                            Quiet = OptBool(root, "quiet") ?? false,
                            Id = OptInt(root, "id") ?? 0,
                        };
                    }
                    case "ack":
                        return new AckMessage
                        {
                            Id = OptInt(root, "id") ?? 0,
                            Ok = OptBool(root, "ok") ?? false,
                            Error = OptString(root, "error"),
                        };
                    case "nav":
                    {
                        var btn = OptString(root, "btn");
                        if (btn != null)
                        {
                            if (Array.IndexOf(NavButtons, btn) < 0) throw new ProtocolException("unknown nav button: " + btn);
                            return new NavEventMessage { Btn = btn, Down = OptBool(root, "down") ?? true };
                        }
                        var on = OptBool(root, "on");
                        if (on == null) throw new ProtocolException("nav without btn or on");
                        return new NavControlMessage { On = on.Value };
                    }
                    case "watch":
                        return new WatchMessage { Sensors = OptBool(root, "sensors") ?? false };
                    default:
                        throw new ProtocolException("unknown message type: " + t);
                }
            }
        }

        /// <summary>Formats a value for logs and the widget (strings plain, numbers invariant).</summary>
        public static string FormatValue(object? value)
        {
            switch (value)
            {
                case null: return "null";
                case string s: return s;
                case bool b: return b ? "true" : "false";
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case JsonRaw r: return r.Json;
                default: return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            }
        }

        private static void WriteValue(Utf8JsonWriter w, object? value)
        {
            switch (value)
            {
                case null: w.WriteNullValue(); break;
                case string s: w.WriteStringValue(s); break;
                case bool b: w.WriteBooleanValue(b); break;
                case int i: w.WriteNumberValue(i); break;
                case long l: w.WriteNumberValue(l); break;
                case float f: w.WriteNumberValue(f); break;
                case double d: w.WriteNumberValue(d); break;
                case JsonRaw r:
                    using (var doc = JsonDocument.Parse(r.Json)) doc.RootElement.WriteTo(w);
                    break;
                default: throw new ProtocolException("unsupported value type: " + value.GetType().Name);
            }
        }

        private static object? ReadValue(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.String: return e.GetString();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                case JsonValueKind.Number:
                    if (e.TryGetInt64(out var l)) return l;
                    return e.GetDouble();
                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    return new JsonRaw(e.GetRawText());
                default: throw new ProtocolException("unsupported json value");
            }
        }

        private static string? OptString(JsonElement root, string name) =>
            root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

        private static int? OptInt(JsonElement root, string name) =>
            root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i) ? i : (int?)null;

        private static bool? OptBool(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var e)) return null;
            if (e.ValueKind == JsonValueKind.True) return true;
            if (e.ValueKind == JsonValueKind.False) return false;
            return null;
        }
    }
}
