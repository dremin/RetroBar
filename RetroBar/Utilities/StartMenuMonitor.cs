using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Common.SupportingClasses;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    public class StartMenuMonitor : IDisposable
    {
        private AppVisibilityHelper _appVisibilityHelper;
        private DispatcherTimer _poller;
        private bool _isVisible;

        public event EventHandler<LauncherVisibilityEventArgs> StartMenuVisibilityChanged;

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
            if (EnvironmentHelper.IsWindows8OrBetter)
            {
                // Windows 8+
                setVisibility(isModernStartMenuOpen());
            }

            if (!EnvironmentHelper.IsWindows8OrBetter || !_isVisible)
            {
                // Windows 7, StartIsBack, Start8+
                setVisibility(isClassicStartMenuOpen());
            }

            if (!_isVisible)
            {
                // Open Shell Menu
                setVisibility(isOpenShellMenuOpen());
            }
        }

        private void setVisibility(bool isVisible)
        {
            if (isVisible == _isVisible)
            {
                return;
            }

            _isVisible = isVisible;

            LauncherVisibilityEventArgs args = new LauncherVisibilityEventArgs
            {
                Visible = _isVisible
            };

            StartMenuVisibilityChanged?.Invoke(this, args);
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

        private IImmersiveLauncher GetImmersiveLauncher(IntPtr taskbarHwnd)
        {
            var shell = (IServiceProvider)new CImmersiveShell();

            if (shell.QueryService(ref CLSID_MonitorManager, ref IID_MonitorManager, out object monitorManagerObj) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to query for IImmersiveMonitorManager");
                return null;
            }
            IImmersiveMonitorManager monitorManager = (IImmersiveMonitorManager)monitorManagerObj;

            if (shell.QueryService(ref CLSID_ImmersiveLauncher, ref IID_ImmersiveLauncher, out object immersiveLauncherObj) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to query for IImmersiveLauncher");
                return null;
            }
            IImmersiveLauncher immersiveLauncher = (IImmersiveLauncher)immersiveLauncherObj;

            if (monitorManager.GetFromHandle(MonitorFromWindow(taskbarHwnd, MONITOR_DEFAULTTONEAREST), out IImmersiveMonitor monitor) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to get monitor from taskbar window handle");
                return null;
            }

            if (immersiveLauncher.ConnectToMonitor(monitor) != 0)
            {
                ShellLogger.Warning("StartMenuMonitor: Failed to connect IImmersiveLauncher to monitor");
                return null;
            }

            return immersiveLauncher;
        }

        internal void ShowStartMenu(IntPtr taskbarHwnd)
        {
            if (!EnvironmentHelper.IsWindows10RS4OrBetter || 
                FindWindowEx(IntPtr.Zero, IntPtr.Zero, "OpenShell.COwnerWindow", IntPtr.Zero) != IntPtr.Zero)
            {
                // Always use the Windows key when IImmersiveLauncher is unavailable
                // Also use the Windows key when Open Shell Menu is running, because we cannot otherwise invoke it
                ShellHelper.ShowStartMenu();
                return;
            }

            try
            {
                IImmersiveLauncher immersiveLauncher = GetImmersiveLauncher(taskbarHwnd);
                if (immersiveLauncher != null && 
                    immersiveLauncher.ShowStartView(IMMERSIVELAUNCHERSHOWMETHOD.ILSM_STARTBUTTON, IMMERSIVELAUNCHERSHOWFLAGS.ILSF_IGNORE_SET_FOREGROUND_ERROR) == 0)
                {
                    return;
                }
                ShellLogger.Warning("StartMenuMonitor: Failed to show Start menu via IImmersiveLauncher");
            }
            catch (Exception e)
            {
                ShellLogger.Warning($"StartMenuMonitor: Failed to show Start menu via IImmersiveLauncher: {e}");
            }

            ShellHelper.ShowStartMenu();
        }

        internal void HideStartMenu(IntPtr taskbarHwnd)
        {
            if (!EnvironmentHelper.IsWindows10RS4OrBetter || !isModernStartMenuOpen())
            {
                return;
            }

            IImmersiveLauncher immersiveLauncher = GetImmersiveLauncher(taskbarHwnd);
            if (immersiveLauncher != null)
            {
                immersiveLauncher.Dismiss(IMMERSIVELAUNCHERDISMISSMETHOD.ILDM_STARTTIP);
            }
        }

        public void Dispose()
        {
            _poller?.Stop();
        }

        #region Immersive launcher interfaces

        // Managed interface definitions c/o https://github.com/MishaProductions/CustomShell/

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
            public int GetHandle(nint phMonitor);
            public int IsConnected(out bool pfConnected);
            public int IsPrimary(out bool pfPrimary);
            public int GetTrustLevel(out uint level);
            public int GetDisplayRect(out Rect prcDisplayRect);
            public int GetOrientation(out uint pdwOrientation);
            public int GetWorkArea(out Rect prcWorkArea);
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
            public int GetAt(out uint idxMonitor, out IImmersiveMonitor monitor);
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
        interface IImmersiveLauncher
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

        static Guid CLSID_MonitorManager = new Guid("47094e3a-0cf2-430f-806f-cf9e4f0f12dd");
        static Guid IID_MonitorManager = new Guid("4d4c1e64-e410-4faa-bafa-59ca069bfec2");
        static Guid CLSID_ImmersiveLauncher = new Guid("6f86e01c-c649-4d61-be23-f1322ddeca9d");
        static Guid IID_ImmersiveLauncher = new Guid("d8d60399-a0f1-f987-5551-321fd1b49864");

        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IServiceProvider
        {
            int QueryService(ref Guid guidService, ref Guid riid,
                       [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        }

        [ComImport]
        [Guid("c2f03a33-21f5-47fa-b4bb-156362a2f239")]
        [ClassInterface(ClassInterfaceType.None)]
        class CImmersiveShell
        {
        }
        #endregion
    }
}
