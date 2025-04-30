using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Forms;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Manages application hotkeys by overriding Explorer's built-in Win+VK mappings.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly HotkeyListenerWindow _listenerWindow;

        public HotkeyManager()
        {
            _listenerWindow = new HotkeyListenerWindow(this);

            if (Settings.Instance.OverrideHotkeys)
            {
                _listenerWindow.RegisterHotkeys();
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void Dispose()
        {
            if (Settings.Instance.OverrideHotkeys)
            {
                _listenerWindow.UnregisterHotkeys();
            }

            _listenerWindow?.Dispose();
        }

        #region Events
        public class TaskbarHotkeyEventArgs : EventArgs
        {
            public int index;
        }

        public event EventHandler<TaskbarHotkeyEventArgs> TaskbarHotkeyPressed;

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.OverrideHotkeys))
            {
                if (Settings.Instance.OverrideHotkeys)
                {
                    _listenerWindow.RegisterHotkeys();
                }
                else
                {
                    _listenerWindow.UnregisterHotkeys();
                }
            }
        }
        #endregion

        private class HotkeyListenerWindow : NativeWindow, IDisposable
        {
            private readonly HotkeyManager _manager;
            private const int HOTKEY_BASE_ID = 500;
            private readonly HashSet<int> _registeredHotkeys = [];
            private const int EXPLORER_UNREGISTER_HOTKEY = (int)WM.USER + 231;
            private List<HotkeyEntry> _explorerHotkeyTable;
            private IntPtr _trayWindow;

            public HotkeyListenerWindow(HotkeyManager manager)
            {
                CreateHandle(new CreateParams());
                _manager = manager;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == (int)WM.HOTKEY)
                {
                    int hotkeyId = m.WParam.ToInt32();
                    if (_registeredHotkeys.Contains(hotkeyId))
                    {
                        _manager.TaskbarHotkeyPressed?.Invoke(this, new TaskbarHotkeyEventArgs { index = hotkeyId });
                        ShellLogger.Debug($"HotkeyManager: Hotkey pressed: ID={hotkeyId}");
                    }
                }

                base.WndProc(ref m);
            }

            public void RegisterHotkeys()
            {
                ShellLogger.Info("HotkeyManager: Registering override hotkeys");

                try
                {
                    // Initialize hotkey table and tray window
                    LoadExplorerResources();

                    // Register the number keys
                    RegisterWinKey(VK.KEY_1, 0);
                    RegisterWinKey(VK.KEY_2, 1);
                    RegisterWinKey(VK.KEY_3, 2);
                    RegisterWinKey(VK.KEY_4, 3);
                    RegisterWinKey(VK.KEY_5, 4);
                    RegisterWinKey(VK.KEY_6, 5);
                    RegisterWinKey(VK.KEY_7, 6);
                    RegisterWinKey(VK.KEY_8, 7);
                    RegisterWinKey(VK.KEY_9, 8);
                    RegisterWinKey(VK.KEY_0, 9);
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Exception during RegisterHotkeys - {ex.Message}");
                }
            }

            private void LoadExplorerResources()
            {
                // Initialize with empty table as fallback
                _explorerHotkeyTable = [];
                _trayWindow = IntPtr.Zero;

                try
                {
                    // FIXME: Try to find a better way to get the explorer.exe path, maybe use the handle of the process
                    string explorerPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot")!, "explorer.exe");
                    if (File.Exists(explorerPath))
                    {
                        _explorerHotkeyTable = HotkeyUtility.BuildExplorerHotkeyTableInternal(explorerPath);
                        ShellLogger.Debug($"HotkeyManager: Found {_explorerHotkeyTable.Count} entries in explorer hotkey table");
                    }
                    else
                    {
                        // This is bad...
                        ShellLogger.Warning("HotkeyManager: explorer.exe not found at expected location");
                    }
                }
                catch (Exception ex)
                {
                    // WTF: Using an older pre-Vista explorer or something else went wrong
                    ShellLogger.Warning($"HotkeyManager: Failed to build explorer hotkey table - {ex.Message}");
                }

                try
                {
                    _trayWindow = WindowHelper.FindWindowsTray(WindowHelper.FindWindowsTray(IntPtr.Zero));
                    if (_trayWindow == IntPtr.Zero)
                    {
                        ShellLogger.Debug("HotkeyManager: Real explorer tray window not found");
                    }
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Failed to find real explorer tray window - {ex.Message}");
                }
            }

            private bool RegisterWinKey(VK key, int taskIndex)
            {
                try
                {
                    // Try to unregister from explorer if possible
                    TryUnregisterExplorerHotkey(key);

                    // Register the hotkey for RetroBar
                    // (will run even if explorer unregistration failed)
                    bool success = RegisterHotKey(
                        Handle,
                        taskIndex,
                        HotkeyUtility.MOD_WIN | HotkeyUtility.MOD_NOREPEAT,
                        (uint)key);

                    if (success)
                    {
                        _registeredHotkeys.Add(taskIndex);
                        ShellLogger.Debug($"HotkeyManager: Registered hotkey Win+{key} with id={taskIndex}");
                    }
                    else
                    {
                        ShellLogger.Warning($"HotkeyManager: Failed to register hotkey Win+{key}");
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Exception registering Win+{key} - {ex.Message}");
                    return false;
                }
            }

            private void TryUnregisterExplorerHotkey(VK key)
            {
                // Skip explorer unregistration if we don't have the required info
                if (_explorerHotkeyTable == null || _explorerHotkeyTable.Count == 0 || _trayWindow == IntPtr.Zero)
                {
                    ShellLogger.Debug($"HotkeyManager: Skipping Explorer hotkey unregistration for Win+{key} (resources not available)");
                    return;
                }

                try
                {
                    // Find key with exactly MOD_WIN modifier (no other modifiers) in the explorer hotkey table
                    int explorerIndex = _explorerHotkeyTable.FindIndex(
                        e => e.VirtualKey == (byte)key && e.Modifier == HotkeyUtility.MOD_WIN);

                    if (explorerIndex < 0)
                    {
                        ShellLogger.Debug($"HotkeyManager: Win+{key} not found in explorer hotkey table");
                        return;
                    }

                    // Calculate the hotkey ID used by explorer
                    int hotkeyId = HOTKEY_BASE_ID + explorerIndex;

                    // Tell explorer to unregister this hotkey
                    SendMessage(_trayWindow, EXPLORER_UNREGISTER_HOTKEY, new IntPtr(hotkeyId), IntPtr.Zero);
                    ShellLogger.Debug($"HotkeyManager: Sent EXPLORER_UNREGISTER_HOTKEY for Win+{key} with id={hotkeyId}");
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Exception unregistering Explorer hotkey Win+{key} - {ex.Message}");
                }
            }

            public void UnregisterHotkeys()
            {
                // Only unregister those we actually registered
                ShellLogger.Info("HotkeyManager: Unregistering override hotkeys");
                foreach (int id in _registeredHotkeys)
                {
                    UnregisterHotKey(Handle, id);
                    ShellLogger.Debug($"HotkeyManager: Unregistered hotkey id={id}");
                }

                _registeredHotkeys.Clear();

                // TODO: Re-register explorer hotkeys
                // (maybe sending some undocumented message to the tray again, otherwise, restart explorer process?)
            }

            public void Dispose() => DestroyHandle();
        }
    }

    public static class HotkeyUtility
    {
        public const uint MOD_WIN = 0x0008; // System.Windows.Input.ModifierKeys.Windows
        public const uint MOD_NOREPEAT = 0x4000;

        /// <summary>
        /// Extracts the Windows hotkey table from explorer.exe by reading the binary file.
        /// </summary>
        /// <param name="explorerPath">The full path to explorer.exe</param>
        /// <returns>A list of hotkey entries found in explorer.exe</returns>
        /// <remarks>
        /// This method works by scanning explorer.exe for byte patterns that match the structure of hotkey entries.
        /// It identifies sequences of bytes that follow the pattern where:
        /// - Each entry is 8 bytes
        /// - Only byte 0 (virtual key code) and byte 4 (modifier) have non-zero values
        /// - The modifier has the Windows key flag set
        ///
        /// The method finds the longest consecutive sequence of valid entries, which is likely
        /// the hotkey table used by explorer to register system-wide Windows+Key combinations.
        /// </remarks>
        public static List<HotkeyEntry> BuildExplorerHotkeyTableInternal(string explorerPath)
        {
            using var mmf = MemoryMappedFile.CreateFromFile(explorerPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var reader = new BinaryReader(stream);
            byte[] data = reader.ReadBytes((int)stream.Length);

            var offsets = new List<int>();
            for (int i = 0; i <= data.Length - 8; i += 8)
            {
                if (IsValidEntryAt(data, i))
                    offsets.Add(i);
            }

            int bestStart = 0, bestCount = 0;
            foreach (int start in offsets)
            {
                int count = 1;
                while (offsets.Contains(start + count * 8)) count++;
                if (count > bestCount)
                {
                    bestStart = start;
                    bestCount = count;
                }
            }

            var table = new List<HotkeyEntry>(bestCount);
            for (int i = 0; i < bestCount; i++)
            {
                int off = bestStart + i * 8;
                table.Add(new HotkeyEntry
                {
                    Index = i,
                    VirtualKey = data[off],
                    Modifier = data[off + 4]
                });
            }

            return table;
        }

        /// <summary>
        /// Checks if the bytes at the specified index match the pattern of a Windows hotkey entry in explorer.exe
        /// </summary>
        /// <param name="bytes">The byte array to check</param>
        /// <param name="offset">The offset to start checking at</param>
        /// <returns>True if the bytes at offset match the hotkey entry pattern</returns>
        private static bool IsValidEntryAt(byte[] bytes, int offset)
        {
            // Entry must be aligned to 8 bytes (hotkey entries are 8 bytes each)
            const int ENTRY_SIZE = 8;
            if (offset % ENTRY_SIZE != 0)
                return false;

            // Only byte 0 (VK code) and byte 4 (modifier) should have values, rest must be zero
            for (int i = 1; i <= 7; i++)
            {
                if (i != 4 && bytes[offset + i] != 0)
                    return false;
            }

            // Check that modifier has Windows key flag set and high bits are clear
            byte modifier = bytes[offset + 4];
            return (modifier & MOD_WIN) != 0 && (modifier & 0xF0) == 0;
        }
    }

    public struct HotkeyEntry
    {
        public int Index; // ID
        public byte VirtualKey;
        public byte Modifier;
    }
}