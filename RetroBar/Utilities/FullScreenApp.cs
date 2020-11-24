using System;
using System.Windows.Forms;
using ManagedShell.Interop;

namespace RetroBar.Utilities
{
    public class FullScreenApp
    {
        public IntPtr hWnd;
        public Screen screen;
        public NativeMethods.Rect rect;
    }
}