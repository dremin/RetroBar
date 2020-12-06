#nullable enable
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
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
            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        internal override void SetPosition()
        {
            Left = Screen.Bounds.Left / dpiScale;
            Width = Screen.Bounds.Width / dpiScale;
            Height = desiredHeight;
            Top = Screen.Bounds.Bottom / dpiScale - Height;

            NotificationArea.Instance.SetTrayHostSizeData(new TrayHostSizeData { edge = (int)appBarEdge, rc = new NativeMethods.Rect { Top = (int)(Top * dpiScale), Left = (int)(Left * dpiScale), Bottom = (int)((Top + Height) * dpiScale), Right = (int)((Left + Width) * dpiScale) } });
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                double newHeight = Application.Current.FindResource("TaskbarHeight") as double? ?? 0;
                if (newHeight != desiredHeight)
                {
                    desiredHeight = newHeight;
                    SetScreenPosition();
                }
            }
        }

        private void Taskbar_OnLocationChanged(object? sender, EventArgs e)
        {
            // primarily for win7/8, they will set up the appbar correctly but then put it in the wrong place
            double desiredTop = Screen.Bounds.Bottom / dpiScale - Height;

            if (Top != desiredTop) Top = desiredTop;
        }

        private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TaskManagerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Shell.StartTaskManager();
        }

        private void PropertiesMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            PropertiesWindow.Instance.Show();
        }
    }
}
