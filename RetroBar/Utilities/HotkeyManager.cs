using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RetroBar.Utilities
{
    public class HotkeyManager : IDisposable
    {
        public HotkeyListenerWindow listenerWindow;

        public HotkeyManager()
        {
            listenerWindow = new HotkeyListenerWindow(this);
            
            if (Settings.Instance.WinNumHotkeysAction != WinNumHotkeysOption.WindowsDefault) listenerWindow.RegisterHotkeys();
            
            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void Dispose()
        {
            if (listenerWindow.IsRegistered) listenerWindow.UnregisterHotkeys();

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
            if (e.PropertyName == nameof(Settings.WinNumHotkeysAction))
            {
                if (!listenerWindow.IsRegistered && Settings.Instance.WinNumHotkeysAction != WinNumHotkeysOption.WindowsDefault)
                {
                    listenerWindow.RegisterHotkeys();
                }
                else if (listenerWindow.IsRegistered && Settings.Instance.WinNumHotkeysAction == WinNumHotkeysOption.WindowsDefault)
                {
                    listenerWindow.UnregisterHotkeys();
                }
            }
        }
        #endregion

        public class HotkeyListenerWindow : NativeWindow, IDisposable
        {
            internal bool IsRegistered;

            private HotkeyManager _manager;

            public HotkeyListenerWindow(HotkeyManager manager)
            {
                CreateHandle(new CreateParams());
                _manager = manager;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == (int)NativeMethods.WM.HOTKEY)
                {
                    int hotkeyId = (int)m.WParam;

                    if (hotkeyId >= 0 && hotkeyId <= 9) // 0-9: Quick switch hotkeys
                    {
                        TaskbarHotkeyEventArgs args = new TaskbarHotkeyEventArgs { index = hotkeyId };
                        _manager.TaskbarHotkeyPressed?.Invoke(this, args);
                    }
                }

                base.WndProc(ref m);
            }

            #region Explorer hotkey table search stuff

            readonly byte[] explorerHotkeySearchPattern = { // 4 entries should be enough to uniquely identify the hotkey table
                0x31,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                0x32,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                0x33,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
                0x34,0x00,0x00,0x00, 0x08,0x00,0x00,0x00,
            };

            const int explorerHotkeyIndexDefault = 17; // Current index as of 24H2, used as a backup in case the index can't be determined

            // Finds the index of the first taskbar hotkey in explorer's hotkey table
            // See https://github.com/dremin/RetroBar/pull/1128#issuecomment-2802222222 for more info
            private int FindExplorerTaskbarHotkeyIndex()
            {
                int index = 0;

                try
                {
                    string sysroot = Environment.GetEnvironmentVariable("SystemRoot");
                    byte[] explorerBytes = File.ReadAllBytes(sysroot + "\\explorer.exe");

                    // Search for the 1234 hotkeys in the exe to find them in the table

                    int patternOffset = explorerBytes.AsSpan().IndexOf(explorerHotkeySearchPattern);
                    if (patternOffset < 0) throw new Exception("Couldn't find hotkey pattern in explorer.exe");

                    // Move backwards in the table until you hit zeroes, then you have the index

                    using (MemoryStream ms = new MemoryStream(explorerBytes))
                    {
                        ms.Seek(patternOffset, SeekOrigin.Begin);
                        ms.Seek(-8, SeekOrigin.Current);

                        byte[] readBuf = new byte[8];
                        byte[] zeros = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                        while (true)
                        {
                            ms.Read(readBuf, 0, 8);
                            ms.Seek(-16, SeekOrigin.Current);

                            if (Enumerable.SequenceEqual(readBuf, zeros) || ms.Position <= 0) break;

                            index++;
                        }
                    }

                }
                catch (Exception e)
                {
                    ShellLogger.Warning(String.Format("Failed to find taskbar hotkey index in explorer: {0}", e.ToString()));
                    index = explorerHotkeyIndexDefault;
                }

                return index;
            }

            #endregion

            public void RegisterHotkeys()
            {
                // Send message to explorer to unregister its hotkeys

                IntPtr trayWnd = WindowHelper.FindWindowsTray(IntPtr.Zero);
                int firstId = FindExplorerTaskbarHotkeyIndex() + 500;

                if (trayWnd != IntPtr.Zero)
                {
                    for (int i = firstId; i < firstId + 10; i++)
                    {
                        NativeMethods.SendMessage(trayWnd,
                            (int)NativeMethods.WM.USER + 231,
                            (IntPtr)i,
                            IntPtr.Zero
                        );
                    }
                }

                // Register RetroBar hotkeys

                for (int i = 0; i < 10; i++)
                {
                    uint keycode = i == 9 ? 0x30 : 0x31 + (uint)i;

                    NativeMethods.RegisterHotKey( // 0-9: Quick switch hotkeys
                        Handle,
                        i,
                        0x4008, // MOD_WIN | MOD_NOREPEAT
                        keycode
                    );
                }

                IsRegistered = true;
            }

            public void UnregisterHotkeys()
            {
                // Unregister RetroBar hotkeys

                for (int i = 0; i < 10; i++)
                {
                    NativeMethods.UnregisterHotKey(Handle, i);
                }

                // TODO: Restart explorer so it registers the hotkeys again
                // Couldn't figure out a way to do this that wouldn't clash with ManagedShell, sry :(

                IsRegistered = false;
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }

    }
}
