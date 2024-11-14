using ManagedShell.Interop;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RetroBar.Utilities
{
    public class ExplorerMonitor : IDisposable
    {
        private bool _explorerMonitorisMonitoring;
        private ExplorerMonitorWindow _explorerMonitorWindow;

        public void ExplorerMonitorStart(WindowManager windowManagerRef)
        {
            if (_explorerMonitorisMonitoring) { return; } // Prevent multiple monitors.

            _explorerMonitorisMonitoring = true;
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

            public ExplorerMonitorWindow(WindowManager windowManager)
            {
                _windowManagerRef = windowManager;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_TASKBARCREATEDMESSAGE)
                {
                    try
                    {
                        _windowManagerRef.ReopenTaskbars();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error handling TaskbarCreated message on ExplorerMonitor: {ex.Message}");
                    }
                }

                base.WndProc(ref m);
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}
