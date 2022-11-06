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

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public DictionaryManager DictionaryManager { get; }

        private ManagedShellLogger _logger;
        private WindowManager _windowManager;

        private readonly StartMenuMonitor _startMenuMonitor;
        private readonly ShellManager _shellManager;
        private readonly Updater _updater;

        public App()
        {
            _shellManager = SetupManagedShell();

            _startMenuMonitor = new StartMenuMonitor(new AppVisibilityHelper(false));
            DictionaryManager = new DictionaryManager();
            _updater = new Updater();

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

            DictionaryManager.SetLanguageFromSettings();
            loadTheme();
            _windowManager = new WindowManager(_shellManager, _startMenuMonitor, _updater);
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            ExitApp();
        }

        private void App_OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            ExitApp();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UseSoftwareRendering")
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
            else if (e.PropertyName == "Theme")
            {
                setTaskIconSize();
            }
        }

        private void loadTheme()
        {
            DictionaryManager.SetThemeFromSettings();
            setTaskIconSize();
        }

        private void setTaskIconSize()
        {
            bool useLargeIcons = FindResource("UseLargeIcons") as bool? ?? false;

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
            config.PinnedNotifyIcons = Settings.Instance.PinnedNotifyIcons;

            return new ShellManager(config);
        }

        private void ExitApp()
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            _windowManager.Dispose();
            DictionaryManager.Dispose();
            _shellManager.Dispose();
            _startMenuMonitor.Dispose();
            _updater.Dispose();
            _logger.Dispose();
        }
    }
}
