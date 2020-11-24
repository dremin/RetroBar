using System;

namespace ManagedShell.Configuration
{
    public class ShellSettings
    {
        public static ShellSettings Instance = new ShellSettings();
        private ShellSettings() { }

        public string[] PinnedNotifyIcons;

        public bool EnableTaskbar;

        public int TaskbarIconSize;
    }
}
