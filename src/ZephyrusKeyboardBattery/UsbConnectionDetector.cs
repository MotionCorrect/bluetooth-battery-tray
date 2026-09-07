using System.Management;

namespace ZephyrusKeyboardBattery;

internal static class UsbConnectionDetector
{
    public static bool IsUsbConnected(DeviceSettings deviceSettings)
    {
        if (string.IsNullOrWhiteSpace(deviceSettings.UsbDeviceIdContains))
        {
            return false;
        }

        try
        {
            var needle = deviceSettings.UsbDeviceIdContains.Trim();
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, PNPDeviceID, Name FROM Win32_PnPEntity WHERE PNPDeviceID IS NOT NULL");

            foreach (var item in searcher.Get().Cast<ManagementObject>())
            {
                var pnpDeviceId = item["PNPDeviceID"]?.ToString() ?? string.Empty;
                var deviceId = item["DeviceID"]?.ToString() ?? string.Empty;
                var name = item["Name"]?.ToString() ?? string.Empty;
                if (ContainsIgnoreCase(pnpDeviceId, needle)
                    || ContainsIgnoreCase(deviceId, needle)
                    || ContainsIgnoreCase(name, needle))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Best-effort fallback only. If WMI is unavailable, keep reporting BLE state.
        }

        return false;
    }

    public static IEnumerable<UsbDeviceInfo> ListPresentUsbHidDevices(string? filter = null)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, PNPDeviceID, Name FROM Win32_PnPEntity WHERE PNPDeviceID IS NOT NULL");

        foreach (var item in searcher.Get().Cast<ManagementObject>())
        {
            var pnpDeviceId = item["PNPDeviceID"]?.ToString() ?? string.Empty;
            var deviceId = item["DeviceID"]?.ToString() ?? pnpDeviceId;
            var name = item["Name"]?.ToString() ?? "Unknown USB/HID device";

            if (!LooksLikeUsbOrHid(pnpDeviceId))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(filter)
                && !ContainsIgnoreCase(name, filter)
                && !ContainsIgnoreCase(pnpDeviceId, filter)
                && !ContainsIgnoreCase(deviceId, filter))
            {
                continue;
            }

            yield return new UsbDeviceInfo(name, string.IsNullOrWhiteSpace(pnpDeviceId) ? deviceId : pnpDeviceId);
        }
    }

    private static bool LooksLikeUsbOrHid(string value)
    {
        return value.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("HID\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsIgnoreCase(string haystack, string needle)
    {
        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record UsbDeviceInfo(string Name, string DeviceId);
