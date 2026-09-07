using System.Management;

namespace ZephyrusKeyboardBattery;

internal static class UsbConnectionDetector
{
    private const string AsusUsbKeyboardVidPid = "VID_0B05&PID_193B";

    public static bool IsUsbConnected()
    {
        try
        {
            foreach (var deviceId in GetMatchingPresentDeviceIds())
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

    public static string? FirstUsbDeviceId()
    {
        try
        {
            return GetMatchingPresentDeviceIds().FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> GetMatchingPresentDeviceIds()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, Name, Status FROM Win32_PnPEntity " +
            "WHERE DeviceID LIKE 'USB\\\\VID_0B05&PID_193B%' " +
            "OR DeviceID LIKE 'HID\\\\VID_0B05&PID_193B%'");

        foreach (ManagementObject device in searcher.Get().Cast<ManagementObject>())
        {
            var status = device["Status"]?.ToString();
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var id = device["DeviceID"]?.ToString();
            if (id?.Contains(AsusUsbKeyboardVidPid, StringComparison.OrdinalIgnoreCase) == true)
            {
                yield return id;
            }
        }
    }
}
