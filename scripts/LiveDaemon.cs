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
using System.Management;

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

                int trayH = rTray.Bottom - rTray.Top;
                int widgetH = 36;
                int initialMargin = Math.Max(2, (trayH - widgetH) / 2);

                int defaultX = posX > 0 ? posX : (rTray.Left + initialMargin);
                int defaultY = rTray.Top + initialMargin;
                win.Left = defaultX;
                win.Top = defaultY;

                Border border = new Border();
                border.CornerRadius = new CornerRadius(6);
                border.BorderThickness = new Thickness(1);
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 24, 24, 24));
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 255, 255, 255));
                border.Padding = new Thickness(7, 2, 7, 2);

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

                MenuItem itemExit = new MenuItem { Header = "Exit WinTaskbarMonitor" };
                itemExit.Click += (s, e) => {
                    win.Close();
                    System.Windows.Threading.Dispatcher.ExitAllFrames();
                    Environment.Exit(0);
                };

                menu.Items.Add(itemLock);
                menu.Items.Add(posMenu);
                menu.Items.Add(itemReset);
                menu.Items.Add(new Separator());
                menu.Items.Add(itemExit);
                win.ContextMenu = menu;

                ToolTip tt = new ToolTip();
                tt.Content = "CPU: Loading...\nGPU: Loading...\nRAM: Loading...";
                win.ToolTip = tt;

                IntPtr mainHwnd = IntPtr.Zero;

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
                            int targetX = rCurTray.Left + smartMargin;
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

                        // Periodically assert TOPMOST so Explorer/taskbar never obscures it
                        if (mainHwnd != IntPtr.Zero) {
                            SetWindowPos(mainHwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040);
                        }
                    } catch {}
                };

                win.Loaded += (s, e) => {
                    mainHwnd = new WindowInteropHelper(win).Handle;
                    int style = GetWindowLong(mainHwnd, -20);
                    SetWindowLong(mainHwnd, -20, style | 0x08000080);
                    SetWindowPos(mainHwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);

                    // Immediate initial layout update for smart margin
                    border.UpdateLayout();
                    double h = border.ActualHeight > 0 ? border.ActualHeight : 36.0;
                    int sm = Math.Max(2, (int)Math.Round(((rTray.Bottom - rTray.Top) - h) / 2.0));
                    if (posX <= 0) {
                        win.Left = rTray.Left + sm;
                        win.Top = rTray.Top + sm;
                    }

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
                FontSize = 8.5, 
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
