using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for Clock.xaml
    /// </summary>
    public partial class Clock : UserControl
    {
        public static DependencyProperty ClockTextProperty = DependencyProperty.Register("ClockText", typeof(string), typeof(Clock));
        public string ClockText
        {
            get { return (string)GetValue(ClockTextProperty); }
            set { SetValue(ClockTextProperty, value); }
        }

        public static DependencyProperty ClockTipProperty = DependencyProperty.Register("ClockTip", typeof(string), typeof(Clock));
        public string ClockTip
        {
            get { return (string)GetValue(ClockTipProperty); }
            set { SetValue(ClockTipProperty, value); }
        }

        private readonly DispatcherTimer clock = new DispatcherTimer(DispatcherPriority.Background);
        private readonly DispatcherTimer singleClick = new DispatcherTimer(DispatcherPriority.Input);

        public Clock()
        {
            InitializeComponent();
            DataContext = this;

            Initialize();
        }

        private void Initialize()
        {
            if (Settings.Instance.ShowClock)
            {
                StartClock();
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
            Microsoft.Win32.SystemEvents.TimeChanged += TimeChanged;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void StartClock()
        {
            SetTime();

            clock.Interval = TimeSpan.FromMilliseconds(500);
            clock.Tick += Clock_Tick;
            clock.Start();

            singleClick.Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime);
            singleClick.Tick += SingleClick_Tick;

            ClockTextBlock.Visibility = Visibility.Visible;
        }

        private void StopClock()
        {
            clock.Stop();

            ClockTextBlock.Visibility = Visibility.Collapsed;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowClock")
            {
                if (Settings.Instance.ShowClock)
                {
                    StartClock();
                }
                else
                {
                    StopClock();
                }
            }
        }

        private void Clock_Tick(object sender, EventArgs args)
        {
            SetTime();
        }

        private void SingleClick_Tick(object sender, EventArgs args)
        {
            // Windows 11 single-click action
            // A double-click will cancel the timer so that this doesn't run

            singleClick.Stop();
            ShellHelper.ShowNotificationCenter();
        }

        private void TimeChanged(object sender, EventArgs e)
        {
            TimeZoneInfo.ClearCachedData();
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.TimeChanged -= TimeChanged;
        }

        private void SetTime()
        {
            DateTime now = DateTime.Now;

            ClockText = now.ToShortTimeString();
            ClockTip = now.ToLongDateString();
        }

        private void OpenDateTimeCpl()
        {
            ShellHelper.StartProcess("timedate.cpl");
        }

        private void Clock_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (EnvironmentHelper.IsWindows11OrBetter)
            {
                singleClick.Start();
            }
            else
            {
                MessageBox.Show("Meh!");
            }
        }

        private void Clock_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            singleClick.Stop();
            OpenDateTimeCpl();

            e.Handled = true;
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDateTimeCpl();
        }
    }
}
