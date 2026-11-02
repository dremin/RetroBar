# Taskbar network speed indicators

Download and Upload appear immediately left of the notification tray, outside its border.
They use RetroBar's dynamic Clock style (font and foreground), inherit taskbar scaling,
and reserve space to avoid moving task buttons as values change. The control follows
the taskbar's trailing dock when orientation changes.

Rates update every second, in decimal bytes per second: B/s, KB/s, MB/s, GB/s, TB/s.
Hover for the counted adapters and unit explanation. These are physical link traffic
rates, including LAN traffic and VPN overhead, rather than an internet speed test.

GetIfTable2 supplies 64-bit InOctets/OutOctets. Only up, hardware interfaces with a
physical connector count; filters, virtual switches, tunnels, disconnected, paused,
low-power and endpoint interfaces do not. Each interface GUID counts once. Each
physical adapter gets its own baseline; additions, removals and decreasing counters
cannot create spikes. Gaps over five seconds rebaseline after sleep or UI stalls.
No active physical adapters shows zero rates; a failed read shows an unavailable dash
and clears the baseline. Unloading stops the timer. Existing network icon selection,
WLAN notifications, drawing and flyout handling are unchanged.

## Validation (October 28, 2026)

- Release builds passed for net10.0-windows and net6.0-windows. The SDK warns that
  .NET 6 is out of support. The installed application uses .NET 10.
- `dotnet run --project tests/RetroBar.NetworkTests -c Release` checks elapsed-time
  calculations, upload/download directions, reset/reconnect/disconnect, adapter
  changes, duplicates, independent adapters, unit rounding, native interface filtering,
  and the preserved tray icon adapter selection and Wi-Fi signal logic.
- Native live sampling counted Wi-Fi, excluding both up VMware adapters on this laptop.
- A successful GitHub archive download transferred 598,960 bytes; the live sampler
  observed changing download rates. `--watch` adds 20 seconds of native sampling.
- A controlled 2 MB zero-filled upload produced about 65-78 KB/s of sustained upload
  traffic at a 64 KiB/s curl limit. The echo endpoint returned HTTP 500 after receiving
  the payload; this was used only to generate outbound traffic, not to validate the endpoint.
- Installed build was restarted and visually inspected in the user's Vista Classic
  theme at 1.05 taskbar scaling. Both labels fit, no overlap, original Wi-Fi icon visible.
  Captures: `artifacts/taskbar-detail.png` and `artifacts/taskbar-upload-detail.png`.
- Disconnect/reconnect, sleep and adapter changes were tested with deterministic
  snapshots; the user's live Wi-Fi was not disabled. Other themes/orientations were
  not manually exercised.

## Preserved installation and restore

Full original installation:
`C:\Users\sstew\AppData\Local\Programs\RetroBar-backup-20261028-211559`

The original DLL hash was verified against the backup before replacement.
`settings.snapshot.json` in that directory preserves the original user settings too;
deployment did not change the user's settings. The original uninstall files are preserved.

To restore the original binaries, run from the repository:

```powershell
.\scripts\Restore-InstalledBuild.ps1 -BackupPath 'C:\Users\sstew\AppData\Local\Programs\RetroBar-backup-20261028-211559'
```

This stops only the installed RetroBar, copies the backup over it, verifies the DLL,
and restarts it. It leaves the current settings in place. Restore was not run because
the validated indicator build is the active installation.
