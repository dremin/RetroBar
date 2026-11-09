![RetroBar screenshot](https://raw.githubusercontent.com/dremin/retrobar/master/retrobar-preview.png)

# RetroBar
[![Current release](https://img.shields.io/github/v/release/dremin/RetroBar)](https://github.com/dremin/RetroBar/releases/latest) ![Build status](https://github.com/dremin/RetroBar/workflows/RetroBar/badge.svg)  [![Discord](https://img.shields.io/badge/discord-white?logo=discord)](https://discord.gg/8bGuPmerxC)

Pining for simpler times? RetroBar teleports you back in time by replacing your modern Windows taskbar with the classic Windows 95, 98, Me, 2000, XP, or Vista style.

RetroBar is based on the [ManagedShell](https://github.com/cairoshell/ManagedShell) library for great compatibility and performance.

## Features in this fork

This [RetroBar fork](https://github.com/sstewart207/RetroBar) adds:

- **Restored network tray icon on Windows 11 24H2 and newer.** Shows Wi-Fi signal strength, Ethernet connectivity, and disconnected or limited connectivity states, with access to the network flyout and settings.
- **Live upload and download speeds.** Clearly labeled rates sit immediately left of the notification tray and update about once per second, with readable B/s, KB/s, MB/s, GB/s, and TB/s units.
- **Matching taskbar styling.** Speed indicators use the current theme's clock font and text color, follow taskbar scaling, and reserve space so changing values do not shift task buttons.
- **Traffic counted once.** Rates count connected physical adapters and exclude virtual adapters, VPN tunnels, and filter interfaces to avoid counting the same traffic twice. Hover over the indicators to see which adapters are included.
- **Graceful connection changes.** Adapter changes, disconnections, counter resets, and resume from sleep reset the sampling baseline to avoid misleading spikes.

Rates use bytes per second (1 KB = 1,000 bytes) and include local network traffic. See [network speed details and validation](NETWORK-SPEED.md) for implementation notes, testing, and restore instructions.

## Requirements
- Windows 7 SP1, Windows 8.1, Windows 10, or Windows 11
  - Fresh installs of Windows 7 may require additional Windows updates. See [#1319](https://github.com/dremin/RetroBar/issues/1319) for details.
- Microsoft .NET Desktop Runtime - When using the RetroBar installer, this is automatically downloaded and installed if necessary. If you're using the portable version, you will be prompted to download on first launch.

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
- Customizable XP-style collapsible notification area with drag reordering
- Input language and keyboard layout switcher
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

Looking for more themes? [Check out the great community-made RetroBar themes on DeviantArt](https://www.deviantart.com/tag/retrobar), and the theme-repository channel on [our Discord](https://discord.gg/8bGuPmerxC)!

## Supported languages
- Arabic (العربية)
- Basque (euskara)
- Belarusian (беларуская)
- Bulgarian (български)
- Catalan (català)
- Chinese (Simplified) (中文(简体))
- Chinese (Traditional) (中文(繁體))
- Croatian (hrvatski)
- Czech (čeština)
- Danish (dansk)
- Dutch (Nederlands)
- English
- English (United Kingdom)
- Finnish (Suomi)
- French (français)
- German (Deutsch)
- Greek (ελληνικά)
- Hebrew (עברית)
- Hungarian (magyar)
- Icelandic (íslenska)
- Indonesian (Indonesia)
- Italian (italiano)
- Japanese (日本語)
- Korean (한국어)
- Latvian (latviešu)
- Lithuanian (lietuvių)
- Luxembourgish (Lëtzebuergesch)
- Macedonian (Македонски)
- Malay (Melayu)
- Maltese (Malti)
- Norsk (bokmål)
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
- Welsh (Cymraeg)

## Custom languages and themes
RetroBar supports custom languages and themes. You may install community-made theme files that you have downloaded in RetroBar Properties > Advanced.

You may manually install custom languages or themes by creating a `Languages` or a `Themes` directory in `%localappdata%\RetroBar`, and placing valid `.xaml` language or theme files there.

Themes use the XAML `ResourceDictionary` format. When creating a new theme, [view the included example themes](https://github.com/dremin/RetroBar/tree/master/RetroBar/Themes) to get started.

## Open-Shell Menu users

You may need to adjust some Open-Shell Menu settings for the best compatibility with RetroBar. We recommend the following settings:

- Controls > Windows Key opens > Open-Shell Menu
- Menu Look > Align start menu to working area
