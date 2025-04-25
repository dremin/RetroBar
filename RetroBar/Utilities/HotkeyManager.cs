using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.IO;
using System.Windows.Forms;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    public class HotkeyManager : IDisposable
    {
        public HotkeyListenerWindow listenerWindow;

        public HotkeyManager()
        {
            listenerWindow = new HotkeyListenerWindow(this);

            if (Settings.Instance.OverrideHotkeys)
            {
                listenerWindow.RegisterHotkeys();
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void Dispose()
        {
            if (Settings.Instance.OverrideHotkeys)
            {
                listenerWindow.UnregisterHotkeys();
            }

            listenerWindow?.Dispose();
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
                    listenerWindow.RegisterHotkeys();
                }
                else
                {
                    listenerWindow.UnregisterHotkeys();
                }
            }
        }
        #endregion

        public class HotkeyListenerWindow : NativeWindow, IDisposable
        {
            private readonly HotkeyManager _manager;

            public HotkeyListenerWindow(HotkeyManager manager)
            {
                CreateHandle(new CreateParams());
                _manager = manager;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == (int)WM.HOTKEY)
                {
                    int hotkeyId = (int)m.WParam;

                    if (hotkeyId >= 0 && hotkeyId <= 9) // 0-9: Quick switch hotkeys
                    {
                        TaskbarHotkeyEventArgs args = new() { index = hotkeyId };
                        _manager.TaskbarHotkeyPressed?.Invoke(this, args);
                    }
                }

                base.WndProc(ref m);
            }

            #region Explorer hotkey table detection

            /// <summary>
            /// Contains logic to locate Explorer’s built‑in taskbar hotkey table index at runtime.
            /// </summary>
            private static class ExplorerHotkeyTable
            {
                // Pattern of four consecutive hotkey entries for Win+1…Win+4 (each entry is two DWORDs: key code and modifiers)
                private static readonly byte[] Pattern =
                [
                    0x31,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                    0x32,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                    0x33,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                    0x34,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                ];

                // Fallback index in case detection fails (known working default on recent Windows versions)
                private const int DefaultIndex = 17;

                // Size in bytes of one hotkey entry (two 32-bit values)
                private const int EntrySize = sizeof(int) * 2;

                // Cached result to avoid repeated file scans
                private static int? _cachedIndex;

                /// <summary>
                /// Scans the local Explorer.exe to find the zero‑based index of the first taskbar hotkey entry.
                /// </summary>
                /// <returns>
                /// The index of the first taskbar hotkey entry in Explorer’s hotkey table, or <see cref="DefaultIndex" /> if detection fails.
                /// </returns>
                public static int GetIndex()
                {
                    if (_cachedIndex.HasValue) return _cachedIndex.Value;

                    try
                    {
                        // Read Explorer binary into memory
                        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                        var path = Path.Combine(systemRoot!, "explorer.exe");
                        byte[] data = File.ReadAllBytes(path);

                        // Locate our known pattern (Win+1…4)
                        int offset = data.AsSpan().IndexOf(Pattern);
                        if (offset < 0)
                            throw new InvalidOperationException("Pattern not found in explorer.exe");

                        // Walk backwards entry-by-entry until we hit an all-zero entry (table header)
                        int index = 0;
                        int position = offset - EntrySize;
                        while (position >= 0)
                        {
                            // Check the two consecutive DWORDs
                            bool isEmpty = true;
                            for (int i = 0; i < EntrySize; i++)
                            {
                                if (data[position + i] != 0)
                                {
                                    isEmpty = false;
                                    break;
                                }
                            }

                            if (isEmpty)
                                break;

                            index++;
                            position -= EntrySize;
                        }

                        _cachedIndex = index;
                    }
                    catch (Exception ex)
                    {
                        ShellLogger.Warning($"HotkeyManager: Explorer hotkey index detection failed: {ex.Message}");
                        _cachedIndex = DefaultIndex;
                    }

                    return _cachedIndex.Value;
                }
            }

            #endregion

            /// <summary>
            /// Custom window message to ask Explorer to unregister a hotkey.
            /// </summary>
            private const int EXPLORER_UNREGISTER_HOTKEY = (int)WM.USER + 231;

            /// <summary>
            /// Modifier flag to suppress auto‑repeat on hotkey registrations.
            /// </summary>
            private const uint MOD_NOREPEAT = 0x4000;

            private static void UnregisterExplorerHotkey(IntPtr trayWnd, int id)
            {
                // Send message to explorer to unregister its hotkey
                if (trayWnd != IntPtr.Zero)
                {
                    SendMessage(trayWnd,
                        EXPLORER_UNREGISTER_HOTKEY,
                        (IntPtr)id,
                        IntPtr.Zero
                    );
                }
            }

            private void UnregisterExplorerHotkeys()
            {
                // Send message to explorer to unregister its hotkeys
                IntPtr trayWnd = WindowHelper.FindWindowsTray(WindowHelper.FindWindowsTray(IntPtr.Zero));
                int firstId = ExplorerHotkeyTable.GetIndex() + 500;

                if (trayWnd != IntPtr.Zero)
                {
                    UnregisterExplorerHotkey(trayWnd, firstId);
                    UnregisterExplorerHotkey(trayWnd, firstId + 1);
                    UnregisterExplorerHotkey(trayWnd, firstId + 2);
                    UnregisterExplorerHotkey(trayWnd, firstId + 3);
                    UnregisterExplorerHotkey(trayWnd, firstId + 4);
                    UnregisterExplorerHotkey(trayWnd, firstId + 5);
                    UnregisterExplorerHotkey(trayWnd, firstId + 6);
                    UnregisterExplorerHotkey(trayWnd, firstId + 7);
                    UnregisterExplorerHotkey(trayWnd, firstId + 8);
                    UnregisterExplorerHotkey(trayWnd, firstId + 9);
                }
            }

            private readonly System.Collections.Generic.HashSet<int> _registeredHotkeys = [];

            private bool RegisterWinKey(VK key, int id)
            {
                bool success = RegisterHotKey(
                    Handle,
                    id,
                    (uint)System.Windows.Input.ModifierKeys.Windows | MOD_NOREPEAT,
                    (uint)key);

                if (success)
                {
                    _registeredHotkeys.Add(id);
                    ShellLogger.Debug($"HotkeyManager: Registered Win+{key} (id {id})");
                }
                else
                {
                    ShellLogger.Warning($"HotkeyManager: Failed to register Win+{key} (id {id})");
                }

                return success;
            }

            public void RegisterHotkeys()
            {
                // Remove explorer hotkeys
                UnregisterExplorerHotkeys();

                // Reset any prior registrations
                _registeredHotkeys.Clear();

                // Try to register Win[0-9] hotkeys for RetroBar
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

            public void UnregisterHotkeys()
            {
                // Only unregister those we actually registered
                foreach (int id in _registeredHotkeys)
                {
                    UnregisterHotKey(Handle, id);
                }

                _registeredHotkeys.Clear();
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}