using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Interop
{
    public partial class NativeMethods
    {
        public const int DWM_TNP_VISIBLE = 0x8,
            DWM_TNP_OPACITY = 0x4,
            DWM_TNP_RECTDESTINATION = 0x1,
            DWM_TNP_RECTSOURCE = 0x2,
            DWM_TNP_SOURCECLIENTAREAONLY = 0x10;

        const string DwmApi_DllName = "dwmapi";

        [DllImport(DwmApi_DllName)]
        public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport(DwmApi_DllName)]
        public static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport(DwmApi_DllName)]
        public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [DllImport(DwmApi_DllName)]
        public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [DllImport(DwmApi_DllName, EntryPoint = "#113", SetLastError = true)]
        public static extern uint DwmActivateLivePreview(uint enable, IntPtr targetHwnd, IntPtr callingHwnd, AeroPeekType type);

        // Windows 8.1+ version has an extra unknown parameter
        [DllImport(DwmApi_DllName, EntryPoint = "#113", SetLastError = true)]
        public static extern uint DwmActivateLivePreview(uint enable, IntPtr targetHwnd, IntPtr callingHwnd, AeroPeekType type, IntPtr unknown);

        [DllImport(DwmApi_DllName)]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out uint pvAttribute, int cbAttribute);

        [DllImport(DwmApi_DllName)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport(DwmApi_DllName, PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        }

        public enum DWMNCRENDERINGPOLICY
        {
            DWMNCRP_USEWINDOWSTYLE,
            DWMNCRP_DISABLED,
            DWMNCRP_ENABLED,
            DWMNCRP_LAST
        }

        public enum AeroPeekType : uint
        {
            Default = 0,
            Desktop = 1,
            Window = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSIZE
        {
            public int x;
            public int y;
        }
    }
}
