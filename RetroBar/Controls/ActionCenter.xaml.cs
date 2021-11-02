using ManagedShell.Common.Helpers;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for ToolbarButton.xaml
    /// </summary>
    public partial class ActionCenter : UserControl
    {
        public ActionCenter()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShellHelper.ShowActionCenter();
        }
    }
}
