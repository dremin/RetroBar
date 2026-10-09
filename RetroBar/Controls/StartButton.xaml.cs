#nullable enable
using System;
using System.Runtime.InteropServices;
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

    public partial class StartButton : UserControl
    {
        private FloatingStartButton? floatingStartButton;
        private bool allowOpenStart;
        private bool visibilityChanged;
        private DelayedActivationHandler? dragHandler;
        private readonly DispatcherTimer pendingOpenTimer;
        private bool? useFloatingThemeCached;
        private bool openingFloatingStart;
        private bool floatingStartTopmost = true;
        private double appliedFallbackScale = 1.0;
        private DispatcherTimer? orbRaiseTimer;
        private int orbRaiseTicks;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_REMOTESESSION = 0x1000;


        private static bool? remoteSessionCached;

        private static bool IsRemoteSession =>
            remoteSessionCached ??= GetSystemMetrics(SM_REMOTESESSION) != 0 &&
                                    Environment.GetEnvironmentVariable("RETROBAR_FORCE_FLOATING_ORB") != "1";

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
                
                SetStartMenuState(false);
            };
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                useFloatingThemeCached = null;

               
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(syncFloatingStartWithTheme));
            }
        }

        private bool useFloatingTheme()
        {
            useFloatingThemeCached ??= !IsRemoteSession && (Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false);
            return useFloatingThemeCached.Value;
        }

        private void syncFloatingStartWithTheme()
        {
            
            if (!IsLoaded || IsFloating) return;

            useFloatingThemeCached = null;
            updateRemoteFallbackScale();

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
            scheduleFloatingStartRaise();
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
                Host.SizeChanged += Host_SizeChanged;
            }

            updateRemoteFallbackScale();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StartMenuMonitor.StartMenuVisibilityChanged -= AppVisibilityHelper_StartMenuVisibilityChanged;

            if (Host != null)
            {
                Host.PropertyChanged -= Taskbar_PropertyChanged;
                Host.SizeChanged -= Host_SizeChanged;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            dragHandler?.Dispose();
            orbRaiseTimer?.Stop();

            hideFloatingStart();
        }

        private void AppVisibilityHelper_StartMenuVisibilityChanged(object? sender, StartMenuMonitor.StartMenuMonitorEventArgs e)
        {
            if (e.Visible && Host != null &&
                ((e.TaskbarHwndActivated != IntPtr.Zero && e.TaskbarHwndActivated != Host.Handle) ||
                (e.StartHmonitor != IntPtr.Zero && e.StartHmonitor != Host.Screen.HMonitor)))
            {
                
                return;
            }
            if (e.Visible)
            {
                scheduleFloatingStartRaise();
            }

            SetStartMenuState(e.Visible);
        }

        private void StartButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            visibilityChanged = true;
        }

        private void StartButton_LayoutUpdated(object? sender, EventArgs e)
        {
            updateRemoteFallbackScale();

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

            bool useFloatingStartButton = !IsRemoteSession && (Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false);

            if (!useFloatingStartButton || Visibility != Visibility.Visible) return;

            if (floatingStartButton == null)
            {
                if (openingFloatingStart) return;
                openingFloatingStart = true;

                try
                {
                    floatingStartButton = new FloatingStartButton(this, getButtonRect());
                    floatingStartButton.Show();
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

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            updateRemoteFallbackScale();
        }


        private void updateRemoteFallbackScale()
        {
            if (IsFloating) return;

            double scale = 1.0;

            if (IsRemoteSession &&
                (Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false) &&
                Host != null &&
                Host.Orientation == Orientation.Horizontal &&
                Host.ActualHeight > 0 &&
                !double.IsNaN(Start.Height) &&
                Start.Height > 0)
            {
                scale = Math.Min(1.0, Host.ActualHeight / Start.Height);
            }

       
            if (Math.Abs(scale - appliedFallbackScale) < 0.001) return;

            appliedFallbackScale = scale;
            Start.LayoutTransform = scale < 1.0 ? new ScaleTransform(scale, scale) : Transform.Identity;
        }

        private NativeMethods.Rect getButtonRect()
        {
      
            Point buttonPosPixels = Start.PointToScreen(new Point(FlowDirection == FlowDirection.LeftToRight ? 0 : Start.ActualWidth, 0));
            Point buttonSizePixels = Start.PointToScreen(new Point(FlowDirection == FlowDirection.LeftToRight ? Start.ActualWidth : 0, Start.ActualHeight));


            if (Host?.AutoHideElement?.RenderTransform is TranslateTransform tt)
            {
                buttonPosPixels.X -= (tt.X * Host.DpiScale);
                buttonPosPixels.Y -= (tt.Y * Host.DpiScale);
                buttonSizePixels.X -= (tt.X * Host.DpiScale);
                buttonSizePixels.Y -= (tt.Y * Host.DpiScale);
            }


            return new NativeMethods.Rect(
                (int)Math.Floor(buttonPosPixels.X),
                (int)Math.Floor(buttonPosPixels.Y),
                (int)Math.Ceiling(buttonSizePixels.X),
                (int)Math.Ceiling(buttonSizePixels.Y));
        }

        public void UpdateFloatingStartCoordinates()
        {

            if (!IsVisible) return;

            if (floatingStartButton == null)
            {

                if (!IsFloating && IsLoaded && (Host == null || Host.Opacity == 1) && useFloatingTheme())
                {
                    openFloatingStart();
                }

                return;
            }

            floatingStartButton.SetPosition(getButtonRect());
        }

        public void UpdateFloatingStartTopmost(bool topmost)
        {
            floatingStartTopmost = topmost;

            if (floatingStartButton == null) return;

            floatingStartButton.Topmost = topmost;

            if (!topmost)
            {
               
                NativeMethods.SetWindowPos(
                floatingStartButton.Handle,
                Host.Handle,
                0, 0, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE | (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
            }
            else
            {
              
                NativeMethods.SetWindowPos(
                floatingStartButton.Handle,
                (IntPtr)NativeMethods.WindowZOrder.HWND_TOPMOST,
                0, 0, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE | (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }


        private void raiseFloatingStart()
        {
            if (floatingStartButton == null || !floatingStartTopmost) return;

            NativeMethods.SetWindowPos(
                floatingStartButton.Handle,
                (IntPtr)NativeMethods.WindowZOrder.HWND_TOPMOST,
                0, 0, 0, 0,
                (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE | (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
        }

        private void scheduleFloatingStartRaise()
        {
            if (IsFloating || floatingStartButton == null) return;

            raiseFloatingStart();
            orbRaiseTicks = 0;

            if (orbRaiseTimer == null)
            {
                orbRaiseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                orbRaiseTimer.Tick += (s, args) =>
                {
                    raiseFloatingStart();

                    
                    if (++orbRaiseTicks >= 12)
                    {
                        orbRaiseTimer?.Stop();
                    }
                };
            }

            orbRaiseTimer.Start();
        }

        #endregion
    }
}
