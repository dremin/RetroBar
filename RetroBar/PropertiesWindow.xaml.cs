using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ManagedShell.Common.Helpers;
using RetroBar.Utilities;
using System.Windows;
using System.Windows.Controls;
using ManagedShell.Common.Logging;
using Microsoft.Win32;
using ManagedShell.AppBar;
using System.Windows.Forms;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        private static PropertiesWindow _instance;

        private readonly ThemeManager _themeManager;

        public IEnumerable<AppBarEdge> AppBarEdgeValues
        {
            get
            {
                return Enum.GetValues(typeof(AppBarEdge)).Cast<AppBarEdge>();
            }
        }

        private PropertiesWindow(ThemeManager themeManager)
        {
            _themeManager = themeManager;

            InitializeComponent();

            LoadAutoStart();
            LoadThemes();
        }

        public static void Open(ThemeManager themeManager)
        {
            if (_instance == null)
            {
                _instance = new PropertiesWindow(themeManager);
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }
        }

        private void LoadAutoStart()
        {
            try
            {
                RegistryKey rKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                List<string> rKeyValueNames = rKey?.GetValueNames().ToList();

                if (rKeyValueNames != null)
                {
                    if (rKeyValueNames.Contains("RetroBar"))
                    {
                        AutoStartCheckBox.IsChecked = true;
                    }
                    else
                    {
                        AutoStartCheckBox.IsChecked = false;
                    }
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"PropertiesWindow: Unable to load autorun setting from registry: {e.Message}");
            }
        }

        private void LoadThemes()
        {
            foreach (var theme in _themeManager.GetThemes())
            {
                cboThemeSelect.Items.Add(theme);
            }
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SetQuickLaunchLocation_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Quick Launch - Choose a folder";
            fbd.UseDescriptionForTitle = true;
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = Settings.Instance.QuickLaunchPath;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.Instance.QuickLaunchPath = fbd.SelectedPath;
            }
        }

        private void PropertiesWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _instance = null;
        }

        private void PropertiesWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Left = 10;
            Top = (ScreenHelper.PrimaryMonitorDeviceSize.Height / DpiHelper.DpiScale) - Height - 40;

            switch (Settings.Instance.Edge)
            {
                case AppBarEdge.Left:
                case AppBarEdge.Top:
                    Left = (SystemInformation.WorkingArea.Left / DpiHelper.DpiScale) + 10;
                    Top = (SystemInformation.WorkingArea.Top / DpiHelper.DpiScale) + 10;
                    break;
                case AppBarEdge.Right:
                    Left = (SystemInformation.WorkingArea.Right / DpiHelper.DpiScale) - Width - 10;
                    Top = (SystemInformation.WorkingArea.Top / DpiHelper.DpiScale) + 10;
                    break;
                case AppBarEdge.Bottom:
                    Left = (SystemInformation.WorkingArea.Left / DpiHelper.DpiScale) + 10;
                    Top = (SystemInformation.WorkingArea.Bottom / DpiHelper.DpiScale) - Height - 10;
                    break;
            }
        }

        private void AutoStartCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryKey rKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                var chkBox = (System.Windows.Controls.CheckBox)sender;

                if (chkBox.IsChecked.Equals(false))
                {
                    rKey?.DeleteValue("RetroBar");
                }
                else
                {
                    rKey?.SetValue("RetroBar", ExePath.GetExecutablePath());
                }
            }
            catch (Exception exception)
            {
                ShellLogger.Error($"PropertiesWindow: Unable to update registry autorun setting: {exception.Message}");
            }
        }
    }
}
