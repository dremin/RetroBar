using System;
using System.Runtime.InteropServices;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Structs;

namespace ManagedShell.ShellFolders.Interfaces
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214e4-0000-0000-c000-000000000046")]
    public interface IContextMenu
    {
        // Adds commands to a shortcut menu
        [PreserveSig()]
        Int32 QueryContextMenu(
            IntPtr hmenu,
            uint iMenu,
            uint idCmdFirst,
            uint idCmdLast,
            CMF uFlags);

        // Carries out the command associated with a shortcut menu item
        [PreserveSig()]
        Int32 InvokeCommand(
            ref CMINVOKECOMMANDINFOEX info);

        // Retrieves information about a shortcut menu command, 
        // including the help string and the language-independent, 
        // or canonical, name for the command
        [PreserveSig()]
        Int32 GetCommandString(
            uint idcmd,
            GCS uflags,
            uint reserved,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] commandstring,
            int cch);
    }
}
