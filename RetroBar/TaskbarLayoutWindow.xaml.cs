using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using RetroBar.Utilities;
using System.ComponentModel;
using System.Windows;

namespace RetroBar
{
    /// <summary>
    /// Rows and visible items of the taskbars: for all of them together,
    /// or for the primary and the other monitors when they are configured separately.
    /// </summary>
    public partial class TaskbarLayoutWindow : Window
    {
        private static TaskbarLayoutWindow _instance;

        private bool _isLoading;

        private TaskbarLayoutWindow(AppBarScreen screen)
        {
            InitializeComponent();

            spnRowCount.Maximum = Settings.Instance.RowLimit;

            LoadMonitors(screen);

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
            LoadValues();
        }

        public static void Open(Window owner, AppBarScreen screen, Point position)
        {
            if (_instance == null)
            {
                _instance = new TaskbarLayoutWindow(screen);
                _instance.Owner = owner;
                _instance.Left = position.X + 10;
                _instance.Top = position.Y + 10;
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }
        }

        private class MonitorItem
        {
            public string Id;
            public bool Primary;
            public string Label;
            public override string ToString() => Label;
        }

        private void LoadMonitors(AppBarScreen current)
        {
            int number = 0;
            MonitorItem selected = null;

            foreach (AppBarScreen screen in AppBarScreen.FromAllScreens())
            {
                number++;
                MonitorIdentifier.MonitorInfo info = MonitorIdentifier.GetMonitor(screen.DeviceName);
                string label = $"{number} - {info.Name}";

                if (screen.Primary)
                {
                    label += " " + (string)FindResource("taskbar_layout_primary_suffix");
                }

                MonitorItem item = new MonitorItem { Id = info.Id, Primary = screen.Primary, Label = label };
                cboMonitor.Items.Add(item);

                if (current != null ? screen.DeviceName == current.DeviceName : screen.Primary)
                {
                    selected = item;
                }
            }

            // The global settings, used by monitors that have no settings of their own.
            cboMonitor.Items.Add(new MonitorItem { Id = null, Label = (string)FindResource("taskbar_layout_default") });
            cboMonitor.SelectedItem = selected ?? cboMonitor.Items[0];
        }

        private MonitorItem SelectedMonitor => cboMonitor.SelectedItem as MonitorItem;

        // True when editing one monitor's own settings; false when editing the global settings.
        private bool IsSeparate => Settings.Instance.UseMonitorOverrides && SelectedMonitor?.Id != null;

        private void LoadValues()
        {
            Settings settings = Settings.Instance;
            _isLoading = true;

            MonitorPanel.Visibility = settings.UseMonitorOverrides ? Visibility.Visible : Visibility.Collapsed;
            RowsPanel.Visibility = settings.Edge is AppBarEdge.Top or AppBarEdge.Bottom ? Visibility.Visible : Visibility.Collapsed;

            if (IsSeparate)
            {
                MonitorItem monitor = SelectedMonitor;
                spnRowCount.Value = settings.GetRowCount(monitor.Id);
                cbShowStartButton.IsChecked = settings.GetShowStartButton(monitor.Id, monitor.Primary);
                cbShowQuickLaunch.IsChecked = settings.GetShowQuickLaunch(monitor.Id);
                cbShowNotifyIcons.IsChecked = settings.GetShowNotifyIcons(monitor.Id);
                cbShowClock.IsChecked = settings.GetShowClock(monitor.Id);
                cbShowStartButtonMultiMon.Visibility = Visibility.Collapsed;
                // Start button on secondary taskbars is supported on Windows 10 and later only.
                cbShowStartButton.IsEnabled = monitor.Primary || EnvironmentHelper.IsWindows10OrBetter;
            }
            else
            {
                spnRowCount.Value = settings.RowCount;
                cbShowStartButton.IsChecked = settings.ShowStartButton;
                cbShowStartButtonMultiMon.IsChecked = settings.ShowStartButtonMultiMon;
                cbShowQuickLaunch.IsChecked = settings.ShowQuickLaunch;
                cbShowNotifyIcons.IsChecked = settings.ShowNotifyIcons;
                cbShowClock.IsChecked = settings.ShowClock;
                cbShowStartButton.IsEnabled = true;

                // Start button on secondary taskbars is supported on Windows 10 and later only.
                cbShowStartButtonMultiMon.Visibility = settings.ShowMultiMon && EnvironmentHelper.IsWindows10OrBetter ? Visibility.Visible : Visibility.Collapsed;
                cbShowStartButtonMultiMon.IsEnabled = settings.ShowStartButton;
            }

            _isLoading = false;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.RowCount):
                case nameof(Settings.ShowStartButton):
                case nameof(Settings.ShowStartButtonMultiMon):
                case nameof(Settings.ShowQuickLaunch):
                case nameof(Settings.ShowNotifyIcons):
                case nameof(Settings.ShowClock):
                case nameof(Settings.ShowMultiMon):
                case nameof(Settings.ConfigureTaskbarsSeparately):
                case nameof(Settings.MonitorOverrides):
                case nameof(Settings.Edge):
                    // Also covers rows changed by dragging the taskbar edge
                    LoadValues();
                    break;
            }
        }

        private void CboMonitor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isLoading && IsLoaded)
            {
                LoadValues();
            }
        }

        private void SpnRowCount_ValueChangedByUser(object sender, System.EventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            int rows = spnRowCount.Value;

            if (IsSeparate)
            {
                Settings.Instance.SetMonitorOverride(SelectedMonitor.Id, o => o.RowCount = rows);
            }
            else
            {
                Settings.Instance.RowCount = rows;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            bool value = (sender as System.Windows.Controls.CheckBox)?.IsChecked == true;
            Settings settings = Settings.Instance;

            if (IsSeparate)
            {
                if (sender == cbShowStartButton) settings.SetMonitorOverride(SelectedMonitor.Id, o => o.ShowStartButton = value);
                else if (sender == cbShowQuickLaunch) settings.SetMonitorOverride(SelectedMonitor.Id, o => o.ShowQuickLaunch = value);
                else if (sender == cbShowNotifyIcons) settings.SetMonitorOverride(SelectedMonitor.Id, o => o.ShowNotifyIcons = value);
                else if (sender == cbShowClock) settings.SetMonitorOverride(SelectedMonitor.Id, o => o.ShowClock = value);
                return;
            }

            if (sender == cbShowStartButton) settings.ShowStartButton = value;
            else if (sender == cbShowStartButtonMultiMon) settings.ShowStartButtonMultiMon = value;
            else if (sender == cbShowQuickLaunch) settings.ShowQuickLaunch = value;
            else if (sender == cbShowNotifyIcons) settings.ShowNotifyIcons = value;
            else if (sender == cbShowClock) settings.ShowClock = value;
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            _instance = null;
        }
    }
}
