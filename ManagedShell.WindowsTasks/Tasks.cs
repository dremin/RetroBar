using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using ManagedShell.Common.DesignPatterns;
using ManagedShell.Common.Logging;

namespace ManagedShell.WindowsTasks
{
    public class Tasks : SingletonObject<Tasks>, IDisposable
    {
        private ICollectionView groupedWindows;

        public ICollectionView GroupedWindows => groupedWindows;

        private Tasks()
        {
            // prepare collections
            groupedWindows = CollectionViewSource.GetDefaultView(TasksService.Instance.Windows);
            groupedWindows.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            groupedWindows.CollectionChanged += groupedWindows_Changed;
            groupedWindows.Filter = groupedWindows_Filter;

            if (groupedWindows is ICollectionViewLiveShaping taskbarItemsView)
            {
                taskbarItemsView.IsLiveFiltering = true;
                taskbarItemsView.LiveFilteringProperties.Add("ShowInTaskbar");
                taskbarItemsView.IsLiveGrouping = true;
                taskbarItemsView.LiveGroupingProperties.Add("Category");
            }
        }

        public void Initialize(ITaskCategoryProvider taskCategoryProvider)
        {
            if (!TasksService.Instance.IsInitialized)
            {
                TasksService.Instance.SetTaskCategoryProvider(taskCategoryProvider);
                Initialize();
            }
        }

        public void Initialize()
        {
            TasksService.Instance.Initialize();
        }

        public void CloseWindow(ApplicationWindow window)
        {
            if (window.Close() != IntPtr.Zero)
            {
                CairoLogger.Instance.Debug($"Removing window {window.Title} from collection due to no response");
                window.Dispose();
                TasksService.Instance.Windows.Remove(window);
            }
        }

        private void groupedWindows_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            // yup, do nothing. helps prevent a NRE
        }

        private bool groupedWindows_Filter(object item)
        {
            if (item is ApplicationWindow window && window.ShowInTaskbar)
                return true;
            
            return false;
        }

        public void Dispose()
        {
            TasksService.Instance.Dispose();
        }
    }
}
