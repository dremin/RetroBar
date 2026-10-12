using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for FloatingStartButton.xaml
    /// </summary>
    public partial class FloatingStartButton : Window
    {
        private WindowInteropHelper helper;
        private NativeMethods.Rect startupRect;

        public IntPtr Handle;

        public FloatingStartButton(StartButton mainButton, NativeMethods.Rect rect)
        {
            Owner = mainButton.Host;
            DataContext = mainButton;

            InitializeComponent();
            startupRect = rect;

            // Render the existing start button control as the ViewRect fill
            VisualBrush visualBrush = new VisualBrush(mainButton.Start);
            ViewRect.Fill = visualBrush;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // set up helper and get handle
            helper = new WindowInteropHelper(this);
            Handle = helper.Handle;

            // set up window procedure
            HwndSource source = HwndSource.FromHwnd(Handle);
            source.AddHook(WndProc);

            // Makes click-through by adding transparent style, hide from taskbar
            NativeMethods.SetWindowLong(Handle, NativeMethods.WindowLongFlags.GWL_EXSTYLE, (NativeMethods.GetWindowLong(Handle, NativeMethods.WindowLongFlags.GWL_EXSTYLE) & ~(int)NativeMethods.ExtendedWindowStyles.WS_EX_APPWINDOW) | (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW | (int)NativeMethods.ExtendedWindowStyles.WS_EX_TRANSPARENT);

            WindowHelper.ExcludeWindowFromPeek(Handle);

            SetPosition(startupRect);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Make transparent to hit tests
            if (msg == (int)NativeMethods.WM.NCHITTEST)
            {
                handled = true;
                return (IntPtr)(-1);
            }

            if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING)
            {
                // Extract the WINDOWPOS structure corresponding to this message
                NativeMethods.WINDOWPOS wndPos = NativeMethods.WINDOWPOS.FromMessage(lParam);

                // WORKAROUND WPF bug: https://github.com/dotnet/wpf/issues/7561
                // If there is no NOMOVE or NOSIZE or NOACTIVATE flag, and there is a NOZORDER flag, add the NOACTIVATE flag
                if ((wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOMOVE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOSIZE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOZORDER) != 0)
                {
                    wndPos.flags |= NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
                    wndPos.UpdateMessage(lParam);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        internal void SetPosition(NativeMethods.Rect rect)
        {
            NativeMethods.Rect currentRect;
            NativeMethods.GetWindowRect(Handle, out currentRect);

            if (rect.Left == currentRect.Left && rect.Top == currentRect.Top && rect.Right == currentRect.Right && rect.Bottom == currentRect.Bottom)
            {
                return;
            }

            int swp = (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
            NativeMethods.SetWindowPos(Handle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, swp);

            if (Owner is Taskbar host && host.Screen != null)
            {
                // Clip the window to the monitor bounds to prevent bleeding into adjacent screens
                var bounds = host.Screen.Bounds;
                int clipLeft = Math.Max(0, bounds.Left - rect.Left);
                int clipTop = Math.Max(0, bounds.Top - rect.Top);
                int clipRight = Math.Min(rect.Width, bounds.Right - rect.Left);
                int clipBottom = Math.Min(rect.Height, bounds.Bottom - rect.Top);

                // Clip the window so as to not cover the in-taskbar button
                switch (host.AppBarEdge)
                {
                    case AppBarEdge.Bottom:
                        clipBottom = Math.Min(clipBottom, host.WindowRect.Top - rect.Top);
                        break;
                    case AppBarEdge.Top:
                        clipTop = Math.Max(clipTop, host.WindowRect.Bottom - rect.Top);
                        break;
                    case AppBarEdge.Left:
                        clipLeft = Math.Max(clipLeft, host.WindowRect.Right - rect.Left);
                        break;
                    case AppBarEdge.Right:
                        clipRight = Math.Min(clipRight, host.WindowRect.Left - rect.Left);
                        break;
                }

                if (clipLeft < clipRight && clipTop < clipBottom)
                {
                    if (host.FlowDirection == FlowDirection.RightToLeft)
                    {
                        // RTL uses a top-right origin instead of top-left
                        int left = rect.Width - clipRight;
                        int right = rect.Width - clipLeft;
                        clipLeft = left;
                        clipRight = right;
                    }

                    IntPtr hRgn = NativeMethods.CreateRectRgn(clipLeft, clipTop, clipRight, clipBottom);
                    if (hRgn != IntPtr.Zero && NativeMethods.SetWindowRgn(Handle, hRgn, true) == 0)
                    {
                        // If SetWindowRgn succeeds, the system owns the region.
                        // If it fails, we still own it and should delete it.
                        NativeMethods.DeleteObject(hRgn);
                    }
                }
            }
        }
    }
}
