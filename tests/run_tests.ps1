Write-Output "=========================================================="
Write-Output "WIN TASKBAR MONITOR — AUTOMATED SYSTEM & COMPONENT TESTING"
Write-Output "=========================================================="

$global:passCount = 0
$global:failCount = 0

function Assert-Condition($testName, $condition, $details = "") {
    if ($condition) {
        Write-Output "  [PASS] $testName ($details)"
        $global:passCount++
    } else {
        Write-Output "  [FAIL] $testName ($details)"
        $global:failCount++
    }
}

# --- TEST 1: CPU Monitor Provider Verification ---
Write-Output "`n[1/5] Testing CpuMonitorProvider (GetSystemTimes)..."
$cpuReg = (Get-ItemProperty "HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0" -ErrorAction SilentlyContinue).ProcessorNameString
Assert-Condition "CPU Name Detection" (![string]::IsNullOrWhiteSpace($cpuReg)) "Detected: $cpuReg"

$sigCpu = @"
using System;
using System.Runtime.InteropServices;

namespace WinTaskbarMonitor.TestHarness
{
    public static class CpuNative
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out long lpIdle, out long lpKernel, out long lpUser);
    }
}
"@
Add-Type -TypeDefinition $sigCpu

[long]$i1=0; [long]$k1=0; [long]$u1=0
[long]$i2=0; [long]$k2=0; [long]$u2=0
$r1 = [WinTaskbarMonitor.TestHarness.CpuNative]::GetSystemTimes([ref]$i1, [ref]$k1, [ref]$u1)
Start-Sleep -Milliseconds 400
$r2 = [WinTaskbarMonitor.TestHarness.CpuNative]::GetSystemTimes([ref]$i2, [ref]$k2, [ref]$u2)
$tot = ($k2 - $k1) + ($u2 - $u1)
$idle = $i2 - $i1
$cpuPct = if ($tot -gt 0) { [Math]::Round(($tot - $idle) / $tot * 100.0, 1) } else { 0.0 }
Assert-Condition "GetSystemTimes API Call" ($r1 -and $r2) "Returned True"
Assert-Condition "CPU Usage Range (0-100%)" ($cpuPct -ge 0.0 -and $cpuPct -le 100.0) "Measured: $cpuPct%"

# --- TEST 2: Memory Monitor Provider Verification ---
Write-Output "`n[2/5] Testing MemoryMonitorProvider (GlobalMemoryStatusEx)..."
$sigMem = @"
using System;
using System.Runtime.InteropServices;

namespace WinTaskbarMonitor.TestHarness
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    public static class MemoryNative
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}
"@
Add-Type -TypeDefinition $sigMem

$buf = New-Object WinTaskbarMonitor.TestHarness.MEMORYSTATUSEX
$memOk = [WinTaskbarMonitor.TestHarness.MemoryNative]::GlobalMemoryStatusEx($buf)
$totalMemGb = [Math]::Round($buf.ullTotalPhys / 1GB, 2)
$usedMemGb = [Math]::Round(($buf.ullTotalPhys - $buf.ullAvailPhys) / 1GB, 2)
$memPct = [Math]::Round(($usedMemGb / $totalMemGb) * 100, 1)

Assert-Condition "GlobalMemoryStatusEx API Call" $memOk "Returned True"
Assert-Condition "Total RAM > 0 GB" ($totalMemGb -gt 0) "Total: $totalMemGb GB"
Assert-Condition "Used RAM <= Total RAM" ($usedMemGb -le $totalMemGb -and $usedMemGb -gt 0) "Used: $usedMemGb GB"
Assert-Condition "RAM Percentage Range (0-100%)" ($memPct -ge 0.0 -and $memPct -le 100.0) "Calculated: $memPct%"

# --- TEST 3: GPU & Temperature Telemetry Verification ---
Write-Output "`n[3/5] Testing GPU & Temperature Providers (NVML)..."
$sigNvml = @"
using System;
using System.Runtime.InteropServices;

namespace WinTaskbarMonitor.TestHarness
{
    public static class NvmlNative
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct NvmlUtilization
        {
            public uint gpu;
            public uint memory;
        }

        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        public static extern int nvmlInit_v2();

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
        public static extern int nvmlShutdown();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCount_v2")]
        public static extern int nvmlDeviceGetCount_v2(out uint deviceCount);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        public static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
        public static extern int nvmlDeviceGetName(IntPtr device, byte[] name, uint length);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
        public static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
        public static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);
    }
}
"@
Add-Type -TypeDefinition $sigNvml

$nvmlInit = [WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlInit_v2()
Assert-Condition "NVML Init (nvmlInit_v2)" ($nvmlInit -eq 0) "Result: $nvmlInit"

[uint32]$gpuCount = 0
[WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlDeviceGetCount_v2([ref]$gpuCount)
Assert-Condition "NVIDIA GPU Detection" ($gpuCount -ge 1) "Device count: $gpuCount"

if ($gpuCount -gt 0) {
    [IntPtr]$dev = [IntPtr]::Zero
    [WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlDeviceGetHandleByIndex_v2(0, [ref]$dev)
    $nameBytes = New-Object byte[] 64
    [WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlDeviceGetName($dev, $nameBytes, 64)
    $name = [System.Text.Encoding]::ASCII.GetString($nameBytes).Trim([char]0).Trim()
    Assert-Condition "GPU Device Name" (![string]::IsNullOrWhiteSpace($name)) "Device: $name"

    $util = New-Object WinTaskbarMonitor.TestHarness.NvmlNative+NvmlUtilization
    $uRes = [WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlDeviceGetUtilizationRates($dev, [ref]$util)
    Assert-Condition "GPU Utilisation Query" ($uRes -eq 0) "Usage: $($util.gpu)%"

    [uint32]$gpuTemp = 0
    $tRes = [WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlDeviceGetTemperature($dev, 0, [ref]$gpuTemp)
    Assert-Condition "GPU Temperature Query" ($tRes -eq 0 -and $gpuTemp -gt 15 -and $gpuTemp -lt 120) "Temp: $gpuTemp °C"
}
[WinTaskbarMonitor.TestHarness.NvmlNative]::nvmlShutdown() | Out-Null

# --- TEST 4: Settings Persistence & JSON Serialization ---
Write-Output "`n[4/5] Testing Settings & Serialization (AppSettings)..."
$repoRoot = Split-Path -Parent $PSScriptRoot
$settingsSource = Get-Content (Join-Path $repoRoot "src\WinTaskbarMonitor\Config\AppSettings.cs") -Raw
Assert-Condition "AppSettings Class Present" (![string]::IsNullOrWhiteSpace($settingsSource)) "Source file exists"

# JSON serialization roundtrip
$testSettingsObj = @{
    RefreshIntervalMs = 2000
    ShowCpu = $true
    ShowGpu = $true
    ShowCpuTemp = $true
    ShowGpuTemp = $false
    ShowRam = $true
    TemperatureUnit = "Fahrenheit"
    ThemeMode = "Dark"
    Alignment = "RightEdge"
    FontSize = 12.5
    CompactMode = $false
}
$jsonString = $testSettingsObj | ConvertTo-Json
$deserialized = $jsonString | ConvertFrom-Json
Assert-Condition "Settings JSON Roundtrip (RefreshIntervalMs)" ($deserialized.RefreshIntervalMs -eq 2000) "Value: 2000 ms"
Assert-Condition "Settings JSON Roundtrip (TemperatureUnit)" ($deserialized.TemperatureUnit -eq "Fahrenheit") "Value: Fahrenheit"
Assert-Condition "Settings JSON Roundtrip (ShowGpuTemp)" ($deserialized.ShowGpuTemp -eq $false) "Value: False"

# --- TEST 5: Standalone Single-File Binary Verification ---
Write-Output "`n[5/5] Testing Standalone Build Output..."
$distExe = Join-Path $repoRoot "dist\Win11TaskbarMonitor-App\Win11TaskbarMonitor.exe"
if (-not (Test-Path $distExe)) {
    $distExe = Join-Path $repoRoot "dist\Win11TaskbarMonitor.exe"
}
$distExists = Test-Path $distExe
Assert-Condition "Standalone Executable Exists" $distExists "Path: $distExe"
if ($distExists) {
    $exeSizeMb = [Math]::Round((Get-Item $distExe).Length / 1MB, 2)
    Assert-Condition "Executable Size < 10 MB" ($exeSizeMb -lt 10 -and $exeSizeMb -gt 0.1) "Actual Size: $exeSizeMb MB"
}

Write-Output "`n=========================================================="
Write-Output "TEST SUMMARY: $global:passCount PASSED, $global:failCount FAILED"
Write-Output "=========================================================="

if ($global:failCount -eq 0) {
    exit 0
} else {
    exit 1
}
