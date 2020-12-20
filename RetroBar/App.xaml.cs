using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Common.Logging.Observers;
using ManagedShell.Management;
using RetroBar.Utilities;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ThemeManager ThemeManager { get; }

        private readonly ShellManager _shellManager;

        public App()
        {
            ThemeManager = new ThemeManager();

            _shellManager = new ShellManager();
        }

        public void ExitGracefully()
        {
            _shellManager.AppBarManager.SignalGracefulShutdown();
            Current.Shutdown();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            ThemeManager.SetThemeFromSettings();
            SetupManagedShell();

            Taskbar taskbar = new Taskbar(_shellManager, Screen.PrimaryScreen);
            taskbar.Show();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            ExitApp();
        }

        private void App_OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            ExitApp();
        }

        private void SetupManagedShell()
        {
            ShellLogger.Severity = LogSeverity.Debug;
            ShellLogger.Attach(new ConsoleLog());
        }

        private void ExitApp()
        {
            Shell.DisposeIml();

            _shellManager.FullScreenHelper.Dispose();

            _shellManager.NotificationArea.Dispose();
            _shellManager.Tasks.Dispose();
        }
    }
}
