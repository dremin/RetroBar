using System;
using ManagedShell.Common.Enums;

namespace ManagedShell.Common.SupportingClasses
{
    public class AppVisibilityEventArgs : EventArgs
    {
        public IntPtr MonitorHandle;
        public MONITOR_APP_VISIBILITY PreviousMode;
        public MONITOR_APP_VISIBILITY CurrentMode;
    }
}
