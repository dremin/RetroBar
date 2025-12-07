using System;
using System.Runtime.InteropServices;

namespace ManagedShell.ShellFolders.Interfaces
{
    [ComImport]
    [Guid("B3A4B685-B685-4805-99D9-5DEAD2873236")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IParentAndItem
    {
        int SetParentAndItem(IntPtr pidlParent, IShellFolder psf, IntPtr pidlChild);

        int GetParentAndItem(out IntPtr ppidlParent, out IShellFolder ppsf, out IntPtr ppidlChild);
    }
}
