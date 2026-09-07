namespace ZephyrusKeyboardBattery;

public sealed class FakeBatteryReader(int percent, AppSettings? settings = null) : IBatteryReader
{
    private readonly AppSettings _settings = settings ?? SettingsStore.Load();

    public Task<BatteryReadResult> ReadAsync(TimeSpan? timeout = null)
    {
        var results = _settings.Devices
            .Select(device => new BatteryReadResult(device.DeviceDisplayName, Math.Clamp(percent, 0, 100), true, "Ok", Source: "Fake"))
            .ToList();

        if (results.Count == 1)
        {
            return Task.FromResult(results[0]);
        }

        return Task.FromResult(new BatteryReadResult(
            "Bluetooth batteries",
            results.Min(result => result.Percent),
            true,
            "Ok",
            Source: "Fake",
            DeviceResults: results));
    }
}
