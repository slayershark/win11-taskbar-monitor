# WinTaskbarMonitor — Native PowerShell Runner (Bypasses Smart App Control restrictions)
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Drawing

# 1. Native API Definitions
$nativeCode = @"
using System;
using System.Runtime.InteropServices;

namespace WinTaskbarMonitor.Native
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

        public MEMORYSTATUSEX() {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlUtilization
    {
        public uint gpu;
        public uint memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    public static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out long lpIdle, out long lpKernel, out long lpUser);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

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
Add-Type -TypeDefinition $nativeCode -ErrorAction SilentlyContinue

# 2. XAML Definition
$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="WinTaskbarMonitor"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        SizeToContent="WidthAndHeight"
        ResizeMode="NoResize"
        UseLayoutRounding="True"
        TextOptions.TextFormattingMode="Display"
        TextOptions.TextRenderingMode="ClearType"
        FontFamily="Segoe UI Variable Text, Segoe UI, sans-serif">

    <Border x:Name="MainBorder"
            CornerRadius="6"
            BorderThickness="1"
            Background="#D7202020"
            BorderBrush="#46FFFFFF"
            Padding="6,3,6,3"
            SnapsToDevicePixels="True">

        <Border.ContextMenu>
            <ContextMenu x:Name="WidgetContextMenu">
                <MenuItem Header="Refresh Rate">
                    <MenuItem x:Name="MenuRate500" Header="500 ms" />
                    <MenuItem x:Name="MenuRate1000" Header="1 second" IsChecked="True" />
                    <MenuItem x:Name="MenuRate2000" Header="2 seconds" />
                    <MenuItem x:Name="MenuRate5000" Header="5 seconds" />
                </MenuItem>
                <MenuItem Header="Temperature Unit">
                    <MenuItem x:Name="MenuTempC" Header="Celsius (°C)" IsChecked="True" />
                    <MenuItem x:Name="MenuTempF" Header="Fahrenheit (°F)" />
                </MenuItem>
                <Separator />
                <MenuItem x:Name="MenuExit" Header="Exit" FontWeight="SemiBold" />
            </ContextMenu>
        </Border.ContextMenu>

        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
            <!-- CPU -->
            <StackPanel x:Name="CpuPanel" Orientation="Horizontal" VerticalAlignment="Center" Margin="3,0,3,0">
                <TextBlock Text="CPU" FontSize="9.5" FontWeight="SemiBold" Foreground="#9CA3AF" VerticalAlignment="Center" Margin="0,0,4,0" />
                <TextBlock x:Name="TxtCpu" Text="0%" FontSize="11.5" Foreground="#F5F5F5" VerticalAlignment="Center" MinWidth="28" TextAlignment="Right" />
            </StackPanel>

            <Rectangle Width="1" Height="14" Fill="#32FFFFFF" VerticalAlignment="Center" Margin="6,0,6,0" />

            <!-- GPU -->
            <StackPanel x:Name="GpuPanel" Orientation="Horizontal" VerticalAlignment="Center" Margin="3,0,3,0">
                <TextBlock Text="GPU" FontSize="9.5" FontWeight="SemiBold" Foreground="#9CA3AF" VerticalAlignment="Center" Margin="0,0,4,0" />
                <TextBlock x:Name="TxtGpu" Text="0%" FontSize="11.5" Foreground="#F5F5F5" VerticalAlignment="Center" MinWidth="28" TextAlignment="Right" />
            </StackPanel>

            <Rectangle Width="1" Height="14" Fill="#32FFFFFF" VerticalAlignment="Center" Margin="6,0,6,0" />

            <!-- CPU Temp -->
            <StackPanel x:Name="CpuTempPanel" Orientation="Horizontal" VerticalAlignment="Center" Margin="3,0,3,0">
                <TextBlock Text="CPU" FontSize="9.5" FontWeight="SemiBold" Foreground="#9CA3AF" VerticalAlignment="Center" Margin="0,0,4,0" />
                <TextBlock x:Name="TxtCpuTemp" Text="N/A" FontSize="11.5" Foreground="#9CA3AF" VerticalAlignment="Center" MinWidth="34" TextAlignment="Right" />
            </StackPanel>

            <Rectangle Width="1" Height="14" Fill="#32FFFFFF" VerticalAlignment="Center" Margin="6,0,6,0" />

            <!-- GPU Temp -->
            <StackPanel x:Name="GpuTempPanel" Orientation="Horizontal" VerticalAlignment="Center" Margin="3,0,3,0">
                <TextBlock Text="GPU" FontSize="9.5" FontWeight="SemiBold" Foreground="#9CA3AF" VerticalAlignment="Center" Margin="0,0,4,0" />
                <TextBlock x:Name="TxtGpuTemp" Text="N/A" FontSize="11.5" Foreground="#9CA3AF" VerticalAlignment="Center" MinWidth="34" TextAlignment="Right" />
            </StackPanel>

            <Rectangle Width="1" Height="14" Fill="#32FFFFFF" VerticalAlignment="Center" Margin="6,0,6,0" />

            <!-- RAM -->
            <StackPanel x:Name="RamPanel" Orientation="Horizontal" VerticalAlignment="Center" Margin="3,0,3,0">
                <TextBlock Text="RAM" FontSize="9.5" FontWeight="SemiBold" Foreground="#9CA3AF" VerticalAlignment="Center" Margin="0,0,4,0" />
                <TextBlock x:Name="TxtRam" Text="0%" FontSize="11.5" Foreground="#F5F5F5" VerticalAlignment="Center" MinWidth="32" TextAlignment="Right" />
            </StackPanel>
        </StackPanel>
    </Border>
</Window>
"@

$reader = [System.Xml.XmlReader]::Create([System.IO.StringReader]::new($xaml))
$window = [System.Windows.Markup.XamlReader]::Load($reader)

# UI Elements
$txtCpu = $window.FindName("TxtCpu")
$txtGpu = $window.FindName("TxtGpu")
$txtCpuTemp = $window.FindName("TxtCpuTemp")
$txtGpuTemp = $window.FindName("TxtGpuTemp")
$txtRam = $window.FindName("TxtRam")

$cpuPanel = $window.FindName("CpuPanel")
$gpuPanel = $window.FindName("GpuPanel")
$cpuTempPanel = $window.FindName("CpuTempPanel")
$gpuTempPanel = $window.FindName("GpuTempPanel")
$ramPanel = $window.FindName("RamPanel")

# Context Menu Items
$menuExit = $window.FindName("MenuExit")
$menuRate500 = $window.FindName("MenuRate500")
$menuRate1000 = $window.FindName("MenuRate1000")
$menuRate2000 = $window.FindName("MenuRate2000")
$menuRate5000 = $window.FindName("MenuRate5000")
$menuTempC = $window.FindName("MenuTempC")
$menuTempF = $window.FindName("MenuTempF")

$brushNormal = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromRgb(245, 245, 245))
$brushSubtle = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromRgb(156, 163, 175))
$brushWarn = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromRgb(245, 158, 11))
$brushCrit = [System.Windows.Media.SolidColorBrush]::new([System.Windows.Media.Color]::FromRgb(239, 68, 68))

$script:isFahrenheit = $false
$script:nvmlHandle = [IntPtr]::Zero
$script:nvmlAvailable = $false
$script:gpuName = "GPU"

# Initialize NVML
try {
    if ([WinTaskbarMonitor.Native.NativeMethods]::nvmlInit_v2() -eq 0) {
        $script:nvmlAvailable = $true
        [uint32]$count = 0
        if ([WinTaskbarMonitor.Native.NativeMethods]::nvmlDeviceGetCount_v2([ref]$count) -eq 0 -and $count -gt 0) {
            [IntPtr]$dev = [IntPtr]::Zero
            if ([WinTaskbarMonitor.Native.NativeMethods]::nvmlDeviceGetHandleByIndex_v2(0, [ref]$dev) -eq 0) {
                $script:nvmlHandle = $dev
                $buf = New-Object byte[] 64
                if ([WinTaskbarMonitor.Native.NativeMethods]::nvmlDeviceGetName($dev, $buf, 64) -eq 0) {
                    $script:gpuName = [System.Text.Encoding]::ASCII.GetString($buf).Trim([char]0).Trim()
                }
            }
        }
    }
} catch {}

# CPU Tracking Variables
$script:prevIdle = 0
$script:prevKernel = 0
$script:prevUser = 0
$script:cpuName = (Get-ItemProperty "HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0" -ErrorAction SilentlyContinue).ProcessorNameString
if (!$script:cpuName) { $script:cpuName = "CPU" }

# Warmup CPU
[long]$i=0; [long]$k=0; [long]$u=0
[WinTaskbarMonitor.Native.NativeMethods]::GetSystemTimes([ref]$i, [ref]$k, [ref]$u)
$script:prevIdle = $i; $script:prevKernel = $k; $script:prevUser = $u

# Reposition Window
function Update-AnchorPosition {
    $appbar = New-Object WinTaskbarMonitor.Native.APPBARDATA
    $appbar.cbSize = [System.Runtime.InteropServices.Marshal]::SizeOf($appbar)
    $res = [WinTaskbarMonitor.Native.NativeMethods]::SHAppBarMessage(5, [ref]$appbar) # ABM_GETTASKBARPOS = 5
    
    $screen = [System.Windows.SystemParameters]::WorkArea
    $w = if ($window.ActualWidth -gt 0) { $window.ActualWidth } else { 380 }
    $h = if ($window.ActualHeight -gt 0) { $window.ActualHeight } else { 36 }

    if ($res -ne 0 -and $appbar.rc.Bottom -gt 0) {
        $trayWnd = [WinTaskbarMonitor.Native.NativeMethods]::FindWindow("Shell_TrayWnd", $null)
        $trayNotifyWnd = [IntPtr]::Zero
        if ($trayWnd -ne [IntPtr]::Zero) {
            $trayNotifyWnd = [WinTaskbarMonitor.Native.NativeMethods]::FindWindowEx($trayWnd, [IntPtr]::Zero, "TrayNotifyWnd", $null)
        }

        $trayRect = New-Object WinTaskbarMonitor.Native.RECT
        if ($trayNotifyWnd -ne [IntPtr]::Zero -and [WinTaskbarMonitor.Native.NativeMethods]::GetWindowRect($trayNotifyWnd, [ref]$trayRect)) {
            $window.Left = $trayRect.Left - $w - 12
        } else {
            $window.Left = $appbar.rc.Right - 240 - $w
        }
        $tbHeight = $appbar.rc.Bottom - $appbar.rc.Top
        $window.Top = $appbar.rc.Top + [Math]::Max(2, ($tbHeight - $h) / 2)
    } else {
        $window.Left = $screen.Right - $w - 180
        $window.Top = $screen.Bottom - $h - 6
    }
}

function Clamp-Val([double]$v, [double]$min, [double]$max) {
    if ($v -lt $min) { return $min }
    if ($v -gt $max) { return $max }
    return $v
}

# Update Telemetry Ticks
function Update-Telemetry {
    # 1. CPU
    [long]$curIdle=0; [long]$curKernel=0; [long]$curUser=0
    if ([WinTaskbarMonitor.Native.NativeMethods]::GetSystemTimes([ref]$curIdle, [ref]$curKernel, [ref]$curUser)) {
        $tot = ($curKernel - $script:prevKernel) + ($curUser - $script:prevUser)
        $idl = $curIdle - $script:prevIdle
        $script:prevIdle = $curIdle; $script:prevKernel = $curKernel; $script:prevUser = $curUser
        
        $cpuPct = if ($tot -gt 0) { Clamp-Val ([Math]::Round(($tot - $idl) / $tot * 100.0, 1)) 0.0 100.0 } else { 0.0 }
        $txtCpu.Text = "$([int][Math]::Round($cpuPct))%"
        $cpuPanel.ToolTip = "$($script:cpuName)`nUsage: $cpuPct%"
        $txtCpu.Foreground = if ($cpuPct -ge 90) { $brushCrit } elseif ($cpuPct -ge 75) { $brushWarn } else { $brushNormal }
    }

    # 2. RAM
    $memBuf = New-Object WinTaskbarMonitor.Native.MEMORYSTATUSEX
    if ([WinTaskbarMonitor.Native.NativeMethods]::GlobalMemoryStatusEx($memBuf)) {
        $totGb = [Math]::Round($memBuf.ullTotalPhys / 1GB, 1)
        $usedGb = [Math]::Round(($memBuf.ullTotalPhys - $memBuf.ullAvailPhys) / 1GB, 1)
        $ramPct = if ($totGb -gt 0) { Clamp-Val ([Math]::Round(($usedGb / $totGb) * 100.0, 1)) 0.0 100.0 } else { 0.0 }
        
        $txtRam.Text = "$([int][Math]::Round($ramPct))%"
        $ramPanel.ToolTip = "RAM: $usedGb / $totGb GB ($ramPct%)"
        $txtRam.Foreground = if ($ramPct -ge 90) { $brushCrit } elseif ($ramPct -ge 75) { $brushWarn } else { $brushNormal }
    }

    # 3. GPU & Temp
    if ($script:nvmlAvailable -and $script:nvmlHandle -ne [IntPtr]::Zero) {
        $util = New-Object WinTaskbarMonitor.Native.NvmlUtilization
        if ([WinTaskbarMonitor.Native.NativeMethods]::nvmlDeviceGetUtilizationRates($script:nvmlHandle, [ref]$util) -eq 0) {
            $txtGpu.Text = "$($util.gpu)%"
            $gpuPanel.ToolTip = "$($script:gpuName)`nUsage: $($util.gpu)%"
            $txtGpu.Foreground = if ($util.gpu -ge 90) { $brushCrit } elseif ($util.gpu -ge 75) { $brushWarn } else { $brushNormal }
        }

        [uint32]$t = 0
        if ([WinTaskbarMonitor.Native.NativeMethods]::nvmlDeviceGetTemperature($script:nvmlHandle, 0, [ref]$t) -eq 0) {
            if ($t -gt 0 -and $t -lt 125) {
                $displayTemp = if ($script:isFahrenheit) { [Math]::Round($t * 1.8 + 32) } else { $t }
                $unit = if ($script:isFahrenheit) { "°F" } else { "°C" }
                $txtGpuTemp.Text = "$displayTemp$unit"
                $gpuTempPanel.ToolTip = "$($script:gpuName)`nTemperature: $displayTemp$unit"
                $txtGpuTemp.Foreground = if ($t -ge 85) { $brushCrit } elseif ($t -ge 75) { $brushWarn } else { $brushNormal }
            }
        }
    }
}

# Timer Setup
$timer = New-Object System.Windows.Threading.DispatcherTimer
$timer.Interval = [TimeSpan]::FromSeconds(1)
$timer.Add_Tick({ Update-Telemetry })

# Window Events
$window.Add_Loaded({
    $helper = New-Object System.Windows.Interop.WindowInteropHelper($window)
    $hWnd = $helper.Handle
    
    # Apply WS_EX_TOOLWINDOW (0x80) and WS_EX_NOACTIVATE (0x08000000)
    $exStyle = [WinTaskbarMonitor.Native.NativeMethods]::GetWindowLong($hWnd, -20)
    [WinTaskbarMonitor.Native.NativeMethods]::SetWindowLong($hWnd, -20, $exStyle -bor 0x08000080) | Out-Null
    
    Update-AnchorPosition
    Update-Telemetry
    $timer.Start()
})

$window.Add_MouseLeftButtonDown({
    $window.DragMove()
})

# Context Menu Event Handlers
$menuExit.Add_Click({
    $timer.Stop()
    if ($script:nvmlAvailable) {
        [WinTaskbarMonitor.Native.NativeMethods]::nvmlShutdown() | Out-Null
    }
    $window.Close()
})

$menuTempC.Add_Click({
    $script:isFahrenheit = $false
    $menuTempC.IsChecked = $true
    $menuTempF.IsChecked = $false
    Update-Telemetry
})

$menuTempF.Add_Click({
    $script:isFahrenheit = $true
    $menuTempC.IsChecked = $false
    $menuTempF.IsChecked = $true
    Update-Telemetry
})

$menuRate500.Add_Click({
    $timer.Interval = [TimeSpan]::FromMilliseconds(500)
    $menuRate500.IsChecked = $true; $menuRate1000.IsChecked = $false; $menuRate2000.IsChecked = $false; $menuRate5000.IsChecked = $false
})
$menuRate1000.Add_Click({
    $timer.Interval = [TimeSpan]::FromSeconds(1)
    $menuRate500.IsChecked = $false; $menuRate1000.IsChecked = $true; $menuRate2000.IsChecked = $false; $menuRate5000.IsChecked = $false
})
$menuRate2000.Add_Click({
    $timer.Interval = [TimeSpan]::FromSeconds(2)
    $menuRate500.IsChecked = $false; $menuRate1000.IsChecked = $false; $menuRate2000.IsChecked = $true; $menuRate5000.IsChecked = $false
})
$menuRate5000.Add_Click({
    $timer.Interval = [TimeSpan]::FromSeconds(5)
    $menuRate500.IsChecked = $false; $menuRate1000.IsChecked = $false; $menuRate2000.IsChecked = $false; $menuRate5000.IsChecked = $true
})

# Run Application
$app = New-Object System.Windows.Application
$app.Run($window) | Out-Null
