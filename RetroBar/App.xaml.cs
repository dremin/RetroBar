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
        public static bool IsShuttingDown;
        public ThemeManager ThemeManager { get; }

        private readonly ShellManager _shellManager;
        private readonly ExplorerHelper _explorerHelper;
        public readonly FullScreenHelper _fullScreenHelper;

        public App()
        {
            ThemeManager = new ThemeManager();
            _fullScreenHelper = new FullScreenHelper();

            _shellManager = new ShellManager();
            _explorerHelper = new ExplorerHelper(_shellManager);
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            ThemeManager.SetThemeFromSettings();
            SetupManagedShell();

            Taskbar taskbar = new Taskbar(_shellManager, _explorerHelper, _fullScreenHelper, Screen.PrimaryScreen);
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
            CairoLogger.Severity = LogSeverity.Debug;
            CairoLogger.Attach(new ConsoleLog());
        }

        private void ExitApp()
        {
            Shell.DisposeIml();

            _fullScreenHelper.Dispose();

            _shellManager.NotificationArea.Dispose();
            _shellManager.Tasks.Dispose();
        }
    }
}
