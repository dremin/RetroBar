using ManagedShell.Interop;
using System;

namespace ManagedShell.AppBar
{
    public class FullScreenApp
    {
        public IntPtr hWnd;
        public ScreenInfo screen;
        public NativeMethods.Rect rect;
        public string title;
        public bool fromTasksService;
    }
}