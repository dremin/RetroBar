using System;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    public class SafeNotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public NIF uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        public string szTip;
        public int dwState;
        public int dwStateMask;
        public string szInfo;
        public uint uVersion;  // used with NIM_SETVERSION, values 0, 3 and 4
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public uint hBalloonIcon;

        public SafeNotifyIconData() { }

        public SafeNotifyIconData(NOTIFYICONDATA nid)
        {
            cbSize = nid.cbSize;
            hWnd = (IntPtr)nid.hWnd;
            uID = nid.uID;
            uFlags = nid.uFlags;
            uCallbackMessage = nid.uCallbackMessage;
            hIcon = (IntPtr)nid.hIcon;
            szTip = nid.szTip;
            dwState = nid.dwState;
            dwStateMask = nid.dwStateMask;
            szInfo = nid.szInfo;
            uVersion = nid.uVersion;
            szInfoTitle = nid.szInfoTitle;
            dwInfoFlags = nid.dwInfoFlags;
            guidItem = nid.guidItem;
            hBalloonIcon = nid.hBalloonIcon;
        }
    }
}
