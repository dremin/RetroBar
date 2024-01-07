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
using System.Windows.Input;
using ManagedShell.Common.Logging;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for Taskbar.xaml
    /// </summary>
    public partial class Taskbar : AppBarWindow
    {
        public bool IsLocked
        {
            get
            {
                return Settings.Instance.LockTaskbar;
            }
        }

        public bool IsScaled
        {
            get
            {
                return DpiScale > 1 || Settings.Instance.TaskbarScale > 1;
            }
        }

        private bool _clockRightClicked;
        private bool _notifyAreaRightClicked;
        private bool _startMenuOpen;
        private LowLevelMouseHook _mouseDragHook;
        private Point? _mouseDragStart = null;
        private ShellManager _shellManager;
        private Updater _updater;

        public WindowManager windowManager;

        public Taskbar(WindowManager windowManager, ShellManager shellManager, StartMenuMonitor startMenuMonitor, Updater updater, AppBarScreen screen, AppBarEdge edge, AppBarMode mode)
            : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen, edge, mode, 0)
        {
            _shellManager = shellManager;
            _updater = updater;
            this.windowManager = windowManager;

            InitializeComponent();
            DataContext = _shellManager;
            StartButton.StartMenuMonitor = startMenuMonitor;

            DesiredHeight = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarHeight") as double? ?? 0);
            DesiredWidth = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarWidth") as double? ?? 0);

            if (AppBarMode == AppBarMode.AutoHide || !Settings.Instance.LockTaskbar)
            {
                double unlockedSize = Application.Current.FindResource("TaskbarUnlockedSize") as double? ?? 0;
                DesiredHeight += unlockedSize;
                DesiredWidth += unlockedSize;
            }

            AllowsTransparency = mode == AppBarMode.AutoHide || (Application.Current.FindResource("AllowsTransparency") as bool? ?? false);

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

            AutoHideElement = TaskbarContentControl;

            PropertyChanged += Taskbar_PropertyChanged;
        }

        private void Taskbar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DpiScale")
            {
                OnPropertyChanged("IsScaled");
            }
        }

        protected override void OnSourceInitialized(object sender, EventArgs e)
        {
            base.OnSourceInitialized(sender, e);

            SetLayoutRounding();
            SetBlur(Application.Current.FindResource("AllowsTransparency") as bool? ?? false);
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

            if (AppBarMode == AppBarMode.AutoHide || !Settings.Instance.LockTaskbar)
            {
                double unlockedSize = Application.Current.FindResource("TaskbarUnlockedSize") as double? ?? 0;
                newHeight += unlockedSize;
                newWidth += unlockedSize;
            }

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
                bool newTransparency = AppBarMode == AppBarMode.AutoHide || (Application.Current.FindResource("AllowsTransparency") as bool? ?? false);

                if (AllowsTransparency != newTransparency && Screen.Primary)
                {
                    // Transparency cannot be changed on an open window.
                    windowManager.ReopenTaskbars();
                    return;
                }

                SetBlur(Application.Current.FindResource("AllowsTransparency") as bool? ?? false);
                PeekDuringAutoHide();
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
                PeekDuringAutoHide();
                AppBarEdge = Settings.Instance.Edge;
                SetScreenPosition();
            }
            else if (e.PropertyName == "Language")
            {
                FlowDirection newFlowDirection = Application.Current.FindResource("flow_direction") as FlowDirection? ?? FlowDirection.LeftToRight;

                if (FlowDirection != newFlowDirection && Screen.Primary)
                {
                    // It is necessary to reopen the taskbars to refresh menu sizes.
                    windowManager.ReopenTaskbars();
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
                PeekDuringAutoHide();
                RecalculateSize();
                OnPropertyChanged("IsScaled");
            }
            else if (e.PropertyName == "AutoHide")
            {
                bool newTransparency = Settings.Instance.AutoHide || (Application.Current.FindResource("AllowsTransparency") as bool? ?? false);

                if (AllowsTransparency == newTransparency)
                {
                    AppBarMode = Settings.Instance.AutoHide ? AppBarMode.AutoHide : AppBarMode.Normal;
                }
                else if (Screen.Primary)
                {
                    // Auto hide requires transparency
                    // Transparency cannot be changed on an open window.
                    windowManager.ReopenTaskbars();
                }
            }
            else if (e.PropertyName == "LockTaskbar")
            {
                OnPropertyChanged("IsLocked");
                PeekDuringAutoHide();
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
                    desiredLeft = Screen.Bounds.Right / DpiScale - DesiredWidth;
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
                    desiredTop = Screen.Bounds.Bottom / DpiScale - DesiredHeight;
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

        private void Taskbar_Deactivated(object sender, EventArgs e)
        {
            if (AppBarMode != AppBarMode.AutoHide)
            {
                // Prevent focus indicators and tooltips while not the active window
                // When auto-hide is enabled, this is performed by auto-hide events instead
                ResetControlFocus();
            }
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

        private void ResetControlFocus()
        {
            FocusDummyButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
        }

        protected override bool ShouldAllowAutoHide()
        {
            return (!_startMenuOpen || !Screen.Primary) && base.ShouldAllowAutoHide();
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
                windowManager.NotifyDisplayChange(reason);
            }
            else
            {
                // Update window as necessary
                base.SetScreenProperties(reason);
            }
        }

        protected override void OnAutoHideAnimationBegin(bool isHiding)
        {
            base.OnAutoHideAnimationBegin(isHiding);

            // Prevent focus indicators and tooltips while hidden
            ResetControlFocus();
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

        public void SetStartMenuOpen(bool isOpen)
        {
            bool currentAutoHide = AllowAutoHide;
            _startMenuOpen = isOpen;

            if (AllowAutoHide != currentAutoHide)
            {
                OnPropertyChanged("AllowAutoHide");
            }
        }

        private void Clock_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _clockRightClicked = true;
        }

        private void NotifyArea_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _notifyAreaRightClicked = true;
        }

        private void Taskbar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsLocked)
            {
                // Start low-level mouse hook to receive current drag position
                // The hook should be stopped upon mouse up
                StartMouseDragHook();
            }
        }

        private AppBarEdge DragCoordsToScreenEdge(int x, int y)
        {
            // The areas of the screen which determine the dragged-to edge are divided in an X.
            // To determine the edge, split the screen into quadrants, and then split the quadrants diagonally, alternating.
            double relativeX = ((double)x - Screen.Bounds.Left) / Screen.Bounds.Width;
            double relativeY = ((double)y - Screen.Bounds.Top) / Screen.Bounds.Height;

            // We will use the relative coordinates to form quadrants
            // Determine the edge based on the quadrant

            if (relativeX < 0.5 && relativeY < 0.5)
            {
                // top-left quadrant
                if (relativeX >= relativeY)
                {
                    return AppBarEdge.Top;
                }
                else
                {
                    return AppBarEdge.Left;
                }
            }
            else if (relativeX >= 0.5 && relativeY < 0.5)
            {
                // top-right quadrant
                // adjust relativeX to the same base as relativeY
                relativeX -= 0.5;

                if (relativeX + relativeY < 0.5)
                {
                    return AppBarEdge.Top;
                }
                else
                {
                    return AppBarEdge.Right;
                }
            }
            else if (relativeX < 0.5 && relativeY >= 0.5)
            {
                // bottom-left quadrant
                // adjust relativeY to the same base as relativeX
                relativeY -= 0.5;

                if (relativeX + relativeY < 0.5)
                {
                    return AppBarEdge.Left;
                }
                else
                {
                    return AppBarEdge.Bottom;
                }
            }
            else
            {
                // bottom-right quadrant
                if (relativeX >= relativeY)
                {
                    return AppBarEdge.Right;
                }
                else
                {
                    return AppBarEdge.Bottom;
                }
            }
        }

        private void MouseDragHook_LowLevelMouseEvent(object sender, LowLevelMouseHook.LowLevelMouseEventArgs e)
        {
            switch (e.Message)
            {
                case NativeMethods.WM.MOUSEMOVE:
                    if (_mouseDragStart == null)
                    {
                        return;
                    }

                    if (Math.Abs(e.HookStruct.pt.X - (double)(_mouseDragStart?.X)) <= SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(e.HookStruct.pt.Y - (double)(_mouseDragStart?.Y)) <= SystemParameters.MinimumVerticalDragDistance)
                    {
                        return;
                    }

                    AppBarEdge newEdge = DragCoordsToScreenEdge(e.HookStruct.pt.X, e.HookStruct.pt.Y);
                    if (newEdge != AppBarEdge)
                    {
                        Settings.Instance.Edge = newEdge;
                    }
                    break;
                case NativeMethods.WM.LBUTTONUP:
                case NativeMethods.WM.LBUTTONDOWN:
                case NativeMethods.WM.MBUTTONUP:
                case NativeMethods.WM.MBUTTONDOWN:
                case NativeMethods.WM.RBUTTONUP:
                case NativeMethods.WM.RBUTTONDOWN:
                case NativeMethods.WM.XBUTTONUP:
                case NativeMethods.WM.XBUTTONDOWN:
                    StopMouseDragHook();
                    break;
            }
        }

        private void StartMouseDragHook()
        {
            if (_mouseDragHook != null)
            {
                return;
            }

            _mouseDragHook = new LowLevelMouseHook();
            _mouseDragHook.LowLevelMouseEvent += MouseDragHook_LowLevelMouseEvent;
            _mouseDragHook.Initialize();
            _mouseDragStart = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);

            ShellLogger.Debug($"Mouse drag hook started");
        }

        private void StopMouseDragHook()
        {
            _mouseDragHook.LowLevelMouseEvent -= MouseDragHook_LowLevelMouseEvent;
            _mouseDragHook.Dispose();
            _mouseDragHook = null;
            _mouseDragStart = null;

            ShellLogger.Debug("Mouse drag hook removed");
        }
    }
}
