using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;
using RetroBar.Utilities;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for ShowDesktopButton.xaml
    /// </summary>
    public partial class ShowDesktopButton : UserControl
    {
        private const int TOGGLE_DESKTOP = 407;
        private IntPtr taskbarHandle = Process.GetCurrentProcess().MainWindowHandle;
        private bool isWindows81OrBetter = EnvironmentHelper.IsWindows81OrBetter;
        private bool isLoaded;
        private DelayedActivationHandler dragHandler;

        public static DependencyProperty TasksServiceProperty = DependencyProperty.Register(nameof(TasksService), typeof(TasksService), typeof(ShowDesktopButton), new PropertyMetadata(TasksChangedCallback));

        public TasksService TasksService
        {
            get { return (TasksService)GetValue(TasksServiceProperty); }
            set { SetValue(TasksServiceProperty, value); }
        }

        public ShowDesktopButton()
        {
            InitializeComponent();
        }

        private static void TasksChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ShowDesktopButton button && e.OldValue == null && e.NewValue != null)
            {
                button.SetupButton();
            }
        }

        private void SetIconSize()
        {
            if (DpiHelper.DpiScale > 1 || Settings.Instance.TaskbarScale > 1)
            {
                ShowDesktopIcon.Source = (System.Windows.Media.ImageSource)FindResource("ShowDesktopIconImageLarge");
            }
            else
            {
                ShowDesktopIcon.Source = (System.Windows.Media.ImageSource)FindResource("ShowDesktopIconImageSmall");
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            PeekAtDesktopItem.IsEnabled = true;
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                PeekAtDesktopItem.IsEnabled = false;
            }
        }

        private void ToggleDesktop()
        {
            NativeMethods.SendMessage(WindowHelper.FindWindowsTray(IntPtr.Zero),
                (int)NativeMethods.WM.COMMAND, (IntPtr)TOGGLE_DESKTOP, IntPtr.Zero);
        }

        private void PeekAtDesktop(uint shouldPeek)
        {
            if (Settings.Instance.PeekAtDesktop && NativeMethods.DwmIsCompositionEnabled())
            {
                if (isWindows81OrBetter)
                {
                    NativeMethods.DwmActivateLivePreview(shouldPeek, taskbarHandle,
                        IntPtr.Zero, NativeMethods.AeroPeekType.Desktop, IntPtr.Zero);
                }
                else
                {
                    NativeMethods.DwmActivateLivePreview(shouldPeek, taskbarHandle,
                        IntPtr.Zero, NativeMethods.AeroPeekType.Desktop);
                }
            }
        }

        private void ShowDesktop_OnMouseEnter(object sender, RoutedEventArgs e)
        {
            PeekAtDesktop(1);
        }

        private void ShowDesktop_OnMouseLeave(object sender, RoutedEventArgs e)
        {
            PeekAtDesktop(0);
        }

        private void ShowDesktop_OnClick(object sender, RoutedEventArgs e)
        {
            // If the user activates a window other than the desktop, HandleWindowActivated will deselect the button.
            ToggleDesktop();
        }

        private void OpenDisplayPropertiesCpl()
        {
            ShellHelper.StartProcess("desk.cpl");
        }

        private void PropertiesItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenDisplayPropertiesCpl();
        }

        private void HandleWindowActivated(object sender, WindowEventArgs e)
        {
            if (ShowDesktop.IsChecked == true)
            {
                ShowDesktop.IsChecked = false;
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.TaskbarScale))
            {
                SetIconSize();
            }
        }

        private void SetupButton()
        {
            if (!isLoaded && TasksService != null)
            {
                SetIconSize();
                TasksService.WindowActivated += HandleWindowActivated;

                Settings.Instance.PropertyChanged += Settings_PropertyChanged;

                dragHandler = new DelayedActivationHandler(() =>
                {
                    if (ShowDesktop.IsChecked == false)
                    {
                        ToggleDesktop();
                        ShowDesktop.IsChecked = true;
                    }
                });

                isLoaded = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetupButton();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                if (TasksService != null)
                {
                    TasksService.WindowActivated -= HandleWindowActivated;
                }

                Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
                dragHandler?.Dispose();
                isLoaded = false;
            }
        }

        private void ShowDesktop_DragEnter(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragEnter(e);
        }

        private void ShowDesktop_DragLeave(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragLeave();
        }
    }
}