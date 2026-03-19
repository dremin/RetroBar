using ManagedShell.ShellFolders;
using RetroBar.Utilities;
using System.Collections.Generic;

namespace RetroBar.Extensions
{
    public static class QuickLaunchIconExtensions
    {
        public static bool IsEnabledOnDisplay(this ShellFile file, string deviceName)
        {
            var setting = Settings.Instance.QuickLaunchIconSettings.Find(s => s.Path == file.Path);

            // No setting means show everywhere (default)
            if (setting.Path == null)
            {
                return true;
            }

            return setting.DisabledOnDisplays == null || !setting.DisabledOnDisplays.Contains(deviceName);
        }

        public static void SetEnabledOnDisplay(this ShellFile file, string deviceName, bool enabled)
        {
            var settings = new List<QuickLaunchIconSetting>(Settings.Instance.QuickLaunchIconSettings);
            var currentSettingIndex = settings.FindIndex(s => s.Path == file.Path);

            if (currentSettingIndex >= 0)
            {
                var disabledDisplays = new List<string>(settings[currentSettingIndex].DisabledOnDisplays ?? new List<string>());

                if (!enabled && !disabledDisplays.Contains(deviceName))
                {
                    disabledDisplays.Add(deviceName);
                }
                else if (enabled)
                {
                    disabledDisplays.Remove(deviceName);
                }

                if (disabledDisplays.Count == 0)
                {
                    // Back to default (show everywhere) — remove the entry entirely
                    settings.RemoveAt(currentSettingIndex);
                }
                else
                {
                    settings[currentSettingIndex] = new QuickLaunchIconSetting
                    {
                        Path = file.Path,
                        DisabledOnDisplays = disabledDisplays
                    };
                }
            }
            else
            {
                if (!enabled)
                {
                    settings.Add(new QuickLaunchIconSetting
                    {
                        Path = file.Path,
                        DisabledOnDisplays = new List<string> { deviceName }
                    });
                }
                else
                {
                    // Enabling on a display when no setting exists — already shown everywhere, nothing to do
                    return;
                }
            }

            Settings.Instance.QuickLaunchIconSettings = settings;
        }
    }
}