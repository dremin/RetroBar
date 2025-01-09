using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.UWPInterop;
using Microsoft.Win32;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for Clock.xaml
    /// </summary>
    public partial class Clock : UserControl
    {
        public static DependencyProperty NowProperty = DependencyProperty.Register("Now", typeof(DateTime), typeof(Clock));

        public DateTime Now
        {
            get { return (DateTime)GetValue(NowProperty); }
            set { SetValue(NowProperty, value); }
        }

        private readonly DispatcherTimer clock = new DispatcherTimer(DispatcherPriority.Background);

        private bool _isLoaded;

        public Clock()
        {
            InitializeComponent();
            DataContext = this;

            clock.Interval = TimeSpan.FromMilliseconds(200);
            clock.Tick += Clock_Tick;
        }

        private void Initialize()
        {
            SetCurrentCulture();
            if (Settings.Instance.ShowClock)
            {
                StartClock();
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
            SystemEvents.TimeChanged += TimeChanged;
            SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        }

        private void StartClock()
        {
            SetTime();

            clock.Start();

            Visibility = Visibility.Visible;
        }

        private void StopClock()
        {
            clock.Stop();

            Visibility = Visibility.Collapsed;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.ShowClock))
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

        private static void SetConverterCultureRecursively(DependencyObject main)
        {
            if (main != null)
            {
                var binding = BindingOperations.GetBinding(main, TextBlock.TextProperty);

                if (binding != null)
                {
                    BindingOperations.SetBinding(main, TextBlock.TextProperty,
                        new Binding(binding.Path.Path)
                            { StringFormat = binding.StringFormat, ConverterCulture = CultureInfo.CurrentCulture });
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(main); i++)
                {
                    if (VisualTreeHelper.GetChild(main, i) is UIElement sub)
                    {
                        SetConverterCultureRecursively(sub);
                    }
                }
            }
        }

        private void SetCurrentCulture()
        {
            try
            {
                var iKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\International", false);
                if (iKey == null) return;
                var iCi = (CultureInfo)CultureInfo.GetCultureInfo((string)iKey.GetValue("LocaleName")).Clone();
                iCi.DateTimeFormat.ShortDatePattern = (string)iKey.GetValue("sShortDate");
                iCi.DateTimeFormat.ShortTimePattern = (string)iKey.GetValue("sShortTime");
                iCi.DateTimeFormat.LongDatePattern = (string)iKey.GetValue("sLongDate");
                iCi.DateTimeFormat.LongTimePattern = (string)iKey.GetValue("sTimeFormat");
                iCi.DateTimeFormat.DateSeparator = (string)iKey.GetValue("sDate");
                iCi.DateTimeFormat.TimeSeparator = (string)iKey.GetValue("sTime");
                iCi.DateTimeFormat.AMDesignator = (string)iKey.GetValue("s1159");
                iCi.DateTimeFormat.PMDesignator = (string)iKey.GetValue("s2359");

                CultureInfo.CurrentCulture = iCi;
                SetConverterCultureRecursively(this);
                SetConverterCultureRecursively(ClockTip);
            }
            catch (Exception e)
            {
                ShellLogger.Error($"Clock: Unable to set the current culture: {e.Message}");
            }
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Locale)
            {
                SetCurrentCulture();
            }
        }

        private void SetTime()
        {
            Now = DateTime.Now;
        }

        private void Clock_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Instance.ClockClickAction == ClockClickOption.DoNothing)
            {
                return;
            }

            e.Handled = true;

            switch (Settings.Instance.ClockClickAction)
            {
                case ClockClickOption.OpenModernCalendar:
                    Point screenPosition = PointToScreen(new(0, 0));
                    ManagedShell.Interop.NativeMethods.Rect rect = new(
                        (int)screenPosition.X, (int)screenPosition.Y,
                        (int)(screenPosition.X + RenderSize.Width),
                        (int)(screenPosition.Y + RenderSize.Height)
                    );
                    ImmersiveShellHelper.ShowClockFlyout(rect);
                    break;
                case ClockClickOption.OpenAeroCalendar:
                    IntPtr hWnd = (PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource).Handle;
                    ClockFlyoutLauncher.ShowAeroClockFlyout(hWnd);
                    break;
                case ClockClickOption.OpenNotificationCenter:
                    if (EnvironmentHelper.IsWindows10RS4OrBetter)
                    {
                        ImmersiveShellHelper.ShowActionCenter();
                    }
                    else
                    {
                        ShellHelper.ShowActionCenter();
                    }
                    break;
            }
        }

        private void Clock_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShellHelper.StartProcess("timedate.cpl");

            e.Handled = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                Initialize();

                _isLoaded = true;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopClock();

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            SystemEvents.TimeChanged -= TimeChanged;
            SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;

            _isLoaded = false;
        }
    }
}