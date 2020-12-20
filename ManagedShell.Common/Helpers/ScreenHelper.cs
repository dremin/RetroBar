using System.Drawing;
using ManagedShell.Interop;

namespace ManagedShell.Common.Helpers
{
    public class ScreenHelper
    {
        public static Size PrimaryMonitorDeviceSize => new Size(NativeMethods.GetSystemMetrics(0), NativeMethods.GetSystemMetrics(1));
    }
}
