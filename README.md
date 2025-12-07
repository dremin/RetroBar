# RetroBar (Enhanced Fork)

A fork of [RetroBar](https://github.com/dremin/RetroBar) with additional features and quality-of-life improvements.

![RetroBar Preview](https://raw.githubusercontent.com/dremin/retrobar/master/retrobar-preview.png)

## About This Fork

This fork adds several features I wanted for my own daily use. It's not perfect and there are some bugs here and there, but it works well day-to-day and I find it to be an improvement over the original.

## What's New in This Fork

- **Media control buttons in window previews** - Play, pause, skip tracks directly from the taskbar thumbnail (for apps that support it like Spotify, media players, etc.)
- **Pinned apps open in place** - When you click a pinned app, it opens where the pin is instead of creating a separate button on the right with other running programs
- **Show date in the clock** - Option to display the date alongside the time
- **12-hour or 24-hour clock** - Choose your preferred time format
- **Date format options** - Switch between MM/DD/YYYY and DD/MM/YYYY
- **Live window thumbnails** - Hover over taskbar buttons to see live previews with title bar and close button
- **Improved window grouping** - Better support for grouping similar windows together

## Requirements

- Windows 7 SP1, Windows 8.1, Windows 10, or Windows 11
- [.NET 6.0.2 or later desktop runtime](https://dotnet.microsoft.com/download/dotnet/6.0/runtime)

## Features (from Original RetroBar)

- Replaces default Windows taskbar with classic layout
- Native notification area with balloon notification support
- Native task list with UWP app support and drag reordering
- Quick launch toolbar
- Start button opens modern start menu
- Show/hide clock, auto-hide taskbar
- Display on any side of screen
- Resizable with multiple row support
- Multi-monitor support
- Customizable XP-style collapsible notification area

## Included Themes

- System (Classic, XP, Vista)
- Watercolor
- Windows 95-98, Me, 2000
- Windows XP: Classic, Blue, Olive Green, Silver, Royale, Royale Noir, Embedded, Zune
- Windows Longhorn Aero
- Windows Vista: Aero, Basic, Classic

More themes available on [DeviantArt](https://www.deviantart.com/tag/retrobar).

## Custom Themes and Languages

Install custom themes by creating a `Themes` directory in `%localappdata%\RetroBar` and placing valid `.xaml` theme files there. Same applies for `Languages` directory.

## Credits

- Original RetroBar by [dremin](https://github.com/dremin/RetroBar)
- Based on [ManagedShell](https://github.com/cairoshell/ManagedShell) by Cairo Shell team
- Community theme creators on DeviantArt

## License

Same license as the original RetroBar project.

---

## Technical Details

This fork includes a modified version of [ManagedShell](https://github.com/cairoshell/ManagedShell) bundled directly in the repository (changes are pending upstream review).

### Building from Source

**Prerequisites:**
- Visual Studio 2022 with .NET desktop development workload
- .NET 6.0 SDK

**Build Command:**
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\amd64\MSBuild.exe" RetroBar.sln -p:Configuration=Release -p:Platform="Any CPU"
```

### ManagedShell Changes

The bundled ManagedShell adds support for thumbnail toolbar buttons (the media controls you see in window previews). This required adding:

- `ThumbnailButton` class - A wrapper for native Windows thumbnail button data
- `ThumbnailButtons` property on `ApplicationWindow` - Exposes the button array
- `ThumbnailButtonImageList` property - For extracting button icons
