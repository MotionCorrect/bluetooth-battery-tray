namespace ZephyrusKeyboardBattery;

public sealed class FakeBatteryReader(int percent, AppSettings? settings = null) : IBatteryReader
{
    private readonly AppSettings _settings = settings ?? SettingsStore.Load();

    public Task<BatteryReadResult> ReadAsync(TimeSpan? timeout = null)
    {
        return Task.FromResult(new BatteryReadResult(_settings.DeviceDisplayName, percent, true, "Ok"));
    }
}
