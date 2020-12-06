using System.ComponentModel;
using System.Windows;
using ManagedShell.Common.Helpers;
using RetroBar.Utilities;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        private static PropertiesWindow _instance;
        public static PropertiesWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PropertiesWindow();
                }

                return _instance;
            }
        }

        public PropertiesWindow()
        {
            InitializeComponent();

            LoadThemes();

            Left = 10;
            Top = (AppBarHelper.PrimaryMonitorDeviceSize.Height / Shell.DpiScale) - Height - 40;
        }

        private void LoadThemes()
        {
            foreach (var theme in ThemeManager.Instance.GetThemes())
            {
                cboThemeSelect.Items.Add(theme);
            }
        }

        private void PropertiesWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _instance = null;
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
