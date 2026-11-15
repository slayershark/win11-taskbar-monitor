# Win11TaskbarMonitor — Installation Script
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
Write-Host "        Win11TaskbarMonitor — System Installation         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Paths
$sourceExe = "$PSScriptRoot\..\dist\Win11TaskbarMonitor.exe"
if (!(Test-Path $sourceExe)) {
    Write-Host "`n[*] Building release executable..." -ForegroundColor Yellow
    $dotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\dotnet" }
    $env:Path = "$dotnetRoot;" + $env:Path
    & dotnet publish "$PSScriptRoot\..\src\WinTaskbarMonitor\WinTaskbarMonitor.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$PSScriptRoot\..\dist" | Out-Null
}

if (!(Test-Path $sourceExe)) {
    Write-Error "Could not find Win11TaskbarMonitor.exe in $sourceExe"
    exit 1
}

# Determine target folder based on Scope
if ($Scope -eq "AllUsers") {
    $installDir = "$env:ProgramFiles\Win11TaskbarMonitor"
    $startMenuDir = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
    $desktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Win11TaskbarMonitor"
} else {
    $installDir = "$env:LOCALAPPDATA\Programs\Win11TaskbarMonitor"
    $startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Win11TaskbarMonitor"
}

$targetExe = "$installDir\Win11TaskbarMonitor.exe"
$targetUninstallScript = "$installDir\uninstall.ps1"

Write-Host "`n[*] Installing to ($Scope): $installDir" -ForegroundColor Gray

# 2. Stop running instances if any
$running = Get-Process | Where-Object { $_.ProcessName -match "Win11TaskbarMonitor|WinTaskbarMonitor" }
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
Unblock-File -Path $targetExe -ErrorAction SilentlyContinue
Copy-Item -Path "$PSScriptRoot\uninstall.ps1" -Destination $targetUninstallScript -Force
Copy-Item -Path "$PSScriptRoot\Run-WinTaskbarMonitor.ps1" -Destination "$installDir\Run-WinTaskbarMonitor.ps1" -Force
Copy-Item -Path "$PSScriptRoot\LiveDaemon.cs" -Destination "$installDir\LiveDaemon.cs" -Force
$icoSource = "$PSScriptRoot\..\assets\app.ico"
$targetIco = "$installDir\app.ico"
if (Test-Path $icoSource) {
    Copy-Item -Path $icoSource -Destination $targetIco -Force
}
Write-Host "[+] Copied executable, icon, runner daemon, and uninstaller" -ForegroundColor Green

# Remove legacy WinTaskbarMonitor shortcuts if present
Remove-Item "$startMenuDir\WinTaskbarMonitor.lnk" -ErrorAction SilentlyContinue
Remove-Item "$desktopPath\WinTaskbarMonitor.lnk" -ErrorAction SilentlyContinue

# 4. Create Start Menu Shortcut (Using silent runner to prevent SAC policy blocks)
$shortcutPath = "$startMenuDir\Win11TaskbarMonitor.lnk"
$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "powershell.exe"
$shortcut.Arguments = "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$installDir\Run-WinTaskbarMonitor.ps1`""
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Lightweight Windows 11 Taskbar System Monitor"
$shortcut.IconLocation = if (Test-Path $targetIco) { "$targetIco,0" } else { "$targetExe,0" }
$shortcut.Save()
Write-Host "[+] Created Start Menu shortcut in $startMenuDir" -ForegroundColor Green

# 5. Optional Desktop Shortcut
if ($CreateDesktopShortcut) {
    $desktopShortcut = "$desktopPath\Win11TaskbarMonitor.lnk"
    $scDesktop = $wshShell.CreateShortcut($desktopShortcut)
    $scDesktop.TargetPath = "powershell.exe"
    $scDesktop.Arguments = "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$installDir\Run-WinTaskbarMonitor.ps1`""
    $scDesktop.WorkingDirectory = $installDir
    $scDesktop.Description = "Lightweight Windows 11 Taskbar System Monitor"
    $scDesktop.IconLocation = if (Test-Path $targetIco) { "$targetIco,0" } else { "$targetExe,0" }
    $scDesktop.Save()
    Write-Host "[+] Created Desktop shortcut" -ForegroundColor Green
}

# 6. Register in Windows 'Installed Apps' / Add or Remove Programs
if (!(Test-Path $uninstallKey)) {
    New-Item -Path $uninstallKey -Force | Out-Null
}

$exeSizeKb = [Math]::Round((Get-Item $targetExe).Length / 1KB)

Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "Win11TaskbarMonitor"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.1.3"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "slayershark"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $installDir
$displayIcon = if (Test-Path $targetIco) { "$targetIco,0" } else { "$targetExe,0" }
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value $displayIcon
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$targetUninstallScript`""
Set-ItemProperty -Path $uninstallKey -Name "EstimatedSize" -Value $exeSizeKb
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -Type DWord
Write-Host "[+] Registered in Windows Installed Apps" -ForegroundColor Green

# 7. Optional Start with Windows
if ($StartWithWindows) {
    $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $runVal = "powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$installDir\Run-WinTaskbarMonitor.ps1`""
    Set-ItemProperty -Path $runKey -Name "Win11TaskbarMonitor" -Value $runVal
    Write-Host "[+] Enabled Start with Windows" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "          Installation Completed Successfully!            " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Win11TaskbarMonitor is now installed." -ForegroundColor White
Write-Host "Zero-policy runner enables seamless execution without Developer Mode." -ForegroundColor White

if ($LaunchAfterInstall) {
    Write-Host "`n[*] Launching Win11TaskbarMonitor..." -ForegroundColor Yellow
    Start-Process powershell.exe -ArgumentList "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$installDir\Run-WinTaskbarMonitor.ps1`""
}
