using System.Windows;
using System.Windows.Controls;
using ManagedShell.WindowsTray;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyIconList.xaml
    /// </summary>
    public partial class NotifyIconList : UserControl
    {
        private bool isLoaded;

        public NotifyIconList()
        {
            InitializeComponent();
        }

        private void NotifyIconList_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                NotificationArea.Instance.Initialize();
                UnpinnedNotifyIconList.ItemsSource = NotificationArea.Instance.UnpinnedIcons;
                PinnedNotifyIconList.ItemsSource = NotificationArea.Instance.PinnedIcons;

                isLoaded = true;
            }
        }

        private void NotifyIconList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
