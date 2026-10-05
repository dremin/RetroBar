#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for StartButton.xaml
    /// </summary>
    public partial class StartButton : UserControl
    {
        private FloatingStartButton? floatingStartButton;
        private bool allowOpenStart;
        private bool visibilityChanged;
        private DelayedActivationHandler? dragHandler;
        private readonly DispatcherTimer pendingOpenTimer;
        private bool? useFloatingThemeCached;
        private bool openingFloatingStart;

        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(Taskbar), typeof(StartButton));
        public static DependencyProperty StartMenuMonitorProperty = DependencyProperty.Register(nameof(StartMenuMonitor), typeof(StartMenuMonitor), typeof(StartButton));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public static DependencyProperty IsFloatingProperty = DependencyProperty.Register(nameof(IsFloating), typeof(bool), typeof(StartButton), new PropertyMetadata(false, OnIsFloatingChanged));

        private static void OnIsFloatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StartButton startButton && (bool)e.NewValue)
            {
                startButton.Start.Margin = new Thickness(0);
            }
        }

        public bool IsFloating
        {
            get { return (bool)GetValue(IsFloatingProperty); }
            set { SetValue(IsFloatingProperty, value); }
        }

        public StartMenuMonitor StartMenuMonitor
        {
            get { return (StartMenuMonitor)GetValue(StartMenuMonitorProperty); }
            set { SetValue(StartMenuMonitorProperty, value); }
        }

        public StartButton()
        {
            InitializeComponent();

            pendingOpenTimer = new DispatcherTimer(DispatcherPriority.Background);
            pendingOpenTimer.Interval = new TimeSpan(0, 0, 0, 1);
            pendingOpenTimer.Tick += (sender, args) =>
            {
                // if the start menu didn't open, flip the button back to unchecked
                SetStartMenuState(false);
            };
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                useFloatingThemeCached = null;

                // Run after the theme dictionary has been swapped, whatever order the Settings
                // handlers fire in, and after any taskbar reopen has been processed.
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(syncFloatingStartWithTheme));
            }
        }

        private bool useFloatingTheme()
        {
            useFloatingThemeCached ??= Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false;
            return useFloatingThemeCached.Value;
        }

        private void syncFloatingStartWithTheme()
        {
            // A closed or replaced taskbar must not spawn an orb of its own.
            if (!IsLoaded || IsFloating) return;

            useFloatingThemeCached = null;
            OrbLog.Write("syncFloatingStartWithTheme: theme=" + Settings.Instance.Theme + " useFloating=" + useFloatingTheme() + " existing=" + (floatingStartButton != null));

            if (useFloatingTheme())
            {
                if (floatingStartButton == null)
                {
                    openFloatingStart();
                }
                else
                {
                    showFloatingStart();
                }
            }
            else if (floatingStartButton != null)
            {
                closeFloatingStart();
            }
        }

        public void SetStartMenuState(bool opened)
        {
            Dispatcher.Invoke(() =>
            {
                Start.IsChecked = opened;
                Host?.SetStartMenuOpen(opened);
            });
            pendingOpenTimer.Stop();
        }

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            if (allowOpenStart)
            {
                OpenStartMenu();
                return;
            }

            SetStartMenuState(false);
        }

        private void OpenStartMenu()
        {
            Host?.SetTrayHost();
            Host?.SetStartMenuOpen(true);
            pendingOpenTimer.Start();
            if (Host != null && StartMenuMonitor != null && Settings.Instance.ShowMultiMon && Settings.Instance.ShowStartButtonMultiMon)
            {
                StartMenuMonitor.ShowStartMenu(Host.Handle);
            }
            else
            {
                ShellHelper.ShowStartMenu();
            }
        }

        private void Start_DragEnter(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragEnter(e);
        }

        private void Start_DragLeave(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragLeave();
        }

        private void Start_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            allowOpenStart = Start.IsChecked == false;
        }

        private void Start_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (EnvironmentHelper.IsWindows10OrBetter)
            {
                ShellHelper.ShowStartContextMenu();
                e.Handled = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            OrbLog.Write("StartButton Loaded: floating=" + IsFloating + " host=" + (Host != null) + " theme=" + Settings.Instance.Theme + " vis=" + Visibility + " isVisible=" + IsVisible);

            StartMenuMonitor.StartMenuVisibilityChanged += AppVisibilityHelper_StartMenuVisibilityChanged;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            dragHandler = new DelayedActivationHandler(() =>
            {
                if (Start.IsChecked == false)
                {
                    OpenStartMenu();
                }
            });

            openFloatingStart();

            IsVisibleChanged += StartButton_IsVisibleChanged;
            LayoutUpdated += StartButton_LayoutUpdated;

            if (Host != null)
            {
                Host.PropertyChanged += Taskbar_PropertyChanged;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StartMenuMonitor.StartMenuVisibilityChanged -= AppVisibilityHelper_StartMenuVisibilityChanged;

            if (Host != null)
            {
                Host.PropertyChanged -= Taskbar_PropertyChanged;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            dragHandler?.Dispose();

            hideFloatingStart();
        }

        private void AppVisibilityHelper_StartMenuVisibilityChanged(object? sender, StartMenuMonitor.StartMenuMonitorEventArgs e)
        {
            if (e.Visible && Host != null &&
                ((e.TaskbarHwndActivated != IntPtr.Zero && e.TaskbarHwndActivated != Host.Handle) ||
                (e.StartHmonitor != IntPtr.Zero && e.StartHmonitor != Host.Screen.HMonitor)))
            {
                // Only set as visible when activated from our taskbar
                return;
            }
            SetStartMenuState(e.Visible);
        }

        private void StartButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            visibilityChanged = true;
        }

        private void StartButton_LayoutUpdated(object? sender, EventArgs e)
        {
            if (!visibilityChanged)
            {
                UpdateFloatingStartCoordinates();
                return;
            }

            visibilityChanged = false;

            if (IsVisible)
            {
                openFloatingStart();
            }
            else
            {
                hideFloatingStart();
            }
        }

        private void Taskbar_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Host != null && e.PropertyName == nameof(Opacity))
            {
                if (Host.Opacity == 1)
                {
                    openFloatingStart();
                }
                else
                {
                    hideFloatingStart();
                }
            }
        }

        #region Floating start button

        private void openFloatingStart()
        {
            if (IsFloating) return;

            bool useFloatingStartButton = Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false;

            OrbLog.Write("openFloatingStart: useFloating=" + useFloatingStartButton + " vis=" + Visibility + " existing=" + (floatingStartButton != null));

            if (!useFloatingStartButton || Visibility != Visibility.Visible) return;

            if (floatingStartButton == null)
            {
                if (openingFloatingStart) return;
                openingFloatingStart = true;

                try
                {
                    var startRect = getButtonRect();
                    OrbLog.Write("creating orb window, startRect=(" + startRect.Left + "," + startRect.Top + "," + startRect.Right + "," + startRect.Bottom + ") size=" + startRect.Width + "x" + startRect.Height);
                    floatingStartButton = new FloatingStartButton(this, startRect);
                    floatingStartButton.Show();
                    OrbLog.Write("orb shown: " + OrbLog.Describe(floatingStartButton.Handle) + " wpfVisible=" + floatingStartButton.IsVisible);

                    var shown = floatingStartButton;
                    var snapshot = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    snapshot.Tick += (s, args) =>
                    {
                        snapshot.Stop();
                        var now = getButtonRect();
                        try
                        {
                            var inner = shown.Content as StartButton;
                            var btn = inner?.Start;
                            var border = btn?.Template?.FindName("StartButtonBorder", btn) as Border;
                            string brushInfo = "none";
                            if (border?.Background is ImageBrush ib)
                            {
                                var bi = ib.ImageSource as System.Windows.Media.Imaging.BitmapImage;
                                brushInfo = "ImageBrush uri=" + bi?.UriSource + " px=" + bi?.PixelWidth + "x" + bi?.PixelHeight +
                                            " downloading=" + bi?.IsDownloading + " viewbox=" + ib.Viewbox;
                            }
                            else if (border?.Background != null)
                            {
                                brushInfo = border.Background.GetType().Name;
                            }
                            OrbLog.Write("orb inner: startButton=" + (inner != null) + " visible=" + inner?.IsVisible + " btnSize=" + btn?.ActualWidth + "x" + btn?.ActualHeight +
                                         " btnOpacity=" + btn?.Opacity + " btnVis=" + btn?.Visibility + " borderSize=" + border?.ActualWidth + "x" + border?.ActualHeight +
                                         " borderOpacity=" + border?.Opacity + " bg=" + brushInfo + " windowBg=" + shown.Background);
                        }
                        catch (Exception ex)
                        {
                            OrbLog.Write("orb inner describe failed: " + ex.Message);
                        }

                        OrbLog.Write("orb 2s later: " + OrbLog.Describe(shown.Handle) + " wpfVisible=" + shown.IsVisible + " topmost=" + shown.Topmost +
                                     " actual=" + shown.ActualWidth + "x" + shown.ActualHeight + " buttonRectNow=(" + now.Left + "," + now.Top + "," + now.Right + "," + now.Bottom + ")" +
                                     " hostOpacity=" + (Host != null ? Host.Opacity : -1));
                    };
                    snapshot.Start();
                }
                catch (Exception ex)
                {
                    OrbLog.Write("orb creation FAILED: " + ex);
                    throw;
                }
                finally
                {
                    openingFloatingStart = false;
                }
            }
            else
            {
                showFloatingStart();
            }

            // Hide the original so only the floating orb is visible.
            Opacity = 0;
        }

        private void showFloatingStart()
        {
            if (floatingStartButton == null) return;

            UpdateFloatingStartCoordinates();
            floatingStartButton.Visibility = Visibility.Visible;
        }

        private void hideFloatingStart()
        {
            if (IsFloating) return;
            if (floatingStartButton == null) return;

            floatingStartButton.Visibility = Visibility.Hidden;
        }

        private void closeFloatingStart()
        {
            floatingStartButton?.Close();
            floatingStartButton = null;
            Opacity = 1;
        }

        private NativeMethods.Rect getButtonRect()
        {
            // Get the pixel values of the start button's bounds
            Point buttonPosPixels = Start.PointToScreen(new Point(FlowDirection == FlowDirection.LeftToRight ? 0 : Start.ActualWidth, 0));
            Point buttonSizePixels = Start.PointToScreen(new Point(FlowDirection == FlowDirection.LeftToRight ? Start.ActualWidth : 0, Start.ActualHeight));

            // If the start button is currently translated, we get the translated position
            // and need to offset by that much to be positioned correctly.
            if (Host?.AutoHideElement?.RenderTransform is TranslateTransform tt)
            {
                buttonPosPixels.X -= (tt.X * Host.DpiScale);
                buttonPosPixels.Y -= (tt.Y * Host.DpiScale);
                buttonSizePixels.X -= (tt.X * Host.DpiScale);
                buttonSizePixels.Y -= (tt.Y * Host.DpiScale);
            }

            // Round outward instead of truncating, so fractional DPI positions never shave a pixel
            // off the edges of the orb.
            return new NativeMethods.Rect(
                (int)Math.Floor(buttonPosPixels.X),
                (int)Math.Floor(buttonPosPixels.Y),
                (int)Math.Ceiling(buttonSizePixels.X),
                (int)Math.Ceiling(buttonSizePixels.Y));
        }

        public void UpdateFloatingStartCoordinates()
        {
            // Can't get our coordinates if we aren't visible.
            if (!IsVisible) return;

            if (floatingStartButton == null)
            {
                // Self-heal: the theme wants an orb but none exists (for example after a live theme switch).
                if (!IsFloating && IsLoaded && (Host == null || Host.Opacity == 1) && useFloatingTheme())
                {
                    OrbLog.Write("self-heal: creating missing orb");
                    openFloatingStart();
                }

                return;
            }

            floatingStartButton.SetPosition(getButtonRect());
        }

        public void UpdateFloatingStartTopmost(bool topmost)
        {
            if (floatingStartButton == null) return;

            floatingStartButton.Topmost = topmost;

            if (!topmost)
            {
                // Setting Topmost=false itself does not guarantee that we will go below a full-screen window.
                NativeMethods.SetWindowPos(
                floatingStartButton.Handle,
                Host.Handle,
                0, 0, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE | (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
            }
            else
            {
                // Ensure the floating start button is truly topmost when restoring.
                NativeMethods.SetWindowPos(
                floatingStartButton.Handle,
                (IntPtr)NativeMethods.WindowZOrder.HWND_TOPMOST,
                0, 0, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE | (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }

        #endregion
    }
}
