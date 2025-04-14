using ManagedShell;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RetroBar.Utilities
{
    public class ExplorerMonitor : IDisposable
    {
        public ExplorerMonitorWindow explorerMonitorWindow;

        public void ExplorerMonitorStart(WindowManager windowManagerRef, ShellManager shellManager)
        {
            if (explorerMonitorWindow != null) { return; } // Prevent multiple monitors.

            explorerMonitorWindow = new ExplorerMonitorWindow(windowManagerRef, shellManager); // Start monitoring.

            if (Settings.Instance.OverrideHotkeys) explorerMonitorWindow.RegisterHotkeys();
        }

        public void Dispose()
        {
            if (Settings.Instance.OverrideHotkeys) explorerMonitorWindow.UnregisterHotkeys();
            explorerMonitorWindow?.Dispose();
        }

        public class ExplorerMonitorWindow : NativeWindow, IDisposable
        {
            private readonly ShellManager _shellManager;
            private readonly WindowManager _windowManagerRef;
            private static readonly int WM_TASKBARCREATEDMESSAGE = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            public ExplorerMonitorWindow(WindowManager windowManagerRef, ShellManager shellManager)
            {
                _shellManager = shellManager;
                _windowManagerRef = windowManagerRef;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if ((m.Msg == (int)NativeMethods.WM.HOTKEY))
                {
                    int hotkeyId = (int)m.WParam;

                    if (hotkeyId >= 0 && hotkeyId <= 9) // 0-9: Quick switch hotkeys
                    {
                        try
                        {
                            bool exists = _shellManager.Tasks.GroupedWindows.MoveCurrentToPosition(hotkeyId);

                            if (exists)
                            {
                                ApplicationWindow window = _shellManager.Tasks.GroupedWindows.CurrentItem as ApplicationWindow;

                                if (window.State == ApplicationWindow.WindowState.Active && window.CanMinimize)
                                {
                                    window.Minimize();
                                }
                                else
                                {
                                    window.BringToFront();
                                }
                            }
                        }
                        catch (ArgumentOutOfRangeException) { }
                    }
                }
                else if (m.Msg == WM_TASKBARCREATEDMESSAGE)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            // Reopen taskbars if explorer.exe is restarted.
                            _windowManagerRef.ReopenTaskbars();
                            // Re-initialize the tasks service to prevent leftover File Explorer windows.
                            _shellManager.Tasks.Dispose();
                            _shellManager.Tasks.Initialize(true);
                        }
                        catch (Exception ex)
                        {
                            ShellLogger.Warning($"Error handling TaskbarCreated message on ExplorerMonitor: {ex.Message}");
                        }
                    });
                }
                
                base.WndProc(ref m); // Call the base class to process other messages so we dont accidentally cause crashes or bugs.
            }
            
            public void RegisterHotkeys()
            {
                // Send message to explorer to unregister its hotkeys

                IntPtr trayWnd = WindowHelper.FindWindowsTray(IntPtr.Zero);
                int firstId = FindExplorerTaskbarHotkeyIndex() + 500;

                if (trayWnd != IntPtr.Zero)
                {
                    for (int i = firstId; i <= firstId + 10; i++)
                    {
                        NativeMethods.SendMessage(trayWnd, 
                            (int)NativeMethods.WM.USER + 231,
                            (IntPtr)i, 
                            IntPtr.Zero
                        );
                    }
                }

                // Register hotkeys in the ExplorerMonitorWindow

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
            }

            public void UnregisterHotkeys()
            {
                for (int i = 0; i < 10; i++)
                {
                    NativeMethods.UnregisterHotKey(Handle, i);
                }
            }

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


            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}
