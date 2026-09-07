namespace ZephyrusKeyboardBattery;

public interface IBatteryReader
{
    Task<BatteryReadResult> ReadAsync(TimeSpan? timeout = null);
}
