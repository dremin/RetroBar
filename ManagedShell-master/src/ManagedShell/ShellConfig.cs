using ManagedShell.Common.Enums;

namespace ManagedShell
{
    public struct ShellConfig
    {
        #region Tasks

        /// <summary>
        /// Controls whether the tasks service will be made available for use with ShellManager.<br />
        /// <br />
        /// By default, this is enabled.
        /// </summary>
        public bool EnableTasksService;
        
        /// <summary>
        /// Controls whether the tasks service will be initialized with ManagedShell.<br />
        /// It is possible to initialize the tasks service at a later time.<br />
        /// <br />
        /// By default, this is enabled.
        /// </summary>
        public bool AutoStartTasksService;

        /// <summary>
        /// Controls whether the tasks service will be multi-mon aware when using AutoStartTasksService.<br />
        /// This keeps the HMonitor property of each ApplicationWindow updated.<br />
        /// This setting applies only to Windows 7. The tasks service is always multi-mon aware on Windows 8 and newer.<br />
        /// <br />
        /// By default, this is enabled.
        /// </summary>
        public bool MultiMonAwareTasksService;

        /// <summary>
        /// This is the icon size that ManagedShell will request from each task.<br />
        /// <br />
        /// By default, the small size is used.
        /// </summary>
        public IconSize TaskIconSize;

        #endregion

        #region Tray

        /// <summary>
        /// Controls whether the tray services will be made available for use with ShellManager.<br />
        /// <br />
        /// By default, this is enabled.
        /// </summary>
        public bool EnableTrayService;
        
        /// <summary>
        /// Controls whether the tray service will be initialized with ManagedShell.<br />
        /// It is possible to initialize the tray service at a later time.<br />
        /// <br />
        /// By default, this is enabled.
        /// </summary>
        public bool AutoStartTrayService;

        /// <summary>
        /// The NotificationArea object outputs two collections, PinnedIcons and UnpinnedIcons.<br />
        /// This setting sets the initial NotifyIcons that should be included in the PinnedIcons collection.<br />
        /// <br />
        /// By default, ManagedShell will pin Action Center, Power, Network, and Volume.<br />
        /// <br />
        /// Each string in the array references the unique identifier for the NotifyIcon:<br />
        ///   - If the NotifyIcon has a valid GUID, use the GUID.<br />
        ///   - Otherwise, the string follows the format: "PathToExe:UID"
        /// </summary>
        public string[] PinnedNotifyIcons;
        
        #endregion
    }
}
