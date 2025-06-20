﻿using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Manages application hotkeys by overriding Explorer's built-in Win + optional modifier + VK hotkeys
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly HotkeyListenerWindow _listenerWindow;

        private const int TOGGLE_TASKBAR_HOTKEY_ID = 9999;
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
            if (Settings.Instance.EnableTaskbarToggleHotkey)
            {
                _listenerWindow.RegisterToggleAutoHide();
            }
            else if (!Settings.Instance.EnableTaskbarToggleHotkey)
            {
                _listenerWindow.UnregisterHotkeys();
                ShellLogger.Debug("Alt+T toggle is disabled by settings.");
            }
        }
        #endregion

        private class HotkeyListenerWindow : NativeWindow, IDisposable
        {
            internal bool IsRegistered => _registeredHotkeys.Count > 0;
            private readonly HotkeyManager _manager;
            private readonly HashSet<int> _registeredHotkeys = [];
            private const int WMTRAY_UNREGISTERHOTKEY = (int)WM.USER + 231;
            private List<TrayHotkey.Entry> _trayHotkeyTable;
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
                    if (hotkeyId == TOGGLE_TASKBAR_HOTKEY_ID)
                    {
                        // Toggle built-in AutoHide setting
                        Settings.Instance.AutoHide = !Settings.Instance.AutoHide;
                        ShellLogger.Debug($"HotkeyManager: Alt+T toggled AutoHide to {Settings.Instance.AutoHide}");
                        return;
                    }
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

            public void RegisterToggleAutoHide()
            {
                ShellLogger.Debug("HotkeyManager: Registering toggle autohide hotkey");
                // Register Alt+T for toggling taskbar
                bool altTRegistered = RegisterHotKey(
                                        Handle,
                                        TOGGLE_TASKBAR_HOTKEY_ID,
                                        (uint)(MOD.ALT | MOD.NOREPEAT),
                                        (uint)VK.KEY_T);

                if (altTRegistered)
                {
                    _registeredHotkeys.Add(TOGGLE_TASKBAR_HOTKEY_ID);
                    ShellLogger.Info("HotkeyManager: Registered Alt+T for AutoHide toggle");
                }
                else if (Settings.Instance.EnableTaskbarToggleHotkey)
                {
                    ShellLogger.Info("HotkeyManager: Autohide toggled using Alt+T");
                }
                else
                {
                    ShellLogger.Warning("HotkeyManager: FAILED to register Alt+T for AutoHide toggle");
                }
            }

            public void UnregisterToggleAutoHide()
            {
                    UnregisterHotKey(Handle, 9999);
                    ShellLogger.Info($"HotkeyManager: Unregistered Alt+T for AutoHide toggle");
            }

            private void LoadExplorerResources()
            {
                // Initialize with empty table as fallback
                _trayHotkeyTable = [];
                _trayWindow = IntPtr.Zero;

                TryFindTrayWindow();
                TryBuildHotkeyTable();
            }

            private void TryBuildHotkeyTable()
            {
                if (_trayWindow == IntPtr.Zero)
                {
                    return;
                }

                try
                {
                    _trayHotkeyTable = TrayHotkey.BuildTable(_trayWindow);
                    ShellLogger.Debug($"HotkeyManager: Found {_trayHotkeyTable.Count} entries in Explorer hotkey table");
                }
                catch (Exception ex)
                {
                    ShellLogger.Warning($"HotkeyManager: Failed to build Explorer hotkey table - {ex.Message}");
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
                        ShellLogger.Debug($"HotkeyManager: Registered hotkey {modifiers}+{key} with ID={taskIndex}");
                    }
                    else
                    {
                        ShellLogger.Warning($"HotkeyManager: Failed to register hotkey {modifiers}+{key} with ID={taskIndex}");
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

                    // Found a match - send unregister message to Explorer
                    int trayHotkeyId = _trayHotkeyTable[trayHotkeyIndex].Id;
                    SendMessage(_trayWindow, WMTRAY_UNREGISTERHOTKEY, new IntPtr(trayHotkeyId), IntPtr.Zero);
                    ShellLogger.Debug($"HotkeyManager: Sent WMTRAY_UNREGISTERHOTKEY for hotkey ID={trayHotkeyId}");
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
                    ShellLogger.Debug($"HotkeyManager: Unregistered hotkey ID={id}");
                }

                _registeredHotkeys.Clear();

                // TODO: Re-register Explorer hotkeys
                // (maybe sending some undocumented message to the tray again, otherwise, restart Explorer process?)
            }

            public void Dispose() => DestroyHandle();
        }
    }
}