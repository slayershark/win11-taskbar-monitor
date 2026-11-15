@echo off
echo Stopping Win11TaskbarMonitor...
powershell -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'Run-WinTaskbarMonitor' -or $_.Name -match 'Win11TaskbarMonitor|WinTaskbarMonitor' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"
echo Done.
