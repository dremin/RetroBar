using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RetroBar.Extensions
{
    public static class NotifyIconExtensions
    {

        public static string GetInvertIdentifier(this NotifyIcon icon)
        {
            if (icon.GUID != default) return icon.GUID.ToString();
            else return icon.Path + ":" + icon.UID.ToString();
        }

        public static bool CanInvert(this NotifyIcon icon) {
            return Settings.Instance.InvertNotifyIcons.Contains(icon.GetInvertIdentifier());
        }

        public static void SetCanInvert(this NotifyIcon icon, bool canInvert)
        {
            var identifier = icon.GetInvertIdentifier();
            var settings = new List<string>(Settings.Instance.InvertNotifyIcons);
            var changed = false;

            if (!canInvert)
            {
                changed = settings.Remove(identifier);
            }
            else if (!settings.Contains(identifier))
            {
                settings.Add(identifier);
                changed = true;
            }

            if (changed)
            {
                Settings.Instance.InvertNotifyIcons = settings;
            }
        }
    }
}
