using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;

namespace ManagedShell.Common.Common
{
    public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
    {

        Dispatcher _dispatcher;
        ReaderWriterLockSlim _lock;
        public ThreadSafeObservableCollection()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _lock = new ReaderWriterLockSlim();
        }

        protected override void ClearItems()
        {
            if (_dispatcher.CheckAccess())
            {
                try
                {
                    _lock.EnterWriteLock();
                    base.ClearItems();
                }
                finally { _lock.ExitWriteLock(); }
            }
            else
            {
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { Clear(); },null);
            }
        }
        protected override void InsertItem(int index, T item)
        {
            if (_dispatcher.CheckAccess())
            {
                try
                {
                    _lock.EnterWriteLock();
                    if (index >= 0 && index <= Count)
                        base.InsertItem(index, item);
                }
                finally { _lock.ExitWriteLock(); }
            }
            else
            {
                object[] e = new object[] { index, item };
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { InsertItemImpl(e); }, e);
            }
        }
        void InsertItemImpl(object[] e)
        {
            if (_dispatcher.CheckAccess())
            {
                InsertItem((int)e[0], (T)e[1]);
            }
            else
            {
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { InsertItemImpl(e); });
            }
        }
        protected override void MoveItem(int oldIndex, int newIndex)
        {
            if (_dispatcher.CheckAccess())
            {
                try
                {
                    _lock.EnterWriteLock();
                    if (oldIndex < Count && newIndex < Count && oldIndex != newIndex)
                        base.MoveItem(oldIndex, newIndex);
                }
                finally { _lock.ExitWriteLock(); }
            }
            else
            {
                object[] e = new object[] { oldIndex, newIndex };
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { MoveItemImpl(e); }, e);
            }
        }
        void MoveItemImpl(object[] e)
        {
            if (_dispatcher.CheckAccess())
            {
                MoveItem((int)e[0], (int)e[1]);
            }
            else
            {
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { MoveItemImpl(e); });
            }
        }
        protected override void RemoveItem(int index)
        {
            if (_dispatcher.CheckAccess())
            {
                try
                {
                    _lock.EnterWriteLock();
                    if (index < Count)
                        base.RemoveItem(index);
                }
                finally { _lock.ExitWriteLock(); }
            }
            else
            {
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { RemoveItem(index); }, index);
            }
        }
        protected override void SetItem(int index, T item)
        {
            if (_dispatcher.CheckAccess())
            {
                try
                {
                    _lock.EnterWriteLock();
                    base.SetItem(index, item);
                }
                finally { _lock.ExitWriteLock(); }
            }
            else
            {
                object[] e = new object[] { index, item };
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { SetItemImpl(e); }, e);
            }
        }
        void SetItemImpl(object[] e)
        {
            if (_dispatcher.CheckAccess())
            {
                SetItem((int)e[0], (T)e[1]);
            }
            else
            {
                _dispatcher.Invoke(DispatcherPriority.Background, (SendOrPostCallback)delegate { SetItemImpl(e); });
            }
        }
    }
}
