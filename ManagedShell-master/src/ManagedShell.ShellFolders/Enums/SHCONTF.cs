using System;

namespace ManagedShell.ShellFolders.Enums
{
    // Determines the type of items included in an enumeration. 
    // These values are used with the IShellFolder::EnumObjects method
    [Flags]
    public enum SHCONTF
    {
        FOLDERS = 0x0020,
        NONFOLDERS = 0x0040,
        INCLUDEHIDDEN = 0x0080,
        INIT_ON_FIRST_NEXT = 0x0100,
        NETPRINTERSRCH = 0x0200,
        SHAREABLE = 0x0400,
        STORAGE = 0x0800,
    }
}
