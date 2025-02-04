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
        private IntPtr handle;
        private NativeMethods.Rect startupRect;

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
            handle = helper.Handle;

            // set up window procedure
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(WndProc);

            // Makes click-through by adding transparent style
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE) | (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW | (int)NativeMethods.ExtendedWindowStyles.WS_EX_TRANSPARENT);

            WindowHelper.ExcludeWindowFromPeek(helper.Handle);

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

            handled = false;
            return IntPtr.Zero;
        }

        internal void SetPosition(NativeMethods.Rect rect)
        {
            NativeMethods.Rect currentRect;
            NativeMethods.GetWindowRect(handle, out currentRect);

            if (rect.Left == currentRect.Left && rect.Top == currentRect.Top && rect.Right == currentRect.Right && rect.Bottom == currentRect.Bottom)
            {
                return;
            }

            int swp = (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
            NativeMethods.SetWindowPos(handle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, swp);
        }
    }
}
