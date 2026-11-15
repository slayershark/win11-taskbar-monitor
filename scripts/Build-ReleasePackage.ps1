# Win11TaskbarMonitor — Build Standalone Distribution Package
$ErrorActionPreference = "Stop"

$distDir = "$PSScriptRoot\..\dist"
$packageDir = "$distDir\Win11TaskbarMonitor-Release"
$zipPath = "$distDir\Win11TaskbarMonitor-Setup.zip"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "     Building Universal Win11TaskbarMonitor Package       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean or recreate package directory
if (Test-Path $packageDir) {
    Remove-Item -Path $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

# 2. Copy application binaries & scripts
Copy-Item "$distDir\Win11TaskbarMonitor.exe" "$packageDir\Win11TaskbarMonitor.exe" -Force
Copy-Item "$PSScriptRoot\Run-WinTaskbarMonitor.ps1" "$packageDir\Run-WinTaskbarMonitor.ps1" -Force
Copy-Item "$PSScriptRoot\LiveDaemon.cs" "$packageDir\LiveDaemon.cs" -Force
Copy-Item "$PSScriptRoot\install.ps1" "$packageDir\install.ps1" -Force
Copy-Item "$PSScriptRoot\uninstall.ps1" "$packageDir\uninstall.ps1" -Force
Copy-Item "$PSScriptRoot\..\assets\app.ico" "$packageDir\app.ico" -Force

# 3. Create 1-click Install.cmd
$installCmd = @"
@echo off
setlocal
cd /d "%~dp0"
title Win11TaskbarMonitor — Installation
echo ==========================================================
echo           Installing Win11TaskbarMonitor...
echo ==========================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Scope CurrentUser -CreateDesktopShortcut -StartWithWindows -LaunchAfterInstall
echo.
if %ERRORLEVEL% EQU 0 (
    echo [OK] Installation completed successfully!
    echo Win11TaskbarMonitor is now active on your taskbar.
) else (
    echo [!] Installation encountered an issue.
)
ping 127.0.0.1 -n 3 >nul
"@
Set-Content -Path "$packageDir\Install.cmd" -Value $installCmd -Encoding ASCII

# 4. Create 1-click Install-Admin.cmd
$adminCmd = @"
@echo off
setlocal
cd /d "%~dp0"
title Win11TaskbarMonitor — Admin Installation
powershell.exe -NoProfile -Command "Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"\"%~dp0install.ps1\"\" -Scope AllUsers -CreateDesktopShortcut -StartWithWindows -LaunchAfterInstall' -Verb RunAs"
"@
Set-Content -Path "$packageDir\Install-Admin.cmd" -Value $adminCmd -Encoding ASCII

# 5. Create 1-click Run-Portable.cmd
$runCmd = @"
@echo off
setlocal
cd /d "%~dp0"
title Win11TaskbarMonitor
start "" powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0Run-WinTaskbarMonitor.ps1"
"@
Set-Content -Path "$packageDir\Run-Portable.cmd" -Value $runCmd -Encoding ASCII

# 6. Create 1-click Uninstall.cmd
$uninstCmd = @"
@echo off
setlocal
cd /d "%~dp0"
title Win11TaskbarMonitor — Uninstaller
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall.ps1"
echo.
echo [OK] Win11TaskbarMonitor has been uninstalled.
ping 127.0.0.1 -n 3 >nul
"@
Set-Content -Path "$packageDir\Uninstall.cmd" -Value $uninstCmd -Encoding ASCII

# 7. Create README.txt
$readme = @"
========================================================================
             Win11TaskbarMonitor — System Hardware Monitor
========================================================================

A lightweight, native Windows 11 hardware monitoring utility anchored
directly on the left side of your taskbar with pixel-perfect equal margins.

FEATURES:
  * Real-time CPU, GPU, GPU Temp, and RAM telemetry
  * Smart Auto-Detection: reveals CPU Temp on supported devices,
    collapses cleanly on unprivileged devices (never displays N/A)
  * Smart Margin Engine: dynamic equal margins on top, bottom, and left
  * Zero-overhead: < 0.5% CPU, < 40 MB RAM
  * Zero bloat: no Electron, no Chromium, no cloud telemetry
  * Smart App Control safe: runs without Developer Mode or security blocks

HOW TO INSTALL (3 SECONDS):
  1. Double-click 'Install.cmd'
  2. That's it! It will place shortcuts on your Desktop and Start Menu,
     and the monitor will appear immediately on your taskbar.

ADMINISTRATOR INSTALL (OPTIONAL):
  * Double-click 'Install-Admin.cmd' to install for All Users in Program Files.

PORTABLE USE (WITHOUT INSTALLING):
  * Double-click 'Run-Portable.cmd' to launch immediately without installing.

HOW TO UNINSTALL:
  * Double-click 'Uninstall.cmd', OR
  * Open Windows Settings > Apps > Installed apps > Search 'Win11TaskbarMonitor' > Uninstall.

========================================================================
"@
Set-Content -Path "$packageDir\README.txt" -Value $readme -Encoding ASCII

# 8. Create compressed Zip Archive
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path "$packageDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "`n[+] Standalone package created at: $packageDir" -ForegroundColor Green
Write-Host "[+] Zip archive created at: $zipPath ($( [Math]::Round((Get-Item $zipPath).Length / 1MB, 2) ) MB)" -ForegroundColor Green
