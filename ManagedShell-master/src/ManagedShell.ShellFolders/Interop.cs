using System;
using System.Runtime.InteropServices;
using ManagedShell.Interop;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Interfaces;
using ManagedShell.ShellFolders.Structs;

namespace ManagedShell.ShellFolders
{
    class Interop
    {
        public const uint CMD_FIRST = 1;
        public const uint CMD_LAST = 30000;
        public static Guid IID_IContextMenu = new Guid("{000214e4-0000-0000-c000-000000000046}");
        public static Guid IID_IContextMenu2 = new Guid("{000214f4-0000-0000-c000-000000000046}");
        public static Guid IID_IContextMenu3 = new Guid("{bcfce0a0-ec17-11d0-8d10-00a0c90f2719}");
        public static Guid IID_IShellExtInit = new Guid("{000214e8-0000-0000-c000-000000000046}");
        public static Guid CLSID_NewMenu = new Guid("{D969A300-E7FF-11d0-A93B-00A0C90F2719}");
        public static int cbInvokeCommand = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
        public static ComTaskScheduler ShellItemScheduler = new ComTaskScheduler();

        /// <summary>
        /// Retrieves the High Word of a WParam of a WindowMessage
        /// </summary>
        /// <param name="ptr">The pointer to the WParam</param>
        /// <returns>The unsigned integer for the High Word</returns>
        public static ulong HiWord(IntPtr ptr)
        {
            if (((ulong)ptr & 0x80000000) == 0x80000000)
                return ((ulong)ptr >> 16);
            else
                return ((ulong)ptr >> 16) & 0xffff;
        }

        /// <summary>
        /// Retrieves the Low Word of a WParam of a WindowMessage
        /// </summary>
        /// <param name="ptr">The pointer to the WParam</param>
        /// <returns>The unsigned integer for the Low Word</returns>
        public static ulong LoWord(IntPtr ptr)
        {
            return (ulong)ptr & 0xffff;
        }
        
        // Retrieves the IShellFolder interface for the desktop folder,
        // which is the root of the Shell's namespace. 
        [DllImport("shell32.dll")]
        public static extern int SHGetDesktopFolder(out IntPtr ppshf);

        [DllImport("shell32.dll")]
        public static extern int SHGetIDListFromObject(IShellItem punk, out IntPtr ppidl);

        [DllImport("shell32.dll")]
        public static extern int SHGetIDListFromObject(IShellFolder punk, out IntPtr ppidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromIDList(
            [In] IntPtr pidl,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItemImageFactory ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromIDList(
            [In] IntPtr pidl,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemWithParent(
            [In] IntPtr pidlParent,
            [In] IShellFolder psfParent,
            [In] IntPtr pidl,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 3)] out IShellItem ppv);
        // appends a new item to the end of the specified menu bar, drop-down menu, submenu, 
        // or shortcut menu. You can use this function to specify the content, appearance, and 
        // behavior of the menu item
        [DllImport("user32",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        public static extern bool AppendMenu(
            IntPtr hMenu,
            MFT uFlags,
            uint uIDNewItem,
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpNewItem);
        
        [DllImport("user32",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        public static extern bool SetMenuDefaultItem(
            IntPtr hMenu,
            uint uItem,
            uint fByPos);

        // Retrieves a handle to the drop-down menu or submenu activated by the specified menu item
        [DllImport("user32",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        public static extern IntPtr GetSubMenu(
            IntPtr hMenu,
            int nPos);

        // Retrieves a drag/drop helper interface for drawing the drag/drop images
        [DllImport("ole32.dll",
            CharSet = CharSet.Auto,
            SetLastError = true)]
        public static extern int CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            CLSCTX dwClsContext,
            ref Guid riid,
            out IntPtr ppv);

        // Displays a shortcut menu at the specified location and 
        // tracks the selection of items on the shortcut menu
        [DllImport("user32.dll",
            ExactSpelling = true,
            CharSet = CharSet.Auto)]
        public static extern uint TrackPopupMenuEx(
            IntPtr hmenu,
            TPM flags,
            int x,
            int y,
            IntPtr hwnd,
            IntPtr lptpm);

        // Creates a popup-menu. The menu is initially empty, but it can be filled with 
        // menu items by using the InsertMenuItem, AppendMenu, and InsertMenu functions
        [DllImport("user32",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        public static extern IntPtr CreatePopupMenu();

        // Destroys the specified menu and frees any memory that the menu occupies
        [DllImport("user32",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        public static extern bool DestroyMenu(
            IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern uint GetMenuDefaultItem(IntPtr hMenu, uint fByPos, uint gmdiFlags);
    }
}
