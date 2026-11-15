@echo off
setlocal
echo Starting WinTaskbarMonitor...
start "" powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0scripts\Run-WinTaskbarMonitor.ps1"
