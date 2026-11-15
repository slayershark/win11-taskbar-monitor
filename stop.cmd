@echo off
echo Stopping WinTaskbarMonitor...
powershell -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'Run-WinTaskbarMonitor' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }"
echo Done.
