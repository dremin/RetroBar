using ManagedShell.Common.Extensions;
using Microsoft.Win32;

namespace ManagedShell.Common.Helpers
{
    public static class GroupPolicyHelper
    {
        const string ExplorerPolicyKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";


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
        public static bool NoDesktop => Registry.CurrentUser.GetSubKeyValue<int>(ExplorerPolicyKey, "NoDesktop") == 1;

        /// <summary>
        /// This policy setting allows you to remove Security and Maintenance from the system control area.
        ///
        /// If you enable this policy setting, the Security and Maintenance icon is not displayed in the system notification area.
        ///
        /// If you disable or do not configure this policy setting, the Security and Maintenance icon is displayed in the system notification area. 
        /// </summary>
        /// <remarks>
        /// Policy: Remove the Security and Maintenance icon
        /// Category Path: User Configuration\Administrative Templates\Start Menu and Taskbar\
        /// Supported On: At least Microsoft Windows Vista
        /// Registry Key: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
        /// Value: HideSCAHealth
        /// Admx: Taskbar.admx
        /// Documentation: https://gpsearch.azurewebsites.net/#4678
        /// </remarks>
        public static bool HideScaHealth => Registry.CurrentUser.GetSubKeyValue<int>(ExplorerPolicyKey, "HideSCAHealth") == 1;

        /// <summary>
        /// This policy setting allows you to remove the Meet Now icon from the system control area.
        ///
        /// If you enable this policy setting, the Meet Now icon is not displayed in the system notification area.
        /// 
        /// If you disable or do not configure this policy setting, the Meet Now icon is displayed in the system notification area. 
        /// </summary>
        /// <remarks>
        /// Policy: Remove the volume control icon
        /// Category Path: User Configuration\Administrative Templates\Start Menu and Taskbar\
        /// Supported On: At least Microsoft Windows Vista
        /// Registry Key: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
        /// Value: HideSCAMeetNow
        /// Admx: Taskbar.admx
        /// Documentation: https://gpsearch.azurewebsites.net/#15675
        /// </remarks>
        public static bool HideScaMeetNow => Registry.CurrentUser.GetSubKeyValue<int>(ExplorerPolicyKey, "HideSCAMeetNow") == 1;

        /// <summary>
        /// This policy setting allows you to remove the networking icon from the system control area.
        ///
        /// If you enable this policy setting, the networking icon is not displayed in the system notification area.
        ///
        /// If you disable or do not configure this policy setting, the networking icon is displayed in the system notification area. 
        /// </summary>
        /// <remarks>
        /// Policy: Remove the networking icon
        /// Category Path: User Configuration\Administrative Templates\Start Menu and Taskbar\
        /// Supported On: At least Microsoft Windows Vista
        /// Registry Key: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
        /// Value: HideSCANetwork
        /// Admx: Taskbar.admx
        /// Documentation: https://gpsearch.azurewebsites.net/#4676
        /// </remarks>
        public static bool HideScaNetwork => Registry.CurrentUser.GetSubKeyValue<int>(ExplorerPolicyKey, "HideSCANetwork") == 1;

        /// <summary>
        /// This policy setting allows you to remove the battery meter from the system control area.
        ///
        /// If you enable this policy setting, the battery meter is not displayed in the system notification area.
        ///
        /// If you disable or do not configure this policy setting, the battery meter is displayed in the system notification area. 
        /// </summary>
        /// <remarks>
        /// Policy: Remove the battery meter
        /// Category Path: User Configuration\Administrative Templates\Start Menu and Taskbar\
        /// Supported On: At least Microsoft Windows Vista
        /// Registry Key: HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
        /// Value: HideSCAPower
        /// Admx: Taskbar.admx
        /// Documentation: https://gpsearch.azurewebsites.net/#4675
        /// </remarks>
        public static bool HideScaPower => Registry.CurrentUser.GetSubKeyValue<int>(ExplorerPolicyKey, "HideSCAPower") == 1;

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
        public static bool HideScaVolume => Registry.CurrentUser.GetSubKeyValue<int>(ExplorerPolicyKey, "HideSCAVolume") == 1;
    }
}