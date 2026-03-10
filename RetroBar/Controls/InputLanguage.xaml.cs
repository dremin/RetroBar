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
using WinForms = System.Windows.Forms;

namespace RetroBar.Controls
{
    public static class InputMethodSwitcher
    {
        public static void Toggle()
        {
            var list = WinForms.InputLanguage.InstalledInputLanguages.Cast<WinForms.InputLanguage>().ToList();
            if (list.Count < 2)
                return;

            var current = WinForms.InputLanguage.CurrentInputLanguage;
            int index = list.IndexOf(current);
            int next = (index + 1) % list.Count;

            WinForms.InputLanguage.CurrentInputLanguage = list[next];
        }
    }
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
                    InputLanguageText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    InputLanguageText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                JapaneseImeRemove();
                InputLanguageText.Visibility = Visibility.Visible;
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

        private void ShowLanguageMenu()
        {
            var menu = new ContextMenu();

            foreach (WinForms.InputLanguage lang in WinForms.InputLanguage.InstalledInputLanguages)
            {
                string code = lang.Culture.TwoLetterISOLanguageName.ToUpperInvariant();

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 0)
                };

                var badge = new Border
                {
                    Background = (Brush)Application.Current.Resources["InputLanguageBackground"],
                    Padding = new Thickness(1, 1, 2, 3),
                    Margin = new Thickness(0, 0, 6, 0),
                    Child = new TextBlock
                    {
                        Text = code,
                        FontSize = 10,
                        FontWeight = FontWeights.UltraLight,
                        Foreground = (Brush)Application.Current.Resources["InputLanguageSelectorForeground"]
                    }
                };

                var text = new TextBlock
                {
                    Text = lang.Culture.DisplayName
                };

                panel.Children.Add(badge);
                panel.Children.Add(text);

                var item = new MenuItem
                {
                    Header = panel,
                    Tag = lang,
                    IsCheckable = true,
                    IsChecked = lang.Equals(WinForms.InputLanguage.CurrentInputLanguage)
                };

                item.Click += (s, e) =>
                {
                    WinForms.InputLanguage.CurrentInputLanguage = (WinForms.InputLanguage)((MenuItem)s).Tag;
                };

                menu.Items.Add(item);
            }

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        private void UserControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowLanguageMenu();//InputMethodSwitcher.Toggle();
        }
    };
}