<p align="center">
  <img src="app_gui.png" width="600">
</p>

Kinect Head Tracker v2.1 is a **modern, stability-first rewrite** of the classic Kinect head-tracking concept, designed for **long-running simulation use** and **serious rigs**.

This project began as a fork of the original Magic Mau's Kinect Head Tracker, but has since evolved into a **fully streamlined, hardened application** with a strong focus on reliability, safety, and usability.

---

## Why v2.1?

The original tracker proved that Kinect head tracking was possible — but it also carried years of technical debt:
- Unused and legacy code paths
- Memory instability over long sessions
- Fragile recovery when tracking was lost
- Limited quality-of-life features
- Unclear and finicky UI options

**v2.1 fixes that foundation.**

This is not a feature-piled rewrite — it is a **carefully engineered rebuild** that prioritizes correctness, resilience, and clarity.

---

## Core Design Principles

### 🧠 Stability First
Built to run for hours without degradation. No creeping memory usage, no gradual failure modes.

### 🔁 Graceful Recovery
If face tracking is lost, the system recovers automatically — no restarts, no lockups, no frozen output.

### 🧱 Clean Architecture
Tracking logic is fully isolated from networking and transport layers, keeping the core engine predictable and safe.

### 🛡 Memory Safety
Safe managed/unmanaged image handling eliminates common Kinect crashes and GDI+ failures seen in older implementations.

---

## Features

### 🎯 Accurate Head Tracking
- Smooth, consistent pose output
- Designed for simulation and camera-driven environments

### 🌐 Optional Network Streaming
- UDP output for use with OpenTrack or custom pipelines
- Explicit start/stop control (never forced)

### ⚙️ Full Settings System (v2.1)
- Run on Windows startup (Task Scheduler)
- Auto-start tracking engine
- Auto-start UDP streaming (starts only after tracking data is flowing)
- Persistent window position
- Configurable network target (IP/port) for advanced setups
- Sensible defaults with advanced control available

> **Note on “Run at startup”:** this setting uses **Windows Task Scheduler**.  
> On some systems, creating/updating the scheduled task may require running the app **once as Administrator**.  
> If the checkbox doesn’t appear to “stick,” right-click the app → **Run as Administrator**, toggle it again, then reboot/log off to verify.

### 🖥 Polished Desktop UI
- Minimal, focused interface
- Clear engine and streaming state
- Designed to stay out of your way during use
- Graceful “Disconnected” handling when no Kinect is attached (no modal error spam)

---

## Built for Long Sessions

This tracker is intended for:
- Driving simulators
- Flight simulators
- Virtual camera rigs
- Dedicated head-tracking machines

It has been tested under **extended continuous operation**, with safeguards specifically designed to prevent the slow failures common in older Kinect tools.

---

## Who Is This For?

If you want:
- A **set-and-forget** Kinect head tracker
- Something that doesn’t require babysitting
- Clean code you can understand and extend, or just build and use
- A tool that behaves like professional software

This is for you.

---

## License & Credits

This project is a fork and evolution of the original Magic Mau Kinect Head Tracker.
Significant architectural, stability, and usability improvements have been made.

---

## Building from Source

If you want to build the project yourself (dependencies, SDK links, MSBuild/dotnet commands),
see:

👉 [build_readme.md](build_readme.md)

## Precompiled Installer

A precompiled, code-signed Windows installer will be available for convenience at:

👉 https://www.nickdodd.com/downloads/kinect-head-tracker

This installer is optional and provided for users who prefer a ready-to-run build.
The full source code remains available in this repository.

---

Enjoy stable Kinect V1 head tracking!
