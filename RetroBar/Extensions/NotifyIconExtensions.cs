using ManagedShell.WindowsTray;
using RetroBar.Utilities;
using System.Collections.Generic;

namespace RetroBar.Extensions
{
    public static class NotifyIconExtensions
    {
        public static string GetInvertIdentifier(this NotifyIcon icon)
        {
            if (icon.GUID != default) return icon.GUID.ToString();
            else return icon.Path + ":" + icon.UID.ToString();
        }

        public static NotifyIconBehavior GetBehavior(this NotifyIcon icon)
        {
            if (icon.IsPinned)
            {
                return NotifyIconBehavior.AlwaysShow;
            }

            if (Settings.Instance.NotifyIconBehaviors.Find(setting => setting.Identifier == icon.Identifier) is NotifyIconBehaviorSetting iconSetting)
            {
                return iconSetting.Behavior;
            }

            return NotifyIconBehavior.HideWhenInactive;
        }

        public static void SetBehavior(this NotifyIcon icon, NotifyIconBehavior behavior)
        {
            var settings = new List<NotifyIconBehaviorSetting>(Settings.Instance.NotifyIconBehaviors);
            var currentSettingIndex = settings.FindIndex(setting => setting.Identifier == icon.Identifier);

            if (currentSettingIndex >= 0)
            {
                settings[currentSettingIndex] = new NotifyIconBehaviorSetting
                {
                    Identifier = icon.Identifier,
                    Behavior = behavior
                };
            }
            else
            {
                settings.Add(new NotifyIconBehaviorSetting
                {
                    Identifier = icon.Identifier,
                    Behavior = behavior
                });
            }

            Settings.Instance.NotifyIconBehaviors = settings;
            icon.OnPropertyChanged("IsPinned");
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
