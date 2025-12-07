using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace ManagedShell.WindowsTasks
{
    public class Tasks : IDisposable
    {
        private readonly TasksService _tasksService;
        private ICollectionView groupedWindows;

        public ICollectionView GroupedWindows => groupedWindows;

        public Tasks(TasksService tasksService)
        {
            _tasksService = tasksService;
            // prepare collections
            groupedWindows = CreateGroupedWindowsCollection();
        }

        public ICollectionView CreateGroupedWindowsCollection()
        {
            ICollectionView collection;
            if (groupedWindows == null)
            {
                collection = CollectionViewSource.GetDefaultView(_tasksService.Windows);
            }
            else
            {
                collection = new CollectionViewSource { Source = groupedWindows.SourceCollection }.View;
            }

            collection.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            collection.CollectionChanged += groupedWindows_Changed;
            collection.Filter = groupedWindows_Filter;

            if (collection is ICollectionViewLiveShaping taskbarItemsView)
            {
                taskbarItemsView.IsLiveFiltering = true;
                taskbarItemsView.LiveFilteringProperties.Add("HMonitor");
                taskbarItemsView.LiveFilteringProperties.Add("ShowInTaskbar");
                taskbarItemsView.IsLiveGrouping = true;
                taskbarItemsView.LiveGroupingProperties.Add("Category");
            }

            return collection;
        }

        public void Initialize(ITaskCategoryProvider taskCategoryProvider, bool withMultiMonTracking = false)
        {
            if (!_tasksService.IsInitialized)
            {
                SetTaskCategoryProvider(taskCategoryProvider);
                Initialize(withMultiMonTracking);
            }
        }

        public void Initialize(bool withMultiMonTracking = false)
        {
            _tasksService.Initialize(withMultiMonTracking);
        }

        public void SetTaskCategoryProvider(ITaskCategoryProvider taskCategoryProvider)
        {
            if (_tasksService.TaskCategoryProvider != null)
            {
                _tasksService.TaskCategoryProvider.Dispose();
            }

            _tasksService.SetTaskCategoryProvider(taskCategoryProvider);
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
            _tasksService.Dispose();
        }
    }
}
