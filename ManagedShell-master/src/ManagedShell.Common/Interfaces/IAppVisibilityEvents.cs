using System;
using System.Runtime.InteropServices;
using ManagedShell.Common.Enums;

namespace ManagedShell.Common.Interfaces
{
    [ComImport, Guid("6584CE6B-7D82-49C2-89C9-C6BC02BA8C38"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppVisibilityEvents
    {
        long AppVisibilityOnMonitorChanged(
            [In] IntPtr hMonitor,
            [In] MONITOR_APP_VISIBILITY previousMode,
            [In] MONITOR_APP_VISIBILITY currentMode);

        long LauncherVisibilityChange([In] bool currentVisibleState);
    }
}
