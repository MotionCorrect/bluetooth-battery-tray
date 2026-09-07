using System.Management;
using System.Text.RegularExpressions;

namespace ZephyrusKeyboardBattery;

internal static partial class DeviceDiscovery
{
    public static IEnumerable<BluetoothDeviceInfo> ListBluetoothLeDevices(string? filter = null)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, Name, Status FROM Win32_PnPEntity WHERE DeviceID LIKE 'BTHLE\\\\DEV_%'");

        foreach (ManagementObject device in searcher.Get().Cast<ManagementObject>())
        {
            var deviceId = device["DeviceID"]?.ToString() ?? string.Empty;
            var name = device["Name"]?.ToString() ?? string.Empty;
            var status = device["Status"]?.ToString() ?? string.Empty;
            var address = TryParseBluetoothLeAddress(deviceId);

            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            if (!Matches(filter, name, deviceId, address))
            {
                continue;
            }

            yield return new BluetoothDeviceInfo(name, address, status, deviceId);
        }
    }

    public static string? TryParseBluetoothLeAddress(string deviceId)
    {
        var match = BluetoothLeDeviceIdRegex().Match(deviceId);
        return match.Success ? match.Groups["address"].Value.ToUpperInvariant() : null;
    }

    private static bool Matches(string? filter, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return values.Any(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"BTHLE\\DEV_(?<address>[0-9A-Fa-f]{12})\\")]
    private static partial Regex BluetoothLeDeviceIdRegex();
}

public sealed record BluetoothDeviceInfo(string Name, string Address, string Status, string DeviceId);
