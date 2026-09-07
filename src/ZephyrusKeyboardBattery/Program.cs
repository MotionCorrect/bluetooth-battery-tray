using System.Windows.Forms;

namespace ZephyrusKeyboardBattery;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            PrintHelp();
            return 0;
        }

        if (args.Contains("--list-bluetooth", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            ListBluetoothDevices(ArgValue(args, "--filter"));
            return 0;
        }

        if (args.Contains("--list-usb", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            ListUsbDevices(ArgValue(args, "--filter"));
            return 0;
        }

        if (args.Contains("--show-config", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            SettingsStore.EnsureExists();
            Console.WriteLine(SettingsStore.SettingsPath);
            Console.WriteLine(File.ReadAllText(SettingsStore.SettingsPath));
            return 0;
        }

        if (args.Contains("--configure", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            Configure(args, replaceAllDevices: true);
            return 0;
        }

        if (args.Contains("--add-device", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleBridge.AttachParentConsole();
            Configure(args, replaceAllDevices: false);
            return 0;
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

        var settings = SettingsStore.Load();
        if (args.Contains("--check-once", StringComparer.OrdinalIgnoreCase))
        {
            return CheckOnceAsync(settings).GetAwaiter().GetResult();
        }

        var fakePercent = ParseFakePercent(args);
        IBatteryReader reader = fakePercent is int percent ? new FakeBatteryReader(percent, settings) : new BatteryReader(settings);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayAppContext(settings, reader));
        return 0;
    }

    private static async Task<int> CheckOnceAsync(AppSettings settings)
    {
        ConsoleBridge.AttachParentConsole();
        var result = await new BatteryReader(settings).ReadAsync(TimeSpan.FromSeconds(20));
        StatusStore.Write(result);
        Console.WriteLine(result.ToDisplayLine());
        Console.WriteLine($"Status file: {StatusStore.LastStatusPath}");
        return result.Percent.HasValue || result.IsConnected ? 0 : 2;
    }

    private static void Configure(string[] args, bool replaceAllDevices)
    {
        var settings = SettingsStore.Load();
        var device = BuildDeviceFromArgs(args, replaceAllDevices ? settings.Devices.FirstOrDefault() : null);

        if (replaceAllDevices)
        {
            settings.Devices = [device];
        }
        else
        {
            UpsertDevice(settings.Devices, device);
        }

        ApplyGlobalSettingsFromArgs(settings, args);
        SettingsStore.Save(settings);
        Console.WriteLine($"Saved config: {SettingsStore.SettingsPath}");
        Console.WriteLine(File.ReadAllText(SettingsStore.SettingsPath));
    }

    private static DeviceSettings BuildDeviceFromArgs(string[] args, DeviceSettings? fallback)
    {
        return new DeviceSettings
        {
            DeviceDisplayName = ArgValue(args, "--name") ?? fallback?.DeviceDisplayName ?? AppConstants.DefaultDisplayName,
            BluetoothAddress = ArgValue(args, "--address") ?? fallback?.BluetoothAddress,
            UsbDeviceIdContains = ArgValue(args, "--usb-id") ?? ArgValue(args, "--fallback-id") ?? fallback?.UsbDeviceIdContains,
            UsbConnectionLabel = ArgValue(args, "--connection-label") ?? fallback?.UsbConnectionLabel ?? "USB-C connected"
        };
    }

    private static void UpsertDevice(List<DeviceSettings> devices, DeviceSettings device)
    {
        var existing = devices.FindIndex(candidate =>
            string.Equals(candidate.DeviceDisplayName, device.DeviceDisplayName, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(candidate.BluetoothAddress)
                && string.Equals(candidate.BluetoothAddress, device.BluetoothAddress, StringComparison.OrdinalIgnoreCase)));

        if (existing >= 0)
        {
            devices[existing] = device;
        }
        else
        {
            devices.Add(device);
        }
    }

    private static void ApplyGlobalSettingsFromArgs(AppSettings settings, string[] args)
    {
        if (int.TryParse(ArgValue(args, "--low"), out var low))
        {
            settings.LowThresholdPercent = low;
        }

        if (int.TryParse(ArgValue(args, "--warning"), out var warning))
        {
            settings.WarningThresholdPercent = warning;
        }

        if (int.TryParse(ArgValue(args, "--poll-seconds"), out var pollSeconds))
        {
            settings.PollIntervalSeconds = pollSeconds;
        }
    }

    private static void ListBluetoothDevices(string? filter)
    {
        foreach (var device in DeviceDiscovery.ListBluetoothLeDevices(filter).OrderBy(device => device.Name))
        {
            Console.WriteLine($"{device.Name} | address={device.Address} | status={device.Status}");
        }
    }

    private static void ListUsbDevices(string? filter)
    {
        foreach (var device in UsbConnectionDetector.ListPresentUsbHidDevices(filter).OrderBy(device => device.Name))
        {
            Console.WriteLine($"{device.Name} | id={device.DeviceId}");
        }
    }

    private static int? ParseFakePercent(string[] args)
    {
        if (args.Contains("--fake-low", StringComparer.OrdinalIgnoreCase))
        {
            return 18;
        }

        var arg = ArgValue(args, "--fake-percent");
        return int.TryParse(arg, out var percent)
            ? Math.Clamp(percent, 0, 100)
            : null;
    }

    private static string? ArgValue(string[] args, string name)
    {
        var prefix = name + "=";
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return args[i][prefix.Length..];
            }

            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Bluetooth Battery Tray");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  BluetoothBatteryTray --list-bluetooth [--filter text]");
        Console.WriteLine("  BluetoothBatteryTray --list-usb [--filter text]");
        Console.WriteLine("  BluetoothBatteryTray --configure --name \"Device Name\" --address AABBCCDDEEFF [--usb-id VID_1234&PID_5678]");
        Console.WriteLine("  BluetoothBatteryTray --add-device --name \"Second Device\" --address 112233445566 [--fallback-id text] [--connection-label text]");
        Console.WriteLine("  BluetoothBatteryTray --check-once");
        Console.WriteLine("  BluetoothBatteryTray --install-startup");
        Console.WriteLine("  BluetoothBatteryTray --uninstall-startup");
        Console.WriteLine("  BluetoothBatteryTray --show-config");
    }
}
