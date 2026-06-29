using System;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using RetroBar.Converters;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    public partial class InputLanguage : UserControl
    {
        public static DependencyProperty LocaleIdentifierProperty = DependencyProperty.Register(nameof(LocaleIdentifier), typeof(CultureInfo), typeof(InputLanguage));

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

            UpdateTextScaling();
        }

        // The unscaled box size from the InputLanguageBox style, captured once so the scaling
        // doesn't hard-code it.
        private double _baseBoxHeight = double.NaN;
        private double _baseBoxMinWidth = double.NaN;

        private void UpdateTextScaling()
        {
            // Only scale within the actual taskbar; the properties window preview hosts this
            // same control without the taskbar's ScaleTransform, so it should stay unscaled.
            double scale = Window.GetWindow(this) is RetroBar.Taskbar ? Settings.Instance.TaskbarScale : 1.0;

            if (double.IsNaN(_baseBoxHeight))
            {
                _baseBoxHeight = InputLanguageBox.Height;
                _baseBoxMinWidth = InputLanguageBox.MinWidth;
            }

            // Render the whole indicator at real, pixel-aligned sizes with a 1/scale
            // counter-transform, so both the box and the text stay crisp (no fractional-pixel
            // blur) and the text centers in real-pixel space rather than in the taskbar's
            // fractional scaled space. The base font size comes from the current theme's
            // GlobalFontSize.
            if (scale > 1.0 && Application.Current.TryFindResource("GlobalFontSize") is double baseFontSize)
            {
                InputLanguageBox.LayoutTransform = new ScaleTransform(1.0 / scale, 1.0 / scale);
                InputLanguageBox.Height = Math.Round(_baseBoxHeight * scale);
                InputLanguageBox.MinWidth = Math.Round(_baseBoxMinWidth * scale);
                InputLanguageText.FontSize = Math.Round(baseFontSize * scale);
            }
            else
            {
                InputLanguageBox.LayoutTransform = null;
                InputLanguageBox.ClearValue(FrameworkElement.HeightProperty);
                InputLanguageBox.ClearValue(FrameworkElement.MinWidthProperty);
                InputLanguageText.ClearValue(TextBlock.FontSizeProperty);
            }
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

        private void JapaneseImeAdd()
        {
            var ChkControl = InputLanguageDockPanel.Children
                                .OfType<JapaneseIme>()
                                .FirstOrDefault();

            if (ChkControl != null)
            {
                return;
            }

            var NewControl = new JapaneseIme();
            InputLanguageDockPanel.Children.Add(NewControl);
        }
        
        private void JapaneseImeRemove()
        {
            var DelControl = InputLanguageDockPanel.Children
                                .OfType<JapaneseIme>()
                                .FirstOrDefault();

            if (DelControl != null)
            {
                InputLanguageDockPanel.Children.Remove(DelControl);
            }
        }

        private int InstalledLanguagesCount()
        {
            int LangCount = 0;

            foreach (System.Windows.Forms.InputLanguage CurLang in System.Windows.Forms.InputLanguage.InstalledInputLanguages)
            {
                LangCount++;
            }

            return LangCount;
        }

        private void LayoutWatchTick(object sender, EventArgs args)
        {
            SetLocaleIdentifier();

            var LocaleConverter = new CultureInfoToLocaleNameConverter();
            string LocaleName = (string)LocaleConverter.Convert(LocaleIdentifier, typeof(string), "TwoLetterIsoLanguageName", CultureInfo.InvariantCulture);

            if (LocaleName == "JA")
            {
                JapaneseImeAdd();

                if (InstalledLanguagesCount() == 1)
                {
                    InputLanguageBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    InputLanguageBox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                JapaneseImeRemove();
                InputLanguageBox.Visibility = Visibility.Visible;
            }
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
            else if (e.PropertyName == nameof(Settings.TaskbarScale))
            {
                UpdateTextScaling();
            }
            else if (e.PropertyName == nameof(Settings.Theme))
            {
                // The base font size can change with the theme; re-apply once it has loaded.
                Dispatcher.BeginInvoke(new Action(UpdateTextScaling), DispatcherPriority.Loaded);
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