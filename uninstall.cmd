@echo off
setlocal
echo ==========================================================
echo             Uninstalling Win11TaskbarMonitor...
echo ==========================================================
echo.
powershell.exe -NoProfile -Command "Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"\"%~dp0scripts\uninstall.ps1\"\"' -Verb RunAs"
