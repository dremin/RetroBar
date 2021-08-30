using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ManagedShell.WindowsTray;
using RetroBar.Utilities;

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

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CollapseNotifyIcons")
            {
                if (Settings.Instance.CollapseNotifyIcons)
                {
                    UnpinnedNotifyIconList.Visibility = Visibility.Collapsed;
                    SetToggleVisibility();
                }
                else
                {
                    NotifyIconToggleButton.IsChecked = false;
                    NotifyIconToggleButton.Visibility = Visibility.Collapsed;
                    UnpinnedNotifyIconList.Visibility = Visibility.Visible;
                }
            }
        }

        private void NotifyIconList_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded && NotificationArea != null)
            {
                UnpinnedNotifyIconList.ItemsSource = NotificationArea.UnpinnedIcons;
                PinnedNotifyIconList.ItemsSource = NotificationArea.PinnedIcons;

                NotificationArea.UnpinnedIcons.CollectionChanged += UnpinnedIcons_CollectionChanged;

                if (Settings.Instance.CollapseNotifyIcons)
                {
                    SetToggleVisibility();
                }
                else
                {
                    UnpinnedNotifyIconList.Visibility = Visibility.Visible;
                }

                isLoaded = true;
            }
        }

        private void UnpinnedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetToggleVisibility();

            if (UnpinnedNotifyIconList.Items.Count < 1 && NotifyIconToggleButton.IsChecked == true)
            {
                NotifyIconToggleButton.IsChecked = false;
            }
        }

        private void NotifyIconToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            UnpinnedNotifyIconList.Visibility =
                NotifyIconToggleButton.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            NotifyIconToggleButton.ToolTip = NotifyIconToggleButton.IsChecked == true ? "Cacher" : "Afficher les icônes masquées";
        }

        private void SetToggleVisibility()
        {
            if (!Settings.Instance.CollapseNotifyIcons) return;

            NotifyIconToggleButton.Visibility =
                UnpinnedNotifyIconList.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
