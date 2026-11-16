# PROJECT_BRIEF.md — Product Requirements Document

## 1. Project Overview

**Win11TaskbarMonitor** is a lightweight, dedicated Windows 11 desktop utility designed to monitor critical hardware parameters in real time directly from or alongside the Windows taskbar. 

Inspired by minimalist taskbar telemetry utilities, Win11TaskbarMonitor strips away superfluous UI bloat, heavy web-view dependencies (Electron/Chromium), unnecessary cloud synchronisation, and heavy background overhead, delivering an instantaneous, at-a-glance hardware telemetry experience.

---

## 2. Objectives and Intended User Experience

- **At-a-Glance Telemetry**: View CPU load, GPU load, CPU temperature, GPU temperature, and RAM usage without opening Task Manager or switching application focus.
- **Taskbar Native Feel**: Seamlessly integrates into the Windows 11 taskbar landscape, appearing as though it is an authentic component of the operating system.
- **Zero Distraction**: Fixed-width tabular digits prevent visual jitter or jumping text. Clean typography (Segoe UI Variable) with subtle visual cues.
- **Standard User Friendly**: Operates reliably in standard (non-elevated) Windows user mode. Gracefully reports `N/A` for protected sensors without crashing or nagging users for elevation.
- **Minimal Resource Footprint**: Consumes negligible system resources (< 0.5% CPU, < 40 MB RAM) to ensure monitoring does not affect gaming, development, or creative workflows.

---

## 3. Functional Requirements

### 3.1 Hardware Telemetry
1. **CPU Usage**:
   - Total system CPU utilisation in percent (`0% - 100%`).
   - Sourced via high-resolution Win32 system times (`GetSystemTimes` / PDH).
2. **GPU Usage**:
   - Aggregate GPU utilisation percent (`0% - 100%`).
   - Sourced via DXGI / D3D performance counters (`GPU Engine(*)\Utilization Percentage`) and NVML for NVIDIA GPUs.
   - Detects all available GPUs (Integrated + Discrete) and allows user to select the monitored GPU or auto-select the active discrete adapter.
3. **CPU Temperature**:
   - Sourced in Celsius (°C), with optional Fahrenheit (°F) display toggle.
   - Sourced via hardware sensor provider (LibreHardwareMonitorLib / ACPI / WMI).
   - If inaccessible without administrative ring-0 driver, display `N/A` cleanly.
4. **GPU Temperature**:
   - Sourced in Celsius (°C), with optional Fahrenheit (°F) display toggle.
   - Direct NVML query for NVIDIA GPUs (`nvmlDeviceGetTemperature`), ADL for AMD, or fallback sensor library.
   - If sensor is absent or inactive (e.g., dGPU powered down on laptops), display `N/A` or idle status.
5. **RAM Usage**:
   - Memory utilisation percentage (`0% - 100%`).
   - Formatted readout displaying used memory and total physical memory (e.g., `12.4 / 32 GB · 39%` or `12.4 GB (39%)`).
   - Sourced via `GlobalMemoryStatusEx`.

### 3.2 Display & Formatting
- Individual metric toggle (enable/disable CPU, GPU, CPU Temp, GPU Temp, RAM).
- Reorderable or clean standard horizontal layout:
  `CPU 24%  |  GPU 18%  |  CPU 52°C  |  GPU 46°C  |  RAM 39%`
- Tabular numerals (`FontNumeralAlignment.Tabular`) to guarantee jitter-free readouts.
- Threshold-based color accents:
  - Normal (< 75%): Default theme text color.
  - Elevated (75% - 89%): Amber / warning accent.
  - Critical (≥ 90%): Coral / soft red alert accent.

### 3.3 User Interaction & Settings
- **Hover Tooltips**: Detailed breakdown per metric on mouse hover:
  - CPU tooltip: Model name, logical cores, exact utilisation.
  - GPU tooltip: Adapter name, driver version, VRAM usage (if available), temperature.
  - RAM tooltip: Used GB, Free GB, Total installed GB.
- **Context Menu** (Right-click):
  - Refresh Rate selector (500ms, 1s, 2s, 5s).
  - Temperature unit toggle (°C / °F).
  - Monitored GPU selector (detects all GPUs).
  - Metric visibility sub-menu.
  - Theme mode toggle (System, Dark, Light).
  - Open Settings / About window.
  - Exit application.
- **Settings Dialog**:
  - Full configuration GUI for appearance, positioning, startup, and sensor sources.
  - Settings persisted automatically to local JSON (`%APPDATA%\WinTaskbarMonitor\settings.json`).

### 3.4 Windows 11 Taskbar Integration
- Since Windows 11 removed the legacy DeskBand API, the application must use a robust **Taskbar-Anchored Window**:
  - High-precision AppBar tracker monitoring the Windows 11 Taskbar (`Shell_TrayWnd`).
  - Anchors dynamically alongside the system notification area (`TrayNotifyWnd` / `Taskbar.ModernNotifyIconOverflowWindow`) or user-configured screen edge.
  - Detects taskbar resolution changes, multi-monitor configuration, DPI scale adjustments, and taskbar auto-hide events (`ABM_GETTASKBARPOS`).
  - Auto-recovers position if Windows Explorer restarts.
  - `WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`, and topmost positioning to prevent taking focus or appearing in Alt+Tab.

---

## 4. Non-Functional Requirements

- **Performance**:
  - CPU usage: Average < 0.5% at 1-second refresh rate.
  - Working set RAM: < 40 MB.
  - Startup time: Cold start < 1.0 second.
- **Reliability**:
  - Zero application crashes on hardware sensor exceptions or missing drivers.
  - Thread safety: Background timer ticks marshal safely to UI dispatchers.
  - Fault tolerance: If a sensor fails, retry with exponential backoff or maintain `N/A` without freezing the process.
- **Security & Privacy**:
  - 100% offline operation: No internet connection requests, no sockets, no telemetry.
  - Operates under standard user rights (LUA / Least-privilege User Account).
- **Deployment**:
  - Standalone x64 executable or lightweight portable folder.
  - No required runtime installers beyond standard .NET Desktop Runtime (or framework-dependent / self-contained single-file).

---

## 5. Excluded Features (Out of Scope)

To maintain extreme lightness and focus, the following are strictly excluded:
- Network activity / bandwidth graphs or download/upload meters.
- Disk read/write throughput meters.
- Heavy historical graphing engines or multi-hour logging databases.
- Web-based dashboards, cloud syncing, remote mobile monitoring.
- Overclocking, fan speed control, or voltage tweaking.
- Game overlays (DirectX/Vulkan OSD hooks).

---

## 6. Final Acceptance Criteria

- [ ] Runs smoothly on 64-bit Windows 11.
- [ ] Displays accurate CPU, GPU, and RAM utilisation matching Task Manager.
- [ ] Displays genuine CPU and GPU temperatures when sensors are queryable, or cleanly displays `N/A`.
- [ ] Never crashes when encountering unsupported hardware or non-elevated user permissions.
- [ ] Stays anchored to the Windows 11 taskbar across DPI scales, screen resolutions, and Explorer restarts.
- [ ] Respects system, dark, and light themes with Segoe UI Variable tabular numerals.
- [ ] Settings persist reliably across application restarts.
- [ ] Validated performance footprint: < 0.5% average CPU overhead and < 40 MB RAM.
