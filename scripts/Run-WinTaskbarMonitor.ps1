# WinTaskbarMonitor — Persistent Live Desktop Monitor
param(
    [int]$AnchorX = 1220,
    [string]$CaptureProofPath = ""
)

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Xaml

$source = Get-Content -Path "$PSScriptRoot\LiveDaemon.cs" -Raw -Encoding UTF8
Add-Type -TypeDefinition $source -ReferencedAssemblies PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Xaml -ErrorAction Stop

[WinTaskbarMonitor.LiveDaemon.LiveApp]::Launch($AnchorX, $CaptureProofPath)
