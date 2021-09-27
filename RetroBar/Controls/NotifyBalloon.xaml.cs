using ManagedShell.WindowsTray;
using System.Windows;
using System.Windows.Controls;
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
            InitializeComponent();
        }

        public void Show(NotificationBalloonInfo balloonInfo)
        {
            DataContext = balloonInfo;
            BalloonPopup.IsOpen = true;

            startTimer(balloonInfo.Timeout);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            closeBalloon();
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
        }
    }
}
