using System.Runtime.InteropServices;
using ManagedShell.Interop;

namespace ManagedShell.ShellFolders.Structs
{
    // Contains extended parameters for the TrackPopupMenuEx function
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct TPMPARAMS
    {
        int cbSize;
        NativeMethods.Rect rcExclude;
    }
}
