using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    public class TrayService : IDisposable
    {
        private const string NotifyWndClass = "TrayNotifyWnd";
        private const string TrayWndClass = "Shell_TrayWnd";
        private readonly int[] ForwardMessagesPost = { (int)WM.USER + 372 };

        private AppBarMessageDelegate appBarMessageDelegate;
        private IconDataDelegate iconDataDelegate;
        private SystrayDelegate trayDelegate;
        private WndProcDelegate wndProcDelegate;

        private IntPtr HwndTray;
        private IntPtr HwndNotify;
        private IntPtr HwndFwd;
        private IntPtr hInstance = Marshal.GetHINSTANCE(typeof(TrayService).Module);

        private readonly DispatcherTimer trayMonitor = new DispatcherTimer(DispatcherPriority.Background);

        public TrayService()
        {
            SetupTrayMonitor();
        }

        #region Set callbacks
        internal void SetSystrayCallback(SystrayDelegate theDelegate)
        {
            trayDelegate = theDelegate;
        }

        internal void SetIconDataCallback(IconDataDelegate theDelegate)
        {
            iconDataDelegate = theDelegate;
        }

        internal void SetAppBarMessageCallback(AppBarMessageDelegate theDelegate)
        {
            appBarMessageDelegate = theDelegate;
        }
        #endregion

        internal IntPtr Initialize()
        {
            if (HwndTray != IntPtr.Zero)
            {
                return HwndTray;
            }

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
                Resume();
                SendTaskbarCreated();
            }
        }

        internal void Suspend()
        {
            // if we go beneath another tray, it will receive messages
            if (HwndTray != IntPtr.Zero)
            {
                trayMonitor.Stop();
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
                SetWindowsTrayBottommost();
                MakeTrayTopmost();
                trayMonitor.Start();
            }
        }

        internal void SetTrayHostSizeData(TrayHostSizeData data)
        {
            if (HwndTray != IntPtr.Zero)
            {
                SetWindowPos(HwndTray, IntPtr.Zero, data.rc.Left, data.rc.Top, data.rc.Width, data.rc.Height, (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOZORDER);
            }

            if (HwndNotify != IntPtr.Zero)
            {
                SetWindowPos(HwndNotify, IntPtr.Zero, data.rc.Left, data.rc.Top, data.rc.Width, data.rc.Height, (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOZORDER);
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
            trayMonitor.Stop();
            DestroyWindows();

            if (!EnvironmentHelper.IsAppRunningAsShell)
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
                                    ShellLogger.Debug("TrayService: Size incorrect for AppBarData");
                                    break;
                                }

                                bool handled = false;
                                IntPtr abmResult = IntPtr.Zero;
                                if (appBarMessageDelegate != null)
                                {
                                    abmResult = appBarMessageDelegate(amd, ref handled);
                                }

                                if (handled)
                                {
                                    ShellLogger.Debug($"TrayService: Handled AppBar message {(ABMsg)amd.dwMessage}");
                                    return abmResult;
                                }

                                ShellLogger.Debug($"TrayService: Forwarding AppBar message {(ABMsg)amd.dwMessage}");
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

            if (msg == (int)WM.COPYDATA ||
                msg == (int)WM.ACTIVATEAPP ||
                msg == (int)WM.COMMAND ||
                msg >= (int)WM.USER)
            {
                return ForwardMsg(hWnd, msg, wParam, lParam);
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        #region Event handling
        private IntPtr ForwardMsg(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (HwndFwd == IntPtr.Zero || !IsWindow(HwndFwd))
            {
                HwndFwd = WindowHelper.FindWindowsTray(HwndTray);
            }

            if (HwndFwd != IntPtr.Zero)
            {
                if (msg >= (int)WM.USER && ForwardMessagesPost.Contains(msg))
                {
                    ShellLogger.Debug($"TrayService: Forwarding message via PostMessage: {msg}");
                    PostMessage(HwndFwd, (uint)msg, wParam, lParam);
                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
                return SendMessage(HwndFwd, msg, wParam, lParam);
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
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
                Convert.ToInt32(23 * DpiHelper.DpiScale), IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

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
                Convert.ToInt32(23 * DpiHelper.DpiScale), HwndTray, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (HwndNotify == IntPtr.Zero)
            {
                ShellLogger.Info($"TrayService: Error creating {NotifyWndClass} window ({Marshal.GetLastWin32Error()})");
            }
            else
            {
                ShellLogger.Debug($"TrayService: Created {NotifyWndClass}");
            }
        }

        private void SetupTrayMonitor()
        {
            trayMonitor.Interval = new TimeSpan(0, 0, 0, 0, 100);
            trayMonitor.Tick += TrayMonitor_Tick;
        }

        private void TrayMonitor_Tick(object sender, EventArgs e)
        {
            if (HwndTray == IntPtr.Zero) return;

            IntPtr taskbarHwnd = FindWindow(TrayWndClass, "");

            if (taskbarHwnd == HwndTray) return;

            ShellLogger.Debug("TrayService: Raising Shell_TrayWnd");
            MakeTrayTopmost();
        }

        private void SetWindowsTrayBottommost()
        {
            IntPtr taskbarHwnd = WindowHelper.FindWindowsTray(HwndTray);

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