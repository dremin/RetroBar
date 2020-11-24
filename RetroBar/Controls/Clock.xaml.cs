using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;

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

        public Clock()
        {
            InitializeComponent();
            DataContext = this;

            InitializeClock();
        }

        private void InitializeClock()
        {
            SetTime();
            DispatcherTimer clock = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 500), DispatcherPriority.Background, Clock_Tick, Dispatcher);

            Microsoft.Win32.SystemEvents.TimeChanged += TimeChanged;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
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
            Shell.StartProcess("timedate.cpl");
        }

        private void Clock_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenDateTimeCpl();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDateTimeCpl();
        }
    }
}
