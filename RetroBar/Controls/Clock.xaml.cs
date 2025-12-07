using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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
        public static readonly DependencyProperty NowProperty = DependencyProperty.Register(nameof(Now), typeof(DateTime), typeof(Clock));

        public DateTime Now
        {
            get => (DateTime)GetValue(NowProperty);
            set => SetValue(NowProperty, value);
        }

        private readonly DispatcherTimer _clock = new DispatcherTimer(DispatcherPriority.Background);
        private bool _isLoaded;

        private const int LOCALE_NAME_MAX_LENGTH = 85;

        public Clock()
        {
            InitializeComponent();
            DataContext = this;

            _clock.Interval = TimeSpan.FromMilliseconds(200);
            _clock.Tick += Clock_Tick;
        }

        private void Initialize()
        {
            UpdateClockTemplate();

            // Delay UpdateUserCulture slightly to ensure visual tree is fully loaded
            // This is ONLY needed at initialization - later updates work immediately
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateUserCulture();
            }), DispatcherPriority.Loaded);

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

        private void UpdateClockTemplate()
        {
            if (Settings.Instance.ShowClockDate)
            {
                // Use our custom two-row template with date
                Template = FindResource("ClockWithDateTemplate") as ControlTemplate;
            }
            else
            {
                // Use the theme's default template
                Template = TryFindResource("ClockTemplateKey") as ControlTemplate;
            }
        }

        private void StartClock()
        {
            SetTime();

            _clock.Start();

            Visibility = Visibility.Visible;
        }

        private void StopClock()
        {
            _clock.Stop();

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
            else if (e.PropertyName == nameof(Settings.ShowClockSeconds) ||
                     e.PropertyName == nameof(Settings.ClockTimeFormat) ||
                     e.PropertyName == nameof(Settings.ClockDateFormat))
            {
                UpdateUserCulture();
            }
            else if (e.PropertyName == nameof(Settings.ShowClockDate))
            {
                UpdateClockTemplate();
                // Delay culture update to ensure new template is fully applied
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateUserCulture();
                }), DispatcherPriority.Loaded);
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

        private static void SetConverterCultureRecursively(DependencyObject main, CultureInfo ci)
        {
            if (main != null)
            {
                var binding = BindingOperations.GetBinding(main, TextBlock.TextProperty);

                if (binding != null)
                {
                    // Clear the old binding completely
                    BindingOperations.ClearBinding(main, TextBlock.TextProperty);

                    // Create a completely new binding with the new culture
                    BindingOperations.SetBinding(main, TextBlock.TextProperty,
                        new Binding(binding.Path.Path)
                            { StringFormat = binding.StringFormat, ConverterCulture = ci });
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(main); i++)
                {
                    if (VisualTreeHelper.GetChild(main, i) is UIElement sub)
                    {
                        SetConverterCultureRecursively(sub, ci);
                    }
                }
            }
        }

        private void UpdateUserCulture()
        {
            // Get system culture for month/day names and AM/PM indicators, but we'll override patterns
            CultureInfo userCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

            // OVERRIDE time pattern - Settings are the ONLY source of truth for format
            if (Settings.Instance.ClockTimeFormat == ClockTimeFormat.Hour12)
            {
                // 12-hour format
                userCulture.DateTimeFormat.ShortTimePattern = Settings.Instance.ShowClockSeconds ? "h:mm:ss tt" : "h:mm tt";
                userCulture.DateTimeFormat.LongTimePattern = Settings.Instance.ShowClockSeconds ? "h:mm:ss tt" : "h:mm tt";
            }
            else
            {
                // 24-hour format
                userCulture.DateTimeFormat.ShortTimePattern = Settings.Instance.ShowClockSeconds ? "HH:mm:ss" : "HH:mm";
                userCulture.DateTimeFormat.LongTimePattern = Settings.Instance.ShowClockSeconds ? "HH:mm:ss" : "HH:mm";
            }

            // OVERRIDE date pattern - Settings are the ONLY source of truth for format
            if (Settings.Instance.ClockDateFormat == ClockDateFormat.DayMonthYear)
            {
                userCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                userCulture.DateTimeFormat.LongDatePattern = "dddd, dd MMMM yyyy";
            }
            else
            {
                userCulture.DateTimeFormat.ShortDatePattern = "MM/dd/yyyy";
                userCulture.DateTimeFormat.LongDatePattern = "dddd, MMMM dd, yyyy";
            }

            // Apply culture to bindings recursively
            SetConverterCultureRecursively(this, userCulture);
            if (ClockTip != null)
            {
                SetConverterCultureRecursively(ClockTip, userCulture);
            }

            // Force a refresh of the current time to apply the new culture immediately
            SetTime();
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Locale)
            {
                CultureInfo.CurrentCulture.ClearCachedData();
                UpdateUserCulture();
            }
        }

        private void SetTime()
        {
            Now = DateTime.Now;
        }

        private static string GetUserDefaultLocaleNameWrapper()
        {
            var sb = new StringBuilder(LOCALE_NAME_MAX_LENGTH);
            int ret = GetUserDefaultLocaleName(sb, sb.Capacity);
            return ret > 0 ? sb.ToString() : null;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetUserDefaultLocaleName(StringBuilder lpLocaleName, int cchLocaleName);

        [DllImport("kernel32.dll")]
        private static extern int GetUserDefaultLCID();

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