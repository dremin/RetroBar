using GongSolutions.Wpf.DragDrop;
using ManagedShell.WindowsTray;
using Tray = ManagedShell.WindowsTray;
using RetroBar.Extensions;
using RetroBar.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyIconList.xaml
    /// </summary>
    public partial class NotifyIconList : UserControl
    {
        private bool _isLoaded;
        private ObservableCollection<Tray.NotifyIcon> promotedIcons = new ObservableCollection<Tray.NotifyIcon>();
        private NotifyIconDropHandler dropHandler;
        private ListCollectionView collectionView;

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

        private bool IsCollapsed()
        {
            return Settings.Instance.CollapseNotifyIcons && NotifyIconToggleButton.IsChecked != true;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.CollapseNotifyIcons))
            {
                if (Settings.Instance.CollapseNotifyIcons)
                {
                    SetToggleVisibility();
                }
                else
                {
                    NotifyIconToggleButton.IsChecked = false;
                    NotifyIconToggleButton.Visibility = Visibility.Collapsed;
                }
                collectionView?.Refresh();
            }
            else if (e.PropertyName == nameof(Settings.InvertIconsMode) || e.PropertyName == nameof(Settings.InvertNotifyIcons) || e.PropertyName == nameof(Settings.NotifyIconOrder))
            {
                // Reload icons
                collectionView?.Refresh();
            }
        }

        private void SetNotificationAreaCollections()
        {
            if (!_isLoaded && NotificationArea != null)
            {
                promotedIcons.CollectionChanged += PromotedIcons_CollectionChanged;
                NotificationArea.NotificationBalloonShown += NotificationArea_NotificationBalloonShown;
                NotificationArea.UnpinnedIcons.CollectionChanged += UnpinnedIcons_CollectionChanged;
                NotificationArea.UnpinnedIcons.Filter = UnpinnedNotifyIcons_Filter;
                Settings.Instance.PropertyChanged += Settings_PropertyChanged;

                collectionView = new ListCollectionView(NotificationArea.TrayIcons);
                collectionView.CustomSort = new NotifyIconComparer(this);
                collectionView.Filter = NotifyIcons_Filter;
                var collectionViewShaping = collectionView as ICollectionViewLiveShaping;
                collectionViewShaping.IsLiveFiltering = true;
                collectionViewShaping.LiveFilteringProperties.Add("IsHidden");
                collectionViewShaping.LiveFilteringProperties.Add("IsPinned");
                NotifyIcons.ItemsSource = collectionView;

                if (Settings.Instance.CollapseNotifyIcons)
                {
                    SetToggleVisibility();
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

        private bool NotifyIcons_Filter(object icon)
        {
            if (icon is Tray.NotifyIcon notifyIcon)
            {
                return (!IsCollapsed() || notifyIcon.IsPinned || promotedIcons.Contains(notifyIcon))
                    && !notifyIcon.IsHidden
                    && notifyIcon.GetBehavior() != NotifyIconBehavior.Remove;
            }
            return false;
        }

        private bool UnpinnedNotifyIcons_Filter(object obj)
        {
            // This filter is used when we check if the toggle should hide
            if (obj is Tray.NotifyIcon notifyIcon)
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

            Tray.NotifyIcon notifyIcon = e.Balloon.NotifyIcon;

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

            if (NotificationArea != null)
            {
                promotedIcons.CollectionChanged -= PromotedIcons_CollectionChanged;
                NotificationArea.NotificationBalloonShown -= NotificationArea_NotificationBalloonShown;
                NotificationArea.UnpinnedIcons.CollectionChanged -= UnpinnedIcons_CollectionChanged;
                NotificationArea.UnpinnedIcons.Filter = UnpinnedNotifyIcons_Filter;
                Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            }

            _isLoaded = false;
        }

        private void UnpinnedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetToggleVisibility();
        }

        private void PromotedIcons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsCollapsed())
            {
                collectionView?.Refresh();
            }
        }

        private void NotifyIconToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            collectionView?.Refresh();
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

        public void UpdateIconOrder(IDropInfo dropInfo)
        {
            if (NotificationArea == null || collectionView == null) return;

            var visibleIcons = collectionView.Cast<Tray.NotifyIcon>().ToList();

            if (IsCollapsed())
            {
                // Do not save temporary promoted icons
                visibleIcons = visibleIcons.Where(i => !promotedIcons.Contains(i)).ToList();
            }

            // Update the dragged icon's position in the list
            if (dropInfo.Data is Tray.NotifyIcon draggedIcon)
            {
                int insertIndex = dropInfo.InsertIndex;
                if (insertIndex > 0 && visibleIcons.IndexOf(draggedIcon) < insertIndex && visibleIcons.Remove(draggedIcon))
                {
                    insertIndex--;
                }
                visibleIcons.Insert(insertIndex, draggedIcon);
            }
            else
            {
                return;
            }
            
            // Never overwrite the list to prevent clearing out settings for non-visible icons
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

        public class NotifyIconComparer : System.Collections.IComparer
        {
            private NotifyIconList _host;

            public NotifyIconComparer(NotifyIconList host)
            {
                _host = host;
            }

            public int Compare(object x, object y)
            {
                if (x is Tray.NotifyIcon xIcon && y is Tray.NotifyIcon yIcon && Settings.Instance.NotifyIconOrder is List<string> setting)
                {
                    if (_host.IsCollapsed())
                    {
                        bool xPromoted = _host.promotedIcons.Contains(xIcon);
                        bool yPromoted = _host.promotedIcons.Contains(yIcon);
                        if (xPromoted && !yPromoted)
                        {
                            return -1;
                        }
                        if (!xPromoted && yPromoted)
                        {
                            return 1;
                        }
                    }
                    return setting.IndexOf(xIcon.GetInvertIdentifier()).CompareTo(setting.IndexOf(yIcon.GetInvertIdentifier()));
                }
                return 0;
            }
        }
    }
}