using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        const string Gdi32_DllName = "gdi32.dll";

        [DllImport(Gdi32_DllName)]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
