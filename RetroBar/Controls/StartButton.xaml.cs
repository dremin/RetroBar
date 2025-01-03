#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
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
        private readonly DispatcherTimer pendingOpenTimer;

        public static DependencyProperty HostProperty = DependencyProperty.Register("Host", typeof(Taskbar), typeof(StartButton));
        public static DependencyProperty StartMenuMonitorProperty = DependencyProperty.Register("StartMenuMonitor", typeof(StartMenuMonitor), typeof(StartButton));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
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
                bool useFloatingStartButton = Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false;

                if (!useFloatingStartButton && floatingStartButton != null)
                {
                    closeFloatingStart();
                }
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
            if (Host != null && StartMenuMonitor != null && Settings.Instance.ShowMultiMon)
            {
                StartMenuMonitor.ShowStartMenu(Host.Handle);
            }
            else
            {
                ShellHelper.ShowStartMenu();
            }
        }

        #region Drag
        private bool inDrag;
        private DispatcherTimer? dragTimer;

        private void dragTimer_Tick(object? sender, EventArgs e)
        {
            if (inDrag && Start.IsChecked == false)
            {
                OpenStartMenu();
            }

            dragTimer?.Stop();
        }

        private void Start_DragEnter(object sender, DragEventArgs e)
        {
            // Ignore drag operations from a reorder
            if (!inDrag && !e.Data.GetDataPresent("GongSolutions.Wpf.DragDrop"))
            {
                inDrag = true;
                dragTimer?.Start();
            }
        }

        private void Start_DragLeave(object sender, DragEventArgs e)
        {
            if (inDrag)
            {
                dragTimer?.Stop();
                inDrag = false;
            }
        }

        #endregion

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

            // drag support - delayed activation using system setting
            dragTimer = new DispatcherTimer { Interval = SystemParameters.MouseHoverTime };
            dragTimer.Tick += dragTimer_Tick;

            openFloatingStart();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StartMenuMonitor.StartMenuVisibilityChanged -= AppVisibilityHelper_StartMenuVisibilityChanged;

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            hideFloatingStart();
        }

        private void AppVisibilityHelper_StartMenuVisibilityChanged(object? sender, StartMenuMonitor.StartMenuMonitorEventArgs e)
        {
            if (e.Visible && e.TaskbarHwndActivated != IntPtr.Zero && Host != null && e.TaskbarHwndActivated != Host.Handle)
            {
                // Only set as visible when activated from our taskbar
                return;
            }
            SetStartMenuState(e.Visible);
        }

        #region Floating start button

        private void openFloatingStart()
        {
            bool useFloatingStartButton = Application.Current.FindResource("UseFloatingStartButton") as bool? ?? false;

            if (!useFloatingStartButton || Host?.Screen.Primary != true) return;

            if (floatingStartButton == null)
            {
                floatingStartButton = new FloatingStartButton(this, getButtonCoordinates(), getButtonSize());
                floatingStartButton.Show();
            }
            else
            {
                showFloatingStart();
            }
        }

        private void showFloatingStart()
        {
            if (floatingStartButton == null) return;

            UpdateFloatingStartCoordinates();
            floatingStartButton.Visibility = Visibility.Visible;
        }

        private void hideFloatingStart()
        {
            if (floatingStartButton == null) return;

            floatingStartButton.Visibility = Visibility.Hidden;
        }

        private void closeFloatingStart()
        {
            floatingStartButton?.Close();
            floatingStartButton = null;
        }

        private Point getButtonCoordinates()
        {
            // Get the location of the start button's top left
            Point buttonPosPixels = Start.PointToScreen(new Point(0, 0));

            // Convert from pixels to WPF points
            PresentationSource source = PresentationSource.FromVisual(this);
            Point buttonPosPoints = source.CompositionTarget.TransformFromDevice.Transform(buttonPosPixels);

            // If the start button is currently translated, we get the translated position
            // and need to offset by that much to be positioned correctly.
            if (Host?.AutoHideElement?.RenderTransform is TranslateTransform tt)
            {
                buttonPosPoints.X += (tt.X * -1);
                buttonPosPoints.Y += (tt.Y * -1);
            }

            return buttonPosPoints;
        }

        private Size getButtonSize()
        {
            return new Size(Start.ActualWidth * Settings.Instance.TaskbarScale, Start.ActualHeight * Settings.Instance.TaskbarScale);
        }

        public void UpdateFloatingStartCoordinates()
        {
            // Can't get our coordinates if we aren't visible.
            if (!IsVisible || floatingStartButton == null) return;

            floatingStartButton.SetPosition(getButtonCoordinates(), getButtonSize());
        }

        #endregion
    }
}