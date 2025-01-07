using System;
using System.Runtime.InteropServices;
using ManagedShell.Common.Helpers;
using static ManagedShell.Interop.NativeMethods;
using Screen = System.Windows.Forms.Screen;

namespace RetroBar.Utilities
{
    internal class ClockFlyoutLauncher
    {
        private const SetWindowPosFlags NoPosFlags = SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOZORDER;

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
    }
}