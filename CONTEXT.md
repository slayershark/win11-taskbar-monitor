# CONTEXT.md — Persistent Development Context

## 1. Project Objectives & Current Status

**Project**: WinTaskbarMonitor  
**Objective**: Build a lightweight, native, and highly reliable Windows 11 taskbar hardware telemetry utility (CPU, GPU, CPU Temp, GPU Temp, RAM) without bloat, web dependencies, or excessive resource overhead.  
**Current Milestone**: Phase 1 & Phase 2 Complete (Assessment, Tooling Setup, Architecture, and Documentation Foundation finalized). Phase 3 (Project Scaffold & Core Infrastructure) ready to begin.

---

## 2. Environment & Repository Status

- **Operating System**: Windows 11 Build 26300 (x64)
- **CPU**: 11th Gen Intel(R) Core(TM) i5-1135G7 (4 cores, 8 threads)
- **GPUs Detected**:
  1. Intel(R) Iris(R) Xe Graphics (Integrated)
  2. NVIDIA GeForce MX350 (Discrete)
- **Runtime & SDK**: .NET 8 SDK 8.0.428 (installed in `%LOCALAPPDATA%\dotnet`, environment variables configured: `DOTNET_ROOT` and User `Path`).
- **Git Repository**: Initialized locally in `d:\WinTaskbarMonitor`.
  - Identity configured at repo level:
    - `user.name`: `slayershark`
    - `user.email`: `sandeepsuwal9@gmail.com`
  - Current Branch: `main` (or default local branch)
  - Remote: None currently configured.

---

## 3. Completed Work

- [x] Initial host environment and hardware assessment completed.
- [x] .NET 8 SDK (8.0.428) installed in user directory and tested.
- [x] Local Git repository initialized and configured with mandated username and email.
- [x] Created comprehensive documentation foundation:
  - `AGENTS.md`: Permanent coding and architecture guidelines.
  - `PROJECT_BRIEF.md`: Complete functional and non-functional requirements.
  - `ARCHITECTURE.md`: In-depth architecture specification and technical decisions.
  - `TASK_PLAN.md`: 11-phase implementation roadmap with verification criteria.
  - `CONTEXT.md`: Persistent session context.
  - `CHANGELOG.md`: Chronological development log.
  - `TEST_PLAN.md`: Detailed testing and quality assurance plan.
  - `.gitignore`: Configured for .NET 8, C#, Visual Studio, and Windows builds.

---

## 4. Current Architecture Decisions

1. **WPF on .NET 8 LTS**: Chosen over WinUI 3 (too heavy) and WinForms (inferior DirectWrite typography and Fluent styling). Provides built-in hardware acceleration and sub-second startup.
2. **Taskbar-Anchored Floating Tool Window**: Avoids fragile Explorer DLL injection and non-functional DeskBand APIs. Uses Win32 AppBar hooks (`SHAppBarMessage`), `WS_EX_TOOLWINDOW`, and `WS_EX_NOACTIVATE` to remain seamlessly docked next to the system tray.
3. **Multi-Source Hardware Telemetry**:
   - CPU: Win32 `GetSystemTimes` (zero-allocation, microseconds execution time).
   - RAM: Win32 `GlobalMemoryStatusEx`.
   - GPU: Dual-pipeline supporting NVIDIA NVML (`nvml.dll`) and Windows Performance Counters (`GPU Engine(*)\Utilization Percentage` via PDH) for Intel and AMD.
   - Temperature: Direct NVML for NVIDIA GPUs; fallback to LibreHardwareMonitorLib for CPU and other GPUs. Gracefully outputs `N/A` when unprivileged access prevents sensor reading.

---

## 5. Source File Manifest

*(Will expand as implementation proceeds in Phase 3)*

- `.gitignore` — Git exclusion rules for .NET and Windows development.
- `AGENTS.md` — Permanent development rules and architectural constraints.
- `PROJECT_BRIEF.md` — Product requirements document and acceptance criteria.
- `ARCHITECTURE.md` — Technical design, data flow, and API contracts.
- `TASK_PLAN.md` — Phase-by-phase implementation progress tracker.
- `CONTEXT.md` — This persistent context document.
- `CHANGELOG.md` — Chronological record of meaningful project changes.
- `TEST_PLAN.md` — Test cases, acceptance verification, and benchmark logs.

---

## 6. Known Limitations & Potential Challenges

- **CPU Temperature in Standard User Mode**: On modern Intel/AMD platforms, reading CPU MSRs (Model-Specific Registers) requires ring-0 driver access. Running in standard user mode without elevation may prevent CPU temperature reading on some motherboards. The system will gracefully show `N/A` rather than crashing or forcing UAC elevation.
- **NVIDIA Laptop dGPU Sleep State**: The MX350 may enter deep sleep state (D3Cold) when idle on battery to save power. Polling temperature must not continuously wake up the dGPU unnecessarily; when dGPU is in low-power sleep, it will report 0% utilisation and cached or idle temperature.

---

## 7. Next Recommended Development Task

**Task 3.1: Create .NET 8 WPF Project Structure** (`WinTaskbarMonitor.csproj`, manifest, core namespaces, and initial build verification).
