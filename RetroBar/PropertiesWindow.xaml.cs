using ManagedShell.Common.Helpers;
using RetroBar.Utilities;
using System.Windows;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        private readonly ThemeManager _themeManager;

        public PropertiesWindow(ThemeManager themeManager)
        {
            _themeManager = themeManager;

            InitializeComponent();

            LoadThemes();

            Left = 10;
            Top = (ScreenHelper.PrimaryMonitorDeviceSize.Height / Shell.DpiScale) - Height - 40;
        }

        private void LoadThemes()
        {
            foreach (var theme in _themeManager.GetThemes())
            {
                cboThemeSelect.Items.Add(theme);
            }
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
