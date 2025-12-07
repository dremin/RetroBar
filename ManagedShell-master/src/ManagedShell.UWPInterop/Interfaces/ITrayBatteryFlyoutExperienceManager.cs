using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("0a73aedc-1c68-410d-8d53-63af80951e8f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITrayBatteryFlyoutExperienceManager
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void ShowFlyout(Windows.Foundation.Rect rect);
        void HideFlyout();
    }
}
