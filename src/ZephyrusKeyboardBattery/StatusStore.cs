using System.Text.RegularExpressions;

namespace ZephyrusKeyboardBattery;

internal static partial class StatusStore
{
    public static string StatusDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppName);

    public static string LastStatusPath => Path.Combine(StatusDirectory, "last-status.txt");

    public static void Write(BatteryReadResult result)
    {
        Directory.CreateDirectory(StatusDirectory);
        File.WriteAllText(LastStatusPath, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  {result.ToDisplayLine()}{Environment.NewLine}");

        if (result.DeviceResults is { Count: > 0 })
        {
            foreach (var deviceResult in result.DeviceResults)
            {
                WriteLastKnownPercent(deviceResult);
            }

            return;
        }

        WriteLastKnownPercent(result);
    }

    public static int? ReadLastKnownPercent(DeviceSettings device)
    {
        foreach (var path in LastKnownPercentCandidatePaths(device))
        {
            var value = TryReadPercent(path);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static void WriteLastKnownPercent(BatteryReadResult result)
    {
        // Only update the durable percent when it came from a fresh Bluetooth read.
        // In USB-C/wired/HID fallback mode the app may show this stale value, clearly labeled as last BT.
        if (result.Percent is not int percent || result.IsStale)
        {
            return;
        }

        var path = LastKnownPercentPath(result.DeviceDisplayName);
        File.WriteAllText(path, percent.ToString());
    }

    private static int? TryReadPercent(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var text = File.ReadAllText(path).Trim();
            return int.TryParse(text, out var percent) ? Math.Clamp(percent, 0, 100) : null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> LastKnownPercentCandidatePaths(DeviceSettings device)
    {
        yield return LastKnownPercentPath(device.DeviceDisplayName);

        // Backward compatibility with the original single-device status file.
        yield return Path.Combine(StatusDirectory, "last-known-percent.txt");
    }

    private static string LastKnownPercentPath(string deviceDisplayName)
    {
        return Path.Combine(StatusDirectory, $"last-known-percent-{SanitizeFileName(deviceDisplayName)}.txt");
    }

    private static string SanitizeFileName(string value)
    {
        var sanitized = UnsafeFileNameCharsRegex().Replace(value.Trim(), "-");
        return string.IsNullOrWhiteSpace(sanitized) ? "device" : sanitized;
    }

    [GeneratedRegex("[^A-Za-z0-9_.-]+")]
    private static partial Regex UnsafeFileNameCharsRegex();
}
