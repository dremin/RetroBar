using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Runtime.InteropServices;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    public class TrayService : IDisposable
    {
        private const string NotifyWndClass = "TrayNotifyWnd";
        private const string TrayWndClass = "Shell_TrayWnd";

        private IconDataDelegate iconDataDelegate;
        private TrayHostSizeDelegate trayHostSizeDelegate;
        private SystrayDelegate trayDelegate;
        private WndProcDelegate wndProcDelegate;

        private IntPtr HwndTray;
        private IntPtr HwndNotify;
        private IntPtr HwndFwd;
        private IntPtr hInstance = Marshal.GetHINSTANCE(typeof(TrayService).Module);

        public TrayService()
        {

        }

        #region Set callbacks
        public void SetSystrayCallback(SystrayDelegate theDelegate)
        {
            trayDelegate = theDelegate;
        }

        public void SetIconDataCallback(IconDataDelegate theDelegate)
        {
            iconDataDelegate = theDelegate;
        }

        public void SetTrayHostSizeCallback(TrayHostSizeDelegate theDelegate)
        {
            trayHostSizeDelegate = theDelegate;
        }
        #endregion

        internal IntPtr Initialize()
        {
            DestroyWindows();

            wndProcDelegate = WndProc;

            RegisterTrayWnd();
            RegisterNotifyWnd();

            return HwndTray;
        }

        /// <summary>
        /// Starts the system tray listener (send the TaskbarCreated message).
        /// </summary>
        internal void Run()
        {
            if (HwndTray != IntPtr.Zero)
            {
                SetWindowsTrayBottommost();

                SetWindowPos(HwndTray, IntPtr.Zero, 0, 0, 0, 0,
                    (int)SetWindowPosFlags.SWP_NOMOVE |
                    (int)SetWindowPosFlags.SWP_NOACTIVATE |
                    (int)SetWindowPosFlags.SWP_NOSIZE);

                SendTaskbarCreated();
            }
        }

        internal void Suspend()
        {
            // if we go beneath another tray, it will receive messages
            if (HwndTray != IntPtr.Zero)
            {
                SetWindowPos(HwndTray, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0,
                    (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE |
                    (int)SetWindowPosFlags.SWP_NOSIZE);
            }
        }

        internal void Resume()
        {
            // if we are above another tray, we will receive messages
            if (HwndTray != IntPtr.Zero)
            {
                MakeTrayTopmost();
                SetWindowsTrayBottommost();
            }
        }

        private void SendTaskbarCreated()
        {
            int msg = RegisterWindowMessage("TaskbarCreated");

            if (msg > 0)
            {
                ShellLogger.Debug("TrayService: Sending TaskbarCreated message");
                SendNotifyMessage(HWND_BROADCAST,
                    (uint)msg, UIntPtr.Zero, IntPtr.Zero);
            }
        }

        private void DestroyWindows()
        {
            if (HwndNotify != IntPtr.Zero)
            {
                DestroyWindow(HwndNotify);
                UnregisterClass(NotifyWndClass, hInstance);
                ShellLogger.Debug($"TrayService: Unregistered {NotifyWndClass}");
            }

            if (HwndTray != IntPtr.Zero)
            {
                DestroyWindow(HwndTray);
                UnregisterClass(TrayWndClass, hInstance);
                ShellLogger.Debug($"TrayService: Unregistered {TrayWndClass}");
            }
        }

        public void Dispose()
        {
            DestroyWindows();

            if (!Shell.IsCairoRunningAsShell)
                SendTaskbarCreated();
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((WM)msg)
            {
                case WM.COPYDATA:
                    if (lParam == IntPtr.Zero)
                    {
                        ShellLogger.Debug("TrayService: CopyData is null");
                        break;
                    }

                    COPYDATASTRUCT copyData =
                        (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));

                    switch ((int)copyData.dwData)
                    {
                        case 0:
                            // AppBar message
                            if (Marshal.SizeOf(typeof(APPBARMSGDATAV3)) == copyData.cbData)
                            {
                                APPBARMSGDATAV3 amd = (APPBARMSGDATAV3)Marshal.PtrToStructure(copyData.lpData,
                                    typeof(APPBARMSGDATAV3));

                                if (Marshal.SizeOf(typeof(APPBARDATAV2)) != amd.abd.cbSize)
                                {
                                    ShellLogger.Debug("TrayService: Size incorrect for APPBARMSGDATAV3");
                                    break;
                                }

                                if (AppBarMessageAction(amd))
                                {
                                    return (IntPtr)1;
                                }
                            }
                            else
                            {
                                ShellLogger.Debug("TrayService: AppBar message received, but with unknown size");
                            }
                            break;
                        case 1:
                            SHELLTRAYDATA trayData =
                                (SHELLTRAYDATA)Marshal.PtrToStructure(copyData.lpData,
                                    typeof(SHELLTRAYDATA));
                            if (trayDelegate != null)
                            {
                                if (trayDelegate(trayData.dwMessage, new SafeNotifyIconData(trayData.nid)))
                                {
                                    return (IntPtr)1;
                                }

                                ShellLogger.Debug("TrayService: Ignored notify icon message");
                            }
                            else
                            {
                                ShellLogger.Info("TrayService: TrayDelegate is null");
                            }
                            break;
                        case 3:
                            WINNOTIFYICONIDENTIFIER iconData =
                                (WINNOTIFYICONIDENTIFIER)Marshal.PtrToStructure(copyData.lpData,
                                    typeof(WINNOTIFYICONIDENTIFIER));

                            if (iconDataDelegate != null)
                            {
                                return iconDataDelegate(iconData.dwMessage, iconData.hWnd, iconData.uID,
                                    iconData.guidItem);
                            }

                            ShellLogger.Info("TrayService: IconDataDelegate is null");
                            break;
                    }

                    break;
                case WM.WINDOWPOSCHANGED:
                    WINDOWPOS wndPos = WINDOWPOS.FromMessage(lParam);

                    if ((wndPos.flags & SetWindowPosFlags.SWP_SHOWWINDOW) != 0)
                    {
                        SetWindowLong(HwndTray, GWL_STYLE,
                            GetWindowLong(HwndTray, GWL_STYLE) &
                            ~(int)WindowStyles.WS_VISIBLE);

                        ShellLogger.Debug($"TrayService: {TrayWndClass} became visible; hiding");
                    }
                    break;
            }

            if (msg == (int)WM.COPYDATA || msg == (int)WM.ACTIVATEAPP)
            {
                return ForwardMsg(msg, wParam, lParam);
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        #region Event handling
        private bool AppBarMessageAction(APPBARMSGDATAV3 amd)
        {
            // only handle ABM_GETTASKBARPOS, send other AppBar messages to default handler
            switch ((ABMsg)amd.dwMessage)
            {
                case ABMsg.ABM_GETTASKBARPOS:
                    IntPtr hShared = SHLockShared((IntPtr)amd.hSharedMemory, (uint)amd.dwSourceProcessId);
                    APPBARDATAV2 abd = (APPBARDATAV2)Marshal.PtrToStructure(hShared, typeof(APPBARDATAV2));
                    FillTrayHostSizeData(ref abd);
                    Marshal.StructureToPtr(abd, hShared, false);
                    SHUnlockShared(hShared);
                    ShellLogger.Debug("TrayService: Responded to ABM_GETTASKBARPOS");
                    return true;
            }
            return false;
        }

        private void FillTrayHostSizeData(ref APPBARDATAV2 abd)
        {
            if (trayHostSizeDelegate != null)
            {
                TrayHostSizeData msd = trayHostSizeDelegate();
                abd.rc = msd.rc;
                abd.uEdge = (uint)msd.edge;
            }
            else
            {
                ShellLogger.Info("TrayService: TrayHostSizeDelegate is null");
            }
        }

        private IntPtr ForwardMsg(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (HwndFwd == IntPtr.Zero || !IsWindow(HwndFwd))
            {
                HwndFwd = FindWindowsTray();
            }

            if (HwndFwd != IntPtr.Zero)
            {
                return SendMessage(HwndFwd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }
        #endregion

        #region Window helpers
        private ushort RegisterWndClass(string name)
        {
            WNDCLASS newClass = new WNDCLASS
            {
                lpszClassName = name,
                hInstance = hInstance,
                style = 0x8,
                lpfnWndProc = wndProcDelegate
            };

            return RegisterClass(ref newClass);
        }

        private void RegisterTrayWnd()
        {
            ushort trayClassReg = RegisterWndClass(TrayWndClass);
            if (trayClassReg == 0)
            {
                ShellLogger.Info($"TrayService: Error registering {TrayWndClass} class ({Marshal.GetLastWin32Error()})");
            }

            HwndTray = CreateWindowEx(
                ExtendedWindowStyles.WS_EX_TOPMOST |
                ExtendedWindowStyles.WS_EX_TOOLWINDOW, trayClassReg, "",
                WindowStyles.WS_POPUP | WindowStyles.WS_CLIPCHILDREN |
                WindowStyles.WS_CLIPSIBLINGS, 0, 0, GetSystemMetrics(0),
                (int)(23 * Shell.DpiScale), IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (HwndTray == IntPtr.Zero)
            {
                ShellLogger.Info($"TrayService: Error creating {TrayWndClass} window ({Marshal.GetLastWin32Error()})");
            }
            else
            {
                ShellLogger.Debug($"TrayService: Created {TrayWndClass}");
            }
        }

        private void RegisterNotifyWnd()
        {
            ushort trayNotifyClassReg = RegisterWndClass(NotifyWndClass);
            if (trayNotifyClassReg == 0)
            {
                ShellLogger.Info($"TrayService: Error registering {NotifyWndClass} class ({Marshal.GetLastWin32Error()})");
            }

            HwndNotify = CreateWindowEx(0, trayNotifyClassReg, null,
                WindowStyles.WS_CHILD | WindowStyles.WS_CLIPCHILDREN |
                WindowStyles.WS_CLIPSIBLINGS, 0, 0, GetSystemMetrics(0),
                (int)(23 * Shell.DpiScale), HwndTray, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (HwndNotify == IntPtr.Zero)
            {
                ShellLogger.Info($"TrayService: Error creating {NotifyWndClass} window ({Marshal.GetLastWin32Error()})");
            }
            else
            {
                ShellLogger.Debug($"TrayService: Created {NotifyWndClass}");
            }
        }

        private IntPtr FindWindowsTray()
        {
            IntPtr taskbarHwnd = FindWindow(TrayWndClass, "");

            if (HwndTray != IntPtr.Zero)
            {
                while (taskbarHwnd == HwndTray)
                {
                    taskbarHwnd = FindWindowEx(IntPtr.Zero, taskbarHwnd, TrayWndClass, "");
                }
            }

            return taskbarHwnd;
        }

        private void SetWindowsTrayBottommost()
        {
            IntPtr taskbarHwnd = FindWindowsTray();

            if (taskbarHwnd != IntPtr.Zero)
            {
                SetWindowPos(taskbarHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0,
                    (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE |
                    (int)SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        private void MakeTrayTopmost()
        {
            if (HwndTray != IntPtr.Zero)
            {
                SetWindowPos(HwndTray, (IntPtr)WindowZOrder.HWND_TOPMOST, 0, 0, 0, 0,
                    (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE |
                    (int)SetWindowPosFlags.SWP_NOSIZE);
            }
        }
        #endregion
    }
}