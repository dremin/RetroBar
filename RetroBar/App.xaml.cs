using System;
using ManagedShell.Common.Logging;
using ManagedShell.Common.Logging.Observers;
using ManagedShell;
using RetroBar.Utilities;
using System.Windows;
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using Application = System.Windows.Application;
using System.IO;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ThemeManager ThemeManager { get; }

        private string _logPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroBar"), "Logs");
        private string _logName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff");
        private string _logExt = "log";
        private LogSeverity _logSeverity = LogSeverity.Debug;

        private Taskbar _taskbar;
        private readonly AppVisibilityHelper _appVisibilityHelper;
        private readonly ShellManager _shellManager;

        public App()
        {
            _appVisibilityHelper = new AppVisibilityHelper();
            _shellManager = SetupManagedShell();
            
            ThemeManager = new ThemeManager();
        }

        public void ExitGracefully()
        {
            _shellManager.AppBarManager.SignalGracefulShutdown();
            Current.Shutdown();
        }

        public void ReopenTaskbar()
        {
            _taskbar.AllowClose = true;
            _taskbar?.Close();
            openTaskbar();
        }

        private void openTaskbar()
        {
            _taskbar = new Taskbar(_shellManager, _appVisibilityHelper, AppBarScreen.FromPrimaryScreen(), AppBarEdge.Bottom);
            _taskbar.Show();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            ThemeManager.SetThemeFromSettings();
            openTaskbar();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            ExitApp();
        }

        private void App_OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            ExitApp();
        }

        private ShellManager SetupManagedShell()
        {
            EnvironmentHelper.IsAppRunningAsShell = NativeMethods.GetShellWindow() == IntPtr.Zero;
            
            ShellLogger.Severity = _logSeverity;

            string fileName = Path.Combine(_logPath, $"{_logName}.{_logExt}");
            FileLog fileLog = new FileLog(fileName);
            fileLog.Open();
            ShellLogger.Attach(fileLog);

            ShellLogger.Attach(new ConsoleLog());

            return new ShellManager(ShellManager.DefaultShellConfig);
        }

        private void ExitApp()
        {
            ThemeManager.Dispose();
            _shellManager.Dispose();
            _appVisibilityHelper.Dispose();
        }
    }
}
