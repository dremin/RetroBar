using System;
using System.Windows.Forms;
using ManagedShell.Interop;

namespace ManagedShell.Common.SupportingClasses
{
    public class ShellWindow : NativeWindowEx, IDisposable
    {
        public bool IsShellWindow;
        public EventHandler WallpaperChanged;
        public EventHandler WorkAreaChanged;

        public ShellWindow()
        {
            CreateParams cp = new CreateParams();
            cp.Style |= (int)NativeMethods.WindowStyles.WS_VISIBLE;
            cp.Style |= unchecked((int)NativeMethods.WindowStyles.WS_POPUP);
            cp.ExStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW | 
                (int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE;
            cp.Height = SystemInformation.VirtualScreen.Height;
            cp.Width = SystemInformation.VirtualScreen.Width;
            cp.X = SystemInformation.VirtualScreen.Left;
            cp.Y = SystemInformation.VirtualScreen.Top;

            CreateHandle(cp);
            MessageReceived += ShellWndProc;
            NativeMethods.SetWindowLong(Handle, NativeMethods.GWL_EXSTYLE, 
                NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE) & 
                ~(int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE);

            if (NativeMethods.SetShellWindow(Handle) == 1)
            {
                // we did it
                IsShellWindow = true;
            }
        }

        public void SetSize()
        {
            NativeMethods.SetWindowPos(Handle, IntPtr.Zero, SystemInformation.VirtualScreen.Left, 
                SystemInformation.VirtualScreen.Top, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, 
                (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE);
        }

        public void Dispose()
        {
            NativeMethods.DestroyWindow(Handle);
        }

        private void ShellWndProc(ref Message msg, ref bool handled)
        {
            // Window procedure for the native window
            // Because other desktop windows are children, we need to pass them some received events.
            if (msg.Msg == (int)NativeMethods.WM.SETTINGCHANGE && msg.WParam.ToInt32() == (int)NativeMethods.SPI.SETWORKAREA)
            {
                WorkAreaChanged?.Invoke(this, new EventArgs());
            }
            else if (msg.Msg == (int)NativeMethods.WM.SETTINGCHANGE && msg.WParam.ToInt32() == (int)NativeMethods.SPI.SETDESKWALLPAPER)
            {
                WallpaperChanged?.Invoke(this, new EventArgs());
                msg.Result = new IntPtr(NativeMethods.MA_NOACTIVATE);
            }
        }
    }
}
