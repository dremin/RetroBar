using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ManagedShell.Common.Helpers;
using RetroBar.Utilities;
using System.Windows;
using ManagedShell.Common.Logging;
using Microsoft.Win32;
using ManagedShell.AppBar;
using System.Windows.Forms;
using ManagedShell.WindowsTray;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        private static PropertiesWindow _instance;

        private readonly double _barSize;
        private readonly DictionaryManager _dictionaryManager;
        private readonly double _dpiScale;
        private readonly NotificationArea _notificationArea;
        private readonly AppBarScreen _screen;

        // Previews should always assume bottom edge
        public AppBarEdge AppBarEdge
        {
            get => AppBarEdge.Bottom;
        }

        // Previews should always assume horizontal orientation
        public Orientation Orientation
        {
            get => Orientation.Horizontal;
        }

        private PropertiesWindow(NotificationArea notificationArea, DictionaryManager dictionaryManager, AppBarScreen screen, double dpiScale, double barSize)
        {
            _barSize = barSize;
            _dictionaryManager = dictionaryManager;
            _dpiScale = dpiScale;
            _notificationArea = notificationArea;
            _screen = screen;

            InitializeComponent();

            LoadAutoStart();
            LoadLanguages();
            LoadThemes();
            LoadVersion();
        }

        public static PropertiesWindow Open(NotificationArea notificationArea, DictionaryManager dictionaryManager, AppBarScreen screen, double dpiScale, double barSize)
        {
            if (_instance == null)
            {
                _instance = new PropertiesWindow(notificationArea, dictionaryManager, screen, dpiScale, barSize);
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }

            return _instance;
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

        private void LoadLanguages()
        {
            foreach (var language in _dictionaryManager.GetLanguages())
            {
                cboLanguageSelect.Items.Add(language);
            }
        }

        private void LoadThemes()
        {
            foreach (var theme in _dictionaryManager.GetThemes())
            {
                cboThemeSelect.Items.Add(theme);
            }
        }

        private void LoadVersion()
        {
            txtVersion.Text = string.Format((string)FindResource("version"), System.Windows.Forms.Application.ProductVersion);
        }

        private void UpdateWindowPosition()
        {
            switch (Settings.Instance.Edge)
            {
                case (int)AppBarEdge.Left:
                    Left = (_screen.Bounds.Left / _dpiScale) + _barSize + 10;
                    Top = (_screen.WorkingArea.Top / _dpiScale) + 10;
                    break;
                case (int)AppBarEdge.Top:
                    Left = (_screen.WorkingArea.Left / _dpiScale) + 10;
                    Top = (_screen.Bounds.Top / _dpiScale) + _barSize + 10;
                    break;
                case (int)AppBarEdge.Right:
                    Left = (_screen.Bounds.Right / _dpiScale) - _barSize - Width - 10;
                    Top = (_screen.WorkingArea.Top / _dpiScale) + 10;
                    break;
                case (int)AppBarEdge.Bottom:
                    Left = (_screen.WorkingArea.Left / _dpiScale) + 10;
                    Top = (_screen.Bounds.Bottom / _dpiScale) - _barSize - Height - 10;
                    break;
            }
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SetQuickLaunchLocation_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = (string)FindResource("quick_launch_folder");
#if NETCOREAPP3_0_OR_GREATER
            fbd.UseDescriptionForTitle = true;
#endif
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
            UpdateWindowPosition();
        }

        private void PropertiesWindow_OnContentRendered(object sender, EventArgs e)
        {
            UpdateWindowPosition();
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

        private void cboLanguageSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadVersion();
        }
        
        private void cboEdgeSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboEdgeSelect.SelectedItem == null)
            {
                cboEdgeSelect.SelectedValue = cboEdgeSelect.Items[Settings.Instance.Edge];
            }
        }

        private void cboMultiMonMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboMultiMonMode.SelectedItem == null)
            {
                cboMultiMonMode.SelectedValue = cboMultiMonMode.Items[Settings.Instance.MultiMonMode];
            }
        }

        private void CustomizeNotifications_OnClick(object sender, RoutedEventArgs e)
        {
            OpenCustomizeNotifications();
        }

        public void OpenCustomizeNotifications()
        {
            NotificationPropertiesWindow.Open(_notificationArea, new Point(Left, Top));
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            ShellHelper.ExecuteProcess(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
