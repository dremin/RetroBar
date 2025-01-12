using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Common.SupportingClasses;
using ManagedShell.UWPInterop;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    public class StartMenuMonitor : IDisposable
    {
        private AppVisibilityHelper _appVisibilityHelper;
        private DispatcherTimer _poller;
        private bool _isVisible;
        private IntPtr _taskbarHwndActivated;

        public event EventHandler<StartMenuMonitorEventArgs> StartMenuVisibilityChanged;

        public StartMenuMonitor(AppVisibilityHelper appVisibilityHelper)
        {
            _appVisibilityHelper = appVisibilityHelper;
            setupPoller();
        }

        private void setupPoller()
        {
            if (_poller == null)
            {
                _poller = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };

                _poller.Tick += poller_Tick;
            }

            _poller.Start();
        }

        private void poller_Tick(object sender, EventArgs e)
        {
            bool newIsVisible = false;
            if (EnvironmentHelper.IsWindows8OrBetter && isModernStartMenuOpen())
            {
                // Windows 8+
                newIsVisible = true;
            }

            if (!newIsVisible && isClassicStartMenuOpen())
            {
                // Windows 7, StartIsBack, Start8+
                newIsVisible = true;
            }

            if (!newIsVisible && isOpenShellMenuOpen())
            {
                // Open Shell Menu
                newIsVisible = true;
                relocateStartMenuByClass("OpenShell.CMenuContainer");
            }

            setVisibility(newIsVisible);
        }

        private void setVisibility(bool isVisible)
        {
            if (isVisible == _isVisible)
            {
                return;
            }

            _isVisible = isVisible;

            StartMenuMonitorEventArgs args = new StartMenuMonitorEventArgs
            {
                Visible = _isVisible,
                TaskbarHwndActivated = _taskbarHwndActivated
            };

            StartMenuVisibilityChanged?.Invoke(this, args);

            if (_taskbarHwndActivated != IntPtr.Zero)
            {
                // Now that it has been consumed, reset to prevent sending stale data
                // if the menu is opened again not by the start button.
                _taskbarHwndActivated = IntPtr.Zero;
            }
        }

        private bool isModernStartMenuOpen()
        {
            if (_appVisibilityHelper == null)
            {
                ShellLogger.Error("StartMenuMonitor: AppVisibilityHelper is null");
                return false;
            }

            return _appVisibilityHelper.IsLauncherVisible();
        }

        private bool isClassicStartMenuOpen()
        {
            return isVisibleByClass("DV2ControlHost");
        }

        private bool isOpenShellMenuOpen()
        {
            return isVisibleByClass("OpenShell.CMenuContainer");
        }

        private bool isVisibleByClass(string className)
        {
            IntPtr hStartMenu = FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, IntPtr.Zero);

            if (hStartMenu == IntPtr.Zero)
            {
                return false;
            }

            return IsWindowVisible(hStartMenu);
        }

        private void relocateStartMenuByClass(string className)
        {
            if (_taskbarHwndActivated == IntPtr.Zero)
            {
                return;
            }

            // Get current window rects
            IntPtr hStartMenu = FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, IntPtr.Zero);
            if (hStartMenu == IntPtr.Zero)
            {
                return;
            }
            FlowDirection flowDirection = Application.Current.FindResource("flow_direction") as FlowDirection? ?? FlowDirection.LeftToRight;
            GetWindowRect(hStartMenu, out ManagedShell.Interop.NativeMethods.Rect startMenuRect);
            GetWindowRect(_taskbarHwndActivated, out ManagedShell.Interop.NativeMethods.Rect taskbarRect);

            int x = 0, y = 0;
            switch (Settings.Instance.Edge)
            {
                case AppBarEdge.Left:
                    // Top right of taskbar is menu top left
                    x = taskbarRect.Right;
                    y = taskbarRect.Top;
                    break;
                case AppBarEdge.Right:
                    // Top left of taskbar is menu top right
                    x = taskbarRect.Left - startMenuRect.Width;
                    y = taskbarRect.Top;
                    break;
                case AppBarEdge.Top:
                    if (flowDirection == FlowDirection.LeftToRight)
                    {
                        // Bottom left of taskbar is menu top left
                        x = taskbarRect.Left;
                    }
                    else
                    {
                        // Bottom right of taskbar is menu top right
                        x = taskbarRect.Right - startMenuRect.Width;
                    }
                    y = taskbarRect.Bottom;
                    break;
                case AppBarEdge.Bottom:
                    if (flowDirection == FlowDirection.LeftToRight)
                    {
                        // Top left of taskbar is menu bottom left
                        x = taskbarRect.Left;
                    }
                    else
                    {
                        // Top right of taskbar is menu bottom right
                        x = taskbarRect.Right - startMenuRect.Width;
                    }
                    y = taskbarRect.Top - startMenuRect.Height;
                    break;
            }

            if (y == startMenuRect.Top && x == startMenuRect.Left)
            {
                // Start menu is already in the correct position
                return;
            }

            SetWindowPos(hStartMenu, IntPtr.Zero, x, y, 0, 0, (int)(SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_FRAMECHANGED));
        }

        private IImmersiveMonitor GetImmersiveMonitor(ManagedShell.UWPInterop.Interfaces.IServiceProvider shell, IntPtr hWnd)
        {
            if (shell.QueryService(ref CLSID_ImmersiveMonitorManager, ref IID_ImmersiveMonitorManager, out object monitorManagerObj) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to query for IImmersiveMonitorManager");
                return null;
            }
            IImmersiveMonitorManager monitorManager = (IImmersiveMonitorManager)monitorManagerObj;

            if (monitorManager.GetFromHandle(MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST), out IImmersiveMonitor monitor) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to get monitor from taskbar window handle");
                return null;
            }

            return monitor;
        }

        private IImmersiveLauncher_Win10RS1 GetImmersiveLauncher_Win10RS1(IntPtr taskbarHwnd)
        {
            var shell = ImmersiveShellHelper.GetImmersiveShell();
            if (shell.QueryService(ref CLSID_ImmersiveLauncher, ref IID_ImmersiveLauncher_Win10RS1, out object immersiveLauncherObj) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to query for IImmersiveLauncher_Win10RS1");
                return null;
            }
            IImmersiveLauncher_Win10RS1 immersiveLauncher = (IImmersiveLauncher_Win10RS1)immersiveLauncherObj;

            IImmersiveMonitor monitor = GetImmersiveMonitor(shell, taskbarHwnd);
            if (monitor == null || immersiveLauncher.ConnectToMonitor(monitor) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to connect IImmersiveLauncher_Win10RS1 to monitor");
                return null;
            }

            return immersiveLauncher;
        }

        private IImmersiveLauncher_Win81 GetImmersiveLauncher_Win81(IntPtr taskbarHwnd)
        {
            var shell = ImmersiveShellHelper.GetImmersiveShell();
            if (shell.QueryService(ref CLSID_ImmersiveLauncher, ref IID_ImmersiveLauncher_Win81, out object immersiveLauncherObj) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to query for IImmersiveLauncher_Win81");
                return null;
            }
            IImmersiveLauncher_Win81 immersiveLauncher = (IImmersiveLauncher_Win81)immersiveLauncherObj;

            IImmersiveMonitor monitor = GetImmersiveMonitor(shell, taskbarHwnd);
            if (monitor == null || immersiveLauncher.ConnectToMonitor(monitor) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to connect IImmersiveLauncher_Win81 to monitor");
                return null;
            }

            return immersiveLauncher;
        }

        internal void ShowStartMenu(IntPtr taskbarHwnd)
        {
            _taskbarHwndActivated = taskbarHwnd;

            if (!EnvironmentHelper.IsWindows10OrBetter ||
                FindWindowEx(IntPtr.Zero, IntPtr.Zero, "OpenShell.COwnerWindow", IntPtr.Zero) != IntPtr.Zero ||
                FindWindowEx(IntPtr.Zero, IntPtr.Zero, "DV2ControlHost", IntPtr.Zero) != IntPtr.Zero)
            {
                // Always use the Windows key when IImmersiveLauncher or IImmersiveMonitor is unavailable
                // Also use the Windows key when Open Shell Menu or StartIsBack is running, because we cannot otherwise invoke it
                ShellHelper.ShowStartMenu();
                return;
            }

            try
            {
                // Allow Explorer to steal focus
                GetWindowThreadProcessId(FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", "Program Manager"), out uint procId);
                AllowSetForegroundWindow(procId);

                if (EnvironmentHelper.IsWindows10RS1OrBetter)
                {
                    IImmersiveLauncher_Win10RS1 immersiveLauncher = GetImmersiveLauncher_Win10RS1(taskbarHwnd);
                    if (immersiveLauncher != null &&
                        immersiveLauncher.ShowStartView(IMMERSIVELAUNCHERSHOWMETHOD.ILSM_STARTBUTTON, IMMERSIVELAUNCHERSHOWFLAGS.ILSF_IGNORE_SET_FOREGROUND_ERROR) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    IImmersiveLauncher_Win81 immersiveLauncher = GetImmersiveLauncher_Win81(taskbarHwnd);
                    if (immersiveLauncher != null &&
                        immersiveLauncher.ShowStartView(IMMERSIVELAUNCHERSHOWMETHOD.ILSM_STARTBUTTON, IMMERSIVELAUNCHERSHOWFLAGS.ILSF_IGNORE_SET_FOREGROUND_ERROR) == 0)
                    {
                        return;
                    }
                }
                ShellLogger.Warning("StartMenuMonitor: Failed to show Start menu via IImmersiveLauncher");
            }
            catch (Exception e)
            {
                ShellLogger.Warning($"StartMenuMonitor: Failed to show Start menu via IImmersiveLauncher: {e}");
            }

            ShellHelper.ShowStartMenu();
        }

        public void Dispose()
        {
            _poller?.Stop();
        }

        #region Immersive launcher interfaces

        // Most managed interface definitions c/o https://github.com/MishaProductions/CustomShell/

        enum IMMERSIVE_MONITOR_FILTER_FLAGS
        {
            IMMERSIVE_MONITOR_FILTER_FLAGS_NONE = 0x0,
            IMMERSIVE_MONITOR_FILTER_FLAGS_DISABLE_TRAY = 0x1,
        }

        [ComImport]
        [Guid("880b26f8-9197-43d0-8045-8702d0d72000")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IImmersiveMonitor
        {
            public int GetIdentity(out uint pIdentity);
            public int Append(object unknown);
            public int GetHandle(out nint phMonitor);
            public int IsConnected(out bool pfConnected);
            public int IsPrimary(out bool pfPrimary);
            public int GetTrustLevel(out uint level);
            public int GetDisplayRect(out ManagedShell.Interop.NativeMethods.Rect prcDisplayRect);
            public int GetOrientation(out uint pdwOrientation);
            public int GetWorkArea(out ManagedShell.Interop.NativeMethods.Rect prcWorkArea);
            public int IsEqual(IImmersiveMonitor pMonitor, out bool pfEqual);
            public int GetTrustLevel2(out uint level);
            public int GetEffectiveDpi(out uint dpiX, out uint dpiY);
            public int GetFilterFlags(out IMMERSIVE_MONITOR_FILTER_FLAGS flags);
        }

        enum IMMERSIVE_MONITOR_MOVE_DIRECTION
        {
            IMMD_PREVIOUS = 0,
            IMMD_NEXT = 1
        }

        [ComImport]
        [Guid("4d4c1e64-e410-4faa-bafa-59ca069bfec2")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IImmersiveMonitorManager
        {
            public int GetCount(out uint pcMonitors);
            public int GetConnectedCount(out uint pcMonitors);
            public int GetAt(uint idxMonitor, out IImmersiveMonitor monitor);
            public int GetFromHandle(nint monitor, out IImmersiveMonitor monitor2);
            public int GetFromIdentity(uint identity, out IImmersiveMonitor monitor);
            public int GetImmersiveProxyMonitor(out IImmersiveMonitor monitor);
            public int QueryService(nint monit, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
            public int QueryServiceByIdentity(uint monit, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
            public int QueryServiceFromWindow(nint hwnd, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
            public int QueryServiceFromPoint(nint point, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
            public int GetNextImmersiveMonitor(IMMERSIVE_MONITOR_MOVE_DIRECTION direction, IImmersiveMonitor monitor, out IImmersiveMonitor monitorout);
            public int GetMonitorArray(out object array);
            public int SetFilter(object filter);
        }

        enum IMMERSIVELAUNCHERSHOWMETHOD
        {
            ILSM_INVALID = 0x0,
            ILSM_HSHELLTASKMAN = 0x1,
            ILSM_IMMERSIVEBACKGROUND = 0x4,
            ILSM_APPCLOSED = 0x6,
            ILSM_STARTBUTTON = 0xB,
            ILSM_RETAILDEMO_EDUCATIONAPP = 0xC,
            ILSM_BACK = 0xD,
            ILSM_SESSIONONUNLOCK = 0xE
        }

        enum IMMERSIVELAUNCHERSHOWFLAGS
        {
            ILSF_NONE = 0x0,
            ILSF_IGNORE_SET_FOREGROUND_ERROR = 0x4,
        }

        enum IMMERSIVELAUNCHERDISMISSMETHOD
        {
            ILDM_INVALID = 0x0,
            ILDM_HSHELLTASKMAN = 0x1,
            ILDM_STARTCHARM = 0x2,
            ILDM_BACKGESTURE = 0x3,
            ILDM_ESCAPEKEY = 0x4,
            ILDM_SHOWDESKTOP = 0x5,
            ILDM_STARTTIP = 0x6,
            ILDM_GENERIC_NONANIMATING = 0x7,
        }

        [ComImport]
        [Guid("d8d60399-a0f1-f987-5551-321fd1b49864")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IImmersiveLauncher_Win10RS1
        {
            public int ShowStartView(IMMERSIVELAUNCHERSHOWMETHOD showMethod, IMMERSIVELAUNCHERSHOWFLAGS showFlags);
            public int Dismiss(IMMERSIVELAUNCHERDISMISSMETHOD dismissMethod);
            public int Dismiss2(IMMERSIVELAUNCHERDISMISSMETHOD dismissMethod);
            public int DismissSynchronouslyWithoutTransition();
            public int IsVisible(out bool p0);
            public int OnStartButtonPressed(IMMERSIVELAUNCHERSHOWMETHOD showMethod, IMMERSIVELAUNCHERDISMISSMETHOD dismissMethod);
            public int SetForeground();
            public int ConnectToMonitor(IImmersiveMonitor monitor);
            public int GetMonitor(out IImmersiveMonitor monitor);
            public int OnFirstSignAnimationFinished();
            public int Prelaunch();
        }

        [ComImport]
        [Guid("93f91f5a-a4ca-4205-9beb-ce4d17c708f9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IImmersiveLauncher_Win81
        {
            public int ShowStartView(IMMERSIVELAUNCHERSHOWMETHOD showMethod, IMMERSIVELAUNCHERSHOWFLAGS showFlags);
            public int Unknown2();
            public int Unknown3();
            public int Unknown4();
            public int Unknown5();
            public int Dismiss(IMMERSIVELAUNCHERDISMISSMETHOD dismissMethod);
            public int Unknown7();
            public int IsVisible(out bool p0);
            public int Unknown9();
            public int Unknown10();
            public int Unknown11();
            public int Unknown12();
            public int Unknown13();
            public int Unknown14();
            public int Unknown15();
            public int ConnectToMonitor(IImmersiveMonitor monitor);
            public int GetMonitor(out IImmersiveMonitor monitor);
        }

        static Guid CLSID_ImmersiveMonitorManager = new Guid("47094e3a-0cf2-430f-806f-cf9e4f0f12dd");
        static Guid IID_ImmersiveMonitorManager = new Guid("4d4c1e64-e410-4faa-bafa-59ca069bfec2");
        static Guid CLSID_ImmersiveLauncher = new Guid("6f86e01c-c649-4d61-be23-f1322ddeca9d");
        static Guid IID_ImmersiveLauncher_Win10RS1 = new Guid("d8d60399-a0f1-f987-5551-321fd1b49864");
        static Guid IID_ImmersiveLauncher_Win81 = new Guid("93f91f5a-a4ca-4205-9beb-ce4d17c708f9");
        #endregion

        public class StartMenuMonitorEventArgs : LauncherVisibilityEventArgs
        {
            public IntPtr TaskbarHwndActivated;
        }
    }
}
