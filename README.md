﻿![alt text](https://raw.githubusercontent.com/dremin/retrobar/master/retrobar-preview.png "RetroBar")

# RetroBar
[![Current release](https://img.shields.io/github/v/release/dremin/RetroBar)](https://github.com/dremin/RetroBar/releases/latest) ![Build status](https://github.com/dremin/RetroBar/workflows/RetroBar/badge.svg)

Pining for simpler times? RetroBar teleports you back in time by replacing your modern Windows taskbar with the classic Windows 95, 98, Me, 2000, XP, or Vista style.

RetroBar is based on the [ManagedShell](https://github.com/cairoshell/ManagedShell) library for great compatibility and performance.

## Requirements
- Windows 7 SP1, Windows 8.1, Windows 10, or Windows 11
- [.NET 6.0.2 or later desktop runtime](https://dotnet.microsoft.com/download/dotnet/6.0/runtime) (select the appropriate download button under "Run desktop apps")

## Features
- Replaces default Windows taskbar with classic layout
- Native notification area with balloon notification support
- Native task list with UWP app support and drag reordering
- Quick launch toolbar
- Start button opens modern start menu
- Ability to show or hide the clock
- Ability to auto-hide the taskbar
- Locked and unlocked taskbar appearances
- Display taskbar on any side of the screen (even on Windows 11)
- Resizable taskbar with support for multiple rows
- Option to display the taskbar, notification area, and clock on multiple monitors
- Ability to show Vista-style window thumbnails
- Customizable XP-style collapsible notification area
- Custom theme support

## Included themes
- System (Classic, XP, and Vista)
- Watercolor
- Windows 95-98
- Windows Me
- Windows 2000
- Windows XP:
  - Classic
  - Blue
  - Olive Green
  - Silver
  - Royale
  - Royale Noir
  - Embedded Style
  - Zune Style
- Windows Longhorn Aero
- Windows Vista:
  - Aero
  - Basic
  - Classic

## Supported languages
- Arabic (العربية)
- Basque (euskara)
- Bulgarian (български)
- Catalan (català)
- Chinese (Simplified) (中文(简体))
- Chinese (Traditional) (中文(繁體))
- Croatian (hrvatski)
- Czech (čeština)
- Dutch (Nederlands)
- English
- English (United Kingdom)
- Finnish (Suomi)
- French (français)
- German (Deutsch)
- Greek (ελληνικά)
- Hebrew (עברית)
- Hungarian (magyar)
- Indonesian (Indonesia)
- Italian (italiano)
- Japanese (日本語)
- Korean (한국어)
- Latvian (latviešu)
- Lithuanian (lietuvių)
- Luxembourgish (Lëtzebuergesch)
- Malay (Melayu)
- Persian (فارسی)
- Polish (polski)
- Portuguese (português)
- Romanian (română)
- Russian (русский)
- Serbian (Cyrillic) (српски)
- Serbian (Latin) (srpski)
- Slovak (slovenčina)
- Spanish (español)
- Swedish (svenska)
- Thai (ไทย)
- Turkish (Türkçe)
- Ukrainian (українська)
- Vietnamese (Tiếng Việt)

## Custom languages and themes
RetroBar supports custom languages and themes. To use custom languages or themes, create a `Languages` or a `Themes` directory and place valid `.xaml` language or theme files there.

Valid locations to create a `Languages` or a `Themes` directory:
- In the same directory as `RetroBar.exe`
- In `%localappdata%\RetroBar`

Themes use the XAML `ResourceDictionary` format. [View the included example themes](https://github.com/dremin/RetroBar/tree/master/RetroBar/Themes) to get started.
