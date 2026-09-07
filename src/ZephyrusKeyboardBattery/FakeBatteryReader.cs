namespace ZephyrusKeyboardBattery;

public sealed class FakeBatteryReader(int percent) : IBatteryReader
{
    public Task<BatteryReadResult> ReadAsync(TimeSpan? timeout = null)
    {
        return Task.FromResult(new BatteryReadResult(percent, true, "Ok"));
    }
}
