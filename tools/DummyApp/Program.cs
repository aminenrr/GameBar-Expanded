// DummyApp: a stand in for Alien / Dolby / DualSense Controls during the P0 spike.
// Pipe SERVER on \\.\pipe\ControlsHub.Dummy (current user only), JSON lines protocol.
// Keys: 1 to 5 = preset, arrows = d pad, x = cross, o = circle, s = square, t = triangle,
// q = l1, e = r1, Esc = quit. Every change is pushed at once, nothing is polled.
// --auto <n>: no keyboard, pushes n preset changes and nav presses (CI smoke test only).
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using ControlsHub.Protocol;

internal static class Program
{
    private const string PipeName = "ControlsHub.Dummy";
    private static readonly object Gate = new();
    private static readonly List<Client> Clients = new();
    private static string _preset = "balanced";
    private static readonly TaskCompletionSource FirstSubscriber = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        int auto = 0;
        if (args.Length >= 2 && args[0] == "--auto") auto = int.Parse(args[1]);

        Console.WriteLine($"DummyApp: pipe server on \\\\.\\pipe\\{PipeName}, preset={_preset}");
        if (auto == 0)
            Console.WriteLine("Keys: 1 battery, 2 quiet, 3 balanced, 4 performance, 5 custom | arrows, x cross, o circle, s square, t triangle, q L1, e R1 | Esc quit");

        _ = Task.Run(AcceptLoop);

        if (auto > 0) return await AutoRun(auto);

        while (true)
        {
            var key = Console.ReadKey(intercept: true); // blocks until a key, no polling
            switch (key.Key)
            {
                case ConsoleKey.Escape: return 0;
                case ConsoleKey.D1: case ConsoleKey.NumPad1: SetPreset("battery", "key"); break;
                case ConsoleKey.D2: case ConsoleKey.NumPad2: SetPreset("quiet", "key"); break;
                case ConsoleKey.D3: case ConsoleKey.NumPad3: SetPreset("balanced", "key"); break;
                case ConsoleKey.D4: case ConsoleKey.NumPad4: SetPreset("performance", "key"); break;
                case ConsoleKey.D5: case ConsoleKey.NumPad5: SetPreset("custom", "key"); break;
                case ConsoleKey.UpArrow: Nav("up"); break;
                case ConsoleKey.DownArrow: Nav("down"); break;
                case ConsoleKey.LeftArrow: Nav("left"); break;
                case ConsoleKey.RightArrow: Nav("right"); break;
                case ConsoleKey.X: Nav("cross"); break;
                case ConsoleKey.O: Nav("circle"); break;
                case ConsoleKey.S: Nav("square"); break;
                case ConsoleKey.T: Nav("triangle"); break;
                case ConsoleKey.Q: Nav("l1"); break;
                case ConsoleKey.E: Nav("r1"); break;
            }
        }
    }

    private static async Task<int> AutoRun(int count)
    {
        // Test helper only: wait for a subscriber, then push changes on a short timer.
        if (await Task.WhenAny(FirstSubscriber.Task, Task.Delay(60000)) != FirstSubscriber.Task)
        {
            Console.WriteLine("auto: no subscriber");
            return 2;
        }
        await Task.Delay(500);
        for (int i = 0; i < count; i++)
        {
            SetPreset(KeySchema.AlienPresets[i % KeySchema.AlienPresets.Length], "auto");
            Nav(HubProtocol.NavButtons[i % HubProtocol.NavButtons.Length], force: true);
            await Task.Delay(100);
        }
        await Task.Delay(1000);
        Console.WriteLine("auto: done");
        return 0;
    }

    private static void SetPreset(string preset, string source)
    {
        lock (Gate) _preset = preset;
        Console.WriteLine($"preset -> {preset} ({source})");
        var s = new StateMessage();
        s.Values["preset"] = preset;
        Broadcast(s, c => c.Subscribed);
    }

    private static void Nav(string btn, bool force = false)
    {
        int sent;
        lock (Gate) sent = Clients.Count(c => c.NavOn || force);
        Console.WriteLine(sent > 0 ? $"nav {btn}" : $"nav {btn} (ignored: no client asked for nav)");
        Broadcast(new NavEventMessage { Btn = btn, Down = true }, c => c.NavOn || force);
    }

    private static void Broadcast(HubMessage msg, Func<Client, bool> filter)
    {
        var line = HubProtocol.Serialize(msg);
        List<Client> targets;
        lock (Gate) targets = Clients.Where(filter).ToList();
        foreach (var c in targets) _ = c.SendAsync(line);
    }

    private static PipeSecurity CurrentUserOnly()
    {
        var sec = new PipeSecurity();
        sec.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var me = WindowsIdentity.GetCurrent().User!;
        sec.AddAccessRule(new PipeAccessRule(me, PipeAccessRights.FullControl, AccessControlType.Allow));
        return sec;
    }

    private static async Task AcceptLoop()
    {
        var security = CurrentUserOnly();
        while (true)
        {
            var server = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, 4,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
            try
            {
                await server.WaitForConnectionAsync(); // overlapped wait, wakes only on a client
            }
            catch (Exception e)
            {
                Console.WriteLine("accept failed: " + e.Message);
                server.Dispose();
                await Task.Delay(1000);
                continue;
            }
            var client = new Client(server);
            lock (Gate) Clients.Add(client);
            Console.WriteLine("client connected");
            _ = Task.Run(() => Serve(client));
        }
    }

    private static async Task Serve(Client c)
    {
        try
        {
            using var reader = new StreamReader(c.Stream, new UTF8Encoding(false));
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                HubMessage msg;
                try { msg = HubProtocol.Parse(line); }
                catch (ProtocolException e) { Console.WriteLine("dropped bad line: " + e.Message); continue; }
                await Handle(c, msg);
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        finally
        {
            lock (Gate) Clients.Remove(c);
            c.Stream.Dispose();
            Console.WriteLine("client disconnected");
        }
    }

    private static async Task Handle(Client c, HubMessage msg)
    {
        switch (msg)
        {
            case HelloMessage h:
                Console.WriteLine($"hello from {h.Client} v{h.V}");
                await c.SendAsync(HubProtocol.Serialize(new HelloMessage { App = "dummy", AppVersion = "0.1.0" }));
                break;
            case SubMessage:
                c.Subscribed = true;
                FirstSubscriber.TrySetResult();
                var full = new StateMessage();
                lock (Gate) full.Values["preset"] = _preset;
                await c.SendAsync(HubProtocol.Serialize(full));
                break;
            case NavControlMessage nc:
                c.NavOn = nc.On;
                Console.WriteLine("nav stream " + (nc.On ? "on" : "off"));
                break;
            case SetMessage set:
                if (KeySchema.TryValidateSet("dummy", set, out var error))
                {
                    await c.SendAsync(HubProtocol.Serialize(new AckMessage { Id = set.Id, Ok = true }));
                    SetPreset((string)set.Value!, set.Quiet ? "hub, quiet" : "hub");
                }
                else
                {
                    await c.SendAsync(HubProtocol.Serialize(new AckMessage { Id = set.Id, Ok = false, Error = error }));
                }
                break;
            case OpenMessage:
                Console.WriteLine("open requested (a real app shows its window)");
                break;
        }
    }

    private sealed class Client
    {
        private readonly SemaphoreSlim _write = new(1, 1);
        public Client(NamedPipeServerStream s) { Stream = s; }
        public NamedPipeServerStream Stream { get; }
        public volatile bool Subscribed;
        public volatile bool NavOn;

        public async Task SendAsync(string line)
        {
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            await _write.WaitAsync();
            try { await Stream.WriteAsync(bytes); await Stream.FlushAsync(); }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally { _write.Release(); }
        }
    }
}
