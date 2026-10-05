using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for FloatingStartButton.xaml
    /// </summary>
    public partial class FloatingStartButton : Window, INotifyPropertyChanged
    {
        // Declared locally so this file does not depend on which ManagedShell version provides them.
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        private WindowInteropHelper helper;
        private NativeMethods.Rect startupRect;
        private (int Left, int Top, int Right, int Bottom)? appliedClip;

        private StartButton MainButton => (StartButton)DataContext;

        public bool IsScaled => MainButton.Host.IsScaled;
        public int Rows => MainButton.Host.Rows;
        public AppBarEdge AppBarEdge => MainButton.Host.AppBarEdge;
        public Orientation Orientation => MainButton.Host.Orientation;

        public event PropertyChangedEventHandler PropertyChanged;
        public IntPtr Handle;

        public FloatingStartButton(StartButton mainButton, NativeMethods.Rect rect)
        {
            Owner = mainButton.Host;
            DataContext = mainButton;

            InitializeComponent();
            startupRect = rect;

            if (mainButton.Host != null)
            {
                mainButton.Host.PropertyChanged += Host_PropertyChanged;
            }
        }

        private void Host_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // set up helper and get handle
            helper = new WindowInteropHelper(this);
            Handle = helper.Handle;

            // set up window procedure
            HwndSource source = HwndSource.FromHwnd(Handle);
            source.AddHook(WndProc);

            WindowHelper.HideWindowFromTasks(Handle);
            WindowHelper.ExcludeWindowFromPeek(Handle);

            SetPosition(startupRect);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Prevent window activation on click so the taskbar/start menu doesn't lose focus
            if (msg == (int)NativeMethods.WM.MOUSEACTIVATE)
            {
                handled = true;
                return (IntPtr)NativeMethods.MA_NOACTIVATE;
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
            startupRect = rect;
            NativeMethods.Rect currentRect;
            NativeMethods.GetWindowRect(Handle, out currentRect);

            if (rect.Left != currentRect.Left || rect.Top != currentRect.Top || rect.Right != currentRect.Right || rect.Bottom != currentRect.Bottom)
            {
                int swp = (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
                NativeMethods.SetWindowPos(Handle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, swp);

                // WPF can adjust the window size (e.g. DPI changes), so use the real bounds from here on.
                NativeMethods.GetWindowRect(Handle, out currentRect);
            }

            UpdateClipRegion(currentRect);
        }

        private void UpdateClipRegion(NativeMethods.Rect windowRect)
        {
            // Clip the window to the monitor bounds to prevent bleeding into adjacent screens.
            // The region is only applied when the window actually crosses the monitor edge. Otherwise
            // any previous region is cleared, so a stale or undersized region can never cut off the orb.
            if (MainButton.Host?.Screen == null)
            {
                return;
            }

            var bounds = MainButton.Host.Screen.Bounds;
            int clipLeft = Math.Max(0, bounds.Left - windowRect.Left);
            int clipTop = Math.Max(0, bounds.Top - windowRect.Top);
            int clipRight = Math.Min(windowRect.Width, bounds.Right - windowRect.Left);
            int clipBottom = Math.Min(windowRect.Height, bounds.Bottom - windowRect.Top);

            bool needsClip = clipLeft > 0 || clipTop > 0 || clipRight < windowRect.Width || clipBottom < windowRect.Height;
            (int, int, int, int)? desiredClip = needsClip && clipLeft < clipRight && clipTop < clipBottom
                ? (clipLeft, clipTop, clipRight, clipBottom)
                : null;

            if (needsClip && desiredClip == null)
            {
                // Window is entirely off this monitor; leave the current region alone.
                return;
            }

            if (desiredClip.Equals(appliedClip))
            {
                return;
            }

            appliedClip = desiredClip;

            if (desiredClip is { } c)
            {
                IntPtr hRgn = CreateRectRgn(c.Item1, c.Item2, c.Item3, c.Item4);
                SetWindowRgn(Handle, hRgn, true);
            }
            else
            {
                SetWindowRgn(Handle, IntPtr.Zero, true);
            }
        }
    }
}
