using System;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Common.SupportingClasses;
using ManagedShell.Interop;

namespace RetroBar.Utilities
{
    public class StartMenuMonitor: IDisposable
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
            setVisibility(EnvironmentHelper.IsWindows8OrBetter ? isStartMenuOpen8() : isStartMenuOpen7());

            if (!_isVisible)
            {
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

        private bool isStartMenuOpen8()
        {
            if (_appVisibilityHelper == null)
            {
                ShellLogger.Error("StartMenuMonitor: AppVisibilityHelper is null");
                return false;
            }

            return _appVisibilityHelper.IsLauncherVisible();
        }

        private bool isStartMenuOpen7()
        {
            return isVisibleByClass("DV2ControlHost");
        }

        private bool isOpenShellMenuOpen()
        {
            return isVisibleByClass("OpenShell.CMenuContainer");
        }

        private bool isVisibleByClass(string className)
        {
            IntPtr hStartMenu = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, IntPtr.Zero);

            if (hStartMenu == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.IsWindowVisible(hStartMenu);
        }

        public void Dispose()
        {
            _poller?.Stop();
        }
    }
}
