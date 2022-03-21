using RetroBar.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for ToolbarButton.xaml
    /// </summary>
    public partial class ToolbarButton : UserControl
    {
        private bool _isLoaded;

        public ToolbarButton()
        {
            InitializeComponent();

            setIconBinding();
        }

        private void setIconBinding()
        {
            string bindingPath = "SmallIcon";
            bool useLargeIcons = Application.Current.FindResource("UseLargeIcons") as bool? ?? false;

            if (useLargeIcons)
            {
                bindingPath = "LargeIcon";
            }

            Binding iconBinding = new Binding(bindingPath);
            iconBinding.Mode = BindingMode.OneWay;
            ToolbarIcon.SetBinding(Image.SourceProperty, iconBinding);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
            _isLoaded = true;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                setIconBinding();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            _isLoaded = false;
        }
    }
}
