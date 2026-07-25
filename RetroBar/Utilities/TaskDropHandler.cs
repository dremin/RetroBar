using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using GongSolutions.Wpf.DragDrop;
using ManagedShell.WindowsTasks;
using RetroBar.Controls;

namespace RetroBar.Utilities
{
    public class TaskDropHandler : IDropTarget
    {
        private TaskList _taskList;

        public TaskDropHandler(TaskList taskList)
        {
            _taskList = taskList;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (Settings.Instance.GroupTaskbarButtons && (dropInfo.Data is CollectionViewGroup || dropInfo.Data is ApplicationWindow))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = System.Windows.DragDropEffects.Move;
                return;
            }

            DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (Settings.Instance.GroupTaskbarButtons && (dropInfo.Data is CollectionViewGroup || dropInfo.Data is ApplicationWindow))
            {
                var sourceItem = dropInfo.Data;
                var targetItem = dropInfo.TargetItem;

                if (sourceItem == targetItem || targetItem == null)
                    return;

                // Safely get the underlying collection as IList instead of the Tasks object wrapper.
                var tasksCollection = _taskList.UnderlyingTasks;
                if (tasksCollection == null)
                    return;

                List<ApplicationWindow> sourceWindows = new List<ApplicationWindow>();
                if (sourceItem is CollectionViewGroup sourceGroup)
                {
                    foreach (var item in sourceGroup.Items)
                    {
                        if (item is ApplicationWindow window)
                            sourceWindows.Add(window);
                    }
                }
                else if (sourceItem is ApplicationWindow window)
                {
                    sourceWindows.Add(window);
                }

                int targetIndex = tasksCollection.Count; // Default to end
                if (targetItem is CollectionViewGroup targetGroup)
                {
                    // Find the first or last window of the target group based on InsertPosition
                    var targetWindows = targetGroup.Items.OfType<ApplicationWindow>().ToList();
                    if (targetWindows.Any())
                    {
                        if (dropInfo.InsertPosition == RelativeInsertPosition.BeforeTargetItem)
                        {
                            targetIndex = tasksCollection.IndexOf(targetWindows.First());
                        }
                        else
                        {
                            targetIndex = tasksCollection.IndexOf(targetWindows.Last()) + 1;
                        }
                    }
                }
                else if (targetItem is ApplicationWindow targetWindow)
                {
                    targetIndex = tasksCollection.IndexOf(targetWindow);
                    if (dropInfo.InsertPosition == RelativeInsertPosition.AfterTargetItem)
                    {
                        targetIndex++;
                    }
                }

                if (targetIndex < 0) targetIndex = 0;
                if (targetIndex > tasksCollection.Count) targetIndex = tasksCollection.Count;

                foreach (var window in sourceWindows)
                {
                    int currentIndex = tasksCollection.IndexOf(window);
                    if (currentIndex != -1)
                    {
                        if (currentIndex < targetIndex)
                        {
                            targetIndex--;
                        }
                        tasksCollection.RemoveAt(currentIndex);
                    }
                }

                for (int i = 0; i < sourceWindows.Count; i++)
                {
                    tasksCollection.Insert(targetIndex + i, sourceWindows[i]);
                }

                return;
            }

            try
            {
                DragDrop.DefaultDropHandler.Drop(dropInfo);
            }
            catch
            {
                // Ignore any internal Gong WPF DragDrop exceptions to prevent crashes
            }
        }

        public void DragEnter(IDropInfo dropInfo)
        {
            if (dropInfo.Data is CollectionViewGroup || dropInfo.Data is ApplicationWindow) return;
            DragDrop.DefaultDropHandler.DragEnter(dropInfo);
        }

        public void DragLeave(IDropInfo dropInfo)
        {
            if (dropInfo.Data is CollectionViewGroup || dropInfo.Data is ApplicationWindow) return;
            DragDrop.DefaultDropHandler.DragLeave(dropInfo);
        }
    }
}