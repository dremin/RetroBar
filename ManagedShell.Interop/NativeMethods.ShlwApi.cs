using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        const string ShlwApi_DllName = "shlwapi.dll";

        [DllImport(ShlwApi_DllName)]
        public static extern IntPtr SHLockShared(IntPtr hData, uint dwProcessId);

        [DllImport(ShlwApi_DllName, SetLastError = true)]
        public static extern bool SHUnlockShared(IntPtr pvData);
    }
}
