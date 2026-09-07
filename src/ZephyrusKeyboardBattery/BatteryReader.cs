using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace ZephyrusKeyboardBattery;

public sealed class BatteryReader(AppSettings settings) : IBatteryReader
{
    private static readonly Guid BatteryServiceUuid = Guid.Parse("0000180f-0000-1000-8000-00805f9b34fb");
    private static readonly Guid BatteryLevelUuid = Guid.Parse("00002a19-0000-1000-8000-00805f9b34fb");

    public async Task<BatteryReadResult> ReadAsync(TimeSpan? timeout = null)
    {
        var devices = settings.Devices.Count > 0 ? settings.Devices : SettingsStore.CreateDefaultSettings().Devices;
        if (devices.Count == 1)
        {
            return await ReadDeviceAsync(devices[0], timeout);
        }

        var results = new List<BatteryReadResult>();
        foreach (var device in devices)
        {
            results.Add(await ReadDeviceAsync(device, timeout));
        }

        return Aggregate(results);
    }

    private async Task<BatteryReadResult> ReadDeviceAsync(DeviceSettings deviceSettings, TimeSpan? timeout = null)
    {
        if (!deviceSettings.TryGetBluetoothAddress(out var address))
        {
            return new BatteryReadResult(
                deviceSettings.DeviceDisplayName,
                null,
                false,
                "Not configured",
                $"Set BluetoothAddress in {SettingsStore.SettingsPath}");
        }

        var readTask = ReadCoreAsync(deviceSettings, address);
        var maxWait = timeout ?? TimeSpan.FromSeconds(20);
        var completed = await Task.WhenAny(readTask, Task.Delay(maxWait));
        if (completed != readTask)
        {
            return FallbackConnection(deviceSettings, $"No BLE response within {maxWait.TotalSeconds:0}s");
        }

        return await readTask;
    }

    private async Task<BatteryReadResult> ReadCoreAsync(DeviceSettings deviceSettings, ulong address)
    {
        try
        {
            using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
            if (device is null)
            {
                return FallbackConnection(deviceSettings, "Device not found");
            }

            var connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected;

            var services = await device.GetGattServicesForUuidAsync(BatteryServiceUuid, BluetoothCacheMode.Uncached);
            if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
            {
                return FallbackConnection(deviceSettings, $"Battery service read failed: {services.Status}", connected);
            }

            using var service = services.Services[0];
            var characteristics = await service.GetCharacteristicsForUuidAsync(BatteryLevelUuid, BluetoothCacheMode.Uncached);
            if (characteristics.Status != GattCommunicationStatus.Success || characteristics.Characteristics.Count == 0)
            {
                return FallbackConnection(deviceSettings, $"Battery characteristic read failed: {characteristics.Status}", connected);
            }

            var read = await characteristics.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Uncached);
            if (read.Status != GattCommunicationStatus.Success)
            {
                return FallbackConnection(deviceSettings, $"Battery value read failed: {read.Status}", connected);
            }

            var reader = DataReader.FromBuffer(read.Value);
            var percent = Math.Clamp(reader.ReadByte(), (byte)0, (byte)100);
            return new BatteryReadResult(deviceSettings.DeviceDisplayName, percent, connected, "Ok", Source: "Bluetooth LE");
        }
        catch (Exception ex)
        {
            return FallbackConnection(deviceSettings, ex.Message);
        }
    }

    private static BatteryReadResult Aggregate(IReadOnlyList<BatteryReadResult> results)
    {
        var usablePercents = results
            .Where(result => result.Percent.HasValue)
            .Select(result => result.Percent!.Value)
            .ToList();
        var freshPercents = results
            .Where(result => result.Percent.HasValue && !result.IsStale)
            .Select(result => result.Percent!.Value)
            .ToList();
        var percent = freshPercents.Count > 0
            ? freshPercents.Min()
            : usablePercents.Count > 0 ? usablePercents.Min() : null as int?;
        var connected = results.Any(result => result.IsConnected);
        var unavailableCount = results.Count(result => !result.Percent.HasValue && !result.IsConnected);
        var staleOnly = percent.HasValue && freshPercents.Count == 0;
        var status = unavailableCount == 0 ? "Ok" : $"{unavailableCount} unavailable";

        return new BatteryReadResult(
            "Bluetooth batteries",
            percent,
            connected,
            status,
            IsStale: staleOnly,
            Source: "Multiple",
            DeviceResults: results);
    }

    private static BatteryReadResult FallbackConnection(DeviceSettings deviceSettings, string bluetoothError, bool bluetoothConnected = false)
    {
        if (!UsbConnectionDetector.IsUsbConnected(deviceSettings))
        {
            return new BatteryReadResult(deviceSettings.DeviceDisplayName, null, bluetoothConnected, "Unavailable", bluetoothError, Source: "Bluetooth LE");
        }

        var lastKnownPercent = StatusStore.ReadLastKnownPercent(deviceSettings);
        return new BatteryReadResult(
            deviceSettings.DeviceDisplayName,
            lastKnownPercent,
            true,
            deviceSettings.UsbConnectionLabel,
            bluetoothError,
            IsStale: lastKnownPercent.HasValue,
            Source: deviceSettings.UsbConnectionLabel);
    }
}
