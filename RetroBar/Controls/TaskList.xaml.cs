using ManagedShell.AppBar;
using ManagedShell.WindowsTasks;
using ManagedShell.Common.Helpers;
using ManagedShell.ShellFolders;
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

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : UserControl
    {
        private bool isLoaded;
        private bool isScrollable;
        private double DefaultButtonWidth;
        private double MinButtonWidth;
        private double TaskButtonLeftMargin;
        private double TaskButtonRightMargin;
        private ICollectionView taskbarItems;
        private ObservableCollection<TaskGroup> groupedTasks = new ObservableCollection<TaskGroup>();
        private Dictionary<string, TaskGroup> groupLookup = new Dictionary<string, TaskGroup>();

        // Combined pins and programs mode
        private ShellFolder quickLaunchFolder;
        private ObservableCollection<TaskbarItem> combinedItems = new ObservableCollection<TaskbarItem>();
        private Dictionary<string, TaskbarItem> pinnedItemsLookup = new Dictionary<string, TaskbarItem>();

        public static DependencyProperty ButtonWidthProperty = DependencyProperty.Register(nameof(ButtonWidth), typeof(double), typeof(TaskList), new PropertyMetadata(new double()));

        public double ButtonWidth
        {
            get { return (double)GetValue(ButtonWidthProperty); }
            set { SetValue(ButtonWidthProperty, value); }
        }

        public static DependencyProperty TasksProperty = DependencyProperty.Register(nameof(Tasks), typeof(Tasks), typeof(TaskList), new PropertyMetadata(TasksChangedCallback));

        public Tasks Tasks
        {
            get { return (Tasks)GetValue(TasksProperty); }
            set { SetValue(TasksProperty, value); }
        }

        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(Taskbar), typeof(TaskList), new PropertyMetadata(TasksChangedCallback));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public TaskList()
        {
            InitializeComponent();
        }

        private void SetStyles()
        {
            DefaultButtonWidth = Application.Current.FindResource("TaskButtonWidth") as double? ?? 0;
            MinButtonWidth = Application.Current.FindResource("TaskButtonMinWidth") as double? ?? 0;
            Thickness buttonMargin;

            if (Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
            {
                buttonMargin = Application.Current.FindResource("TaskButtonVerticalMargin") as Thickness? ?? new Thickness();
            }
            else
            {
                buttonMargin = Application.Current.FindResource("TaskButtonMargin") as Thickness? ?? new Thickness();
            }

            TaskButtonLeftMargin = buttonMargin.Left;
            TaskButtonRightMargin = buttonMargin.Right;
        }

        private void TaskList_OnLoaded(object sender, RoutedEventArgs e)
        {
            SetStyles();
            SetTasksCollection();
        }

        private void SetTasksCollection()
        {
            if (!isLoaded && Tasks != null && Host != null)
            {
                taskbarItems = Tasks.CreateGroupedWindowsCollection();
                if (taskbarItems != null)
                {
                    taskbarItems.CollectionChanged += GroupedWindows_CollectionChanged;
                    taskbarItems.Filter = Tasks_Filter;
                }

                UpdateTasksDisplay();

                Settings.Instance.PropertyChanged += Settings_PropertyChanged;
                Host.hotkeyManager.TaskbarHotkeyPressed += TaskList_TaskbarHotkeyPressed;

                isLoaded = true;
            }
        }

        private void UpdateTasksDisplay()
        {
            if (Settings.Instance.CombinePinsAndPrograms)
            {
                // Use combined pins and programs display
                RebuildCombinedItems();
                TasksList.ItemsSource = combinedItems;
            }
            else if (Settings.Instance.GroupTaskbarButtons)
            {
                // Use grouped display
                RebuildGroupedTasks();
                TasksList.ItemsSource = groupedTasks;
            }
            else
            {
                // Use ungrouped display
                TasksList.ItemsSource = taskbarItems;
            }
        }

        private void RebuildGroupedTasks()
        {
            // Build groups in temporary lookup
            var tempGroupLookup = new Dictionary<string, TaskGroup>();

            if (taskbarItems != null)
            {
                foreach (var item in taskbarItems)
                {
                    if (item is ApplicationWindow window && Tasks_Filter(window))
                    {
                        string groupKey = GetGroupKey(window);

                        if (!tempGroupLookup.ContainsKey(groupKey))
                        {
                            var group = new TaskGroup(groupKey);
                            tempGroupLookup[groupKey] = group;
                        }

                        tempGroupLookup[groupKey].AddWindow(window);
                    }
                }
            }

            // Update the lookup
            groupLookup = tempGroupLookup;

            // Hide the TasksList to prevent visible flickering during rebuild
            TasksList.Visibility = Visibility.Collapsed;

            // Create a new collection with all groups already in it
            var newCollection = new ObservableCollection<TaskGroup>(tempGroupLookup.Values);

            // Replace the entire collection reference
            groupedTasks = newCollection;

            // Update ItemsSource
            TasksList.ItemsSource = groupedTasks;

            // Force layout to materialize all items and load all icons while hidden
            TasksList.UpdateLayout();

            // Show the TasksList after everything is loaded and ready
            // Use Dispatcher to ensure layout is complete before showing
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
            {
                TasksList.Visibility = Visibility.Visible;
            }));
        }

        private void UpdateGroupedTasksIncremental(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is ApplicationWindow window && Tasks_Filter(window))
                            {
                                string groupKey = GetGroupKey(window);

                                if (!groupLookup.ContainsKey(groupKey))
                                {
                                    var group = new TaskGroup(groupKey);
                                    groupLookup[groupKey] = group;
                                    groupedTasks.Add(group);
                                }

                                groupLookup[groupKey].AddWindow(window);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is ApplicationWindow window)
                            {
                                string groupKey = GetGroupKey(window);

                                if (groupLookup.ContainsKey(groupKey))
                                {
                                    var group = groupLookup[groupKey];
                                    group.RemoveWindow(window);

                                    // Remove the group if it's now empty
                                    if (group.Windows.Count == 0)
                                    {
                                        groupedTasks.Remove(group);
                                        groupLookup.Remove(groupKey);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    // Fall back to rebuild for complex changes
                    RebuildGroupedTasks();
                    break;
            }
        }

        private string GetGroupKey(ApplicationWindow window)
        {
            // Group by AppUserModelID if available, otherwise by executable path
            if (!string.IsNullOrEmpty(window.AppUserModelID))
            {
                return window.AppUserModelID;
            }
            return window.WinFileName ?? window.Title;
        }

        private void SetupQuickLaunchFolder()
        {
            if (quickLaunchFolder != null)
                return;

            try
            {
                string path = Environment.ExpandEnvironmentVariables(Settings.Instance.QuickLaunchPath);
                quickLaunchFolder = new ShellFolder(path, IntPtr.Zero, true);

                if (quickLaunchFolder.Files != null)
                {
                    ((INotifyCollectionChanged)quickLaunchFolder.Files).CollectionChanged += QuickLaunchFiles_CollectionChanged;
                }
            }
            catch
            {
                // Failed to load Quick Launch folder
                quickLaunchFolder = null;
            }
        }

        private void CleanupQuickLaunchFolder()
        {
            if (quickLaunchFolder != null)
            {
                if (quickLaunchFolder.Files != null)
                {
                    ((INotifyCollectionChanged)quickLaunchFolder.Files).CollectionChanged -= QuickLaunchFiles_CollectionChanged;
                }
                quickLaunchFolder.Dispose();
                quickLaunchFolder = null;
            }
        }

        private void RebuildCombinedItems()
        {
            // Build the entire list first
            var tempItems = new List<TaskbarItem>();
            var tempLookup = new Dictionary<string, TaskbarItem>();

            // Setup Quick Launch folder if needed
            if (quickLaunchFolder == null && Settings.Instance.ShowQuickLaunch)
            {
                SetupQuickLaunchFolder();
            }

            // First, add all pinned items
            if (quickLaunchFolder?.Files != null)
            {
                foreach (ShellFile pinnedFile in quickLaunchFolder.Files)
                {
                    TaskbarItem item = new TaskbarItem(pinnedFile);
                    tempItems.Add(item);

                    string key = TaskbarItemMatcher.GetPinGroupKey(pinnedFile);
                    if (!string.IsNullOrEmpty(key))
                    {
                        tempLookup[key] = item;
                    }
                }
            }

            // Track unpinned running programs for grouping
            var unpinnedLookup = new Dictionary<string, TaskbarItem>();

            // Then, match or add running windows
            if (taskbarItems != null)
            {
                foreach (var obj in taskbarItems)
                {
                    if (obj is ApplicationWindow window && Tasks_Filter(window))
                    {
                        bool matched = false;

                        // Try to find a matching pinned item
                        foreach (var pinnedItem in tempLookup.Values)
                        {
                            if (TaskbarItemMatcher.DoesWindowMatchPin(window, pinnedItem.PinnedItem))
                            {
                                pinnedItem.AddWindow(window);
                                matched = true;
                                break;
                            }
                        }

                        // If no match, add as unpinned running program (with grouping if enabled)
                        if (!matched)
                        {
                            if (Settings.Instance.GroupTaskbarButtons)
                            {
                                // Group unpinned items by their group key
                                string groupKey = GetGroupKey(window);

                                if (unpinnedLookup.ContainsKey(groupKey))
                                {
                                    // Add to existing group
                                    unpinnedLookup[groupKey].AddWindow(window);
                                }
                                else
                                {
                                    // Create new unpinned item
                                    TaskbarItem newItem = new TaskbarItem(window);
                                    unpinnedLookup[groupKey] = newItem;
                                    tempItems.Add(newItem);
                                }
                            }
                            else
                            {
                                // No grouping - add each window separately
                                tempItems.Add(new TaskbarItem(window));
                            }
                        }
                    }
                }
            }

            // Update lookups
            pinnedItemsLookup.Clear();
            foreach (var kvp in tempLookup)
            {
                pinnedItemsLookup[kvp.Key] = kvp.Value;
            }

            // Hide the TasksList to prevent visible flickering during rebuild
            TasksList.Visibility = Visibility.Collapsed;

            // Create a new collection with all items already in it
            var newCollection = new ObservableCollection<TaskbarItem>(tempItems);

            // Replace the entire collection reference
            combinedItems = newCollection;

            // Update ItemsSource
            TasksList.ItemsSource = combinedItems;

            // Force layout to materialize all items and load all icons while hidden
            TasksList.UpdateLayout();

            // Show the TasksList after everything is loaded and ready
            // Use Dispatcher to ensure layout is complete before showing
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
            {
                TasksList.Visibility = Visibility.Visible;
            }));
        }

        private void UpdateCombinedItemsIncremental(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is ApplicationWindow window && Tasks_Filter(window))
                            {
                                bool matched = false;

                                // Try to match with existing pinned item
                                foreach (var pinnedItem in pinnedItemsLookup.Values)
                                {
                                    if (TaskbarItemMatcher.DoesWindowMatchPin(window, pinnedItem.PinnedItem))
                                    {
                                        pinnedItem.AddWindow(window);
                                        matched = true;
                                        break;
                                    }
                                }

                                // If no match, add as new unpinned item (with grouping if enabled)
                                if (!matched)
                                {
                                    if (Settings.Instance.GroupTaskbarButtons)
                                    {
                                        // Try to find existing unpinned group
                                        string groupKey = GetGroupKey(window);
                                        TaskbarItem existingGroup = combinedItems.FirstOrDefault(ti =>
                                            !ti.IsPinned && ti.IsRunning && GetGroupKey(ti.RunningWindow) == groupKey);

                                        if (existingGroup != null)
                                        {
                                            // Add to existing group
                                            existingGroup.AddWindow(window);
                                        }
                                        else
                                        {
                                            // Create new unpinned item
                                            combinedItems.Add(new TaskbarItem(window));
                                        }
                                    }
                                    else
                                    {
                                        // No grouping - add each window separately
                                        combinedItems.Add(new TaskbarItem(window));
                                    }
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is ApplicationWindow window)
                            {
                                // Find the TaskbarItem containing this window
                                TaskbarItem taskbarItem = combinedItems.FirstOrDefault(ti =>
                                    ti.RunningWindow == window || ti.Windows.Contains(window));

                                if (taskbarItem != null)
                                {
                                    taskbarItem.RemoveWindow(window);

                                    // If it's not pinned and has no more windows, remove it
                                    if (!taskbarItem.IsPinned && !taskbarItem.IsRunning)
                                    {
                                        combinedItems.Remove(taskbarItem);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    RebuildCombinedItems();
                    break;
            }
        }

        private void QuickLaunchFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Settings.Instance.CombinePinsAndPrograms)
            {
                RebuildCombinedItems();
            }
        }

        private static void TasksChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TaskList taskList && e.OldValue == null && e.NewValue != null)
            {
                taskList.SetTasksCollection();
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.MultiMonMode))
            {
                taskbarItems?.Refresh();
            }
            else if (e.PropertyName == nameof(Settings.ShowMultiMon))
            {
                if (Settings.Instance.MultiMonMode != MultiMonOption.AllTaskbars)
                {
                    taskbarItems?.Refresh();
                }
            }
            else if (e.PropertyName == nameof(Settings.CompressTaskbarButtons))
            {
                SetTaskButtonWidth();
            }
            else if (e.PropertyName == nameof(Settings.CompressTaskbarButtonWidth))
            {
                SetTaskButtonWidth();
            }
            else if (e.PropertyName == nameof(Settings.GroupTaskbarButtons))
            {
                UpdateTasksDisplay();
                SetTaskButtonWidth();
            }
            else if (e.PropertyName == nameof(Settings.CombinePinsAndPrograms))
            {
                if (Settings.Instance.CombinePinsAndPrograms)
                {
                    SetupQuickLaunchFolder();
                }
                else
                {
                    CleanupQuickLaunchFolder();
                }
                UpdateTasksDisplay();
                SetTaskButtonWidth();
            }
        }
        private void TaskList_TaskbarHotkeyPressed(object sender, HotkeyManager.TaskbarHotkeyEventArgs e)
        {
            if (Settings.Instance.WinNumHotkeysAction == WinNumHotkeysOption.SwitchTasks && Host.Screen.Primary)
            {
                try
                {
                    bool exists = taskbarItems.MoveCurrentToPosition(e.index);

                    if (exists)
                    {
                        ApplicationWindow window = taskbarItems.CurrentItem as ApplicationWindow;

                        if (e.isShiftPressed)
                        {
                            // Open new instance when Shift is pressed
                            ShellHelper.StartProcess(window.IsUWP ? "appx:" + window.AppUserModelID : window.WinFileName);
                        }
                        else
                        {
                            // Normal behavior - switch to existing window
                            if (window.State == ApplicationWindow.WindowState.Active && window.CanMinimize)
                            {
                                window.Minimize();
                            }
                            else
                            {
                                window.BringToFront();
                            }
                        }
                    }

                }
                catch (ArgumentOutOfRangeException) { }
            }
        }

        private bool Tasks_Filter(object obj)
        {
            if (obj is ApplicationWindow window)
            {
                if (!window.ShowInTaskbar)
                {
                    return false;
                }

                if (!Settings.Instance.ShowMultiMon || Settings.Instance.MultiMonMode == MultiMonOption.AllTaskbars)
                {
                    return true;
                }

                if (Settings.Instance.MultiMonMode == MultiMonOption.SameAsWindowAndPrimary && Host.Screen.Primary)
                {
                    return true;
                }

                IntPtr hMonitor = window.HMonitor;
                if (Host.Screen.Primary && !Host.windowManager.IsValidHMonitor(hMonitor))
                {
                    return true;
                }

                if (hMonitor != Host.Screen.HMonitor)
                {
                    return false;
                }
            }

            return true;
        }

        private void TaskList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (taskbarItems != null)
            {
                taskbarItems.CollectionChanged -= GroupedWindows_CollectionChanged;
                taskbarItems.Filter = null;
            }

            if (Host != null)
            {
                Host.hotkeyManager.TaskbarHotkeyPressed -= TaskList_TaskbarHotkeyPressed;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            CleanupQuickLaunchFolder();

            isLoaded = false;
        }

        private void GroupedWindows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Settings.Instance.CombinePinsAndPrograms)
            {
                UpdateCombinedItemsIncremental(e);
            }
            else if (Settings.Instance.GroupTaskbarButtons)
            {
                UpdateGroupedTasksIncremental(e);
            }
            SetTaskButtonWidth();
        }

        private void TaskList_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTaskButtonWidth();
        }

        private void SetTaskButtonWidth()
        {
            if (Host is null)
                return; // The state is trashed, but presumably it's just a transition

            if (Settings.Instance.Edge == AppBarEdge.Left || Settings.Instance.Edge == AppBarEdge.Right)
            {
                ButtonWidth = ActualWidth;
                SetScrollable(true); // while technically not always scrollable, we don't run into DPI-specific issues with it enabled while vertical
                return;
            }

            double height = ActualHeight;
            int rows = Host.Rows;

            int taskCount = TasksList.Items.Count;
            double margin = TaskButtonLeftMargin + TaskButtonRightMargin;
            double maxWidth = TasksList.ActualWidth / Math.Ceiling((double)taskCount / rows);
            double defaultWidth = Settings.Instance.CompressTaskbarButtons ? Settings.Instance.CompressTaskbarButtonWidth : DefaultButtonWidth + margin;
            double minWidth = Settings.Instance.CompressTaskbarButtons ? Settings.Instance.CompressTaskbarButtonWidth : MinButtonWidth + margin;

            if (maxWidth > defaultWidth)
            {
                ButtonWidth = defaultWidth;
                SetScrollable(false);
            }
            else if (maxWidth < minWidth)
            {
                ButtonWidth = Math.Ceiling(defaultWidth / 2);
                SetScrollable(true);
            }
            else
            {
                ButtonWidth = Math.Floor(maxWidth);
                SetScrollable(false);
            }
        }

        private void SetScrollable(bool canScroll)
        {
            if (canScroll == isScrollable) return;

            if (canScroll)
            {
                TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            isScrollable = canScroll;
        }

        private void TasksScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!isScrollable)
            {
                e.Handled = true;
            }
        }

        private void PinnedToolbarButton_OnClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToolbarButton icon = sender as ToolbarButton;
            if (icon == null)
            {
                return;
            }

            System.Windows.Input.Mouse.Capture(null);
            ShellFile file = icon.DataContext as ShellFile;

            if (file != null && !string.IsNullOrEmpty(file.Path))
            {
                ShellHelper.StartProcess(file.Path);
            }
        }
    }
}