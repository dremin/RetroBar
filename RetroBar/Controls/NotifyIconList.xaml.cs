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
        private NotificationArea _notificationArea;

        public NotifyIconList()
        {
            InitializeComponent();
        }

        public void SetNotificationArea(NotificationArea notificationArea)
        {
            _notificationArea = notificationArea;
        }

        private void NotifyIconList_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                _notificationArea.Initialize();
                UnpinnedNotifyIconList.ItemsSource = _notificationArea.UnpinnedIcons;
                PinnedNotifyIconList.ItemsSource = _notificationArea.PinnedIcons;

                isLoaded = true;
            }
        }

        private void NotifyIconList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
