# Win11TaskbarMonitor — Persistent Live Desktop Monitor
param(
    [int]$AnchorX = 0,
    [string]$CaptureProofPath = ""
)

$ErrorActionPreference = "Stop"

try {
    Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Xaml, System.Management
    $source = Get-Content -Path "$PSScriptRoot\LiveDaemon.cs" -Raw -Encoding UTF8
    Add-Type -TypeDefinition $source -ReferencedAssemblies PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Xaml, System.Management -ErrorAction Stop

    [Win11TaskbarMonitor.LiveDaemon.LiveApp]::Launch($AnchorX, $CaptureProofPath)
} catch {
    $logPath = "$env:LOCALAPPDATA\Programs\Win11TaskbarMonitor\error.log"
    "[$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))] $($_.Exception.ToString())" | Out-File -FilePath $logPath -Append -Encoding UTF8
}
