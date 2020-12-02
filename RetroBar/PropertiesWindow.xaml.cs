using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
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
            Top = AppBarHelper.PrimaryMonitorDeviceSize.Height - Height - 40;
        }

        private void LoadThemes()
        {
            cboThemeSelect.Items.Add("Default");

            foreach (string subStr in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\Themes").Where(s => Path.GetExtension(s).Contains("xaml")))
            {
                string theme = Path.GetFileName(subStr);
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
