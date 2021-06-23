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

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ThemeManager ThemeManager { get; }

        private Taskbar _taskbar;
        private readonly ShellManager _shellManager;

        public App()
        {
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
            _taskbar = new Taskbar(_shellManager, AppBarScreen.FromPrimaryScreen(), AppBarEdge.Bottom);
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
            
            ShellLogger.Severity = LogSeverity.Debug;
            ShellLogger.Attach(new ConsoleLog());

            return new ShellManager(ShellManager.DefaultShellConfig);
        }

        private void ExitApp()
        {
            ThemeManager.Dispose();
            _shellManager.Dispose();
        }
    }
}
