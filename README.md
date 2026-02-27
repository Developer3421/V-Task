<div align="center">

<img src="V-Task/Icons/AppIcon.png" alt="V-Task Logo" width="120"/>

# V-Task — Resource Monitor

**A lightweight, modern system resource monitor for Windows built with Avalonia UI**

[![Platform](https://img.shields.io/badge/platform-Windows-blue?logo=windows)](https://github.com)
[![Framework](https://img.shields.io/badge/framework-.NET%2010-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-Avalonia%2011.3-orange)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

</div>

---

## 📸 Overview

**V-Task** is a sleek and efficient system resource monitor that gives you real-time insight into your PC's performance — all in one beautiful, warm-toned interface. No bloat, no ads, no tracking.

---

## ✨ Features

| Feature | Details |
|---|---|
| 🧠 **CPU Monitoring** | Usage percentage, processor name, physical & logical core count |
| 💾 **Memory** | RAM usage, available memory, swap/page file details, frequency, type & slots |
| 🎮 **GPU** | Video memory usage, GPU name, driver version, interface info |
| 💿 **Disk** | Real-time disk activity and utilization |
| 🌐 **Network** | Live download/upload speeds, total data transferred, Wi-Fi & Ethernet status |
| 💻 **System** | System uptime, configurable refresh rate |
| 🌍 **Multi-language** | English, Українська, Deutsch, Türkçe, Русский |
| 🔒 **Privacy-first** | Zero telemetry, zero network calls — everything stays on your device |

---

## 🖥️ Interface

- **Dashboard** — All key metrics at a glance on one clean screen
- **Memory Panel** — Detailed RAM and virtual memory breakdown
- **GPU Panel** — Comprehensive graphics card monitoring

Modern warm-toned design with smooth Fluent UI styling.

---

## 🚀 Getting Started

### Requirements
- Windows 10 or later
- .NET 10 Runtime
 
## Download
Microsoft Store: https://apps.microsoft.com/detail/9P405177WBX9

## 🏗️ Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | [Avalonia UI 11.3](https://avaloniaui.net/) |
| Hardware readings | [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) |
| Local storage | [LiteDB](https://www.litedb.org/) |
| System data | WMI (`System.Management`), `PerformanceCounter` |
| Target runtime | .NET 10 |

---

## 🔒 Privacy

V-Task does **not** collect, send, or store any personal data.  
The only thing saved locally is your selected interface language.

See [`PRIVACY_POLICY.md`](PRIVACY_POLICY.md) for full details.

---

## 🌍 Localization

V-Task is available in 5 languages out of the box:

- 🇺🇸 English
- 🇺🇦 Українська (Ukrainian)
- 🇩🇪 Deutsch (German)
- 🇹🇷 Türkçe (Turkish)
- 🇷🇺 Русский (Russian)

Language strings are stored in `Resources/Strings.{lang}.axaml` and can be extended easily.

---

## 📄 License

This project is licensed under the **MIT License** — feel free to use, modify, and distribute.

---

<div align="center">

Made with ❤️ by **Oleh Kurylo**

</div>
