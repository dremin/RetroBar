using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using RetroBar.Utilities;
namespace RetroBar.Controls
{
    public partial class KeyboardLayout : UserControl
    {
        #region DllImports
        [DllImport("user32.dll")] 
        private static extern IntPtr GetForegroundWindow();
    
        [DllImport("user32.dll")] 
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
    
        [DllImport("user32.dll")] 
        private static extern IntPtr GetKeyboardLayout(uint thread);
        #endregion
        
        public static DependencyProperty LocaleIdentifierProperty = DependencyProperty.Register("LocaleIdentifierProperty", typeof(CultureInfo), typeof(KeyboardLayout));

        private readonly DispatcherTimer clock = new DispatcherTimer(DispatcherPriority.Background);
        
        public CultureInfo LocaleIdentifier
        {
            get { return (CultureInfo)GetValue(LocaleIdentifierProperty); }
            set { SetValue(LocaleIdentifierProperty, value); }
        }

        private bool _isLoaded;
        
        public KeyboardLayout()
        {
            InitializeComponent();
            DataContext = this;
            
            clock.Interval = TimeSpan.FromMilliseconds(200);
            clock.Tick += Clock_Tick;
        }
        
        private void Initialize()
        {
            if (Settings.Instance.ShowKeyboardLayout)
            {
                StartClock();
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
            
            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void StartClock()
        {
            clock.Start();
            
            Visibility = Visibility.Visible;
        }
        
        private void Clock_Tick(object sender, EventArgs args)
        {
            var fWnd = GetForegroundWindow();
            var fProc = GetWindowThreadProcessId(fWnd, IntPtr.Zero);
            var layout = GetKeyboardLayout(fProc).ToInt32() & 0xFFFF;
            
            LocaleIdentifier = new CultureInfo(layout);
        }

        private void StopClock()
        {
            clock.Stop();

            Visibility = Visibility.Collapsed;
        }
        
        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowKeyboardLayout")
            {
                if (Settings.Instance.ShowKeyboardLayout)
                {
                    StartClock();
                }
                else
                {
                    StopClock();
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
            StopClock();
            
            _isLoaded = false;
        }
    };
}