using System;
using System.Runtime.InteropServices;

namespace ManagedShell.WindowsTray
{
    [ComImport]
    [Guid("25DEAD04-1EAC-4911-9E3A-AD0A4AB560FD")]
    class TrayNotify
    {
    }

    enum NOTIFYITEM_PREFERENCE
    {
        PREFERENCE_SHOW_WHEN_ACTIVE = 0,
        PREFERENCE_SHOW_NEVER = 1,
        PREFERENCE_SHOW_ALWAYS = 2
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYITEM
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszExeName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszIconText;
        public IntPtr hIcon;
        public IntPtr hWnd;
        public NOTIFYITEM_PREFERENCE dwUserPref;
        public uint uID;
        public Guid guidItem;
    };

    [ComImport]
    [Guid("D782CCBA-AFB0-43F1-94DB-FDA3779EACCB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INotificationCB
    {
        void Notify([In] uint nEvent, [In] ref NOTIFYITEM notifyItem);
    }

    [ComImport]
    [Guid("FB852B2C-6BAD-4605-9551-F15F87830935")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITrayNotifyLegacy
    {
        void RegisterCallback([MarshalAs(UnmanagedType.Interface)] INotificationCB callback);
        void SetPreference([In] ref NOTIFYITEM notifyItem);
        void EnableAutoTray([In] bool enabled);
    }

    [ComImport]
    [Guid("D133CE13-3537-48BA-93A7-AFCD5D2053B4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITrayNotify
    {
        void RegisterCallback([MarshalAs(UnmanagedType.Interface)] INotificationCB callback, [Out] out ulong handle);
        void UnregisterCallback([In] ulong handle);
        void SetPreference([In] ref NOTIFYITEM notifyItem);
        void EnableAutoTray([In] bool enabled);
        void DoAction([In] bool enabled);
    }
}