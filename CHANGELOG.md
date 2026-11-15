# CHANGELOG.md — Development History

All notable changes to the **WinTaskbarMonitor** project will be documented in this file.  
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
