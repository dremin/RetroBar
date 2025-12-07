using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        const string ShlwApi_DllName = "shlwapi.dll";

        [DllImport(ShlwApi_DllName)]
        public static extern IntPtr SHAllocShared(IntPtr hData, uint dwSize, uint dwProcessId);

        [DllImport(ShlwApi_DllName)]
        public static extern IntPtr SHFreeShared(IntPtr hData, uint dwProcessId);

        [DllImport(ShlwApi_DllName)]
        public static extern IntPtr SHLockShared(IntPtr hData, uint dwProcessId);

        [DllImport(ShlwApi_DllName, SetLastError = true)]
        public static extern bool SHUnlockShared(IntPtr pvData);

        [DllImport(ShlwApi_DllName, BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        public static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);
    }
}
