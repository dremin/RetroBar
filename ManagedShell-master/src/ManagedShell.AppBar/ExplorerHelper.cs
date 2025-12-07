using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTray;
using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using ManagedShell.Common.Logging;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.AppBar
{
    public class ExplorerHelper
    {
        private static ABState? startupTaskbarState;
        internal NotificationArea _notificationArea;


        private readonly DispatcherTimer taskbarMonitor = new DispatcherTimer(DispatcherPriority.Background);

        private bool _hideExplorerTaskbar;

        public bool HideExplorerTaskbar
        {
            get => _hideExplorerTaskbar;

            set
            {
                if (value != _hideExplorerTaskbar)
                {
                    _hideExplorerTaskbar = value;

                    if (_hideExplorerTaskbar)
                    {
                        HideTaskbar();
                    }
                    else
                    {
                        ShowTaskbar();
                    }
                }
            }
        }

        public ExplorerHelper() : this(null)
        {
        }

        public ExplorerHelper(NotificationArea notificationArea)
        {
            _notificationArea = notificationArea;

            SetupTaskbarMonitor();
        }

        public void SetTaskbarVisibility(int swp)
        {
            // only run this if our TaskBar is enabled, or if we are showing the Windows TaskBar
            if (swp != (int)SetWindowPosFlags.SWP_HIDEWINDOW || HideExplorerTaskbar)
            {
                IntPtr taskbarHwnd = WindowHelper.FindWindowsTray(getNotifyAreaHandle());
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

        public void SetSecondaryTaskbarVisibility(int swp)
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

        public void SetTaskbarState(ABState state)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = WindowHelper.FindWindowsTray(getNotifyAreaHandle()),
                lParam = (IntPtr)state
            };

            SHAppBarMessage((int)ABMsg.ABM_SETSTATE, ref abd);
        }

        public ABState GetTaskbarState()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = WindowHelper.FindWindowsTray(getNotifyAreaHandle())
            };

            uint uState = SHAppBarMessage((int)ABMsg.ABM_GETSTATE, ref abd);

            return (ABState)uState;
        }

        private IntPtr getNotifyAreaHandle()
        {
            if (_notificationArea == null) return IntPtr.Zero;

            return _notificationArea.Handle;
        }

        private void HideTaskbar()
        {
            if (!EnvironmentHelper.IsAppRunningAsShell)
            {
                if (startupTaskbarState == null)
                {
                    startupTaskbarState = GetTaskbarState();
                }

                if (HideExplorerTaskbar)
                {
                    DoHideTaskbar();
                    taskbarMonitor.Start();
                }
            }
        }

        private void DoHideTaskbar()
        {
            SetTaskbarState(ABState.AutoHide);
            SetTaskbarVisibility((int)SetWindowPosFlags.SWP_HIDEWINDOW);
        }

        private void ShowTaskbar()
        {
            if (!EnvironmentHelper.IsAppRunningAsShell)
            {
                SetTaskbarState(startupTaskbarState ?? ABState.Default);
                SetTaskbarVisibility((int)SetWindowPosFlags.SWP_SHOWWINDOW);
                taskbarMonitor.Stop();
            }
        }

        private void SetupTaskbarMonitor()
        {
            taskbarMonitor.Interval = new TimeSpan(0, 0, 0, 0, 100);
            taskbarMonitor.Tick += TaskbarMonitor_Tick;
        }

        private void TaskbarMonitor_Tick(object sender, EventArgs e)
        {
            IntPtr taskbarHwnd = WindowHelper.FindWindowsTray(getNotifyAreaHandle());

            if (IsWindowVisible(taskbarHwnd))
            {
                ShellLogger.Debug("ExplorerHelper: Hiding unwanted Windows taskbar");
                DoHideTaskbar();
                return;
            }

            IntPtr secTaskbarHwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_SecondaryTrayWnd", null);

            // if we have 3+ monitors there may be multiple secondary TaskBars
            while (secTaskbarHwnd != IntPtr.Zero)
            {
                if (IsWindowVisible(secTaskbarHwnd))
                {
                    ShellLogger.Debug("ExplorerHelper: Hiding unwanted Windows taskbar");
                    DoHideTaskbar();
                    return;
                }

                secTaskbarHwnd = FindWindowEx(IntPtr.Zero, secTaskbarHwnd, "Shell_SecondaryTrayWnd", null);
            }
        }
    }
}