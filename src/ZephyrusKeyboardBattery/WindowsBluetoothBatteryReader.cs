using System.Management;
using System.Runtime.InteropServices;

namespace ZephyrusKeyboardBattery;

internal static class WindowsBluetoothBatteryReader
{
    private static readonly DEVPROPKEY DeviceBatteryLifeProperty = new()
    {
        fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"),
        pid = 2
    };

    public static int? TryReadPercent(DeviceSettings deviceSettings)
    {
        if (!deviceSettings.TryGetBluetoothAddress(out _))
        {
            return null;
        }

        var address = NormalizeAddress(deviceSettings.BluetoothAddress);
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        foreach (var instanceId in FindPrimaryBluetoothDeviceInstanceIds(address))
        {
            var percent = TryReadBatteryLifeProperty(instanceId);
            if (percent.HasValue)
            {
                return percent;
            }
        }

        return null;
    }

    private static IEnumerable<string> FindPrimaryBluetoothDeviceInstanceIds(string normalizedAddress)
    {
        ManagementObjectSearcher? searcher = null;
        try
        {
            searcher = new ManagementObjectSearcher("SELECT PNPDeviceID, Name, PNPClass FROM Win32_PnPEntity");
            foreach (ManagementBaseObject item in searcher.Get())
            {
                using (item)
                {
                    var instanceId = item["PNPDeviceID"] as string;
                    if (string.IsNullOrWhiteSpace(instanceId))
                    {
                        continue;
                    }

                    var compactId = NormalizeInstanceId(instanceId);
                    if (compactId.StartsWith($"BTHLE\\DEV_{normalizedAddress}\\", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return instanceId;
                    }
                }
            }
        }
        finally
        {
            searcher?.Dispose();
        }
    }

    private static int? TryReadBatteryLifeProperty(string instanceId)
    {
        var locateResult = CM_Locate_DevNode(out var devInst, instanceId, 0);
        if (locateResult != 0)
        {
            return null;
        }

        var propertyKey = DeviceBatteryLifeProperty;
        uint propertyType;
        uint bufferSize = 4;
        var buffer = new byte[bufferSize];
        var readResult = CM_Get_DevNode_Property(devInst, ref propertyKey, out propertyType, buffer, ref bufferSize, 0);
        if (readResult != 0 || bufferSize == 0)
        {
            return null;
        }

        var percent = buffer[0];
        return percent <= 100 ? percent : null;
    }

    private static string NormalizeAddress(string? address)
    {
        return (address ?? string.Empty)
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(":", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .Trim()
            .ToUpperInvariant();
    }

    private static string NormalizeInstanceId(string instanceId)
    {
        return instanceId.Trim().ToUpperInvariant();
    }

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNode(out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_DevNode_Property(
        uint dnDevInst,
        ref DEVPROPKEY propertyKey,
        out uint propertyType,
        byte[] propertyBuffer,
        ref uint propertyBufferSize,
        uint ulFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;
    }
}
