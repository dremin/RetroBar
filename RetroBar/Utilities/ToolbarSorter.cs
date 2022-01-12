using ManagedShell.ShellFolders;
using RetroBar.Controls;
using System.Collections;
using System.Collections.Generic;

namespace RetroBar.Utilities
{
    public class ToolbarSorter : IComparer
    {
        private Toolbar _toolbar;

        public ToolbarSorter(Toolbar toolbar)
        {
            _toolbar = toolbar;
        }

        public int Compare(object x, object y)
        {
            if (x is ShellItem a && y is ShellItem b)
            {
                List<string> desiredSort = Settings.Instance.QuickLaunchOrder;

                // If reordering, modify the desired sort to reflect the in-flight drop.
                if (_toolbar.DropHandler.DropInFlight != null && 
                    _toolbar.DropHandler.DropInFlight.Data is ShellFile droppedFile && 
                    (droppedFile == a || droppedFile == b))
                {
                    int desiredIndex = _toolbar.DropHandler.DropInFlight.InsertIndex;
                    int currentIndex = desiredSort.IndexOf(droppedFile.Path);

                    if (currentIndex >= 0)
                    {
                        // If we are dragging to a position after what is being removed, update desired position
                        if (desiredIndex > currentIndex)
                        {
                            desiredIndex--;
                        }

                        desiredSort.RemoveAt(currentIndex);
                    }

                    // Safety first!
                    if (desiredIndex > desiredSort.Count)
                    {
                        desiredIndex = desiredSort.Count;
                    }

                    desiredSort.Insert(desiredIndex, droppedFile.Path);
                }

                if (!desiredSort.Contains(a.Path) && !desiredSort.Contains(b.Path))
                {
                    return 0;
                }

                if (!desiredSort.Contains(a.Path))
                {
                    return 1;
                }

                if (!desiredSort.Contains(b.Path))
                {
                    return -1;
                }

                int indexA = desiredSort.IndexOf(a.Path);
                int indexB = desiredSort.IndexOf(b.Path);

                if (indexA < indexB)
                {
                    return -1;
                }

                if (indexA > indexB)
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}
