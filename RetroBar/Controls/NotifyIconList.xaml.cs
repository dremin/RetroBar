using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
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
        private CollectionViewSource allNotifyIconsSource;
        private CollectionViewSource pinnedNotifyIconsSource;
        private ObservableCollection<ManagedShell.WindowsTray.NotifyIcon> promotedIcons = new ObservableCollection<ManagedShell.WindowsTray.NotifyIcon>();

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
                    NotifyIcons.ItemsSource = pinnedNotifyIconsSource.View;
                    SetToggleVisibility();
                }
                else
                {
                    NotifyIconToggleButton.IsChecked = false;
                    NotifyIconToggleButton.Visibility = Visibility.Collapsed;

                    NotifyIcons.ItemsSource = allNotifyIconsSource.View;
                }
            }
        }

        private void NotifyIconList_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded && NotificationArea != null)
            {
                CompositeCollection allNotifyIcons = new CompositeCollection();
                allNotifyIcons.Add(new CollectionContainer { Collection = NotificationArea.UnpinnedIcons });
                allNotifyIcons.Add(new CollectionContainer { Collection = NotificationArea.PinnedIcons });
                allNotifyIconsSource = new CollectionViewSource { Source = allNotifyIcons };

                CompositeCollection pinnedNotifyIcons = new CompositeCollection();
                pinnedNotifyIcons.Add(new CollectionContainer { Collection = promotedIcons });
                pinnedNotifyIcons.Add(new CollectionContainer { Collection = NotificationArea.PinnedIcons });
                pinnedNotifyIconsSource = new CollectionViewSource { Source = pinnedNotifyIcons };

                NotificationArea.UnpinnedIcons.CollectionChanged += UnpinnedIcons_CollectionChanged;
                NotificationArea.NotificationBalloonShown += NotificationArea_NotificationBalloonShown;

                if (Settings.Instance.CollapseNotifyIcons)
                {
                    NotifyIcons.ItemsSource = pinnedNotifyIconsSource.View;
                    SetToggleVisibility();

                    if (NotifyIconToggleButton.IsChecked == true)
                    {
                        NotifyIconToggleButton.IsChecked = false;
                    }
                }
                else
                {
                    NotifyIcons.ItemsSource = allNotifyIconsSource.View;
                }

                isLoaded = true;
            }
        }

        private void NotificationArea_NotificationBalloonShown(object sender, NotificationBalloonEventArgs e)
        {
            // This is used to promote unpinned icons to show when the tray is collapsed.

            if (NotificationArea == null)
            {
                return;
            }

            ManagedShell.WindowsTray.NotifyIcon notifyIcon = e.Balloon.NotifyIcon;

            if (NotificationArea.PinnedIcons.Contains(notifyIcon))
            {
                // Do not promote pinned icons (they're already there!)
                return;
            }

            if (promotedIcons.Contains(notifyIcon))
            {
                // Do not duplicate promoted icons
                return;
            }

            promotedIcons.Add(notifyIcon);

            DispatcherTimer unpromoteTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(e.Balloon.Timeout + 250) // Keep it around for a few ms for the animation to complete
            };
            unpromoteTimer.Tick += (object sender, EventArgs e) =>
            {
                if (promotedIcons.Contains(notifyIcon))
                {
                    promotedIcons.Remove(notifyIcon);
                }
                unpromoteTimer.Stop();
            };
            unpromoteTimer.Start();
        }

        private void NotifyIconList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (NotificationArea != null)
            {
                NotificationArea.UnpinnedIcons.CollectionChanged -= UnpinnedIcons_CollectionChanged;
                NotificationArea.NotificationBalloonShown -= NotificationArea_NotificationBalloonShown;
            }

            isLoaded = false;
        }

        private void UnpinnedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetToggleVisibility();
        }

        private void NotifyIconToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (NotifyIconToggleButton.IsChecked == true)
            {

                NotifyIcons.ItemsSource = allNotifyIconsSource.View;
            }
            else
            {
                NotifyIcons.ItemsSource = pinnedNotifyIconsSource.View;
            }
        }

        private void SetToggleVisibility()
        {
            if (!Settings.Instance.CollapseNotifyIcons) return;

            if (NotificationArea.UnpinnedIcons.IsEmpty)
            {
                NotifyIconToggleButton.Visibility = Visibility.Collapsed;

                if (NotifyIconToggleButton.IsChecked == true)
                {
                    NotifyIconToggleButton.IsChecked = false;
                }
            }
            else
            {
                NotifyIconToggleButton.Visibility = Visibility.Visible;
            }
        }
    }
}
