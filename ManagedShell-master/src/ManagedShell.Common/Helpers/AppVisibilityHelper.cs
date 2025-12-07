using System;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Interfaces;
using ManagedShell.Common.Logging;
using ManagedShell.Common.SupportingClasses;

namespace ManagedShell.Common.Helpers
{
    public class AppVisibilityHelper : IDisposable
    {
        public event EventHandler<AppVisibilityEventArgs> AppVisibilityChanged;
        public event EventHandler<LauncherVisibilityEventArgs> LauncherVisibilityChanged;

        private IAppVisibility _appVis;
        private int _eventCookie;
        private bool _useEvents;

        // Note: when using events, this object must be held in memory by the consumer
        public AppVisibilityHelper(bool useEvents)
        {
            if (!EnvironmentHelper.IsWindows8OrBetter)
            {
                ShellLogger.Debug("AppVisibilityHelper: Requires Windows 8 or higher");
                return;
            }

            // get IAppVisibility instance
            _appVis = (IAppVisibility)new AppVisibility();
            _useEvents = useEvents;

            if (_appVis == null)
            {
                ShellLogger.Debug("AppVisibilityHelper: Unable create IAppVisibility");
                return;
            }

            if (!_useEvents)
            {
                return;
            }

            if (!EnvironmentHelper.IsWindows10OrBetter)
            {
                ShellLogger.Debug("AppVisibilityHelper: Events require Windows 10 or higher");
                return;
            }

            // register for events
            // only works properly on Windows 10 or later
            AppVisibilityEvents events = new AppVisibilityEvents();
            events.AppVisibilityChanged += Events_AppVisibilityChanged;
            events.LauncherVisibilityChanged += Events_LauncherVisibilityChanged;

            if (_appVis.Advise(events, out _eventCookie) == 0)
            {
                // subscribed to events successfully
                ShellLogger.Debug("AppVisibilityHelper: Subscribed to change events");
            }
            else
            {
                ShellLogger.Debug("AppVisibilityHelper: Unable to subscribe to change events");
            }
        }

        private void Events_AppVisibilityChanged(object sender, AppVisibilityEventArgs e)
        {
            AppVisibilityChanged?.Invoke(sender, e);
        }

        private void Events_LauncherVisibilityChanged(object sender, LauncherVisibilityEventArgs e)
        {
            LauncherVisibilityChanged?.Invoke(sender, e);
        }

        public bool IsLauncherVisible()
        {
            if (!EnvironmentHelper.IsWindows8OrBetter)
            {
                return false;
            }

            if (_appVis == null)
            {
                return false;
            }

            _appVis.IsLauncherVisible(out bool pfVisible);

            return pfVisible;
        }

        public MONITOR_APP_VISIBILITY GetAppVisibilityOnMonitor(IntPtr hMonitor)
        {
            if (!EnvironmentHelper.IsWindows8OrBetter)
            {
                return MONITOR_APP_VISIBILITY.MAV_UNKNOWN;
            }

            if (_appVis == null)
            {
                return MONITOR_APP_VISIBILITY.MAV_UNKNOWN;
            }

            _appVis.GetAppVisibilityOnMonitor(hMonitor, out MONITOR_APP_VISIBILITY pMode);

            return pMode;
        }

        public void Dispose()
        {
            if (!EnvironmentHelper.IsWindows8OrBetter)
            {
                return;
            }

            if (_appVis == null)
            {
                return;
            }

            if (_useEvents && _eventCookie > 0)
            {
                // unregister from events
                if (_appVis.Unadvise(_eventCookie) == 0)
                {
                    ShellLogger.Debug("AppVisibilityHelper: Unsubscribed from change events");
                }
            }
        }
    }
}
