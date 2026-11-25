# Build-All.ps1 — Universal Build & Package Script for Win11TaskbarMonitor
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       Win11TaskbarMonitor — Complete Release Build       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Stop any running instances
Write-Host "[1/5] Stopping running monitor instances..." -ForegroundColor Yellow
Stop-Process -Name "Win11TaskbarMonitor" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

# 2. Build NativeMonitor
Write-Host "`n[2/5] Compiling NativeMonitor (win-x64)..." -ForegroundColor Yellow
$nativeOut = Join-Path $repoRoot "dist\Win11TaskbarMonitor-App"
& $dotnet publish "$repoRoot\src\NativeMonitor\NativeMonitor.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $nativeOut
if ($LASTEXITCODE -ne 0) { throw "NativeMonitor compilation failed." }

Copy-Item "$nativeOut\Win11TaskbarMonitor.exe" "$repoRoot\dist\Win11TaskbarMonitor.exe" -Force
Write-Host "[+] NativeMonitor built at: $nativeOut\Win11TaskbarMonitor.exe" -ForegroundColor Green

# 3. Create Installer Payload archive
Write-Host "`n[3/5] Packing payload.zip for installer embedding..." -ForegroundColor Yellow
$payloadZip = Join-Path $repoRoot "src\Installer\payload.zip"
if (Test-Path $payloadZip) {
    Remove-Item $payloadZip -Force
}
$payloadFiles = @(Get-ChildItem -Path $nativeOut).FullName + (Join-Path $repoRoot "assets\app.ico")
Compress-Archive -Path $payloadFiles -DestinationPath $payloadZip -CompressionLevel Optimal
Write-Host "[+] Created payload.zip ($([Math]::Round((Get-Item $payloadZip).Length / 1MB, 2)) MB)" -ForegroundColor Green

# 4. Build Setup Installer
Write-Host "`n[4/5] Compiling Win11TaskbarMonitor-Setup.exe..." -ForegroundColor Yellow
$installerOut = Join-Path $repoRoot "dist\installer"
& $dotnet publish "$repoRoot\src\Installer\Installer.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $installerOut
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }

$finalSetupExe = Join-Path $repoRoot "dist\Win11TaskbarMonitor-Setup.exe"
Copy-Item "$installerOut\Win11TaskbarMonitor-Setup.exe" $finalSetupExe -Force
Write-Host "[+] Installer built at: $finalSetupExe ($([Math]::Round((Get-Item $finalSetupExe).Length / 1MB, 2)) MB)" -ForegroundColor Green

# 5. Build Release Zip Package
Write-Host "`n[5/5] Packaging standalone distribution..." -ForegroundColor Yellow
& powershell.exe -ExecutionPolicy Bypass -File "$repoRoot\scripts\Build-ReleasePackage.ps1"
if ($LASTEXITCODE -ne 0) { throw "Release package generation failed." }

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "       BUILD AND PACKAGING COMPLETE SUCCESSFULLY!         " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
