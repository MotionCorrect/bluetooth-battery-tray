$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
dotnet publish .\src\ZephyrusKeyboardBattery\ZephyrusKeyboardBattery.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o .\publish\win-x64
Write-Host "Published to $root\publish\win-x64\BluetoothBatteryTray.exe"
