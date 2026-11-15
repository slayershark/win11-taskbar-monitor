# WinTaskbarMonitor — Persistent Live Desktop Monitor
param(
    [int]$AnchorX = 0,
    [string]$CaptureProofPath = ""
)

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Xaml, System.Management

$source = Get-Content -Path "$PSScriptRoot\LiveDaemon.cs" -Raw -Encoding UTF8
Add-Type -TypeDefinition $source -ReferencedAssemblies PresentationFramework, PresentationCore, WindowsBase, System.Drawing, System.Xaml, System.Management -ErrorAction Stop

[WinTaskbarMonitor.LiveDaemon.LiveApp]::Launch($AnchorX, $CaptureProofPath)
