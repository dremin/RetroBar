using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

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

        private readonly int _ExplorerMonitorUpdateDelay = 300; // Update delay in ms for explorer monitor thread. NOTE: If this value is too high, explorer.exe will get stuck on a restart loop!
        private volatile bool _ExplorerMonitorIsMonitoring;
        private Thread _ExplorerMonitorThread;
        private int _ExplorerMonitorLastExplorerPid = -1;

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
            // Restarting RetroBar when Explorer is shutdown
            // will allow Explorer to initialize properly,
            // this fixes crash-restart-loop bug.

            // Prevent multiple monitor threads.
            if(_ExplorerMonitorIsMonitoring){return;} 

            _ExplorerMonitorIsMonitoring = true; // Set flag to true, to prevent multiple monitor threads.

            // Get the expected shell (Explorer) executable name and path from the registry.
            string shellExecutableName = GetShellExecutableNameFromRegistry();
            string expectedShellPath = GetShellPathFromRegistry();

            // Start monitor thread.
            _ExplorerMonitorThread = new Thread(() =>
            {
                while (_ExplorerMonitorIsMonitoring) // This "while" loop is basically the main loop of the monitor thread.
                {
                    try
                    {
                        // Get the desktop shell process ID.
                        uint shellProcessId = GetShellProcessId();

                        // Get the current processes with the retrieved executable name.
                        Process[] explorerProcesses = Process.GetProcessesByName(shellExecutableName);

                        // Filter processes.
                        var validExplorerProcesses = explorerProcesses.Where(p =>
                        {
                            try
                            {
                                string processPath = p.MainModule?.FileName;

                                if (shellProcessId == 0) // We did not find shellProcessId, or we are using custom shell
                                {
                                    return string.Equals(processPath, expectedShellPath, StringComparison.OrdinalIgnoreCase);
                                }
                                else // shellProcessId was found
                                {
                                    return string.Equals(processPath, expectedShellPath, StringComparison.OrdinalIgnoreCase) && (uint)p.Id == shellProcessId;
                                }
                            }
                            catch
                            {
                                return false; // Skip inaccessible processes.
                            }
                        }).ToList();


                        if (!validExplorerProcesses.Any()) // No valid processes found.
                        {
                            // Only act if previously tracked Explorer was running.
                            if (Interlocked.CompareExchange(ref _ExplorerMonitorLastExplorerPid, 0, 0) != -1)
                            {
                                // If we landed here, this might indicate that Explorer is trying to start but failing,
                                // or Explorer was closed manually, so we will restart RetroBar just in case RetroBar is causing a crash.
                                RestartRetroBar();
                            }
                        }
                        else  // Valid process was found.
                        {
                            Process desktopShellProcess = validExplorerProcesses.First();

                            int currentExplorerPid = desktopShellProcess.Id;

                            // Check if PID has changed.
                            int lastExplorerPid = Interlocked.CompareExchange(ref _ExplorerMonitorLastExplorerPid, 0, 0);
                            if (lastExplorerPid != -1 && lastExplorerPid != currentExplorerPid)
                            {
                                // Explorer PID was changed (Explorer has restarted), so we will restart RetroBar.
                                RestartRetroBar();
                            }

                            // Update the tracked PID.
                            Interlocked.Exchange(ref _ExplorerMonitorLastExplorerPid, currentExplorerPid);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error monitoring explorer.exe: {ex.Message}");
                    }

                    // Sleep for a short interval to avoid excessive CPU usage.
                    // CPU usage was 0.00% with 300ms delay on test systems.
                    // 300ms should be fine, unless your system is a potato.
                    // NOTE: You can increase _ExplorerMonitorUpdateDelay value to decrease CPU usage,
                    // but if this value is too high, Explorer will get stuck on a crash-restart-loop!
                    Thread.Sleep(_ExplorerMonitorUpdateDelay);
                }
            });

            _ExplorerMonitorThread.IsBackground = true; // Ensure our monitor thread exits when RetroBar exits.
            _ExplorerMonitorThread.Start();
        }

        private static string GetShellExecutableNameFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    string shellValue = key?.GetValue("Shell") as string;

                    if (!string.IsNullOrEmpty(shellValue))
                    {
                        return System.IO.Path.GetFileNameWithoutExtension(shellValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading shell executable name from registry: {ex.Message}");
            }

            // Default to "explorer" if registry lookup fails.
            return "explorer";
        }

        private static string GetShellPathFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    // "Shell" key could either be explorer.exe or a custom shell path
                    string shellValue = key?.GetValue("Shell") as string;

                    if (!string.IsNullOrEmpty(shellValue))
                    {
                        // If the shell is "explorer.exe", construct its full path from the SystemRoot variable
                        if (shellValue.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                            if (!string.IsNullOrEmpty(systemRoot))
                            {
                                return System.IO.Path.Combine(systemRoot, "explorer.exe");
                            }
                        }
                        // If the shell is a custom executable, return the full path (if its already a full path)
                        else if (System.IO.Path.IsPathRooted(shellValue))
                        {
                            return shellValue; // Return the custom shells full path
                        }
                        else
                        {
                            // If the shell value is a relative path, we could assume its in the SystemRoot directory
                            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                            if (!string.IsNullOrEmpty(systemRoot))
                            {
                                return System.IO.Path.Combine(systemRoot, shellValue);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading shell path from registry: {ex.Message}");
            }

            // Fallback to default explorer.exe path if something goes wrong or no shell is found
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\explorer.exe");
        }

        private static uint GetShellProcessId()
        {
            // This is a call that is available on Windows 10 and 11.
            // It will return the exact explorer process that is hosting the desktop shell,
            // this ensures we dont accidentally target something like "File Explorer".
            IntPtr shellWindowHandle = NativeMethods.GetShellWindow();

            if (shellWindowHandle == IntPtr.Zero)
            {
                Console.WriteLine("Shell window not found.");
                return 0;
            }

            NativeMethods.GetWindowThreadProcessId(shellWindowHandle, out uint shellProcessId);
            return shellProcessId;
        }

        private static void RestartRetroBar()
        {
            // RestartRetroBar function is called if we detect that explorer.exe PID has changed.

            // Get RetroBar application path.
            string appPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (!string.IsNullOrEmpty(appPath))
            {
                try
                {
                    // Start a new instance before killing the current process.
                    // There is a delay on RetroBar init so this is fine.
                    // NOTE: If the start delay of RetroBar is decreased or removed in the future,
                    // we will need to add a delay before starting new RetroBar instance.
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
