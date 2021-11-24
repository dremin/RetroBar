![alt text](https://raw.githubusercontent.com/dremin/retrobar/master/retrobar-preview.png "RetroBar")

# RetroBar
[![Current release](https://img.shields.io/github/v/release/dremin/RetroBar)](https://github.com/dremin/RetroBar/releases/latest) ![Build status](https://github.com/dremin/RetroBar/workflows/RetroBar/badge.svg)

Pining for simpler times? RetroBar teleports you back in time by replacing your modern Windows taskbar with the classic Windows 95, 98, Me, 2000, or XP style.

RetroBar is based on the [ManagedShell](https://github.com/cairoshell/ManagedShell) library for great compatibility and performance.

## Requirements
- Windows 7 SP1, Windows 8.1, Windows 10 or later
- [.NET Core 3.1 desktop runtime](https://dotnet.microsoft.com/download/dotnet/3.1/runtime) (select the appropriate download button under "Run desktop apps")

## Features
- Replaces default Windows taskbar with classic layout
- Native notification area with balloon notification support
- Native task list with UWP app support
- Quick launch toolbar
- Start button opens modern start menu
- Ability to show or hide the clock
- Display taskbar on any side of the screen (even on Windows 11)
- Option to display the taskbar, notification area, and clock on multiple monitors
- XP-style collapsible notification area
- Custom theme support
- Several themes included:
  - System
  - Watercolor
  - Windows 95-98
  - Windows Me
  - Windows 2000
  - Windows XP (Blue, Silver and Classic)
- Support for the following languages:
  - English
  - Spanish (español)
  - French (français)
  - Portuguese (português)
  - Simplified Chinese (简体中文)
  - Russian (русский)
  - Hungarian (magyar)
  - Vietnamese (Tiếng Việt)
  - Hebrew (עברית)

## Custom themes
RetroBar supports custom themes. To use custom themes, create a `Themes` directory in the same directory as `RetroBar.exe`, and place valid `.xaml` theme files there.

Themes use the XAML `ResourceDictionary` format. [View the included example themes](https://github.com/dremin/RetroBar/tree/master/RetroBar/Themes) to get started.