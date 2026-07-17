using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for Taskbar.xaml
    /// </summary>
    public partial class Taskbar : AppBarWindow
    {
        public bool IsLocked => Settings.Instance.LockTaskbar;

        public bool IsScaled => DpiScale > 1 || Settings.Instance.TaskbarScale > 1;

        private double _unlockedMargin;
        public double DesiredRowHeight { get; private set; }

        public int Rows
        {
            get => Settings.Instance.RowCount;
            set => Settings.Instance.RowCount = value;
        }

        private bool _startMenuOpen;
        private Point? _mouseDragStart = null;
        private bool _isDragging;
        private bool _mouseDragResize = false;
        private readonly DictionaryManager _dictionaryManager;
        private readonly ShellManager _shellManager;
        private readonly StartMenuMonitor _startMenuMonitor;
        private readonly Updater _updater;
        private bool _fullScreenSuppressed;
        private double _resizeStartPos;

        public WindowManager windowManager;
        public HotkeyManager hotkeyManager;

        public Taskbar(WindowManager windowManager, DictionaryManager dictionaryManager, ShellManager shellManager, StartMenuMonitor startMenuMonitor, Updater updater, HotkeyManager hotkeyManager, AppBarScreen screen, AppBarEdge edge, AppBarMode mode)
            : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen, edge, mode, 0)
        {
            _dictionaryManager = dictionaryManager;
            _shellManager = shellManager;
            _startMenuMonitor = startMenuMonitor;
            _updater = updater;
            this.windowManager = windowManager;
            this.hotkeyManager = hotkeyManager;

            InitializeComponent();
            DataContext = _shellManager;
            StartButton.StartMenuMonitor = startMenuMonitor;

            RecalculateSize(false);

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

            UpdateStartButton();

            AutoHideElement = TaskbarContentControl;

            PropertyChanged += Taskbar_PropertyChanged;

            _startMenuMonitor.StartMenuVisibilityChanged += StartMenuMonitor_StartMenuVisibilityChanged;
            _shellManager.TasksService.WindowActivated += TasksService_WindowActivated;
        }

        private void TasksService_WindowActivated(object sender, ManagedShell.WindowsTasks.WindowEventArgs e)
        {
            // If full-screen is suppressed, and a full-screen window is activated, it's time to un-suppress.

            if (!_fullScreenSuppressed)
            {
                return;
            }

            _fullScreenSuppressed = false;

            if (!HasFullScreenApp())
            {
                return;
            }

            for (int i = 0; i < _fullScreenHelper.FullScreenApps.Count; i++)
            {
                if (_fullScreenHelper.FullScreenApps[i].hWnd == e.Window.Handle)
                {
                    base.OnFullScreenEnter(_fullScreenHelper.FullScreenApps[i]);
                    return;
                }
            }
        }

        private void StartMenuMonitor_StartMenuVisibilityChanged(object sender, StartMenuMonitor.StartMenuMonitorEventArgs e)
        {
            if (!HasFullScreenApp() || !e.Visible)
            {
                return;
            }

            _fullScreenSuppressed = true;
            base.OnFullScreenLeave();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                bool newTransparency = AppBarMode == AppBarMode.AutoHide || (Application.Current.FindResource("AllowsTransparency") as bool? ?? false);

                if (AllowsTransparency != newTransparency && Screen.Primary)
                {
                    // Transparency cannot be changed on an open window.
                    windowManager.ReopenTaskbars();
                    return;
                }

                SetBlur(AllowsBlur());
                PeekDuringAutoHide();
                RecalculateSize();
            }
            else if (e.PropertyName == nameof(Settings.ShowQuickLaunch))
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
            else if (e.PropertyName == nameof(Settings.Edge))
            {
                PeekDuringAutoHide();
                AppBarEdge = Settings.Instance.Edge;
                SetWindowPosition(GetDesiredRect());
            }
            else if (e.PropertyName == nameof(Settings.Language))
            {
                FlowDirection newFlowDirection = Application.Current.FindResource("flow_direction") as FlowDirection? ?? FlowDirection.LeftToRight;

                if (FlowDirection != newFlowDirection && Screen.Primary)
                {
                    // It is necessary to reopen the taskbars to refresh menu sizes.
                    windowManager.ReopenTaskbars();
                    return;
                }
            }
            else if (e.PropertyName == nameof(Settings.ShowDesktopButton))
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
            else if (e.PropertyName == nameof(Settings.TaskbarScale))
            {
                PeekDuringAutoHide();
                RecalculateSize();
                OnPropertyChanged(nameof(IsScaled));
            }
            else if (e.PropertyName == nameof(Settings.AutoHide))
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
            else if (e.PropertyName == nameof(Settings.LockTaskbar))
            {
                OnPropertyChanged(nameof(IsLocked));
                PeekDuringAutoHide();
                RecalculateSize();
            }
            else if (e.PropertyName == nameof(Settings.RowCount))
            {
                PeekDuringAutoHide();
                RecalculateSize();
                OnPropertyChanged(nameof(Rows));
            }
            else if (e.PropertyName == nameof(Settings.TaskbarWidth))
            {
                PeekDuringAutoHide();
                RecalculateSize();
            }
            else if (e.PropertyName == nameof(Settings.ShowStartButtonMultiMon))
            {
                UpdateStartButton();
            }
            else if (e.PropertyName == nameof(Settings.AutoHideTransparent))
            {
                PeekDuringAutoHide();
            }
            else if (e.PropertyName == nameof(Settings.AllowBlurBehind))
            {
                SetBlur(AllowsBlur());
            }
        }

        #region AppBarWindow overrides
        protected override void OnSourceInitialized(object sender, EventArgs e)
        {
            base.OnSourceInitialized(sender, e);

            SetLayoutRounding();
            SetBlur(AllowsBlur());
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
                _dictionaryManager.SetThemeFromSettings();
            }
            else if (msg == (int)NativeMethods.WM.SETTINGCHANGE && wParam == (IntPtr)NativeMethods.SPI.SETWORKAREA && Settings.Instance.ShowMultiMon)
            {
                windowManager.NotifyWorkAreaChange();
            }

            return IntPtr.Zero;
        }

        protected override void CustomClosing()
        {
            if (AllowClose)
            {
                QuickLaunchToolbar.Visibility = Visibility.Collapsed;

                Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
                _startMenuMonitor.StartMenuVisibilityChanged -= StartMenuMonitor_StartMenuVisibilityChanged;
                _shellManager.TasksService.WindowActivated -= TasksService_WindowActivated;
            }
        }

        protected override void SetScreenProperties(ScreenSetupReason reason)
        {
            if (reason == ScreenSetupReason.DpiChange)
            {
                // DPI change is per-monitor, update ourselves
                UpdatePosition();
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

        protected override bool ShouldAllowAutoHide()
        {
            return (!_startMenuOpen || !Screen.Primary) && base.ShouldAllowAutoHide();
        }

        protected override void OnAutoHideAnimationBegin(bool isHiding)
        {
            base.OnAutoHideAnimationBegin(isHiding);

            // Prevent focus indicators and tooltips while hidden
            ResetControlFocus();

            if (!isHiding && Opacity < 1)
            {
                Opacity = 1;
                OnPropertyChanged(nameof(Opacity));
            }
        }

        protected override void OnAutoHideAnimationComplete(bool isHiding)
        {
            base.OnAutoHideAnimationComplete(isHiding);

            if (isHiding && Settings.Instance.AutoHideTransparent && AllowsTransparency && AllowAutoHide)
            {
                Opacity = 0.01;
                OnPropertyChanged(nameof(Opacity));
            }
        }
        #endregion

        #region Taskbar events
        private void Taskbar_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DpiScale))
            {
                OnPropertyChanged(nameof(IsScaled));
            }
        }

        private void Taskbar_OnLocationChanged(object sender, EventArgs e)
        {
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
        #endregion

        #region Context menu
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (_updater.IsUpdateAvailable)
            {
                UpdateAvailableMenuItem.Visibility = Visibility.Visible;
            }
        }

        private void SetTimeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShellHelper.StartProcess("timedate.cpl");
        }

        private void CustomizeNotificationsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            PropertiesWindow propWindow = PropertiesWindow.Open(_shellManager.NotificationArea, _dictionaryManager, Screen, DpiScale, Orientation == Orientation.Horizontal ? DesiredHeight : DesiredWidth);
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
            PropertiesWindow.Open(_shellManager.NotificationArea, _dictionaryManager, Screen, DpiScale, Orientation == Orientation.Horizontal ? DesiredHeight : DesiredWidth);
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
        #endregion

        private void RecalculateSize(bool performResize = true)
        {
            _unlockedMargin = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarUnlockedSize") as double? ?? 0);
            DesiredRowHeight = Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarRowHeight") as double? ?? 0);
            double newWidth = (Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarWidth") as double? ?? 0)) + DesiredRowHeight * (Settings.Instance.TaskbarWidth - 1);
            double newHeight = (Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarHeight") as double? ?? 0)) + DesiredRowHeight * (Rows - 1);

            if (AppBarMode == AppBarMode.AutoHide || !Settings.Instance.LockTaskbar)
            {
                newHeight += _unlockedMargin;
                newWidth += _unlockedMargin;
            }

            bool heightChanged = newHeight != DesiredHeight;
            bool widthChanged = newWidth != DesiredWidth;

            DesiredHeight = newHeight;
            DesiredWidth = newWidth;

            if (!performResize)
            {
                return;
            }

            if ((Orientation == Orientation.Horizontal && heightChanged) || (Orientation == Orientation.Vertical && widthChanged))
            {
                UpdatePosition();
            }
        }

        private void ResetControlFocus()
        {
            FocusDummyButton.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
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

        public void SetStartMenuOpen(bool isOpen)
        {
            bool currentAutoHide = AllowAutoHide;
            _startMenuOpen = isOpen;

            if (AllowAutoHide != currentAutoHide)
            {
                OnPropertyChanged(nameof(AllowAutoHide));
            }
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

        private void UpdateTrayPosition()
        {
            if (Screen.Primary)
            {
                SetTrayHost();
            }
        }

        private void UpdateStartButton()
        {
            if (!Screen.Primary && !Settings.Instance.ShowStartButtonMultiMon)
            {
                StartButton.Visibility = Visibility.Collapsed;
                return;
            }

            StartButton.Visibility = Visibility.Visible;
        }

        private bool HasFullScreenApp()
        {
            bool hasFullScreenApp = false;

            foreach (var app in _fullScreenHelper.FullScreenApps)
            {
                if (app.screen.DeviceName == Screen.DeviceName || app.screen.IsVirtualScreen)
                {
                    hasFullScreenApp = true;
                    break;
                }
            }

            return hasFullScreenApp;
        }

        private bool AllowsBlur()
        {
            return Settings.Instance.AllowBlurBehind &&
                   (Application.Current.FindResource("AllowsTransparency") as bool? ?? false);
        }

        #region Unlocked taskbar drag
        private void Taskbar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsLocked) return;

            var screenPos = PointToScreen(e.GetPosition(this));

            // if mouse is in resize‐zone, begin resize drag
            if (IsMouseInResizeArea())
            {
                _mouseDragResize = true;
                // remember starting coordinate along drag axis
                _resizeStartPos = AppBarEdge == AppBarEdge.Top || AppBarEdge == AppBarEdge.Bottom
                    ? screenPos.Y
                    : screenPos.X;
                Mouse.Capture(this);
                return;
            }

            // otherwise begin reposition drag
            _mouseDragStart = e.GetPosition(this);
            _isDragging = true;
            Mouse.Capture(this);
        }

        private void Taskbar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseDragResize)
            {
                _mouseDragResize = false;
                Mouse.Capture(null);
                return;
            }

            if (!_isDragging) return;

            _isDragging = false;
            _mouseDragStart = null;
            Mouse.Capture(null);
        }

        private void Taskbar_OnMouseMove(object sender, MouseEventArgs e)
        {
            // Show resize cursor for resizable taskbars
            if (_mouseDragResize || (!_isDragging && IsMouseInResizeArea()))
            {
                Cursor = Orientation == Orientation.Horizontal ? Cursors.SizeNS : Cursors.SizeWE;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }

            if (_mouseDragResize)
            {
                // Use WPF's Mouse position instead of WinForms
                Point cursorPosition = PointToScreen(Mouse.GetPosition(this));
                int mouseX = (int)cursorPosition.X;
                int mouseY = (int)cursorPosition.Y;

                // Process resize operation directly instead of using BeginInvoke
                // This avoids dispatcher overhead and potential lag when Explorer is not running
                double scaledRowHeight = DesiredRowHeight * DpiScale;

                if (Orientation == Orientation.Horizontal)
                {
                    // Use Screen reference instead of PrimaryScreen to handle multi-monitor setups correctly
                    double taskbarEdge = (AppBarEdge == AppBarEdge.Top
                        ? Screen.Bounds.Top + (DesiredHeight * DpiScale)
                        : Screen.Bounds.Bottom - (DesiredHeight * DpiScale)
                    );

                    if ((AppBarEdge == AppBarEdge.Top && mouseY < taskbarEdge - SystemParameters.MinimumVerticalDragDistance
                         || AppBarEdge == AppBarEdge.Bottom && mouseY > taskbarEdge + SystemParameters.MinimumVerticalDragDistance)
                         && Settings.Instance.RowCount > 1)
                    {
                        Settings.Instance.RowCount--;
                    }
                    else if ((AppBarEdge == AppBarEdge.Top && mouseY >= taskbarEdge + scaledRowHeight
                              || AppBarEdge == AppBarEdge.Bottom && mouseY <= taskbarEdge - scaledRowHeight)
                              && Settings.Instance.RowCount < Settings.Instance.RowLimit)
                    {
                        Settings.Instance.RowCount++;
                    }
                }
                else
                {
                    double taskbarEdge = (AppBarEdge == AppBarEdge.Left
                        ? Screen.Bounds.Left + (DesiredWidth * DpiScale)
                        : Screen.Bounds.Right - (DesiredWidth * DpiScale)
                    );

                    if ((AppBarEdge == AppBarEdge.Left && mouseX > taskbarEdge + scaledRowHeight
                         || AppBarEdge == AppBarEdge.Right && mouseX < taskbarEdge - scaledRowHeight)
                         && Settings.Instance.TaskbarWidth < Settings.Instance.TaskbarWidthLimit)
                    {
                        Settings.Instance.TaskbarWidth++;
                    }
                    else if ((AppBarEdge == AppBarEdge.Left && mouseX < taskbarEdge - SystemParameters.MinimumHorizontalDragDistance
                              || AppBarEdge == AppBarEdge.Right && mouseX > taskbarEdge + SystemParameters.MinimumHorizontalDragDistance)
                              && Settings.Instance.TaskbarWidth > 1)
                    {
                        Settings.Instance.TaskbarWidth--;
                    }
                }
                return;
            }

            // reposition‐while‐dragging
            if (!_isDragging || _mouseDragStart == null)
                return;

            // only start moving edge after system drag threshold
            var pt = e.GetPosition(this);
            if (Math.Abs(pt.X - _mouseDragStart.Value.X) <= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(pt.Y - _mouseDragStart.Value.Y) <= SystemParameters.MinimumVerticalDragDistance)
                return;

            var screenPos = PointToScreen(pt);
            var newEdge = DragCoordsToScreenEdge((int)screenPos.X, (int)screenPos.Y);

            if (newEdge != AppBarEdge)
                Settings.Instance.Edge = newEdge;
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

        private bool IsMouseInResizeArea()
        {
            if (IsLocked) return false;

            // Calculate resize region size once
            int resizeRegionSize = (int)((_unlockedMargin > 0 ? _unlockedMargin : SystemParameters.MinimumVerticalDragDistance * Settings.Instance.TaskbarScale) * DpiScale);

            // Get cursor position using WPF's Mouse class instead of System.Windows.Forms.Cursor
            Point cursorPos = PointToScreen(Mouse.GetPosition(this));
            int mouseX = (int)cursorPos.X;
            int mouseY = (int)cursorPos.Y;

            // Create boundary rectangles based on edge position
            int scaledTop = (int)(Top * DpiScale);
            int scaledLeft = (int)(Left * DpiScale);
            int scaledBottom = (int)((Top + Height) * DpiScale);
            int scaledRight = (int)((Left + Width) * DpiScale);

            // Check if mouse is in resize area based on current edge
            switch (AppBarEdge)
            {
                case AppBarEdge.Bottom:
                    return mouseY <= scaledTop + resizeRegionSize;
                case AppBarEdge.Top:
                    return mouseY >= scaledBottom - resizeRegionSize;
                case AppBarEdge.Left:
                    return mouseX >= scaledRight - resizeRegionSize;
                case AppBarEdge.Right:
                    return mouseX <= scaledLeft + resizeRegionSize;
                default:
                    return false;
            }
        }
        #endregion
    }
}