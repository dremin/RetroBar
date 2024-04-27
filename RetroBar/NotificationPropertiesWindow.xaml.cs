using ManagedShell.WindowsTray;
using RetroBar.Extensions;
using RetroBar.Utilities;
using System.Windows;
using System.Windows.Controls;

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
            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InvertNotifyIcons")
            {
                // Reload icons
                DataContext = null;
                DataContext = _notificationArea;
            }
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
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            _instance = null;
        }

        private void BehaviorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Instance.PinnedNotifyIcons = _notificationArea.PinnedNotifyIcons;
        }

        private void InvertCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var icon = checkBox.DataContext as NotifyIcon;
            icon.SetCanInvert(true);
        }

        private void InvertCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var icon = checkBox.DataContext as NotifyIcon;
            icon.SetCanInvert(false);
        }

        private void Icon_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            var notifyIcon = image.DataContext as NotifyIcon;
            applyEffects(image, notifyIcon);
        }

        private void applyEffects(Image NotifyIconImage, NotifyIcon TrayIcon)
        {
            if (TrayIcon == null)
            {
                return;
            }

            if (!TrayIcon.CanInvert())
            {
                NotifyIconImage.Effect = null;
                return;
            }

            if (NotifyIconImage.Effect == null)
            {
                NotifyIconImage.Effect = new InvertEffect();
            }
        }
    }
}
