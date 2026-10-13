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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using Application = System.Windows.Application;


namespace RetroBar
{
    /// <summary>
    /// Interaction logic for Taskbar.xaml
    /// </summary>
    public partial class Taskbar : AppBarWindow
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        private DispatcherTimer _maximizedWindowTimer;
        private IntPtr _taskbarWindowHandle;

        public static readonly DependencyProperty IsFullscreenWindowMaximizedProperty =
            DependencyProperty.Register("IsFullscreenWindowMaximized", typeof(bool), typeof(Taskbar),
                new PropertyMetadata(false));

        /// <summary>
        /// True when a window on this taskbar's screen is currently maximized (and isn't RetroBar itself).
        /// Themes can bind/trigger on this (e.g. via RelativeSource FindAncestor AncestorType=Window)
        /// to change their own appearance, the same way they already do for Orientation.
        /// </summary>
        public bool IsFullscreenWindowMaximized
        {
            get => (bool)GetValue(IsFullscreenWindowMaximizedProperty);
            private set => SetValue(IsFullscreenWindowMaximizedProperty, value);
        }

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
        private LowLevelMouseHook _mouseDragHook;
        private Point? _mouseDragStart = null;
        private bool _mouseDragResize = false;
        private readonly DictionaryManager _dictionaryManager;
        private readonly ShellManager _shellManager;
        private readonly StartMenuMonitor _startMenuMonitor;
        private readonly Updater _updater;
        private bool _fullScreenSuppressed;
        private int _openMenus;
        
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

            _maximizedWindowTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _maximizedWindowTimer.Tick += MaximizedWindowTimer_Tick;

            // Only Windows Vista Aero models the glass-turns-opaque-on-maximize behavior,
            // so don't bother polling the foreground window on any other theme.
            if (IsVistaAeroThemeActive())
            {
                _maximizedWindowTimer.Start();
            }

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

                if (IsVistaAeroThemeActive())
                {
                    _maximizedWindowTimer.Start();
                }
                else
                {
                    _maximizedWindowTimer.Stop();
                    IsFullscreenWindowMaximized = false;
                }
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
                UpdatePosition();
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

        private const string VistaAeroThemeName = "Windows Vista Aero";

        private bool IsVistaAeroThemeActive()
        {
            return Settings.Instance.Theme == VistaAeroThemeName;
        }

        private bool IsForegroundWindowMaximizedOnThisScreen()
        {
            // Borderless/exclusive fullscreen apps (games, video players, F11 browsers)
            // don't set WS_MAXIMIZE, so IsZoomed alone would miss them entirely.
            // Reuse the same FullScreenHelper-backed check the auto-hide logic already
            // uses, so a real fullscreen app on this screen counts the same as maximized.
            if (HasFullScreenApp())
            {
                return true;
            }

            IntPtr foregroundWindow = GetForegroundWindow();

            if (foregroundWindow == IntPtr.Zero)
            {
                return false;
            }

            // Ignore RetroBar itself.
            if (_taskbarWindowHandle != IntPtr.Zero && foregroundWindow == _taskbarWindowHandle)
            {
                return false;
            }

            try
            {
                System.Windows.Forms.Screen foregroundScreen = System.Windows.Forms.Screen.FromHandle(foregroundWindow);

                if (foregroundScreen == null || foregroundScreen.DeviceName != Screen.DeviceName)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return IsZoomed(foregroundWindow);
        }

        private void MaximizedWindowTimer_Tick(object sender, EventArgs e)
        {
            IsFullscreenWindowMaximized = IsForegroundWindowMaximizedOnThisScreen();
        }

        #region AppBarWindow overrides
        protected override void OnSourceInitialized(object sender, EventArgs e)
        {
            base.OnSourceInitialized(sender, e);

            _taskbarWindowHandle = new WindowInteropHelper(this).Handle;

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
            if (_maximizedWindowTimer != null)
            {
                _maximizedWindowTimer.Stop();
                _maximizedWindowTimer.Tick -= MaximizedWindowTimer_Tick;
                _maximizedWindowTimer = null;
            }

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
            return (!_startMenuOpen || !Screen.Primary) && _openMenus < 1 && base.ShouldAllowAutoHide();
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

        protected override void OnFullScreenEnter(FullScreenApp app)
        {
            base.OnFullScreenEnter(app);
            StartButton?.UpdateFloatingStartTopmost(false);
        }

        protected override void OnFullScreenLeave()
        {
            base.OnFullScreenLeave();
            StartButton?.UpdateFloatingStartTopmost(true);
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

            if (NativeMethods.GetAsyncKeyState((int)System.Windows.Forms.Keys.ShiftKey) < 0 && Settings.Instance.ShowExitMenuItem)
            {
                RestartMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                RestartMenuItem.Visibility = Visibility.Collapsed;
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
            ((App)Application.Current).ExitGracefully();
        }

        private void RestartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).RestartApp();
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

        public void AddOpenMenu()
        {
            bool currentAutoHide = AllowAutoHide;
            _openMenus++;

            if (AllowAutoHide != currentAutoHide)
            {
                OnPropertyChanged(nameof(AllowAutoHide));
            }
        }

        public void RemoveOpenMenu()
        {
            bool currentAutoHide = AllowAutoHide;
            _openMenus--;

            if (AllowAutoHide != currentAutoHide)
            {
                OnPropertyChanged(nameof(AllowAutoHide));
            }
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

        #region Unlocked taskbar drag hook
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

                    if (_mouseDragResize)
                    {
                        Dispatcher.BeginInvoke(() => {
                            int mouseX = e.HookStruct.pt.X;
                            int mouseY = e.HookStruct.pt.Y;
                            // Calculate where the resize edge should be, in case the actual resize operation is lagging behind the mouse
                            double scaledRowHeight = DesiredRowHeight * DpiScale;
                            if (Orientation == Orientation.Horizontal)
                            {
                                double taskbarEdge = AppBarEdge == AppBarEdge.Top ? Screen.Bounds.Top + (DesiredHeight * DpiScale) : Screen.Bounds.Bottom - (DesiredHeight * DpiScale);
                                if ((AppBarEdge == AppBarEdge.Top && mouseY < taskbarEdge - SystemParameters.MinimumVerticalDragDistance ||
                                     AppBarEdge == AppBarEdge.Bottom && mouseY > taskbarEdge + SystemParameters.MinimumVerticalDragDistance) &&
                                     Settings.Instance.RowCount > 1)
                                {
                                    // If mouse is inside the taskbar and more than the minimum drag distance away, decrement size
                                    Settings.Instance.RowCount -= 1;
                                }
                                else if ((AppBarEdge == AppBarEdge.Top && mouseY >= taskbarEdge + scaledRowHeight ||
                                          AppBarEdge == AppBarEdge.Bottom && mouseY <= taskbarEdge - scaledRowHeight) &&
                                          Settings.Instance.RowCount < Settings.Instance.RowLimit)
                                {
                                    // If mouse is outside the taskbar and at least one row height away, increment size
                                    Settings.Instance.RowCount += 1;
                                }
                            }
                            else
                            {
                                double taskbarEdge = AppBarEdge == AppBarEdge.Left ? Screen.Bounds.Left + (DesiredWidth * DpiScale) : Screen.Bounds.Right - (DesiredWidth * DpiScale);
                                if ((AppBarEdge == AppBarEdge.Left && mouseX > taskbarEdge + scaledRowHeight ||
                                     AppBarEdge == AppBarEdge.Right && mouseX < taskbarEdge - scaledRowHeight) &&
                                    Settings.Instance.TaskbarWidth < Settings.Instance.TaskbarWidthLimit)
                                {
                                    Settings.Instance.TaskbarWidth += 1;
                                }
                                else if ((AppBarEdge == AppBarEdge.Left && mouseX < taskbarEdge - SystemParameters.MinimumHorizontalDragDistance ||
                                          AppBarEdge == AppBarEdge.Right && mouseX > taskbarEdge + SystemParameters.MinimumHorizontalDragDistance) &&
                                    Settings.Instance.TaskbarWidth > 1)
                                {
                                    Settings.Instance.TaskbarWidth -= 1;
                                }
                            }
                        });
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
            _mouseDragResize = IsMouseInResizeArea();

            ShellLogger.Debug($"Mouse drag hook started");
        }

        private void StopMouseDragHook()
        {
            _mouseDragHook.LowLevelMouseEvent -= MouseDragHook_LowLevelMouseEvent;
            _mouseDragHook.Dispose();
            _mouseDragHook = null;
            _mouseDragStart = null;
            _mouseDragResize = false;

            ShellLogger.Debug("Mouse drag hook removed");
        }

        private bool IsMouseInResizeArea()
        {
            if (IsLocked) return false;

            int resizeRegionSize = (int)((_unlockedMargin > 0 ? _unlockedMargin : SystemParameters.MinimumVerticalDragDistance * Settings.Instance.TaskbarScale) * DpiScale);
            int mouseX = System.Windows.Forms.Cursor.Position.X;
            int mouseY = System.Windows.Forms.Cursor.Position.Y;

            if (AppBarEdge == AppBarEdge.Bottom && mouseY <= (int)(Top * DpiScale) + resizeRegionSize)
            {
                return true;
            }
            else if (AppBarEdge == AppBarEdge.Top && mouseY >= (int)((Top + Height) * DpiScale) - resizeRegionSize)
            {
                return true;
            }
            else if (AppBarEdge == AppBarEdge.Left && mouseX >= (int)((Left + Width) * DpiScale) - resizeRegionSize)
            {
                return true;
            }
            else if (AppBarEdge == AppBarEdge.Right && mouseX <= (int)(Left * DpiScale) + resizeRegionSize)
            {
                return true;
            }

            return false;
        }

        private void Taskbar_MouseMove(object sender, MouseEventArgs e)
        {
            // Show resize cursor for resizable taskbars
            if (IsMouseInResizeArea() || _mouseDragResize)
            {
                Cursor = Orientation == Orientation.Horizontal ? Cursors.SizeNS : Cursors.SizeWE;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }
        #endregion
    }
}
