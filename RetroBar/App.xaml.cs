using System;
using ManagedShell;
using RetroBar.Utilities;
using System.Windows;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using Application = System.Windows.Application;
using System.Windows.Interop;
using System.Windows.Media;
using ManagedShell.Common.Enums;
using System.Diagnostics;
using System.Reflection;
using ManagedShell.Common.Logging;
using System.Linq;
using System.Runtime.InteropServices;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // --- WinAPI for taskbar hide/show ---
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        // ------------------------------------

        private bool _errorVisible;
        private ManagedShellLogger _logger;
        private WindowManager _windowManager;

        private readonly DictionaryManager _dictionaryManager;
        private readonly ExplorerMonitor _explorerMonitor;
        private readonly ShellManager _shellManager;
        private readonly StartMenuMonitor _startMenuMonitor;
        private readonly Updater _updater;
        private readonly HotkeyManager _hotkeyManager;

        public App()
        {
            _shellManager = SetupManagedShell();

            _explorerMonitor = new ExplorerMonitor();
            _startMenuMonitor = new StartMenuMonitor(new AppVisibilityHelper(false));
            _dictionaryManager = new DictionaryManager();
            _updater = new Updater();
            _hotkeyManager = new HotkeyManager();

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void ExitGracefully()
        {
            _shellManager.AppBarManager.SignalGracefulShutdown();
            Current.Shutdown();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            if (Settings.Instance.UseSoftwareRendering)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }

            // Hide Windows taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            ShowWindow(taskbarHandle, SW_HIDE);

            _dictionaryManager.SetLanguageFromSettings();
            loadTheme();
            _windowManager = new WindowManager(_dictionaryManager, _explorerMonitor, _shellManager, _startMenuMonitor, _updater, _hotkeyManager);
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            // Restore Windows taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            ShowWindow(taskbarHandle, SW_SHOW);

            ExitApp();
        }

        private void App_OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            // Restore Windows taskbar
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            ShowWindow(taskbarHandle, SW_SHOW);

            ExitApp();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.UseSoftwareRendering))
            {
                if (Settings.Instance.UseSoftwareRendering)
                {
                    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                }
                else
                {
                    RenderOptions.ProcessRenderMode = RenderMode.Default;
                }
            }
            else if (e.PropertyName == nameof(Settings.Theme) || e.PropertyName == nameof(Settings.TaskbarScale))
            {
                setTaskIconSize();
            }
        }

        private void loadTheme()
        {
            _dictionaryManager.SetThemeFromSettings();
            setTaskIconSize();
        }

        private void setTaskIconSize()
        {
            bool useLargeIcons = Settings.Instance.TaskbarScale > 1 || (FindResource("UseLargeIcons") as bool? ?? false);

            if (_shellManager.TasksService.TaskIconSize != IconSize.Small != useLargeIcons)
            {
                _shellManager.TasksService.TaskIconSize = useLargeIcons ? IconSize.Large : IconSize.Small;
            }
        }

        private ShellManager SetupManagedShell()
        {
            EnvironmentHelper.IsAppRunningAsShell = NativeMethods.GetShellWindow() == IntPtr.Zero;

            _logger = new ManagedShellLogger();

            ShellConfig config = ShellManager.DefaultShellConfig;
            config.PinnedNotifyIcons = Settings.Instance.NotifyIconBehaviors.Where(setting => setting.Behavior == NotifyIconBehavior.AlwaysShow).Select(setting => setting.Identifier).ToArray();

            return new ShellManager(config);
        }

        public void RestartApp()
        {
            try
            {
                // run the program again
                Process current = new Process();
                current.StartInfo.FileName = ExePath.GetExecutablePath();
                current.Start();

                // close this instance
                ExitGracefully();
            }
            catch
            { }
        }

        private void ExitApp()
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            _explorerMonitor.Dispose();
            _windowManager.Dispose();
            _dictionaryManager.Dispose();
            _shellManager.Dispose();
            _startMenuMonitor.Dispose();
            _updater.Dispose();
            _hotkeyManager.Dispose();
            _logger.Dispose();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            string inner = "";
            if (e.Exception.InnerException != null)
                inner = "\r\n\r\nInner exception:\r\nMessage: " + e.Exception.InnerException.Message + "\r\nTarget Site: " + e.Exception.InnerException.TargetSite + "\r\n\r\n" + e.Exception.InnerException.StackTrace;

            string msg = "Would you like to restart RetroBar?\r\n\r\nPlease submit a bug report with a screenshot of this error. Thanks! \r\nMessage: " + e.Exception.Message + "\r\nTarget Site: " + e.Exception.TargetSite + "\r\nVersion: " + version + "\r\n\r\n" + e.Exception.StackTrace + inner;

            ShellLogger.Error(msg, e.Exception);

            string dMsg;

            if (msg.Length > 1000)
                dMsg = msg.Substring(0, 999) + "...";
            else
                dMsg = msg;

            try
            {
                if (!_errorVisible)
                {
                    _errorVisible = true;

                    // Automatically restart for known render thread failure messages.
                    if (e.Exception.Message.StartsWith("UCEERR_RENDERTHREADFAILURE"))
                    {
                        RestartApp();
                        Environment.FailFast("Automatically restarted RetroBar due to a render thread failure.");
                    }
                    else
                    {
                        if (MessageBox.Show(dMsg, "RetroBar Error", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            // it's like getting a morning coffee.
                            RestartApp();
                            Environment.FailFast("User restarted RetroBar due to an exception.");
                        }
                    }

                    _errorVisible = false;
                }
            }
            catch
            {
                // If this fails we're probably up the creek. Abandon ship!
                ExitGracefully();
            }

            e.Handled = true;
        }
    }
}
