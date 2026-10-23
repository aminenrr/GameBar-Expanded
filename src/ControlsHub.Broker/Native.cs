using System.Runtime.InteropServices;

namespace ControlsHub.Broker;

internal static partial class Native
{
    private const int AppModelErrorNoPackage = 15700;
    private const int StdOutputHandle = -11;

    [LibraryImport("kernel32.dll")]
    private static unsafe partial int GetCurrentPackageFullName(ref uint length, char* buffer);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllocConsole();

    [LibraryImport("kernel32.dll")]
    private static partial nint GetStdHandle(int which);

    /// <summary>True when the process runs with package identity (installed MSIX).</summary>
    public static unsafe bool IsPackaged()
    {
        uint len = 0;
        return GetCurrentPackageFullName(ref len, null) != AppModelErrorNoPackage;
    }

    /// <summary>True when stdout already goes somewhere (redirected to a file or pipe).</summary>
    public static bool HasStdOut()
    {
        var h = GetStdHandle(StdOutputHandle);
        return h != 0 && h != -1;
    }
}
