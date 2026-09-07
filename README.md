# Zephyrus Keyboard Battery

A tiny Windows tray utility for monitoring the **ASUS ROG Zephyrus Duo Keyboard** battery on this PC.

This is intentionally small and special-purpose: it hardcodes the known keyboard identifiers and focuses on a glanceable tray indicator rather than a generic Bluetooth-device dashboard.

## Features

- Reads exact battery percentage from the keyboard's Bluetooth LE Battery Service.
- Displays large, color-coded percentage digits in the Windows notification area.
- Shows the exact status in the tray tooltip and right-click menu.
- Warns when battery is low (`<= 20%`).
- Detects the USB-C/wired device path when Bluetooth battery reads are unavailable.
- In USB-C mode, shows `USB-C connected` and, when available, keeps displaying the last known Bluetooth battery percentage.
- Optional current-user auto-start via the Windows `HKCU` Run key.
- No installer required; publish as a single self-contained Windows executable.

## Hardware assumptions

The app is built for this known local device:

| Item | Value |
| --- | --- |
| Windows Bluetooth name | `Zephyrus Duo Keyboard` |
| BLE address | `CD4AA5ADDD4B` |
| BLE Battery Service | `0000180f-0000-1000-8000-00805f9b34fb` |
| BLE Battery Level characteristic | `00002a19-0000-1000-8000-00805f9b34fb` |
| USB-C/wired identifier | `VID_0B05&PID_193B` |

If the keyboard is re-paired and Windows assigns a different BLE address, update `KeyboardBluetoothAddress` in `src/ZephyrusKeyboardBattery/AppConstants.cs`.

## Tray behavior

- Green badge: battery above warning threshold.
- Yellow badge: battery at or below `30%`.
- Red badge: battery at or below `20%`.
- Gray/USB badge: Bluetooth percentage unavailable or USB-C/wired mode detected.
- Lightning marker: USB-C/wired mode detected.

Right-click the tray icon for:

- current battery status
- `Check now`
- `Start with Windows`
- `Exit`

## Build locally

Requirements:

- Windows 10 or later
- .NET 8 SDK

```bash
cd /c/git/zephyrus-keyboard-battery

dotnet restore ZephyrusKeyboardBattery.sln
dotnet build ZephyrusKeyboardBattery.sln --configuration Release
```

## Run a one-shot battery check

```bash
dotnet run --project src/ZephyrusKeyboardBattery/ZephyrusKeyboardBattery.csproj -- --check-once
```

A successful Bluetooth read looks like:

```text
Zephyrus Duo Keyboard: 100%
```

The app also writes its latest status to:

```text
%LOCALAPPDATA%\ZephyrusKeyboardBattery\last-status.txt
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
publish\win-x64\ZephyrusKeyboardBattery.exe
```

## Auto-start

Install current-user startup using the published executable:

```bash
./publish/win-x64/ZephyrusKeyboardBattery.exe --install-startup
```

Remove current-user startup:

```bash
./publish/win-x64/ZephyrusKeyboardBattery.exe --uninstall-startup
```

The registry value is stored at:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ZephyrusKeyboardBattery
```

## CI

GitHub Actions builds the app on `windows-latest`, publishes a self-contained `win-x64` executable, and uploads it as an artifact.

## Limitations

- Fresh exact battery percentage comes from Bluetooth LE only.
- USB-C/wired mode detection is best-effort and does not guarantee a live USB battery percentage; it may show the last known Bluetooth percentage.
- Charging state is not authoritative because the keyboard does not expose a separate standard charging-status characteristic through the probed BLE services.

## License

MIT
