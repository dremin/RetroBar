using GongSolutions.Wpf.DragDrop;
using RetroBar.Controls;

namespace RetroBar.Utilities
{
    public class NotifyIconDropHandler : IDropTarget
    {
        private NotifyIconList _notifyIconList;

        public IDropInfo DropInFlight { get; set; }

        public NotifyIconDropHandler(NotifyIconList notifyIconList)
        {
            _notifyIconList = notifyIconList;
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
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
            // Save before the drop in order to catch any items not yet saved
            _notifyIconList.SaveIconOrder();
            DropInFlight = dropInfo;

            DragDrop.DefaultDropHandler.Drop(dropInfo);

            // Save post-drop state
            _notifyIconList.SaveIconOrder();
            DropInFlight = null;
        }
    }
}
