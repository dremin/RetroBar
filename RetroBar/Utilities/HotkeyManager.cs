using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Forms;
using static ManagedShell.Interop.NativeMethods;
using static RetroBar.Utilities.Enums;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Manages application hotkeys by overriding Explorer's built-in Win + optional modifier + VK hotkeys
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly HotkeyListenerWindow _listenerWindow;

        public HotkeyManager()
        {
            _listenerWindow = new HotkeyListenerWindow(this);

            if (Settings.Instance.WinNumHotkeysAction != WinNumHotkeysOption.WindowsDefault)
            {
                _listenerWindow.RegisterHotkeys();
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void Dispose()
        {
            if (_listenerWindow.IsRegistered)
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
            if (e.PropertyName == nameof(Settings.WinNumHotkeysAction))
            {
                if (!_listenerWindow.IsRegistered && Settings.Instance.WinNumHotkeysAction != WinNumHotkeysOption.WindowsDefault)
                {
                    _listenerWindow.RegisterHotkeys();
                }
                else if (_listenerWindow.IsRegistered && Settings.Instance.WinNumHotkeysAction == WinNumHotkeysOption.WindowsDefault)
                {
                    _listenerWindow.UnregisterHotkeys();
                }
            }
        }
        #endregion

        private class HotkeyListenerWindow : NativeWindow, IDisposable
        {
            internal bool IsRegistered => _registeredHotkeys.Count > 0;
            private readonly HotkeyManager _manager;
            private readonly HashSet<int> _registeredHotkeys = [];
            private const int WMTRAY_UNREGISTERHOTKEY = (int)WM.USER + 231;
            private List<TrayHotkeyEntry> _trayHotkeyTable;
            private bool _unregisterFromExplorer = true;
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

                    _unregisterFromExplorer = _trayWindow != IntPtr.Zero;
                    if (!_unregisterFromExplorer)
                    {
                        ShellLogger.Info("HotkeyManager: Explorer resources not fully available - hotkeys will be registered but not unregistered from Explorer");
                    }

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
                _trayHotkeyTable = [];
                _trayWindow = IntPtr.Zero;

                // Step 1: Try to build the hotkey table
                bool tableLoaded = TryBuildHotkeyTable();

                // Step 2: Only find the tray window if we have a valid hotkey table
                if (tableLoaded && _trayHotkeyTable.Count > 0)
                {
                    TryFindTrayWindow();
                }
            }

            private bool TryBuildHotkeyTable()
            {
                try
                {
                    string explorerPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot")!, "explorer.exe");
                    if (!File.Exists(explorerPath))
                    {
                        ShellLogger.Warning("HotkeyManager: explorer.exe not found at expected location");
                        return false;
                    }

                    _trayHotkeyTable = HotkeyUtility.BuildTrayHotkeyTableInternal(explorerPath);
                    ShellLogger.Debug($"HotkeyManager: Found {_trayHotkeyTable.Count} entries in Explorer hotkey table");
                    return true;
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Failed to build Explorer hotkey table - {ex.Message}");
                    return false;
                }
            }

            private void TryFindTrayWindow()
            {
                try
                {
                    _trayWindow = WindowHelper.FindWindowsTray(WindowHelper.FindWindowsTray(IntPtr.Zero));
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Failed to find real Explorer tray window - {ex.Message}");
                }
            }

            private bool RegisterWinKey(VK key, int taskIndex, MOD additionalModifiers = 0)
            {
                // Combine the base modifiers with any additional modifiers
                MOD modifiers = MOD.WIN | MOD.NOREPEAT | additionalModifiers;

                try
                {
                    // Try to unregister from Explorer if possible
                    if (_unregisterFromExplorer)
                    {
                        TryUnregisterTrayHotkey(key, additionalModifiers);
                    }

                    // Attempt to register the hotkey for RetroBar
                    bool success = RegisterHotKey(
                        Handle,
                        taskIndex,
                        (uint)modifiers,
                        (uint)key);

                    if (success)
                    {
                        _registeredHotkeys.Add(taskIndex);
                        ShellLogger.Debug($"HotkeyManager: Registered hotkey {modifiers}+{key} with id={taskIndex}");
                    }
                    else
                    {
                        ShellLogger.Warning($"HotkeyManager: Failed to register hotkey {modifiers}+{key} with id={taskIndex}");
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Exception registering hotkey {modifiers}+{key} - {ex.Message}");
                    return false;
                }
            }

            private void TryUnregisterTrayHotkey(VK key, MOD additionalModifiers = 0)
            {
                try
                {
                    // Combine WIN with any additional modifiers (no need to check for MOD.NOREPEAT here)
                    MOD searchModifier = MOD.WIN | additionalModifiers;

                    // Find key with the specified modifiers in the Explorer hotkey table
                    int trayHotkeyIndex = _trayHotkeyTable.FindIndex(e => e.VirtualKey == (byte)key && e.Modifier == (byte)searchModifier);

                    // Exit if no matching hotkey found
                    if (trayHotkeyIndex < 0) return;

                    // Found a match - send unregister message to explorer
                    int trayHotkeyId = _trayHotkeyTable[trayHotkeyIndex].Id;
                    SendMessage(_trayWindow, WMTRAY_UNREGISTERHOTKEY, new IntPtr(trayHotkeyId), IntPtr.Zero);
                    ShellLogger.Debug($"HotkeyManager: Sent WMTRAY_UNREGISTERHOTKEY for hotkey id={trayHotkeyId}");
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Exception unregistering Explorer hotkey - {ex.Message}");
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

                // TODO: Re-register Explorer hotkeys
                // (maybe sending some undocumented message to the tray again, otherwise, restart Explorer process?)
            }

            public void Dispose() => DestroyHandle();
        }
    }

    public static class HotkeyUtility
    {
        /// <summary>
        /// Extracts the Windows hotkey table registered by Shell_TrayWnd from explorer.exe by reading the binary file.
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
        /// the hotkey table used by Explorer's Shell_TrayWnd to register system-wide Windows+Key combinations.
        /// </remarks>
        public static List<TrayHotkeyEntry> BuildTrayHotkeyTableInternal(string explorerPath)
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

            var table = new List<TrayHotkeyEntry>(bestCount);
            for (int i = 0; i < bestCount; i++)
            {
                int off = bestStart + i * 8;
                table.Add(new TrayHotkeyEntry
                {
                    Id = i + 500, // Shell_TrayWnd hotkey IDs start at 500
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
            return (modifier & (uint)MOD.WIN) != 0 && (modifier & 0xF0) == 0;
        }
    }

    public struct TrayHotkeyEntry
    {
        public int Id;
        public byte VirtualKey;
        public byte Modifier;
    }

    // TODO: Move to upstream ManagedShell library
    internal class Enums
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
        [Flags]
        public enum MOD : uint
        {
            ALT = 0x0001,
            CONTROL = 0x0002,
            NOREPEAT = 0x4000,
            SHIFT = 0x0004,
            WIN = 0x0008 // System.Windows.Input.ModifierKeys.Windows
        }
    }
}