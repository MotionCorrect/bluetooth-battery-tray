$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
dotnet publish .\src\ZephyrusKeyboardBattery\ZephyrusKeyboardBattery.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
Write-Host "Published to $root\publish\win-x64\ZephyrusKeyboardBattery.exe"
