using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    // We have too many things in here
    // Lets focus more on single responsibility
    public class AppBarHelper
    {
        private readonly ExplorerHelper _explorerHelper;

        public AppBarHelper(ExplorerHelper explorerHelper)
        {
            _explorerHelper = explorerHelper;
        }

        private static object appBarLock = new object();
        public static List<IntPtr> AppBars { get; } = new List<IntPtr>();

        private static int uCallBack = 0;

        public static System.Drawing.Size PrimaryMonitorDeviceSize
        {
            get
            {
                return new System.Drawing.Size(GetSystemMetrics(0), GetSystemMetrics(1));
            }
        }

        public int RegisterBar(AppBarWindow abWindow, double width, double height, ABEdge edge = ABEdge.ABE_TOP)
        {
            lock (appBarLock)
            {
                APPBARDATA abd = new APPBARDATA();
                abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                IntPtr handle = new WindowInteropHelper(abWindow).Handle;
                abd.hWnd = handle;

                if (!AppBars.Contains(handle))
                {
                    uCallBack = RegisterWindowMessage("AppBarMessage");
                    abd.uCallbackMessage = uCallBack;

                    _explorerHelper.SuspendTrayService();
                    uint ret = SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
                    _explorerHelper.ResumeTrayService();
                    AppBars.Add(handle);
                    CairoLogger.Debug("AppBarHelper: Created AppBar for handle " + handle.ToString());

                    ABSetPos(abWindow, width, height, edge, true);
                }
                else
                {
                    _explorerHelper.SuspendTrayService();
                    SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
                    _explorerHelper.ResumeTrayService();
                    AppBars.Remove(handle);
                    CairoLogger.Debug("AppBarHelper: Removed AppBar for handle " + handle.ToString());

                    return 0;
                }
            }

            return uCallBack;
        }


        public void AppBarActivate(IntPtr hwnd)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hwnd,
                lParam = (IntPtr)Convert.ToInt32(true)
            };

            _explorerHelper.SuspendTrayService();
            SHAppBarMessage((int)ABMsg.ABM_ACTIVATE, ref abd);
            _explorerHelper.ResumeTrayService();

            // apparently the TaskBars like to pop up when AppBars change
            // TODO: if (_shellSettings.EnableTaskbar)
            {
                _explorerHelper.SetSecondaryTaskbarVisibility((int)SetWindowPosFlags.SWP_HIDEWINDOW);
            }
        }

        public void AppBarWindowPosChanged(IntPtr hwnd)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hwnd
            };

            _explorerHelper.SuspendTrayService();
            SHAppBarMessage((int)ABMsg.ABM_WINDOWPOSCHANGED, ref abd);
            _explorerHelper.ResumeTrayService();

            // apparently the TaskBars like to pop up when AppBars change
            // TODO: if (_shellSettings.EnableTaskbar)
            {
                _explorerHelper.SetSecondaryTaskbarVisibility((int)SetWindowPosFlags.SWP_HIDEWINDOW);
            }
        }

        public void ABSetPos(AppBarWindow abWindow, double width, double height, ABEdge edge, bool isCreate = false)
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

                _explorerHelper.SuspendTrayService();
                SHAppBarMessage((int)ABMsg.ABM_QUERYPOS, ref abd);
                _explorerHelper.ResumeTrayService();

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

                _explorerHelper.SuspendTrayService();
                SHAppBarMessage((int)ABMsg.ABM_SETPOS, ref abd);
                _explorerHelper.ResumeTrayService();

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
                    CairoLogger.Debug($"AppBarHelper: {abWindow.Name} changing position (TxLxBxR) to {abd.rc.Top}x{abd.rc.Left}x{abd.rc.Bottom}x{ abd.rc.Right} from {abWindow.Top * abWindow.dpiScale}x{abWindow.Left * abWindow.dpiScale}x{(abWindow.Top * abWindow.dpiScale) + sHeight}x{ (abWindow.Left * abWindow.dpiScale) + sWidth}");
                    abWindow.SetAppBarPosition(abd.rc);
                }

                abWindow.AfterAppBarPos(isSameCoords, abd.rc);

                if (abd.rc.Bottom - abd.rc.Top < sHeight)
                {
                    ABSetPos(abWindow, width, height, edge);
                }
            }
        }
    }
}