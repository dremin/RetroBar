using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyBalloon.xaml
    /// </summary>
    public partial class NotifyBalloon : UserControl
    {
        private DispatcherTimer _closeTimer;
        private NotificationBalloon _balloonInfo = new NotificationBalloon();

        public NotifyBalloon()
        {
            DataContext = _balloonInfo;
            InitializeComponent();
        }

        public void Show(NotificationBalloon balloonInfo, UIElement placementTarget)
        {
            _balloonInfo = balloonInfo;
            DataContext = _balloonInfo;

            playSound(_balloonInfo);

            BalloonPopup.PlacementTarget = placementTarget;
            BalloonPopup.Placement = PlacementMode.Custom;
            BalloonPopup.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(PlacePopup);
            BalloonPopup.IsOpen = true;

            _balloonInfo.SetVisibility(BalloonVisibility.Visible);

            startTimer(balloonInfo.Timeout);
        }

        public CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        {
            CustomPopupPlacement placement;

            DpiScale dpiScale = VisualTreeHelper.GetDpi(this);

            switch ((AppBarEdge)Settings.Instance.Edge)
            {
                case AppBarEdge.Top:
                    placement = new CustomPopupPlacement(new Point((popupSize.Width * -1) + (offset.X * dpiScale.DpiScaleX),
                        targetSize.Height + (offset.Y * dpiScale.DpiScaleY)),
                        PopupPrimaryAxis.Horizontal);
                    break;
                case AppBarEdge.Left:
                    placement = new CustomPopupPlacement(new Point(offset.X * dpiScale.DpiScaleX,
                        (popupSize.Height * -1) + (offset.Y * dpiScale.DpiScaleY)),
                        PopupPrimaryAxis.Horizontal);
                    break;
                default:
                    // bottom or right taskbar
                    placement = new CustomPopupPlacement(new Point((popupSize.Width * -1) + (offset.X * dpiScale.DpiScaleX),
                        (popupSize.Height * -1) + (offset.Y * dpiScale.DpiScaleY)),
                        PopupPrimaryAxis.Horizontal);
                    break;
            }

            return new CustomPopupPlacement[] { placement };
        }

        private void playSound(NotificationBalloon balloonInfo)
        {
            if (BalloonPopup.IsOpen)
            {
                return;
            }

            if ((balloonInfo.Flags & ManagedShell.Interop.NativeMethods.NIIF.NOSOUND) != 0)
            {
                return;
            }

            SoundHelper.PlayNotificationSound();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            closeBalloon();
            _balloonInfo.SetVisibility(BalloonVisibility.Hidden);
            e.Handled = true;
        }

        private void closeBalloon()
        {
            _closeTimer?.Stop();
            BalloonPopup.IsOpen = false;
        }

        private void startTimer(int timeout)
        {
            _closeTimer?.Stop();

            _closeTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(timeout)
            };

            _closeTimer.Tick += CloseTimer_Tick;
            _closeTimer.Start();
        }

        private void CloseTimer_Tick(object sender, System.EventArgs e)
        {
            closeBalloon();
            _balloonInfo.SetVisibility(BalloonVisibility.TimedOut);
        }

        private void ContentControl_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Prevent taskbar context menu from appearing
            e.Handled = true;
        }

        private void ContentControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _balloonInfo.Click();

            closeBalloon();
            e.Handled = true;
        }
    }
}
