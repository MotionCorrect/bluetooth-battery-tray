namespace ZephyrusKeyboardBattery;

public sealed record BatteryReadResult(
    string DeviceDisplayName,
    int? Percent,
    bool IsConnected,
    string Status,
    string? Error = null,
    bool IsStale = false,
    string Source = "Bluetooth LE",
    IReadOnlyList<BatteryReadResult>? DeviceResults = null)
{
    public bool IsAggregate => DeviceResults is { Count: > 0 };

    public string ToDisplayLine()
    {
        if (DeviceResults is { Count: > 0 })
        {
            return string.Join("; ", DeviceResults.Select(result => result.ToDisplayLine()));
        }

        if (Percent is int percent)
        {
            if (IsStale)
            {
                return $"{DeviceDisplayName}: {Status} (last BT {percent}%)";
            }

            return $"{DeviceDisplayName}: {percent}%";
        }

        return string.IsNullOrWhiteSpace(Error)
            ? $"{DeviceDisplayName}: {Status}"
            : $"{DeviceDisplayName}: {Status} ({Error})";
    }

    public string ToShortDisplayLine()
    {
        if (DeviceResults is { Count: > 0 })
        {
            return string.Join(" | ", DeviceResults.Select(result => result.ToShortDisplayLine()));
        }

        return Percent is int percent
            ? $"{DeviceDisplayName}: {percent}%{(IsStale ? "*" : string.Empty)}"
            : $"{DeviceDisplayName}: {Status}";
    }
}
