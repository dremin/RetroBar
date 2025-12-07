using System;
using System.Runtime.InteropServices;
using System.Text;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Structs;

namespace ManagedShell.ShellFolders.Interfaces
{
    [ComImport]
    [Guid("000214f4-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IContextMenu2
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
            [MarshalAs(UnmanagedType.LPWStr)]
            StringBuilder commandstring,
            int cch);

        // Allows client objects of the IContextMenu interface to 
        // handle messages associated with owner-drawn menu items
        [PreserveSig]
        Int32 HandleMenuMsg(
            uint uMsg,
            IntPtr wParam,
            IntPtr lParam);
    }
}
