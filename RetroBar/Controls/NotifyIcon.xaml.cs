using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyIcon.xaml
    /// </summary>
    public partial class NotifyIcon : UserControl
    {
        private ManagedShell.WindowsTray.NotifyIcon TrayIcon;

        public NotifyIcon()
        {
            InitializeComponent();
        }

        private void NotifyIcon_OnLoaded(object sender, RoutedEventArgs e)
        {
            TrayIcon = DataContext as ManagedShell.WindowsTray.NotifyIcon;
        }

        private void NotifyIcon_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TrayIcon?.IconMouseDown(e.ChangedButton, MouseHelper.GetCursorPositionParam(), System.Windows.Forms.SystemInformation.DoubleClickTime);
        }

        private void NotifyIcon_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TrayIcon?.IconMouseUp(e.ChangedButton, MouseHelper.GetCursorPositionParam(), System.Windows.Forms.SystemInformation.DoubleClickTime);
        }

        private void NotifyIcon_OnMouseEnter(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            if (TrayIcon != null)
            {
                // update icon position for Shell_NotifyIconGetRect
                Decorator sendingDecorator = sender as Decorator;
                Point location = sendingDecorator.PointToScreen(new Point(0, 0));
                double dpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;

                TrayIcon.Placement = new NativeMethods.Rect { Top = (int)location.Y, Left = (int)location.X, Bottom = (int)(sendingDecorator.ActualHeight * dpiScale), Right = (int)(sendingDecorator.ActualWidth * dpiScale) };
                TrayIcon.IconMouseEnter(MouseHelper.GetCursorPositionParam());
            }
        }

        private void NotifyIcon_OnMouseLeave(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            TrayIcon?.IconMouseLeave(MouseHelper.GetCursorPositionParam());
        }

        private void NotifyIcon_OnMouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            TrayIcon?.IconMouseMove(MouseHelper.GetCursorPositionParam());
        }
    }
}
