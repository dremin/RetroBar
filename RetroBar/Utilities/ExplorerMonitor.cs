using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RetroBar.Utilities
{
    public class ExplorerMonitor : IDisposable
    {
        private ExplorerMonitorWindow _explorerMonitorWindow;

        public void ExplorerMonitorStart(WindowManager windowManagerRef)
        {
            if (_explorerMonitorWindow != null) { return; } // Prevent multiple monitors.

            _explorerMonitorWindow = new ExplorerMonitorWindow(windowManagerRef); // Start monitoring.
        }

        public void Dispose()
        {
            _explorerMonitorWindow?.Dispose();
        }

        private class ExplorerMonitorWindow : NativeWindow, IDisposable
        {
            private readonly WindowManager _windowManagerRef;
            private static readonly int WM_TASKBARCREATEDMESSAGE = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            public ExplorerMonitorWindow(WindowManager windowManagerRef)
            {
                _windowManagerRef = windowManagerRef;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_TASKBARCREATEDMESSAGE)
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(() => {
                        try
                        {
                            _windowManagerRef.ReopenTaskbars(); // Reopen taskbars if explorer.exe is restarted.
                        }
                        catch (Exception ex)
                        {
                            ShellLogger.Warning($"Error handling TaskbarCreated message on ExplorerMonitor: {ex.Message}");
                        }
                    });
                }

                base.WndProc(ref m); // Call the base class to process other messages so we dont accidentally cause crashes or bugs.
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}
