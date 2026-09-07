# Bluetooth Battery Tray

A tiny configurable Windows tray utility for monitoring a Bluetooth Low Energy peripheral that exposes the standard BLE Battery Service.

Device-specific Bluetooth/USB identifiers live in a local settings file, not in code.

## Features

- Reads exact battery percentage from the standard BLE Battery Service.
- Displays large, color-coded percentage digits in the Windows notification area.
- Shows exact status in the tray tooltip and right-click menu.
- Warns when battery is low.
- Optional USB-C/wired-mode detection using a configurable USB/HID device-id fragment.
- In USB-C mode, can show `USB-C connected` and the last known Bluetooth percentage when available.
- Optional current-user auto-start via the Windows `HKCU` Run key.
- No installer required; publish as a single self-contained Windows executable.

## Requirements

- Windows 10 or later
- A Bluetooth LE device exposing:
  - Battery Service: `0000180f-0000-1000-8000-00805f9b34fb`
  - Battery Level characteristic: `00002a19-0000-1000-8000-00805f9b34fb`
- .NET 8 SDK to build from source

## Configure a device

List paired/present BLE devices:

```bash
BluetoothBatteryTray.exe --list-bluetooth
BluetoothBatteryTray.exe --list-bluetooth --filter keyboard
```

Configure the app with a display name and BLE address:

```bash
BluetoothBatteryTray.exe --configure \
  --name "My Keyboard" \
  --address AABBCCDDEEFF
```

Optional USB-C/wired fallback detection:

```bash
BluetoothBatteryTray.exe --list-usb --filter keyboard

BluetoothBatteryTray.exe --configure \
  --name "My Keyboard" \
  --address AABBCCDDEEFF \
  --usb-id "VID_1234&PID_5678"
```

Show the active local config:

```bash
BluetoothBatteryTray.exe --show-config
```

Settings are stored at:

```text
%LOCALAPPDATA%\BluetoothBatteryTray\settings.json
```

Example config:

```json
{
  "DeviceDisplayName": "My Keyboard",
  "BluetoothAddress": "AABBCCDDEEFF",
  "UsbDeviceIdContains": "VID_1234&PID_5678",
  "LowThresholdPercent": 20,
  "WarningThresholdPercent": 30,
  "PollIntervalSeconds": 300,
  "LowBatteryRenotifyHours": 8
}
```

## Tray behavior

- Green badge: battery above warning threshold.
- Yellow badge: battery at or below warning threshold.
- Red badge: battery at or below low threshold.
- Gray/USB badge: Bluetooth percentage unavailable or USB-C/wired mode detected.
- Lightning marker: USB-C/wired mode detected.

Right-click the tray icon for:

- current battery status
- `Check now`
- `Open settings file`
- `Start with Windows`
- `Exit`

## Build locally

```bash
dotnet restore ZephyrusKeyboardBattery.sln
dotnet build ZephyrusKeyboardBattery.sln --configuration Release
```

## Run a one-shot battery check

```bash
dotnet run --project src/ZephyrusKeyboardBattery/ZephyrusKeyboardBattery.csproj -- --check-once
```

A successful Bluetooth read looks like:

```text
My Keyboard: 87%
```

The app also writes its latest status to:

```text
%LOCALAPPDATA%\BluetoothBatteryTray\last-status.txt
```

## Publish single executable

```bash
dotnet publish src/ZephyrusKeyboardBattery/ZephyrusKeyboardBattery.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  --output publish/win-x64
```

Published executable:

```text
publish\win-x64\BluetoothBatteryTray.exe
```

## Auto-start

Install current-user startup using the published executable:

```bash
./publish/win-x64/BluetoothBatteryTray.exe --install-startup
```

Remove current-user startup:

```bash
./publish/win-x64/BluetoothBatteryTray.exe --uninstall-startup
```

The registry value is stored at:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\BluetoothBatteryTray
```

## CI and releases

GitHub Actions builds the app on `windows-latest`, publishes a self-contained `win-x64` executable, and uploads it as an artifact.

Tagged releases include the GitHub source archives automatically plus a downloadable Windows binary archive.

## Limitations

- Fresh exact battery percentage comes from Bluetooth LE only.
- USB-C/wired mode detection is best-effort and does not guarantee a live USB battery percentage; it may show the last known Bluetooth percentage.
- Charging state is not authoritative unless a device exposes charging telemetry through some other device-specific interface.
- Some peripherals sleep aggressively; if the battery service is temporarily unreachable, wake the device and use `Check now`.

## License

MIT
