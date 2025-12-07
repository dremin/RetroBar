using System;

namespace ManagedShell.ShellFolders.Enums
{
    // Defines the values used with the IShellFolder::GetDisplayNameOf and IShellFolder::SetNameOf 
    // methods to specify the type of file or folder names used by those methods
    [Flags]
    public enum SHGDN
    {
        NORMAL = 0x0000,
        INFOLDER = 0x0001,
        FOREDITING = 0x1000,
        FORADDRESSBAR = 0x4000,
        FORPARSING = 0x8000
    }
}
