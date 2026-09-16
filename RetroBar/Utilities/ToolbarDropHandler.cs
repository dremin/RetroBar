using GongSolutions.Wpf.DragDrop;
using ManagedShell.ShellFolders;
using RetroBar.Controls;

namespace RetroBar.Utilities
{
    public class ToolbarDropHandler : IDropTarget
    {
        private Toolbar _toolbar;

        public IDropInfo DropInFlight { get; set; }

        public ToolbarDropHandler(Toolbar toolbar)
        {
            _toolbar = toolbar;
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is System.Windows.DataObject dataObject)
            {
                if (dataObject != null && dataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    dropInfo.Effects = System.Windows.DragDropEffects.Link;
                    return;
                }
            }

            if (dropInfo.Data is ShellFile && IsCrossingBetweenToolbarAndOverflow(dropInfo))
            {
                // Dragging between the visible Quick Launch icons and the chevron's
                // overflow flyout - always a valid move, regardless of what the default
                // handler would make of their differently-shaped backing collections
                // (see Drop, below, for why this needs its own handling).
                // Deliberately not opening the flyout for a visual preview here: doing so
                // (confirmed by testing, not just suspected) hands mouse capture to the
                // native menu the instant it opens, which cuts the drag operation off
                // before the drop can land. Silent-but-working beats visible-but-broken.
                dropInfo.Effects = System.Windows.DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                return;
            }

            DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        /// <summary>
        /// True when a drag started in one of the toolbar/overflow-flyout pair and is
        /// currently over the other one - i.e. it's crossing between the two different
        /// views (see Toolbar.MoveQuickLaunchItem) rather than just reordering within
        /// whichever one it started in.
        /// </summary>
        private static bool IsCrossingBetweenToolbarAndOverflow(IDropInfo dropInfo)
        {
            return dropInfo.DragInfo != null
                   && dropInfo.VisualTarget != null
                   && dropInfo.DragInfo.VisualSource != dropInfo.VisualTarget;
        }

#if !NETCOREAPP3_1_OR_GREATER
        public void DragEnter(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragEnter(dropInfo);
        }

        public void DragLeave(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragLeave(dropInfo);
        }
#endif

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is System.Windows.DataObject dataObject)
            {
                if (dataObject != null && dataObject.ContainsFileDropList())
                {
                    _toolbar.AddToSource(dataObject.GetFileDropList());
                    return;
                }
            }

            if (dropInfo.Data is ShellFile draggedFile && IsCrossingBetweenToolbarAndOverflow(dropInfo))
            {
                // The visible Quick Launch icons and the chevron's overflow flyout are
                // two different views over the same underlying order (see OverflowPanel)
                // - one shows whatever currently fits, the other shows whatever doesn't.
                // So dragging an icon across between them is really just moving it
                // within that one shared order and letting the existing automatic
                // space-based overflow figure out where it lands, same as it already
                // does for every icon - not a separate persisted "always overflow" flag
                // the way the removed feature worked. Handled here instead of via the
                // default same-collection move below, which would otherwise try (and
                // fail) to move the item between two unrelated collections.
                bool insertAfterTarget = dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem);
                _toolbar.MoveQuickLaunchItem(draggedFile, dropInfo.TargetItem as ShellFile, insertAfterTarget);
                return;
            }

            // Save before the drop in order to catch any items not yet saved
            _toolbar.SaveItemOrder();
            DropInFlight = dropInfo;

            DragDrop.DefaultDropHandler.Drop(dropInfo);

            // Save post-drop state
            _toolbar.SaveItemOrder();
            DropInFlight = null;
        }
    }
}