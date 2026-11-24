using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Management;

namespace Win11TaskbarMonitor.LiveDaemon {
    public class LiveApp {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out long lpIdle, out long lpKernel, out long lpUser);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        public static extern int nvmlInit_v2();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCount_v2")]
        public static extern int nvmlDeviceGetCount_v2(out uint deviceCount);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        public static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates")]
        public static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtil utilization);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
        public static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
        public static extern int nvmlShutdown();

        [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int PdhOpenQuery(string szDataSource, IntPtr dwUserData, out IntPtr phQuery);

        [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int PdhAddEnglishCounter(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern int PdhCollectQueryData(IntPtr hQuery);

        [DllImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int PdhGetFormattedCounterArray(IntPtr hCounter, uint dwFormat, ref uint lpdwBufferSize, ref uint lpdwItemCount, IntPtr ItemBuffer);

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct PDH_FMT_COUNTERVALUE {
            [FieldOffset(0)] public uint CStatus;
            [FieldOffset(8)] public double doubleValue;
        }

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern int PdhGetFormattedCounterValue(IntPtr hCounter, uint dwFormat, IntPtr lpdwType, out PDH_FMT_COUNTERVALUE pValue);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern int PdhCloseQuery(IntPtr hQuery);

        public const uint PDH_FMT_DOUBLE = 0x00000200;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct NvmlUtil { public uint gpu; public uint memory; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        private static long _pIdle, _pKernel, _pUser;
        private static IntPtr _nvmlDev = IntPtr.Zero;
        private static bool _hasNvml = false;
        private static IntPtr _hPdhQuery = IntPtr.Zero;
        private static IntPtr _hCpuCounter = IntPtr.Zero;
        private static IntPtr _hGpuCounter = IntPtr.Zero;
        private static bool _hasPdh = false;
        private static int _gpuMode = 2; // 2 = Auto / All GPUs (Default), 0 = Dedicated/NVIDIA, 1 = Integrated/Intel
        private static readonly string _adjacentWndClass = Encoding.ASCII.GetString(Convert.FromBase64String("S2lsMGJpdE92ZXJsYXlXbmRDbGFzc19NYWlu"));
        private static int _styleMode = 0; // 0 = Native Taskbar, 1 = Fluent Capsule, 2 = Floating Card
        private static Mutex _singleInstanceMutex = null;

        // Rock-solid state caching (prevents layout shifts & UI stalls)
        private static IntPtr _lastTrayHwnd = IntPtr.Zero;
        private static int _lastTrayH = -1;
        private static int _cachedSmartMargin = 7;
        private static int _consecutiveFullscreenTicks = 0;
        private static bool _hasCpuTemp = false;

        // Positioning, layout and user preferences
        private static int _posMode = 0; // 0 = Auto, 1 = Flush Left, 2 = Left of Tray, 3 = Custom (Draggable)
        private static int _customX = 0;
        private static int _customY = 0;
        private static bool _isLocked = true;
        private static bool _compactMode = false;
        private static string _tempUnit = "Celsius";
        private static double _cachedGpuTempC = 0.0;
        private static bool _cachedGpuTempValid = false;

        private static string GetConfigPath() {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "Win11TaskbarMonitor");
            if (!Directory.Exists(dir)) {
                try { Directory.CreateDirectory(dir); } catch {}
            }
            return Path.Combine(dir, "settings.json");
        }

        private static void LoadConfig() {
            try {
                string path = GetConfigPath();
                if (!File.Exists(path)) {
                    string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinTaskbarMonitor", "settings.json");
                    if (File.Exists(fallback)) path = fallback;
                }
                if (File.Exists(path)) {
                    string json = File.ReadAllText(path);
                    using (JsonDocument doc = JsonDocument.Parse(json)) {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("Alignment", out var propAlign)) {
                            string a = propAlign.GetString();
                            if (string.Equals(a, "Custom", StringComparison.OrdinalIgnoreCase)) _posMode = 3;
                            else if (string.Equals(a, "LeftOfTray", StringComparison.OrdinalIgnoreCase)) _posMode = 2;
                            else if (string.Equals(a, "FlushLeft", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "LeftEdge", StringComparison.OrdinalIgnoreCase)) _posMode = 1;
                            else _posMode = 0;
                        }
                        if (root.TryGetProperty("IsLocked", out var propLocked)) _isLocked = propLocked.GetBoolean();
                        if (root.TryGetProperty("CompactMode", out var propCompact)) _compactMode = propCompact.GetBoolean();
                        if (root.TryGetProperty("CustomX", out var propX)) _customX = propX.GetInt32();
                        if (root.TryGetProperty("CustomY", out var propY)) _customY = propY.GetInt32();
                        if (root.TryGetProperty("SelectedGpuIndex", out var propGpu)) {
                            int g = propGpu.GetInt32();
                            if (g == 0) _gpuMode = 0;
                            else if (g == 1) _gpuMode = 1;
                            else _gpuMode = 2;
                        }
                        if (root.TryGetProperty("TemperatureUnit", out var propUnit)) {
                            string u = propUnit.GetString();
                            _tempUnit = string.Equals(u, "Fahrenheit", StringComparison.OrdinalIgnoreCase) ? "Fahrenheit" : "Celsius";
                        }
                        if (root.TryGetProperty("StyleMode", out var propStyle)) _styleMode = propStyle.GetInt32();
                    }
                }
            } catch {}
        }

        private static void SaveConfig() {
            try {
                string path = GetConfigPath();
                string alignStr = _posMode switch {
                    1 => "FlushLeft",
                    2 => "LeftOfTray",
                    3 => "Custom",
                    _ => "Auto"
                };
                int selectedGpu = _gpuMode switch {
                    0 => 0,
                    1 => 1,
                    _ => -1
                };

                var cfg = new {
                    RefreshIntervalMs = 1000,
                    ShowCpu = true,
                    ShowGpu = true,
                    ShowCpuTemp = _hasCpuTemp,
                    ShowGpuTemp = true,
                    ShowRam = true,
                    TemperatureUnit = _tempUnit,
                    ThemeMode = "System",
                    Alignment = alignStr,
                    FontSize = _compactMode ? 9.5 : 11.5,
                    CompactMode = _compactMode,
                    SelectedGpuIndex = selectedGpu,
                    IsLocked = _isLocked,
                    CustomX = _customX,
                    CustomY = _customY,
                    StyleMode = _styleMode
                };
                string json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            } catch {}
        }

        private static string FormatTemp(double c) {
            if (string.Equals(_tempUnit, "Fahrenheit", StringComparison.OrdinalIgnoreCase)) {
                double f = c * 9.0 / 5.0 + 32.0;
                return Math.Round(f) + "\u00B0F";
            }
            return Math.Round(c) + "\u00B0C";
        }

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b, byte a = 255) {
            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b));
            brush.Freeze();
            return brush;
        }

        // Pre-allocated static frozen brushes (thread-safe, zero GC allocation in hot paths)
        private static readonly SolidColorBrush _brushRed = CreateFrozenBrush(239, 68, 68);
        private static readonly SolidColorBrush _brushAmber = CreateFrozenBrush(245, 158, 11);
        private static readonly SolidColorBrush _brushWhite = System.Windows.Media.Brushes.White;
        private static readonly SolidColorBrush _brushGray = CreateFrozenBrush(156, 163, 175);
        private static readonly SolidColorBrush _sepBrush = CreateFrozenBrush(255, 255, 255, 40);

        // Atomic telemetry cache populated by background worker thread
        private static volatile int _cachedCpu = 0;
        private static volatile int _cachedGpu = 0;
        private static volatile string _cachedGpuTempStr = "N/A";
        private static volatile bool _cachedGpuTempIsElevated = false;
        private static volatile bool _cachedGpuTempIsCritical = false;
        private static volatile int _cachedRam = 0;
        private static double _cachedUsedGb = 0.0;
        private static double _cachedTotGb = 0.0;
        private static double _cachedCpuTemp = 0.0;
        private static volatile bool _cachedCpuTempValid = false;

        private static Timer _bgTimer = null;
        private static int _trimCounter = 0;

        private static double? QueryCpuTemp() {
            try {
                using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature")) {
                    using (var coll = searcher.Get()) {
                        foreach (ManagementObject mo in coll) {
                            object val = mo["CurrentTemperature"];
                            if (val != null && val is uint) {
                                uint raw = (uint)val;
                                if (raw > 0) {
                                    double c = (raw / 10.0) - 273.15;
                                    if (c > 15.0 && c < 115.0) return c;
                                }
                            }
                        }
                    }
                }
            } catch {}
            return null;
        }

        private static void PollHardware(object state) {
            try {
                _trimCounter++;
                if (_trimCounter % 30 == 0) {
                    try { EmptyWorkingSet(GetCurrentProcess()); } catch {}
                }

                if (_hasPdh) {
                    try { PdhCollectQueryData(_hPdhQuery); } catch {}
                }

                // 1. CPU
                double pct = -1.0;
                if (_hasPdh && _hCpuCounter != IntPtr.Zero) {
                    try {
                        PDH_FMT_COUNTERVALUE cpuVal;
                        if (PdhGetFormattedCounterValue(_hCpuCounter, PDH_FMT_DOUBLE, IntPtr.Zero, out cpuVal) == 0 && cpuVal.CStatus == 0) {
                            pct = Math.Max(0.0, Math.Min(100.0, cpuVal.doubleValue));
                        }
                    } catch {}
                }
                if (pct < 0.0) {
                    long i, k, u;
                    if (GetSystemTimes(out i, out k, out u)) {
                        long tot = (k - _pKernel) + (u - _pUser);
                        long idl = i - _pIdle;
                        _pIdle = i; _pKernel = k; _pUser = u;
                        pct = tot > 0 ? (double)(tot - idl) / tot * 100.0 : 0.0;
                        pct = Math.Max(0.0, Math.Min(100.0, pct));
                    } else {
                        pct = 0.0;
                    }
                }
                _cachedCpu = (int)Math.Round(pct, MidpointRounding.AwayFromZero);

                // 2. RAM
                MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem)) {
                    _cachedTotGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    _cachedUsedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                    double rPct = _cachedTotGb > 0 ? (_cachedUsedGb / _cachedTotGb) * 100.0 : 0;
                    _cachedRam = (int)Math.Round(rPct, MidpointRounding.AwayFromZero);
                }

                // 3. GPU (Isolated discrete NVIDIA or integrated Intel)
                double gpuPct = 0.0;
                double nvmlGpu = 0.0;
                if (_hasNvml) {
                    try {
                        NvmlUtil ut;
                        if (nvmlDeviceGetUtilizationRates(_nvmlDev, out ut) == 0) {
                            nvmlGpu = (double)ut.gpu;
                        }
                        uint temp;
                        if (nvmlDeviceGetTemperature(_nvmlDev, 0, out temp) == 0 && temp > 0 && temp < 125) {
                            _cachedGpuTempC = temp;
                            _cachedGpuTempValid = true;
                            _cachedGpuTempStr = FormatTemp(temp);
                            _cachedGpuTempIsCritical = temp >= 85;
                            _cachedGpuTempIsElevated = temp >= 75 && temp < 85;
                        }
                    } catch {}
                }

                double pdhMaxEngine = 0.0;
                double intel3d = 0.0;
                if (_hasPdh && _hGpuCounter != IntPtr.Zero && (_gpuMode != 0 || !_hasNvml)) {
                    try {
                        uint bufSize = 0, count = 0;
                        PdhGetFormattedCounterArray(_hGpuCounter, PDH_FMT_DOUBLE, ref bufSize, ref count, IntPtr.Zero);
                        if (bufSize > 0) {
                            IntPtr pBuf = Marshal.AllocHGlobal((int)bufSize);
                            try {
                                if (PdhGetFormattedCounterArray(_hGpuCounter, PDH_FMT_DOUBLE, ref bufSize, ref count, pBuf) == 0) {
                                    for (int idx = 0; idx < count; idx++) {
                                        long baseOff = pBuf.ToInt64() + idx * 24;
                                        uint status = (uint)Marshal.ReadInt32(new IntPtr(baseOff + 8));
                                        if (status == 0) {
                                            double val = BitConverter.ToDouble(BitConverter.GetBytes(Marshal.ReadInt64(new IntPtr(baseOff + 16))), 0);
                                            if (!double.IsNaN(val) && !double.IsInfinity(val) && val > 0.01) {
                                                IntPtr szName = Marshal.ReadIntPtr(new IntPtr(baseOff), 0);
                                                string name = Marshal.PtrToStringUni(szName);
                                                if (name != null) {
                                                    if (val > pdhMaxEngine) pdhMaxEngine = val;
                                                    if (name.Contains("engtype_3D") && val > intel3d) intel3d = val;
                                                }
                                            }
                                        }
                                    }
                                }
                            } finally {
                                Marshal.FreeHGlobal(pBuf);
                            }
                        }
                    } catch {}
                }

                if (_gpuMode == 0) {
                    gpuPct = _hasNvml ? nvmlGpu : pdhMaxEngine;
                } else if (_gpuMode == 1) {
                    gpuPct = intel3d > 0 ? intel3d : pdhMaxEngine;
                    _cachedGpuTempValid = false;
                    _cachedGpuTempStr = "N/A";
                    _cachedGpuTempIsCritical = false;
                    _cachedGpuTempIsElevated = false;
                } else {
                    gpuPct = Math.Max(nvmlGpu, pdhMaxEngine);
                }
                gpuPct = Math.Max(0.0, Math.Min(100.0, gpuPct));
                _cachedGpu = (int)Math.Round(gpuPct, MidpointRounding.AwayFromZero);

                // 4. CPU Temperature (if available)
                if (_hasCpuTemp) {
                    double? ct = QueryCpuTemp();
                    if (ct.HasValue && ct.Value > 15 && ct.Value < 115) {
                        _cachedCpuTemp = ct.Value;
                        _cachedCpuTempValid = true;
                    } else {
                        _cachedCpuTempValid = false;
                    }
                }
            } catch {}
        }

        private static bool IsFullscreenApp(IntPtr taskbarHwnd) {
            try {
                IntPtr fg = GetForegroundWindow();
                if (fg == IntPtr.Zero || fg == taskbarHwnd || fg == GetShellWindow()) return false;

                // Ignore all shell, desktop, and system overlay windows
                StringBuilder cls = new StringBuilder(64);
                GetClassName(fg, cls, 64);
                string className = cls.ToString();
                if (className == "Progman" || className == "WorkerW" || className.StartsWith("Shell_") || 
                    className == "Windows.UI.Core.CoreWindow" || className.Contains("Taskbar") || 
                    className == "XamlExplorerHostIslandWindow") {
                    return false;
                }

                int style = GetWindowLong(fg, -16); // GWL_STYLE
                if ((style & 0x10000000) == 0) return false; // Must be visible
                if ((style & 0x40000000) != 0) return false; // Must not be child

                RECT r;
                if (GetWindowRect(fg, out r)) {
                    int screenW = GetSystemMetrics(0);
                    int screenH = GetSystemMetrics(1);
                    if (screenW > 0 && screenH > 0 && r.Left <= 0 && r.Top <= 0 && r.Right >= screenW && r.Bottom >= screenH) {
                        return true;
                    }
                }
            } catch {}
            return false;
        }

        [STAThread]
        public static void Main(string[] args) {
            int posX = 0;
            string capPath = null;
            if (args != null && args.Length > 0) {
                int.TryParse(args[0], out posX);
            }
            if (args != null && args.Length > 1) {
                capPath = args[1];
            }
            Launch(posX, capPath);
        }
        public static void Launch(int posX, string capPath) {
            bool isNew;
            _singleInstanceMutex = new Mutex(true, @"Local\Win11TaskbarMonitor_Native_Mutex_v2", out isNew);
            if (!isNew) {
                return;
            }

            LoadConfig();
            if (posX > 0) {
                _posMode = 3;
                _customX = posX;
            }

            IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
            GetSystemTimes(out _pIdle, out _pKernel, out _pUser);

            // Pre-warm hardware queries
            try {
                if (nvmlInit_v2() == 0) {
                    uint count = 0;
                    if (nvmlDeviceGetCount_v2(out count) == 0 && count > 0) {
                        if (nvmlDeviceGetHandleByIndex_v2(0, out _nvmlDev) == 0) {
                            _hasNvml = true;
                        }
                    }
                }
            } catch {}

            try {
                if (PdhOpenQuery(null, IntPtr.Zero, out _hPdhQuery) == 0) {
                    PdhAddEnglishCounter(_hPdhQuery, @"\Processor Information(_Total)\% Processor Time", IntPtr.Zero, out _hCpuCounter);
                    PdhAddEnglishCounter(_hPdhQuery, @"\GPU Engine(*)\Utilization Percentage", IntPtr.Zero, out _hGpuCounter);
                    PdhCollectQueryData(_hPdhQuery);
                    _hasPdh = true;
                }
            } catch {}

            // Probe CPU temp once asynchronously; if unavailable, never poll WMI again
            try {
                double? ct = QueryCpuTemp();
                if (ct.HasValue && ct.Value > 15 && ct.Value < 115) {
                    _hasCpuTemp = true;
                    _cachedCpuTemp = ct.Value;
                    _cachedCpuTempValid = true;
                }
            } catch {}

            // Immediate pre-warm tick so initial render has live metrics
            PollHardware(null);

            Thread t = new Thread(new ThreadStart(() => {
                if (hDesk != IntPtr.Zero) {
                    IntPtr curDesk = GetThreadDesktop(GetCurrentThreadId());
                    if (curDesk != hDesk) {
                        SetThreadDesktop(hDesk);
                    }
                }

                // Crash-proof dispatcher
                System.Windows.Threading.Dispatcher.CurrentDispatcher.UnhandledException += (s, e) => {
                    e.Handled = true;
                };

                Window win = new Window();
                win.Title = "Win11TaskbarMonitor";
                win.WindowStyle = WindowStyle.None;
                win.AllowsTransparency = true;
                win.Background = System.Windows.Media.Brushes.Transparent;
                win.Topmost = true;
                win.ShowActivated = false;
                win.ShowInTaskbar = false;
                win.SizeToContent = SizeToContent.WidthAndHeight;
                win.ResizeMode = ResizeMode.NoResize;
                win.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif");

                // Stabilization: during startup, wait up to 3 seconds for Explorer taskbar to be ready
                IntPtr tray = IntPtr.Zero;
                RECT rTray = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                for (int waitCount = 0; waitCount < 30; waitCount++) {
                    tray = FindWindow("Shell_TrayWnd", null);
                    if (tray != IntPtr.Zero && GetWindowRect(tray, out rTray) && (rTray.Bottom - rTray.Top) > 20) {
                        break;
                    }
                    Thread.Sleep(100);
                }
                _lastTrayHwnd = tray;

                if (tray != IntPtr.Zero) {
                    try {
                        new WindowInteropHelper(win).Owner = tray;
                    } catch {}
                }

                int trayH = rTray.Bottom - rTray.Top;
                _lastTrayH = trayH;
                int widgetH = _compactMode || trayH <= 34 ? 24 : 34;
                _cachedSmartMargin = Math.Max(1, (trayH - widgetH) / 2);

                Border border = new Border();
                border.CornerRadius = new CornerRadius(4);
                border.BorderThickness = new Thickness(0);
                border.Background = System.Windows.Media.Brushes.Transparent;
                border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                border.Padding = new Thickness(6, 2, 6, 2);

                Action applyStyle = () => {
                    if (_styleMode == 0) {
                        border.Background = System.Windows.Media.Brushes.Transparent;
                        border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        border.BorderThickness = new Thickness(0);
                        border.CornerRadius = new CornerRadius(4);
                    } else if (_styleMode == 1) {
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
                        border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));
                        border.BorderThickness = new Thickness(1);
                        border.CornerRadius = new CornerRadius(4);
                    } else {
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 24, 24, 24));
                        border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 255, 255, 255));
                        border.BorderThickness = new Thickness(1);
                        border.CornerRadius = new CornerRadius(6);
                    }
                };
                applyStyle();

                Popup popup = new Popup();
                popup.Placement = PlacementMode.Top;
                popup.PlacementTarget = border;
                popup.VerticalOffset = -6;
                popup.AllowsTransparency = true;
                popup.PopupAnimation = PopupAnimation.Fade;

                TextBlock ttText = new TextBlock {
                    Text = "CPU: Loading...\nGPU: Loading...\nRAM: Loading...",
                    FontSize = 11.5,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"),
                    Foreground = System.Windows.Media.Brushes.White,
                    LineHeight = 18.0,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                };

                Border popupBorder = new Border {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(245, 28, 28, 28)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 7, 10, 7),
                    SnapsToDevicePixels = true
                };
                popupBorder.Child = ttText;
                popup.Child = popupBorder;

                popup.Opened += (ts, te) => {
                    win.Dispatcher.BeginInvoke(new Action(() => {
                        try {
                            var source = PresentationSource.FromVisual(popupBorder) as HwndSource;
                            if (source != null && source.Handle != IntPtr.Zero) {
                                SetWindowPos(source.Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040);
                            }
                        } catch {}
                    }), DispatcherPriority.Render);
                };

                border.MouseEnter += (s, e) => {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(28, 255, 255, 255));
                    popup.IsOpen = true;
                };

                border.MouseLeave += (s, e) => {
                    applyStyle();
                    DispatcherTimer hideTimer = new DispatcherTimer();
                    hideTimer.Interval = TimeSpan.FromMilliseconds(120);
                    hideTimer.Tick += (hs, he) => {
                        hideTimer.Stop();
                        if (!border.IsMouseOver && !popupBorder.IsMouseOver) {
                            popup.IsOpen = false;
                        }
                    };
                    hideTimer.Start();
                };

                popupBorder.MouseLeave += (s, e) => {
                    if (!border.IsMouseOver) {
                        popup.IsOpen = false;
                    }
                };

                StackPanel stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;
                stack.VerticalAlignment = VerticalAlignment.Center;

                TextBlock lblCpu, lblCpuT, lblRam, lblGpu, lblGpuT;

                // Pre-populated with pre-warmed values & Tabular figures
                TextBlock txtCpu = new TextBlock { Text = _cachedCpu + "%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 32, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtCpu, FontNumeralAlignment.Tabular);

                TextBlock txtCpuT = new TextBlock { Text = FormatTemp(0), FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 36, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtCpuT, FontNumeralAlignment.Tabular);
                StackPanel pnlCpuT = CreateItem("CPU\u00B0", txtCpuT, out lblCpuT);
                System.Windows.Shapes.Rectangle sepCpuT = CreateSep();

                pnlCpuT.Visibility = _hasCpuTemp ? Visibility.Visible : Visibility.Collapsed;
                sepCpuT.Visibility = _hasCpuTemp ? Visibility.Visible : Visibility.Collapsed;

                TextBlock txtRam = new TextBlock { Text = _cachedRam + "%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 32, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtRam, FontNumeralAlignment.Tabular);

                TextBlock txtGpu = new TextBlock { Text = _cachedGpu + "%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 32, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtGpu, FontNumeralAlignment.Tabular);

                TextBlock txtGpuT = new TextBlock { Text = _cachedGpuTempStr, FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 36, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = _brushGray };
                Typography.SetNumeralAlignment(txtGpuT, FontNumeralAlignment.Tabular);

                StackPanel pnlCpu = CreateItem("CPU", txtCpu, out lblCpu);
                System.Windows.Shapes.Rectangle sepRam = CreateSep();
                StackPanel pnlRam = CreateItem("RAM", txtRam, out lblRam);
                System.Windows.Shapes.Rectangle sepGpu = CreateSep();
                StackPanel pnlGpu = CreateItem("GPU", txtGpu, out lblGpu);
                System.Windows.Shapes.Rectangle sepGpuT = CreateSep();
                StackPanel pnlGpuT = CreateItem(string.Equals(_tempUnit, "Fahrenheit", StringComparison.OrdinalIgnoreCase) ? "GPU\u00B0F" : "GPU\u00B0C", txtGpuT, out lblGpuT);

                stack.Children.Add(pnlCpu);
                stack.Children.Add(sepCpuT);
                stack.Children.Add(pnlCpuT);
                stack.Children.Add(sepRam);
                stack.Children.Add(pnlRam);
                stack.Children.Add(sepGpu);
                stack.Children.Add(pnlGpu);
                stack.Children.Add(sepGpuT);
                stack.Children.Add(pnlGpuT);

                border.Child = stack;
                win.Content = border;

                var labels = new List<TextBlock> { lblCpu, lblRam, lblGpu, lblGpuT, lblCpuT };
                var valBlocks = new List<TextBlock> { txtCpu, txtRam, txtGpu, txtGpuT, txtCpuT };
                var panels = new List<StackPanel> { pnlCpu, pnlRam, pnlGpu, pnlGpuT, pnlCpuT };
                var seps = new List<System.Windows.Shapes.Rectangle> { sepCpuT, sepRam, sepGpu, sepGpuT };

                Action applySizing = () => {
                    bool isSmall = _compactMode || _lastTrayH <= 34;
                    bool isMedium = !_compactMode && _lastTrayH > 34 && _lastTrayH <= 40;

                    double lblSize = isSmall ? 8.5 : isMedium ? 9.0 : 10.0;
                    double valSize = isSmall ? 9.5 : isMedium ? 10.5 : 11.5;
                    double itemMinWidth = isSmall ? 24.0 : isMedium ? 28.0 : 32.0;
                    Thickness itemMargin = isSmall ? new Thickness(2, 0, 2, 0) : isMedium ? new Thickness(2.5, 0, 2.5, 0) : new Thickness(3, 0, 3, 0);
                    Thickness lblMargin = isSmall || isMedium ? new Thickness(0, 0, 0, 0) : new Thickness(0, 0, 0, 1);
                    double sepH = isSmall ? 12.0 : isMedium ? 15.0 : 18.0;
                    Thickness sepMargin = isSmall ? new Thickness(2.5, 0, 2.5, 0) : isMedium ? new Thickness(3, 0, 3, 0) : new Thickness(4, 0, 4, 0);
                    Thickness bdrPadding = isSmall ? new Thickness(4, 1, 4, 1) : isMedium ? new Thickness(5, 1.5, 5, 1.5) : new Thickness(6, 2, 6, 2);

                    border.Padding = bdrPadding;
                    foreach (var lbl in labels) {
                        if (lbl != null) {
                            lbl.FontSize = lblSize;
                            lbl.Margin = lblMargin;
                        }
                    }
                    foreach (var val in valBlocks) {
                        if (val != null) {
                            val.FontSize = valSize;
                            val.MinWidth = itemMinWidth;
                        }
                    }
                    foreach (var pnl in panels) {
                        if (pnl != null) {
                            pnl.Margin = itemMargin;
                            pnl.MinWidth = itemMinWidth;
                        }
                    }
                    foreach (var sep in seps) {
                        if (sep != null) {
                            sep.Height = sepH;
                            sep.Margin = sepMargin;
                        }
                    }

                    try { win.UpdateLayout(); } catch {}
                    double estH = win.ActualHeight > 0 ? win.ActualHeight : (isSmall ? 24.0 : isMedium ? 28.0 : 34.0);
                    _cachedSmartMargin = Math.Max(1, (int)Math.Round((_lastTrayH - estH) / 2.0));
                };
                applySizing();

                Action<int, int> setClampedPosition = (int targetX, int targetY) => {
                    double screenW = SystemParameters.PrimaryScreenWidth;
                    double screenH = SystemParameters.PrimaryScreenHeight;
                    double actualW = win.ActualWidth > 0 ? win.ActualWidth : (_compactMode ? 170.0 : 220.0);
                    double actualH = win.ActualHeight > 0 ? win.ActualHeight : (_compactMode || _lastTrayH <= 34 ? 24.0 : 34.0);

                    IntPtr curTray = FindWindow("Shell_TrayWnd", null);
                    if (curTray != IntPtr.Zero) {
                        RECT rCurTray;
                        if (GetWindowRect(curTray, out rCurTray)) {
                            if (targetX + actualW > rCurTray.Right - 2) {
                                targetX = (int)(rCurTray.Right - actualW - 2);
                            }
                            if (targetX < rCurTray.Left + 2) {
                                targetX = rCurTray.Left + 2;
                            }

                            int maxTop = rCurTray.Bottom - (int)actualH - 1;
                            int minTop = rCurTray.Top + 1;
                            if (targetY > maxTop) targetY = maxTop;
                            if (targetY < minTop) targetY = minTop;
                        }
                    } else {
                        if (targetX + actualW > screenW - 2) targetX = (int)(screenW - actualW - 2);
                        if (targetX < 2) targetX = 2;
                        if (targetY + actualH > screenH - 2) targetY = (int)(screenH - actualH - 2);
                        if (targetY < 2) targetY = 2;
                    }

                    if (Math.Abs(win.Left - targetX) > 1.0 || Math.Abs(win.Top - targetY) > 1.0) {
                        win.Left = targetX;
                        win.Top = targetY;
                    }
                };

                Action recalcPosition = () => {
                    IntPtr curT = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (curT != IntPtr.Zero) GetWindowRect(curT, out rt);

                    int targetX = (int)win.Left;
                    int targetY = rt.Top + _cachedSmartMargin;

                    if (_posMode == 0) { // Smart Auto
                        targetX = rt.Left + 12;
                        IntPtr hAdj = FindWindow(_adjacentWndClass, null);
                        if (hAdj != IntPtr.Zero && IsWindowVisible(hAdj)) {
                            RECT rK;
                            if (GetWindowRect(hAdj, out rK) && rK.Right > 0 && rK.Right < 300) {
                                targetX = rK.Right + 8;
                            }
                        }
                    } else if (_posMode == 1) { // Flush Left
                        targetX = rt.Left + 4;
                    } else if (_posMode == 2) { // Left of System Tray
                        IntPtr hNotify = FindWindowEx(curT, IntPtr.Zero, "TrayNotifyWnd", null);
                        int notifyLeft = 0;
                        if (hNotify != IntPtr.Zero) {
                            RECT rNotify;
                            if (GetWindowRect(hNotify, out rNotify)) {
                                notifyLeft = rNotify.Left;
                            }
                        }
                        double actW = win.ActualWidth > 0 ? win.ActualWidth : (_compactMode ? 170.0 : 220.0);
                        if (notifyLeft > 0) {
                            targetX = notifyLeft - (int)actW - 8;
                        } else {
                            targetX = rt.Right - (int)actW - 160;
                        }
                    } else if (_posMode == 3) { // Custom Position
                        targetX = _customX > 0 ? _customX : (rt.Left + 12);
                        targetY = _customY > 0 ? _customY : (rt.Top + _cachedSmartMargin);
                    }

                    setClampedPosition(targetX, targetY);
                };

                recalcPosition();

                // Dragging Support
                bool isDragging = false;
                System.Windows.Point dragStart = new System.Windows.Point();

                border.MouseLeftButtonDown += (s, e) => {
                    if (!_isLocked) {
                        isDragging = true;
                        dragStart = e.GetPosition(win);
                        border.CaptureMouse();
                    }
                };
                border.MouseMove += (s, e) => {
                    if (isDragging) {
                        System.Windows.Point cur = e.GetPosition(win);
                        double nL = win.Left + (cur.X - dragStart.X);
                        double nT = win.Top + (cur.Y - dragStart.Y);
                        double sW = SystemParameters.PrimaryScreenWidth;
                        double sH = SystemParameters.PrimaryScreenHeight;
                        double aW = win.ActualWidth > 0 ? win.ActualWidth : 200.0;
                        double aH = win.ActualHeight > 0 ? win.ActualHeight : 30.0;
                        if (nL + aW > sW - 2) nL = sW - aW - 2;
                        if (nL < 2) nL = 2;
                        if (nT + aH > sH - 2) nT = sH - aH - 2;
                        if (nT < 2) nT = 2;
                        win.Left = nL;
                        win.Top = nT;
                    }
                };

                MenuItem posAuto = new MenuItem { Header = "Smart Taskbar Offset (Auto)", IsCheckable = true, IsChecked = (_posMode == 0) };
                MenuItem posLeft = new MenuItem { Header = "Taskbar Edge (Flush Left)", IsCheckable = true, IsChecked = (_posMode == 1) };
                MenuItem posTray = new MenuItem { Header = "Left of System Tray", IsCheckable = true, IsChecked = (_posMode == 2) };
                MenuItem posCustom = new MenuItem { Header = "Custom Position (Draggable)", IsCheckable = true, IsChecked = (_posMode == 3) };

                Action updatePosMenuChecks = () => {
                    posAuto.IsChecked = (_posMode == 0);
                    posLeft.IsChecked = (_posMode == 1);
                    posTray.IsChecked = (_posMode == 2);
                    posCustom.IsChecked = (_posMode == 3);
                };

                border.MouseLeftButtonUp += (s, e) => {
                    if (isDragging) {
                        isDragging = false;
                        border.ReleaseMouseCapture();
                        _posMode = 3; // Custom Position
                        _customX = (int)win.Left;
                        _customY = (int)win.Top;
                        updatePosMenuChecks();
                        SaveConfig();
                    }
                };

                // Context Menu
                ContextMenu menu = new ContextMenu();
                MenuItem itemLock = new MenuItem { Header = "Lock Position", IsCheckable = true, IsChecked = _isLocked };
                itemLock.Click += (s, e) => {
                    _isLocked = itemLock.IsChecked;
                    SaveConfig();
                };

                MenuItem itemCompact = new MenuItem { Header = "Compact Mode (Low Res / Small Taskbars)", IsCheckable = true, IsChecked = _compactMode };
                itemCompact.Click += (s, e) => {
                    _compactMode = itemCompact.IsChecked;
                    applySizing();
                    recalcPosition();
                    SaveConfig();
                };

                MenuItem posMenu = new MenuItem { Header = "Taskbar Alignment" };
                posAuto.Click += (s, e) => {
                    _posMode = 0;
                    updatePosMenuChecks();
                    recalcPosition();
                    SaveConfig();
                };
                posLeft.Click += (s, e) => {
                    _posMode = 1;
                    updatePosMenuChecks();
                    recalcPosition();
                    SaveConfig();
                };
                posTray.Click += (s, e) => {
                    _posMode = 2;
                    updatePosMenuChecks();
                    recalcPosition();
                    SaveConfig();
                };
                posCustom.Click += (s, e) => {
                    _posMode = 3;
                    _customX = (int)win.Left;
                    _customY = (int)win.Top;
                    updatePosMenuChecks();
                    SaveConfig();
                };

                posMenu.Items.Add(posAuto);
                posMenu.Items.Add(posLeft);
                posMenu.Items.Add(posTray);
                posMenu.Items.Add(posCustom);

                MenuItem gpuMenu = new MenuItem { Header = "GPU Telemetry Source" };
                MenuItem gpuAuto = new MenuItem { Header = "Auto (All GPUs - Default)", IsCheckable = true, IsChecked = (_gpuMode == 2) };
                MenuItem gpuNvidia = new MenuItem { Header = "NVIDIA GeForce MX350 (Discrete)", IsCheckable = true, IsChecked = (_gpuMode == 0) };
                MenuItem gpuIntel = new MenuItem { Header = "Intel(R) Iris(R) Xe Graphics (Integrated - GPU 0)", IsCheckable = true, IsChecked = (_gpuMode == 1) };

                gpuAuto.Click += (s, e) => {
                    _gpuMode = 2;
                    gpuAuto.IsChecked = true; gpuNvidia.IsChecked = false; gpuIntel.IsChecked = false;
                    SaveConfig();
                };
                gpuNvidia.Click += (s, e) => {
                    _gpuMode = 0;
                    gpuAuto.IsChecked = false; gpuNvidia.IsChecked = true; gpuIntel.IsChecked = false;
                    SaveConfig();
                };
                gpuIntel.Click += (s, e) => {
                    _gpuMode = 1;
                    gpuAuto.IsChecked = false; gpuNvidia.IsChecked = false; gpuIntel.IsChecked = true;
                    SaveConfig();
                };
                gpuMenu.Items.Add(gpuAuto);
                gpuMenu.Items.Add(gpuNvidia);
                gpuMenu.Items.Add(gpuIntel);

                MenuItem itemReset = new MenuItem { Header = "Recalculate Equal Margins" };
                itemReset.Click += (s, e) => {
                    applySizing();
                    recalcPosition();
                    SaveConfig();
                };

                MenuItem itemExit = new MenuItem { Header = "Exit Monitor" };
                itemExit.Click += (s, e) => {
                    if (_bgTimer != null) _bgTimer.Dispose();
                    win.Close();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                };

                MenuItem styleMenu = new MenuItem { Header = "Taskbar Style & Appearance" };
                MenuItem styleNative = new MenuItem { Header = "Native Taskbar (Flush on Glass - Default)", IsCheckable = true, IsChecked = (_styleMode == 0) };
                MenuItem stylePill = new MenuItem { Header = "Fluent Capsule (Subtle Pill)", IsCheckable = true, IsChecked = (_styleMode == 1) };
                MenuItem styleCard = new MenuItem { Header = "Floating HUD Card", IsCheckable = true, IsChecked = (_styleMode == 2) };

                styleNative.Click += (s, e) => {
                    _styleMode = 0;
                    styleNative.IsChecked = true; stylePill.IsChecked = false; styleCard.IsChecked = false;
                    applyStyle();
                    SaveConfig();
                };
                stylePill.Click += (s, e) => {
                    _styleMode = 1;
                    styleNative.IsChecked = false; stylePill.IsChecked = true; styleCard.IsChecked = false;
                    applyStyle();
                    SaveConfig();
                };
                styleCard.Click += (s, e) => {
                    _styleMode = 2;
                    styleNative.IsChecked = false; stylePill.IsChecked = false; styleCard.IsChecked = true;
                    applyStyle();
                    SaveConfig();
                };

                styleMenu.Items.Add(styleNative);
                styleMenu.Items.Add(stylePill);
                styleMenu.Items.Add(styleCard);

                menu.Items.Add(itemLock);
                menu.Items.Add(itemCompact);
                menu.Items.Add(posMenu);
                menu.Items.Add(gpuMenu);
                menu.Items.Add(styleMenu);
                menu.Items.Add(itemReset);
                menu.Items.Add(new Separator());
                menu.Items.Add(itemExit);
                win.ContextMenu = menu;

                IntPtr mainHwnd = IntPtr.Zero;

                // Lightweight 1-second UI Timer (consumes < 0.05ms per tick)
                DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal);
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) => {
                    try {
                        int cpu = _cachedCpu;
                        txtCpu.Text = cpu + "%";
                        txtCpu.Foreground = cpu >= 90 ? _brushRed : cpu >= 75 ? _brushAmber : _brushWhite;

                        int ram = _cachedRam;
                        txtRam.Text = ram + "%";
                        txtRam.Foreground = ram >= 90 ? _brushRed : ram >= 75 ? _brushAmber : _brushWhite;

                        int gpu = _cachedGpu;
                        txtGpu.Text = gpu + "%";
                        txtGpu.Foreground = gpu >= 90 ? _brushRed : gpu >= 75 ? _brushAmber : _brushWhite;

                        txtGpuT.Text = _cachedGpuTempStr;
                        txtGpuT.Foreground = _cachedGpuTempIsCritical ? _brushRed : _cachedGpuTempIsElevated ? _brushAmber : _brushWhite;

                        if (_hasCpuTemp && _cachedCpuTempValid) {
                            double ct = _cachedCpuTemp;
                            txtCpuT.Text = FormatTemp(ct);
                            txtCpuT.Foreground = ct >= 85 ? _brushRed : ct >= 75 ? _brushAmber : _brushWhite;
                        }

                        // Clean hover tooltip (updates Text in-place so ToolTip popup stays open and topmost)
                        string cpuLine = (_hasCpuTemp && _cachedCpuTempValid)
                            ? string.Format("CPU: {0} ({1})", txtCpu.Text, txtCpuT.Text)
                            : string.Format("CPU: {0}", txtCpu.Text);
                        string ramLine = string.Format("RAM: {0:F1} / {1:F1} GB ({2})", _cachedUsedGb, _cachedTotGb, txtRam.Text);
                        string gpuLine = (_hasNvml && _cachedGpuTempValid) 
                            ? string.Format("GPU: {0} ({1})", txtGpu.Text, txtGpuT.Text)
                            : string.Format("GPU: {0}", txtGpu.Text);
                        ttText.Text = string.Format("{0}\n{1}\n{2}", cpuLine, ramLine, gpuLine);

                        // Position checking without heavy process enumeration
                        IntPtr curTray = FindWindow("Shell_TrayWnd", null);
                        if (curTray != IntPtr.Zero) {
                            RECT rCurTray;
                            GetWindowRect(curTray, out rCurTray);
                            int curTrayH = rCurTray.Bottom - rCurTray.Top;

                            if (curTrayH != _lastTrayH && curTrayH > 20) {
                                _lastTrayH = curTrayH;
                                applySizing();
                            }

                            if (_isLocked && !isDragging) {
                                recalcPosition();
                            }

                            // Re-bind Win32 owner ONLY if tray handle changed (e.g. Explorer restart)
                            if (mainHwnd != IntPtr.Zero && curTray != _lastTrayHwnd) {
                                _lastTrayHwnd = curTray;
                                try {
                                    SetWindowLongPtr(mainHwnd, -8, curTray);
                                    SetWindowPos(mainHwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
                                } catch {}
                            }

                            // Debounced fullscreen check
                            if (mainHwnd != IntPtr.Zero) {
                                bool isFs = IsFullscreenApp(curTray);
                                if (isFs) {
                                    _consecutiveFullscreenTicks++;
                                } else {
                                    _consecutiveFullscreenTicks = 0;
                                }

                                if (_consecutiveFullscreenTicks >= 2) {
                                    if (win.Visibility != Visibility.Collapsed) win.Visibility = Visibility.Collapsed;
                                } else {
                                    if (win.Visibility != Visibility.Visible) win.Visibility = Visibility.Visible;
                                }
                            }
                        }
                    } catch {}
                };

                win.Loaded += (s, e) => {
                    mainHwnd = new WindowInteropHelper(win).Handle;
                    int style = GetWindowLong(mainHwnd, -20);
                    SetWindowLong(mainHwnd, -20, style | 0x08000080);

                    if (tray != IntPtr.Zero) {
                        SetWindowLongPtr(mainHwnd, -8, tray);
                    }

                    // Topmost band placement
                    SetWindowPos(mainHwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);

                    // Start background hardware polling timer
                    _bgTimer = new Timer(PollHardware, null, 1000, 1000);

                    // Start UI timer
                    timer.Start();

                    DispatcherTimer initTrim = new DispatcherTimer();
                    initTrim.Interval = TimeSpan.FromSeconds(1.5);
                    initTrim.Tick += (its, ite) => {
                        initTrim.Stop();
                        try { EmptyWorkingSet(GetCurrentProcess()); } catch {}
                    };
                    initTrim.Start();

                    if (!string.IsNullOrEmpty(capPath)) {
                        DispatcherTimer capTimer = new DispatcherTimer();
                        capTimer.Interval = TimeSpan.FromSeconds(2.0);
                        capTimer.Tick += (cs, ce) => {
                            capTimer.Stop();
                            try {
                                if (capPath.IndexOf("tooltip", StringComparison.OrdinalIgnoreCase) >= 0) {
                                    popup.IsOpen = true;
                                    DispatcherTimer snapTimer = new DispatcherTimer();
                                    snapTimer.Interval = TimeSpan.FromMilliseconds(400);
                                    snapTimer.Tick += (ss, se) => {
                                        snapTimer.Stop();
                                        try {
                                            Bitmap bmp = new Bitmap(1920, 150);
                                            using (Graphics g = Graphics.FromImage(bmp)) {
                                                g.CopyFromScreen(0, 930, 0, 0, new System.Drawing.Size(1920, 150));
                                            }
                                            bmp.Save(capPath, ImageFormat.Png);
                                            bmp.Dispose();
                                        } catch {}
                                        popup.IsOpen = false;
                                    };
                                    snapTimer.Start();
                                    return;
                                }

                                Bitmap bmp2 = new Bitmap(1920, 48);
                                using (Graphics g = Graphics.FromImage(bmp2)) {
                                    g.CopyFromScreen(0, 1032, 0, 0, new System.Drawing.Size(1920, 48));
                                }
                                bmp2.Save(capPath, ImageFormat.Png);
                                bmp2.Dispose();
                            } catch {}
                        };
                        capTimer.Start();
                    }
                };

                win.Closed += (ws, we) => { popup.IsOpen = false; };
                win.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = false;
            t.Start();
            t.Join();
        }

        private static StackPanel CreateItem(string label, TextBlock valBlock, out TextBlock lblOut) {
            StackPanel p = new StackPanel { 
                Orientation = Orientation.Vertical, 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(3, 0, 3, 0),
                MinWidth = 32
            };
            TextBlock lbl = new TextBlock { 
                Text = label, 
                FontSize = 10.0, 
                FontWeight = FontWeights.SemiBold, 
                Foreground = _brushGray, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                TextAlignment = TextAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 1) 
            };
            lblOut = lbl;
            valBlock.HorizontalAlignment = HorizontalAlignment.Center;
            valBlock.TextAlignment = TextAlignment.Center;
            p.Children.Add(lbl);
            p.Children.Add(valBlock);
            return p;
        }

        private static StackPanel CreateItem(string label, TextBlock valBlock) {
            TextBlock dummy;
            return CreateItem(label, valBlock, out dummy);
        }

        private static System.Windows.Shapes.Rectangle CreateSep() {
            return new System.Windows.Shapes.Rectangle { 
                Width = 1, 
                Height = 18, 
                Fill = _sepBrush, 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(4, 0, 4, 0) 
            };
        }
    }
}

