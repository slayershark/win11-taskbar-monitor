using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Win11TaskbarMonitor.Installer
{
    public partial class MainWindow : Window
    {
        private enum WizardStep
        {
            Welcome,
            Preferences,
            Installing,
            Finished,
            Uninstall
        }

        private WizardStep _currentStep = WizardStep.Welcome;
        private readonly bool _isUninstallMode;
        private readonly bool _isSilentMode;

        public MainWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            string exeName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule?.FileName ?? "").ToLowerInvariant();

            foreach (string arg in args)
            {
                string lower = arg.ToLowerInvariant();
                if (lower == "/uninstall" || lower == "-uninstall" || lower == "--uninstall")
                    _isUninstallMode = true;
                if (lower == "/silent" || lower == "/s" || lower == "-s" || lower == "/quiet" || lower == "-quiet")
                    _isSilentMode = true;
            }

            if (exeName.Contains("uninstall"))
            {
                _isUninstallMode = true;
            }

            string defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Win11TaskbarMonitor");
            TxtInstallPath.Text = defaultPath;

            if (_isUninstallMode)
            {
                SwitchToStep(WizardStep.Uninstall);
            }
            else
            {
                SwitchToStep(WizardStep.Welcome);
            }

            if (_isSilentMode)
            {
                Loaded += async (s, e) => await RunSilentIfRequestedAsync();
                SourceInitialized += async (s, e) => await RunSilentIfRequestedAsync();
                ContentRendered += async (s, e) => await RunSilentIfRequestedAsync();
            }
        }

        private int _silentExecuted = 0;
        private async Task RunSilentIfRequestedAsync()
        {
            if (!_isSilentMode) return;
            if (System.Threading.Interlocked.Exchange(ref _silentExecuted, 1) != 0) return;

            Visibility = Visibility.Hidden;
            if (_isUninstallMode)
                await PerformUninstallAsync();
            else
                await PerformInstallAsync();
            Application.Current.Shutdown();
        }

        private void SwitchToStep(WizardStep step)
        {
            _currentStep = step;

            PanelWelcome.Visibility = step == WizardStep.Welcome ? Visibility.Visible : Visibility.Collapsed;
            PanelPreferences.Visibility = step == WizardStep.Preferences ? Visibility.Visible : Visibility.Collapsed;
            PanelProgress.Visibility = step == WizardStep.Installing ? Visibility.Visible : Visibility.Collapsed;
            PanelFinished.Visibility = step == WizardStep.Finished ? Visibility.Visible : Visibility.Collapsed;
            PanelUninstall.Visibility = step == WizardStep.Uninstall ? Visibility.Visible : Visibility.Collapsed;

            switch (step)
            {
                case WizardStep.Welcome:
                    BtnBack.Visibility = Visibility.Collapsed;
                    BtnNext.Content = "Next >";
                    BtnNext.IsEnabled = true;
                    BtnCancel.Content = "Cancel";
                    BtnCancel.IsEnabled = true;
                    break;

                case WizardStep.Preferences:
                    BtnBack.Visibility = Visibility.Visible;
                    BtnNext.Content = "Install";
                    BtnNext.IsEnabled = true;
                    BtnCancel.Content = "Cancel";
                    BtnCancel.IsEnabled = true;
                    break;

                case WizardStep.Installing:
                    BtnBack.Visibility = Visibility.Collapsed;
                    BtnNext.Visibility = Visibility.Collapsed;
                    BtnCancel.IsEnabled = false;
                    break;

                case WizardStep.Finished:
                    BtnBack.Visibility = Visibility.Collapsed;
                    BtnNext.Visibility = Visibility.Visible;
                    BtnNext.Content = "Finish";
                    BtnNext.IsEnabled = true;
                    BtnCancel.Visibility = Visibility.Collapsed;
                    TxtSummaryDest.Text = $"Destination: {TxtInstallPath.Text}";
                    break;

                case WizardStep.Uninstall:
                    Title = "Win11TaskbarMonitor Uninstallation";
                    TxtHeaderTitle.Text = "Win11TaskbarMonitor Uninstallation";
                    TxtHeaderSub.Text = "Cleanly remove Win11TaskbarMonitor from this system";
                    BtnBack.Visibility = Visibility.Collapsed;
                    BtnNext.Content = "Uninstall";
                    BtnNext.IsEnabled = true;
                    BtnCancel.Content = "Cancel";
                    BtnCancel.IsEnabled = true;
                    break;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Destination Folder",
                InitialDirectory = TxtInstallPath.Text
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                TxtInstallPath.Text = Path.Combine(dialog.FolderName, "Win11TaskbarMonitor");
            }
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentStep)
            {
                case WizardStep.Welcome:
                    SwitchToStep(WizardStep.Preferences);
                    break;

                case WizardStep.Preferences:
                    SwitchToStep(WizardStep.Installing);
                    await PerformInstallAsync();
                    SwitchToStep(WizardStep.Finished);
                    break;

                case WizardStep.Finished:
                    if (ChkLaunchNow.IsChecked == true)
                    {
                        string exePath = Path.Combine(TxtInstallPath.Text, "Win11TaskbarMonitor.exe");
                        if (File.Exists(exePath))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = exePath,
                                    UseShellExecute = true,
                                    WorkingDirectory = TxtInstallPath.Text
                                });
                            }
                            catch { }
                        }
                    }
                    Close();
                    break;

                case WizardStep.Uninstall:
                    SwitchToStep(WizardStep.Installing);
                    TxtProgressTitle.Text = "Uninstalling Win11TaskbarMonitor...";
                    await PerformUninstallAsync();
                    MessageBox.Show(
                        "Win11TaskbarMonitor has been successfully removed from your computer.",
                        "Uninstall Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Close();
                    break;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == WizardStep.Preferences)
            {
                SwitchToStep(WizardStep.Welcome);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LogProgress(int percentage, string status, string? detail = null)
        {
            if (_isSilentMode) return;
            try
            {
                Dispatcher.Invoke(() =>
                {
                    PrgProgress.Value = percentage;
                    TxtProgressStatus.Text = status;
                    if (!string.IsNullOrEmpty(detail))
                    {
                        TxtProgressLog.Text += $"[{DateTime.Now:HH:mm:ss}] {detail}\n";
                    }
                });
            }
            catch { }
        }

        private async Task PerformInstallAsync()
        {
            string installDir = TxtInstallPath.Text.Trim();
            bool makeDesktopShortcut = ChkDesktopShortcut.IsChecked ?? true;
            bool makeStartMenuShortcut = ChkStartMenuShortcut.IsChecked ?? true;
            bool startWithWindows = ChkStartWithWindows.IsChecked ?? true;
            int gpuModeIndex = CmbGpuMode.SelectedIndex;
            string tempUnit = CmbTempUnit.SelectedIndex == 1 ? "Fahrenheit" : "Celsius";

            await Task.Run(() =>
            {
                try
                {
                    // 1. Terminate existing instances
                    LogProgress(10, "Closing existing monitor processes...", "Checking for active Win11TaskbarMonitor instances...");
                    foreach (var p in Process.GetProcessesByName("Win11TaskbarMonitor"))
                    {
                        try
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                            LogProgress(15, "Terminated active process", $"Stopped PID {p.Id}");
                        }
                        catch { }
                    }

                    // 2. Prepare target directory
                    LogProgress(25, "Preparing target installation directory...", $"Creating directory: {installDir}");
                    if (!Directory.Exists(installDir))
                    {
                        Directory.CreateDirectory(installDir);
                    }

                    // 3. Extract payload
                    LogProgress(35, "Extracting application binaries and libraries...", "Unpacking embedded payload archive...");
                    var assembly = Assembly.GetExecutingAssembly();
                    string resourceName = "Win11TaskbarMonitor.Installer.payload.zip";
                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                            {
                                int total = archive.Entries.Count;
                                int count = 0;
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    count++;
                                    string destPath = Path.Combine(installDir, entry.FullName);
                                    if (string.IsNullOrEmpty(entry.Name))
                                    {
                                        Directory.CreateDirectory(destPath);
                                        continue;
                                    }

                                    string? parent = Path.GetDirectoryName(destPath);
                                    if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                                    {
                                        Directory.CreateDirectory(parent);
                                    }

                                    entry.ExtractToFile(destPath, overwrite: true);
                                    int pct = 35 + (int)((count / (double)total) * 30);
                                    LogProgress(pct, $"Extracting {entry.Name}...", $"Unpacked {entry.FullName}");
                                }
                            }
                        }
                        else
                        {
                            throw new FileNotFoundException("Embedded payload resource could not be found.");
                        }
                    }

                    // 4. Copy self as Uninstall.exe
                    string currentSetupExe = Environment.ProcessPath ?? "";
                    string targetUninstaller = Path.Combine(installDir, "Uninstall.exe");
                    if (!string.IsNullOrEmpty(currentSetupExe) && File.Exists(currentSetupExe))
                    {
                        try
                        {
                            File.Copy(currentSetupExe, targetUninstaller, overwrite: true);
                            LogProgress(68, "Configuring uninstaller...", "Created Uninstall.exe");
                        }
                        catch (Exception ex)
                        {
                            LogProgress(68, "Configuring uninstaller...", $"Notice: {ex.Message}");
                        }
                    }

                    // 5. Create Shortcuts
                    string appExe = Path.Combine(installDir, "Win11TaskbarMonitor.exe");
                    string iconPath = Path.Combine(installDir, "app.ico");
                    if (!File.Exists(iconPath)) iconPath = appExe;

                    if (makeStartMenuShortcut)
                    {
                        LogProgress(72, "Creating Start Menu shortcut...", "Registering Windows Start Menu entry...");
                        string smDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs");
                        string smShortcut = Path.Combine(smDir, "Win11TaskbarMonitor.lnk");
                        CreateShortcut(smShortcut, appExe, iconPath, "Lightweight Windows 11 Taskbar Monitor");
                    }

                    if (makeDesktopShortcut)
                    {
                        LogProgress(78, "Creating Desktop shortcut...", "Placing shortcut on User Desktop...");
                        string dtDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        string dtShortcut = Path.Combine(dtDir, "Win11TaskbarMonitor.lnk");
                        CreateShortcut(dtShortcut, appExe, iconPath, "Lightweight Windows 11 Taskbar Monitor");
                    }

                    // 6. Configure Startup
                    if (startWithWindows)
                    {
                        LogProgress(84, "Registering Windows startup...", "Adding HKCU Run registry entry...");
                        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                        if (runKey != null)
                        {
                            runKey.SetValue("Win11TaskbarMonitor", $"\"{appExe}\"");
                        }
                    }

                    // 7. Register in Windows Installed Apps (Add/Remove Programs)
                    LogProgress(90, "Registering application with Windows Settings...", "Creating Uninstall registry keys...");
                    using (RegistryKey? uninstKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Win11TaskbarMonitor"))
                    {
                        if (uninstKey != null)
                        {
                            uninstKey.SetValue("DisplayName", "Win11TaskbarMonitor");
                            uninstKey.SetValue("DisplayVersion", "1.2.2");
                            uninstKey.SetValue("Publisher", "slayershark");
                            uninstKey.SetValue("InstallLocation", installDir);
                            uninstKey.SetValue("DisplayIcon", $"{iconPath},0");
                            uninstKey.SetValue("UninstallString", $"\"{targetUninstaller}\" /uninstall");
                            uninstKey.SetValue("QuietUninstallString", $"\"{targetUninstaller}\" /uninstall /silent");
                            uninstKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                            uninstKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                            uninstKey.SetValue("EstimatedSize", 5000, RegistryValueKind.DWord);
                        }
                    }

                    // 8. Save Initial Preferences to settings.json
                    LogProgress(96, "Saving user configuration preferences...", "Writing settings.json...");
                    int selectedGpu = gpuModeIndex switch
                    {
                        1 => 0,  // Dedicated NVIDIA
                        2 => 1,  // Integrated Intel
                        _ => -1  // Auto (-1)
                    };

                    var initialSettings = new
                    {
                        RefreshIntervalMs = 1000,
                        ShowCpu = true,
                        ShowGpu = true,
                        ShowCpuTemp = true,
                        ShowGpuTemp = true,
                        ShowRam = true,
                        TemperatureUnit = tempUnit,
                        ThemeMode = "System",
                        Alignment = "LeftOfTray",
                        FontSize = 11.0,
                        CompactMode = false,
                        SelectedGpuIndex = selectedGpu
                    };

                    string json = JsonSerializer.Serialize(initialSettings, new JsonSerializerOptions { WriteIndented = true });
                    string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    foreach (string folderName in new[] { "Win11TaskbarMonitor", "WinTaskbarMonitor" })
                    {
                        string sDir = Path.Combine(appDataRoot, folderName);
                        if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
                        File.WriteAllText(Path.Combine(sDir, "settings.json"), json);
                    }

                    LogProgress(100, "Installation completed!", "All components successfully configured and ready.");
                }
                catch (Exception ex)
                {
                    LogProgress(100, "Error during installation", ex.Message);
                    if (!_isSilentMode)
                    {
                        MessageBox.Show(
                            $"An error occurred during installation:\n\n{ex.Message}",
                            "Installation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            });
        }

        private async Task PerformUninstallAsync()
        {
            bool removeSettings = ChkRemoveSettings.IsChecked ?? true;

            await Task.Run(() =>
            {
                try
                {
                    LogProgress(15, "Stopping monitor processes...", "Terminating running instances...");
                    foreach (var p in Process.GetProcessesByName("Win11TaskbarMonitor"))
                    {
                        try
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                        }
                        catch { }
                    }

                    LogProgress(35, "Removing shortcuts...", "Deleting Desktop and Start Menu shortcuts...");
                    string smShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Win11TaskbarMonitor.lnk");
                    if (File.Exists(smShortcut)) File.Delete(smShortcut);

                    string dtShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Win11TaskbarMonitor.lnk");
                    if (File.Exists(dtShortcut)) File.Delete(dtShortcut);

                    LogProgress(55, "Removing Windows startup entry...", "Cleaning HKCU Run key...");
                    using (RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true))
                    {
                        runKey?.DeleteValue("Win11TaskbarMonitor", throwOnMissingValue: false);
                    }

                    LogProgress(70, "Unregistering application...", "Removing Windows Installed Apps registration...");
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Win11TaskbarMonitor", throwOnMissingSubKey: false);

                    if (removeSettings)
                    {
                        LogProgress(85, "Removing configuration settings...", "Purging AppData folder...");
                        string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        foreach (string folderName in new[] { "Win11TaskbarMonitor", "WinTaskbarMonitor" })
                        {
                            string sDir = Path.Combine(appDataRoot, folderName);
                            if (Directory.Exists(sDir))
                            {
                                try { Directory.Delete(sDir, recursive: true); } catch { }
                            }
                        }
                    }

                    LogProgress(95, "Scheduling cleanup of application files...", "Removing program directory...");
                    string currentExe = Environment.ProcessPath ?? "";
                    string installDir = Path.GetDirectoryName(currentExe) ?? "";

                    // If running from the install folder, delete other files and schedule self deletion
                    if (Directory.Exists(installDir) && File.Exists(Path.Combine(installDir, "Win11TaskbarMonitor.exe")))
                    {
                        foreach (string file in Directory.GetFiles(installDir))
                        {
                            if (!file.Equals(currentExe, StringComparison.OrdinalIgnoreCase))
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }

                        // Schedule folder removal after process terminates
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c timeout /t 2 /nobreak >nul & rmdir /s /q \"{installDir}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                    }

                    LogProgress(100, "Uninstallation completed!", "Win11TaskbarMonitor removed.");
                }
                catch (Exception ex)
                {
                    LogProgress(100, "Error during uninstallation", ex.Message);
                }
            });
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string iconPath, string description)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType)!;
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                    shortcut.IconLocation = iconPath;
                    shortcut.Description = description;
                    shortcut.Save();
                }
            }
            catch { }
        }
    }
}
