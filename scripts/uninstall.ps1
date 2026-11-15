# WinTaskbarMonitor — Uninstallation Script
[CmdletBinding()]
param(
    [switch]$RemoveSettings
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "        WinTaskbarMonitor — System Uninstallation         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Stop running processes
$running = Get-Process -Name "WinTaskbarMonitor" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "`n[*] Stopping running instance..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

# 2. Remove Shortcuts
$startMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\WinTaskbarMonitor.lnk"
if (Test-Path $startMenuShortcut) {
    Remove-Item -Path $startMenuShortcut -Force
    Write-Host "[+] Removed Start Menu shortcut" -ForegroundColor Green
}

$desktopShortcut = "$([Environment]::GetFolderPath('Desktop'))\WinTaskbarMonitor.lnk"
if (Test-Path $desktopShortcut) {
    Remove-Item -Path $desktopShortcut -Force
    Write-Host "[+] Removed Desktop shortcut" -ForegroundColor Green
}

# 3. Remove from Windows Startup (Run key)
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$existingRun = Get-ItemProperty -Path $runKey -Name "WinTaskbarMonitor" -ErrorAction SilentlyContinue
if ($existingRun) {
    Remove-ItemProperty -Path $runKey -Name "WinTaskbarMonitor" -Force
    Write-Host "[+] Removed from Windows Startup" -ForegroundColor Green
}

# 4. Remove from Windows 'Installed Apps' / Add or Remove Programs
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTaskbarMonitor"
if (Test-Path $uninstallKey) {
    Remove-Item -Path $uninstallKey -Recurse -Force
    Write-Host "[+] Removed from Windows Installed Apps registration" -ForegroundColor Green
}

# 5. Remove Settings if requested
if ($RemoveSettings) {
    $settingsDir = "$env:APPDATA\WinTaskbarMonitor"
    if (Test-Path $settingsDir) {
        Remove-Item -Path $settingsDir -Recurse -Force
        Write-Host "[+] Removed application settings from AppData" -ForegroundColor Green
    }
}

# 6. Remove Program Directory
$installDir = "$env:LOCALAPPDATA\Programs\WinTaskbarMonitor"
if (Test-Path $installDir) {
    # Remove files except this uninstaller if it is currently executing from within installDir
    Get-ChildItem -Path $installDir -Exclude "uninstall.ps1" | Remove-Item -Recurse -Force
    
    # Schedule removal of the remaining directory after script termination
    Start-Process -WindowStyle Hidden -FilePath "powershell.exe" -ArgumentList "-NoProfile -Command `"Start-Sleep -Seconds 1; Remove-Item -Path '$installDir' -Recurse -Force`""
    Write-Host "[+] Cleaned up installation folder" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "         Uninstallation Completed Successfully!           " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
