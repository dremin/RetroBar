using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("d669a58e-6b18-4d1d-9004-a8862adb0a20"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IControlCenterExperienceManager
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void HotKeyInvoked(int kind);
    }
}
