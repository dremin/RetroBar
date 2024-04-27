using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    public partial class InputLanguage : UserControl
    {
        public static DependencyProperty LocaleIdentifierProperty = DependencyProperty.Register("LocaleIdentifierProperty", typeof(CultureInfo), typeof(InputLanguage));

        public CultureInfo LocaleIdentifier
        {
            get { return (CultureInfo)GetValue(LocaleIdentifierProperty); }
            set { SetValue(LocaleIdentifierProperty, value); }
        }

        private readonly DispatcherTimer layoutWatch = new DispatcherTimer(DispatcherPriority.Background);

        private bool _isLoaded;

        public InputLanguage()
        {
            InitializeComponent();
            DataContext = this;

            layoutWatch.Interval = TimeSpan.FromMilliseconds(200);
            layoutWatch.Tick += LayoutWatchTick;
        }

        private void Initialize()
        {
            if (Settings.Instance.ShowInputLanguage)
            {
                StartWatch();
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void SetLocaleIdentifier()
        {
            try
            {
                var managedShellLayout = KeyboardLayoutHelper.GetKeyboardLayout(false);
                LocaleIdentifier = CultureInfo.GetCultureInfo((short)managedShellLayout.HKL);
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"Error getting locale identifier: {ex.Message}");
            }
        }

        private void StartWatch()
        {
            SetLocaleIdentifier();
            
            layoutWatch.Start();

            Visibility = Visibility.Visible;
        }

        private void LayoutWatchTick(object sender, EventArgs args)
        {
            SetLocaleIdentifier();
        }

        private void StopWatch()
        {
            layoutWatch.Stop();

            Visibility = Visibility.Collapsed;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.ShowInputLanguage))
            {
                if (Settings.Instance.ShowInputLanguage)
                {
                    StartWatch();
                }
                else
                {
                    StopWatch();
                }
            }
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
            StopWatch();
            
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            _isLoaded = false;
        }
    };
}