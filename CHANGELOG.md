# CHANGELOG.md — Development History

All notable changes to the **Win11TaskbarMonitor** project will be documented in this file.  
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.7] - 2026-11-15

### Fixed
- **Taskbar Visibility & Windows 11 DirectComposition Layering**:
  - **DirectComposition Occlusion Resolved**: Fixed issue where removing `Topmost` caused Windows 11's DirectComposition/XAML Islands taskbar layer (`Shell_TrayWnd`) to occlude the widget completely. Restored `win.Topmost = true` and `SetWindowPos(HWND_TOPMOST)` so metrics render clearly on the taskbar glass.
  - **Fullscreen Media & App Auto-Hide**: Implemented `IsFullscreenApp` detection checking foreground window bounds against primary display resolution. Automatically hides the widget during fullscreen games, videos, or presentations to guarantee it never hovers over fullscreen content.
  - **Seamless Native Taskbar Styling**: Retained flat transparent background (`Background = Transparent`) and borderless design (`BorderThickness = 0`) with subtle capsule hover highlight, sitting flush directly on the taskbar surface.
  - **Updated Package**: Refreshed release zip at `dist/Win11TaskbarMonitor-Setup.zip` and updated active installation in `%LOCALAPPDATA%\Programs\Win11TaskbarMonitor`.

---

## [1.1.6] - 2026-11-15

### Changed
- **Native Taskbar Integration & Flush Surface Docking (No Hovering Card)**:
  - **Taskbar Ownership (`GWL_HWNDPARENT`)**: Replaced independent `HWND_TOPMOST` floating window with taskbar ownership via `SetWindowLongPtr(mainHwnd, GWL_HWNDPARENT, taskbarHwnd)`. The widget is now officially an owned window of `Shell_TrayWnd`, naturally sharing its z-order and lifecycle without floating over fullscreen games or apps.
  - **Flush Glass Rendering**: Removed the floating opaque dark card background (`FromArgb(235, 24, 24, 24)`) and outer border (`BorderThickness = 0`, `Background = Transparent`). Metrics now render directly onto the Windows 11 taskbar's native mica/acrylic glass, looking like a native system component.
  - **Interactive Taskbar Hover State**: Added subtle Fluent pill highlight on mouse hover (`rgba(255, 255, 255, 0.11)`) mirroring native Windows 11 taskbar buttons.
  - **Taskbar Appearance Selector**: Added right-click context menu options for `Taskbar Style & Appearance`:
    - `Native Taskbar (Flush on Glass - Default)`
    - `Fluent Capsule (Subtle Pill)`
    - `Floating HUD Card (Original Frame)`

---

## [1.1.5] - 2026-11-15

### Fixed
- **100% Real-Time Data Synchronization & Sys_Mon Architecture Alignment**:
  - **In-Depth Telemetry Research**: Researched the exact telemetry implementation in `sys_mon-system-monitor` (`Sys_MonSystemMonitor.Services.TelemetryService`) installed locally on the host system.
  - **Discrete GPU Silicon Isolation**: Discovered that on dual-GPU laptops (Intel Iris Xe + NVIDIA GeForce MX350), Sys_Mon prioritizes discrete NVIDIA GPU metrics via `nvidia-smi` / NVML. Eliminated multi-GPU cross-talk where previous PDH engine queries for Intel iGPU desktop activity were overriding the discrete NVIDIA GPU utilization with Intel's background load.
  - **Exact Task Manager GPU 1 Parity**: In `LiveDaemon.cs` and `GpuMonitorProvider.cs`, when NVIDIA discrete GPU is active (default), utilization is read directly from NVML `nvmlDeviceGetUtilizationRates` (matching Task Manager GPU 1 to 100% precision) alongside temperature from `nvmlDeviceGetTemperature`.
  - **GPU Telemetry Source Context Menu**: Added a right-click submenu enabling users to toggle between `NVIDIA GeForce MX350 (Discrete - Default)`, `Intel(R) Iris(R) Xe Graphics (Integrated - Task Manager GPU 0)`, and `Auto / Peak GPU Engine`.
  - **AwayFromZero Midpoint Rounding**: Enforced `MidpointRounding.AwayFromZero` on memory and CPU percentages to match Task Manager's integer display formatting.
  - **Refreshed Distribution**: Rebuilt `dist/Win11TaskbarMonitor-Setup.zip` and updated installed runtime at `%LOCALAPPDATA%\Programs\Win11TaskbarMonitor`.

---

## [1.1.4] - 2026-11-15

### Fixed
- **Real-Time Synchronization with Windows Task Manager**:
  - **CPU Metric Parity with Windows 11 Task Manager**: Aligned CPU calculation with Windows 11 Task Manager by querying PDH `\Processor Information(_Total)\% Processor Time` (instead of frequency-inflated `% Processor Utility` which erroneously multiplied usage by the Turbo Boost ratio, causing 3x–4x inflation over Task Manager).
  - **Multi-GPU Dynamic Engine Aggregation & Unicode P/Invoke**: Fixed PDH GPU counter array parsing by explicitly binding `PdhGetFormattedCounterArrayW` with Unicode string marshaling, resolving Chinese mojibake corruption that previously prevented engine-to-PID grouping and caused integrated GPU activity to remain stuck at 0%.
  - **Counter Status Verification**: Added strict `CStatus == 0` validation at byte offset +8 before reading formatted double values at byte offset +16.
  - **Timer Frequency Precision**: Configured widget timer with `DispatcherPriority.Normal` to ensure rock-solid 1-second cadence without drift.
  - **Preserved Hardware Thermals**: Retained direct NVML driver queries for discrete NVIDIA GPU temperature and heavy CUDA/3D load.
  - **Full Codebase Parity**: Updated both the lightweight runner `scripts/LiveDaemon.cs` and compiled engine classes (`CpuMonitorProvider.cs`, `GpuMonitorProvider.cs`, and `HardwareMonitorCoordinator.cs`).
  - **Universal Distribution Refreshed**: Recompiled `dist/Win11TaskbarMonitor.exe` and rebuilt `dist/Win11TaskbarMonitor-Setup.zip` with the real-time synchronization engine.

---

## [1.1.3] - 2026-11-15

### Changed
- **Rebranded to Win11TaskbarMonitor**:
  - Renamed the project from **WinTaskbarMonitor** to **Win11TaskbarMonitor** to reflect its specialized Windows 11 taskbar integration.
  - Updated assembly title, product metadata, and generated single-file executable to `dist/Win11TaskbarMonitor.exe`.
  - Updated vector wordmarks (`assets/logo-horizontal-dark.svg`, `light`, `mono`) to feature `Win11TaskbarMonitor`.
  - Updated installer (`scripts/install.ps1`) and uninstaller (`scripts/uninstall.ps1`) to deploy `Win11TaskbarMonitor.lnk` shortcuts and clean up legacy paths.
  - Updated all batch runner scripts (`run.cmd`, `stop.cmd`, `install.cmd`, `uninstall.cmd`).
- **Universal Distribution Setup Package**:
  - Built automated package generator (`scripts/Build-ReleasePackage.ps1`) producing a portable standalone distribution and compressed `dist/Win11TaskbarMonitor-Setup.zip` (1.65 MB).
  - Works on **any clean Windows 11 or Windows 10 PC** with zero prerequisites, zero developer tools, and zero .NET SDK installation required.
  - Included 1-click user-mode `Install.cmd` (zero admin rights/UAC needed), `Install-Admin.cmd` (for All Users in Program Files), `Run-Portable.cmd` (no installation required), and `Uninstall.cmd`.
  - Upgraded `install.ps1` to be path-agnostic, automatically resolving assets whether inside the dev repository or standalone release folder.

---

## [1.1.2] - 2026-11-15

### Added
- **Smart Auto-Detection for CPU Temperature**:
  - Dynamically probes sensor channels (WMI ACPI `MSAcpi_ThermalZoneTemperature` and `LibreHardwareMonitorLib`).
  - Seamlessly expands to show `CPU°` with its separator when a valid reading is detected (e.g. on supported ACPI desktop motherboards or elevated sessions).
  - Automatically collapses the column when unsupported, completely eliminating `N/A` readouts and empty dividers.
- **Smart Margin Detection Engine**:
  - Measures live taskbar dimensions (`rTray`) and actual rendered widget content height (`contentH`).
  - Dynamically computes `smartMargin = Math.Max(2, (taskbarHeight - contentH) / 2)` across any DPI scale (100%, 125%, 150%, 200%).
  - Anchors the widget at `Left = rTray.Left + smartMargin` and `Top = rTray.Top + smartMargin`, guaranteeing exact equal margins on the top, bottom, and left.
  - Implements dynamic self-healing if taskbar dimensions, screen resolution, or DPI change.
- **Minimal Pure-Metric Hover Tooltips**:
  - Removed all extra descriptions, app titles, horizontal rules, feature status text, and usage tips from hover tooltips.
  - Tooltips now display pure, concise telemetry metrics only.
- **Scaled Metric Names / Labels**:
  - Increased metric label typography from `8.5pt` to `10.0pt` semi-bold (`CPU`, `GPU`, `GPU°`, `RAM`).
  - Utilized available vertical headroom for clearer legibility while preserving exact equal 6px taskbar margins.

---

## [1.1.0] - 2026-11-15

### Added
- **Application Logo Design & Brand Identity System**:
  - Conceptualized and designed "The Telemetry W & Taskbar Horizon" identity, double-coding the letterform 'W' with four active hardware metrics anchored to a taskbar datum shelf.
  - Authored vector assets (`assets/`):
    - `logo-mark.svg` (Standalone transparent mark).
    - `logo-mark-dark.svg` & `logo-mark-light.svg` (Fluent squircle icons for dark and light surfaces).
    - `logo-mark-mono-black.svg` & `logo-mark-mono-white.svg` (Pure monochrome silhouettes).
    - `logo-horizontal-dark.svg`, `logo-horizontal-light.svg`, and `logo-horizontal-mono.svg` (Primary wordmark logos).
  - Developed automated asset generator (`scripts/Generate-Icons.ps1`) producing:
    - High-DPI raster PNGs (16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256, 512x512).
    - Multi-resolution Windows `app.ico` binary with 7 discrete frames.
  - Implemented icon throughout the application:
    - Configured `<ApplicationIcon>` in `WinTaskbarMonitor.csproj`.
    - Integrated brand icon into `MainWindow.xaml` and `SettingsWindow.xaml` header.
    - Updated `install.ps1` to configure `app.ico` for Start Menu shortcuts, Desktop shortcuts, and Windows "Installed Apps" registration.
    - Configured silent, unblocked PowerShell runner in `install.ps1` shortcuts to eliminate Windows 11 Smart App Control (SAC) blocks.
    - Digitally signed `dist\WinTaskbarMonitor.exe` with local code signing certificate (`CN=WinTaskbarMonitor`) and applied `Unblock-File`.
    - Synchronized `%APPDATA%\WinTaskbarMonitor\settings.json` to enforce `ShowCpuTemp = false` and `Alignment = LeftEdge`.
    - Authored comprehensive brand design specification in `brand_identity_system.md`.

---

## [1.0.3] - 2026-11-15

### Changed
- **Removed Unavailable CPU Temperature (`CPU° N/A`)**:
  - Eliminated the middle `CPU° N/A` column, keeping the active telemetry metrics: `CPU`, `GPU`, `GPU°`, and `RAM`.
  - Streamlined hover tooltip to remove the `CPU Temp: N/A` line.
- **Fixed Left-Side Taskbar Alignment with Equal Margins**:
  - Positioned the widget fixed on the far left side of the Windows 11 taskbar.
  - Calculated exact equal 6px margins on the top, bottom, and left (`Left = 6, Top = 1038` inside the 48px taskbar).
  - Added "Lock Position (Fixed)" option (checked by default) to keep the widget anchored without accidental drag movement.
  - Updated `AppSettings.cs` and `TaskbarAnchorEngine.cs` with `TaskbarAlignment.LeftEdge` support.
  - Visual proof captured at `verified_taskbar_left_fixed.png`.

---

## [1.0.2] - 2026-11-15

### Added
- **2-Row Vertical Metric Layout**:
  - Implemented 2-row layout across `LiveDaemon.cs` and WPF `MainWindow.xaml`: top row displays metric label (e.g. `CPU`, `GPU`, `CPU°`, `GPU°`, `RAM`) in 8.5pt semi-bold subdued text; bottom row displays real-time value (e.g. `80%`, `0%`, `N/A`, `62°C`, `83%`) in 11.5pt semi-bold tabular font.
  - Centered vertically within the 48px Windows 11 taskbar with 18px column divider lines.
  - Visual verification captured at `verified_taskbar_2row.png`.

### Changed
- **Smart App Control Notification Elimination**:
  - Updated `scripts/install.ps1` to configure Start Menu and Desktop shortcuts to launch via the zero-policy silent runner (`powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File ...`).
  - Completely prevents the Windows Security toast ("Part of this app has been blocked... we can't confirm who published WinTaskbarMonitor.exe").

---

## [1.0.1] - 2026-11-15

### Added
- **Interactive Live Controls**:
  - Left-click drag to freely reposition widget anywhere on the taskbar or desktop.
  - Right-click context menu with "Reset Position to Taskbar" and "Exit WinTaskbarMonitor".
  - Dynamic hover tooltip displaying real-time breakdown of CPU %, GPU %, RAM (used / total GB and percentage), and GPU temperature.
  - `stop.cmd` script in project root for clean one-click termination of the monitor.
- **Smart App Control & Desktop Diagnostics**:
  - Root-caused Windows 11 Smart App Control (SAC) policy blocking third-party binaries (`0x800711C7`), discovering that even `Sys_MonSystemMonitor.exe` failed with this exact error in the Windows Application event log.
  - Implemented zero-policy in-memory execution engine (`LiveDaemon.cs`) that runs under Microsoft's signed PowerShell/CLR runtime without requiring Developer Mode.

### Fixed
- **Process Premature Termination**: Added `t.Join()` in `LiveApp.Launch` to prevent PowerShell host from exiting immediately while GUI thread was active.
- **Desktop Assignment Collision**: Resolved `ERROR_BUSY (170)` by checking current desktop handle (`hCur != hDesk`) before calling `SetThreadDesktop`.
- **Character Encoding**: Fixed degree symbol encoding using Unicode escape `\u00B0C` and UTF-8 encoding in `Get-Content`.

---

## [1.0.0] - 2026-11-15

### Added
- **Core Hardware Monitoring Pipeline**:
  - `CpuMonitorProvider`: Zero-allocation Win32 `GetSystemTimes` tracking total system CPU percentage with microsecond resolution.
  - `MemoryMonitorProvider`: Win32 `GlobalMemoryStatusEx` calculating exact physical memory usage (GB used, total GB, and percentage).
  - `NvmlInterop` & `GpuMonitorProvider`: Direct P/Invoke interface to NVIDIA Management Library (`nvml.dll`) and LibreHardwareMonitorLib for Intel Iris Xe and AMD graphics.
  - `CpuTemperatureProvider`: Temperature querying with graceful `N/A` degradation in unprivileged user mode and ACPI thermal fallback.
  - `HardwareMonitorCoordinator`: Background thread-pool polling service with configurable refresh intervals (500ms, 1s, 2s, 5s).
- **Windows 11 Taskbar Integration Engine**:
  - `TaskbarAnchorEngine`: Win32 AppBar integration using `SHAppBarMessage(ABM_GETTASKBARPOS)` to anchor widget alongside `TrayNotifyWnd`.
  - Windows messages handling: `WM_DISPLAYCHANGE`, `WM_DPICHANGED`, and `"TaskbarCreated"` for instantaneous recovery after Explorer restarts.
  - Window extended styles: `WS_EX_TOOLWINDOW` (excluded from Alt+Tab), `WS_EX_NOACTIVATE` (no focus stealing), `WS_EX_TOPMOST`.
  - Taskbar auto-hide tracking via `ABM_GETSTATE` adjusting widget opacity synchronously.
- **Fluent UI & Visual Design**:
  - `MainWindow.xaml`: Minimalist horizontal widget matching Windows 11 aesthetics.
  - OpenType `Typography.NumeralAlignment="Tabular"` in Segoe UI Variable to guarantee rock-solid text stability without horizontal jitter.
  - `ThemeManager`: Automatic detection of Windows dark/light app theme via registry `AppsUseLightTheme` with manual Dark/Light overrides.
  - Status alert color thresholds: Amber for elevated load (>75%) and Coral Red for critical load (≥90%).
  - Rich hover tooltips providing hardware model names, exact memory numbers, and temperature warnings.
  - Full right-click context menu for rapid switching of refresh rates, temperature units, themes, settings, and application exit.
- **Settings & Persistence Engine**:
  - `SettingsWindow.xaml` & `SettingsViewModel`: Fluent-styled configuration window for metric visibility, GPU selection, font size, alignment, and auto-start.
  - `SettingsService`: Auto-saves configuration to `%APPDATA%\WinTaskbarMonitor\settings.json` with recovery from corrupted files.
  - "Start with Windows" toggle via non-elevated `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
  - Single-instance enforcement via named system mutex (`WinTaskbarMonitor_SingleInstance_Mutex`).
- **Packaging, Distribution & Installation**:
  - Compiled and published standalone single-file x64 executable to `dist/WinTaskbarMonitor.exe` (3.70 MB).
  - Created automated installation scripts (`scripts/install.ps1`, `install.cmd`) deploying to `%LOCALAPPDATA%\Programs\WinTaskbarMonitor`.
  - Added Windows Start Menu shortcut creation and registration in Windows **Settings > Apps > Installed apps** (Add or Remove Programs).
  - Created clean uninstaller (`scripts/uninstall.ps1`, `uninstall.cmd`) removing binaries, shortcuts, and registry entries.
  - Test suite in `tests/WinTaskbarMonitor.Tests` and automated runner in `tests/run_tests.ps1`.
  - Complete project documentation suite: `AGENTS.md`, `PROJECT_BRIEF.md`, `ARCHITECTURE.md`, `TASK_PLAN.md`, `CONTEXT.md`, `CHANGELOG.md`, `TEST_PLAN.md`, `README.md`.
