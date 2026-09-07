using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace ZephyrusKeyboardBattery;

public sealed class BatteryReader : IBatteryReader
{
    private static readonly Guid BatteryServiceUuid = Guid.Parse("0000180f-0000-1000-8000-00805f9b34fb");
    private static readonly Guid BatteryLevelUuid = Guid.Parse("00002a19-0000-1000-8000-00805f9b34fb");

    public async Task<BatteryReadResult> ReadAsync(TimeSpan? timeout = null)
    {
        var readTask = ReadCoreAsync();
        var maxWait = timeout ?? TimeSpan.FromSeconds(20);
        var completed = await Task.WhenAny(readTask, Task.Delay(maxWait));
        if (completed != readTask)
        {
            return new BatteryReadResult(null, false, "Timed out", $"No BLE response within {maxWait.TotalSeconds:0}s");
        }

        return await readTask;
    }

    private static async Task<BatteryReadResult> ReadCoreAsync()
    {
        try
        {
            using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(AppConstants.KeyboardBluetoothAddress);
            if (device is null)
            {
                return UsbFallback("Device not found");
            }

            var connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected;

            var services = await device.GetGattServicesForUuidAsync(BatteryServiceUuid, BluetoothCacheMode.Uncached);
            if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
            {
                return UsbFallback($"Battery service read failed: {services.Status}", connected);
            }

            using var service = services.Services[0];
            var characteristics = await service.GetCharacteristicsForUuidAsync(BatteryLevelUuid, BluetoothCacheMode.Uncached);
            if (characteristics.Status != GattCommunicationStatus.Success || characteristics.Characteristics.Count == 0)
            {
                return UsbFallback($"Battery characteristic read failed: {characteristics.Status}", connected);
            }

            var read = await characteristics.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Uncached);
            if (read.Status != GattCommunicationStatus.Success)
            {
                return UsbFallback($"Battery value read failed: {read.Status}", connected);
            }

            var reader = DataReader.FromBuffer(read.Value);
            var percent = Math.Clamp(reader.ReadByte(), (byte)0, (byte)100);
            return new BatteryReadResult(percent, connected, "Ok", Source: "Bluetooth LE");
        }
        catch (Exception ex)
        {
            return UsbFallback(ex.Message);
        }
    }

    private static BatteryReadResult UsbFallback(string bluetoothError, bool bluetoothConnected = false)
    {
        if (!UsbConnectionDetector.IsUsbConnected())
        {
            return new BatteryReadResult(null, bluetoothConnected, "Unavailable", bluetoothError, Source: "Bluetooth LE");
        }

        var lastKnownPercent = StatusStore.ReadLastKnownPercent();
        return new BatteryReadResult(
            lastKnownPercent,
            true,
            "USB-C connected",
            bluetoothError,
            IsStale: lastKnownPercent.HasValue,
            Source: "USB-C");
    }
}
