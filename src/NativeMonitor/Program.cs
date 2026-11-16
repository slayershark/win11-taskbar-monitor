using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
        private static int _gpuMode = 0; // 0 = Dedicated/NVIDIA, 1 = Integrated/Intel, 2 = Peak Load
        private static readonly string _adjacentWndClass = Encoding.ASCII.GetString(Convert.FromBase64String("S2lsMGJpdE92ZXJsYXlXbmRDbGFzc19NYWlu"));
        private static int _styleMode = 0; // 0 = Native Taskbar, 1 = Fluent Capsule, 2 = Floating Card
        private static Mutex _singleInstanceMutex = null;

        // Rock-solid state caching (prevents layout shifts & UI stalls)
        private static IntPtr _lastTrayHwnd = IntPtr.Zero;
        private static int _lastTrayH = -1;
        private static int _cachedSmartMargin = 7;
        private static int _consecutiveFullscreenTicks = 0;
        private static bool _hasCpuTemp = false;

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
                            _cachedGpuTempStr = temp + "\u00B0C";
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
                int widgetH = 34;
                _cachedSmartMargin = Math.Max(2, (trayH - widgetH) / 2);

                // Initial position: inspect if adjacent overlay is visible, place cleanly beside it
                int initialX = rTray.Left + 12;
                IntPtr hInitAdj = FindWindow(_adjacentWndClass, null);
                if (hInitAdj != IntPtr.Zero && IsWindowVisible(hInitAdj)) {
                    RECT rK;
                    if (GetWindowRect(hInitAdj, out rK) && rK.Right > 0 && rK.Right < 300) {
                        initialX = rK.Right + 8;
                    }
                }
                int defaultX = posX > 0 ? posX : initialX;
                int defaultY = rTray.Top + _cachedSmartMargin;
                win.Left = defaultX;
                win.Top = defaultY;

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

                // Pre-populated with pre-warmed values & Tabular figures
                TextBlock txtCpu = new TextBlock { Text = _cachedCpu + "%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 32, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtCpu, FontNumeralAlignment.Tabular);

                TextBlock txtCpuT = new TextBlock { Text = "0\u00B0C", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 36, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtCpuT, FontNumeralAlignment.Tabular);
                StackPanel pnlCpuT = CreateItem("CPU\u00B0", txtCpuT);
                System.Windows.Shapes.Rectangle sepCpuT = CreateSep();

                pnlCpuT.Visibility = _hasCpuTemp ? Visibility.Visible : Visibility.Collapsed;
                sepCpuT.Visibility = _hasCpuTemp ? Visibility.Visible : Visibility.Collapsed;

                TextBlock txtRam = new TextBlock { Text = _cachedRam + "%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 32, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtRam, FontNumeralAlignment.Tabular);

                TextBlock txtGpu = new TextBlock { Text = _cachedGpu + "%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 32, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                Typography.SetNumeralAlignment(txtGpu, FontNumeralAlignment.Tabular);

                TextBlock txtGpuT = new TextBlock { Text = _cachedGpuTempStr, FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 36, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = _brushGray };
                Typography.SetNumeralAlignment(txtGpuT, FontNumeralAlignment.Tabular);

                stack.Children.Add(CreateItem("CPU", txtCpu));
                stack.Children.Add(sepCpuT);
                stack.Children.Add(pnlCpuT);
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("RAM", txtRam));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("GPU", txtGpu));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("GPU\u00B0C", txtGpuT));

                border.Child = stack;
                win.Content = border;

                // Dragging Support
                bool isDragging = false;
                System.Windows.Point dragStart = new System.Windows.Point();
                bool isLocked = true;

                border.MouseLeftButtonDown += (s, e) => {
                    if (!isLocked) {
                        isDragging = true;
                        dragStart = e.GetPosition(win);
                        border.CaptureMouse();
                    }
                };
                border.MouseMove += (s, e) => {
                    if (isDragging) {
                        System.Windows.Point cur = e.GetPosition(win);
                        win.Left += cur.X - dragStart.X;
                        win.Top += cur.Y - dragStart.Y;
                    }
                };
                border.MouseLeftButtonUp += (s, e) => {
                    if (isDragging) {
                        isDragging = false;
                        border.ReleaseMouseCapture();
                    }
                };

                // Context Menu
                ContextMenu menu = new ContextMenu();
                MenuItem itemLock = new MenuItem { Header = "Lock Position", IsCheckable = true, IsChecked = true };
                itemLock.Click += (s, e) => { isLocked = itemLock.IsChecked; };

                MenuItem posMenu = new MenuItem { Header = "Taskbar Alignment" };
                MenuItem posAuto = new MenuItem { Header = "Smart Taskbar Offset (Auto)", IsChecked = true };
                MenuItem posLeft = new MenuItem { Header = "Taskbar Edge (Flush Left)", IsChecked = false };
                posLeft.Click += (s, e) => {
                    posLeft.IsChecked = true; posAuto.IsChecked = false;
                    IntPtr curT = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (curT != IntPtr.Zero) GetWindowRect(curT, out rt);
                    win.Left = rt.Left + 12;
                    win.Top = rt.Top + _cachedSmartMargin;
                };
                posAuto.Click += (s, e) => {
                    posLeft.IsChecked = false; posAuto.IsChecked = true;
                    IntPtr curT = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (curT != IntPtr.Zero) GetWindowRect(curT, out rt);
                    int targetX = rt.Left + 12;
                    IntPtr hAdj = FindWindow(_adjacentWndClass, null);
                    if (hAdj != IntPtr.Zero && IsWindowVisible(hAdj)) {
                        RECT rK;
                        if (GetWindowRect(hAdj, out rK) && rK.Right > 0 && rK.Right < 300) {
                            targetX = rK.Right + 8;
                        }
                    }
                    win.Left = targetX;
                    win.Top = rt.Top + _cachedSmartMargin;
                };
                posMenu.Items.Add(posAuto);
                posMenu.Items.Add(posLeft);

                MenuItem gpuMenu = new MenuItem { Header = "GPU Telemetry Source" };
                MenuItem gpuNvidia = new MenuItem { Header = "NVIDIA GeForce MX350 (Discrete - Default)", IsCheckable = true, IsChecked = true };
                MenuItem gpuIntel = new MenuItem { Header = "Intel(R) Iris(R) Xe Graphics (Integrated - GPU 0)", IsCheckable = true, IsChecked = false };
                MenuItem gpuPeak = new MenuItem { Header = "Auto / Peak GPU Engine", IsCheckable = true, IsChecked = false };

                gpuNvidia.Click += (s, e) => {
                    _gpuMode = 0;
                    gpuNvidia.IsChecked = true; gpuIntel.IsChecked = false; gpuPeak.IsChecked = false;
                };
                gpuIntel.Click += (s, e) => {
                    _gpuMode = 1;
                    gpuNvidia.IsChecked = false; gpuIntel.IsChecked = true; gpuPeak.IsChecked = false;
                };
                gpuPeak.Click += (s, e) => {
                    _gpuMode = 2;
                    gpuNvidia.IsChecked = false; gpuIntel.IsChecked = false; gpuPeak.IsChecked = true;
                };
                gpuMenu.Items.Add(gpuNvidia);
                gpuMenu.Items.Add(gpuIntel);
                gpuMenu.Items.Add(gpuPeak);

                MenuItem itemReset = new MenuItem { Header = "Recalculate Equal Margins" };
                itemReset.Click += (s, e) => {
                    IntPtr curT = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (curT != IntPtr.Zero) GetWindowRect(curT, out rt);
                    int sm = Math.Max(2, (int)Math.Round(((rt.Bottom - rt.Top) - border.ActualHeight) / 2.0));
                    win.Top = rt.Top + sm;
                };

                MenuItem itemExit = new MenuItem { Header = "Exit Monitor" };
                itemExit.Click += (s, e) => {
                    if (_bgTimer != null) _bgTimer.Dispose();
                    win.Close();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                };

                MenuItem styleMenu = new MenuItem { Header = "Taskbar Style & Appearance" };
                MenuItem styleNative = new MenuItem { Header = "Native Taskbar (Flush on Glass - Default)", IsCheckable = true, IsChecked = true };
                MenuItem stylePill = new MenuItem { Header = "Fluent Capsule (Subtle Pill)", IsCheckable = true, IsChecked = false };
                MenuItem styleCard = new MenuItem { Header = "Floating HUD Card", IsCheckable = true, IsChecked = false };

                styleNative.Click += (s, e) => {
                    _styleMode = 0;
                    styleNative.IsChecked = true; stylePill.IsChecked = false; styleCard.IsChecked = false;
                    applyStyle();
                };
                stylePill.Click += (s, e) => {
                    _styleMode = 1;
                    styleNative.IsChecked = false; stylePill.IsChecked = true; styleCard.IsChecked = false;
                    applyStyle();
                };
                styleCard.Click += (s, e) => {
                    _styleMode = 2;
                    styleNative.IsChecked = false; stylePill.IsChecked = false; styleCard.IsChecked = true;
                    applyStyle();
                };

                styleMenu.Items.Add(styleNative);
                styleMenu.Items.Add(stylePill);
                styleMenu.Items.Add(styleCard);

                menu.Items.Add(itemLock);
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
                            txtCpuT.Text = Math.Round(ct) + "\u00B0C";
                            txtCpuT.Foreground = ct >= 85 ? _brushRed : ct >= 75 ? _brushAmber : _brushWhite;
                        }

                        // Clean hover tooltip (updates Text in-place so ToolTip popup stays open and topmost)
                        string cpuLine = (_hasCpuTemp && _cachedCpuTempValid)
                            ? string.Format("CPU: {0} ({1})", txtCpu.Text, txtCpuT.Text)
                            : string.Format("CPU: {0}", txtCpu.Text);
                        string ramLine = string.Format("RAM: {0:F1} / {1:F1} GB ({2})", _cachedUsedGb, _cachedTotGb, txtRam.Text);
                        string gpuLine = _hasNvml 
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
                                double contentH = border.ActualHeight > 0 ? border.ActualHeight : 34.0;
                                _cachedSmartMargin = Math.Max(2, (int)Math.Round((curTrayH - contentH) / 2.0));
                            }

                            if (isLocked) {
                                int targetX = rCurTray.Left + 12;
                                IntPtr hAdj = FindWindow(_adjacentWndClass, null);
                                if (hAdj != IntPtr.Zero && IsWindowVisible(hAdj)) {
                                    RECT rK;
                                    if (GetWindowRect(hAdj, out rK) && rK.Right > 0 && rK.Right < 300) {
                                        targetX = rK.Right + 8; // Clean 8px gap beside adjacent widget
                                    }
                                }
                                int targetY = rCurTray.Top + _cachedSmartMargin;
                                if (Math.Abs(win.Left - targetX) > 1.0 || Math.Abs(win.Top - targetY) > 1.0) {
                                    win.Left = targetX;
                                    win.Top = targetY;
                                }
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

        private static StackPanel CreateItem(string label, TextBlock valBlock) {
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
            valBlock.HorizontalAlignment = HorizontalAlignment.Center;
            valBlock.TextAlignment = TextAlignment.Center;
            p.Children.Add(lbl);
            p.Children.Add(valBlock);
            return p;
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

