using GongSolutions.Wpf.DragDrop;
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
                }
            }

            DragDrop.DefaultDropHandler.DragOver(dropInfo);
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