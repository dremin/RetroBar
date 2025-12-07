using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.WindowsTasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.AppBar
{
    public sealed class FullScreenHelper : IDisposable
    {
        private readonly DispatcherTimer _fullscreenCheck;
        private readonly TasksService _tasksService;

        public ObservableCollection<FullScreenApp> FullScreenApps = new ObservableCollection<FullScreenApp>();
        public ObservableCollection<FullScreenApp> InactiveFullScreenApps = new ObservableCollection<FullScreenApp>();

        public FullScreenHelper(TasksService tasksService)
        {
            _tasksService = tasksService;

            if (_tasksService != null && EnvironmentHelper.IsWindows8OrBetter)
            {
                // On Windows 8 and newer, TasksService will tell us when windows enter and exit full screen
                _tasksService.FullScreenChanged += TasksService_FullScreenChanged;
                _tasksService.MonitorChanged += TasksService_Event;
                _tasksService.DesktopActivated += TasksService_Event;
                _tasksService.WindowActivated += TasksService_Event;
                return;
            }

            _fullscreenCheck = new DispatcherTimer(DispatcherPriority.Background, System.Windows.Application.Current.Dispatcher)
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100)
            };

            _fullscreenCheck.Tick += FullscreenCheck_Tick;
            _fullscreenCheck.Start();
        }

        private void TasksService_Event(object sender, EventArgs e)
        {
            updateFullScreenWindows();
        }

        private void TasksService_FullScreenChanged(object sender, FullScreenEventArgs e)
        {
            if (InactiveFullScreenApps.Count > 0 && InactiveFullScreenApps.Any(app => app.hWnd == e.Handle))
            {
                // If this window is in the inactive list, remove it--the message that triggered this event takes precedence
                InactiveFullScreenApps.Remove(InactiveFullScreenApps.First(app => app.hWnd == e.Handle));
            }

            if (FullScreenApps.Any(app => app.hWnd == e.Handle) == e.IsEntering)
            {
                if (e.IsEntering)
                {
                    FullScreenApp existingApp = FullScreenApps.First(app => app.hWnd == e.Handle);
                    if (!existingApp.fromTasksService)
                    {
                        // Grant this app TasksService treatment
                        existingApp.fromTasksService = true;
                    }
                }
                return;
            }

            if (e.IsEntering)
            {
                // When TasksService gives us a full-screen window handle, trust that it is full-screen in terms of bounds
                FullScreenApp appNew = getFullScreenApp(e.Handle, true);

                if (appNew != null)
                {
                    ShellLogger.Debug($"FullScreenHelper: Adding full screen app from TasksService {appNew.hWnd} ({appNew.title})");
                    FullScreenApps.Add(appNew);
                }
            }
            else
            {
                foreach (FullScreenApp app in FullScreenApps)
                {
                    if (app.hWnd == e.Handle)
                    {
                        ShellLogger.Debug($"FullScreenHelper: Removing full screen app from TasksService {app.hWnd} ({app.title})");
                        FullScreenApps.Remove(app);
                        break;
                    }
                }
            }
        }

        private void FullscreenCheck_Tick(object sender, EventArgs e)
        {
            updateFullScreenWindows();
        }

        private void updateFullScreenWindows()
        {
            IntPtr hWnd = GetForegroundWindow();

            List<FullScreenApp> removeApps = new List<FullScreenApp>();
            List<FullScreenApp> removeInactiveApps = new List<FullScreenApp>();
            bool skipAdd = false;

            // first check if this window is already in our list. if so, remove it if necessary
            foreach (FullScreenApp app in FullScreenApps)
            {
                FullScreenApp appCurrentState = getFullScreenApp(app.hWnd, app.fromTasksService);

                if (appCurrentState != null)
                {
                    // App is still full-screen
                    if (app.hWnd == hWnd)
                    {
                        // App is still foreground
                        if (app.screen.DeviceName != appCurrentState.screen.DeviceName)
                        {
                            // The app moved to another monitor
                            // Remove and add back to collection to trigger change events
                            // This will be added back immediately because it is foreground
                            ShellLogger.Debug($"FullScreenHelper: Monitor changed for full screen app {app.hWnd} ({app.title})");
                        }
                        else
                        {
                            // Still same screen, do nothing
                            skipAdd = true;
                            continue;
                        }
                    }
                    else if (Screen.FromHandle(hWnd).DeviceName != appCurrentState.screen.DeviceName)
                    {
                        // if the full-screen window is no longer foreground, keep it
                        // as long as the foreground window is on a different screen.
                        continue;
                    }
                    else
                    {
                        // Still full screen but no longer active
                        if ((GetWindowLong(hWnd, GWL_EXSTYLE) & (int)ExtendedWindowStyles.WS_EX_TOPMOST) == (int)ExtendedWindowStyles.WS_EX_TOPMOST)
                        {
                            // If the new foreground window is a topmost window, don't consider this full-screen app inactive
                            continue;
                        }
                        ShellLogger.Debug($"FullScreenHelper: Inactive full screen app {app.hWnd} ({app.title})");
                    }
                    InactiveFullScreenApps.Add(app);
                }

                removeApps.Add(app);
            }

            // remove any changed windows we found
            if (removeApps.Count > 0)
            {
                foreach (FullScreenApp existingApp in removeApps)
                {
                    ShellLogger.Debug($"FullScreenHelper: Removing full screen app {existingApp.hWnd} ({existingApp.title})");
                    FullScreenApps.Remove(existingApp);
                }
            }

            // clean up any inactive windows that are no longer full-screen
            if (InactiveFullScreenApps.Count > 0)
            {
                foreach (FullScreenApp app in InactiveFullScreenApps)
                {
                    FullScreenApp appCurrentState = getFullScreenApp(app.hWnd, app.fromTasksService);
                    if (appCurrentState == null)
                    {
                        // No longer a full-screen window
                        removeInactiveApps.Add(app);
                    }
                    else if (appCurrentState.screen.DeviceName != app.screen.DeviceName)
                    {
                        // The app moved to another monitor while inactive
                        app.screen = appCurrentState.screen;
                    }
                }
            }

            // remove any changed inactive windows we found
            if (removeInactiveApps.Count > 0)
            {
                foreach (FullScreenApp existingApp in removeInactiveApps)
                {
                    ShellLogger.Debug($"FullScreenHelper: Removing inactive full screen app {existingApp.hWnd} ({existingApp.title})");
                    InactiveFullScreenApps.Remove(existingApp);
                }
            }

            // check if this is a new full screen app
            if (!skipAdd)
            {
                FullScreenApp appAdd;
                bool wasInactive = false;
                if (InactiveFullScreenApps.Count > 0 && InactiveFullScreenApps.Any(app => app.hWnd == hWnd))
                {
                    // This is a previously-active full-screen app that became active again.
                    wasInactive = true;
                    appAdd = InactiveFullScreenApps.First(app => app.hWnd == hWnd);
                }
                else
                {
                    appAdd = getFullScreenApp(hWnd);
                }

                if (appAdd != null)
                {
                    ShellLogger.Debug($"FullScreenHelper: Adding{(wasInactive ? " reactivated" : "")} full screen app {appAdd.hWnd} ({appAdd.title})");
                    FullScreenApps.Add(appAdd);
                    if (wasInactive)
                    {
                        InactiveFullScreenApps.Remove(appAdd);
                    }
                }
            }
        }

        private FullScreenApp getFullScreenApp(IntPtr hWnd, bool fromTasksService = false)
        {
            ScreenInfo screenInfo = null;
            Rect rect = GetEffectiveWindowRect(hWnd);

            if (!fromTasksService)
            {
                var allScreens = Screen.AllScreens.Select(ScreenInfo.Create).ToList();
                if (allScreens.Count > 1) allScreens.Add(ScreenInfo.CreateVirtualScreen());

                foreach (var screen in allScreens)
                {
                    if (rect.Top == screen.Bounds.Top && rect.Left == screen.Bounds.Left &&
                        rect.Bottom == screen.Bounds.Bottom && rect.Right == screen.Bounds.Right)
                    {
                        screenInfo = screen;
                        break;
                    }
                }

                if (screenInfo == null)
                {
                    // If the window rect does not match any screen's bounds, it's not full screen
                    return null;
                }
            }

            ApplicationWindow win = new ApplicationWindow(null, hWnd);
            if (!CanFullScreen(win))
            {
                return null;
            }

            if (screenInfo == null)
            {
                screenInfo = ScreenInfo.Create(Screen.FromHandle(hWnd));
            }

            // this is a full screen app
            return new FullScreenApp { hWnd = hWnd, screen = screenInfo, rect = rect, title = win.Title, fromTasksService = fromTasksService };
        }

        private Rect GetEffectiveWindowRect(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            Rect rect;

            if ((((int)WindowStyles.WS_CAPTION | (int)WindowStyles.WS_THICKFRAME) & style) == ((int)WindowStyles.WS_CAPTION | (int)WindowStyles.WS_THICKFRAME) ||
                (((uint)WindowStyles.WS_POPUP | (uint)WindowStyles.WS_THICKFRAME) & style) == ((uint)WindowStyles.WS_POPUP | (uint)WindowStyles.WS_THICKFRAME) ||
                (((uint)WindowStyles.WS_POPUP | (uint)WindowStyles.WS_BORDER) & style) == ((uint)WindowStyles.WS_POPUP | (uint)WindowStyles.WS_BORDER))
            {
                GetClientRect(hWnd, out rect);
                MapWindowPoints(hWnd, IntPtr.Zero, ref rect, 2);
            }
            else
            {
                GetWindowRect(hWnd, out rect);
            }

            return rect;
        }

        private bool CanFullScreen(ApplicationWindow window)
        {
            // make sure this is not us
            GetWindowThreadProcessId(window.Handle, out uint hwndProcId);
            if (hwndProcId == GetCurrentProcessId())
            {
                return false;
            }

            // make sure this is fullscreen-able
            if (!IsWindow(window.Handle) || !IsWindowVisible(window.Handle) || IsIconic(window.Handle))
            {
                return false;
            }

            // Make sure this isn't explicitly marked as being non-rude
            IntPtr isNonRudeHwnd = GetProp(window.Handle, "NonRudeHWND");
            if (isNonRudeHwnd != IntPtr.Zero)
            {
                return false;
            }

            // make sure this is not a cloaked window
            if (EnvironmentHelper.IsWindows8OrBetter)
            {
                int cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
                DwmGetWindowAttribute(window.Handle, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out uint cloaked, cbSize);
                if (cloaked > 0)
                {
                    return false;
                }
            }

            // make sure this is not an immersive shell window
            if (window.IsImmersiveShellWindow())
            {
                return false;
            }

            if (!EnvironmentHelper.IsWindows8OrBetter)
            {
                // make sure this is not the shell desktop
                // In Windows 8 and newer, the NonRudeHWND property is set and this is not needed
                if (window.ClassName == "Progman" || window.ClassName == "WorkerW")
                {
                    return false;
                }
            }

            // make sure this is not a transparent window
            int styles = window.ExtendedWindowStyles;
            if ((styles & (int)ExtendedWindowStyles.WS_EX_LAYERED) != 0 && ((styles & (int)ExtendedWindowStyles.WS_EX_TRANSPARENT) != 0 || (styles & (int)ExtendedWindowStyles.WS_EX_NOACTIVATE) != 0))
            {
                return false;
            }

            return true;
        }

        private void ResetScreenCache()
        {
            // use reflection to empty screens cache
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var fi = typeof(Screen).GetField("screens", flags) ?? typeof(Screen).GetField("s_screens", flags);

            if (fi == null)
            {
                ShellLogger.Warning("FullScreenHelper: Unable to reset screens cache");
                return;
            }

            fi.SetValue(null, null);
        }

        public void NotifyScreensChanged()
        {
            ResetScreenCache();
        }

        public void Dispose()
        {
            _fullscreenCheck?.Stop();

            if (_tasksService != null && EnvironmentHelper.IsWindows8OrBetter)
            {
                _tasksService.FullScreenChanged -= TasksService_FullScreenChanged;
                _tasksService.MonitorChanged -= TasksService_Event;
                _tasksService.DesktopActivated -= TasksService_Event;
                _tasksService.WindowActivated -= TasksService_Event;
                return;
            }
        }
    }
}
