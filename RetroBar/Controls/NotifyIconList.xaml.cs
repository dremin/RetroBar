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

        public static DependencyProperty NotificationAreaProperty = DependencyProperty.Register("NotificationArea", typeof(NotificationArea), typeof(NotifyIconList));

        public NotificationArea NotificationArea
        {
            get { return (NotificationArea)GetValue(NotificationAreaProperty); }
            set { SetValue(NotificationAreaProperty, value); }
        }

        public NotifyIconList()
        {
            InitializeComponent();
        }

        private void NotifyIconList_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded && NotificationArea != null)
            {
                NotificationArea.Initialize();
                UnpinnedNotifyIconList.ItemsSource = NotificationArea.UnpinnedIcons;
                PinnedNotifyIconList.ItemsSource = NotificationArea.PinnedIcons;

                isLoaded = true;
            }
        }
    }
}
