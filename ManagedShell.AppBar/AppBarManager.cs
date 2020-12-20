using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ManagedShell.Common.Helpers;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.AppBar
{
    public class AppBarManager
    {
        private static object appBarLock = new object();
        
        private readonly ExplorerHelper _explorerHelper;
        private int uCallBack;
        
        public List<AppBarWindow> AppBars { get; } = new List<AppBarWindow>();
        public EventHandler<AppBarEventArgs> AppBarEvent;

        public AppBarManager(ExplorerHelper explorerHelper)
        {
            _explorerHelper = explorerHelper;
        }

        public void SignalGracefulShutdown()
        {
            foreach (AppBarWindow window in AppBars)
            {
                window.AllowClose = true;
            }
        }

        public void NotifyAppBarEvent(AppBarWindow sender, AppBarEventReason reason)
        {
            AppBarEventArgs args = new AppBarEventArgs { Reason = reason };
            AppBarEvent?.Invoke(sender, args);
        }

        #region AppBar message helpers
        public int RegisterBar(AppBarWindow abWindow, double width, double height, ABEdge edge = ABEdge.ABE_TOP)
        {
            lock (appBarLock)
            {
                APPBARDATA abd = new APPBARDATA();
                abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                abd.hWnd = abWindow.Handle;

                if (!AppBars.Contains(abWindow))
                {
                    uCallBack = RegisterWindowMessage("AppBarMessage");
                    abd.uCallbackMessage = uCallBack;

                    _explorerHelper.SuspendTrayService();
                    SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
                    _explorerHelper.ResumeTrayService();
                    
                    AppBars.Add(abWindow);
                    
                    ShellLogger.Debug($"AppBarManager: Created AppBar for handle {abWindow.Handle}");

                    ABSetPos(abWindow, width, height, edge, true);
                }
                else
                {
                    _explorerHelper.SuspendTrayService();
                    SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
                    _explorerHelper.ResumeTrayService();
                    AppBars.Remove(abWindow);
                    ShellLogger.Debug($"AppBarManager: Removed AppBar for handle {abWindow.Handle}");

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
            if (_explorerHelper.HideExplorerTaskbar)
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
            if (_explorerHelper.HideExplorerTaskbar)
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
                int right = ScreenHelper.PrimaryMonitorDeviceSize.Width;
                int bottom = ScreenHelper.PrimaryMonitorDeviceSize.Height;

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
                        if (!abWindow.requiresScreenEdge)
                        {
                            abd.rc.Top = top + Convert.ToInt32(GetAppBarEdgeWindowsHeight((ABEdge)abd.uEdge, abWindow.Screen));
                        }
                        else
                        {
                            abd.rc.Top = top;
                        }

                        abd.rc.Bottom = abd.rc.Top + sHeight;
                    }
                    else
                    {
                        if (!abWindow.requiresScreenEdge)
                        {
                            abd.rc.Bottom = bottom - Convert.ToInt32(GetAppBarEdgeWindowsHeight((ABEdge)abd.uEdge, abWindow.Screen));
                        }
                        else
                        {
                            abd.rc.Bottom = bottom;
                        }

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
                    ShellLogger.Debug($"AppBarManager: {abWindow.Name} changing position (TxLxBxR) to {abd.rc.Top}x{abd.rc.Left}x{abd.rc.Bottom}x{abd.rc.Right} from {abWindow.Top * abWindow.dpiScale}x{abWindow.Left * abWindow.dpiScale}x{(abWindow.Top * abWindow.dpiScale) + sHeight}x{ (abWindow.Left * abWindow.dpiScale) + sWidth}");
                    abWindow.SetAppBarPosition(abd.rc);
                }

                abWindow.AfterAppBarPos(isSameCoords, abd.rc);

                if (abd.rc.Bottom - abd.rc.Top < sHeight)
                {
                    ABSetPos(abWindow, width, height, edge);
                }
            }
        }
        #endregion

        #region Work area
        public double GetAppBarEdgeWindowsHeight(ABEdge edge, Screen screen)
        {
            double edgeHeight = 0;
            double dpiScale = 1;
            Rect workAreaRect = GetWorkArea(ref dpiScale, screen, true, true);

            switch (edge)
            {
                case ABEdge.ABE_TOP:
                    edgeHeight += workAreaRect.Top / dpiScale;
                    break;
                case ABEdge.ABE_BOTTOM:
                    edgeHeight += (screen.Bounds.Bottom - workAreaRect.Bottom) / dpiScale;
                    break;
                case ABEdge.ABE_LEFT:
                    edgeHeight += workAreaRect.Left / dpiScale;
                    break;
                case ABEdge.ABE_RIGHT:
                    edgeHeight += (screen.Bounds.Right - workAreaRect.Right) / dpiScale;
                    break;
            }

            return edgeHeight;
        }

        public Rect GetWorkArea(ref double dpiScale, Screen screen, bool edgeBarsOnly, bool enabledBarsOnly)
        {
            double topEdgeWindowHeight = 0;
            double bottomEdgeWindowHeight = 0;
            Rect rc;
            rc.Left = screen.Bounds.Left;
            rc.Right = screen.Bounds.Right;

            // get appropriate windows for this display
            foreach (var window in AppBars)
            {
                if (window.Screen.DeviceName == screen.DeviceName)
                {
                    if ((window.enableAppBar || !enabledBarsOnly) && (window.requiresScreenEdge || !edgeBarsOnly))
                    {
                        if (window.appBarEdge == ABEdge.ABE_TOP)
                        {
                            topEdgeWindowHeight += window.ActualHeight;
                        }
                        else if (window.appBarEdge == ABEdge.ABE_BOTTOM)
                        {
                            bottomEdgeWindowHeight += window.ActualHeight;
                        }
                    }

                    dpiScale = window.dpiScale;
                    break;
                }
            }

            rc.Top = screen.Bounds.Top + (int)(topEdgeWindowHeight * dpiScale);
            rc.Bottom = screen.Bounds.Bottom - (int)(bottomEdgeWindowHeight * dpiScale);

            return rc;
        }

        public void SetWorkArea(Screen screen)
        {
            double dpiScale = 1;
            Rect rc = GetWorkArea(ref dpiScale, screen, false, true);

            SystemParametersInfo((int)SPI.SETWORKAREA, 1, ref rc, (uint)(SPIF.UPDATEINIFILE | SPIF.SENDWININICHANGE));
        }

        public static void ResetWorkArea()
        {
            if (Shell.IsCairoRunningAsShell)
            {
                // TODO this is wrong for multi-display
                // set work area back to full screen size. we can't assume what pieces of the old work area may or may not be still used
                Rect oldWorkArea;
                oldWorkArea.Left = SystemInformation.VirtualScreen.Left;
                oldWorkArea.Top = SystemInformation.VirtualScreen.Top;
                oldWorkArea.Right = SystemInformation.VirtualScreen.Right;
                oldWorkArea.Bottom = SystemInformation.VirtualScreen.Bottom;

                SystemParametersInfo((int)SPI.SETWORKAREA, 1, ref oldWorkArea,
                    (uint)(SPIF.UPDATEINIFILE | SPIF.SENDWININICHANGE));
            }
        }
        #endregion
    }
}