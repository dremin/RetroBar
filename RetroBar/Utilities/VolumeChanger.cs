using System;
using ManagedShell.Interop;

namespace RetroBar.Utilities
{
    class VolumeChanger
    {
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;

        public static void ChangeVolume(IntPtr hWnd, bool upOrDown)
        {
            NativeMethods.SendMessage(hWnd, (int)NativeMethods.WM.APPCOMMAND, hWnd,
                upOrDown ? (IntPtr)APPCOMMAND_VOLUME_UP : (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }
    }
}