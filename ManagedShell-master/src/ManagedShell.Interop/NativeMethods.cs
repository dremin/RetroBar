using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Interop
{
    /// <summary>
    /// Container class for Win32 Native methods used within the desktop application (e.g. shutdown, sleep, et al).
    /// </summary>
    public partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;

            public int Height => Bottom - Top;
        }

        /// <summary>
        /// Used by HSHELL_GETMINRECT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ShortRect
        {
            public ShortRect(short left, short top, short right, short bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public short Left;
            public short Top;
            public short Right;
            public short Bottom;

            public int Width => Right - Left;

            public int Height => Bottom - Top;
        }

        public struct POINT
        {
            public POINT(long x, long y)
            {
                this.x = x;
                this.y = y;
            }
            
            public long x;
            public long y;
        }

        // lo = x; hi = y
        public static int MakeLParam(int loWord, int hiWord)
        {
            return ((short)hiWord << 16) | ((short)loWord & 0xffff);
        }

        public static IntPtr MakeLParamIntPtr(int loWord, int hiWord)
        {
            return new IntPtr(MakeLParam(loWord, hiWord));
        }
    }
}