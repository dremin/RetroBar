using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ManagedShell.ShellFolders.Interfaces;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Structs;
using NativeMethods = ManagedShell.Interop.NativeMethods;

namespace ManagedShell.ShellFolders
{
    public abstract class ShellContextMenu : NativeWindow
    {
        // Properties
        protected List<ShellNewMenuCommand> ShellNewMenus = new List<ShellNewMenuCommand>();
        
        internal IContextMenu iContextMenu;
        internal IContextMenu2 iContextMenu2;
        internal IContextMenu3 iContextMenu3;

        internal IntPtr iContextMenuPtr;
        internal IntPtr iContextMenu2Ptr;
        internal IntPtr iContextMenu3Ptr;

        internal IntPtr nativeMenuPtr;

        protected int x;
        protected int y;

        #region Helpers
        internal void FreeResources()
        {
            if (iContextMenu != null)
            {
                Marshal.FinalReleaseComObject(iContextMenu);
                iContextMenu = null;
            }

            if (iContextMenu2 != null)
            {
                Marshal.FinalReleaseComObject(iContextMenu2);
                iContextMenu2 = null;
            }

            if (iContextMenu3 != null)
            {
                Marshal.FinalReleaseComObject(iContextMenu3);
                iContextMenu3 = null;
            }

            if (iContextMenuPtr != IntPtr.Zero)
            {
                Marshal.Release(iContextMenuPtr);
                iContextMenuPtr = IntPtr.Zero;
            }

            if (iContextMenu2Ptr != IntPtr.Zero)
            {
                Marshal.Release(iContextMenu2Ptr);
                iContextMenu2Ptr = IntPtr.Zero;
            }

            if (iContextMenu3Ptr != IntPtr.Zero)
            {
                Marshal.Release(iContextMenu3Ptr);
                iContextMenu3Ptr = IntPtr.Zero;
            }

            if (nativeMenuPtr != IntPtr.Zero)
            {
                Interop.DestroyMenu(nativeMenuPtr);
                nativeMenuPtr = IntPtr.Zero;
            }
        }
        
        protected string GetCommandString(IContextMenu iContextMenu, uint idcmd, bool executeString)
        {
            string command = GetCommandStringW(iContextMenu, idcmd, executeString);

            if (string.IsNullOrEmpty(command))
                command = GetCommandStringA(iContextMenu, idcmd, executeString);

            return command;
        }

        /// <summary>
        /// Retrieves the command string for a specific item from an iContextMenu (Ansi)
        /// </summary>
        /// <param name="iContextMenu">the IContextMenu to receive the string from</param>
        /// <param name="idcmd">the id of the specific item</param>
        /// <param name="executeString">indicating whether it should return an execute string or not</param>
        /// <returns>if executeString is true it will return the executeString for the item, 
        /// otherwise it will return the help info string</returns>
        private string GetCommandStringA(IContextMenu iContextMenu, uint idcmd, bool executeString)
        {
            string info = string.Empty;
            byte[] bytes = new byte[256];
            int index;

            iContextMenu.GetCommandString(
                idcmd,
                (executeString ? GCS.VERBA : GCS.HELPTEXTA),
                0,
                bytes,
                256);

            index = 0;
            while (index < bytes.Length && bytes[index] != 0)
            { index++; }

            if (index < bytes.Length)
                info = Encoding.Default.GetString(bytes, 0, index);

            return info;
        }

        /// <summary>
        /// Retrieves the command string for a specific item from an iContextMenu (Unicode)
        /// </summary>
        /// <param name="iContextMenu">the IContextMenu to receive the string from</param>
        /// <param name="idcmd">the id of the specific item</param>
        /// <param name="executeString">indicating whether it should return an execute string or not</param>
        /// <returns>if executeString is true it will return the executeString for the item, 
        /// otherwise it will return the help info string</returns>
        private string GetCommandStringW(IContextMenu iContextMenu, uint idcmd, bool executeString)
        {
            string info = string.Empty;
            byte[] bytes = new byte[256];
            int index;

            iContextMenu.GetCommandString(
                idcmd,
                (executeString ? GCS.VERBW : GCS.HELPTEXTW),
                0,
                bytes,
                256);

            index = 0;
            while (index < bytes.Length - 1 && (bytes[index] != 0 || bytes[index + 1] != 0))
            { index += 2; }

            if (index < bytes.Length - 1)
                info = Encoding.Unicode.GetString(bytes, 0, index);

            return info;
        }

        /// <summary>
        /// Invokes a specific command from an IContextMenu
        /// </summary>
        /// <param name="iContextMenu">the IContextMenu containing the item</param>
        /// <param name="workingDir">the parent directory from where to invoke</param>
        /// <param name="cmd">the index of the command to invoke</param>
        /// <param name="ptInvoke">the point (in screen coordinates) from which to invoke</param>
        protected void InvokeCommand(IContextMenu iContextMenu, string workingDir, uint cmd, Point ptInvoke)
        {
            CMINVOKECOMMANDINFOEX invoke = new CMINVOKECOMMANDINFOEX();
            invoke.cbSize = Interop.cbInvokeCommand;
            invoke.lpVerb = (IntPtr)cmd;
            invoke.lpVerbW = (IntPtr)cmd;
            invoke.lpDirectory = workingDir;
            invoke.lpDirectoryW = workingDir;
            invoke.fMask = CMIC.ASYNCOK | CMIC.FLAG_LOG_USAGE | CMIC.UNICODE | CMIC.PTINVOKE |
                ((Control.ModifierKeys & Keys.Control) != 0 ? CMIC.CONTROL_DOWN : 0) |
                ((Control.ModifierKeys & Keys.Shift) != 0 ? CMIC.SHIFT_DOWN : 0);
            invoke.ptInvoke = new NativeMethods.POINT(ptInvoke.X, ptInvoke.Y);
            invoke.nShow = NativeMethods.WindowShowStyle.ShowNormal;

            iContextMenu.InvokeCommand(ref invoke);
        }
        #endregion

        /// <summary>
        /// This method receives WindowMessages. It will make the "Open With" and "Send To" work 
        /// by calling HandleMenuMsg and HandleMenuMsg2.
        /// </summary>
        /// <param name="m">the Message of the Browser's WndProc</param>
        /// <returns>true if the message has been handled, false otherwise</returns>
        protected override void WndProc(ref Message m)
        {
            if (iContextMenu != null &&
                m.Msg == (int)NativeMethods.WM.MENUSELECT &&
                ((int)Interop.HiWord(m.WParam) & (int)MFT.SEPARATOR) == 0 &&
                ((int)Interop.HiWord(m.WParam) & (int)MFT.POPUP) == 0)
            {
                string info = GetCommandString(
                    iContextMenu,
                    (uint)Interop.LoWord(m.WParam) - Interop.CMD_FIRST,
                    false);
            }

            if (iContextMenu2 != null &&
                (m.Msg == (int)NativeMethods.WM.INITMENUPOPUP ||
                    m.Msg == (int)NativeMethods.WM.MEASUREITEM ||
                    m.Msg == (int)NativeMethods.WM.DRAWITEM))
            {
                if (iContextMenu2.HandleMenuMsg(
                    (uint)m.Msg, m.WParam, m.LParam) == NativeMethods.S_OK)
                    return;
            }

            if (iContextMenu3 != null &&
                m.Msg == (int)NativeMethods.WM.MENUCHAR)
            {
                if (iContextMenu3.HandleMenuMsg2(
                    (uint)m.Msg, m.WParam, m.LParam, IntPtr.Zero) == NativeMethods.S_OK)
                    return;
            }
            
            foreach (var subMenu in ShellNewMenus)
            {
                if (m.WParam != subMenu.nativeMenuPtr)
                {
                    continue;
                }
                
                if (subMenu.iContextMenu2 != null &&
                    (m.Msg == (int)NativeMethods.WM.INITMENUPOPUP ||
                        m.Msg == (int)NativeMethods.WM.MEASUREITEM ||
                        m.Msg == (int)NativeMethods.WM.DRAWITEM))
                {
                    if (subMenu.iContextMenu2.HandleMenuMsg(
                        (uint)m.Msg, m.WParam, m.LParam) == NativeMethods.S_OK)
                        return;
                }

                if (subMenu.iContextMenu3 != null &&
                    m.Msg == (int)NativeMethods.WM.MENUCHAR)
                {
                    if (subMenu.iContextMenu3.HandleMenuMsg2(
                        (uint)m.Msg, m.WParam, m.LParam, IntPtr.Zero) == NativeMethods.S_OK)
                        return;
                }
            }

            base.WndProc(ref m);
        }
    }
}
