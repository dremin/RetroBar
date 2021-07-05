using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for StartButton.xaml
    /// </summary>
    public partial class StartButton : UserControl
    {
        private bool allowOpenStart;
        private readonly DispatcherTimer pendingOpenTimer;

        public StartButton()
        {
            InitializeComponent();

            pendingOpenTimer = new DispatcherTimer(DispatcherPriority.Background);
            pendingOpenTimer.Interval = new TimeSpan(0, 0, 0, 1);
            pendingOpenTimer.Tick += (sender, args) =>
            {
                // if the start menu didn't open, flip the button back to unchecked
                Start.IsChecked = false;
                pendingOpenTimer.Stop();
            };
        }

        public void SetStartMenuState(bool opened)
        {
            Start.IsChecked = opened;
            pendingOpenTimer.Stop();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (allowOpenStart)
            {
                pendingOpenTimer.Start();
                ShellHelper.ShowStartMenu();
                return;
            }

            Start.IsChecked = false;
        }

        private void Explore_OnClick(object sender, RoutedEventArgs e)
        {
            ShellHelper.StartProcess("explorer.exe");
        }

        private void Start_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            allowOpenStart = Start.IsChecked == false;
        }
    }
}
