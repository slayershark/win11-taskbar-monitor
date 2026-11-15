# TASK_PLAN.md — Implementation Roadmap

This document is the primary progress tracker for the WinTaskbarMonitor project.  
Tasks are divided into logical phases with measurable verification criteria.

Status Indicators:
- `[ ]` Not started
- `[~]` In progress
- `[x]` Completed and verified
- `[!]` Blocked or requires attention

---

## Phase 1: Project Assessment & Technical Research
- [x] **Task 1.1: System Environment & Host Tooling Inspection**
  - *Description*: Inspect host OS, available .NET runtimes/SDKs, compilers, git status, and hardware components (CPU, iGPU, dGPU).
  - *Expected Outcome*: Detailed understanding of available toolchains and target machine hardware.
  - *Dependencies*: None.
  - *Verification Requirements*: Terminal commands verify Windows 11 build, .NET runtimes, git identity, and Intel iGPU + NVIDIA MX350 presence.
  - *Blockers*: None.
- [x] **Task 1.2: Local Development Environment Setup (.NET 8 SDK & Git Identity)**
  - *Description*: Install .NET 8 SDK into user space, set environment paths, initialize Git repository, and configure repo-level identity (`slayershark` / `sandeepsuwal9@gmail.com`).
  - *Expected Outcome*: Working `dotnet` CLI with .NET 8 SDK and clean local Git repository.
  - *Dependencies*: Task 1.1.
  - *Verification Requirements*: `dotnet --info` outputs SDK 8.0.428; `git config --local -l` shows correct user name and email.
  - *Blockers*: None.
- [x] **Task 1.3: Foundation Documentation Setup**
  - *Description*: Author mandatory documentation suite (`AGENTS.md`, `PROJECT_BRIEF.md`, `ARCHITECTURE.md`, `TASK_PLAN.md`, `CONTEXT.md`, `CHANGELOG.md`, `TEST_PLAN.md`, `.gitignore`).
  - *Expected Outcome*: Complete documentation foundation establishing technical rules, requirements, architecture, and testing plans prior to application code.
  - *Dependencies*: Task 1.1, Task 1.2.
  - *Verification Requirements*: All 8 documentation files exist, follow guidelines, and are cross-referenced.
  - *Blockers*: None.

---

## Phase 2: Requirements & Architecture Finalisation
- [x] **Task 2.1: Finalise Data Source Specifications for CPU, GPU, RAM, and Temperatures**
  - *Description*: Detail P/Invoke signatures and fallback ladders for hardware queries without administrative escalation.
  - *Expected Outcome*: Documented hardware collection methods in `ARCHITECTURE.md`.
  - *Dependencies*: Phase 1.
  - *Verification Requirements*: Validated against available Windows 11 APIs and NVML library availability.
  - *Blockers*: None.
- [x] **Task 2.2: Taskbar Anchoring Architecture Finalisation**
  - *Description*: Specify the floating overlay window architecture using Win32 AppBar messages and Shell tray hooks to achieve native taskbar look without DeskBand.
  - *Expected Outcome*: Fully detailed anchoring algorithm in `ARCHITECTURE.md`.
  - *Dependencies*: Phase 1.
  - *Verification Requirements*: Mathematical coordinate alignment and DPI scaling formulas defined.
  - *Blockers*: None.

---

## Phase 3: Project Scaffold & Core Infrastructure
- [ ] **Task 3.1: Create .NET 8 WPF Project Structure**
  - *Description*: Scaffold `WinTaskbarMonitor.csproj` with modern C# 12, nullable reference types, Windows 11 manifest (per-monitor DPI v2 awareness), and folder structure.
  - *Expected Outcome*: Clean compilable project structure matching the modular architecture.
  - *Dependencies*: Phase 2.
  - *Verification Requirements*: `dotnet build` succeeds with zero errors and zero warnings.
  - *Blockers*: None.
- [ ] **Task 3.2: Configuration & Persistence Engine**
  - *Description*: Implement `AppSettings` data model, `ISettingsService`, and JSON serialization targeting `%APPDATA%\WinTaskbarMonitor\settings.json`.
  - *Expected Outcome*: Robust settings manager with defaults, save/load, and error resilience.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Unit/integration test verifies loading, modifying, and restoring settings.
  - *Blockers*: None.

---

## Phase 4: Hardware Monitoring Implementations
- [ ] **Task 4.1: CPU Monitoring Provider (Win32 GetSystemTimes)**
  - *Description*: Implement high-performance, zero-allocation CPU usage calculator using kernel32 `GetSystemTimes`.
  - *Expected Outcome*: Accurate total CPU utilisation updated every tick.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Verified against Windows Task Manager CPU percentage (within ±2%).
  - *Blockers*: None.
- [ ] **Task 4.2: RAM Monitoring Provider (Win32 GlobalMemoryStatusEx)**
  - *Description*: Implement memory provider reporting total RAM, available RAM, used RAM (in GB), and usage percentage.
  - *Expected Outcome*: Exact RAM metrics matching Task Manager memory performance tab.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Matches `Get-CimInstance Win32_OperatingSystem` within 0.1 GB.
  - *Blockers*: None.
- [ ] **Task 4.3: GPU Monitoring Provider (NVML + Windows Performance Counters / PDH)**
  - *Description*: Implement GPU provider supporting both discrete (NVIDIA NVML) and integrated (Intel Iris Xe via PDH / DXGI) graphics adapters with multi-GPU detection.
  - *Expected Outcome*: Real-time GPU utilisation for user-selected or auto-detected GPU.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Verified on host machine containing both Intel Iris Xe and NVIDIA MX350.
  - *Blockers*: None.
- [ ] **Task 4.4: Temperature Monitoring Provider (CPU & GPU)**
  - *Description*: Implement CPU and GPU temperature query engine with graceful `N/A` fallback when sensors are inaccessible.
  - *Expected Outcome*: Real-time °C/°F temperatures for CPU and GPU, or clean `N/A` without exceptions or crashes.
  - *Dependencies*: Task 4.1, Task 4.3.
  - *Verification Requirements*: Validated with NVML for NVIDIA GPU and sensor library for CPU; handles non-admin mode safely.
  - *Blockers*: None.
- [ ] **Task 4.5: Hardware Monitoring Coordinator**
  - *Description*: Implement background timer service (`PeriodicTimer`) that polls providers asynchronously and emits consolidated `HardwareSnapshot`.
  - *Expected Outcome*: Decoupled background telemetry pipeline running at configurable intervals (500ms, 1s, 2s, 5s).
  - *Dependencies*: Tasks 4.1 - 4.4.
  - *Verification Requirements*: Timer ticks smoothly, CPU usage stays below 0.5%.
  - *Blockers*: None.

---

## Phase 5: Windows 11 Taskbar Integration Engine
- [ ] **Task 5.1: Win32 Taskbar Geometry & AppBar Tracker**
  - *Description*: Implement `TaskbarAnchorEngine` to track taskbar position (`Shell_TrayWnd`), tray area (`TrayNotifyWnd`), screen bounds, and taskbar orientation.
  - *Expected Outcome*: Accurate screen coordinates for anchoring widget flush with taskbar elements.
  - *Dependencies*: Task 3.1.
  - *Verification Requirements*: Coordinates match taskbar rectangle at standard and custom resolutions.
  - *Blockers*: None.
- [ ] **Task 5.2: Per-Monitor DPI Awareness & Scale Handling**
  - *Description*: Handle DPI scale changes (`WM_DPICHANGED`, `WM_DISPLAYCHANGE`) dynamically to ensure sharp, properly sized text.
  - *Expected Outcome*: Widget scales smoothly without blurriness or layout distortion.
  - *Dependencies*: Task 5.1.
  - *Verification Requirements*: Tested at 100%, 125%, 150% DPI scale factors.
  - *Blockers*: None.
- [ ] **Task 5.3: Shell Events, Auto-Hide, & Explorer Restart Recovery**
  - *Description*: Register for `"TaskbarCreated"` window message and monitor taskbar auto-hide state (`ABM_GETSTATE`).
  - *Expected Outcome*: Widget survives Explorer crash/restart and follows taskbar auto-hide correctly.
  - *Dependencies*: Task 5.1.
  - *Verification Requirements*: Simulating `taskkill /f /im explorer.exe & start explorer.exe` recovers the widget position automatically within 2 seconds.
  - *Blockers*: None.

---

## Phase 6: UI/UX & Visual Implementation
- [ ] **Task 6.1: Main Taskbar Widget View & Tabular Numerals**
  - *Description*: Create lightweight, borderless WPF overlay window using Segoe UI Variable, tabular numbers, subtle separators, and fixed-width slots.
  - *Expected Outcome*: Jitter-free, clean taskbar widget showing `CPU 24% | GPU 18% | CPU 52°C | GPU 46°C | RAM 39%`.
  - *Dependencies*: Phase 4, Phase 5.
  - *Verification Requirements*: No horizontal shift or jitter when values transition across 1, 2, or 3 digits.
  - *Blockers*: None.
- [ ] **Task 6.2: Theme Engine (System, Dark, Light)**
  - *Description*: Implement theme manager that tracks Windows app light/dark theme registry (`AppsUseLightTheme`) and applies Fluent palette colors with high contrast.
  - *Expected Outcome*: Flawless visual harmony with Windows 11 dark and light taskbars.
  - *Dependencies*: Task 6.1.
  - *Verification Requirements*: Toggling Windows Dark/Light mode in Settings immediately adapts widget colors.
  - *Blockers*: None.
- [ ] **Task 6.3: Threshold Accents & Tooltip Details**
  - *Description*: Add subtle visual alerts for elevated (>75%) and critical (≥90%) loads, plus detailed hover tooltips for all metrics.
  - *Expected Outcome*: Informative tooltips on mouse hover without cluttering the main taskbar bar.
  - *Dependencies*: Task 6.1.
  - *Verification Requirements*: Tooltips display CPU model, core count, GPU model, memory details on hover.
  - *Blockers*: None.
- [ ] **Task 6.4: System Tray Icon & Context Menu**
  - *Description*: Implement notification tray icon with comprehensive context menu (quick theme, refresh rate, metrics toggle, settings, exit).
  - *Expected Outcome*: Convenient quick-access menu accessible from both taskbar widget right-click and system tray.
  - *Dependencies*: Task 6.1, Task 3.2.
  - *Verification Requirements*: All context menu actions function properly.
  - *Blockers*: None.

---

## Phase 7: Settings & Configuration Interface
- [ ] **Task 7.1: Dedicated Settings Window**
  - *Description*: Build a clean, Fluent-styled Settings Window with tabs/sections for Display, Metrics, Hardware, and Behavior.
  - *Expected Outcome*: User-friendly configuration UI allowing selection of GPUs, display toggles, font size, and temperature units.
  - *Dependencies*: Task 3.2, Phase 6.
  - *Verification Requirements*: All modifications update the live widget and persist to disk.
  - *Blockers*: None.
- [ ] **Task 7.2: Windows Startup Integration**
  - *Description*: Implement "Start with Windows" toggle utilizing `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
  - *Expected Outcome*: Reliable startup registration without requiring administrator elevation.
  - *Dependencies*: Task 7.1.
  - *Verification Requirements*: Registry key verified, clean removal when untoggled.
  - *Blockers*: None.

---

## Phase 8: Performance Optimisation
- [ ] **Task 8.1: CPU & Memory Profiling**
  - *Description*: Measure and optimize hot paths in polling loop and UI dispatcher updates to ensure strict adherence to resource budgets (< 0.5% CPU, < 40 MB RAM).
  - *Expected Outcome*: Verified low-overhead operation across all refresh rates (500ms, 1s, 2s, 5s).
  - *Dependencies*: Phases 4, 5, 6.
  - *Verification Requirements*: Performance measurements recorded in `TEST_PLAN.md`.
  - *Blockers*: None.

---

## Phase 9: Reliability, Compatibility & Edge Case Testing
- [ ] **Task 9.1: Multi-GPU and Unsupported Sensor Resilience**
  - *Description*: Test switching between Intel Iris Xe and NVIDIA MX350, disabling sensors, and verifying non-admin graceful fallback (`N/A`).
  - *Expected Outcome*: Zero exceptions, smooth fallback.
  - *Dependencies*: Phase 4.
  - *Verification Requirements*: Passing all test cases in `TEST_PLAN.md`.
  - *Blockers*: None.
- [ ] **Task 9.2: Multi-Monitor, DPI Scaling & Fullscreen Detection**
  - *Description*: Verify behavior during fullscreen games/apps, secondary monitors, and DPI scale adjustments.
  - *Expected Outcome*: Clean overlay behavior that does not interfere with fullscreen applications.
  - *Dependencies*: Phase 5.
  - *Verification Requirements*: Confirmed via automated and manual test scenarios.
  - *Blockers*: None.

---

## Phase 10: Build, Packaging & Distribution
- [ ] **Task 10.1: Standalone Single-File Release Build**
  - *Description*: Configure release publishing profile for trimmed, self-contained or framework-dependent x64 single-file executable.
  - *Expected Outcome*: Portable `WinTaskbarMonitor.exe` ready for instant execution without installer.
  - *Dependencies*: All previous phases.
  - *Verification Requirements*: Release executable runs standalone on a clean Windows 11 environment.
  - *Blockers*: None.
- [ ] **Task 10.2: Documentation and User Guide Finalisation**
  - *Description*: Update `README.md`, build guides, installation and uninstallation instructions.
  - *Expected Outcome*: Clear, comprehensive user and developer documentation.
  - *Dependencies*: Task 10.1.
  - *Verification Requirements*: Documentation audit matches final application functionality.
  - *Blockers*: None.

---

## Phase 11: Final QA & Completion Audit
- [ ] **Task 11.1: Final Acceptance Criteria Verification**
  - *Description*: Comprehensive review against every requirement in Section 12 of `PROJECT_BRIEF.md`.
  - *Expected Outcome*: All acceptance criteria validated and documented in `TEST_PLAN.md` and `CONTEXT.md`.
  - *Dependencies*: Task 10.2.
  - *Verification Requirements*: 100% checklist completion.
  - *Blockers*: None.
- [ ] **Task 11.2: Final Implementation Report**
  - *Description*: Produce comprehensive final report detailing achievements, architectural decisions, performance benchmarks, and repository state.
  - *Expected Outcome*: Final project completion signoff.
  - *Dependencies*: Task 11.1.
  - *Verification Requirements*: Complete summary presented to user.
  - *Blockers*: None.
