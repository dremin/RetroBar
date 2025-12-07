using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop.Interfaces
{
    [ComImport, Guid("2e8fcb18-a0ee-41ad-8ef8-77fb3a370ca5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellExperienceManagerFactory
    {
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        void GetExperienceManager(IntPtr hStrExperience, out IntPtr pp);
    }
}
