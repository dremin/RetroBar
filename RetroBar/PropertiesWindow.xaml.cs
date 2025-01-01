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
using System.Runtime.CompilerServices;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window, INotifyPropertyChanged
    {
        private static PropertiesWindow _instance;

        private readonly double _barSize;
        private readonly DictionaryManager _dictionaryManager;
        private readonly double _dpiScale;
        private readonly NotificationArea _notificationArea;
        private readonly AppBarScreen _screen;

        public event PropertyChangedEventHandler PropertyChanged;

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

        // Previews should always assume normal mode
        public AppBarMode AppBarMode
        {
            get => AppBarMode.Normal;
        }

        // Previews should reflect the locked setting
        public bool IsLocked
        {
            get => Settings.Instance.LockTaskbar;
        }

        // Previews should never be scaled
        public bool IsScaled
        {
            get => false;
        }

        // Previews should always assume 1 row
        public int Rows
        {
            get => 1;
        }

        private PropertiesWindow(NotificationArea notificationArea, DictionaryManager dictionaryManager, AppBarScreen screen, double dpiScale, double barSize)
        {
            _barSize = barSize;
            _dictionaryManager = dictionaryManager;
            _dpiScale = dpiScale;
            _notificationArea = notificationArea;
            _screen = screen;

            InitializeComponent();

            LoadPreviewHeight();
            LoadAutoStart();
            LoadLanguages();
            LoadRows();
            LoadThemes();
            LoadWidth();
            LoadVersion();
            LoadClockActions();

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.LockTaskbar))
            {
                OnPropertyChanged(nameof(IsLocked));
                LoadPreviewHeight();
            }
            else if (e.PropertyName == nameof(Settings.Theme))
            {
                LoadPreviewHeight();
            }
            else if (e.PropertyName == nameof(Settings.Language))
            {
                LoadVersion();
                LoadClockActions();
            }
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadPreviewHeight()
        {
            double size = System.Windows.Application.Current.FindResource("TaskbarHeight") as double? ?? 0;

            if (!IsLocked)
            {
                size += System.Windows.Application.Current.FindResource("TaskbarUnlockedSize") as double? ?? 0;
            }

            TaskbarAppearancePreviewControl.Height = size;
            NotificationAreaPreviewControl.Height = size;
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

        private void LoadRows()
        {
            for (int i = 1; i <= Settings.Instance.RowLimit; i++)
            {
                cboRowCount.Items.Add(i.ToString());
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

        private void LoadWidth()
        {
            sldTaskbarWidth.Maximum = Settings.Instance.TaskbarWidthLimit;
        }

        private void UpdateWindowPosition()
        {
            switch (Settings.Instance.Edge)
            {
                case AppBarEdge.Left:
                    Left = (_screen.Bounds.Left / _dpiScale) + _barSize + 10;
                    Top = (_screen.WorkingArea.Top / _dpiScale) + 10;
                    break;
                case AppBarEdge.Top:
                    Left = (_screen.WorkingArea.Left / _dpiScale) + 10;
                    Top = (_screen.Bounds.Top / _dpiScale) + _barSize + 10;
                    break;
                case AppBarEdge.Right:
                    Left = (_screen.Bounds.Right / _dpiScale) - _barSize - Width - 10;
                    Top = (_screen.WorkingArea.Top / _dpiScale) + 10;
                    break;
                case AppBarEdge.Bottom:
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

        private void LoadClockActions()
        {
            if (EnvironmentHelper.IsWindows10OrBetter)
            {
               return;
            }

            // Remove options unsupported prior to Windows 10.
            var availableClockActions = (FindResource("clock_click_action_values") as Array)?.Cast<object>().ToList();
            if (availableClockActions == null)
            {
                return;
            }

            if (Settings.Instance.ClockClickAction > ClockClickOption.OpenAeroCalendar)
            {
                // ClockClickAction is out of range; reverting to default
                cboClockAction.SelectedValue = availableClockActions[(int)ClockClickOption.DoNothing];
            }

            availableClockActions.RemoveAt((int)ClockClickOption.OpenNotificationCenter);
            availableClockActions.RemoveAt((int)ClockClickOption.OpenModernCalendar);
            cboClockAction.ItemsSource = availableClockActions;
        }

        private void cboEdgeSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboEdgeSelect.SelectedItem == null)
            {
                cboEdgeSelect.SelectedValue = cboEdgeSelect.Items[(int)Settings.Instance.Edge];
            }
        }

        private void cboMultiMonMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboMultiMonMode.SelectedItem == null)
            {
                cboMultiMonMode.SelectedValue = cboMultiMonMode.Items[(int)Settings.Instance.MultiMonMode];
            }
        }

        private void cboInvertIconsMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboInvertIconsMode.SelectedItem == null)
            {
                cboInvertIconsMode.SelectedValue = cboInvertIconsMode.Items[(int)Settings.Instance.InvertIconsMode];
            }
        }

        private void cboMiddleMouseAction_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboMiddleMouseAction.SelectedItem == null)
            {
                cboMiddleMouseAction.SelectedValue = cboMiddleMouseAction.Items[(int)Settings.Instance.TaskMiddleClickAction];
            }
        }

        private void cboClockAction_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboClockAction.SelectedItem == null)
            {
                cboClockAction.SelectedValue = cboClockAction.Items[(int)Settings.Instance.ClockClickAction];
            }
        }

        private void cboRowCount_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboRowCount.SelectedItem == null)
            {
                cboRowCount.SelectedValue = cboRowCount.Items[Settings.Instance.RowCount - 1];
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