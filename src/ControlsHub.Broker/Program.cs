// Controls Hub broker: the only always on piece. Full trust, started at sign in (StartupTask)
// or by the widget (FullTrustProcessLauncher, --connect). Talks to the apps over named pipes
// and to the widget over the in package AppService "ControlsHub.Broker".
//
//   ControlsHub.Broker.exe               background mode (packaged): pipe client, waits for the widget
//   ControlsHub.Broker.exe --connect     same, and opens the widget AppService now (used by the widget)
//   ControlsHub.Broker.exe --console     opens a console and prints live state (works unpackaged)
//   ControlsHub.Broker.exe --exit        asks a running background broker to quit
//   ControlsHub.Broker.exe --selftest    exercises the WinRT AppService types (CI check for NativeAOT)
//   --exit-after <seconds>               quit after a while (CI smoke test)
using ControlsHub.Broker;

internal static class Program
{
    private const string MutexName = @"Local\ControlsHub.Broker";
    private const string ConnectEventName = @"Local\ControlsHub.Broker.Connect";
    private const string ExitEventName = @"Local\ControlsHub.Broker.Exit";
    private const string PipeName = "ControlsHub.Dummy";

    private static async Task<int> Main(string[] args)
    {
        bool console = args.Contains("--console");
        bool connect = args.Contains("--connect");
        int exitAfter = 0;
        var i = Array.IndexOf(args, "--exit-after");
        if (i >= 0 && i + 1 < args.Length) int.TryParse(args[i + 1], out exitAfter);

        if (console && !Native.HasStdOut()) Native.AllocConsole();

        if (args.Contains("--exit"))
        {
            if (EventWaitHandle.TryOpenExisting(ExitEventName, out var ev)) { ev.Set(); return 0; }
            Console.WriteLine("No broker running.");
            return 1;
        }

        if (args.Contains("--selftest")) return await SelfTest.RunAsync();

        Log.Init(console);
        bool packaged = Native.IsPackaged();

        using var connectEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ConnectEventName);
        using var exitEvent = new EventWaitHandle(false, EventResetMode.ManualReset, ExitEventName);
        Mutex? single = null;
        if (!console)
        {
            single = new Mutex(true, MutexName, out bool first);
            if (!first)
            {
                // Already running: hand the connect request over and leave.
                if (connect) connectEvent.Set();
                single.Dispose();
                return 0;
            }
        }

        Log.Write($"broker start pid={Environment.ProcessId} mode={(console ? "console" : "background")} packaged={packaged} aot={!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported} working set {Log.WorkingSet()}");
        Log.Write("log file: " + Log.FilePath);

        WidgetLink? widget = packaged ? new WidgetLink() : null;
        var hub = new Hub(PipeName, widget, consoleWantsNav: console);
        using var cts = new CancellationTokenSource();

        RegisteredWaitHandle? connectWait = null;
        if (widget != null)
        {
            connectWait = ThreadPool.RegisterWaitForSingleObject(connectEvent,
                (_, _) => _ = widget.ConnectAsync(), null, Timeout.Infinite, executeOnlyOnce: false);
            if (connect) _ = widget.ConnectAsync();
        }

        var stop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exitWait = ThreadPool.RegisterWaitForSingleObject(exitEvent, (_, _) => stop.TrySetResult(), null, Timeout.Infinite, true);
        if (console)
        {
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.TrySetResult(); };
            Console.WriteLine("Ctrl+C to quit.");
        }
        if (exitAfter > 0) _ = Task.Delay(TimeSpan.FromSeconds(exitAfter)).ContinueWith(_ => stop.TrySetResult());

        var run = hub.RunAsync(cts.Token);
        await stop.Task;
        cts.Cancel();
        try { await run; } catch (OperationCanceledException) { }

        connectWait?.Unregister(null);
        exitWait.Unregister(null);
        Log.Write($"broker exit after {hub.Changes} state changes, working set {Log.WorkingSet()}");
        single?.ReleaseMutex();
        single?.Dispose();
        return 0;
    }
}
