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
            DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            DropInFlight = dropInfo;

            DragDrop.DefaultDropHandler.Drop(dropInfo);

            _toolbar.SaveItemOrder();
            DropInFlight = null;
        }
    }
}
