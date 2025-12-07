using System;
using System.Runtime.InteropServices;

namespace ManagedShell.UWPInterop
{
    internal class NativeMethods
    {
        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int WindowsCreateString(
            [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
            int length,
            ref IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int WindowsDeleteString(IntPtr hstring);
    }
}
