using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace ControlsHub.Broker;

/// <summary>
/// CI check that the WinRT pieces the broker needs survive NativeAOT: ValueSet boxing and
/// an AppServiceConnection round trip to a package that does not exist (expects a clean failure status).
/// </summary>
internal static class SelfTest
{
    public static async Task<int> RunAsync()
    {
        try
        {
            var vs = new ValueSet { ["t"] = "state", ["preset"] = "quiet", ["down"] = true, ["id"] = 7 };
            if ((string)vs["preset"] != "quiet" || (bool)vs["down"] != true || (int)vs["id"] != 7)
            {
                Console.WriteLine("selftest: ValueSet round trip FAILED");
                return 1;
            }
            Console.WriteLine("selftest: ValueSet ok");

            using var c = new AppServiceConnection
            {
                AppServiceName = WidgetLink.ServiceName,
                PackageFamilyName = "ControlsHub.SelfTest_0000000000000",
            };
            var status = await c.OpenAsync();
            Console.WriteLine($"selftest: AppServiceConnection.OpenAsync -> {status} (expected a failure status)");
            Console.WriteLine($"selftest: packaged={Native.IsPackaged()} working set {Log.WorkingSet()}");
            Console.WriteLine("selftest: PASS");
            return status == AppServiceConnectionStatus.Success ? 1 : 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("selftest: FAILED " + e);
            return 1;
        }
    }
}
