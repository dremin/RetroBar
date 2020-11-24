using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Windows.UI.Xaml.Controls;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Common.Logging.Observers;
using ManagedShell.Configuration;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using Application = System.Windows.Application;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsShuttingDown;

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            ShellSettings.Instance.EnableTaskbar = true;
            ShellSettings.Instance.TaskbarIconSize = (int)IconSize.Small;
            ShellSettings.Instance.PinnedNotifyIcons = new [] { 
                "7820ae76-23e3-4229-82c1-e41cb67d5b9c",
                "7820ae75-23e3-4229-82c1-e41cb67d5b9c",
                "7820ae74-23e3-4229-82c1-e41cb67d5b9c",
                "7820ae73-23e3-4229-82c1-e41cb67d5b9c"
            };

            CairoLogger.Instance.Severity = LogSeverity.Debug;
            CairoLogger.Instance.Attach(new ConsoleLog());

            AppBarHelper.HideWindowsTaskbar();

            SetTheme("Themes\\WindowsXPClassic.xaml");

            Taskbar taskbar = new Taskbar(Screen.PrimaryScreen);
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

        private void ExitApp()
        {
            AppBarHelper.ShowWindowsTaskbar();
            Shell.DisposeIml();

            FullScreenHelper.Instance.Dispose();
            NotificationArea.Instance.Dispose();
            Tasks.Instance.Dispose();
        }

        private void SetTheme(string theme)
        {
            string themeFilePath = AppDomain.CurrentDomain.BaseDirectory + theme;
            if (System.IO.File.Exists(themeFilePath))
            {
                ResourceDictionary newRes = new ResourceDictionary();
                newRes.Source = new Uri(themeFilePath, UriKind.RelativeOrAbsolute);
                Resources.MergedDictionaries.Add(newRes);
            }
        }
    }
}
