@echo off
setlocal
echo Starting Win11TaskbarMonitor...
start "" powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0scripts\Run-WinTaskbarMonitor.ps1"
