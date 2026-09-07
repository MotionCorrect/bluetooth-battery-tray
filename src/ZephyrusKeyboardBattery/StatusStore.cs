namespace ZephyrusKeyboardBattery;

internal static class StatusStore
{
    public static string StatusDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppName);

    public static string LastStatusPath => Path.Combine(StatusDirectory, "last-status.txt");
    public static string LastKnownPercentPath => Path.Combine(StatusDirectory, "last-known-percent.txt");

    public static void Write(BatteryReadResult result)
    {
        Directory.CreateDirectory(StatusDirectory);
        File.WriteAllText(LastStatusPath, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  {result.ToDisplayLine()}{Environment.NewLine}");

        // Only update the durable percent when it came from a fresh Bluetooth read.
        // In USB-C mode the app may show this stale value, clearly labeled as last BT.
        if (result.Percent is int percent && !result.IsStale)
        {
            File.WriteAllText(LastKnownPercentPath, percent.ToString());
        }
    }

    public static int? ReadLastKnownPercent()
    {
        try
        {
            if (!File.Exists(LastKnownPercentPath))
            {
                return null;
            }

            var text = File.ReadAllText(LastKnownPercentPath).Trim();
            return int.TryParse(text, out var percent) ? Math.Clamp(percent, 0, 100) : null;
        }
        catch
        {
            return null;
        }
    }
}
