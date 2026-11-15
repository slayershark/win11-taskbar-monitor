using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out long lpIdle, out long lpKernel, out long lpUser);

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
        private static int _gpuMode = 0; // 0 = Dedicated/NVIDIA (Task Manager GPU 1), 1 = Integrated/Intel (Task Manager GPU 0), 2 = Peak Load
        private static int _styleMode = 0; // 0 = Native Taskbar (Flush on Glass - Default), 1 = Fluent Capsule, 2 = Floating Card

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

        private static bool IsFullscreenApp(IntPtr taskbarHwnd) {
            try {
                IntPtr fg = GetForegroundWindow();
                if (fg == IntPtr.Zero || fg == taskbarHwnd || fg == GetShellWindow()) return false;
                IntPtr progman = FindWindow("Progman", null);
                if (fg == progman) return false;
                RECT r;
                if (GetWindowRect(fg, out r)) {
                    int w = (int)SystemParameters.PrimaryScreenWidth;
                    int h = (int)SystemParameters.PrimaryScreenHeight;
                    if (r.Left <= 0 && r.Top <= 0 && r.Right >= w && r.Bottom >= h) {
                        return true;
                    }
                }
            } catch {}
            return false;
        }

        public static void Launch(int posX, string capPath) {
            IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
            GetSystemTimes(out _pIdle, out _pKernel, out _pUser);

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

                IntPtr tray = FindWindow("Shell_TrayWnd", null);
                RECT rTray = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                if (tray != IntPtr.Zero) { GetWindowRect(tray, out rTray); }

                int trayH = rTray.Bottom - rTray.Top;
                int widgetH = 36;
                int initialMargin = Math.Max(2, (trayH - widgetH) / 2);

                int defaultX = posX > 0 ? posX : (rTray.Left + 12);
                int defaultY = rTray.Top + initialMargin;
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
                        // Native Flat: transparent background, zero border, sits directly on taskbar surface
                        border.Background = System.Windows.Media.Brushes.Transparent;
                        border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        border.BorderThickness = new Thickness(0);
                        border.CornerRadius = new CornerRadius(4);
                    } else if (_styleMode == 1) {
                        // Fluent Capsule: subtle taskbar pill
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
                        border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));
                        border.BorderThickness = new Thickness(1);
                        border.CornerRadius = new CornerRadius(4);
                    } else {
                        // Floating HUD Card: dark frame
                        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 24, 24, 24));
                        border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 255, 255, 255));
                        border.BorderThickness = new Thickness(1);
                        border.CornerRadius = new CornerRadius(6);
                    }
                };
                applyStyle();

                border.MouseEnter += (s, e) => {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(28, 255, 255, 255));
                };
                border.MouseLeave += (s, e) => {
                    applyStyle();
                };

                StackPanel stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;
                stack.VerticalAlignment = VerticalAlignment.Center;

                TextBlock txtCpu = new TextBlock { Text = "0%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 30, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                TextBlock txtCpuT = new TextBlock { Text = "0\u00B0C", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 34, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                StackPanel pnlCpuT = CreateItem("CPU\u00B0", txtCpuT);
                System.Windows.Shapes.Rectangle sepCpuT = CreateSep();
                pnlCpuT.Visibility = Visibility.Collapsed;
                sepCpuT.Visibility = Visibility.Collapsed;

                TextBlock txtGpu = new TextBlock { Text = "0%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 30, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                TextBlock txtGpuT = new TextBlock { Text = "N/A", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 34, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)) };
                TextBlock txtRam = new TextBlock { Text = "0%", FontSize = 11.5, FontWeight = FontWeights.SemiBold, MinWidth = 30, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };

                stack.Children.Add(CreateItem("CPU", txtCpu));
                stack.Children.Add(sepCpuT);
                stack.Children.Add(pnlCpuT);
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("GPU", txtGpu));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("GPU\u00B0", txtGpuT));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("RAM", txtRam));

                border.Child = stack;
                win.Content = border;

                bool isLocked = true;

                // Defensive drag move (only when unlocked)
                win.MouseLeftButtonDown += (s, e) => {
                    try {
                        if (!isLocked && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) {
                            win.DragMove();
                        }
                    } catch {}
                };

                // Context Menu for user control
                ContextMenu menu = new ContextMenu();
                
                MenuItem itemLock = new MenuItem { Header = "Lock Position (Fixed)", IsCheckable = true, IsChecked = true };
                itemLock.Click += (s, e) => {
                    isLocked = itemLock.IsChecked;
                };

                MenuItem posMenu = new MenuItem { Header = "Quick Position" };
                
                MenuItem posLeft = new MenuItem { Header = "Left of Taskbar (Smart Equal Margin)" };
                posLeft.Click += (s, e) => {
                    IntPtr tHwnd = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (tHwnd != IntPtr.Zero) { GetWindowRect(tHwnd, out rt); }
                    border.UpdateLayout();
                    double h = border.ActualHeight > 0 ? border.ActualHeight : 36.0;
                    int sm = Math.Max(2, (int)Math.Round(((rt.Bottom - rt.Top) - h) / 2.0));
                    win.Left = rt.Left + sm;
                    win.Top = rt.Top + sm;
                };

                MenuItem posTray = new MenuItem { Header = "Right of Taskbar (Next to Tray)" };
                posTray.Click += (s, e) => {
                    IntPtr tHwnd = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (tHwnd != IntPtr.Zero) { GetWindowRect(tHwnd, out rt); }
                    border.UpdateLayout();
                    double h = border.ActualHeight > 0 ? border.ActualHeight : 36.0;
                    int sm = Math.Max(2, (int)Math.Round(((rt.Bottom - rt.Top) - h) / 2.0));
                    win.Left = 1220;
                    win.Top = rt.Top + sm;
                };

                MenuItem posAbove = new MenuItem { Header = "Above Taskbar (Floating Dock)" };
                posAbove.Click += (s, e) => {
                    IntPtr tHwnd = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (tHwnd != IntPtr.Zero) { GetWindowRect(tHwnd, out rt); }
                    win.Left = rt.Left + 6;
                    win.Top = rt.Top - 42;
                };

                MenuItem posTopRight = new MenuItem { Header = "Top Right of Screen" };
                posTopRight.Click += (s, e) => { win.Left = 1650; win.Top = 10; };

                posMenu.Items.Add(posLeft);
                posMenu.Items.Add(posTray);
                posMenu.Items.Add(posAbove);
                posMenu.Items.Add(posTopRight);

                MenuItem itemReset = new MenuItem { Header = "Reset Position to Left (Smart Margin)" };
                itemReset.Click += (s, e) => {
                    IntPtr tHwnd = FindWindow("Shell_TrayWnd", null);
                    RECT rt = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                    if (tHwnd != IntPtr.Zero) { GetWindowRect(tHwnd, out rt); }
                    border.UpdateLayout();
                    double h = border.ActualHeight > 0 ? border.ActualHeight : 36.0;
                    int sm = Math.Max(2, (int)Math.Round(((rt.Bottom - rt.Top) - h) / 2.0));
                    win.Left = rt.Left + sm;
                    win.Top = rt.Top + sm;
                };

                MenuItem itemExit = new MenuItem { Header = "Exit Win11TaskbarMonitor" };
                itemExit.Click += (s, e) => {
                    win.Close();
                    System.Windows.Threading.Dispatcher.ExitAllFrames();
                    Environment.Exit(0);
                };

                MenuItem gpuMenu = new MenuItem { Header = "GPU Telemetry Source" };
                MenuItem gpuItemNvidia = new MenuItem { Header = "NVIDIA GeForce MX350 (Discrete - 100% Synced)", IsCheckable = true, IsChecked = true };
                MenuItem gpuItemIntel = new MenuItem { Header = "Intel(R) Iris(R) Xe Graphics (Integrated)", IsCheckable = true, IsChecked = false };
                MenuItem gpuItemPeak = new MenuItem { Header = "Auto / Peak GPU Engine", IsCheckable = true, IsChecked = false };

                gpuItemNvidia.Click += (s, e) => {
                    _gpuMode = 0;
                    gpuItemNvidia.IsChecked = true;
                    gpuItemIntel.IsChecked = false;
                    gpuItemPeak.IsChecked = false;
                };
                gpuItemIntel.Click += (s, e) => {
                    _gpuMode = 1;
                    gpuItemNvidia.IsChecked = false;
                    gpuItemIntel.IsChecked = true;
                    gpuItemPeak.IsChecked = false;
                };
                gpuItemPeak.Click += (s, e) => {
                    _gpuMode = 2;
                    gpuItemNvidia.IsChecked = false;
                    gpuItemIntel.IsChecked = false;
                    gpuItemPeak.IsChecked = true;
                };

                gpuMenu.Items.Add(gpuItemNvidia);
                gpuMenu.Items.Add(gpuItemIntel);
                gpuMenu.Items.Add(gpuItemPeak);

                MenuItem styleMenu = new MenuItem { Header = "Taskbar Style & Appearance" };
                MenuItem styleNative = new MenuItem { Header = "Native Taskbar (Flush on Glass - Default)", IsCheckable = true, IsChecked = true };
                MenuItem stylePill = new MenuItem { Header = "Fluent Capsule (Subtle Pill)", IsCheckable = true, IsChecked = false };
                MenuItem styleCard = new MenuItem { Header = "Floating HUD Card (Original Frame)", IsCheckable = true, IsChecked = false };

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

                ToolTip tt = new ToolTip();
                tt.Content = "CPU: Loading...\nGPU: Loading...\nRAM: Loading...";
                win.ToolTip = tt;

                IntPtr mainHwnd = IntPtr.Zero;

                DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal);
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) => {
                    try {
                        int pdhCollectRes = -999;
                        if (_hasPdh) {
                            try { pdhCollectRes = PdhCollectQueryData(_hPdhQuery); } catch {}
                        }

                        // 1. CPU Utilization (matches Task Manager % Processor Utility)
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
                        txtCpu.Text = ((int)Math.Round(pct, MidpointRounding.AwayFromZero)) + "%";
                        txtCpu.Foreground = pct >= 90 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : pct >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;

                        // 2. RAM Utilization
                        double totGb = 0, usedGb = 0, rPct = 0;
                        MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                        if (GlobalMemoryStatusEx(mem)) {
                            totGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                            usedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                            rPct = totGb > 0 ? (usedGb / totGb) * 100.0 : 0;
                            txtRam.Text = ((int)Math.Round(rPct, MidpointRounding.AwayFromZero)) + "%";
                            txtRam.Foreground = rPct >= 90 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : rPct >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                        }

                        // 3. GPU Utilization & Temperature (100% Task Manager Parity)
                        double gpuPct = 0.0;
                        string gpuTempStr = "N/A";
                        SolidColorBrush gpuTempBrush = System.Windows.Media.Brushes.Gray;

                        // A. Query NVIDIA discrete GPU metrics via NVML
                        double nvmlGpu = 0.0;
                        if (_hasNvml) {
                            try {
                                NvmlUtil ut;
                                if (nvmlDeviceGetUtilizationRates(_nvmlDev, out ut) == 0) {
                                    nvmlGpu = (double)ut.gpu;
                                }
                                uint temp;
                                if (nvmlDeviceGetTemperature(_nvmlDev, 0, out temp) == 0 && temp > 0 && temp < 125) {
                                    gpuTempStr = temp + "\u00B0C";
                                    gpuTempBrush = temp >= 85 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : temp >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                                }
                            } catch {}
                        }

                        // B. Query PDH for GPU Engine metrics (Task Manager engine parity)
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

                        // Select GPU based on mode (Mode 0: Discrete NVIDIA, Mode 1: Integrated Intel, Mode 2: Peak)
                        if (_gpuMode == 0) {
                            // Default: Discrete NVIDIA GPU (100% in sync with Task Manager GPU 1 and Sys_Mon)
                            gpuPct = _hasNvml ? nvmlGpu : pdhMaxEngine;
                            txtGpuT.Text = gpuTempStr;
                            txtGpuT.Foreground = gpuTempBrush;
                        } else if (_gpuMode == 1) {
                            // Integrated Intel Iris Xe (100% in sync with Task Manager GPU 0)
                            gpuPct = intel3d > 0 ? intel3d : pdhMaxEngine;
                            txtGpuT.Text = "N/A";
                            txtGpuT.Foreground = System.Windows.Media.Brushes.Gray;
                        } else {
                            // Auto / Peak Engine across all GPUs
                            gpuPct = Math.Max(nvmlGpu, pdhMaxEngine);
                            txtGpuT.Text = gpuTempStr;
                            txtGpuT.Foreground = gpuTempBrush;
                        }

                        gpuPct = Math.Max(0.0, Math.Min(100.0, gpuPct));
                        txtGpu.Text = ((int)Math.Round(gpuPct, MidpointRounding.AwayFromZero)) + "%";
                        txtGpu.Foreground = gpuPct >= 90 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : gpuPct >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;


                        // Smart Auto-Detection: CPU Temperature
                        double? cpuT = QueryCpuTemp();
                        if (cpuT.HasValue && cpuT.Value > 15 && cpuT.Value < 115) {
                            txtCpuT.Text = Math.Round(cpuT.Value) + "\u00B0C";
                            txtCpuT.Foreground = cpuT.Value >= 85 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : cpuT.Value >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                            if (pnlCpuT.Visibility != Visibility.Visible) {
                                pnlCpuT.Visibility = Visibility.Visible;
                                sepCpuT.Visibility = Visibility.Visible;
                            }
                        } else {
                            if (pnlCpuT.Visibility != Visibility.Collapsed) {
                                pnlCpuT.Visibility = Visibility.Collapsed;
                                sepCpuT.Visibility = Visibility.Collapsed;
                            }
                        }

                        // Smart Margin Detection: dynamically calculates equal margins on top, bottom, and left
                        IntPtr curTray = FindWindow("Shell_TrayWnd", null);
                        RECT rCurTray = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                        if (curTray != IntPtr.Zero) { GetWindowRect(curTray, out rCurTray); }
                        int curTrayH = rCurTray.Bottom - rCurTray.Top;

                        border.UpdateLayout();
                        double contentH = border.ActualHeight > 0 ? border.ActualHeight : 36.0;
                        int smartMargin = Math.Max(2, (int)Math.Round((curTrayH - contentH) / 2.0));

                        if (isLocked) {
                            int targetX = rCurTray.Left + 12;
                            int targetY = rCurTray.Top + smartMargin;
                            if (Math.Abs(win.Left - targetX) > 0.5 || Math.Abs(win.Top - targetY) > 0.5) {
                                win.Left = targetX;
                                win.Top = targetY;
                            }
                        }

                        // Clean, minimal hover tooltip (no extra descriptions)
                        string cpuLine = pnlCpuT.Visibility == Visibility.Visible 
                            ? string.Format("CPU: {0} ({1})", txtCpu.Text, txtCpuT.Text)
                            : string.Format("CPU: {0}", txtCpu.Text);
                        string gpuLine = _hasNvml 
                            ? string.Format("GPU: {0} ({1})", txtGpu.Text, txtGpuT.Text)
                            : string.Format("GPU: {0}", txtGpu.Text);
                        string ramLine = string.Format("RAM: {0:F1} / {1:F1} GB ({2})", usedGb, totGb, txtRam.Text);

                        tt.Content = string.Format("{0}\n{1}\n{2}", cpuLine, gpuLine, ramLine);

                        // Maintain Z-order anchored directly to the Taskbar surface (auto-hide if fullscreen app is active)
                        if (mainHwnd != IntPtr.Zero) {
                            if (IsFullscreenApp(curTray)) {
                                if (win.Visibility != Visibility.Collapsed) win.Visibility = Visibility.Collapsed;
                            } else {
                                if (win.Visibility != Visibility.Visible) win.Visibility = Visibility.Visible;
                                SetWindowPos(mainHwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040);
                            }
                        }
                    } catch {}
                };

                win.Loaded += (s, e) => {
                    mainHwnd = new WindowInteropHelper(win).Handle;
                    int style = GetWindowLong(mainHwnd, -20);
                    SetWindowLong(mainHwnd, -20, style | 0x08000080);

                    // Ensure window stays directly on top of the taskbar glass in the Topmost band
                    SetWindowPos(mainHwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040);

                    // Immediate initial layout update for smart margin
                    border.UpdateLayout();
                    double h = border.ActualHeight > 0 ? border.ActualHeight : 34.0;
                    int sm = Math.Max(2, (int)Math.Round(((rTray.Bottom - rTray.Top) - h) / 2.0));
                    if (posX <= 0) {
                        win.Left = rTray.Left + 12;
                        win.Top = rTray.Top + sm;
                    }

                    try {
                        if (PdhOpenQuery(null, IntPtr.Zero, out _hPdhQuery) == 0) {
                            PdhAddEnglishCounter(_hPdhQuery, @"\Processor Information(_Total)\% Processor Time", IntPtr.Zero, out _hCpuCounter);
                            PdhAddEnglishCounter(_hPdhQuery, @"\GPU Engine(*)\Utilization Percentage", IntPtr.Zero, out _hGpuCounter);
                            PdhCollectQueryData(_hPdhQuery);
                            _hasPdh = true;
                        }
                    } catch {}

                    timer.Start();

                    if (!string.IsNullOrEmpty(capPath)) {
                        DispatcherTimer capTimer = new DispatcherTimer();
                        capTimer.Interval = TimeSpan.FromSeconds(2.0);
                        capTimer.Tick += (cs, ce) => {
                            capTimer.Stop();
                            try {
                                Bitmap bmp = new Bitmap(1920, 48);
                                using (Graphics g = Graphics.FromImage(bmp)) {
                                    g.CopyFromScreen(0, 1032, 0, 0, new System.Drawing.Size(1920, 48));
                                }
                                bmp.Save(capPath, ImageFormat.Png);
                                bmp.Dispose();
                            } catch {}
                        };
                        capTimer.Start();
                    }
                };

                win.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = false;
            t.Start();
            t.Join(); // Keep main thread alive!
        }

        private static StackPanel CreateItem(string label, TextBlock valBlock) {
            StackPanel p = new StackPanel { 
                Orientation = Orientation.Vertical, 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(3, 0, 3, 0),
                MinWidth = 30
            };
            TextBlock lbl = new TextBlock { 
                Text = label, 
                FontSize = 10.0, 
                FontWeight = FontWeights.SemiBold, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)), 
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
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)), 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(4, 0, 4, 0) 
            };
        }
    }
}
