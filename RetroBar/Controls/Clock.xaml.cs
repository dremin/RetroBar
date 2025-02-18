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
            UpdateUserCulture();
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
            else if (e.PropertyName == nameof(Settings.ShowClockSeconds))
            {
                UpdateUserCulture();
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
            CultureInfo userCulture;

            try
            {
                string localeName = GetUserDefaultLocaleNameWrapper();

                if (!string.IsNullOrEmpty(localeName))
                {
                    userCulture = new CultureInfo(localeName);
                }
                else
                {
                    // Fallback: use LCID if locale name isn't available.
                    int lcid = GetUserDefaultLCID();
                    userCulture = new CultureInfo(lcid);
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"Clock: Unable to get the user culture: {e.Message}, defaulting to current culture");
                userCulture = CultureInfo.CurrentCulture;
            }

            if (userCulture.IsReadOnly)
            {
                userCulture = (CultureInfo)userCulture.Clone();
            }

            if (Settings.Instance.ShowClockSeconds)
            {
                userCulture.DateTimeFormat.ShortTimePattern = userCulture.DateTimeFormat.LongTimePattern;
            }

            SetConverterCultureRecursively(this, userCulture);
            SetConverterCultureRecursively(ClockTip, userCulture);
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