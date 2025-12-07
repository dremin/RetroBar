using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("c9ddc674-b44b-4c67-9d79-2b237d9be05a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface INetworkFlyoutExperienceManager_20H1
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void ShowFlyout(Windows.Foundation.Rect rect, int unk);
        void HideFlyout();
    }
}
