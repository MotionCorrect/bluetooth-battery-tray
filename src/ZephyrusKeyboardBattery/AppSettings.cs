using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZephyrusKeyboardBattery;

public sealed class AppSettings
{
    public string DeviceDisplayName { get; set; } = AppConstants.DefaultDisplayName;
    public string? BluetoothAddress { get; set; }
    public string? UsbDeviceIdContains { get; set; }
    public int LowThresholdPercent { get; set; } = AppConstants.DefaultLowThresholdPercent;
    public int WarningThresholdPercent { get; set; } = AppConstants.DefaultWarningThresholdPercent;
    public int PollIntervalSeconds { get; set; } = (int)AppConstants.DefaultPollInterval.TotalSeconds;
    public int LowBatteryRenotifyHours { get; set; } = (int)AppConstants.DefaultLowBatteryRenotifyInterval.TotalHours;

    [JsonIgnore]
    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Clamp(PollIntervalSeconds, 30, 86_400));

    [JsonIgnore]
    public TimeSpan LowBatteryRenotifyInterval => TimeSpan.FromHours(Math.Clamp(LowBatteryRenotifyHours, 1, 168));

    [JsonIgnore]
    public bool HasBluetoothAddress => TryGetBluetoothAddress(out _);

    public bool TryGetBluetoothAddress(out ulong address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(BluetoothAddress))
        {
            return false;
        }

        var normalized = BluetoothAddress.Trim()
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(":", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

        return normalized.Length <= 16 && ulong.TryParse(
            normalized,
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture,
            out address);
    }
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppConstants.AppName);

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions) ?? new AppSettings();
            Normalize(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Normalize(settings);
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static void EnsureExists()
    {
        if (!File.Exists(SettingsPath))
        {
            Save(new AppSettings());
        }
    }

    private static void Normalize(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DeviceDisplayName))
        {
            settings.DeviceDisplayName = AppConstants.DefaultDisplayName;
        }

        settings.LowThresholdPercent = Math.Clamp(settings.LowThresholdPercent, 1, 100);
        settings.WarningThresholdPercent = Math.Clamp(settings.WarningThresholdPercent, settings.LowThresholdPercent, 100);
        settings.PollIntervalSeconds = Math.Clamp(settings.PollIntervalSeconds, 30, 86_400);
        settings.LowBatteryRenotifyHours = Math.Clamp(settings.LowBatteryRenotifyHours, 1, 168);
    }
}
