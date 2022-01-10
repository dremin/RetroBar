using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System.Windows;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for NotificationPropertiesWindow.xaml
    /// </summary>
    public partial class NotificationPropertiesWindow : Window
    {
        private static NotificationPropertiesWindow _instance;

        private readonly NotificationArea _notificationArea;

        private NotificationPropertiesWindow(NotificationArea notificationArea)
        {
            _notificationArea = notificationArea;

            InitializeComponent();

            DataContext = _notificationArea;
        }

        public static void Open(NotificationArea notificationArea, Point position)
        {
            if (_instance == null)
            {
                _instance = new NotificationPropertiesWindow(notificationArea);
                _instance.Left = position.X + 10;
                _instance.Top = position.Y + 10;
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;
        }

        private void BehaviorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Settings.Instance.PinnedNotifyIcons = _notificationArea.PinnedNotifyIcons;
        }
    }
}
