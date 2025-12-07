using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("e44f17e6-ab85-409c-8d01-17d74bec150e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface INetworkFlyoutExperienceManager
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void ShowFlyout(Windows.Foundation.Rect rect);
        void HideFlyout();
    }
}
