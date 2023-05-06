using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell;
using ManagedShell.WindowsTray;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using RetroBar.Utilities;
using Application = System.Windows.Application;
using RetroBar.Controls;
using System.Diagnostics;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for Taskbar.xaml
    /// </summary>
    public partial class Taskbar : AppBarWindow
    {
        private bool _clockRightClicked;
        private bool _notifyAreaRightClicked;
        private ShellManager _shellManager;
        private Updater _updater;
        private WindowManager _windowManager;

        public Taskbar(WindowManager windowManager, ShellManager shellManager, StartMenuMonitor startMenuMonitor, Updater updater, AppBarScreen screen, AppBarEdge edge)
            : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen, edge, 0)
        {
            _shellManager = shellManager;
            _updater = updater;
            _windowManager = windowManager;

            InitializeComponent();
            DataContext = _shellManager;
            StartButton.StartMenuMonitor = startMenuMonitor;

            DesiredHeight = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarHeight") as double? ?? 0);
            DesiredWidth = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarWidth") as double? ?? 0);

            AllowsTransparency = Application.Current.FindResource("AllowsTransparency") as bool? ?? false;

            FlowDirection = Application.Current.FindResource("flow_direction") as FlowDirection? ?? FlowDirection.LeftToRight;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            if (Settings.Instance.ShowQuickLaunch)
            {
                QuickLaunchToolbar.Visibility = Visibility.Visible;
            }

            if (Settings.Instance.ShowDesktopButton)
            {
                ShowDesktopButtonTray.Visibility = Visibility.Visible;
            }

            // Hide the start button on secondary display(s)
            if (!Screen.Primary)
            {
                StartButton.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnSourceInitialized(object sender, EventArgs e)
        {
            base.OnSourceInitialized(sender, e);

            SetLayoutRounding();
            SetBlur(AllowsTransparency);
            UpdateTrayPosition();
        }
        
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            base.WndProc(hwnd, msg, wParam, lParam, ref handled);

            if ((msg == (int)NativeMethods.WM.SYSCOLORCHANGE || 
                    msg == (int)NativeMethods.WM.SETTINGCHANGE) && 
                Settings.Instance.Theme.StartsWith(DictionaryManager.THEME_DEFAULT))
            {
                handled = true;

                // If the color scheme changes, re-apply the current theme to get updated colors.
                ((App)Application.Current).DictionaryManager.SetThemeFromSettings();
            }

            return IntPtr.Zero;
        }

        private void SetLayoutRounding()
        {
            // Layout rounding causes incorrect sizing on non-integer scales
            if (DpiScale % 1 != 0)
            {
                UseLayoutRounding = false;
            }
            else
            {
                UseLayoutRounding = true;
            }
        }

        private void UpdateTrayPosition()
        {
            if (Screen.Primary)
            {
                SetTrayHost();
            }
        }

        private void RecalculateSize()
        {
            double newHeight = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarHeight") as double? ?? 0);
            double newWidth = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarWidth") as double? ?? 0);
            bool heightChanged = newHeight != DesiredHeight;
            bool widthChanged = newWidth != DesiredWidth;

            DesiredHeight = newHeight;
            DesiredWidth = newWidth;

            if (Orientation == Orientation.Horizontal && heightChanged)
            {
                Height = DesiredHeight;
                SetScreenPosition();
            }
            else if (Orientation == Orientation.Vertical && widthChanged)
            {
                Width = DesiredWidth;
                SetScreenPosition();
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                bool newTransparency = Application.Current.FindResource("AllowsTransparency") as bool? ?? false;

                if (AllowsTransparency != newTransparency && Screen.Primary)
                {
                    // Transparency cannot be changed on an open window.
                    _windowManager.ReopenTaskbars();
                    return;
                }

                RecalculateSize();
            }
            else if (e.PropertyName == "ShowQuickLaunch")
            {
                if (Settings.Instance.ShowQuickLaunch)
                {
                    QuickLaunchToolbar.Visibility = Visibility.Visible;
                }
                else
                {
                    QuickLaunchToolbar.Visibility = Visibility.Collapsed;
                }
            }
            else if (e.PropertyName == "Edge")
            {
                AppBarEdge = (AppBarEdge)Settings.Instance.Edge;
                SetScreenPosition();
            }
            else if (e.PropertyName == "Language")
            {
                FlowDirection newFlowDirection = Application.Current.FindResource("flow_direction") as FlowDirection? ?? FlowDirection.LeftToRight;

                if (FlowDirection != newFlowDirection && Screen.Primary)
                {
                    // It is necessary to reopen the taskbars to refresh menu sizes.
                    _windowManager.ReopenTaskbars();
                    return;
                }
            }
            else if (e.PropertyName == "ShowDesktopButton")
            {
                if (Settings.Instance.ShowDesktopButton)
                {
                    ShowDesktopButtonTray.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowDesktopButtonTray.Visibility = Visibility.Collapsed;
                }
            }
            else if (e.PropertyName == "TaskbarScale")
            {
                RecalculateSize();
            }
        }

        private void Taskbar_OnLocationChanged(object sender, EventArgs e)
        {
            // primarily for win7/8, they will set up the appbar correctly but then put it in the wrong place
            if (Orientation == Orientation.Vertical)
            {
                double desiredLeft = 0;

                if (AppBarEdge == AppBarEdge.Left)
                {
                    desiredLeft = Screen.Bounds.Left / DpiScale;
                }
                else if (AppBarEdge == AppBarEdge.Right)
                {
                    desiredLeft = Screen.Bounds.Right / DpiScale - Width;
                }

                if (Left != desiredLeft) Left = desiredLeft;
            }
            else
            {
                double desiredTop = 0;

                if (AppBarEdge == AppBarEdge.Top)
                {
                    desiredTop = Screen.Bounds.Top / DpiScale;
                }
                else if (AppBarEdge == AppBarEdge.Bottom)
                {
                    desiredTop = Screen.Bounds.Bottom / DpiScale - Height;
                }

                if (Top != desiredTop) Top = desiredTop;
            }

            UpdateTrayPosition();
            StartButton?.UpdateFloatingStartCoordinates();
        }

        private void Taskbar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTrayPosition();
            StartButton?.UpdateFloatingStartCoordinates();
        }

        private void DateTimeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShellHelper.StartProcess("timedate.cpl");
        }

        private void CustomizeNotificationsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            PropertiesWindow propWindow = PropertiesWindow.Open(_shellManager.NotificationArea, ((App)Application.Current).DictionaryManager, Screen, DpiScale, Orientation == Orientation.Horizontal ? DesiredHeight : DesiredWidth);
            propWindow.OpenCustomizeNotifications();
        }

        private void TaskManagerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShellHelper.StartTaskManager();
        }

        private void UpdateAvailableMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = _updater.DownloadUrl,
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        private void PropertiesMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            PropertiesWindow.Open(_shellManager.NotificationArea, ((App)Application.Current).DictionaryManager, Screen, DpiScale, Orientation == Orientation.Horizontal ? DesiredHeight : DesiredWidth);
        }

        private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (NativeMethods.GetAsyncKeyState((int)System.Windows.Forms.Keys.ShiftKey) < 0)
            {
                ((App)Application.Current).RestartApp();
            }
            else
            {
                ((App)Application.Current).ExitGracefully();
            }
        }

        protected override void CustomClosing()
        {
            if (AllowClose)
            {
                QuickLaunchToolbar.Visibility = Visibility.Collapsed;
                
                Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            }
        }

        protected override void SetScreenProperties(ScreenSetupReason reason)
        {
            if (reason == ScreenSetupReason.DpiChange)
            {
                // DPI change is per-monitor, update ourselves
                SetScreenPosition();
                SetLayoutRounding();
                return;
            }

            if (Settings.Instance.ShowMultiMon)
            {
                // Re-create RetroBar windows based on new screen setup
                _windowManager.NotifyDisplayChange(reason);
            }
            else
            {
                // Update window as necessary
                base.SetScreenProperties(reason);
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (_updater.IsUpdateAvailable)
            {
                UpdateAvailableMenuItem.Visibility = Visibility.Visible;
            }

            // Some menu items should only be accessible when the clock is what was right-clicked

            if (_clockRightClicked)
            {
                DateTimeMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                DateTimeMenuItem.Visibility = Visibility.Collapsed;
            }

            if(_notifyAreaRightClicked && Settings.Instance.CollapseNotifyIcons)
            {
                CustomizeNotificationsMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                CustomizeNotificationsMenuItem.Visibility = Visibility.Collapsed;
            }

            if (_clockRightClicked || (_notifyAreaRightClicked && Settings.Instance.CollapseNotifyIcons))
            {
                NotificationAreaSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                NotificationAreaSeparator.Visibility = Visibility.Collapsed;
            }

            _clockRightClicked = false;
            _notifyAreaRightClicked = false;
        }

        public void SetTrayHost()
        {
            _shellManager.NotificationArea.SetTrayHostSizeData(new TrayHostSizeData
            {
                edge = (NativeMethods.ABEdge)AppBarEdge,
                rc = new NativeMethods.Rect
                {
                    Top = (int)(Top * DpiScale),
                    Left = (int)(Left * DpiScale),
                    Bottom = (int)((Top + Height) * DpiScale),
                    Right = (int)((Left + Width) * DpiScale)
                }
            });
        }

        private void Clock_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _clockRightClicked = true;
        }

        private void NotifyArea_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _notifyAreaRightClicked = true;
        }
    }
}
