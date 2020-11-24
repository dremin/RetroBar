using System.Runtime.InteropServices;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    /// <summary>
    /// AppBar message data structure type
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TrayHostSizeData
    {
        public Rect rc;
        public int edge;
    }
}
