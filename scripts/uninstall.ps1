# Win11TaskbarMonitor — Uninstallation Script
[CmdletBinding()]
param(
    [switch]$RemoveSettings
)

$ErrorActionPreference = "SilentlyContinue"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       Win11TaskbarMonitor — System Uninstallation        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Stop running processes
$running = Get-Process | Where-Object { $_.ProcessName -match "Win11TaskbarMonitor|WinTaskbarMonitor" }
if ($running) {
    Write-Host "`n[*] Stopping running instance..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

# 2. Remove Shortcuts from all locations
$shortcuts = @(
    "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Win11TaskbarMonitor.lnk",
    "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Win11TaskbarMonitor.lnk",
    "$([Environment]::GetFolderPath('CommonDesktopDirectory'))\Win11TaskbarMonitor.lnk",
    "$([Environment]::GetFolderPath('Desktop'))\Win11TaskbarMonitor.lnk",
    "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\WinTaskbarMonitor.lnk",
    "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\WinTaskbarMonitor.lnk",
    "$([Environment]::GetFolderPath('CommonDesktopDirectory'))\WinTaskbarMonitor.lnk",
    "$([Environment]::GetFolderPath('Desktop'))\WinTaskbarMonitor.lnk"
)

foreach ($sc in $shortcuts) {
    if (Test-Path $sc) {
        Remove-Item -Path $sc -Force
        Write-Host "[+] Removed shortcut: $sc" -ForegroundColor Green
    }
}

# 3. Remove from Windows Startup (Run key)
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
foreach ($k in @("Win11TaskbarMonitor", "WinTaskbarMonitor")) {
    $existingRun = Get-ItemProperty -Path $runKey -Name $k -ErrorAction SilentlyContinue
    if ($existingRun) {
        Remove-ItemProperty -Path $runKey -Name $k -Force
        Write-Host "[+] Removed $k from Windows Startup" -ForegroundColor Green
    }
}

# 4. Remove from Windows 'Installed Apps' / Add or Remove Programs (HKLM & HKCU)
$uninstallKeys = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Win11TaskbarMonitor",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Win11TaskbarMonitor",
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTaskbarMonitor",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTaskbarMonitor"
)

foreach ($uk in $uninstallKeys) {
    if (Test-Path $uk) {
        Remove-Item -Path $uk -Recurse -Force
        Write-Host "[+] Removed from Windows Installed Apps: $uk" -ForegroundColor Green
    }
}

# 5. Remove Settings if requested
if ($RemoveSettings) {
    foreach ($sd in @("$env:APPDATA\Win11TaskbarMonitor", "$env:APPDATA\WinTaskbarMonitor")) {
        if (Test-Path $sd) {
            Remove-Item -Path $sd -Recurse -Force
            Write-Host "[+] Removed application settings from AppData: $sd" -ForegroundColor Green
        }
    }
}

# 6. Remove Program Directories
$installDirs = @(
    "$env:ProgramFiles\Win11TaskbarMonitor",
    "$env:LOCALAPPDATA\Programs\Win11TaskbarMonitor",
    "$env:ProgramFiles\WinTaskbarMonitor",
    "$env:LOCALAPPDATA\Programs\WinTaskbarMonitor"
)

foreach ($dir in $installDirs) {
    if (Test-Path $dir) {
        Get-ChildItem -Path $dir -Exclude "uninstall.ps1" | Remove-Item -Recurse -Force
        Start-Process -WindowStyle Hidden -FilePath "powershell.exe" -ArgumentList "-NoProfile -Command `"Start-Sleep -Seconds 1; Remove-Item -Path '$dir' -Recurse -Force`""
        Write-Host "[+] Removed program directory: $dir" -ForegroundColor Green
    }
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "         Uninstallation Completed Successfully!           " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
