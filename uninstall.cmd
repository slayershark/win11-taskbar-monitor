@echo off
setlocal
echo Uninstalling WinTaskbarMonitor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\uninstall.ps1"
pause
