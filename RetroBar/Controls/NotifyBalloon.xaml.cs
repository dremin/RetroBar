using ManagedShell.WindowsTray;
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
            CustomPopupPlacement placement = new CustomPopupPlacement(new Point((popupSize.Width * -1) + offset.X,
                (popupSize.Height * -1) + offset.Y),
                PopupPrimaryAxis.Horizontal);
            CustomPopupPlacement placement2 = new CustomPopupPlacement(new Point(offset.X,
                (popupSize.Height * -1) + offset.Y),
                PopupPrimaryAxis.Horizontal);
            CustomPopupPlacement placement3 = new CustomPopupPlacement(new Point((popupSize.Width * -1) + offset.X,
                targetSize.Height + offset.Y),
                PopupPrimaryAxis.Horizontal);

            return new CustomPopupPlacement[] { placement, placement2, placement3 };
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
