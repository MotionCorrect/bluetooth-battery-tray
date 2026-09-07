namespace ZephyrusKeyboardBattery;

internal static class AppConstants
{
    public const string AppName = "ZephyrusKeyboardBattery";
    public const string KeyboardDisplayName = "Zephyrus Duo Keyboard";
    public const ulong KeyboardBluetoothAddress = 0xCD4AA5ADDD4B;
    public const int LowThresholdPercent = 20;
    public const int WarningThresholdPercent = 30;
    public static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan LowBatteryRenotifyInterval = TimeSpan.FromHours(8);
}
