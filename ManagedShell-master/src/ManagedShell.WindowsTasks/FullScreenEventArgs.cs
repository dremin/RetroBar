using System;

namespace ManagedShell.WindowsTasks
{
    public class FullScreenEventArgs : EventArgs
    {
        public IntPtr Handle;
        public bool IsEntering;
    }
}
