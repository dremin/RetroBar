using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;

namespace RetroBar.Utilities
{
    public class CloakMonitor : IDisposable, INotifyPropertyChanged
    {
        const int EVENT_OBJECT_CLOAKED = 0x8017;

        private IntPtr cloakEventHook;
        private NativeMethods.WinEventProc cloakEventProc;
        private IntPtr hwndStartMenu;

        private bool _isStartMenuCloaked = true;

        public bool IsStartMenuCloaked
        {
            get => _isStartMenuCloaked;
            set
            {
                _isStartMenuCloaked = value;
                OnPropertyChanged();
            }
        }

        public CloakMonitor()
        {
            RegisterEventHooks();
        }

        private void RegisterEventHooks()
        {
            if (!EnvironmentHelper.IsWindows10OrBetter) return;

            hwndStartMenu = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", "Start");

            if (cloakEventHook == IntPtr.Zero)
            {
                cloakEventProc = CloakEventCallback;

                cloakEventHook = NativeMethods.SetWinEventHook(
                    EVENT_OBJECT_CLOAKED,
                    NativeMethods.EVENT_OBJECT_UNCLOAKED,
                    IntPtr.Zero,
                    cloakEventProc,
                    0,
                    0,
                    NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
            }
        }

        private void CloakEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd != hwndStartMenu) return;

            IsStartMenuCloaked = eventType == EVENT_OBJECT_CLOAKED;
        }

        public void Dispose()
        {
            if (cloakEventHook != IntPtr.Zero) NativeMethods.UnhookWinEvent(cloakEventHook);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
