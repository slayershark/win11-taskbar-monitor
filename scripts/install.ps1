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

# 1. Resolve source files (supports both repo structure and standalone release folder)
$sourceAppDir = $null
if (Test-Path "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App") {
    $sourceAppDir = "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App"
} elseif (Test-Path "$PSScriptRoot\Win11TaskbarMonitor-App") {
    $sourceAppDir = "$PSScriptRoot\Win11TaskbarMonitor-App"
}

$sourceExe = $null
if (Test-Path "$PSScriptRoot\Win11TaskbarMonitor.exe") {
    $sourceExe = "$PSScriptRoot\Win11TaskbarMonitor.exe"
} elseif (Test-Path "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App\Win11TaskbarMonitor.exe") {
    $sourceExe = "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App\Win11TaskbarMonitor.exe"
} elseif (Test-Path "$PSScriptRoot\..\dist\Win11TaskbarMonitor-Standalone\Win11TaskbarMonitor.exe") {
    $sourceExe = "$PSScriptRoot\..\dist\Win11TaskbarMonitor-Standalone\Win11TaskbarMonitor.exe"
} elseif (Test-Path "$PSScriptRoot\..\dist\Win11TaskbarMonitor.exe") {
    $sourceExe = "$PSScriptRoot\..\dist\Win11TaskbarMonitor.exe"
} else {
    # If in a dev environment with dotnet, attempt build
    $dotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\dotnet" }
    $env:Path = "$dotnetRoot;" + $env:Path
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        Write-Host "`n[*] Building release executable..." -ForegroundColor Yellow
        & dotnet publish "$PSScriptRoot\..\src\WinTaskbarMonitor\WinTaskbarMonitor.csproj" -c Release -r win-x64 --self-contained false -o "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App" 2>$null | Out-Null
        if (Test-Path "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App\Win11TaskbarMonitor.exe") {
            $sourceExe = "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App\Win11TaskbarMonitor.exe"
            $sourceAppDir = "$PSScriptRoot\..\dist\Win11TaskbarMonitor-App"
        }
    }
}

$sourceDaemon = if (Test-Path "$PSScriptRoot\LiveDaemon.cs") { "$PSScriptRoot\LiveDaemon.cs" } else { "$PSScriptRoot\..\scripts\LiveDaemon.cs" }
$sourceRunner = if (Test-Path "$PSScriptRoot\Run-WinTaskbarMonitor.ps1") { "$PSScriptRoot\Run-WinTaskbarMonitor.ps1" } else { "$PSScriptRoot\..\scripts\Run-WinTaskbarMonitor.ps1" }
$sourceVbs = if (Test-Path "$PSScriptRoot\Launch-Silent.vbs") { "$PSScriptRoot\Launch-Silent.vbs" } else { "$PSScriptRoot\..\scripts\Launch-Silent.vbs" }
$sourceUninstall = if (Test-Path "$PSScriptRoot\uninstall.ps1") { "$PSScriptRoot\uninstall.ps1" } else { "$PSScriptRoot\..\scripts\uninstall.ps1" }
$sourceIco = if (Test-Path "$PSScriptRoot\app.ico") { "$PSScriptRoot\app.ico" } elseif (Test-Path "$PSScriptRoot\..\assets\app.ico") { "$PSScriptRoot\..\assets\app.ico" } else { $null }

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

if ($sourceAppDir -and (Test-Path $sourceAppDir)) {
    Copy-Item -Path "$sourceAppDir\*" -Destination $installDir -Recurse -Force
    Get-ChildItem -Path $installDir -Filter "*.exe" | ForEach-Object { Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue }
} elseif ($sourceExe -and (Test-Path $sourceExe)) {
    Copy-Item -Path $sourceExe -Destination $targetExe -Force
    Unblock-File -Path $targetExe -ErrorAction SilentlyContinue
}
if (Test-Path $sourceUninstall) {
    Copy-Item -Path $sourceUninstall -Destination $targetUninstallScript -Force
}
if (Test-Path $sourceRunner) {
    Copy-Item -Path $sourceRunner -Destination "$installDir\Run-WinTaskbarMonitor.ps1" -Force
}
if (Test-Path $sourceDaemon) {
    Copy-Item -Path $sourceDaemon -Destination "$installDir\LiveDaemon.cs" -Force
}
if (Test-Path $sourceVbs) {
    Copy-Item -Path $sourceVbs -Destination "$installDir\Launch-Silent.vbs" -Force
}
$targetIco = "$installDir\app.ico"
if ($sourceIco -and (Test-Path $sourceIco)) {
    Copy-Item -Path $sourceIco -Destination $targetIco -Force
}
Write-Host "[+] Copied native application files and dependencies" -ForegroundColor Green

# Remove legacy WinTaskbarMonitor shortcuts if present
Remove-Item "$startMenuDir\WinTaskbarMonitor.lnk" -ErrorAction SilentlyContinue
Remove-Item "$desktopPath\WinTaskbarMonitor.lnk" -ErrorAction SilentlyContinue

# 4. Create Start Menu Shortcut (Native Standalone Executable)
$shortcutPath = "$startMenuDir\Win11TaskbarMonitor.lnk"
$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = "$installDir\Win11TaskbarMonitor.exe"
$shortcut.Arguments = ""
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Lightweight Windows 11 Taskbar System Monitor"
$shortcut.IconLocation = if (Test-Path $targetIco) { "$targetIco,0" } else { "$installDir\Win11TaskbarMonitor.exe,0" }
$shortcut.Save()
Write-Host "[+] Created Start Menu shortcut in $startMenuDir" -ForegroundColor Green

# 5. Optional Desktop Shortcut
if ($CreateDesktopShortcut) {
    $desktopShortcut = "$desktopPath\Win11TaskbarMonitor.lnk"
    $scDesktop = $wshShell.CreateShortcut($desktopShortcut)
    $scDesktop.TargetPath = "$installDir\Win11TaskbarMonitor.exe"
    $scDesktop.Arguments = ""
    $scDesktop.WorkingDirectory = $installDir
    $scDesktop.Description = "Lightweight Windows 11 Taskbar System Monitor"
    $scDesktop.IconLocation = if (Test-Path $targetIco) { "$targetIco,0" } else { "$installDir\Win11TaskbarMonitor.exe,0" }
    $scDesktop.Save()
    Write-Host "[+] Created Desktop shortcut" -ForegroundColor Green
}

# 6. Register in Windows 'Installed Apps' / Add or Remove Programs
if (!(Test-Path $uninstallKey)) {
    New-Item -Path $uninstallKey -Force | Out-Null
}

$exeSizeKb = [Math]::Round((Get-Item $targetExe).Length / 1KB)

Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "Win11TaskbarMonitor"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.2.0"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "slayershark"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $installDir
$displayIcon = if (Test-Path $targetIco) { "$targetIco,0" } else { "$targetExe,0" }
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value $displayIcon
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$targetUninstallScript`""
Set-ItemProperty -Path $uninstallKey -Name "EstimatedSize" -Value $exeSizeKb
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -Type DWord
Write-Host "[+] Registered in Windows Installed Apps" -ForegroundColor Green

# 7. Optional Start with Windows (Native Direct Executable, Zero PowerShell/CMD dependency)
if ($StartWithWindows) {
    $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $runVal = "`"$installDir\Win11TaskbarMonitor.exe`""
    Set-ItemProperty -Path $runKey -Name "Win11TaskbarMonitor" -Value $runVal
    Remove-ItemProperty -Path $runKey -Name "WinTaskbarMonitor" -ErrorAction SilentlyContinue
    Write-Host "[+] Enabled Start with Windows (Native Executable, No CMD/PowerShell Dependency)" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "          Installation Completed Successfully!            " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Win11TaskbarMonitor is now installed as a native Windows executable." -ForegroundColor White
Write-Host "Runs independently with zero PowerShell or terminal window dependency." -ForegroundColor White

if ($LaunchAfterInstall) {
    Write-Host "`n[*] Launching Win11TaskbarMonitor native executable..." -ForegroundColor Yellow
    $res = ([wmiclass]"Win32_Process").Create("$installDir\Win11TaskbarMonitor.exe")
    if ($res.ReturnValue -ne 0) {
        Start-Process "$installDir\Win11TaskbarMonitor.exe"
    }
}
