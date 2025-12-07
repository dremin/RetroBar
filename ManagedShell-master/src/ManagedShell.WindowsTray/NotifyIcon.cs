using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Input;
using static ManagedShell.Interop.NativeMethods;
using ManagedShell.Common.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ManagedShell.Common.Helpers;

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
            MissedNotifications = new ObservableCollection<NotificationBalloon>();
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
            set
            {
                if (_isPinned == value)
                {
                    return;
                }

                if (value)
                {
                    Pin();
                }
                else
                {
                    Unpin();
                }

                // the backing var is updated by the above called methods
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
        /// Gets the order index of the item in the pinned icons
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

        public string Identifier
        {
            get
            {
                if (GUID != default) return GUID.ToString();
                else return Path + ":" + UID.ToString() + ":" + Title;
            }
        }

        #region Balloon Notifications

        public ObservableCollection<NotificationBalloon> MissedNotifications
        {
            get;
            set;
        }

        public event EventHandler<NotificationBalloonEventArgs> NotificationBalloonShown;

        internal void TriggerNotificationBalloon(NotificationBalloon balloonInfo)
        {
            NotificationBalloonEventArgs args = new NotificationBalloonEventArgs
            {
                Balloon = balloonInfo
            };

            NotificationBalloonShown?.Invoke(this, args);

            if (!args.Handled)
            {
                MissedNotifications.Add(balloonInfo);
            }
        }

        #endregion

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
                int insertPos = position;

                for (int i = 0; i < icons.Count; i++)
                {
                    if (IsEqualByIdentifier(icons[i]))
                    {
                        icons.RemoveAt(i);
                    }
                }

                if (PinOrder < position && position > 0)
                {
                    insertPos -= 1;
                }

                icons.Insert(insertPos, Identifier);
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
                OnPropertyChanged("IsPinned");
                _notificationArea.UpdatePinnedIcons();
            }
        }

        public void Unpin()
        {
            if (!IsPinned)
            {
                return;
            }

            List<string> icons = _notificationArea.PinnedNotifyIcons.ToList();

            for (int i = 0; i < icons.Count; i++)
            {
                if (IsEqualByIdentifier(icons[i]))
                {
                    icons.RemoveAt(i);
                }
            }

            _notificationArea.PinnedNotifyIcons = icons.ToArray();

            _isPinned = false;
            PinOrder = 0;

            OnPropertyChanged("IsPinned");
            _notificationArea.UpdatePinnedIcons();
        }

        public void SetPinValues()
        {
            var inPinnedList = false;

            for (int i = 0; i < _notificationArea.PinnedNotifyIcons.Length; i++)
            {
                string item = _notificationArea.PinnedNotifyIcons[i];
                if (IsEqualByIdentifier(item))
                {
                    inPinnedList = true;
                    PinOrder = i;
                    break;
                }
            }

            if (inPinnedList != _isPinned)
            {
                _isPinned = inPinnedList;
                OnPropertyChanged("IsPinned");
            }
        }

        public bool IsEqualByIdentifier(string otherIdentifier)
        {
            otherIdentifier = otherIdentifier.ToLower();

            if (otherIdentifier == GUID.ToString().ToLower())
            {
                return true;
            }

            if (GUID != default || Path == null)
            {
                // Ignore below checks
                return false;
            }

            string pathLower = Path.ToLower();

            if (otherIdentifier.StartsWith(pathLower + ":" + UID.ToString()))
            {
                return true;
            }

            string[] otherIdentifierArray = otherIdentifier.Split(new[] { ':' }, 4);

            if (otherIdentifierArray.Length < 4)
            {
                return false;
            }

            string cleanTitle = Title;
            if (cleanTitle == null)
            {
                cleanTitle = "";
            }

            if (otherIdentifierArray[0] + ":" + otherIdentifierArray[1] == pathLower && otherIdentifierArray[3] == cleanTitle.ToLower())
            {
                return true;
            }

            return false;
        }
        #endregion

        #region Mouse events

        private DateTime _lastLClick = DateTime.Now;
        private DateTime _lastMClick = DateTime.Now;
        private DateTime _lastRClick = DateTime.Now;

        public void IconMouseEnter(uint mouse)
        {
            if (RemoveIfInvalid()) return;

            SendMessage((uint)WM.MOUSEHOVER, mouse);

            if (Version > 3) SendMessage((uint)NIN.POPUPOPEN, mouse);
        }

        public void IconMouseLeave(uint mouse)
        {
            if (RemoveIfInvalid()) return;

            SendMessage((uint)WM.MOUSELEAVE, mouse);

            if (Version > 3) SendMessage((uint)NIN.POPUPCLOSE, mouse);
        }

        public void IconMouseMove(uint mouse)
        {
            if (RemoveIfInvalid()) return;

            SendMessage((uint)WM.MOUSEMOVE, mouse);
        }

        public void IconMouseDown(MouseButton button, uint mouse, int doubleClickTime)
        {
            // allow notify icon to focus so that menus go away after clicking outside
            GetWindowThreadProcessId(HWnd, out uint procId);
            AllowSetForegroundWindow(procId);

            if (button == MouseButton.Left)
            {
                if (handleClickOverride(false))
                {
                    return;
                }

                if (DateTime.Now.Subtract(_lastLClick).TotalMilliseconds <= doubleClickTime)
                {
                    SendMessage((uint)WM.LBUTTONDBLCLK, mouse);
                }
                else
                {
                    SendMessage((uint)WM.LBUTTONDOWN, mouse);
                }

                _lastLClick = DateTime.Now;
            }
            else if (button == MouseButton.Middle)
            {
                if (handleClickOverride(false))
                {
                    return;
                }

                if (DateTime.Now.Subtract(_lastMClick).TotalMilliseconds <= doubleClickTime)
                {
                    SendMessage((uint)WM.MBUTTONDBLCLK, mouse);
                }
                else
                {
                    SendMessage((uint)WM.MBUTTONDOWN, mouse);
                }

                _lastMClick = DateTime.Now;
            }
            else if (button == MouseButton.Right)
            {
                if (DateTime.Now.Subtract(_lastRClick).TotalMilliseconds <= doubleClickTime)
                {
                    SendMessage((uint)WM.RBUTTONDBLCLK, mouse);
                }
                else
                {
                    SendMessage((uint)WM.RBUTTONDOWN, mouse);
                }

                _lastRClick = DateTime.Now;
            }
        }

        public void IconMouseUp(MouseButton button, uint mouse, int doubleClickTime)
        {
            ShellLogger.Debug($"NotifyIcon: {button} mouse button clicked: {Title}");

            if (button == MouseButton.Left)
            {
                if (handleClickOverride(true))
                {
                    return;
                }

                SendMessage((uint)WM.LBUTTONUP, mouse);

                // This is documented as version 4, but Explorer does this for version 3 as well
                if (Version >= 3) SendMessage((uint)NIN.SELECT, mouse);

                _lastLClick = DateTime.Now;
            }
            else if (button == MouseButton.Middle)
            {
                if (handleClickOverride(true))
                {
                    return;
                }

                SendMessage((uint)WM.MBUTTONUP, mouse);

                _lastMClick = DateTime.Now;
            }
            else if (button == MouseButton.Right)
            {
                SendMessage((uint)WM.RBUTTONUP, mouse);

                // This is documented as version 4, but Explorer does this for version 3 as well
                if (Version >= 3) SendMessage((uint)WM.CONTEXTMENU, mouse);

                _lastRClick = DateTime.Now;
            }
        }

        internal bool SendMessage(uint message, uint mouse)
        {
            return SendNotifyMessage(HWnd, CallbackMessage, GetMessageWParam(mouse), message | (GetMessageHiWord() << 16));
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

        private bool handleClickOverride(bool performAction)
        {
            if (NotificationArea.Win11ActionCenterIcons.Contains(GUID.ToString()) && EnvironmentHelper.IsWindows11OrBetter)
            {
                if (performAction)
                {
                    ShellHelper.ShowActionCenter();
                }

                if (!EnvironmentHelper.IsWindows1122H2OrBetter)
                {
                    // Some earlier Windows 11 versions have a crash when sending click events
                    return true;
                }
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
