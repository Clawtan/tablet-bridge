# TabletBridge

[![Platform: Windows 11](https://img.shields.io/badge/Platform-Windows%2011-0078D7.svg)](https://www.microsoft.com/)
[![Language: C#](https://img.shields.io/badge/Language-C%23%20.NET%204.8-purple.svg)](https://dotnet.microsoft.com/)
[![Protocol: UHID / AOA](https://img.shields.io/badge/Protocol-UHID%20%2F%20scrcpy-green.svg)](https://github.com/Genymobile/scrcpy)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, zero-latency Windows System Tray utility in native C# that bridges PC keyboard and mouse control to an Android tablet via USB hardware virtualization (UHID / AOA).

---

## Features

- **Invisible Window Stripping**: Scrcpy capture window is stripped from taskbar via WS_EX_TOOLWINDOW and rendered borderless at 2x2 px (100% invisible).
- **Fluent Monochrome Tray Icon**: Seamlessly matches Windows 11 dark mode taskbar aesthetic.
- **Global Hotkey Switch (Ctrl + Q)**: Instant hardware control toggling between PC and Android tablet using Win32 RegisterHotKey (zero input hook timeouts).
- **Hardware Auto Sleep / Wake**:
  - Switching to PC: Triggers db shell input keyevent 223 (instant tablet display sleep).
  - Switching to Tablet: Triggers input keyevent 224 (instant tablet display wake).

---

## Building from Source

Compile directly via native .NET Framework csc.exe (zero Visual Studio bloat needed):
\\\cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:TabletBridge.exe TabletBridge.cs
\\\

---

## License

MIT License. Built by [Clawtan](https://github.com/Clawtan).