using System.Windows.Forms;

namespace ZephyrusKeyboardBattery;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--check-once", StringComparer.OrdinalIgnoreCase))
        {
            return CheckOnceAsync().GetAwaiter().GetResult();
        }

        if (args.Contains("--install-startup", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            StartupRegistration.Enable();
            Console.WriteLine($"Installed startup: {StartupRegistration.CurrentValue()}");
            return 0;
        }

        if (args.Contains("--uninstall-startup", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            StartupRegistration.Disable();
            Console.WriteLine("Removed startup entry.");
            return 0;
        }

        var fakePercent = ParseFakePercent(args);
        IBatteryReader reader = fakePercent is int percent ? new FakeBatteryReader(percent) : new BatteryReader();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayAppContext(reader));
        return 0;
    }

    private static async Task<int> CheckOnceAsync()
    {
        ConsoleBridge.AttachParentConsole();
        var result = await new BatteryReader().ReadAsync(TimeSpan.FromSeconds(20));
        StatusStore.Write(result);
        Console.WriteLine(result.ToDisplayLine());
        Console.WriteLine($"Status file: {StatusStore.LastStatusPath}");
        return result.Percent.HasValue ? 0 : 2;
    }

    private static int? ParseFakePercent(string[] args)
    {
        if (args.Contains("--fake-low", StringComparer.OrdinalIgnoreCase))
        {
            return 18;
        }

        const string prefix = "--fake-percent=";
        var arg = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (arg is null)
        {
            return null;
        }

        return int.TryParse(arg[prefix.Length..], out var percent)
            ? Math.Clamp(percent, 0, 100)
            : null;
    }
}
