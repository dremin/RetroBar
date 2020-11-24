#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using RetroBar.Utilities;
using ManagedShell.WindowsTray;
using Application = System.Windows.Application;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Taskbar : AppBarWindow
    {
        public Taskbar(Screen screen)
        {
            InitializeComponent();
            Screen = screen;

            appBarEdge = NativeMethods.ABEdge.ABE_BOTTOM;
            desiredHeight = Application.Current.FindResource("TaskbarHeight") as double? ?? 0;
            processScreenChanges = true;

            SetPosition();
        }

        internal override void SetPosition()
        {
            Left = Screen.Bounds.Left / dpiScale;
            Width = Screen.Bounds.Width / dpiScale;
            Height = desiredHeight;
            Top = Screen.Bounds.Bottom / dpiScale - Height;

            NotificationArea.Instance.SetTrayHostSizeData(new TrayHostSizeData { edge = (int)appBarEdge, rc = new NativeMethods.Rect { Top = (int)(Top * dpiScale), Left = (int)(Left * dpiScale), Bottom = (int)((Top + Height) * dpiScale), Right = (int)((Left + Width) * dpiScale) } });
        }

        protected override IntPtr CustomWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM.MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(NativeMethods.MA_NOACTIVATE);
            }

            return IntPtr.Zero;
        }

        private void Taskbar_OnLocationChanged(object? sender, EventArgs e)
        {
            // primarily for win7/8, they will set up the appbar correctly but then put it in the wrong place
            double desiredTop = Screen.Bounds.Bottom / dpiScale - Height;

            if (Top != desiredTop) Top = desiredTop;
        }

        private void Taskbar_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Set the window style to noactivate.
            NativeMethods.SetWindowLong(Handle, NativeMethods.GWL_EXSTYLE,
                NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE) | (int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE);
        }

        private void Taskbar_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // because we are setting WS_EX_NOACTIVATE, popups won't go away when clicked outside, since they are not losing focus (they never got it). calling this fixes that.
            NativeMethods.SetForegroundWindow(Handle);
        }

        private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TaskManagerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Shell.StartTaskManager();
        }
    }
}
