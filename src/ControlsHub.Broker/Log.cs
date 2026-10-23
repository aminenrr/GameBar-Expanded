using System.Text;

namespace ControlsHub.Broker;

/// <summary>Append only log at %LOCALAPPDATA%\ControlsHub\broker.log (redirected into the package
/// folder by Windows when the broker runs packaged), mirrored to the console in console mode.</summary>
internal static class Log
{
    private static readonly object Gate = new();
    private static StreamWriter? _file;
    private static bool _console;

    public static string FilePath { get; private set; } = "";

    public static void Init(bool console)
    {
        _console = console;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ControlsHub");
        try
        {
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "broker.log");
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length > 1024 * 1024)
                File.Move(FilePath, FilePath + ".old", overwrite: true);
            var fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _file = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
        }
        catch (Exception e)
        {
            if (_console) Console.WriteLine("log file unavailable: " + e.Message);
        }
    }

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        lock (Gate)
        {
            try { _file?.WriteLine(line); } catch (IOException) { }
            if (_console) Console.WriteLine(line);
        }
    }

    public static string WorkingSet() => $"{Environment.WorkingSet / (1024.0 * 1024.0):0.0} MB";
}
