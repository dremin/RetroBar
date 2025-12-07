using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        // Undocumented dark mode API for 1903 and later
        // https://github.com/ysc3839/win32-darkmode
        
        public enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        };

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        public static extern bool SetPreferredAppMode(PreferredAppMode mode);

        [DllImport("uxtheme.dll", EntryPoint = "#133")]
        public static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);
    }
}
