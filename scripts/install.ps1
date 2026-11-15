# WinTaskbarMonitor — Installation Script
[CmdletBinding()]
param(
    [ValidateSet("AllUsers", "CurrentUser")]
    [string]$Scope = "AllUsers",
    [switch]$StartWithWindows,
    [switch]$CreateDesktopShortcut,
    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# If AllUsers requested and not elevated, prompt for UAC elevation (standard for Program Files installers)
if ($Scope -eq "AllUsers" -and !$isAdmin) {
    Write-Host "[*] Elevating to Administrator to install into Program Files..." -ForegroundColor Yellow
    $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Scope AllUsers"
    if ($StartWithWindows) { $argList += " -StartWithWindows" }
    if ($CreateDesktopShortcut) { $argList += " -CreateDesktopShortcut" }
    if ($LaunchAfterInstall) { $argList += " -LaunchAfterInstall" }
    
    Start-Process powershell.exe -ArgumentList $argList -Verb RunAs
    exit 0
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "         WinTaskbarMonitor — System Installation          " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Paths
$sourceExe = "$PSScriptRoot\..\dist\WinTaskbarMonitor.exe"
if (!(Test-Path $sourceExe)) {
    Write-Host "`n[*] Building release executable..." -ForegroundColor Yellow
    $dotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\dotnet" }
    $env:Path = "$dotnetRoot;" + $env:Path
    & dotnet publish "$PSScriptRoot\..\src\WinTaskbarMonitor\WinTaskbarMonitor.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$PSScriptRoot\..\dist" | Out-Null
}

if (!(Test-Path $sourceExe)) {
    Write-Error "Could not find WinTaskbarMonitor.exe in $sourceExe"
    exit 1
}

# Determine target folder based on Scope
if ($Scope -eq "AllUsers") {
    $installDir = "$env:ProgramFiles\WinTaskbarMonitor"
    $startMenuDir = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
    $desktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTaskbarMonitor"
} else {
    $installDir = "$env:LOCALAPPDATA\Programs\WinTaskbarMonitor"
    $startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTaskbarMonitor"
}

$targetExe = "$installDir\WinTaskbarMonitor.exe"
$targetUninstallScript = "$installDir\uninstall.ps1"

Write-Host "`n[*] Installing to ($Scope): $installDir" -ForegroundColor Gray

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
$shortcutPath = "$startMenuDir\WinTaskbarMonitor.lnk"
$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Lightweight Windows 11 Taskbar System Monitor"
$shortcut.IconLocation = "$targetExe,0"
$shortcut.Save()
Write-Host "[+] Created Start Menu shortcut in $startMenuDir" -ForegroundColor Green

# 5. Optional Desktop Shortcut
if ($CreateDesktopShortcut) {
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
Write-Host "WinTaskbarMonitor is now installed in Program Files." -ForegroundColor White
Write-Host "Protected directory status enables execution without Developer Mode." -ForegroundColor White

if ($LaunchAfterInstall) {
    Write-Host "`n[*] Launching WinTaskbarMonitor..." -ForegroundColor Yellow
    Start-Process -FilePath $targetExe
}
