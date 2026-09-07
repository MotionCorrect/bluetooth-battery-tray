using System.Management;

namespace ZephyrusKeyboardBattery;

internal static class UsbConnectionDetector
{
    public static bool IsUsbConnected(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.UsbDeviceIdContains))
        {
            return false;
        }

        try
        {
            foreach (var deviceId in GetMatchingPresentDeviceIds(settings.UsbDeviceIdContains))
            {
                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Best-effort fallback only. If WMI is unhappy, do not break BLE reads.
        }

        return false;
    }

    public static IEnumerable<UsbDeviceInfo> ListPresentUsbHidDevices(string? filter = null)
    {
        var normalizedFilter = filter?.Trim();
        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, Name, Status FROM Win32_PnPEntity " +
            "WHERE DeviceID LIKE 'USB\\\\%' OR DeviceID LIKE 'HID\\\\%'");

        foreach (ManagementObject device in searcher.Get().Cast<ManagementObject>())
        {
            var status = device["Status"]?.ToString() ?? string.Empty;
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var id = device["DeviceID"]?.ToString() ?? string.Empty;
            var name = device["Name"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalizedFilter) &&
                !id.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase) &&
                !name.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new UsbDeviceInfo(name, id);
        }
    }

    private static IEnumerable<string> GetMatchingPresentDeviceIds(string deviceIdContains)
    {
        return ListPresentUsbHidDevices(deviceIdContains).Select(device => device.DeviceId);
    }
}

public sealed record UsbDeviceInfo(string Name, string DeviceId);
