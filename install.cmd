@echo off
setlocal
title Win11TaskbarMonitor — Installation
echo ==========================================================
echo          Installing Win11TaskbarMonitor...
echo ==========================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\install.ps1" -Scope CurrentUser -CreateDesktopShortcut -StartWithWindows -LaunchAfterInstall
echo.
if %ERRORLEVEL% NEQ 0 (
    echo [!] Installation encountered an issue.
    pause
)
