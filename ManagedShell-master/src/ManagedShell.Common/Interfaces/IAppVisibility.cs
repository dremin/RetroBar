using System;
using System.Runtime.InteropServices;
using ManagedShell.Common.Enums;

namespace ManagedShell.Common.Interfaces
{
    [ComImport, Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppVisibility
    {
        long GetAppVisibilityOnMonitor([In] IntPtr hMonitor, [Out] out MONITOR_APP_VISIBILITY pMode);
        long IsLauncherVisible([Out] out bool pfVisible);
        long Advise([In] IAppVisibilityEvents pCallback, [Out] out int pdwCookie);
        long Unadvise([In] int dwCookie);
    }
}
