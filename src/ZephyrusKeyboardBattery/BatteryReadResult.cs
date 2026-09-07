namespace ZephyrusKeyboardBattery;

public sealed record BatteryReadResult(
    int? Percent,
    bool IsConnected,
    string Status,
    string? Error = null,
    bool IsStale = false,
    string Source = "Bluetooth LE")
{
    public string ToDisplayLine()
    {
        if (Percent is int percent)
        {
            if (IsStale && Source.Contains("USB", StringComparison.OrdinalIgnoreCase))
            {
                return $"{AppConstants.KeyboardDisplayName}: USB-C connected (last BT {percent}%)";
            }

            var label = percent <= AppConstants.LowThresholdPercent ? "LOW " : string.Empty;
            return $"{AppConstants.KeyboardDisplayName}: {label}{percent}%";
        }

        if (Source.Contains("USB", StringComparison.OrdinalIgnoreCase))
        {
            return $"{AppConstants.KeyboardDisplayName}: USB-C connected (battery unavailable)";
        }

        return $"{AppConstants.KeyboardDisplayName}: {Status}" +
               (string.IsNullOrWhiteSpace(Error) ? string.Empty : $" ({Error})");
    }
}
