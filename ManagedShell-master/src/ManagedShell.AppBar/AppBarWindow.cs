using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ManagedShell.AppBar
{
    public class AppBarWindow : Window, INotifyPropertyChanged
    {
        protected readonly AppBarManager _appBarManager;
        protected readonly ExplorerHelper _explorerHelper;
        protected readonly FullScreenHelper _fullScreenHelper;

        public AppBarScreen Screen;
        protected bool ProcessScreenChanges = true;

        private double _dpiScale = 1.0;
        public double DpiScale
        {
            get
            {
                return _dpiScale;
            }
            set
            {
                if (_dpiScale != value)
                {
                    _dpiScale = value;
                    OnPropertyChanged();
                }
            }
        }

        // Window properties
        private WindowInteropHelper helper;
        private bool IsMoving;
        private bool IsRaising;
        public IntPtr Handle;
        public bool AllowClose;
        public bool IsClosing;
        public bool IsOpening = true;
        protected double DesiredHeight;
        protected double DesiredWidth;
        private bool EnableBlur;
        public NativeMethods.Rect WindowRect = new NativeMethods.Rect();

        // AppBar properties
        private int AppBarMessageId = -1;

        private AppBarEdge _appBarEdge;
        public AppBarEdge AppBarEdge
        {
            get
            {
                return _appBarEdge;
            }
            set
            {
                _appBarEdge = value;
                OnPropertyChanged();
                OnPropertyChanged("Orientation");
            }
        }
        private AppBarMode _appBarMode;
        public AppBarMode AppBarMode
        {
            get
            {
                return _appBarMode;
            }
            set
            {
                _appBarMode = value;
                OnPropertyChanged();
            }
        }
        private FrameworkElement _autoHideElement;
        public FrameworkElement AutoHideElement
        {
            get
            {
                return _autoHideElement;
            }
            set
            {
                _autoHideElement = value;
                _autoHideElement.RenderTransform = new TranslateTransform();
                OnPropertyChanged();
            }
        }
        public bool AllowAutoHide
        {
            get => ShouldAllowAutoHide();
        }
        protected internal bool RequiresScreenEdge;
        protected double AutoHideShowMargin = 2;
        protected double AutoHideDelayMs = 400;
        protected double AutoHideShowDelayMs = 0;
        protected double AutoHideAnimationMs = 300;
        protected double AutoHideShowAnimationMs = 150;

        private bool _isDragWithin;
        private bool _isMouseWithin;
        private bool _isContextMenuOpen;
        private DispatcherTimer _peekAutoHideTimer;

        public Orientation Orientation
        {
            get => (AppBarEdge == AppBarEdge.Left || AppBarEdge == AppBarEdge.Right) ? Orientation.Vertical : Orientation.Horizontal;
        }

        public AppBarWindow(AppBarManager appBarManager, ExplorerHelper explorerHelper, FullScreenHelper fullScreenHelper, AppBarScreen screen, AppBarEdge edge, AppBarMode mode, double size)
        {
            _explorerHelper = explorerHelper;
            _fullScreenHelper = fullScreenHelper;
            _appBarManager = appBarManager;

            Closing += OnClosing;
            SourceInitialized += OnSourceInitialized;

            PreviewDragEnter += AppBarWindow_PreviewDragEnter;
            PreviewDragLeave += AppBarWindow_PreviewDragLeave;
            PreviewDrop += AppBarWindow_PreviewDrop;
            MouseEnter += AppBarWindow_MouseEnter;
            MouseLeave += AppBarWindow_MouseLeave;
            ContextMenuOpening += AppBarWindow_ContextMenuOpening;
            ContextMenuClosing += AppBarWindow_ContextMenuClosing;
            PropertyChanged += AppBarWindow_PropertyChanged;

            ResizeMode = ResizeMode.NoResize;
            Title = "";
            Topmost = true;
            UseLayoutRounding = true;
            WindowStyle = WindowStyle.None;

            Screen = screen;
            AppBarEdge = edge;
            AppBarMode = mode;

            if (Orientation == Orientation.Vertical)
            {
                DesiredWidth = size;
            }
            else
            {
                DesiredHeight = size;
            }
        }

        private void AppBarWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsOpening)
            {
                return;
            }

            if (e.PropertyName == "AllowAutoHide")
            {
                if (AllowAutoHide)
                {
                    AnimateAutoHide(true);
                }
                else
                {
                    AnimateAutoHide(false);
                }
            }
            else if (e.PropertyName == "AppBarMode")
            {
                if (AppBarMode == AppBarMode.Normal)
                {
                    RegisterAppBar();
                }
                else
                {
                    UnregisterAppBar();
                }

                if (AppBarMode == AppBarMode.AutoHide)
                {
                    _appBarManager.RegisterAutoHideBar(this);
                    OnPropertyChanged("AllowAutoHide");
                }
                else
                {
                    _appBarManager.UnregisterAutoHideBar(this);
                    AnimateAutoHide(false, true);
                }

                if (AppBarMode == AppBarMode.None)
                {
                    _appBarManager.RegisterOverlappingBar(this);
                }
                else
                {
                    _appBarManager.UnregisterOverlappingBar(this);
                }
            }
        }

        private void AnimateAutoHide(bool isHiding, bool immediate = false)
        {
            if (AutoHideElement == null)
            {
                return;
            }

            if (isHiding && AppBarMode != AppBarMode.AutoHide)
            {
                return;
            }

            double animTo = 0;

            if (isHiding)
            {
                animTo = Orientation == Orientation.Horizontal ? DesiredHeight : DesiredWidth;
                animTo -= AutoHideShowMargin;

                if (AppBarEdge == AppBarEdge.Top || AppBarEdge == ((FlowDirection == FlowDirection.LeftToRight) ? AppBarEdge.Left : AppBarEdge.Right))
                {
                    animTo *= -1;
                }
            }

            var animation = new DoubleAnimation(animTo, TimeSpan.FromMilliseconds(isHiding ? AutoHideAnimationMs : AutoHideShowAnimationMs).Duration());
            animation.BeginTime = TimeSpan.FromMilliseconds(immediate ? 0 : isHiding ? AutoHideDelayMs : AutoHideShowDelayMs);
            animation.EasingFunction = new SineEase();

            animation.CurrentStateInvalidated += (object sender, EventArgs e) => {
                if (((AnimationClock)sender).CurrentState == ClockState.Active)
                {
                    OnAutoHideAnimationBegin(isHiding);
                }
            };

            animation.Completed += (object sender, EventArgs e) => {
                OnAutoHideAnimationComplete(isHiding);
            };

            AutoHideElement.RenderTransform.BeginAnimation(Orientation == Orientation.Horizontal ? TranslateTransform.YProperty : TranslateTransform.XProperty, animation);
        }

        protected void PeekDuringAutoHide(int msToPeek = 1000)
        {
            if (AppBarMode != AppBarMode.AutoHide)
            {
                return;
            }

            _peekAutoHideTimer?.Stop();

            AnimateAutoHide(false, true);

            _peekAutoHideTimer = new DispatcherTimer();
            _peekAutoHideTimer.Interval = TimeSpan.FromMilliseconds(msToPeek);
            _peekAutoHideTimer.Tick += (object sender, EventArgs e) =>
            {
                _peekAutoHideTimer?.Stop();
                if (AllowAutoHide)
                {
                    AnimateAutoHide(true, true);
                }
            };
            _peekAutoHideTimer.Start();
        }

        #region Events
        protected virtual void OnSourceInitialized(object sender, EventArgs e)
        {
            // set up helper and get handle
            helper = new WindowInteropHelper(this);
            Handle = helper.Handle;

            // set up window procedure
            HwndSource source = HwndSource.FromHwnd(Handle);
            source.AddHook(WndProc);

            // set initial DPI. We do it here so that we get the correct value when DPI has changed since initial user logon to the system.
            if (Screen.Primary)
            {
                DpiHelper.DpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            }

            // use system DPI initially; when we set position we will get WM_DPICHANGED and set it correctly
            DpiScale = DpiHelper.DpiScale;

            SetWindowPosition(GetDesiredRect());

            if (AppBarMode == AppBarMode.Normal)
            {
                RegisterAppBar();
            }
            else if (AppBarMode == AppBarMode.AutoHide)
            {
                _appBarManager.RegisterAutoHideBar(this);
            }
            else
            {
                _appBarManager.RegisterOverlappingBar(this);
            }

            // hide from alt-tab etc
            WindowHelper.HideWindowFromTasks(Handle);

            // register for full-screen notifications
            _fullScreenHelper.FullScreenApps.CollectionChanged += FullScreenApps_CollectionChanged;

            IsOpening = false;
            OnPropertyChanged("AllowAutoHide");
        }

        protected virtual void OnAutoHideAnimationBegin(bool isHiding)
        {
            if (isHiding && EnableBlur && Handle != IntPtr.Zero && AllowsTransparency && AllowAutoHide)
            {
                // Disable blur if enabled and hiding
                WindowHelper.SetWindowBlur(Handle, false);
            }
        }

        protected virtual void OnAutoHideAnimationComplete(bool isHiding)
        {
            if (!isHiding && EnableBlur && Handle != IntPtr.Zero && AllowsTransparency && !AllowAutoHide)
            {
                // Re-enable blur if enabled and showing
                WindowHelper.SetWindowBlur(Handle, true);
            }
        }

        protected virtual void OnFullScreenEnter(FullScreenApp app)
        {
            if (AppBarMode != AppBarMode.Normal && app.fromTasksService)
            {
                // If we are not reserving space, then some maximized windows could be mistaken as full-screen.
                // Use the same strict bounds checks as full-screen apps with fromTasksService=false.
                if (!(app.rect.Top == app.screen.Bounds.Top && app.rect.Left == app.screen.Bounds.Left &&
                    app.rect.Bottom == app.screen.Bounds.Bottom && app.rect.Right == app.screen.Bounds.Right))
                {
                    ShellLogger.Debug($"AppBarWindow: {Name} on {Screen.DeviceName} ignoring full-screen app");
                    return;
                }
            }

            ShellLogger.Debug($"AppBarWindow: {Name} on {Screen.DeviceName} conceding to full-screen app");

            Topmost = false;
            WindowHelper.ShowWindowBottomMost(Handle);
        }

        protected virtual void OnFullScreenLeave()
        {
            ShellLogger.Debug($"AppBarWindow: {Name} on {Screen.DeviceName} returning to normal state");

            IsRaising = true;
            Topmost = true;
            WindowHelper.ShowWindowTopMost(Handle);
            IsRaising = false;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            IsClosing = true;

            CustomClosing();

            if (AllowClose)
            {
                UnregisterAppBar();
                _appBarManager.UnregisterAutoHideBar(this);
                _appBarManager.UnregisterOverlappingBar(this);
                AutoHideElement?.RenderTransform?.BeginAnimation(TranslateTransform.YProperty, null);
                AutoHideElement?.RenderTransform?.BeginAnimation(TranslateTransform.XProperty, null);

                // unregister full-screen notifications
                _fullScreenHelper.FullScreenApps.CollectionChanged -= FullScreenApps_CollectionChanged;
            }
            else
            {
                IsClosing = false;
                e.Cancel = true;
            }
        }

        private void FullScreenApps_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            FullScreenApp app = null;

            foreach (FullScreenApp _app in _fullScreenHelper.FullScreenApps)
            {
                if (_app.screen.DeviceName == Screen.DeviceName || _app.screen.IsVirtualScreen)
                {
                    // there is a full screen app on our screen
                    app = _app;
                    break;
                }
            }

            if (app != null && Topmost)
            {
                OnFullScreenEnter(app);
            }
            else if (app == null && !Topmost)
            {
                OnFullScreenLeave();
            }
        }

        private void AppBarWindow_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            SetAutoHideStateVar(ref _isContextMenuOpen, false);
        }

        private void AppBarWindow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // ContextMenuOpening fires even if the element has no context menu defined, so we must check
            if (HasContextMenu(e.OriginalSource as FrameworkElement))
            {
                SetAutoHideStateVar(ref _isContextMenuOpen, true);
            }
        }

        private void AppBarWindow_PreviewDragEnter(object sender, DragEventArgs e)
        {
            SetAutoHideStateVar(ref _isDragWithin, true);
        }

        private void AppBarWindow_PreviewDragLeave(object sender, DragEventArgs e)
        {
            SetAutoHideStateVar(ref _isDragWithin, false);
        }

        private void AppBarWindow_PreviewDrop(object sender, DragEventArgs e)
        {
            SetAutoHideStateVar(ref _isDragWithin, false);
        }

        private void AppBarWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetAutoHideStateVar(ref _isMouseWithin, true);
        }

        private void AppBarWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetAutoHideStateVar(ref _isMouseWithin, false);
        }

        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == AppBarMessageId && AppBarMessageId != -1)
            {
                switch ((NativeMethods.AppBarNotifications)wParam.ToInt32())
                {
                    case NativeMethods.AppBarNotifications.PosChanged:
                        _appBarManager.ABSetPos(this);
                        break;

                    case NativeMethods.AppBarNotifications.WindowArrange:
                        if ((int)lParam != 0) // before
                        {
                            Visibility = Visibility.Collapsed;
                        }
                        else // after
                        {
                            Visibility = Visibility.Visible;
                        }

                        break;
                }
                handled = true;
            }
            else if (msg == (int)NativeMethods.WM.ACTIVATE && AppBarMode == AppBarMode.Normal && !EnvironmentHelper.IsAppRunningAsShell && !AllowClose)
            {
                _appBarManager.AppBarActivate(this);
            }
            else if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING)
            {
                // Extract the WINDOWPOS structure corresponding to this message
                NativeMethods.WINDOWPOS wndPos = NativeMethods.WINDOWPOS.FromMessage(lParam);

                // Determine if the z-order is changing (absence of SWP_NOZORDER flag)
                // If we are intentionally trying to become topmost, make it so
                if (IsRaising && (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOZORDER) == 0)
                {
                    // Sometimes Windows thinks we shouldn't go topmost, so poke here to make it happen.
                    wndPos.hwndInsertAfter = (IntPtr)NativeMethods.WindowZOrder.HWND_TOPMOST;
                    wndPos.UpdateMessage(lParam);
                }

                // WORKAROUND WPF bug: https://github.com/dotnet/wpf/issues/7561
                // If there is no NOMOVE or NOSIZE or NOACTIVATE flag, and there is a NOZORDER flag, add the NOACTIVATE flag
                if (!IsMoving && 
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOMOVE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOSIZE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOZORDER) != 0)
                {
                    wndPos.flags |= NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
                    wndPos.UpdateMessage(lParam);
                }
            }
            else if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGED)
            {
                // Extract the WINDOWPOS structure corresponding to this message
                NativeMethods.WINDOWPOS wndPos = NativeMethods.WINDOWPOS.FromMessage(lParam);

                // Determine if our window rect has changed and update the cached values
                bool changed = false;
                if ((wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOMOVE) == 0 &&
                    (wndPos.y != WindowRect.Top || wndPos.x != WindowRect.Left))
                {
                    int currentWidth = WindowRect.Width;
                    int currentHeight = WindowRect.Height;
                    WindowRect.Top = wndPos.y;
                    WindowRect.Bottom = WindowRect.Top + currentHeight;
                    WindowRect.Left = wndPos.x;
                    WindowRect.Right = WindowRect.Left + currentWidth;
                    changed = true;
                }
                if ((wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOSIZE) == 0 &&
                    (WindowRect.Bottom != WindowRect.Top + wndPos.cy || WindowRect.Right != WindowRect.Left + wndPos.cx))
                {
                    WindowRect.Bottom = WindowRect.Top + wndPos.cy;
                    WindowRect.Right = WindowRect.Left + wndPos.cx;
                    changed = true;
                }

                if (changed && AppBarMode == AppBarMode.Normal && !EnvironmentHelper.IsAppRunningAsShell && !AllowClose)
                {
                    // Tell other AppBars we changed
                    _appBarManager.AppBarWindowPosChanged(this);
                }

                // Determine if we are intentionally moving
                if (changed && !IsMoving && (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOMOVE) == 0)
                {
                    // Someone else moved us! Let's restore state.
                    ShellLogger.Debug($"AppBarWindow: Repositioning due to unexpected move to {wndPos.x},{wndPos.y}");
                    if (UpdatePosition())
                    {
                        handled = true;
                    }
                }
            }
            else if (msg == (int)NativeMethods.WM.DPICHANGED)
            {
                DpiScale = (wParam.ToInt32() & 0xFFFF) / 96d;

                if (Screen.Primary)
                {
                    DpiHelper.DpiScale = DpiScale;
                }

                // if we are opening, we're getting this message as a result of positioning
                // if we are an AppBar, that code will fix our position, so skip in that case to prevent infinite resizing.
                if (!IsOpening || AppBarMode != AppBarMode.Normal)
                {
                    ProcessScreenChange(ScreenSetupReason.DpiChange);
                }
            }
            else if (msg == (int)NativeMethods.WM.DISPLAYCHANGE)
            {
                ProcessScreenChange(ScreenSetupReason.DisplayChange);
                handled = true;
            }
            else if (msg == (int)NativeMethods.WM.DEVICECHANGE && (int)wParam == 0x0007)
            {
                ProcessScreenChange(ScreenSetupReason.DeviceChange);
                handled = true;
            }
            
            return IntPtr.Zero;
        }
        #endregion

        #region Helpers
        private void SetAutoHideStateVar(ref bool varToSet, bool newValue)
        {
            bool currentAutoHide = AllowAutoHide;
            varToSet = newValue;

            if (AllowAutoHide != currentAutoHide)
            {
                OnPropertyChanged("AllowAutoHide");
            }
        }

        private void ProcessScreenChange(ScreenSetupReason reason)
        {
            // process screen changes if we are on the primary display and the designated window
            // (or any display in the case of a DPI change, since only the changed display receives that message and not all windows receive it reliably)
            // suppress this if we are shutting down (which can trigger this method on multi-dpi setups due to window movements)
            if (((Screen.Primary && ProcessScreenChanges) || reason == ScreenSetupReason.DpiChange) && !AllowClose)
            {
                SetScreenProperties(reason);
            }
        }

        private bool HasContextMenu(FrameworkElement fe)
        {
            if (fe == null)
            {
                return false;
            }

            if (fe.ContextMenu != null)
            {
                return true;
            }
            else
            {
                var parent = VisualTreeHelper.GetParent(fe) as FrameworkElement;
                return HasContextMenu(parent);
            }
        }

        protected void SetBlur(bool enable)
        {
            if (EnableBlur != enable && Handle != IntPtr.Zero && AllowsTransparency)
            {
                EnableBlur = enable;

                if (enable && AppBarMode == AppBarMode.AutoHide && AllowAutoHide)
                {
                    // If we're auto-hidden, don't actually enable blur right now.
                    return;
                }

                WindowHelper.SetWindowBlur(Handle, enable);
            }
        }

        protected void RegisterAppBar()
        {
            if (AppBarMode != AppBarMode.Normal || _appBarManager.AppBars.Contains(this))
            {
                return;
            }

            AppBarMessageId = _appBarManager.RegisterBar(this);
        }

        protected void UnregisterAppBar()
        {
            if (!_appBarManager.AppBars.Contains(this))
            {
                return;
            }

            _appBarManager.RegisterBar(this);
        }

        protected internal NativeMethods.Rect GetDesiredRect()
        {
            NativeMethods.Rect rect = new NativeMethods.Rect();
            int edgeOffset = 0;

            if (!RequiresScreenEdge)
            {
                edgeOffset = _appBarManager.GetAppBarEdgeWindowsHeight(AppBarEdge, Screen, Handle);
            }

            if (Orientation == Orientation.Vertical)
            {
                int width = Convert.ToInt32(DesiredWidth * DpiScale);

                rect.Top = Screen.Bounds.Top;
                rect.Bottom = Screen.Bounds.Bottom;

                if (AppBarEdge == AppBarEdge.Left)
                {
                    rect.Left = Screen.Bounds.Left + edgeOffset;
                    rect.Right = rect.Left + width;
                }
                else
                {
                    rect.Right = Screen.Bounds.Right - edgeOffset;
                    rect.Left = rect.Right - width;
                }
            }
            else
            {
                int height = Convert.ToInt32(DesiredHeight * DpiScale);

                rect.Left = Screen.Bounds.Left;
                rect.Right = Screen.Bounds.Right;

                if (AppBarEdge == AppBarEdge.Bottom)
                {
                    rect.Bottom = Screen.Bounds.Bottom - edgeOffset;
                    rect.Top = rect.Bottom - height;
                }
                else
                {
                    rect.Top = Screen.Bounds.Top + edgeOffset;
                    rect.Bottom = rect.Top + height;
                }
            }

            return rect;
        }

        protected internal bool SetWindowPosition(NativeMethods.Rect newRect)
        {
            var currentRect = WindowRect;
            if (newRect.Top == currentRect.Top &&
                newRect.Left == currentRect.Left &&
                newRect.Bottom == currentRect.Bottom &&
                newRect.Right == currentRect.Right)
            {
                // Rects are the same, we don't need to do anything here
                return false;
            }

            int swp = (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
            if (newRect.Width < 0 || newRect.Height < 0)
            {
                swp |= (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE;
            }

            IsMoving = true;
            NativeMethods.SetWindowPos(Handle, IntPtr.Zero, newRect.Left, newRect.Top, newRect.Width, newRect.Height, swp);
            IsMoving = false;

            if (EnvironmentHelper.IsAppRunningAsShell)
            {
                _appBarManager.SetWorkArea(Screen);
            }

            ShellLogger.Debug($"AppBarWindow: {(!string.IsNullOrEmpty(Title) ? Title : Name)} changed position (TxLxBxR) to {newRect.Top}x{newRect.Left}x{newRect.Bottom}x{newRect.Right} from {currentRect.Top}x{currentRect.Left}x{currentRect.Bottom}x{currentRect.Right}");

            return true;
        }
        #endregion

        #region Virtual methods
        protected virtual bool ShouldAllowAutoHide()
        {
            return AppBarMode == AppBarMode.AutoHide && !_isMouseWithin && !_isContextMenuOpen && !_isDragWithin && (_peekAutoHideTimer == null || !_peekAutoHideTimer.IsEnabled);
        }

        protected virtual void CustomClosing() { }

        protected virtual void SetScreenProperties(ScreenSetupReason reason)
        {
            _fullScreenHelper.NotifyScreensChanged();

            if (Screen.Primary && reason != ScreenSetupReason.DpiChange)
            {
                Screen = AppBarScreen.FromPrimaryScreen();
            }
            UpdatePosition();
        }

        public virtual bool UpdatePosition()
        {
            // Let Explorer AppBar figure out our position if we are an AppBar, otherwise set our desired rect
            if (AppBarMode == AppBarMode.Normal && !EnvironmentHelper.IsAppRunningAsShell)
            {
                return _appBarManager.ABSetPos(this);
            }

            return SetWindowPosition(GetDesiredRect());
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}