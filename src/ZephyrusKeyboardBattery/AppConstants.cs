namespace ZephyrusKeyboardBattery;

internal static class AppConstants
{
    public const string AppName = "BluetoothBatteryTray";
    public const string DefaultDisplayName = "Bluetooth battery device";
    public const int DefaultLowThresholdPercent = 20;
    public const int DefaultWarningThresholdPercent = 30;
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DefaultLowBatteryRenotifyInterval = TimeSpan.FromHours(8);
}
