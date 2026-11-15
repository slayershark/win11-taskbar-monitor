using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WinTaskbarMonitor.LiveDaemon {
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

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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

            Thread t = new Thread(() => {
                if (hDesk != IntPtr.Zero) {
                    IntPtr curDesk = GetThreadDesktop(GetCurrentThreadId());
                    if (curDesk != hDesk) {
                        SetThreadDesktop(hDesk);
                    }
                }

                Window win = new Window();
                win.Title = "WinTaskbarMonitor";
                win.WindowStyle = WindowStyle.None;
                win.AllowsTransparency = true;
                win.Background = System.Windows.Media.Brushes.Transparent;
                win.Topmost = true;
                win.ShowInTaskbar = false;
                win.SizeToContent = SizeToContent.WidthAndHeight;
                win.ResizeMode = ResizeMode.NoResize;
                win.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif");

                IntPtr tray = FindWindow("Shell_TrayWnd", null);
                RECT rTray = new RECT { Left = 0, Top = 1032, Right = 1920, Bottom = 1080 };
                if (tray != IntPtr.Zero) { GetWindowRect(tray, out rTray); }

                int defaultX = posX > 0 ? posX : 1220;
                int defaultY = rTray.Top + (rTray.Bottom - rTray.Top - 34) / 2;
                win.Left = defaultX;
                win.Top = defaultY;

                Border border = new Border();
                border.CornerRadius = new CornerRadius(6);
                border.BorderThickness = new Thickness(1);
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 24, 24, 24));
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 255, 255, 255));
                border.Padding = new Thickness(8, 4, 8, 4);

                StackPanel stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;
                stack.VerticalAlignment = VerticalAlignment.Center;

                TextBlock txtCpu = new TextBlock { Text = "0%", FontSize = 11.5, MinWidth = 28, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                TextBlock txtGpu = new TextBlock { Text = "0%", FontSize = 11.5, MinWidth = 28, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };
                TextBlock txtCpuT = new TextBlock { Text = "N/A", FontSize = 11.5, MinWidth = 34, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)) };
                TextBlock txtGpuT = new TextBlock { Text = "N/A", FontSize = 11.5, MinWidth = 34, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)) };
                TextBlock txtRam = new TextBlock { Text = "0%", FontSize = 11.5, MinWidth = 32, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.White };

                stack.Children.Add(CreateItem("CPU", txtCpu));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("GPU", txtGpu));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("CPU", txtCpuT));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("GPU", txtGpuT));
                stack.Children.Add(CreateSep());
                stack.Children.Add(CreateItem("RAM", txtRam));

                border.Child = stack;
                win.Content = border;

                // Dragging support
                win.MouseLeftButtonDown += (s, e) => {
                    if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) {
                        win.DragMove();
                    }
                };

                // Context Menu for user control
                ContextMenu menu = new ContextMenu();
                MenuItem itemReset = new MenuItem { Header = "Reset Position to Taskbar" };
                itemReset.Click += (s, e) => {
                    win.Left = defaultX;
                    win.Top = defaultY;
                };
                MenuItem itemExit = new MenuItem { Header = "Exit WinTaskbarMonitor" };
                itemExit.Click += (s, e) => {
                    win.Close();
                    System.Windows.Threading.Dispatcher.ExitAllFrames();
                    Environment.Exit(0);
                };
                menu.Items.Add(itemReset);
                menu.Items.Add(new Separator());
                menu.Items.Add(itemExit);
                win.ContextMenu = menu;

                ToolTip tt = new ToolTip();
                tt.Content = "WinTaskbarMonitor\n• Left-click + drag to reposition\n• Right-click for options";
                win.ToolTip = tt;

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) => {
                    try {
                        long i, k, u;
                        if (GetSystemTimes(out i, out k, out u)) {
                            long tot = (k - _pKernel) + (u - _pUser);
                            long idl = i - _pIdle;
                            _pIdle = i; _pKernel = k; _pUser = u;
                            double pct = tot > 0 ? (double)(tot - idl) / tot * 100.0 : 0;
                            pct = Math.Max(0, Math.Min(100, pct));
                            txtCpu.Text = Math.Round(pct) + "%";
                            txtCpu.Foreground = pct >= 90 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : pct >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                        }

                        double totGb = 0, usedGb = 0;
                        MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                        if (GlobalMemoryStatusEx(mem)) {
                            totGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                            usedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                            double rPct = totGb > 0 ? (usedGb / totGb) * 100.0 : 0;
                            txtRam.Text = Math.Round(rPct) + "%";
                            txtRam.Foreground = rPct >= 90 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : rPct >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                        }

                        if (_hasNvml) {
                            NvmlUtil ut;
                            if (nvmlDeviceGetUtilizationRates(_nvmlDev, out ut) == 0) {
                                txtGpu.Text = ut.gpu + "%";
                                txtGpu.Foreground = ut.gpu >= 90 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : ut.gpu >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                            }
                            uint temp;
                            if (nvmlDeviceGetTemperature(_nvmlDev, 0, out temp) == 0 && temp > 0 && temp < 125) {
                                txtGpuT.Text = temp + "\u00B0C";
                                txtGpuT.Foreground = temp >= 85 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) : temp >= 75 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)) : System.Windows.Media.Brushes.White;
                            }
                        }

                        // Rich dynamic tooltip
                        string gpuInfo = _hasNvml ? txtGpu.Text : "N/A";
                        string gpuTempInfo = _hasNvml ? txtGpuT.Text : "N/A";
                        tt.Content = string.Format(
                            "WinTaskbarMonitor\n-------------------------\nCPU: {0}\nGPU: {1}\nRAM: {2:F1} / {3:F1} GB ({4})\nGPU Temp: {5}\nCPU Temp: N/A\n-------------------------\n• Left-click & drag to move\n• Right-click for options",
                            txtCpu.Text, gpuInfo, usedGb, totGb, txtRam.Text, gpuTempInfo);
                    } catch {}
                };

                win.Loaded += (s, e) => {
                    IntPtr hwnd = new WindowInteropHelper(win).Handle;
                    int style = GetWindowLong(hwnd, -20);
                    SetWindowLong(hwnd, -20, style | 0x08000080);
                    SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0020);
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
            });

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = false;
            t.Start();
            t.Join(); // Keep main thread alive!
        }

        private static StackPanel CreateItem(string label, TextBlock valBlock) {
            StackPanel p = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 0, 3, 0) };
            p.Children.Add(new TextBlock { Text = label, FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            p.Children.Add(valBlock);
            return p;
        }

        private static System.Windows.Shapes.Rectangle CreateSep() {
            return new System.Windows.Shapes.Rectangle { Width = 1, Height = 14, Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        }
    }
}
