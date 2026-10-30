# Read-only inventory. No elevation, installs, process termination or policy changes.
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run this inventory on the actual Windows photo booth.' }
$os = Get-CimInstance Win32_OperatingSystem
$computer = Get-CimInstance Win32_ComputerSystem
$printers = @(Get-CimInstance Win32_Printer | Select-Object Name, DriverName, PortName, Default, WorkOffline)
$knownProcesses = @(Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.ProcessName -match 'dslrBooth|LumaBooth|EOSUtility|PhotoBooth' } |
    Select-Object ProcessName, Id)
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$report = [ordered]@{
    recordedAt = (Get-Date).ToUniversalTime().ToString('o')
    windows = $os.Caption
    version = $os.Version
    build = $os.BuildNumber
    architecture = $os.OSArchitecture
    memoryGiB = [Math]::Round($computer.TotalPhysicalMemory / 1GB, 1)
    dotnetOnPath = [bool]$dotnet
    printers = $printers
    potentialCameraOwners = $knownProcesses
    note = 'Inventory only. Camera, printing, kiosk behavior and runtime compatibility are unverified.'
}
$report | ConvertTo-Json -Depth 4
