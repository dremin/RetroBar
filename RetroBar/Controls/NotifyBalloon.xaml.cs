using ManagedShell.AppBar;
using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyBalloon.xaml
    /// </summary>
    public partial class NotifyBalloon : UserControl
    {
        private DispatcherTimer closeTimer;

        public NotifyBalloon()
        {
            DataContext = new NotificationBalloon();
            InitializeComponent();

            // TODO: Find way to promote ballooned icons while hidden
        }

        public void Show(NotificationBalloon balloonInfo, UIElement placementTarget)
        {
            BalloonPopup.PlacementTarget = placementTarget;
            BalloonPopup.Placement = PlacementMode.Custom;
            BalloonPopup.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(PlacePopup);

            DataContext = balloonInfo;
            BalloonPopup.IsOpen = true;

            (DataContext as NotificationBalloon).SetVisibility(BalloonVisibility.Visible);

            startTimer(balloonInfo.Timeout);
        }

        public CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        {
            CustomPopupPlacement placement;

            switch ((AppBarEdge)Settings.Instance.Edge)
            {
                case AppBarEdge.Top:
                    placement = new CustomPopupPlacement(new Point((popupSize.Width * -1) + offset.X,
                        targetSize.Height + offset.Y),
                        PopupPrimaryAxis.Horizontal);
                    break;
                case AppBarEdge.Left:
                    placement = new CustomPopupPlacement(new Point(offset.X,
                        (popupSize.Height * -1) + offset.Y),
                        PopupPrimaryAxis.Horizontal);
                    break;
                default:
                    // bottom or right taskbar
                    placement = new CustomPopupPlacement(new Point((popupSize.Width * -1) + offset.X,
                        (popupSize.Height * -1) + offset.Y),
                        PopupPrimaryAxis.Horizontal);
                    break;
            }

            return new CustomPopupPlacement[] { placement };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            closeBalloon();
            (DataContext as NotificationBalloon).SetVisibility(BalloonVisibility.Hidden);
            e.Handled = true;
        }

        private void closeBalloon()
        {
            closeTimer?.Stop();
            BalloonPopup.IsOpen = false;
        }

        private void startTimer(int timeout)
        {
            closeTimer?.Stop();

            closeTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(timeout)
            };

            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();
        }

        private void CloseTimer_Tick(object sender, System.EventArgs e)
        {
            closeBalloon();
            (DataContext as NotificationBalloon).SetVisibility(BalloonVisibility.TimedOut);
        }

        private void ContentControl_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Prevent taskbar context menu from appearing
            e.Handled = true;
        }

        private void ContentControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            (DataContext as NotificationBalloon).Click();

            closeBalloon();
            e.Handled = true;
        }
    }
}
