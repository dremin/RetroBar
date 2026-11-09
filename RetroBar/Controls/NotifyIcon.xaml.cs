using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell.WindowsTray;
using RetroBar.Extensions;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for NotifyIcon.xaml
    /// </summary>
    public partial class NotifyIcon : UserControl
    {
        private bool isLoaded;
        private ManagedShell.WindowsTray.NotifyIcon TrayIcon;

        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(Taskbar), typeof(NotifyIcon));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public NotifyIcon()
        {
            InitializeComponent();
        }

        private void applyEffects()
        {
            if (TrayIcon == null || Settings.Instance.InvertIconsMode == InvertIconsOption.Never)
            {
                return;
            }

            if (!TrayIcon.CanInvert())
            {
                return;
            }

            bool invertByTheme = Application.Current.FindResource("InvertSystemNotifyIcons") as bool? ?? false;
            bool performInvert = invertByTheme || Settings.Instance.InvertIconsMode == InvertIconsOption.Always;

            if (NotifyIconImage.Effect == null != performInvert)
            {
                return;
            }

            if (performInvert)
            {
                NotifyIconImage.Effect = new InvertEffect();
            }
            else
            {
                NotifyIconImage.Effect = null;
            }
        }

        private void NotifyIcon_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                TrayIcon = DataContext as ManagedShell.WindowsTray.NotifyIcon;

                if (TrayIcon == null)
                {
                    return;
                }

                applyEffects();

                TrayIcon.NotificationBalloonShown += TrayIcon_NotificationBalloonShown;

                // If a notification was received before we started listening, it will be here. Show the first one that is not expired.
                NotificationBalloon firstUnexpiredNotification = TrayIcon.MissedNotifications.FirstOrDefault(balloon => balloon.Received.AddMilliseconds(balloon.Timeout) > DateTime.Now);

                if (firstUnexpiredNotification != null && Host != null && Host.Screen.Primary)
                {
                    BalloonControl.Show(firstUnexpiredNotification, NotifyIconBorder);
                    TrayIcon.MissedNotifications.Remove(firstUnexpiredNotification);
                }

                isLoaded = true;
            }
        }

        private void NotifyIcon_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (TrayIcon != null)
            {
                TrayIcon.NotificationBalloonShown -= TrayIcon_NotificationBalloonShown;
            }
            isLoaded = false;
        }

        private void TrayIcon_NotificationBalloonShown(object sender, NotificationBalloonEventArgs e)
        {
            if (Host == null || !Host.Screen.Primary)
            {
                return;
            }

            if (TrayIcon == null || TrayIcon.GetBehavior() == NotifyIconBehavior.AlwaysHide || TrayIcon.GetBehavior() == NotifyIconBehavior.Remove)
            {
                return;
            }

            if (NotifyIconBorder.DataContext != e.Balloon.NotifyIcon)
            {
                return;
            }

            BalloonControl.Show(e.Balloon, NotifyIconBorder);
            e.Handled = true;
        }

        private void NotifyIcon_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Host?.SetTrayHost();
            if (e.ChangedButton != MouseButton.Left)
            {
                TrayIcon?.IconMouseDown(e.ChangedButton, MouseHelper.GetCursorPositionParam(), System.Windows.Forms.SystemInformation.DoubleClickTime);
            }
        }

        private async void NotifyIcon_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Settings.Instance.UseClassicVolumeFlyout && TrayIcon?.GUID.ToString() == NotificationArea.VOLUME_GUID && Host != null)
                {
                    Point topLeft = NotifyIconBorder.PointToScreen(new Point(0, 0));
                    Point bottomRight = NotifyIconBorder.PointToScreen(new Point(NotifyIconBorder.ActualWidth, NotifyIconBorder.ActualHeight));
                    NativeMethods.Rect icon = new((int)topLeft.X, (int)topLeft.Y, (int)bottomRight.X, (int)bottomRight.Y);
                    if (await ClassicVolumeFlyout.ToggleAsync(icon, Host.AppBarEdge)) return;
                }

                TrayIcon?.IconMouseDown(e.ChangedButton, MouseHelper.GetCursorPositionParam(), System.Windows.Forms.SystemInformation.DoubleClickTime);
            }
            TrayIcon?.IconMouseUp(e.ChangedButton, MouseHelper.GetCursorPositionParam(), System.Windows.Forms.SystemInformation.DoubleClickTime);
        }

        private void NotifyIcon_OnMouseEnter(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            if (TrayIcon != null)
            {
                // update icon position for Shell_NotifyIconGetRect
                Decorator sendingDecorator = sender as Decorator;
                Point location = sendingDecorator.PointToScreen(new Point(0, 0));
                double dpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;

                TrayIcon.Placement = new NativeMethods.Rect { Top = (int)location.Y, Left = (int)location.X, Bottom = (int)(sendingDecorator.ActualHeight * dpiScale), Right = (int)(sendingDecorator.ActualWidth * dpiScale) };
                TrayIcon.IconMouseEnter(MouseHelper.GetCursorPositionParam());
            }
        }

        private void NotifyIcon_OnMouseLeave(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            TrayIcon?.IconMouseLeave(MouseHelper.GetCursorPositionParam());
        }

        private void NotifyIcon_OnMouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            TrayIcon?.IconMouseMove(MouseHelper.GetCursorPositionParam());
        }

        private bool HandleNotificationIconMouseWheel(bool upOrDown)
        {
            switch (TrayIcon?.GUID.ToString())
            {
                case NotificationArea.VOLUME_GUID:
                    VolumeChanger.ChangeVolume(WindowHelper.FindWindowsTray(IntPtr.Zero), upOrDown);
                    return true;
                default:
                    return false;
            }
        }

        private void NotifyIcon_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleNotificationIconMouseWheel(e.Delta > 0);
            e.Handled = true;
        }
    }
}

namespace RetroBar.Utilities
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using ManagedShell.AppBar;
    using static ManagedShell.Interop.NativeMethods;
    using Rect = ManagedShell.Interop.NativeMethods.Rect;
    using Screen = System.Windows.Forms.Screen;

    internal static class ClassicVolumeFlyout
    {
        private static IntPtr _window;
        private static uint _processId;
        private static bool _opening;
        private static bool _activated;
        private static Rect _icon;
        private static LowLevelMouseHook _mouseHook;
        private static readonly DispatcherTimer Timer = new() { Interval = TimeSpan.FromMilliseconds(100) };

        static ClassicVolumeFlyout()
        {
            Application.Current.Exit += (_, _) => Close();
            Timer.Tick += (_, _) =>
            {
                if (!IsOwnedWindow())
                {
                    StopWatching();
                    return;
                }

                if (!Settings.Instance.UseClassicVolumeFlyout)
                {
                    Close();
                    return;
                }

                IntPtr foreground = GetForegroundWindow();
                GetWindowThreadProcessId(_window, out uint processId);
                GetWindowThreadProcessId(foreground, out uint foregroundProcessId);
                if (processId == foregroundProcessId)
                {
                    _activated = true;
                }
                else if (_activated)
                {
                    Close();
                }
            };
        }

        internal static async Task<bool> ToggleAsync(Rect icon, AppBarEdge edge)
        {
            if (_opening) return true;
            if (IsOwnedWindow())
            {
                Close();
                return true;
            }

            StopWatching();
            _opening = true;
            Process process = null;
            try
            {
                // Match the native launch convention: MAKELONG(cursor.x, cursor.y), as a DWORD.
                if (!GetCursorPos(out POINT cursor)) return false;
                uint coordinates = unchecked(((uint)cursor.y & 0xffff) << 16 | ((uint)cursor.x & 0xffff));
                process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "sndvol.exe"), "-f " + coordinates)
                {
                    UseShellExecute = false
                });
                if (process == null) return false;

                IntPtr window = IntPtr.Zero;
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    if (process.HasExited) break;
                    window = FindSliderWindow((uint)process.Id);
                    if (window != IntPtr.Zero) break;
                    window = IntPtr.Zero;
                    await Task.Delay(50);
                }

                if (window == IntPtr.Zero) return false;
                _window = window;
                _processId = (uint)process.Id;
                _icon = icon;
                // sndvol can finish its initial layout after the first visible HWND appears.
                // Reposition only the visible dialog, not Process.MainWindowHandle (which may be an owner).
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    if (!IsOwnedWindow() || !Settings.Instance.UseClassicVolumeFlyout)
                    {
                        Close();
                        return true;
                    }
                    Position(icon, edge);
                    if (attempt < 3) await Task.Delay(100);
                }
                SetForegroundWindow(_window);
                _activated = false;
                _mouseHook = new LowLevelMouseHook();
                _mouseHook.LowLevelMouseEvent += OnMouse;
                _mouseHook.Initialize();
                Timer.Start();
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            finally
            {
                process?.Dispose();
                _opening = false;
            }
        }

        private static IntPtr FindSliderWindow(uint processId)
        {
            IntPtr slider = IntPtr.Zero;
            EnumWindows((window, _) =>
            {
                GetWindowThreadProcessId(window, out uint ownerId);
                if (ownerId != processId || !IsWindowVisible(window)) return true;
                var className = new System.Text.StringBuilder(256);
                GetClassName(window, className, className.Capacity);
                if (className.ToString() != "#32770") return true;
                if (!GetWindowRect(window, out Rect rect) || rect.Right <= rect.Left || rect.Bottom <= rect.Top) return true;
                slider = window;
                return false;
            }, 0);
            return slider;
        }

        private static void Position(Rect icon, AppBarEdge edge)
        {
            if (!GetWindowRect(_window, out Rect rect)) return;
            var area = Screen.FromRectangle(new System.Drawing.Rectangle(icon.Left, icon.Top,
                Math.Max(1, icon.Right - icon.Left), Math.Max(1, icon.Bottom - icon.Top))).WorkingArea;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            // Align the flyout's center with the clicked icon, not its right edge.
            int x = icon.Left + ((icon.Right - icon.Left) - width) / 2;
            // Match the Aero clock flyout's seven-pixel margin from the taskbar.
            const int margin = 7;
            int y = icon.Top - height - margin;
            switch (edge)
            {
                case AppBarEdge.Top: y = icon.Bottom + margin; break;
                case AppBarEdge.Left: x = icon.Right; y = icon.Top + ((icon.Bottom - icon.Top) - height) / 2; break;
                case AppBarEdge.Right: x = icon.Left - width; y = icon.Top + ((icon.Bottom - icon.Top) - height) / 2; break;
            }
            x = Math.Max(area.Left, Math.Min(x, area.Right - width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - height));
            SetWindowPos(_window, IntPtr.Zero, x, y, 0, 0,
                (int)(SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER));
        }

        private static void OnMouse(object sender, LowLevelMouseHook.LowLevelMouseEventArgs e)
        {
            if (e.Message != WM.LBUTTONDOWN && e.Message != WM.RBUTTONDOWN && e.Message != WM.MBUTTONDOWN) return;
            int x = e.HookStruct.pt.X;
            int y = e.HookStruct.pt.Y;
            // Leave the icon click to ToggleAsync; never consume another application's input.
            if (Contains(_icon, x, y)) return;
            if (GetWindowRect(_window, out Rect rect) && Contains(rect, x, y)) return;
            Application.Current.Dispatcher.BeginInvoke(new Action(Close));
        }

        private static bool Contains(Rect rect, int x, int y)
        {
            return x >= rect.Left && x < rect.Right && y >= rect.Top && y < rect.Bottom;
        }

        private static bool IsOwnedWindow()
        {
            if (_window == IntPtr.Zero || !IsWindow(_window)) return false;
            GetWindowThreadProcessId(_window, out uint processId);
            return processId == _processId;
        }

        private static void Close()
        {
            if (IsOwnedWindow()) PostMessage(_window, (uint)WM.CLOSE, IntPtr.Zero, IntPtr.Zero);
            StopWatching();
        }

        private static void StopWatching()
        {
            Timer.Stop();
            _mouseHook?.Dispose();
            _mouseHook = null;
            _window = IntPtr.Zero;
            _processId = 0;
            _activated = false;
        }
    }
}
