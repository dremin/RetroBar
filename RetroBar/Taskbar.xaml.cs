#nullable enable
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell;
using ManagedShell.WindowsTray;
using System;
using System.ComponentModel;
using System.Windows;
using ManagedShell.ShellFolders;
using Application = System.Windows.Application;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Taskbar : AppBarWindow
    {
        private ShellManager _shellManager;

        public Taskbar(ShellManager shellManager, AppBarScreen screen, AppBarEdge edge)
            : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen, edge, 0)
        {
            _shellManager = shellManager;

            InitializeComponent();
            DataContext = _shellManager;
            DesiredHeight = Application.Current.FindResource("TaskbarHeight") as double? ?? 0;
            SetupQuickLaunch();

            _explorerHelper.HideExplorerTaskbar = true;
            
            Utilities.Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public override void SetPosition()
        {
            base.SetPosition();

            _shellManager.NotificationArea.SetTrayHostSizeData(new TrayHostSizeData
            {
                edge = (NativeMethods.ABEdge)AppBarEdge,
                rc = new NativeMethods.Rect
                {
                    Top = (int) (Top * DpiScale),
                    Left = (int) (Left * DpiScale),
                    Bottom = (int) ((Top + Height) * DpiScale),
                    Right = (int) ((Left + Width) * DpiScale)
                }
            });
        }
        
        private void SetupQuickLaunch()
        {
            QuickLaunchToolbar.Folder?.Dispose();
            QuickLaunchToolbar.Folder = null;

            if (Utilities.Settings.Instance.ShowQuickLaunch)
            {
                QuickLaunchToolbar.Folder = new ShellFolder(Environment.ExpandEnvironmentVariables(Utilities.Settings.Instance.QuickLaunchPath), IntPtr.Zero, true);
                QuickLaunchToolbar.Visibility = Visibility.Visible;
            }
            else
            {
                QuickLaunchToolbar.Visibility = Visibility.Collapsed;
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                double newHeight = Application.Current.FindResource("TaskbarHeight") as double? ?? 0;
                if (newHeight != DesiredHeight)
                {
                    DesiredHeight = newHeight;
                    SetScreenPosition();
                }
            }
            else if (e.PropertyName == "ShowQuickLaunch" || e.PropertyName == "QuickLaunchPath")
            {
                SetupQuickLaunch();
            }
        }

        private void Taskbar_OnLocationChanged(object? sender, EventArgs e)
        {
            // primarily for win7/8, they will set up the appbar correctly but then put it in the wrong place
            double desiredTop = Screen.Bounds.Bottom / DpiScale - Height;

            if (Top != desiredTop) Top = desiredTop;
        }

        private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ExitGracefully();
        }

        private void TaskManagerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ShellHelper.StartTaskManager();
        }

        private void PropertiesMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            App app = (App)Application.Current;

            new PropertiesWindow(app.ThemeManager).Show();
        }

        protected override void CustomClosing()
        {
            if (AllowClose)
            {
                _explorerHelper.HideExplorerTaskbar = false;
                
                QuickLaunchToolbar.Folder?.Dispose();
            }
        }
    }
}
