using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for FloatingStartButton.xaml
    /// </summary>
    public partial class FloatingStartButton : Window, INotifyPropertyChanged
    {
        private WindowInteropHelper helper;
        private IntPtr handle;
        private NativeMethods.Rect startupRect;

        private StartButton MainButton => (StartButton)DataContext;

        public bool IsScaled => MainButton.Host.IsScaled;
        public int Rows => MainButton.Host.Rows;
        public AppBarEdge AppBarEdge => MainButton.Host.AppBarEdge;
        public Orientation Orientation => MainButton.Host.Orientation;

        public event PropertyChangedEventHandler PropertyChanged;

        public FloatingStartButton(StartButton mainButton, NativeMethods.Rect rect)
        {
            Owner = mainButton.Host;
            DataContext = mainButton;

            InitializeComponent();
            startupRect = rect;

            // Bind IsChecked directly to the source ToggleButton object, 
            // since we cannot bind to it in XAML because 'Start' is an internal field.
            BindingOperations.SetBinding(Start, ToggleButton.IsCheckedProperty, new Binding(nameof(ToggleButton.IsChecked)) { Source = mainButton.Start, Mode = BindingMode.TwoWay });

            if (mainButton.Host != null)
            {
                mainButton.Host.PropertyChanged += Host_PropertyChanged;
            }
        }

        private void Host_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // set up helper and get handle
            helper = new WindowInteropHelper(this);
            handle = helper.Handle;

            // set up window procedure
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(WndProc);

            // Hide from taskbar
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.WindowLongFlags.GWL_EXSTYLE, (NativeMethods.GetWindowLong(helper.Handle, NativeMethods.WindowLongFlags.GWL_EXSTYLE) & ~(int)NativeMethods.ExtendedWindowStyles.WS_EX_APPWINDOW) | (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW);

            WindowHelper.ExcludeWindowFromPeek(helper.Handle);

            SetPosition(startupRect);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Prevent window activation on click so the taskbar/start menu doesn't lose focus
            if (msg == (int)NativeMethods.WM.MOUSEACTIVATE)
            {
                handled = true;
                return (IntPtr)NativeMethods.MA_NOACTIVATE;
            }

            if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING)
            {
                // Extract the WINDOWPOS structure corresponding to this message
                NativeMethods.WINDOWPOS wndPos = NativeMethods.WINDOWPOS.FromMessage(lParam);

                // WORKAROUND WPF bug: https://github.com/dotnet/wpf/issues/7561
                // If there is no NOMOVE or NOSIZE or NOACTIVATE flag, and there is a NOZORDER flag, add the NOACTIVATE flag
                if ((wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOMOVE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOSIZE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE) == 0 &&
                    (wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOZORDER) != 0)
                {
                    wndPos.flags |= NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
                    wndPos.UpdateMessage(lParam);
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            MainButton.Start_OnClick(sender, e);
        }

        private void Start_DragEnter(object sender, DragEventArgs e)
        {
            MainButton.Start_DragEnter(sender, e);
        }

        private void Start_DragLeave(object sender, DragEventArgs e)
        {
            MainButton.Start_DragLeave(sender, e);
        }

        private void Start_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainButton.Start_OnPreviewMouseLeftButtonDown(sender, e);
        }

        private void Start_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            MainButton.Start_OnMouseRightButtonUp(sender, e);
        }

        internal void SetPosition(NativeMethods.Rect rect)
        {
            startupRect = rect;
            NativeMethods.Rect currentRect;
            NativeMethods.GetWindowRect(handle, out currentRect);

            if (rect.Left == currentRect.Left && rect.Top == currentRect.Top && rect.Right == currentRect.Right && rect.Bottom == currentRect.Bottom)
            {
                return;
            }

            int swp = (int)NativeMethods.SetWindowPosFlags.SWP_NOZORDER | (int)NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE;
            NativeMethods.SetWindowPos(handle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, swp);
        }
    }
}
