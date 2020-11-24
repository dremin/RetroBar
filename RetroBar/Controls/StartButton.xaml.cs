using System.Windows;
using System.Windows.Controls;
using ManagedShell.Common.Helpers;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for StartButton.xaml
    /// </summary>
    public partial class StartButton : UserControl
    {
        public StartButton()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Shell.ShowStartMenu();
        }

        private void Explore_OnClick(object sender, RoutedEventArgs e)
        {
            Shell.StartProcess("explorer.exe");
        }
    }
}
