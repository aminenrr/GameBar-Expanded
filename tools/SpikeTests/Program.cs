// SpikeTests: one off checks for the risky parts of P0. Not shipped in the package.
//
//   SpikeTests wing [seconds]                 wait (default 5 s), then send Win+G with SendInput
//   SpikeTests hidhide-list                   print gaming devices as HidHide sees them
//   SpikeTests hidhide-hide "<instance path>" hide the device, wait for Enter, then unhide
//   SpikeTests hidhide-unhide "<instance path>" unhide (safety net after a crash)
//   SpikeTests hidhide-time "<instance path>" [rounds]  measure hide and unhide time in ms
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0) return Usage();
        try
        {
            switch (args[0])
            {
                case "wing": return Wing(args.Length > 1 ? int.Parse(args[1]) : 5);
                case "hidhide-list": return HidHide.List();
                case "hidhide-hide" when args.Length > 1: return HidHide.HideUntilEnter(args[1]);
                case "hidhide-unhide" when args.Length > 1: return HidHide.Unhide(args[1]);
                case "hidhide-time" when args.Length > 1: return HidHide.Time(args[1], args.Length > 2 ? int.Parse(args[2]) : 5);
                default: return Usage();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("error: " + e.Message);
            return 1;
        }
    }

    private static int Usage()
    {
        Console.WriteLine("""
            SpikeTests wing [seconds]
            SpikeTests hidhide-list
            SpikeTests hidhide-hide "<device instance path>"
            SpikeTests hidhide-unhide "<device instance path>"
            SpikeTests hidhide-time "<device instance path>" [rounds]
            """);
        return 2;
    }

    // ---------- Win+G ----------

    private const ushort VkLWin = 0x5B;
    private const ushort VkG = 0x47;
    private const uint KeyEventFKeyUp = 0x0002;
    private const uint KeyEventFExtendedKey = 0x0001;

    private static int Wing(int seconds)
    {
        for (int s = seconds; s > 0; s--)
        {
            Console.WriteLine($"Win+G in {s} s, switch to the game now...");
            Thread.Sleep(1000);
        }
        var inputs = new[]
        {
            Key(VkLWin, KeyEventFExtendedKey),
            Key(VkG, 0),
            Key(VkG, KeyEventFKeyUp),
            Key(VkLWin, KeyEventFExtendedKey | KeyEventFKeyUp),
        };
        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        Console.WriteLine(sent == inputs.Length
            ? $"Sent Win+G at {DateTime.Now:HH:mm:ss.fff}. Did Game Bar open over the game?"
            : $"SendInput sent {sent} of {inputs.Length} events (error {Marshal.GetLastWin32Error()}). A higher integrity window may be in front; try running elevated.");
        return sent == inputs.Length ? 0 : 1;
    }

    private static INPUT Key(ushort vk, uint flags) => new()
    {
        type = 1, // INPUT_KEYBOARD
        u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } },
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint SendInput(uint count, [In] INPUT[] inputs, int size);
}

/// <summary>
/// Drives HidHide through HidHideCLI.exe. Safety rules: never leave a device hidden
/// (unhide in finally and on Ctrl+C), never unhide a device that was already hidden before
/// we started (DualSense Controls may have hidden it on purpose), restore the cloak state.
/// </summary>
internal static class HidHide
{
    private static readonly string[] CliPaths =
    {
        @"C:\Program Files\Nefarius Software Solutions\HidHide\x64\HidHideCLI.exe",
        @"C:\Program Files\Nefarius Software Solutions e.U\HidHide\x64\HidHideCLI.exe",
    };

    private static string? FindCli() => CliPaths.FirstOrDefault(File.Exists);

    private static string RequireCli()
    {
        var cli = FindCli();
        if (cli != null) return cli;
        throw new InvalidOperationException("""
            HidHideCLI.exe not found. To run this test:
              1. Install HidHide from https://github.com/nefarius/HidHide/releases (reboot when asked).
              2. Open HidHide Configuration Client, Applications tab: add DualSense Controls' exe
                 so it keeps reading the pad while it is hidden from games.
              3. Run this tool from an elevated terminal (HidHide's control device needs admin).
            Looked in:
            """ + string.Join(Environment.NewLine, CliPaths.Select(p => "  " + p)));
    }

    private static (int code, string output, long ms) Run(string cli, params string[] args)
    {
        var psi = new ProcessStartInfo(cli)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        var sw = Stopwatch.StartNew();
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        sw.Stop();
        return (p.ExitCode, (stdout.Result + stderr.Result).Trim(), sw.ElapsedMilliseconds);
    }

    public static int List()
    {
        var cli = RequireCli();
        Console.WriteLine("Gaming devices (copy the deviceInstancePath of the DualSense, the HID one):");
        Console.WriteLine(Run(cli, "--dev-gaming").output);
        Console.WriteLine();
        Console.WriteLine("Currently hidden:");
        Console.WriteLine(Run(cli, "--dev-list").output);
        Console.WriteLine();
        Console.WriteLine("Allowed applications (DualSense Controls must be here):");
        Console.WriteLine(Run(cli, "--app-list").output);
        Console.WriteLine("Cloak: " + Run(cli, "--cloak-state").output);
        return 0;
    }

    private sealed class Session : IDisposable
    {
        private readonly string _cli;
        private readonly string _device;
        private readonly bool _wasCloaked;
        private bool _hidden;
        private int _restored;

        public Session(string cli, string device)
        {
            _cli = cli;
            _device = device;
            var hiddenList = Run(cli, "--dev-list").output;
            if (hiddenList.Contains(device, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("That device is already on HidHide's hidden list (DualSense Controls or you put it there). Not touching it.");
            _wasCloaked = Run(cli, "--cloak-state").output.Contains("--cloak-on");
            Console.CancelKeyPress += OnCancel;
            AppDomain.CurrentDomain.ProcessExit += OnExit;
        }

        public long Hide()
        {
            _hidden = true; // set first so a crash mid call still unhides
            var r = _wasCloaked ? Run(_cli, "--dev-hide", _device) : Run(_cli, "--dev-hide", _device, "--cloak-on");
            if (r.code != 0) throw new InvalidOperationException("hide failed: " + r.output);
            return r.ms;
        }

        public long Unhide()
        {
            var r = _wasCloaked ? Run(_cli, "--dev-unhide", _device) : Run(_cli, "--dev-unhide", _device, "--cloak-off");
            if (r.code != 0) throw new InvalidOperationException("unhide failed: " + r.output);
            _hidden = false;
            return r.ms;
        }

        private void Restore()
        {
            if (Interlocked.Exchange(ref _restored, 1) == 1) return;
            if (!_hidden) return;
            try { Unhide(); Console.WriteLine("Device unhidden."); }
            catch (Exception e) { Console.WriteLine("WARNING: could not unhide, run: SpikeTests hidhide-unhide \"" + _device + "\" (" + e.Message + ")"); }
        }

        private void OnCancel(object? s, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Restore();
            Environment.Exit(130);
        }

        private void OnExit(object? s, EventArgs e) => Restore();

        public void Dispose()
        {
            Restore();
            Console.CancelKeyPress -= OnCancel;
            AppDomain.CurrentDomain.ProcessExit -= OnExit;
        }
    }

    public static int HideUntilEnter(string device)
    {
        var cli = RequireCli();
        using var session = new Session(cli, device);
        var ms = session.Hide();
        Console.WriteLine($"Hidden in {ms} ms. Check: the game lost the pad, DualSense Controls still reads it.");
        Console.WriteLine("Press Enter to unhide (Ctrl+C also unhides).");
        Console.ReadLine();
        return 0; // Dispose unhides
    }

    public static int Unhide(string device)
    {
        var cli = RequireCli();
        var r = Run(cli, "--dev-unhide", device);
        Console.WriteLine(r.code == 0 ? $"Unhidden in {r.ms} ms." : "unhide failed: " + r.output);
        return r.code;
    }

    public static int Time(string device, int rounds)
    {
        var cli = RequireCli();
        using var session = new Session(cli, device);
        var hide = new List<long>();
        var unhide = new List<long>();
        for (int i = 0; i < rounds; i++)
        {
            hide.Add(session.Hide());
            Thread.Sleep(500);
            unhide.Add(session.Unhide());
            Thread.Sleep(500);
            Console.WriteLine($"round {i + 1}: hide {hide[^1]} ms, unhide {unhide[^1]} ms");
        }
        Console.WriteLine($"hide   avg {hide.Average():0} ms, max {hide.Max()} ms");
        Console.WriteLine($"unhide avg {unhide.Average():0} ms, max {unhide.Max()} ms");
        Console.WriteLine("Note: this is the CLI round trip (process start included). The game side reconnect time is what to watch in game.");
        return 0;
    }
}
