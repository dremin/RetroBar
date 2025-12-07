using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.WindowsTray
{
    public class NotificationBalloon
    {
        public string Info { get; internal set; }

        public string Title { get; internal set; }

        public NIIF Flags { get; internal set; }

        public ImageSource Icon { get; internal set; }

        public int Timeout { get; internal set; }

        public DateTime Received { get; internal set; }

        public readonly NotifyIcon NotifyIcon;

        public NotificationBalloon() { }

        public NotificationBalloon(SafeNotifyIconData nicData, NotifyIcon notifyIcon)
        {
            NotifyIcon = notifyIcon;

            Title = nicData.szInfoTitle;
            Info = nicData.szInfo;
            Flags = nicData.dwInfoFlags;
            if ((int)nicData.uVersion >= 1000)
            {
                // Potentially valid timeout provided
                Timeout = (int)nicData.uVersion;
            }
            else
            {
                // Fall back to the system duration
                uint timeoutDuration = 5;
                SystemParametersInfo(SPI.GETMESSAGEDURATION, 0, ref timeoutDuration, SPIF.None);
                // Convert from sec to msec
                Timeout = (int)timeoutDuration * 1000;
            }
            Received = DateTime.Now;

            if (Flags.HasFlag(NIIF.ERROR))
            {
                Icon = GetSystemIcon(SystemIcons.Error.Handle);
            }
            else if (Flags.HasFlag(NIIF.INFO))
            {
                Icon = GetSystemIcon(SystemIcons.Information.Handle);
            }
            else if (Flags.HasFlag(NIIF.WARNING))
            {
                Icon = GetSystemIcon(SystemIcons.Warning.Handle);
            }
            else if (Flags.HasFlag(NIIF.USER))
            {
                if (nicData.hBalloonIcon != 0)
                {
                    SetIconFromHIcon((IntPtr)nicData.hBalloonIcon);
                }
                else if (nicData.hIcon != IntPtr.Zero)
                {
                    SetIconFromHIcon(nicData.hIcon);
                }
            }
        }

        public void SetVisibility(BalloonVisibility visibility)
        {
            if (NotifyIcon == null)
            {
                ShellLogger.Error("NotificationBalloon: NotifyIcon is null");
                return;
            }

            switch (visibility)
            {
                case BalloonVisibility.Visible:
                    NotifyIcon.SendMessage((uint)NIN.BALLOONSHOW, 0);
                    break;
                case BalloonVisibility.Hidden:
                    NotifyIcon.SendMessage((uint)NIN.BALLOONHIDE, 0);
                    break;
                case BalloonVisibility.TimedOut:
                    NotifyIcon.SendMessage((uint)NIN.BALLOONTIMEOUT, 0);
                    break;
                default:
                    break;
            }
        }

        public void Click()
        {
            if (NotifyIcon == null)
            {
                ShellLogger.Error("NotificationBalloon: NotifyIcon is null");
                return;
            }

            NotifyIcon.SendMessage((uint)NIN.BALLOONUSERCLICK, 0);
        }

        private void SetIconFromHIcon(IntPtr hIcon)
        {
            if (hIcon == IntPtr.Zero)
            {
                if (Icon == null)
                {
                    // Use default only if we don't have a valid icon already
                    Icon = IconImageConverter.GetDefaultIcon();
                }

                return;
            }

            ImageSource icon = IconImageConverter.GetImageFromHIcon(hIcon, false);

            if (icon != null)
            {
                Icon = icon;
            }
            else if (icon == null && Icon == null)
            {
                // Use default only if we don't have a valid icon already
                Icon = IconImageConverter.GetDefaultIcon();
            }
        }

        private BitmapSource GetSystemIcon(IntPtr hIcon)
        {
            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bs.Freeze();

            return bs;
        }
    }
}
