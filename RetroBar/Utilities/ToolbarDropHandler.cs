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

        public void DragEnter(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragEnter(dropInfo);
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        public void DragLeave(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragLeave(dropInfo);
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
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
