using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("b1604325-6b59-427b-bf1b-80a2db02d3d8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITrayClockFlyoutExperienceManager
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void ShowFlyout(Windows.Foundation.Rect rect);
        void HideFlyout();
    }
}
