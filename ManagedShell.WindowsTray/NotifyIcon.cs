using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Input;
using static ManagedShell.Interop.NativeMethods;
using ManagedShell.Common.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ManagedShell.WindowsTray
{
    /// <summary>
    /// NotifyIcon class representing a notification area icon.
    /// </summary>
    public class NotifyIcon : IEquatable<NotifyIcon>, INotifyPropertyChanged
    {
        private readonly NotificationArea _notificationArea;


        /// <summary>
        /// Initializes a new instance of the TrayIcon class with no hwnd.
        /// </summary>
        public NotifyIcon(NotificationArea notificationArea) : this(notificationArea, IntPtr.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TrayIcon class with the specified hWnd.
        /// </summary>
        /// <param name="hWnd">The window handle of the icon.</param>
        public NotifyIcon(NotificationArea notificationArea, IntPtr hWnd)
        {
            _notificationArea = notificationArea;
            HWnd = hWnd;
        }

        private ImageSource _icon;

        /// <summary>
        /// Gets or sets the Icon's image.
        /// </summary>
        public ImageSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        private string _title;

        /// <summary>
        /// Gets or sets the Icon's title (tool tip).
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the path to the application that created the icon.
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        private bool _isPinned;

        /// <summary>
        /// Gets or sets whether or not the icon is pinned.
        /// </summary>
        public bool IsPinned
        {
            get
            {
                return _isPinned;
            }
            private set
            {
                _isPinned = value;
                OnPropertyChanged();
            }
        }

        private bool _isHidden;

        /// <summary>
        /// Gets or sets whether or not the icon is hidden.
        /// </summary>
        public bool IsHidden
        {
            get
            {
                return _isHidden;
            }
            set
            {
                _isHidden = value;
                OnPropertyChanged();
            }
        }

        private int _pinOrder;

        /// <summary>
        /// Gets or sets the order index of the item in the pinned icons
        /// </summary>
        public int PinOrder
        {
            get
            {
                return _pinOrder;
            }
            private set
            {
                _pinOrder = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the owners handle.
        /// </summary>
        public IntPtr HWnd
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the callback message id.
        /// </summary>
        public uint CallbackMessage
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the UID of the Icon.
        /// </summary>
        public uint UID
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the GUID of the Icon.
        /// </summary>
        public Guid GUID
        {
            get;
            set;
        }

        public uint Version
        {
            get;
            set;
        }

        public Rect Placement
        {
            get;
            set;
        }

        private string Identifier
        {
            get
            {
                if (GUID != default) return GUID.ToString();
                else return Path + ":" + UID.ToString();
            }
        }

        #region Pinning

        public void Pin()
        {
            Pin(_notificationArea.PinnedNotifyIcons.Length);
        }
        
        public void Pin(int position)
        {
            bool updated = false;

            if (IsPinned && position != PinOrder)
            {
                // already pinned, just moving
                List<string> icons = _notificationArea.PinnedNotifyIcons.ToList();
                icons.Remove(Identifier);
                icons.Insert(position, Identifier);
                _notificationArea.PinnedNotifyIcons = icons.ToArray();
                updated = true;
            }
            else if (!IsPinned)
            {
                // newly pinned. welcome to the party!
                List<string> icons = _notificationArea.PinnedNotifyIcons.ToList();
                icons.Insert(position, Identifier);
                _notificationArea.PinnedNotifyIcons = icons.ToArray();
                updated = true;
            }

            if (updated)
            {
                _notificationArea.UpdatePinnedIcons();
            }
        }

        public void Unpin()
        {
            if (IsPinned)
            {
                List<string> icons = _notificationArea.PinnedNotifyIcons.ToList();
                icons.Remove(Identifier);
                _notificationArea.PinnedNotifyIcons = icons.ToArray();

                IsPinned = false;
                PinOrder = 0;

                _notificationArea.UpdatePinnedIcons();
            }
        }

        public void SetPinValues()
        {
            for (int i = 0; i < _notificationArea.PinnedNotifyIcons.Length; i++)
            {
                string item = _notificationArea.PinnedNotifyIcons[i].ToLower();
                if (item == GUID.ToString().ToLower() || (GUID == default && item == (Path.ToLower() + ":" + UID.ToString())))
                {
                    IsPinned = true;
                    PinOrder = i;
                    break;
                }
            }
        }
        #endregion

        #region Mouse events

        private DateTime _lastLClick = DateTime.Now;
        private DateTime _lastRClick = DateTime.Now;

        public void IconMouseEnter(uint mouse)
        {
            if (RemoveIfInvalid()) return;

            uint wparam = GetMessageWParam(mouse);
            uint hiWord = GetMessageHiWord();

            SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.MOUSEHOVER | (hiWord << 16));

            if (Version > 3)
                SendNotifyMessage(HWnd, CallbackMessage, wparam, NIN_POPUPOPEN | (hiWord << 16));
        }

        public void IconMouseLeave(uint mouse)
        {
            if (RemoveIfInvalid()) return;

            uint wparam = GetMessageWParam(mouse);
            uint hiWord = GetMessageHiWord();

            SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.MOUSELEAVE | (hiWord << 16));

            if (Version > 3)
                SendNotifyMessage(HWnd, CallbackMessage, wparam, NIN_POPUPCLOSE | (hiWord << 16));
        }

        public void IconMouseMove(uint mouse)
        {
            if (RemoveIfInvalid()) return;

            uint wparam = GetMessageWParam(mouse);
            uint hiWord = GetMessageHiWord();

            SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.MOUSEMOVE | (hiWord << 16));
        }

        public void IconMouseClick(MouseButton button, uint mouse, int doubleClickTime)
        {
            ShellLogger.Debug(string.Format("{0} mouse button clicked icon: {1}", button.ToString(), Title));

            // ensure focus so that menus go away after clicking outside
            SetForegroundWindow(_notificationArea.Handle);
            SetForegroundWindow(HWnd);

            uint wparam = GetMessageWParam(mouse);
            uint hiWord = GetMessageHiWord();

            if (button == MouseButton.Left)
            {
                if (DateTime.Now.Subtract(_lastLClick).TotalMilliseconds <= doubleClickTime)
                {
                    SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.LBUTTONDBLCLK | (hiWord << 16));
                }
                else
                {
                    SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.LBUTTONDOWN | (hiWord << 16));
                }

                SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.LBUTTONUP | (hiWord << 16));
                if (Version >= 4) SendNotifyMessage(HWnd, CallbackMessage, mouse, NIN_SELECT | (hiWord << 16));

                _lastLClick = DateTime.Now;
            }
            else if (button == MouseButton.Right)
            {
                if (DateTime.Now.Subtract(_lastRClick).TotalMilliseconds <= doubleClickTime)
                {
                    SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.RBUTTONDBLCLK | (hiWord << 16));
                }
                else
                {
                    SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.RBUTTONDOWN | (hiWord << 16));
                }

                SendNotifyMessage(HWnd, CallbackMessage, wparam, (uint)WM.RBUTTONUP | (hiWord << 16));
                if (Version >= 4) SendNotifyMessage(HWnd, CallbackMessage, mouse, (uint)WM.CONTEXTMENU | (hiWord << 16));

                _lastRClick = DateTime.Now;
            }
        }

        private uint GetMessageHiWord()
        {
            if (Version > 3)
            {
                return UID;
            }

            return 0;
        }

        private uint GetMessageWParam(uint mouse)
        {
            if (Version > 3)
            {
                return mouse;
            }

            return UID;
        }

        private bool RemoveIfInvalid()
        {
            if (!IsWindow(HWnd))
            {
                _notificationArea.TrayIcons.Remove(this);
                return true;
            }

            return false;
        }
        #endregion

        #region IEquatable<NotifyIcon> Members

        /// <summary>
        /// Checks the equality of the icon based on the hWnd and uID;
        /// </summary>
        /// <param name="other">The other NotifyIcon to compare to.</param>
        /// <returns>Indication of equality.</returns>
        public bool Equals(NotifyIcon other)
        {
            if (other == null) return false;

            return (HWnd.Equals(other.HWnd) && UID.Equals(other.UID)) || (other.GUID != Guid.Empty && GUID.Equals(other.GUID));
        }

        public bool Equals(SafeNotifyIconData other)
        {
            return (HWnd.Equals(other.hWnd) && UID.Equals(other.uID)) || (other.guidItem != Guid.Empty && GUID.Equals(other.guidItem));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string PropertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion
    }
}
