namespace ZephyrusKeyboardBattery;

public sealed record BatteryReadResult(
    string DeviceDisplayName,
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
                return $"{DeviceDisplayName}: USB-C connected (last BT {percent}%)";
            }

            return $"{DeviceDisplayName}: {percent}%";
        }

        if (Source.Contains("USB", StringComparison.OrdinalIgnoreCase))
        {
            return $"{DeviceDisplayName}: USB-C connected (battery unavailable)";
        }

        return $"{DeviceDisplayName}: {Status}" +
               (string.IsNullOrWhiteSpace(Error) ? string.Empty : $" ({Error})");
    }
}
