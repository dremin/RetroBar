using System;
using System.Runtime.InteropServices;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Interfaces;

namespace ManagedShell.Common.SupportingClasses
{
    class AppVisibilityEvents : IAppVisibilityEvents
    {
        internal event EventHandler<AppVisibilityEventArgs> AppVisibilityChanged;
        internal event EventHandler<LauncherVisibilityEventArgs> LauncherVisibilityChanged;

        public AppVisibilityEvents() { }

        public long AppVisibilityOnMonitorChanged([In] IntPtr hMonitor, [In] MONITOR_APP_VISIBILITY previousMode, [In] MONITOR_APP_VISIBILITY currentMode)
        {
            AppVisibilityEventArgs args = new AppVisibilityEventArgs
            {
                MonitorHandle = hMonitor,
                PreviousMode = previousMode,
                CurrentMode = currentMode
            };

            AppVisibilityChanged?.Invoke(this, args);
            return 0;
        }

        public long LauncherVisibilityChange([In] bool currentVisibleState)
        {
            LauncherVisibilityEventArgs args = new LauncherVisibilityEventArgs
            {
                Visible = currentVisibleState
            };

            LauncherVisibilityChanged?.Invoke(this, args);
            return 0;
        }
    }
}
