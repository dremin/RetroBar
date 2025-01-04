using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using static ManagedShell.Interop.NativeMethods;
using Rect = ManagedShell.Interop.NativeMethods.Rect;
using Screen = System.Windows.Forms.Screen;

namespace RetroBar.Utilities
{
    internal class ClockFlyoutLauncher
    {
        private const SetWindowPosFlags NoPosFlags = SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOZORDER;

        #region Aero clock

        private static readonly Lazy<Type> AeroClockType = new(() => Type.GetTypeFromCLSID(new("A323554A-0FE1-4E49-AEE1-6722465D799F")));
        private static object _aeroClockInstance;

        [ComImport, Guid("7A5FCA8A-76B1-44C8-A97C-E7173CCA5F4F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAeroClock // For 8+
        {
            [PreserveSig]
            void ShowFlyout(IntPtr hWnd, ref Rect lpRect);
        }

        [ComImport, Guid("4376DF10-A662-420B-B30D-958881461EF9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAeroClockLegacy // For Vista and 7
        {
            [PreserveSig]
            void ShowFlyout(int unk, ref Rect lpRect);
        }

        internal static void ShowAeroClockFlyout(IntPtr taskbarHwnd)
        {
            Screen taskbarScreen = Screen.FromHandle(taskbarHwnd);
            if (!GetWindowRect(taskbarHwnd, out Rect lpRect))
            {
                return;
            }

            _aeroClockInstance ??= Activator.CreateInstance(AeroClockType.Value);

            if (EnvironmentHelper.IsWindows8OrBetter)
            {
                ((IAeroClock)_aeroClockInstance).ShowFlyout(taskbarHwnd, ref lpRect);
            }
            else
            {
                ((IAeroClockLegacy)_aeroClockInstance).ShowFlyout(0, ref lpRect);
            }

            FixAeroClockFlyoutPosition(taskbarScreen);
        }

        private static void FixAeroClockFlyoutPosition(Screen taskbarScreen)
        {
            IntPtr clockFlyoutHwnd = FindWindow("ClockFlyoutWindow", null);
            if (clockFlyoutHwnd == IntPtr.Zero)
            {
                return;
            }

            if (!GetWindowRect(clockFlyoutHwnd, out Rect rect))
            {
                return;
            }

            var wa = taskbarScreen.WorkingArea;
            int newX = rect.Left, newY = rect.Top;

            // Max margin (as used in Windows 7)
            int snap = 15;
            // Vista margin
            int margin = 7;

            // Move to closest edge if the flyout is too close to the screen edge
            if (Math.Abs(newX - wa.Left) <= snap) // Left
            {
                newX = wa.Left + margin;
            }
            else if (Math.Abs(rect.Right - wa.Right) <= snap) // Right
            {
                newX = wa.Right - rect.Width - margin;
            }

            if (Math.Abs(newY - wa.Top) <= snap) // Top
            {
                newY = wa.Top + margin;
            }
            else if (Math.Abs(rect.Bottom - wa.Bottom) <= snap) // Bottom
            {
                newY = wa.Bottom - rect.Height - margin;
            }

            // Keep the flyout inside the working area
            newX = Math.Max(wa.Left + margin, Math.Min(newX, wa.Right - rect.Width - margin));
            newY = Math.Max(wa.Top + margin, Math.Min(newY, wa.Bottom - rect.Height - margin));

            SetWindowPos(clockFlyoutHwnd, IntPtr.Zero, newX, newY, 0, 0, (int)NoPosFlags);
        }

        #endregion Aero clock

        #region Modern clock

        internal static void ShowClockFlyout()
        {
            // Current implementation is for the primary screen only
            Screen screen = Screen.PrimaryScreen;
            FlowDirection flowDirection = Application.Current.FindResource("flow_direction") as FlowDirection? ?? FlowDirection.LeftToRight;

            PostMessage(WindowHelper.FindWindowsTray(WindowHelper.FindWindowsTray(IntPtr.Zero)), (uint)0x5CE, IntPtr.Zero, IntPtr.Zero);

            // Launching the Modern clock is slower than the Aero clock, so we need to wait for the window to be created...
            System.Windows.Threading.DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromTicks(10)
            };

            int attempts = 0;
            StringBuilder className = new(256);

            timer.Tick += (_, _) =>
            {
                if (++attempts > 100)
                {
                    timer.Stop();
                    return;
                }

                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                className.Clear();
                _ = GetClassName(hwnd, className, className.Capacity);
                if (className.ToString() != "Windows.UI.Core.CoreWindow")
                {
                    return;
                }

                ShowWindow(hwnd, (int)WindowShowStyle.Hide);

                if (!GetWindowRect(hwnd, out Rect rect) || rect.Width <= 1)
                {
                    return;
                }

                timer.Stop();

                // Move to corner where the taskbar is
                int newX = Settings.Instance.Edge switch
                {
                    AppBarEdge.Left => screen.WorkingArea.Left,
                    AppBarEdge.Right => screen.WorkingArea.Right - rect.Width,
                    _ => (flowDirection == FlowDirection.LeftToRight) ? screen.WorkingArea.Right - rect.Width : screen.WorkingArea.Left
                };

                int newY = Settings.Instance.Edge switch
                {
                    AppBarEdge.Top => screen.WorkingArea.Top,
                    _ => screen.WorkingArea.Bottom - rect.Height
                };

                SetWindowPos(hwnd, IntPtr.Zero, newX, newY, 0, 0, (int)(NoPosFlags | SetWindowPosFlags.SWP_SHOWWINDOW));
            };

            timer.Start();
        }
    }

    #endregion Modern clock
}