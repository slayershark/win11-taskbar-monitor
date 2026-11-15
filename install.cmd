@echo off
setlocal
echo ==========================================================
echo       Installing Win11TaskbarMonitor to Program Files...
echo ==========================================================
echo.
powershell.exe -NoProfile -Command "Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"\"%~dp0scripts\install.ps1\"\" -Scope AllUsers -CreateDesktopShortcut -StartWithWindows -LaunchAfterInstall' -Verb RunAs"
