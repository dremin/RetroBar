using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ManagedShell.Common.Logging;
using ManagedShell.Configuration;
using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTray;
using static ManagedShell.Interop.NativeMethods;
using ManagedShell.Interop;

namespace RetroBar.Utilities
{
    public static class AppBarHelper
    {
        public enum WinTaskbarState : int
        {
            AutoHide = 1,
            OnTop = 0
        }

        private static object appBarLock = new object();
        public static List<IntPtr> appBars = new List<IntPtr>();
        private static int uCallBack = 0;
        private static WinTaskbarState? startupTaskbarState;

        public static System.Drawing.Size PrimaryMonitorDeviceSize
        {
            get
            {
                return new System.Drawing.Size(NativeMethods.GetSystemMetrics(0), NativeMethods.GetSystemMetrics(1));
            }
        }

        public static int RegisterBar(AppBarWindow abWindow, double width, double height, ABEdge edge = ABEdge.ABE_TOP)
        {
            lock (appBarLock)
            {
                APPBARDATA abd = new APPBARDATA();
                abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                IntPtr handle = new WindowInteropHelper(abWindow).Handle;
                abd.hWnd = handle;

                if (!appBars.Contains(handle))
                {
                    uCallBack = RegisterWindowMessage("AppBarMessage");
                    abd.uCallbackMessage = uCallBack;

                    PrepareForInterop();
                    uint ret = SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
                    InteropDone();
                    appBars.Add(handle);
                    CairoLogger.Instance.Debug("AppBarHelper: Created AppBar for handle " + handle.ToString());

                    ABSetPos(abWindow, width, height, edge, true);
                }
                else
                {
                    PrepareForInterop();
                    SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
                    InteropDone();
                    appBars.Remove(handle);
                    CairoLogger.Instance.Debug("AppBarHelper: Removed AppBar for handle " + handle.ToString());

                    return 0;
                }
            }

            return uCallBack;
        }

        private static void PrepareForInterop()
        {
            // get shell window back so we can do appbar stuff
            TrayService.Instance.Suspend();
        }

        private static void InteropDone()
        {
            // take back over
            TrayService.Instance.Resume();
        }

        public static void SetWinTaskbarVisibility(int swp)
        {
            // only run this if our TaskBar is enabled, or if we are showing the Windows TaskBar
            if (swp != (int)SetWindowPosFlags.SWP_HIDEWINDOW
                || ShellSettings.Instance.EnableTaskbar)
            {
                IntPtr taskbarHwnd = FindTaskbarHwnd();
                IntPtr startButtonHwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, null);

                if (taskbarHwnd != IntPtr.Zero
                    && swp == (int)SetWindowPosFlags.SWP_HIDEWINDOW == IsWindowVisible(taskbarHwnd))
                {
                    SetWindowPos(taskbarHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0, swp | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                    if (startButtonHwnd != IntPtr.Zero)
                    {
                        SetWindowPos(startButtonHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0, swp | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                    }
                }

                // adjust secondary TaskBars for multi-monitor
                SetSecondaryTaskbarVisibility(swp);
            }
        }

        private static void SetSecondaryTaskbarVisibility(int swp)
        {
            IntPtr secTaskbarHwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_SecondaryTrayWnd", null);

            // if we have 3+ monitors there may be multiple secondary TaskBars
            while (secTaskbarHwnd != IntPtr.Zero)
            {
                if (swp == (int)SetWindowPosFlags.SWP_HIDEWINDOW == IsWindowVisible(secTaskbarHwnd))
                {
                    SetWindowPos(secTaskbarHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0, swp | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                }

                secTaskbarHwnd = FindWindowEx(IntPtr.Zero, secTaskbarHwnd, "Shell_SecondaryTrayWnd", null);
            }
        }

        public static void SetWinTaskbarState(WinTaskbarState state)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = FindTaskbarHwnd(),
                lParam = (IntPtr)state
            };

            PrepareForInterop();
            SHAppBarMessage((int)ABMsg.ABM_SETSTATE, ref abd);
            InteropDone();
        }

        public static WinTaskbarState GetWinTaskbarState()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = FindTaskbarHwnd()
            };

            PrepareForInterop();
            uint uState = SHAppBarMessage((int)ABMsg.ABM_GETSTATE, ref abd);
            InteropDone();

            return (WinTaskbarState) uState;
        }

        private static IntPtr FindTaskbarHwnd()
        {
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", "");

            if (NotificationArea.Instance.Handle != null && NotificationArea.Instance.Handle != IntPtr.Zero)
            {
                while (taskbarHwnd == NotificationArea.Instance.Handle)
                {
                    taskbarHwnd = FindWindowEx(IntPtr.Zero, taskbarHwnd, "Shell_TrayWnd", "");
                }
            }

            return taskbarHwnd;
        }

        public static void AppBarActivate(IntPtr hwnd)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hwnd,
                lParam = (IntPtr)Convert.ToInt32(true)
            };

            PrepareForInterop();
            SHAppBarMessage((int)ABMsg.ABM_ACTIVATE, ref abd);
            InteropDone();

            // apparently the TaskBars like to pop up when AppBars change
            if (ShellSettings.Instance.EnableTaskbar)
            {
                SetSecondaryTaskbarVisibility((int)SetWindowPosFlags.SWP_HIDEWINDOW);
            }
        }

        public static void AppBarWindowPosChanged(IntPtr hwnd)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hwnd
            };

            PrepareForInterop();
            SHAppBarMessage((int)ABMsg.ABM_WINDOWPOSCHANGED, ref abd);
            InteropDone();

            // apparently the TaskBars like to pop up when AppBars change
            if (ShellSettings.Instance.EnableTaskbar)
            {
                SetSecondaryTaskbarVisibility((int)SetWindowPosFlags.SWP_HIDEWINDOW);
            }
        }

        public static void ABSetPos(AppBarWindow abWindow, double width, double height, ABEdge edge, bool isCreate = false)
        {
            lock (appBarLock)
            {
                APPBARDATA abd = new APPBARDATA
                {
                    cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                    hWnd = abWindow.Handle,
                    uEdge = (int)edge
                };

                int sWidth = (int)width;
                int sHeight = (int)height;

                int top = 0;
                int left = 0;
                int right = PrimaryMonitorDeviceSize.Width;
                int bottom = PrimaryMonitorDeviceSize.Height;

                if (abWindow.Screen != null)
                {
                    top = abWindow.Screen.Bounds.Y;
                    left = abWindow.Screen.Bounds.X;
                    right = abWindow.Screen.Bounds.Right;
                    bottom = abWindow.Screen.Bounds.Bottom;
                }

                if (abd.uEdge == (int)ABEdge.ABE_LEFT || abd.uEdge == (int)ABEdge.ABE_RIGHT)
                {
                    abd.rc.Top = top;
                    abd.rc.Bottom = bottom;
                    if (abd.uEdge == (int)ABEdge.ABE_LEFT)
                    {
                        abd.rc.Left = left;
                        abd.rc.Right = abd.rc.Left + sWidth;
                    }
                    else
                    {
                        abd.rc.Right = right;
                        abd.rc.Left = abd.rc.Right - sWidth;
                    }
                }
                else
                {
                    abd.rc.Left = left;
                    abd.rc.Right = right;
                    if (abd.uEdge == (int)ABEdge.ABE_TOP)
                    {
                        abd.rc.Top = top;

                        abd.rc.Bottom = abd.rc.Top + sHeight;
                    }
                    else
                    {
                        abd.rc.Bottom = bottom;

                        abd.rc.Top = abd.rc.Bottom - sHeight;
                    }
                }

                PrepareForInterop();
                SHAppBarMessage((int)ABMsg.ABM_QUERYPOS, ref abd);
                InteropDone();

                // system doesn't adjust all edges for us, do some adjustments
                switch (abd.uEdge)
                {
                    case (int)ABEdge.ABE_LEFT:
                        abd.rc.Right = abd.rc.Left + sWidth;
                        break;
                    case (int)ABEdge.ABE_RIGHT:
                        abd.rc.Left = abd.rc.Right - sWidth;
                        break;
                    case (int)ABEdge.ABE_TOP:
                        abd.rc.Bottom = abd.rc.Top + sHeight;
                        break;
                    case (int)ABEdge.ABE_BOTTOM:
                        abd.rc.Top = abd.rc.Bottom - sHeight;
                        break;
                }

                PrepareForInterop();
                SHAppBarMessage((int)ABMsg.ABM_SETPOS, ref abd);
                InteropDone();

                // check if new coords
                bool isSameCoords = false;
                if (!isCreate)
                {
                    bool topUnchanged = abd.rc.Top == (abWindow.Top * abWindow.dpiScale);
                    bool leftUnchanged = abd.rc.Left == (abWindow.Left * abWindow.dpiScale);
                    bool bottomUnchanged = abd.rc.Bottom == (abWindow.Top * abWindow.dpiScale) + sHeight;
                    bool rightUnchanged = abd.rc.Right == (abWindow.Left * abWindow.dpiScale) + sWidth;

                    isSameCoords = topUnchanged
                                   && leftUnchanged
                                   && bottomUnchanged
                                   && rightUnchanged;
                }

                if (!isSameCoords)
                {
                    CairoLogger.Instance.Debug($"AppBarHelper: {abWindow.Name} changing position (TxLxBxR) to {abd.rc.Top}x{abd.rc.Left}x{abd.rc.Bottom}x{ abd.rc.Right} from {abWindow.Top * abWindow.dpiScale}x{abWindow.Left * abWindow.dpiScale}x{(abWindow.Top * abWindow.dpiScale) + sHeight}x{ (abWindow.Left * abWindow.dpiScale) + sWidth}");
                    abWindow.SetAppBarPosition(abd.rc);
                }

                abWindow.AfterAppBarPos(isSameCoords, abd.rc);

                if (abd.rc.Bottom - abd.rc.Top < sHeight)
                {
                    ABSetPos(abWindow, width, height, edge);
                }
            }
        }

        public static void HideWindowsTaskbar()
        {
            if (!Shell.IsCairoRunningAsShell)
            {
                if (startupTaskbarState == null)
                {
                    startupTaskbarState = GetWinTaskbarState();
                }

                if (ShellSettings.Instance.EnableTaskbar)
                {
                    SetWinTaskbarState(WinTaskbarState.AutoHide);
                    SetWinTaskbarVisibility((int) SetWindowPosFlags.SWP_HIDEWINDOW);
                }
            }
        }

        public static void ShowWindowsTaskbar()
        {
            if (!Shell.IsCairoRunningAsShell)
            {
                SetWinTaskbarState(startupTaskbarState ?? WinTaskbarState.OnTop);
                SetWinTaskbarVisibility((int) SetWindowPosFlags.SWP_SHOWWINDOW);
            }
        }
    }
}