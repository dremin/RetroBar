using System.Windows;
using System.Windows.Controls;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskButton.xaml
    /// </summary>
    public partial class TaskButton : UserControl
    {
        private ApplicationWindow Window;

        public TaskButton()
        {
            InitializeComponent();
        }

        private void TaskButton_OnLoaded(object sender, RoutedEventArgs e)
        {
            Window = DataContext as ApplicationWindow;
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (Window.State == ApplicationWindow.WindowState.Active)
            {
                Window.Minimize();
            }
            else
            {
                Window.BringToFront();
            }
        }

        private void Button_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Window != null)
            {
                NativeMethods.WindowShowStyle wss = Window.ShowStyle;
                int ws = Window.WindowStyles;

                // disable window operations depending on current window state. originally tried implementing via bindings but found there is no notification we get regarding maximized state
                MaximizeMenuItem.IsEnabled = (wss != NativeMethods.WindowShowStyle.ShowMaximized && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0);
                MinimizeMenuItem.IsEnabled = (wss != NativeMethods.WindowShowStyle.ShowMinimized && (ws & (int)NativeMethods.WindowStyles.WS_MINIMIZEBOX) != 0);
                RestoreMenuItem.IsEnabled = (wss != NativeMethods.WindowShowStyle.ShowNormal);
                MoveMenuItem.IsEnabled = wss == NativeMethods.WindowShowStyle.ShowNormal;
                SizeMenuItem.IsEnabled = (wss == NativeMethods.WindowShowStyle.ShowNormal && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0);
            }
        }

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Tasks.Instance.CloseWindow(Window);
        }

        private void RestoreMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Restore();
        }

        private void MoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Move();
        }

        private void SizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Size();
        }

        private void MinimizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Minimize();
        }

        private void MaximizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Maximize();
        }
    }
}
