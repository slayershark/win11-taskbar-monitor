@echo off
setlocal
echo Installing WinTaskbarMonitor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\install.ps1" -CreateDesktopShortcut
pause
