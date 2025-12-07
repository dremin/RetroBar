using System;

namespace ManagedShell.ShellFolders.Enums
{
    [Flags]
    public enum STGM
    {
        READ = 0x0,
        WRITE = 0x1,
        READWRITE = 0x2,
        SHARE_DENY_NONE = 0x40,
        SHARE_DENY_READ = 0x30,
        SHARE_DENY_WRITE = 0x20,
        SHARE_EXCLUSIVE = 0x10,
        PRIORITY = 0x40000,
        CREATE = 0x1000,
        CONVERT = 0x20000,
        FAILIFTHERE = 0x0,
        DIRECT = 0x0,
        TRANSACTED = 0x10000,
        NOSCRATCH = 0x100000,
        NOSNAPSHOT = 0x200000,
        SIMPLE = 0x8000000,
        DIRECT_SWMR = 0x400000,
        DELETEONRELEASE = 0x4000000
    }
}
