using System;
using System.Runtime.InteropServices;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.Common.Helpers
{
    public static class WindowHelper
    {
        public const string TrayWndClass = "Shell_TrayWnd";

        public static void ShowWindowBottomMost(IntPtr handle)
        {
            SetWindowPos(
                handle,
                (IntPtr)WindowZOrder.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE/* | SWP_NOZORDER | SWP_NOOWNERZORDER*/);
        }

        public static void ShowWindowTopMost(IntPtr handle)
        {
            SetWindowPos(
                handle,
                (IntPtr)WindowZOrder.HWND_TOPMOST,
                0,
                0,
                0,
                0,
                (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_SHOWWINDOW/* | (int)SetWindowPosFlags.SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOOWNERZORDER*/);
        }

        public static void ShowWindowDesktop(IntPtr hwnd)
        {
            IntPtr desktopHwnd = GetLowestDesktopParentHwnd();

            if (desktopHwnd != IntPtr.Zero)
            {
                IntPtr nextHwnd = GetWindow(desktopHwnd, GetWindow_Cmd.GW_HWNDPREV);
                SetWindowPos(
                    hwnd,
                    nextHwnd,
                    0,
                    0,
                    0,
                    0,
                    (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
            }
            else
            {
                ShowWindowBottomMost(hwnd);
            }
        }

        public static IntPtr GetLowestDesktopParentHwnd()
        {
            IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
            IntPtr desktopHwnd = FindWindowEx(progmanHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (desktopHwnd == IntPtr.Zero)
            {
                IntPtr workerHwnd = IntPtr.Zero;
                IntPtr shellIconsHwnd;
                do
                {
                    workerHwnd = FindWindowEx(IntPtr.Zero, workerHwnd, "WorkerW", null);
                    shellIconsHwnd = FindWindowEx(workerHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                } while (shellIconsHwnd == IntPtr.Zero && workerHwnd != IntPtr.Zero);

                desktopHwnd = workerHwnd;
            }
            else
            {
                desktopHwnd = progmanHwnd;
            }

            return desktopHwnd;
        }

        public static IntPtr GetLowestDesktopChildHwnd()
        {
            IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
            IntPtr desktopHwnd = FindWindowEx(progmanHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (desktopHwnd == IntPtr.Zero)
            {
                IntPtr workerHwnd = IntPtr.Zero;
                IntPtr shellIconsHwnd;
                do
                {
                    workerHwnd = FindWindowEx(IntPtr.Zero, workerHwnd, "WorkerW", null);
                    shellIconsHwnd = FindWindowEx(workerHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                } while (shellIconsHwnd == IntPtr.Zero && workerHwnd != IntPtr.Zero);

                desktopHwnd = shellIconsHwnd;
            }

            return desktopHwnd;
        }
        
        public static void HideWindowFromTasks(IntPtr hWnd)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, (GetWindowLong(hWnd, GWL_EXSTYLE) & ~(int)ExtendedWindowStyles.WS_EX_APPWINDOW) | (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW);

            ExcludeWindowFromPeek(hWnd);
        }

        public static void ExcludeWindowFromPeek(IntPtr hWnd)
        {
            int status = 1;
            DwmSetWindowAttribute(hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK,
                ref status,
                sizeof(int));
        }

        public static void PeekWindow(bool show, IntPtr targetHwnd, IntPtr callingHwnd)
        {
            uint enable = 0;
            if (show) enable = 1;

            if (EnvironmentHelper.IsWindows81OrBetter)
            {
                DwmActivateLivePreview(enable, targetHwnd, callingHwnd, AeroPeekType.Window, IntPtr.Zero);
            }
            else
            {
                DwmActivateLivePreview(enable, targetHwnd, callingHwnd, AeroPeekType.Window);
            }
        }

        public static void SetWindowBlur(IntPtr hWnd, bool enable)
        {
            if (EnvironmentHelper.IsWindows10OrBetter)
            {
                // https://github.com/riverar/sample-win32-acrylicblur
                // License: MIT
                var accent = new AccentPolicy();
                var accentStructSize = Marshal.SizeOf(accent);
                if (enable)
                {
                    if (EnvironmentHelper.IsWindows10RS4OrBetter)
                    {
                        accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                        accent.GradientColor = (0 << 24) | (0xFFFFFF /* BGR */ & 0xFFFFFF);
                    }
                    else
                    {
                        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    }
                }
                else
                {
                    accent.AccentState = AccentState.ACCENT_DISABLED;
                }

                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                SetWindowCompositionAttribute(hWnd, ref data);

                Marshal.FreeHGlobal(accentPtr);
            }
        }

        public static bool SetDarkModePreference(PreferredAppMode mode)
        {
            if (EnvironmentHelper.IsWindows10DarkModeSupported)
            {
                return SetPreferredAppMode(mode);
            }

            return false;
        }

        public static IntPtr FindWindowsTray(IntPtr hwndIgnore)
        {
            IntPtr taskbarHwnd = FindWindow(TrayWndClass, "");

            if (hwndIgnore != IntPtr.Zero)
            {
                while (taskbarHwnd == hwndIgnore)
                {
                    taskbarHwnd = FindWindowEx(IntPtr.Zero, taskbarHwnd, TrayWndClass, "");
                }
            }

            return taskbarHwnd;
        }
    }
}
