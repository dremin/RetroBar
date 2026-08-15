using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        private bool _mouseDragResize;
        private AppBarEdge _dragStartEdge;
        private int _dragStartRowCount;
        private int _dragStartTaskbarWidth;
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
            SetLayoutRounding();
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
            PreviewKeyDown += Taskbar_PreviewKeyDown;

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
                    OnFullScreenEnter(_fullScreenHelper.FullScreenApps[i]);
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
            OnFullScreenLeave();
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
                UpdateLayout();
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
            else if (msg == (int)NativeMethods.WM.SYSCHAR && wParam.ToInt32() == ' ')
            {
                handled = true;
                ShowSystemMenu(hwnd);
            }
            else if (msg == (int)NativeMethods.WM.SYSCOMMAND)
            {
                int sc = wParam.ToInt32() & 0xFFF0;
                if ((sc == NativeMethods.SC_MOVE || sc == NativeMethods.SC_SIZE) && IsLocked)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
                else if (sc == NativeMethods.SC_CLOSE)
                {
                    handled = true;
                    IntPtr progmanHwnd = NativeMethods.FindWindow("Progman", "Program Manager");
                    if (progmanHwnd != IntPtr.Zero)
                    {
                        NativeMethods.PostMessage(progmanHwnd, (uint)NativeMethods.WM.CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                    return IntPtr.Zero;
                }
            }
            else if (msg == (int)NativeMethods.WM.ENTERSIZEMOVE)
            {
                BeginDragOrResize();
            }
            else if (msg == (int)NativeMethods.WM.EXITSIZEMOVE)
            {
                if (NativeMethods.GetAsyncKeyState((int)System.Windows.Forms.Keys.Escape) < 0)
                {
                    CancelDragOrResize();
                }
                else
                {
                    EndDragOrResize();
                }
            }
            else if (msg == (int)NativeMethods.WM.MOVING)
            {
                handled = true;
                if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
                {
                    var newEdge = DragCoordsToScreenEdge(pt.x, pt.y);
                    if (newEdge != AppBarEdge)
                    {
                        Settings.Instance.Edge = newEdge;
                    }
                }

                var desiredRect = GetDesiredRect();
                Marshal.StructureToPtr(desiredRect, lParam, false);
                return (IntPtr)1;
            }
            else if (msg == (int)NativeMethods.WM.SIZING)
            {
                handled = true;
                if (!IsLocked)
                {
                    if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
                    {
                        int currentCoord = (Orientation == Orientation.Horizontal) ? pt.y : pt.x;
                        ProcessResize(currentCoord);
                    }
                }

                var desiredRect = GetDesiredRect();
                Marshal.StructureToPtr(desiredRect, lParam, false);
                return (IntPtr)1;
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
                if (heightChanged || widthChanged)
                {
                    UpdatePosition();
                }
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

        #region Unlocked taskbar drag
        private void BeginDragOrResize()
        {
            _dragStartEdge = AppBarEdge;
            _dragStartRowCount = Settings.Instance.RowCount;
            _dragStartTaskbarWidth = Settings.Instance.TaskbarWidth;
            windowManager?.NotifyDragBegin();
        }

        private void EndDragOrResize()
        {
            _isDragging = false;
            _mouseDragResize = false;
            _mouseDragStart = null;
            Cursor = Cursors.Arrow;
            ReleaseMouseCapture();

            windowManager?.NotifyDragEnd();
        }

        private void CancelDragOrResize()
        {
            _isDragging = false;
            _mouseDragResize = false;
            _mouseDragStart = null;
            Cursor = Cursors.Arrow;
            ReleaseMouseCapture();

            if (Settings.Instance.Edge != _dragStartEdge)
            {
                Settings.Instance.Edge = _dragStartEdge;
            }
            if (Settings.Instance.RowCount != _dragStartRowCount)
            {
                Settings.Instance.RowCount = _dragStartRowCount;
            }
            if (Settings.Instance.TaskbarWidth != _dragStartTaskbarWidth)
            {
                Settings.Instance.TaskbarWidth = _dragStartTaskbarWidth;
            }

            windowManager?.NotifyDragEnd();
        }

        private void Taskbar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && (_isDragging || _mouseDragResize))
            {
                e.Handled = true;
                CancelDragOrResize();
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            if (_isDragging || _mouseDragResize)
            {
                CancelDragOrResize();
            }
        }

        private void Taskbar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsLocked) return;

            BeginDragOrResize();

            // if mouse is in resize‐zone, begin resize drag
            if (IsMouseInResizeArea())
            {
                _mouseDragResize = true;
                CaptureMouse();
                return;
            }

            // otherwise begin reposition drag
            _mouseDragStart = PointToScreen(e.GetPosition(this));
            _isDragging = true;
            CaptureMouse();
        }

        private void Taskbar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseDragResize || _isDragging)
            {
                EndDragOrResize();
            }
        }

        private void ProcessResize(int coordinate)
        {
            double scaledRowHeight = DesiredRowHeight * DpiScale;
            if (scaledRowHeight <= 0) return;

            if (Orientation == Orientation.Horizontal)
            {
                double distance = AppBarEdge == AppBarEdge.Bottom
                    ? Screen.Bounds.Bottom - coordinate
                    : coordinate - Screen.Bounds.Top;

                double baseHeight = (Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarHeight") as double? ?? 0)) * DpiScale;
                if (AppBarMode == AppBarMode.AutoHide || !Settings.Instance.LockTaskbar)
                {
                    baseHeight += _unlockedMargin * DpiScale;
                }

                int targetRows = 1 + (int)Math.Max(0, Math.Round((distance - baseHeight) / scaledRowHeight));
                targetRows = Math.Clamp(targetRows, 1, Settings.Instance.RowLimit);
                if (targetRows != Settings.Instance.RowCount)
                {
                    Settings.Instance.RowCount = targetRows;
                }
            }
            else
            {
                double distance = AppBarEdge == AppBarEdge.Right
                    ? Screen.Bounds.Right - coordinate
                    : coordinate - Screen.Bounds.Left;

                double baseWidth = (Settings.Instance.TaskbarScale * (Application.Current.FindResource("TaskbarWidth") as double? ?? 0)) * DpiScale;
                if (AppBarMode == AppBarMode.AutoHide || !Settings.Instance.LockTaskbar)
                {
                    baseWidth += _unlockedMargin * DpiScale;
                }

                int targetWidth = 1 + (int)Math.Max(0, Math.Round((distance - baseWidth) / scaledRowHeight));
                targetWidth = Math.Clamp(targetWidth, 1, Settings.Instance.TaskbarWidthLimit);
                if (targetWidth != Settings.Instance.TaskbarWidth)
                {
                    Settings.Instance.TaskbarWidth = targetWidth;
                }
            }
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
                if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
                {
                    int currentCoord = Orientation == Orientation.Horizontal ? pt.y : pt.x;
                    ProcessResize(currentCoord);
                }
                return;
            }

            // reposition‐while‐dragging
            if (!_isDragging)
                return;

            if (NativeMethods.GetCursorPos(out NativeMethods.POINT cursorPt))
            {
                var newEdge = DragCoordsToScreenEdge(cursorPt.x, cursorPt.y);
                if (newEdge != AppBarEdge)
                {
                    Settings.Instance.Edge = newEdge;
                }
            }
        }

        private AppBarEdge DragCoordsToScreenEdge(int x, int y)
        {
            double relX = (double)x - Screen.Bounds.Left;
            double relY = (double)y - Screen.Bounds.Top;
            double width = Screen.Bounds.Width;
            double height = Screen.Bounds.Height;

            AppBarEdge vertEdge = relX < width / 2 ? AppBarEdge.Left : AppBarEdge.Right;
            double errorX = relX < width / 2 ? relX : width - relX;

            AppBarEdge horzEdge = relY < height / 2 ? AppBarEdge.Top : AppBarEdge.Bottom;
            double errorY = relY < height / 2 ? relY : height - relY;

            return (errorY * width > errorX * height) ? vertEdge : horzEdge;
        }

        private bool IsMouseInResizeArea()
        {
            if (IsLocked) return false;

            double resizeGrip = _unlockedMargin > 0 ? _unlockedMargin : (SystemParameters.MinimumVerticalDragDistance * Settings.Instance.TaskbarScale);
            Point localPos = Mouse.GetPosition(this);

            switch (AppBarEdge)
            {
                case AppBarEdge.Bottom:
                    return localPos.Y >= 0 && localPos.Y <= resizeGrip;
                case AppBarEdge.Top:
                    return localPos.Y >= ActualHeight - resizeGrip && localPos.Y <= ActualHeight;
                case AppBarEdge.Left:
                    return localPos.X >= ActualWidth - resizeGrip && localPos.X <= ActualWidth;
                case AppBarEdge.Right:
                    return localPos.X >= 0 && localPos.X <= resizeGrip;
                default:
                    return false;
            }
        }
        #endregion

        #region System Menu
        private void ShowSystemMenu(IntPtr hwnd)
        {
            int oldStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.WindowLongFlags.GWL_STYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.WindowLongFlags.GWL_STYLE, oldStyle | (int)NativeMethods.WindowStyles.WS_SYSMENU);

            IntPtr hMenu = NativeMethods.GetSystemMenu(hwnd, false);
            if (hMenu != IntPtr.Zero)
            {
                NativeMethods.EnableMenuItem(hMenu, (uint)NativeMethods.SC_RESTORE, NativeMethods.MF_BYCOMMAND | NativeMethods.MF_GRAYED);
                NativeMethods.EnableMenuItem(hMenu, (uint)NativeMethods.SC_MINIMIZE, NativeMethods.MF_BYCOMMAND | NativeMethods.MF_GRAYED);
                NativeMethods.EnableMenuItem(hMenu, (uint)NativeMethods.SC_MAXIMIZE, NativeMethods.MF_BYCOMMAND | NativeMethods.MF_GRAYED);
                NativeMethods.EnableMenuItem(hMenu, (uint)NativeMethods.SC_CLOSE, NativeMethods.MF_BYCOMMAND | NativeMethods.MF_ENABLED);

                uint sizeMoveFlag = IsLocked ? NativeMethods.MF_GRAYED : NativeMethods.MF_ENABLED;
                NativeMethods.EnableMenuItem(hMenu, (uint)NativeMethods.SC_MOVE, NativeMethods.MF_BYCOMMAND | sizeMoveFlag);
                NativeMethods.EnableMenuItem(hMenu, (uint)NativeMethods.SC_SIZE, NativeMethods.MF_BYCOMMAND | sizeMoveFlag);

                Point anchorPt;
                try
                {
                    anchorPt = (StartButton != null && StartButton.IsVisible)
                        ? StartButton.PointToScreen(new Point(0, 0))
                        : PointToScreen(new Point(0, 0));
                }
                catch
                {
                    anchorPt = new Point(Left * DpiScale, Top * DpiScale);
                }

                int x = (int)anchorPt.X;
                int y = (int)anchorPt.Y;
                NativeMethods.TPM alignFlags = NativeMethods.TPM.LEFTALIGN;

                switch (AppBarEdge)
                {
                    case AppBarEdge.Bottom:
                        alignFlags = NativeMethods.TPM.LEFTALIGN | NativeMethods.TPM.BOTTOMALIGN;
                        break;
                    case AppBarEdge.Top:
                        alignFlags = NativeMethods.TPM.LEFTALIGN | NativeMethods.TPM.TOPALIGN;
                        y += (int)((StartButton?.ActualHeight ?? ActualHeight) * DpiScale);
                        break;
                    case AppBarEdge.Left:
                        alignFlags = NativeMethods.TPM.LEFTALIGN | NativeMethods.TPM.TOPALIGN;
                        x += (int)((StartButton?.ActualWidth ?? ActualWidth) * DpiScale);
                        break;
                    case AppBarEdge.Right:
                        alignFlags = NativeMethods.TPM.RIGHTALIGN | NativeMethods.TPM.TOPALIGN;
                        break;
                }

                uint cmd = NativeMethods.TrackPopupMenuEx(hMenu, NativeMethods.TPM.RETURNCMD | NativeMethods.TPM.LEFTBUTTON | NativeMethods.TPM.RIGHTBUTTON | NativeMethods.TPM.VERTICAL | alignFlags, x, y, hwnd, IntPtr.Zero);
                if (cmd > 0)
                {
                    IntPtr lParam = (cmd == (int)NativeMethods.SC_MOVE || cmd == (int)NativeMethods.SC_SIZE)
                        ? (IntPtr)NativeMethods.MakeLParam(x, y)
                        : IntPtr.Zero;
                    NativeMethods.PostMessage(hwnd, (uint)NativeMethods.WM.SYSCOMMAND, (IntPtr)cmd, lParam);
                }
            }

            NativeMethods.SetWindowLong(hwnd, NativeMethods.WindowLongFlags.GWL_STYLE, oldStyle);
        }
        #endregion
    }
}