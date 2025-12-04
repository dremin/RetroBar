using System;
using System.Windows;
using System.Windows.Threading;

namespace RetroBar.Utilities
{
    public class DelayedActivationHandler : IDisposable
    {
        private bool inDrag;
        private DispatcherTimer activationTimer;
        private Action performAction;

        public DelayedActivationHandler(TimeSpan activationDelay, Action action)
        {
            activationTimer = new DispatcherTimer { Interval = activationDelay };
            activationTimer.Tick += activationTimer_Tick;
            performAction = action;
        }

        public DelayedActivationHandler(Action action) : this(SystemParameters.MouseHoverTime, action) { }

        private void activationTimer_Tick(object sender, EventArgs e)
        {
            if (inDrag)
            {
                performAction?.Invoke();
            }

            activationTimer.Stop();
        }

        public void OnDragEnter(DragEventArgs e)
        {
            // Ignore drag operations from a reorder
            if (!inDrag && !e.Data.GetDataPresent("GongSolutions.Wpf.DragDrop"))
            {
                inDrag = true;
                activationTimer.Start();
            }
        }

        public void OnDragLeave()
        {
            if (inDrag)
            {
                activationTimer.Stop();
                inDrag = false;
            }
        }

        public void Dispose()
        {
            performAction = null;
        }
    }
}
