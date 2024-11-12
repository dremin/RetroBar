using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        private volatile bool _ExplorerMonitorIsMonitoring;
        private ExplorerMonitor _ExplorerMonitor;
        [DllImport("user32.dll", SetLastError = true)] private static extern uint RegisterWindowMessage(string lpString);

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
            // Ensure that when WindowManager is disposed, the ExplorerMonitor is also disposed
            if (_ExplorerMonitor != null){_ExplorerMonitor?.Dispose();}

            _shellManager.ExplorerHelper.HideExplorerTaskbar = false;
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
        }

        public void ExplorerMonitorStart()
        {
            if(_ExplorerMonitorIsMonitoring) // Prevent multiple monitors.
            {
                return;
            }
            else
            {
                _ExplorerMonitorIsMonitoring = true; // We will set flag to true to prevent multiple monitors.

                // Start monitoring
                _ExplorerMonitor = new ExplorerMonitor(this);
                _ExplorerMonitor.Show();
            }
        }

        // ExplorerMonitor is a hidden form that captures taskbar events
        public class ExplorerMonitor : Form
        {
            private readonly WindowManager _windowManager;

            public ExplorerMonitor(WindowManager windowManager)
            {
                _windowManager = windowManager; // This is for the callback

                // These will make the ExplorerMonitor form completely invisible, so we can use it just as a monitor and not form
                ClientSize = new System.Drawing.Size(1, 1); // Set the size to 1x1 pixel (tiny and invisible)
                FormBorderStyle = FormBorderStyle.None; // Make the form borderless
                BackColor = System.Drawing.Color.Lime;  // Use a color thats fully transparent in the form
                TransparencyKey = System.Drawing.Color.Lime; // Set transparency key to make the color transparent
                ShowInTaskbar = false; // Ensure the form doesnt appear in the taskbar
                ControlBox = false; // Ensure no controls (like buttons) are on the form
                Visible = false; // Set the form as invisible
                StartPosition = FormStartPosition.Manual; // Dont center this form
                Location = new System.Drawing.Point(-1000, -1000); // Move it far off-screen
            }

            // We will override WndProc to listen for TaskbarCreated event
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == (int)RegisterWindowMessage("TaskbarCreated"))
                {
                    _windowManager.OnTaskbarCreated();  // Handle TaskbarCreated event
                }

                base.WndProc(ref m);  // Call the base class to process other messages so we dont accidentally cause crashes or bugs.
            }
        }

        // Callback for TaskbarCreated event
        internal void OnTaskbarCreated()
        {
            // If TaskbarCreated event is detected, restart RetroBar for proper re-initializing.
            RestartRetroBar();
        }

        private static void RestartRetroBar()
        {
            string appPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (!string.IsNullOrEmpty(appPath))
            {
                try
                {
                    Process.Start(appPath); // Start a new instance
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restarting RetroBar: {ex.Message}");
                }

                Environment.Exit(0); // Exit the current instance
            }
            else
            {
                throw new InvalidOperationException("Unable to determine the path of RetroBar.");
            }
        }
    }
}
