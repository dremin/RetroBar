using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RetroBar.Utilities
{
    /// <summary>Temporary diagnostics for the Vista start orb. Writes to %TEMP%\RetroBar-orb.log.</summary>
    internal static class OrbLog
    {
        private static readonly object sync = new object();
        private static readonly string path = Path.Combine(Path.GetTempPath(), "RetroBar-orb.log");

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public static void Write(string message)
        {
            try
            {
                lock (sync)
                {
                    File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
                }
            }
            catch
            {
                // diagnostics must never break the app
            }
        }

        public static string Describe(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return "hwnd=0";
                GetWindowRect(hwnd, out RECT r);
                return "hwnd=0x" + hwnd.ToInt64().ToString("X") + " win32visible=" + IsWindowVisible(hwnd) +
                       " rect=(" + r.Left + "," + r.Top + "," + r.Right + "," + r.Bottom + ") size=" + (r.Right - r.Left) + "x" + (r.Bottom - r.Top);
            }
            catch (Exception ex)
            {
                return "describe failed: " + ex.Message;
            }
        }
    }
}
