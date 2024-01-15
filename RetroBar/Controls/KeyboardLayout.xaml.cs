using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RetroBar.Utilities;
namespace RetroBar.Controls
{
    public partial class KeyboardLayout : UserControl
    {
        #region DllImports
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
        [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint thread);
        #endregion
        
        public static DependencyProperty LocaleIdentifierProperty = DependencyProperty.Register("LocaleIdentifierProperty", typeof(CultureInfo), typeof(KeyboardLayout));
        
        public CultureInfo LocaleIdentifier
        {
            get { return (CultureInfo)GetValue(LocaleIdentifierProperty); }
            set { SetValue(LocaleIdentifierProperty, value); }
        }

        private readonly DispatcherTimer layoutWatch = new DispatcherTimer(DispatcherPriority.Background);
        
        private bool _isLoaded;
        
        public KeyboardLayout()
        {
            InitializeComponent();
            DataContext = this;
            
            layoutWatch.Interval = TimeSpan.FromMilliseconds(200);
            layoutWatch.Tick += LayoutWatchTick;
        }
        
        private void Initialize()
        {
            if (Settings.Instance.ShowKeyboardLayout)
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
            var fWnd = GetForegroundWindow();
            var fProc = GetWindowThreadProcessId(fWnd, IntPtr.Zero);
            var layout = GetKeyboardLayout(fProc).ToInt32() & 0xFFFF;
            
            LocaleIdentifier = new CultureInfo(layout);
        }
        
        private void StartWatch()
        {
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
            if (e.PropertyName == "ShowKeyboardLayout")
            {
                if (Settings.Instance.ShowKeyboardLayout)
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
            
            _isLoaded = false;
        }
    };
}