using System;
using System.Collections.Generic;

namespace ControlsHub.Protocol
{
    public enum KeyKind { Enum, Bool, Int, Number, Raw, Action }

    /// <summary>One allowed key: its kind and its limits.</summary>
    public sealed class KeySpec
    {
        public KeySpec(string name, KeyKind kind, double min = 0, double max = 0, string[]? options = null, bool writable = true)
        {
            Name = name; Kind = kind; Min = min; Max = max; Options = options ?? Array.Empty<string>(); Writable = writable;
        }

        public string Name { get; }
        public KeyKind Kind { get; }
        public double Min { get; }
        public double Max { get; }
        public string[] Options { get; }
        public bool Writable { get; }
    }

    /// <summary>
    /// Key lists per app (v1, from AGENTS.md). Anything not on the list is rejected,
    /// numbers are clamped to their range, enums must match exactly.
    /// Ranges for the DualSense gains are placeholders until P1 reads them from the app.
    /// </summary>
    public static class KeySchema
    {
        public static readonly string[] AlienPresets = { "battery", "quiet", "balanced", "performance", "custom" };
        public static readonly string[] AtmosProfiles = { "dynamic", "movie", "music", "game", "voice", "custom" };
        public static readonly string[] DualSenseModes = { "native", "dualsense", "xbox" };

        private static readonly Dictionary<string, Dictionary<string, KeySpec>> Apps = new Dictionary<string, Dictionary<string, KeySpec>>
        {
            ["dummy"] = Build(
                new KeySpec("preset", KeyKind.Enum, options: AlienPresets)),
            ["alien"] = Build(
                new KeySpec("preset", KeyKind.Enum, options: AlienPresets),
                new KeySpec("gmode", KeyKind.Bool),
                new KeySpec("refresh_hz", KeyKind.Int, 30, 500),
                new KeySpec("refresh_options", KeyKind.Raw, writable: false),
                new KeySpec("key_light", KeyKind.Int, 0, 100),
                new KeySpec("cpu_temp", KeyKind.Number, 0, 150, writable: false),
                new KeySpec("gpu_temp", KeyKind.Number, 0, 150, writable: false),
                new KeySpec("cpu_fan", KeyKind.Int, 0, 20000, writable: false),
                new KeySpec("gpu_fan", KeyKind.Int, 0, 20000, writable: false),
                new KeySpec("power_w", KeyKind.Number, 0, 1000, writable: false),
                new KeySpec("ac", KeyKind.Bool, writable: false),
                new KeySpec("clean_ram", KeyKind.Action)),
            ["dolby"] = Build(
                new KeySpec("outputs", KeyKind.Raw, writable: false),
                new KeySpec("output", KeyKind.Raw),
                new KeySpec("atmos", KeyKind.Bool),
                new KeySpec("atmos_profile", KeyKind.Enum, options: AtmosProfiles),
                new KeySpec("prox", KeyKind.Raw)),
            ["dualsense"] = Build(
                new KeySpec("connected", KeyKind.Bool, writable: false),
                new KeySpec("link", KeyKind.Enum, options: new[] { "bt", "usb" }, writable: false),
                new KeySpec("battery", KeyKind.Int, 0, 100, writable: false),
                new KeySpec("charging", KeyKind.Bool, writable: false),
                new KeySpec("link_hz", KeyKind.Int, 0, 8000, writable: false),
                new KeySpec("haptic_lag_ms", KeyKind.Number, 0, 1000, writable: false),
                new KeySpec("mode", KeyKind.Enum, options: DualSenseModes),
                new KeySpec("haptics_gain", KeyKind.Int, 0, 200),
                new KeySpec("rumble_gain", KeyKind.Int, 0, 200),
                new KeySpec("trigger_cap", KeyKind.Int, 0, 100)),
        };

        public static bool IsKnownApp(string app) => Apps.ContainsKey(app);

        public static KeySpec? Find(string app, string key) =>
            Apps.TryGetValue(app, out var keys) && keys.TryGetValue(key, out var spec) ? spec : null;

        /// <summary>Checks a value for a key. Returns false (with a reason) when the key or value is not allowed.</summary>
        public static bool TryNormalize(string app, string key, object? value, out object? normalized, out string? error)
        {
            normalized = null;
            error = null;
            var spec = Find(app, key);
            if (spec == null) { error = "unknown key: " + key; return false; }
            switch (spec.Kind)
            {
                case KeyKind.Enum:
                    if (value is string s && Array.IndexOf(spec.Options, s) >= 0) { normalized = s; return true; }
                    error = "bad value for " + key;
                    return false;
                case KeyKind.Bool:
                    if (value is bool b) { normalized = b; return true; }
                    error = "bad value for " + key;
                    return false;
                case KeyKind.Int:
                {
                    if (!TryNumber(value, out var d)) { error = "bad value for " + key; return false; }
                    normalized = (long)Math.Round(Clamp(d, spec.Min, spec.Max), MidpointRounding.AwayFromZero);
                    return true;
                }
                case KeyKind.Number:
                {
                    if (!TryNumber(value, out var d)) { error = "bad value for " + key; return false; }
                    normalized = Clamp(d, spec.Min, spec.Max);
                    return true;
                }
                case KeyKind.Raw:
                    if (value is JsonRaw || value is string) { normalized = value; return true; }
                    error = "bad value for " + key;
                    return false;
                case KeyKind.Action:
                    normalized = value;
                    return true;
                default:
                    error = "bad key kind";
                    return false;
            }
        }

        /// <summary>Validates a set request. Read only keys are rejected too.</summary>
        public static bool TryValidateSet(string app, SetMessage set, out string? error)
        {
            var spec = Find(app, set.Key);
            if (spec != null && !spec.Writable) { error = "read only key: " + set.Key; return false; }
            if (!TryNormalize(app, set.Key, set.Value, out var v, out error)) return false;
            set.Value = v;
            return true;
        }

        /// <summary>Keeps only allowed keys of a state push, normalized. Rejected key names go to the list.</summary>
        public static StateMessage Filter(string app, StateMessage state, List<string>? rejected = null)
        {
            var result = new StateMessage();
            foreach (var kv in state.Values)
            {
                if (TryNormalize(app, kv.Key, kv.Value, out var v, out _)) result.Values[kv.Key] = v;
                else rejected?.Add(kv.Key);
            }
            return result;
        }

        private static bool TryNumber(object? value, out double d)
        {
            switch (value)
            {
                case long l: d = l; return true;
                case int i: d = i; return true;
                case double x when !double.IsNaN(x) && !double.IsInfinity(x): d = x; return true;
                case float f when !float.IsNaN(f) && !float.IsInfinity(f): d = f; return true;
                default: d = 0; return false;
            }
        }

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        private static Dictionary<string, KeySpec> Build(params KeySpec[] specs)
        {
            var d = new Dictionary<string, KeySpec>(StringComparer.Ordinal);
            foreach (var s in specs) d[s.Name] = s;
            return d;
        }
    }
}
