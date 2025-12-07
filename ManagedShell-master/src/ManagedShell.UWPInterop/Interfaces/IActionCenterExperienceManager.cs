using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("df65db57-d504-456e-8bd7-004ce308d8d9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActionCenterExperienceManager
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void HotKeyInvoked(int kind);
    }
}
