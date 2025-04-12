using ManagedShell;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;
using System;
using System.Diagnostics;
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
                IntPtr trayWnd = NativeMethods.FindWindow("Shell_TrayWnd", "");

                if (trayWnd != IntPtr.Zero)
                {
                    for (int i = 0x205; i <= 0x20e; i++) // 0x205-0x20e are Shell_TrayWnd's quick switch hotkey IDs
                    {
                        NativeMethods.SendMessage(trayWnd, 
                            (int)NativeMethods.WM.USER + 231, // Requests Shell_TrayWnd to unregister hotkeys
                            (IntPtr)i, 
                            IntPtr.Zero
                        );
                    }
                }

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

            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}
