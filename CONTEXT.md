# CONTEXT.md — Persistent Development Context

## 1. Project Objectives & Current Status

**Project**: WinTaskbarMonitor  
**Objective**: Build a lightweight, native, and highly reliable Windows 11 taskbar hardware telemetry utility (CPU, GPU, CPU Temp, GPU Temp, RAM) without bloat, web dependencies, or excessive resource overhead.  
**Current Milestone**: All 11 Phases Complete. Application is fully built, tested, published to `dist/`, and **installed onto the host system** at `%LOCALAPPDATA%\Programs\WinTaskbarMonitor` with Start Menu, Desktop shortcuts, and Windows startup configured.

---

## 2. Environment & Repository Status

- **Operating System**: Windows 11 Build 26300 (x64)
- **CPU**: 11th Gen Intel(R) Core(TM) i5-1135G7 (4 cores, 8 threads)
- **GPUs Detected & Monitored**:
  1. Intel(R) Iris(R) Xe Graphics (Integrated)
  2. NVIDIA GeForce MX350 (Discrete, verified at 59°C via NVML)
- **Runtime & SDK**: .NET 8 SDK 8.0.428 in `%LOCALAPPDATA%\dotnet`.
- **Git Repository**: Initialized locally in `d:\WinTaskbarMonitor`.
  - Identity configured at repo level:
    - `user.name`: `slayershark`
    - `user.email`: `sandeepsuwal9@gmail.com`
  - Current Branch: `master`
  - Latest Commit: `ce28b31` (feat: support Program Files system-wide install to run without Developer Mode)

---

## 3. Completed Features

- [x] **CPU Monitoring**: Zero-allocation Win32 `GetSystemTimes` calculating instantaneous system processor usage.
- [x] **RAM Monitoring**: Win32 `GlobalMemoryStatusEx` calculating total GB, used GB, and percentage (matches Task Manager).
- [x] **GPU Monitoring**: Dual-pipeline architecture with native `nvml.dll` P/Invoke for NVIDIA cards and LibreHardwareMonitorLib for Intel Iris Xe and AMD cards.
- [x] **CPU & GPU Temperature Monitoring**: Querying NVML directly for NVIDIA GPU temp (°C/°F) and graceful `N/A` fallback for unprivileged CPU thermal sensors without elevation or crashing.
- [x] **Taskbar Integration Engine**: Dynamic AppBar tracking (`SHAppBarMessage`), positioning to the left of `TrayNotifyWnd`, DPI scaling awareness (`PerMonitorV2`), and `"TaskbarCreated"` Explorer restart recovery.
- [x] **Window Behaviors**: `WS_EX_TOOLWINDOW` (hidden from Alt+Tab), `WS_EX_NOACTIVATE` (no focus stealing), `WS_EX_TOPMOST`.
- [x] **Visual Design**: Segoe UI Variable typography with OpenType `Typography.NumeralAlignment="Tabular"` to eliminate horizontal jitter; Fluent-style dark, light, and automatic Windows system theme integration.
- [x] **Context Menu**: Right-click menu for quick access to Refresh Rates (500ms, 1s, 2s, 5s), Temperature Units (°C / °F), Theme switching, Settings dialog, and Exit.
- [x] **Settings Interface**: Full Fluent configuration window for metric visibility, preferred GPU selection, font size, alignment, and auto-start.
- [x] **Startup Integration**: Non-elevated auto-start via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- [x] **Single-Instance Enforcement**: Named system Mutex prevents duplicate overlapping instances.
- [x] **Standalone Release**: Published single-file executable `dist/WinTaskbarMonitor.exe` (3.70 MB).

---

## 4. Key Architectural Decisions

1. **Native C# 12 & .NET 8 WPF**: Modern, hardware-accelerated, DirectWrite vector rendering with sub-second cold start and zero Chromium/Electron overhead.
2. **Taskbar-Anchored Floating Tool Window**: Avoids deprecated DeskBand APIs and dangerous Explorer process injection. Uses standard Win32 shell messages to remain docked alongside the taskbar.
3. **Multi-GPU & Graceful Degradation**:
   - Hardware detection queries both NVML and LibreHardwareMonitorLib.
   - Graceful fallback ensures unprivileged sensors report `N/A` cleanly without crashing the process.
4. **Smart App Control (SAC) Transparency**: Windows 11 SAC enforces reputation or Developer Mode on newly compiled binaries. Clearly documented in user guides.

---

## 5. Source File Manifest

- `src/WinTaskbarMonitor/`
  - `WinTaskbarMonitor.csproj`: Project specification with .NET 8, WPF, and LibreHardwareMonitorLib.
  - `app.manifest`: Per-monitor v2 DPI awareness and Windows 11 compatibility.
  - `App.xaml` / `App.xaml.cs`: Application entry point with single-instance mutex and global exception handlers.
  - `MainWindow.xaml` / `MainWindow.xaml.cs`: Main taskbar-anchored widget view, layout, and context menu.
  - `Config/`
    - `AppSettings.cs`: Application settings model with enums and defaults.
    - `ISettingsService.cs` / `SettingsService.cs`: Settings persistence to `%APPDATA%\WinTaskbarMonitor\settings.json` and startup registry.
  - `Monitoring/`
    - `HardwareSnapshot.cs`: Consolidated telemetry record.
    - `CpuMonitorProvider.cs`: Win32 `GetSystemTimes` implementation.
    - `MemoryMonitorProvider.cs`: Win32 `GlobalMemoryStatusEx` implementation.
    - `NvmlInterop.cs`: Native P/Invoke signatures for NVIDIA Management Library.
    - `GpuMonitorProvider.cs`: Multi-GPU provider utilizing NVML and LibreHardwareMonitor.
    - `CpuTemperatureProvider.cs`: CPU thermal provider with WMI/ACPI fallback.
    - `HardwareMonitorCoordinator.cs`: Background asynchronous polling coordinator.
  - `Taskbar/`
    - `TaskbarAnchorEngine.cs`: AppBar tracker, TrayNotifyWnd alignment, DPI scaler, and Explorer restart recovery.
  - `UI/`
    - `ThemeManager.cs`: Windows theme tracker and Fluent color provider.
    - `MainWidgetViewModel.cs`: ViewModel for the taskbar widget with threshold colors and tooltips.
    - `SettingsViewModel.cs`: ViewModel for settings dialog.
    - `SettingsWindow.xaml` / `SettingsWindow.xaml.cs`: Settings configuration dialog.
- `tests/WinTaskbarMonitor.Tests/`
  - `MonitoringTests.cs`: Unit tests for CPU, RAM, GPU, temperature providers, and coordinator.
  - `SettingsTests.cs`: Unit tests for settings defaults, serialization, and cloning.
- `dist/`
  - `WinTaskbarMonitor.exe`: Single-file standalone executable (3.7 MB).

---

## 6. Next Steps & Final Handover

All development phases and acceptance criteria are complete. Prepare final commit and present the comprehensive implementation report.
