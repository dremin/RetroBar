using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ManagedShell.WindowsTray;
using RetroBar.Extensions;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyIconList.xaml
    /// </summary>
    public partial class NotifyIconList : UserControl
    {
        private bool _isLoaded;
        private readonly ObservableCollection<ManagedShell.WindowsTray.NotifyIcon> allNotifyIcons = new ObservableCollection<ManagedShell.WindowsTray.NotifyIcon>();
        private readonly ObservableCollection<ManagedShell.WindowsTray.NotifyIcon> pinnedNotifyIcons = new ObservableCollection<ManagedShell.WindowsTray.NotifyIcon>();
        private ObservableCollection<ManagedShell.WindowsTray.NotifyIcon> promotedIcons = new ObservableCollection<ManagedShell.WindowsTray.NotifyIcon>();
        private NotifyIconDropHandler dropHandler;

        public static DependencyProperty NotificationAreaProperty = DependencyProperty.Register(nameof(NotificationArea), typeof(NotificationArea), typeof(NotifyIconList), new PropertyMetadata(NotificationAreaChangedCallback));

        public NotificationArea NotificationArea
        {
            get { return (NotificationArea)GetValue(NotificationAreaProperty); }
            set { SetValue(NotificationAreaProperty, value); }
        }

        public NotifyIconList()
        {
            InitializeComponent();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.CollapseNotifyIcons))
            {
                if (Settings.Instance.CollapseNotifyIcons)
                {
                    NotifyIcons.ItemsSource = pinnedNotifyIcons;
                    SetToggleVisibility();
                }
                else
                {
                    NotifyIconToggleButton.IsChecked = false;
                    NotifyIconToggleButton.Visibility = Visibility.Collapsed;
                    NotifyIcons.ItemsSource = allNotifyIcons;
                }
            }
            else if (e.PropertyName == nameof(Settings.InvertIconsMode) || e.PropertyName == nameof(Settings.InvertNotifyIcons))
            {
                // Reload icons
                NotifyIcons.ItemsSource = null;

                if (Settings.Instance.CollapseNotifyIcons && NotifyIconToggleButton.IsChecked != true)
                {
                    NotifyIcons.ItemsSource = pinnedNotifyIcons;
                }
                else
                {
                    NotifyIcons.ItemsSource = allNotifyIcons;
                }
            }
        }

        private void SetNotificationAreaCollections()
        {
            if (!_isLoaded && NotificationArea != null)
            {
                RefreshCollections(true);

                NotificationArea.UnpinnedIcons.Filter = UnpinnedNotifyIcons_Filter;

                NotificationArea.UnpinnedIcons.CollectionChanged += UnpinnedIcons_CollectionChanged;
                NotificationArea.PinnedIcons.CollectionChanged += PinnedIcons_CollectionChanged;
                promotedIcons.CollectionChanged += PromotedIcons_CollectionChanged;
                NotificationArea.NotificationBalloonShown += NotificationArea_NotificationBalloonShown;

                Settings.Instance.PropertyChanged += Settings_PropertyChanged;

                if (Settings.Instance.CollapseNotifyIcons)
                {
                    NotifyIcons.ItemsSource = pinnedNotifyIcons;
                    SetToggleVisibility();

                    if (NotifyIconToggleButton.IsChecked == true)
                    {
                        NotifyIconToggleButton.IsChecked = false;
                    }
                }
                else
                {
                    NotifyIcons.ItemsSource = allNotifyIcons;
                }

                _isLoaded = true;
            }
        }

        private static void NotificationAreaChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is NotifyIconList notifyIconList && e.OldValue == null && e.NewValue != null)
            {
                notifyIconList.SetNotificationAreaCollections();
            }
        }

        private bool UnpinnedNotifyIcons_Filter(object obj)
        {
            if (obj is ManagedShell.WindowsTray.NotifyIcon notifyIcon)
            {
                return !notifyIcon.IsPinned && !notifyIcon.IsHidden && notifyIcon.GetBehavior() != NotifyIconBehavior.Remove;
            }

            return true;
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

            if (notifyIcon.GetBehavior() != NotifyIconBehavior.HideWhenInactive)
            {
                // Do not promote icons that are always hidden
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
                Interval = TimeSpan.FromMilliseconds(e.Balloon.Timeout + 500) // Keep it around for a few ms for the animation to complete
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

        private void NotifyIconList_Loaded(object sender, RoutedEventArgs e)
        {
            SetNotificationAreaCollections();

            // Set up drag/drop handler
            if (dropHandler == null)
            {
                dropHandler = new NotifyIconDropHandler(this);
                GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(NotifyIcons, dropHandler);
            }
        }

        private void NotifyIconList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            if (NotificationArea != null)
            {
                NotificationArea.UnpinnedIcons.CollectionChanged -= UnpinnedIcons_CollectionChanged;
                NotificationArea.PinnedIcons.CollectionChanged -= PinnedIcons_CollectionChanged;
                promotedIcons.CollectionChanged -= PromotedIcons_CollectionChanged;
                NotificationArea.NotificationBalloonShown -= NotificationArea_NotificationBalloonShown;
            }

            _isLoaded = false;
        }

        private void UnpinnedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetToggleVisibility();
            RefreshCollections(false);
        }

        private void PinnedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshCollections(true);
        }

        private void PromotedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ManagedShell.WindowsTray.NotifyIcon item in e.NewItems)
                {
                    pinnedNotifyIcons.Insert(0, item);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ManagedShell.WindowsTray.NotifyIcon item in e.OldItems)
                {
                    pinnedNotifyIcons.Remove(item);
                }
            }
        }

        public void RefreshCollections(bool updatePinned)
        {
            if (NotificationArea == null) return;

            if (updatePinned)
            {
                // Refresh pinned icons collection
                pinnedNotifyIcons.Clear();
                foreach (var icon in promotedIcons)
                {
                    pinnedNotifyIcons.Add(icon);
                }
                foreach (var icon in NotificationArea.PinnedIcons.Cast<ManagedShell.WindowsTray.NotifyIcon>().OrderBy(i => Settings.Instance.NotifyIconOrder?.IndexOf(i.GetInvertIdentifier()) ?? -1))
                {
                    pinnedNotifyIcons.Add(icon);
                }
            }
            // Create a list of all icons
            var icons = NotificationArea.UnpinnedIcons.Cast<ManagedShell.WindowsTray.NotifyIcon>()
                .Union(NotificationArea.PinnedIcons.Cast<ManagedShell.WindowsTray.NotifyIcon>())
                .ToList();

            // Sort icons according to saved order
            var sortedIcons = icons.OrderBy(i => Settings.Instance.NotifyIconOrder?.IndexOf(i.GetInvertIdentifier()) ?? -1).ToList();

            // Refresh all icons collection
            allNotifyIcons.Clear();
            foreach (var icon in sortedIcons)
            {
                allNotifyIcons.Add(icon);
            }
        }

        private void NotifyIconToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (NotifyIconToggleButton.IsChecked == true)
            {
                NotifyIcons.ItemsSource = allNotifyIcons;
            }
            else
            {
                NotifyIcons.ItemsSource = pinnedNotifyIcons;
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

        public void SaveIconOrder()
        {
            if (NotificationArea == null) return;

            var visibleIcons = new List<ManagedShell.WindowsTray.NotifyIcon>();

            if (NotifyIcons.ItemsSource != null)
            {
                foreach (var item in NotifyIcons.ItemsSource)
                {
                    if (item is ManagedShell.WindowsTray.NotifyIcon icon)
                    {
                        if (NotifyIcons.ItemsSource == pinnedNotifyIcons && !NotificationArea.PinnedIcons.Contains(icon))
                        {
                            continue; // skip promoted temporary icons
                        }
                        visibleIcons.Add(icon);
                    }
                }
            }

            var oldOrder = Settings.Instance.NotifyIconOrder ?? new List<string>();
            var newOrder = visibleIcons.Select(i => i.GetInvertIdentifier()).ToList();
            var orderSet = new HashSet<string>(newOrder);

            var result = new List<string>();
            int replaceIndex = 0;
            
            foreach (var id in oldOrder)
            {
                if (orderSet.Contains(id))
                {
                    if (replaceIndex < newOrder.Count)
                    {
                        result.Add(newOrder[replaceIndex++]);
                    }
                }
                else
                {
                    result.Add(id);
                }
            }

            while (replaceIndex < newOrder.Count)
            {
                result.Add(newOrder[replaceIndex++]);
            }

            Settings.Instance.NotifyIconOrder = result;
        }
    }
}