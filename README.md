# Win11TaskbarMonitor

**Lightweight Windows 11 Taskbar System Monitor**

Win11TaskbarMonitor is a native, ultra-lightweight Windows 11 desktop utility designed to display essential real-time hardware telemetry directly on or alongside your taskbar.

---

## Features

- **Essential Hardware Telemetry at a Glance**:
  - **CPU Utilisation**: Accurate real-time total CPU percentage.
  - **GPU Utilisation**: Overall GPU usage (supports NVIDIA, AMD, and Intel integrated/discrete GPUs).
  - **CPU & GPU Temperatures**: Clean °C / °F readouts with smart auto-detection for supported sensors.
  - **RAM Utilisation**: Real-time memory percentage, used GB, and total capacity.
- **Native Windows 11 Experience**:
  - Docked directly on the left side of the Windows 11 taskbar with smart equal margins.
  - Segoe UI Variable typography with **tabular numbers** (zero horizontal text shifting).
  - Native Windows 11 dark, light, and automatic system theme integration.
  - Per-monitor DPI awareness and multi-monitor support.
- **Ultra-Lightweight & Performant**:
  - Built with modern C# and .NET 8 on WindowsDesktop.
  - Near-zero CPU overhead (< 0.5% CPU average).
  - Tiny memory footprint (< 40 MB working set).
  - Zero web-view bloat (no Electron, no Chromium).
  - 100% offline, zero telemetry, zero background services.
- **Smart Taskbar Anchor**:
  - Automatically recovers position when Windows Explorer restarts.
  - Follows taskbar auto-hide and resolution changes.
  - Doesn't steal focus when clicked (`WS_EX_NOACTIVATE`) and doesn't clutter Alt+Tab (`WS_EX_TOOLWINDOW`).

---

## Architecture & Project Documentation

This project follows a strict **Documentation-First Workflow**:

- [AGENTS.md](AGENTS.md) — Coding conventions, architectural rules, and development guidelines.
- [PROJECT_BRIEF.md](PROJECT_BRIEF.md) — Full product requirements and acceptance criteria.
- [ARCHITECTURE.md](ARCHITECTURE.md) — Deep technical architecture, P/Invoke signatures, and design decisions.
- [TASK_PLAN.md](TASK_PLAN.md) — Phase-by-phase implementation progress tracker.
- [CONTEXT.md](CONTEXT.md) — Persistent development state, branch status, and environment notes.
- [CHANGELOG.md](CHANGELOG.md) — Chronological history of changes.
- [TEST_PLAN.md](TEST_PLAN.md) — Comprehensive QA checklist, test procedures, and performance benchmarks.

---

## Installation & Removal

### Graphical Setup Wizard (Recommended)
1. Download **`Win11TaskbarMonitor-Setup.exe`** from [Releases](https://github.com/slayershark/win11-taskbar-monitor/releases).
2. Double-click to launch the Windows 11 Fluent Setup Wizard.
3. Configure your preferences:
   * **Custom Destination Folder** (defaults to user-space `%LOCALAPPDATA%\Programs\Win11TaskbarMonitor` with zero UAC elevation required).
   * **Shortcuts**: Optional Desktop and Start Menu shortcuts.
   * **Startup**: Toggle automatic launch on Windows login.
   * **Hardware Preferences**: Choose default GPU telemetry source (Auto / NVIDIA / Intel) and temperature unit (°C / °F).
4. Watch real-time extraction progress and click **Finish** to immediately enjoy the taskbar monitor.

### Portable / Script-Based Install
- **Portable ZIP**: Download `Win11TaskbarMonitor-Setup.zip`, extract, and double-click `Run-Portable.cmd` (or `Win11TaskbarMonitor.exe`).
- **1-Click Script**: Double-click `Install.cmd` in the extracted package.

### Uninstallation
- **Via Windows Settings**: Open Windows **Settings > Apps > Installed apps**, search for **Win11TaskbarMonitor**, and click **Uninstall**. The dedicated uninstaller window will launch with an option to purge configuration settings.
- **Via Uninstaller Binary**: Run `Uninstall.exe` located in the application directory.

---

## Prerequisites & Building

- **Operating System**: Windows 11 (64-bit)
- **SDK**: .NET 8 SDK (or later)

### Build from Source
```powershell
dotnet build -c Release
```

### Run Standalone
```powershell
dotnet run -c Release
```

### Publish Single-File Executable
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## License

MIT License. Designed with focus, simplicity, and performance.
