using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace ControlsHub.Widget
{
    /// <summary>
    /// The widget side of the AppService. The broker connects in (the widget never polls it);
    /// every message raises Message on whatever thread the AppService uses, the page marshals to UI.
    /// </summary>
    internal static class BrokerConnection
    {
        public const string ServiceName = "ControlsHub.Broker";

        private static readonly object Gate = new object();
        private static AppServiceConnection _connection;
        private static BackgroundTaskDeferral _deferral;

        /// <summary>Last value per key, so a page created later can show the current state at once.</summary>
        public static readonly ValueSet LastState = new ValueSet();
        public static bool PipeConnected;
        public static string PipeApp = "";

        public static event Action<ValueSet> Message;
        public static event Action<bool> ConnectedChanged;

        public static bool IsConnected { get { lock (Gate) return _connection != null; } }

        /// <summary>Starts the broker (or wakes the running one) so it opens the AppService to us.</summary>
        public static async void EnsureBroker()
        {
            if (IsConnected) return;
            if (!ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0)) return;
            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("Connect");
            }
            catch (Exception)
            {
                // Broker missing or blocked: the page shows "broker not connected".
            }
        }

        public static void Attach(AppServiceConnection connection, IBackgroundTaskInstance task)
        {
            lock (Gate)
            {
                _connection = connection;
                _deferral = task.GetDeferral();
            }
            task.Canceled += (s, reason) => Detach();
            connection.RequestReceived += OnRequestReceived;
            connection.ServiceClosed += (s, a) => Detach();
            ConnectedChanged?.Invoke(true);
        }

        public static async void Send(ValueSet message)
        {
            AppServiceConnection c;
            lock (Gate) c = _connection;
            if (c == null) return;
            try { await c.SendMessageAsync(message); }
            catch (Exception) { }
        }

        private static void Detach()
        {
            BackgroundTaskDeferral d;
            lock (Gate)
            {
                if (_connection == null) return;
                _connection = null;
                d = _deferral;
                _deferral = null;
            }
            PipeConnected = false;
            d?.Complete();
            ConnectedChanged?.Invoke(false);
        }

        private static void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var msg = args.Request.Message;
            var t = msg.TryGetValue("t", out var tv) ? tv as string : null;
            if (t == "state")
            {
                lock (LastState)
                {
                    foreach (var kv in msg)
                        if (kv.Key != "t") LastState[kv.Key] = kv.Value;
                }
            }
            else if (t == "pipe")
            {
                PipeConnected = msg.TryGetValue("connected", out var c) && c is bool b && b;
                PipeApp = msg.TryGetValue("app", out var a) ? a as string ?? "" : "";
            }
            Message?.Invoke(msg);
        }
    }
}
