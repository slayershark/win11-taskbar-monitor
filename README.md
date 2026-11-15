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

### Quick Install (One-Click)
- **Option 1**: Double-click [install.cmd](install.cmd) in the project root.
- **Option 2**: Run PowerShell:
  ```powershell
  .\scripts\install.ps1 -CreateDesktopShortcut -StartWithWindows
  ```

This cleanly installs Win11TaskbarMonitor to `%LOCALAPPDATA%\Programs\Win11TaskbarMonitor`, creates a Windows Start Menu shortcut, and registers the application in Windows **Installed Apps / Add or Remove Programs**.

### Uninstallation
- **Via Windows Settings**: Open Windows **Settings > Apps > Installed apps**, search for **Win11TaskbarMonitor**, and click **Uninstall**.
- **Via Script**: Double-click [uninstall.cmd](uninstall.cmd) or run:
  ```powershell
  .\scripts\uninstall.ps1
  ```

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
