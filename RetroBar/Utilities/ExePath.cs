using System;
using System.Text;

namespace RetroBar.Utilities
{
    class ExePath
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);
        static readonly int MAX_PATH = 260;

        internal static string GetExecutablePath()
        {
            var sb = new StringBuilder(MAX_PATH);
            GetModuleFileName(IntPtr.Zero, sb, MAX_PATH);
            return sb.ToString();
        }
	}
}
