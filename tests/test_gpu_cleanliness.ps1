$files = @(
    'src/Installer/MainWindow.xaml',
    'src/Installer/MainWindow.xaml.cs',
    'src/NativeMonitor/Program.cs',
    'scripts/LiveDaemon.cs'
)

$hasErrors = $false
foreach ($f in $files) {
    $full = Join-Path $PSScriptRoot "..\$f"
    $matches = Get-Content $full | Select-String -Pattern 'MX350|Iris'
    if ($matches) {
        Write-Host "[FAIL] Found hardcoded GPU strings in $f" -ForegroundColor Red
        $matches | ForEach-Object { Write-Host "  Line $($_.LineNumber): $($_.Line.Trim())" }
        $hasErrors = $true
    } else {
        Write-Host "[PASS] No hardcoded GPU strings in $f" -ForegroundColor Green
    }
}

if ($hasErrors) { exit 1 } else { exit 0 }
