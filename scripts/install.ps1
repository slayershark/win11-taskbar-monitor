# WinTaskbarMonitor — Installation Script
[CmdletBinding()]
param(
    [switch]$StartWithWindows,
    [switch]$CreateDesktopShortcut,
    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "         WinTaskbarMonitor — System Installation          " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Paths
$sourceExe = "$PSScriptRoot\..\dist\WinTaskbarMonitor.exe"
if (!(Test-Path $sourceExe)) {
    # If dist hasn't been built yet, build it
    Write-Host "`n[*] Building release executable..." -ForegroundColor Yellow
    $dotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\dotnet" }
    $env:Path = "$dotnetRoot;" + $env:Path
    & dotnet publish "$PSScriptRoot\..\src\WinTaskbarMonitor\WinTaskbarMonitor.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$PSScriptRoot\..\dist" | Out-Null
}

if (!(Test-Path $sourceExe)) {
    Write-Error "Could not find WinTaskbarMonitor.exe in $sourceExe"
    exit 1
}

$installDir = "$env:LOCALAPPDATA\Programs\WinTaskbarMonitor"
$targetExe = "$installDir\WinTaskbarMonitor.exe"
$targetUninstallScript = "$installDir\uninstall.ps1"

Write-Host "`n[*] Installing to: $installDir" -ForegroundColor Gray

# 2. Stop running instances if any
$running = Get-Process -Name "WinTaskbarMonitor" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "[*] Stopping running instance..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

# 3. Create target directory and copy files
if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
}

Copy-Item -Path $sourceExe -Destination $targetExe -Force
Copy-Item -Path "$PSScriptRoot\uninstall.ps1" -Destination $targetUninstallScript -Force
Write-Host "[+] Copied executable and uninstaller" -ForegroundColor Green

# 4. Create Start Menu Shortcut
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$shortcutPath = "$startMenuDir\WinTaskbarMonitor.lnk"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Lightweight Windows 11 Taskbar System Monitor"
$shortcut.IconLocation = "$targetExe,0"
$shortcut.Save()
Write-Host "[+] Created Start Menu shortcut" -ForegroundColor Green

# 5. Optional Desktop Shortcut
if ($CreateDesktopShortcut) {
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $desktopShortcut = "$desktopPath\WinTaskbarMonitor.lnk"
    $scDesktop = $wshShell.CreateShortcut($desktopShortcut)
    $scDesktop.TargetPath = $targetExe
    $scDesktop.WorkingDirectory = $installDir
    $scDesktop.Description = "Lightweight Windows 11 Taskbar System Monitor"
    $scDesktop.IconLocation = "$targetExe,0"
    $scDesktop.Save()
    Write-Host "[+] Created Desktop shortcut" -ForegroundColor Green
}

# 6. Register in Windows 'Installed Apps' / Add or Remove Programs
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTaskbarMonitor"
if (!(Test-Path $uninstallKey)) {
    New-Item -Path $uninstallKey -Force | Out-Null
}

$exeSizeKb = [Math]::Round((Get-Item $targetExe).Length / 1KB)

Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "WinTaskbarMonitor"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "slayershark"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $installDir
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value "$targetExe,0"
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$targetUninstallScript`""
Set-ItemProperty -Path $uninstallKey -Name "EstimatedSize" -Value $exeSizeKb
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -Type DWord
Write-Host "[+] Registered in Windows Installed Apps" -ForegroundColor Green

# 7. Optional Start with Windows
if ($StartWithWindows) {
    $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    Set-ItemProperty -Path $runKey -Name "WinTaskbarMonitor" -Value "`"$targetExe`""
    Write-Host "[+] Enabled Start with Windows" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "          Installation Completed Successfully!            " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "You can now find WinTaskbarMonitor in your Start Menu." -ForegroundColor White

if ($LaunchAfterInstall) {
    Write-Host "`n[*] Launching WinTaskbarMonitor..." -ForegroundColor Yellow
    Start-Process -FilePath $targetExe
}
