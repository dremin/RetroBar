using ManagedShell.Common.DesignPatterns;
using Microsoft.Win32;

namespace ManagedShell.Common.Helpers
{
    public sealed class GroupPolicyManager : SingletonObject<GroupPolicyManager>
    {
        const string ExplorerPolicyKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";

        private GroupPolicyManager()
        {
        }

        /// <summary>
        /// Removes icons, shortcuts, and other default and user-defined items from the desktop, including 
        /// Briefcase, Recycle Bin, Computer, and Network Locations.
        ///
        /// Removing icons and shortcuts does not prevent the user from using another method to start the 
        /// programs or opening the items they represent.
        ///
        /// Also, see "Items displayed in Places Bar" in User Configuration\Administrative Templates\Windows 
        /// Components\Common Open File Dialog to remove the Desktop icon from the Places Bar.This will help 
        /// prevent users from saving data to the Desktop.
        /// </summary>
        /// <remarks>
        /// Policy: Hide and disable all items on the desktop
        /// Category Path: User Configuration\Administrative Templates\Desktop\
        /// Supported On: At least Microsoft Windows 2000
        /// Registry Key: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
        /// Value: NoDesktop
        /// Admx: Desktop.admx
        /// Documentation: https://gpsearch.azurewebsites.net/#146
        /// </remarks>
        public bool NoDesktop =>
            GetRegistryValue<int>(Registry.CurrentUser, ExplorerPolicyKey, "NoDesktop") == 1;

        /// <summary>
        /// This policy setting allows you to remove the volume control icon from the system control area.
        /// 
        /// If you enable this policy setting, the volume control icon is not displayed in the system notification area.
        ///
        /// If you disable or do not configure this policy setting, the volume control icon is displayed in the system notification area.
        /// </summary>
        /// <remarks>
        /// Policy: Remove the volume control icon
        /// Category Path: User Configuration\Administrative Templates\Start Menu and Taskbar\
        /// Supported On: At least Microsoft Windows Vista
        /// Registry Key: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
        /// Value: HideSCAVolume
        /// Admx: Taskbar.admx
        /// Documentation: https://gpsearch.azurewebsites.net/#4677
        /// </remarks>
        public bool HideScaVolume =>
            GetRegistryValue<int>(Registry.CurrentUser, ExplorerPolicyKey, "HideSCAVolume") == 1;

        private static T GetRegistryValue<T>(RegistryKey key, string subKey, string valueName)
        {
            var reg = key?.OpenSubKey(subKey);
            object val = reg?.GetValue(valueName, default);
            if (val is T value)
                return value;
            
            return default;
        }
    }
}