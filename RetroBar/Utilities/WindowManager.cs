using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RetroBar.Utilities
{
    public class WindowManager : IDisposable
    {
        private static object reopenLock = new object();

        private bool _isSettingDisplays;
        private int _pendingDisplayEvents;
        private List<AppBarScreen> _screenState = new List<AppBarScreen>();
        public List<Taskbar> taskbars = new List<Taskbar>();

        private readonly DictionaryManager _dictionaryManager;
        private readonly ExplorerMonitor _explorerMonitor;
        private readonly StartMenuMonitor _startMenuMonitor;
        private readonly ShellManager _shellManager;
        private readonly Updater _updater;

        public WindowManager(DictionaryManager dictionaryManager, ExplorerMonitor explorerMonitor, ShellManager shellManager, StartMenuMonitor startMenuMonitor, Updater updater)
        {
            _dictionaryManager = dictionaryManager;
            _explorerMonitor = explorerMonitor;
            _shellManager = shellManager;
            _startMenuMonitor = startMenuMonitor;
            _updater = updater;

            _shellManager.ExplorerHelper.HideExplorerTaskbar = true;

            openTaskbars();

            _explorerMonitor.ExplorerMonitorStart(this, _shellManager);

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.ShowMultiMon))
            {
                // Update screen state in case it has changed since last checked
                _screenState = AppBarScreen.FromAllScreens();

                if (_screenState.Count < 2)
                {
                    return;
                }

                ReopenTaskbars();
            }
        }

        public void ReopenTaskbars()
        {
            lock (reopenLock)
            {
                closeTaskbars();
                openTaskbars();
            }
        }

        public void NotifyWorkAreaChange()
        {
            ShellLogger.Debug($"WindowManager: Work area change notification received");
            handleDisplayChange();
        }

        public void NotifyDisplayChange(ScreenSetupReason reason)
        {
            ShellLogger.Debug($"WindowManager: Display change notification received ({reason})");
            handleDisplayChange();
        }

        private void handleDisplayChange()
        {
            _pendingDisplayEvents++;

            if (_isSettingDisplays)
            {
                return;
            }

            _isSettingDisplays = true;

            while (_pendingDisplayEvents > 0)
            {
                // Skip re-opening taskbars if the screens haven't changed
                if (!haveDisplaysChanged())
                {
                    _pendingDisplayEvents--;
                    continue;
                }

                ReopenTaskbars();

                _pendingDisplayEvents--;
            }

            _isSettingDisplays = false;
            ShellLogger.Debug($"WindowManager: Finished processing display events");
        }

        public bool IsValidHMonitor(IntPtr hMonitor)
        {
            foreach(var screen in _screenState)
            {
                if (screen.HMonitor == hMonitor)
                {
                    return true;
                }
            }

            return false;
        }

        private void closeTaskbars()
        {
            ShellLogger.Debug($"WindowManager: Closing all taskbars");

            foreach (var taskbar in taskbars)
            {
                taskbar.AllowClose = true;
                taskbar.Close();
            }

            taskbars.Clear();
        }

        private void openTaskbars()
        {
            _screenState = AppBarScreen.FromAllScreens();

            ShellLogger.Debug($"WindowManager: Opening taskbars");

            if (Settings.Instance.ShowMultiMon)
            {
                foreach (var screen in _screenState)
                {
                    openTaskbar(screen);
                }
            }
            else
            {
                openTaskbar(AppBarScreen.FromPrimaryScreen());
            }
        }

        private void openTaskbar(AppBarScreen screen)
        {
            ShellLogger.Debug($"WindowManager: Opening taskbar on screen {screen.DeviceName}");
            Taskbar taskbar = new Taskbar(this, _dictionaryManager, _shellManager, _startMenuMonitor, _updater, screen, Settings.Instance.Edge, Settings.Instance.AutoHide ? AppBarMode.AutoHide : AppBarMode.Normal, _explorerMonitor);
            taskbar.Show();

            taskbars.Add(taskbar);
        }

        private bool haveDisplaysChanged()
        {
            resetScreenCache();

            if (_screenState.Count == Screen.AllScreens.Length)
            {
                bool same = true;
                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    Screen current = Screen.AllScreens[i];
                    if (!(_screenState[i].Bounds == current.Bounds && _screenState[i].DeviceName == current.DeviceName && _screenState[i].Primary == current.Primary))
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                {
                    ShellLogger.Debug("WindowManager: No display changes");
                    return false;
                }
            }

            return true;
        }

        private void resetScreenCache()
        {
            // use reflection to empty screens cache
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var fi = typeof(Screen).GetField("screens", flags) ?? typeof(Screen).GetField("s_screens", flags)
                ?? throw new Exception("Can't find & reset screens cache inside winforms");
            fi.SetValue(null, null);
        }

        public void Dispose()
        {
            _shellManager.ExplorerHelper.HideExplorerTaskbar = false;
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
        }
    }
}
