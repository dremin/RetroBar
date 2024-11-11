using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace RetroBar.Utilities
{
    public class WindowManager : IDisposable
    {
        private bool _isSettingDisplays;
        private int _pendingDisplayEvents;
        private List<AppBarScreen> _screenState = new List<AppBarScreen>();
        private List<Taskbar> _taskbars = new List<Taskbar>();

        private readonly StartMenuMonitor _startMenuMonitor;
        private readonly ShellManager _shellManager;
        private readonly Updater _updater;

        private readonly int _monitorUpdateDelay = 500; // Update delay in ms for explorer monitor thread. NOTE: If this value is too high, explorer.exe will get stuck on a restart loop!
        private volatile bool _monitoring;
        private Thread _monitorThread;
        private int _lastExplorerPid = -1;

        public WindowManager(ShellManager shellManager, StartMenuMonitor startMenuMonitor, Updater updater)
        {
            _shellManager = shellManager;
            _startMenuMonitor = startMenuMonitor;
            _updater = updater;

            ExplorerMonitorStart();

            _shellManager.ExplorerHelper.HideExplorerTaskbar = true;

            openTaskbars();

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
            closeTaskbars();
            openTaskbars();
        }

        public void NotifyDisplayChange(ScreenSetupReason reason)
        {
            ShellLogger.Debug($"WindowManager: Display change notification received ({reason})");

            if (reason == ScreenSetupReason.DwmChange)
            {
                // RetroBar doesn't care when DWM is toggled
                return;
            }

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

            foreach (var taskbar in _taskbars)
            {
                taskbar.AllowClose = true;
                taskbar.Close();
            }

            _taskbars.Clear();
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
            Taskbar taskbar = new Taskbar(this, _shellManager, _startMenuMonitor, _updater, screen, Settings.Instance.Edge, Settings.Instance.AutoHide ? AppBarMode.AutoHide : AppBarMode.Normal);
            taskbar.Show();

            _taskbars.Add(taskbar);
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

        public void ExplorerMonitorStart()
        {
            // Restarting RetroBar when explorer.exe is shutdown
            // will allow explorer to initialize properly,
            // this fixes "explorer restart loop bug".

            // Prevent multiple monitor threads
            if(_monitoring){return;}

            _monitoring = true; // Set flag to true

            // Start monitor thread
            _monitorThread = new Thread(() =>
            {
                while (_monitoring) // This "while" loop is basically the main loop of the monitor thread.
                {
                    try
                    {
                        // Get the current explorer.exe process
                        Process[] explorerProcesses = Process.GetProcessesByName("explorer");

                        if (explorerProcesses.Length == 0) // explorer.exe is not running
                        {
                            // Only act if previously tracked explorer was running
                            if (Interlocked.CompareExchange(ref _lastExplorerPid, 0, 0) != -1)
                            {
                                // If we landed here, this might indicate that explorer is trying to start but failing,
                                // so we will restart RetroBar just in case RetroBar is crashing explorer.
                                RestartRetroBar();
                            }
                        }
                        else // explorer.exe is running
                        {
                            // Update current PID
                            int currentExplorerPid = explorerProcesses[0].Id;

                            // Check if PID has changed
                            int lastExplorerPid = Interlocked.CompareExchange(ref _lastExplorerPid, 0, 0);
                            if (lastExplorerPid != -1 && lastExplorerPid != currentExplorerPid)
                            {
                                // Explorer PID was changed (explorer has restarted), so we will restart RetroBar.
                                RestartRetroBar();
                            }

                            // Update the tracked PID
                            Interlocked.Exchange(ref _lastExplorerPid, currentExplorerPid);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error monitoring explorer.exe: {ex.Message}");
                    }

                    // Sleep for a short interval to avoid excessive CPU usage.
                    // CPU usage was 0.00% with 500ms delay on test system.
                    // Increase _monitorUpdateDelay value to decrease CPU usage if needed.
                    // 500ms should be fine, unless your system is a potato.
                    // NOTE: If this value is too high, explorer.exe will get stuck on a restart loop!
                    Thread.Sleep(_monitorUpdateDelay);
                }
            });

            _monitorThread.IsBackground = true; // Ensure our monitor thread exits when RetroBar is shutdown.
            _monitorThread.Start();
        }

        private static void RestartRetroBar()
        {
            // RestartRetroBar function is called if we detect that explorer.exe PID has changed.

            // Get RetroBar application path
            string appPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (!string.IsNullOrEmpty(appPath))
            {
                try
                {
                    // Start a new instance before killing the current process.
                    // There is a delay on RetroBar init so this is fine.
                    // NOTE: If the start delay of RetroBar is shortened or removed in the future,
                    // we will need to add a delay here.
                    Process.Start(appPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restarting RetroBar: {ex.Message}");
                }

                // Exit current RetroBar process.
                Environment.Exit(0);
            }
            else
            {
                throw new InvalidOperationException("Unable to determine the path of RetroBar.");
            }
        }
    }
}
