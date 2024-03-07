using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    public class WindowTiler
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        private const int MDITILE_VERTICAL = 0x0000;
        private const int MDITILE_HORIZONTAL = 0x0001;

        [DllImport("user32.dll")]
        private static extern ushort CascadeWindows(IntPtr hwndParent, uint wHow, IntPtr lpRect, uint cKids, IntPtr[] lpKids);

        [DllImport("user32.dll")]
        private static extern ushort CascadeWindows(IntPtr hwndParent, uint wHow, IntPtr lpRect, uint cKids, IntPtr lpKids);

        [DllImport("user32.dll")]
        private static extern int TileWindows(IntPtr hwndParent, int wHow, IntPtr lpRect, int cKids, IntPtr[] lpKids);

        [DllImport("user32.dll")]
        private static extern int TileWindows(IntPtr hwndParent, int wHow, IntPtr lpRect, int cKids, IntPtr lpKids);

        public static void TileWindows(int wHow, IntPtr[] hwndChildren)
        {
            SaveWindowPositions();

            if (hwndChildren == null)
            {
                TileWindows(GetDesktopWindow(), wHow, IntPtr.Zero, 0, IntPtr.Zero);
            }
            else
            {
                TileWindows(GetDesktopWindow(), wHow, IntPtr.Zero, hwndChildren.Length, hwndChildren);
            }
        }

        public static void TileWindowsHorizontally(IntPtr[] hwndChildren = null)
        {
            TileWindows(MDITILE_HORIZONTAL, hwndChildren);
        }

        public static void TileWindowsVertically(IntPtr[] hwndChildren = null)
        {
            TileWindows(MDITILE_VERTICAL, hwndChildren);
        }

        public static void CascadeWindows(IntPtr[] hWndChildren = null)
        {
            SaveWindowPositions();

            if (hWndChildren == null)
            {
                CascadeWindows(GetDesktopWindow(), 0, IntPtr.Zero, 0, IntPtr.Zero);
            }
            else
            {
                CascadeWindows(GetDesktopWindow(), 0, IntPtr.Zero, (uint)hWndChildren.Length, hWndChildren);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public class WindowInfo
        {
            public Rectangle position;
            public string title;
            public string className;

            public WindowInfo(Rectangle position, string title, string className)
            {
                this.position = position;
                this.title = title;
                this.className = className;
            }
        }

        private static List<WindowInfo> infoList;
        private static List<WindowShowStyle> showStyleList;

        /// <summary>
        /// Returns whether a window is normal (1), minimized (2), or maximized (3).
        /// </summary>
        /// <param name="hWnd">The handle of the window.</param>
        private static WindowShowStyle GetWindowShowStyle(IntPtr hWnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(hWnd, ref placement);
            return placement.showCmd;
        }

        private static void SaveWindowPositions()
        {
            infoList = new List<WindowInfo>();
            infoList.Clear();
            showStyleList = new List<WindowShowStyle>();
            showStyleList.Clear();
            foreach (IntPtr hwnd in GetOpenWindows())
            {
                GetWindowRect(hwnd, out Rect rect);
                StringBuilder title = new StringBuilder(255);
                GetWindowText(hwnd, title, 255);
                StringBuilder className = new StringBuilder(255);
                GetClassName(hwnd, className, 255);
                infoList.Add(new WindowInfo(new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top), title.ToString(), className.ToString()));
                showStyleList.Add(GetWindowShowStyle(hwnd));
            }
        }

        public static void RestoreWindowPositions()
        {
            if (infoList == null || infoList.Count == 0)
            {
                return;
            }
            for (int i = infoList.Count; i-- > 0;)
            {
                IntPtr hWnd = FindWindow(infoList[i].className, infoList[i].title);
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, showStyleList[i]); // FIXME: Should we use SetWindowPlacement instead?
                    SetWindowPos(hWnd, IntPtr.Zero, infoList[i].position.X, infoList[i].position.Y, infoList[i].position.Width, infoList[i].position.Height,
                        (int)SetWindowPosFlags.SWP_NOZORDER | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                }
            }
        }

        public static List<IntPtr> GetOpenWindows()
        {
            List<IntPtr> windows = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    windows.Add(hWnd);
                }
                return true;
            }, 0);

            return windows;
        }
    }
}