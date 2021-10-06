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

        private DispatcherTimer clock;

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
            clock = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 500), DispatcherPriority.Background, Clock_Tick,
                Dispatcher);
            ClockTextBlock.Visibility = Visibility.Visible;
        }

        private void StopClock()
        {
            if (clock != null)
            {
                clock.Stop();
                clock = null;
            }

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

        private void Clock_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenDateTimeCpl();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDateTimeCpl();
        }

        private void Clock_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (EnvironmentHelper.IsWindows11OrBetter)
            {
                ShellHelper.ShowNotificationCenter();
            }
        }
    }
}
